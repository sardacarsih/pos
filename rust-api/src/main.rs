use std::{env, net::SocketAddr, process::ExitCode, sync::Arc, time::Duration};

use pos_api::{
    auth::{Role, password, throttle::IpLimiter},
    config::Config,
    db,
    routes::{self, AppState},
};

const USAGE: &str = "Pemakaian:\n  pos-api [serve]\n  pos-api migrate\n  pos-api user-add --username U --nama N --role KASIR|ANGGOTA|ADMIN [--nik NIK]";

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
            let n = db::run(&pool, db::migrate)
                .await
                .map_err(|e| e.to_string())?;
            println!("Migrasi selesai ({n} statement baru).");
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
