use std::{env, net::SocketAddr, process::ExitCode, sync::Arc, time::Duration};

use pos_api::{
    auth::{Role, password, throttle::IpLimiter},
    config::Config,
    db,
    routes::{self, AppState},
};

const USAGE: &str = "Pemakaian:\n  pos-api [serve]\n  pos-api migrate\n  pos-api selftest   (query baca saja; keluar kode 1 bila ada yang GAGAL)\n  pos-api user-add --username U --nama N --role KASIR|ANGGOTA|ADMIN [--nik NIK]\n  pos-api user-add-anggota --xlsx FILE.xlsx | --dry-run   (akun semua anggota aktif yang belum punya akun)";

fn arg(args: &[String], name: &str) -> Option<String> {
    args.iter()
        .position(|a| a == name)
        .and_then(|i| args.get(i + 1))
        .cloned()
}

#[tokio::main]
async fn main() -> ExitCode {
    tracing_subscriber::fmt()
        .with_env_filter(
            tracing_subscriber::EnvFilter::try_from_default_env().unwrap_or_else(|_| "info".into()),
        )
        .init();
    match run().await {
        Ok(()) => ExitCode::SUCCESS,
        Err(e) => {
            eprintln!("Gagal: {e}");
            ExitCode::FAILURE
        }
    }
}

async fn run() -> Result<(), String> {
    let args: Vec<String> = env::args().skip(1).collect();
    let cmd = args.first().map(String::as_str).unwrap_or("serve");
    if matches!(cmd, "-h" | "--help" | "help") {
        println!("{USAGE}");
        return Ok(());
    }
    let cfg = Config::from_env()?;
    let pool = db::connect(&cfg)?;

    match cmd {
        "migrate" => {
            let user = arg(&args, "--admin-user").unwrap_or_else(|| "admin".into());
            let given = arg(&args, "--admin-password").filter(|p| !p.is_empty());
            let acak = given.is_none();
            // Tanpa password eksplisit: acak, tampil sekali (tidak ada akun/password bawaan).
            let pwd = given.unwrap_or_else(|| password::temp_password() + &password::temp_password());
            let hash = password::hash_desktop(&pwd);
            if args.iter().any(|a| a == "--reset-admin") {
                let ada = db::run(&pool, move |c| db::desktop_migrations::reset_admin(c, &user, &hash))
                    .await
                    .map_err(|e| e.to_string())?;
                if !ada {
                    return Err("user admin desktop tidak ditemukan".into());
                }
                println!("Password admin direset; akun diaktifkan dan dibuka kuncinya.");
                if acak {
                    println!("Password baru acak (tampil sekali): {pwd}");
                }
                return Ok(());
            }
            let skip_admin = args.iter().any(|a| a == "--skip-admin");
            let (n_desktop, n_mob, dibuat) = db::run(&pool, move |c| {
                println!("Migrasi desktop:");
                let n_desktop = db::desktop_migrations::jalankan(c, &mut |l| println!("{l}"))?;
                let n_mob = db::migrate(c)?;
                let dibuat = !skip_admin && db::desktop_migrations::seed_admin(c, &user, &hash)?;
                Ok((n_desktop, n_mob, dibuat))
            })
            .await
            .map_err(|e| e.to_string())?;
            println!("{n_desktop} migrasi desktop baru, {n_mob} statement MOB_* baru.");
            if dibuat {
                println!("User admin desktop dibuat.");
                if acak {
                    println!("Password admin acak (tampil sekali): {pwd}");
                }
            }
            Ok(())
        }
        "selftest" => {
            let hasil = db::run(&pool, pos_api::selftest::jalankan)
                .await
                .map_err(|e| e.to_string())?;
            for h in &hasil {
                println!("{}", h.baris());
            }
            let (ok, gagal) = pos_api::selftest::ringkas(&hasil);
            println!("RINGKAS: {ok} OK, {gagal} GAGAL");
            if gagal > 0 {
                return Err(format!("{gagal} pemeriksaan selftest GAGAL"));
            }
            Ok(())
        }
        "user-add" => {
            let username = arg(&args, "--username").ok_or(USAGE)?.to_lowercase();
            let nama = arg(&args, "--nama").ok_or(USAGE)?;
            let role: Role = arg(&args, "--role").ok_or(USAGE)?.parse()?;
            let nik = arg(&args, "--nik");
            if !routes::admin::username_valid(&username) {
                return Err("username tidak valid (3-50 huruf kecil/angka/./_/-)".into());
            }
            if (role == Role::Anggota) != nik.is_some() {
                return Err("--nik wajib untuk ANGGOTA dan hanya untuk ANGGOTA".into());
            }
            let tmp = password::temp_password();
            let hash = password::hash(&tmp).map_err(|e| e.to_string())?;
            db::run(&pool, move |c| {
                db::user::create_user(c, &username, &nama, &hash, role, nik.as_deref()).map(|_| ())
            })
            .await
            .map_err(|e| e.to_string())?;
            // Ditampilkan sekali; wajib diganti saat login pertama.
            println!("User dibuat. Password sementara: {tmp}");
            Ok(())
        }
        "user-add-anggota" => {
            let xlsx = arg(&args, "--xlsx");
            let dry = args.iter().any(|a| a == "--dry-run");
            if xlsx.is_none() && !dry {
                return Err(USAGE.into());
            }
            let default_nik = cfg.default_nik.clone();
            let h = db::run(&pool, move |c| pos_api::anggota_massal::jalankan(c, &default_nik, dry))
                .await
                .map_err(|e| e.to_string())?;
            println!(
                "{}: {} akun, dilewati/gagal: {}",
                if dry { "Dry-run (tidak ada yang ditulis)" } else { "Dibuat" },
                h.dibuat.len(),
                h.dilewati.len()
            );
            if let (Some(f), false) = (xlsx, dry) {
                pos_api::anggota_massal::tulis_xlsx(&f, &h).map_err(|e| {
                    format!("akun sudah dibuat tetapi file gagal ditulis ({e}); reset password lewat admin")
                })?;
                println!("Daftar password sementara: {f} (berisi password, hapus setelah dibagikan)");
            }
            Ok(())
        }
        "serve" => {
            if let Err(error) = pos_api::push::start(pool.clone()) {
                tracing::error!(%error, "Worker FCM tidak berjalan; penjualan tetap tersedia");
            }
            let state = AppState {
                pool,
                cfg: Arc::new(cfg.clone()),
                limiter: Arc::new(IpLimiter::new(20, Duration::from_secs(60))),
            };
            let listener = tokio::net::TcpListener::bind(&cfg.bind)
                .await
                .map_err(|e| format!("bind {}: {e}", cfg.bind))?;
            tracing::info!(
                "mendengarkan di {} (HTTPS dihentikan di reverse proxy)",
                cfg.bind
            );
            axum::serve(
                listener,
                routes::router(state).into_make_service_with_connect_info::<SocketAddr>(),
            )
            .await
            .map_err(|e| e.to_string())
        }
        _ => Err(USAGE.into()),
    }
}
