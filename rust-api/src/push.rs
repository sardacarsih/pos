use std::{fs, time::Duration};

use jsonwebtoken::{Algorithm, EncodingKey, Header, encode};
use serde::{Deserialize, Serialize};
use serde_json::json;

use crate::db::{self, DbPool};

#[derive(Deserialize)]
struct Credentials {
    client_email: String,
    private_key: String,
    token_uri: String,
}

#[derive(Serialize)]
struct Assertion<'a> {
    iss: &'a str,
    scope: &'a str,
    aud: &'a str,
    iat: i64,
    exp: i64,
}

/// Tag = nomor transaksi: kirim ulang (retry) mengganti notifikasi yang sama, bukan menumpuk dobel.
fn message(token: &str, no: &str) -> serde_json::Value {
    json!({"message": {"token": token, "notification": {"title": "Kopkar Kusuma Lestari", "body": "Ada transaksi baru pada akun Anda."}, "data": {"type": "transaction", "no": no}, "android": {"priority": "high", "ttl": "86400s", "notification": {"tag": no, "channel_id": "transaksi_suara2", "sound": "notif_transaksi"}}}})
}

fn token_unregistered(body: &serde_json::Value) -> bool {
    body["error"]["details"]
        .as_array()
        .is_some_and(|d| d.iter().any(|v| v["errorCode"] == "UNREGISTERED"))
}

pub fn start(pool: DbPool) -> Result<(), String> {
    let Ok(project) = std::env::var("POS_FCM_PROJECT_ID") else {
        tracing::info!("FCM tidak dikonfigurasi; worker notifikasi tidak berjalan");
        return Ok(());
    };
    if project.is_empty()
        || !project
            .bytes()
            .all(|c| c.is_ascii_alphanumeric() || c == b'-')
    {
        return Err("POS_FCM_PROJECT_ID tidak valid".into());
    }
    let path = std::env::var("GOOGLE_APPLICATION_CREDENTIALS")
        .map_err(|_| "GOOGLE_APPLICATION_CREDENTIALS belum diset")?;
    let contents =
        fs::read_to_string(path).map_err(|_| "File kredensial FCM tidak dapat dibaca")?;
    let credentials: Credentials =
        serde_json::from_str(&contents).map_err(|_| "Format kredensial FCM tidak valid")?;
    if credentials.token_uri != "https://oauth2.googleapis.com/token" {
        return Err("token_uri kredensial FCM tidak valid".into());
    }
    let key = EncodingKey::from_rsa_pem(credentials.private_key.as_bytes())
        .map_err(|_| "Kunci service account tidak valid")?;
    let client = reqwest::Client::builder()
        .timeout(Duration::from_secs(20))
        .build()
        .map_err(|e| e.to_string())?;
    tokio::spawn(async move {
        let mut cached: Option<(String, i64)> = None;
        let mut last_purge = std::time::Instant::now();
        loop {
            match db::run(&pool, db::push::claim).await {
                Ok(Some(event)) => {
                    let nik = event.nik.clone();
                    let result = async {
                        let devices = db::run(&pool, move |c| db::push::devices(c, &nik)).await.map_err(|_| ())?;
                        if devices.is_empty() { return Ok::<bool, ()>(false); }
                        let now = chrono::Utc::now().timestamp();
                        if cached.as_ref().is_none_or(|(_, until)| *until <= now + 60) {
                            let assertion = encode(&Header::new(Algorithm::RS256), &Assertion {
                                iss: &credentials.client_email, scope: "https://www.googleapis.com/auth/firebase.messaging",
                                aud: &credentials.token_uri, iat: now, exp: now + 3600,
                            }, &key).map_err(|_| ())?;
                            let response = client.post(&credentials.token_uri).form(&[("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"), ("assertion", assertion.as_str())]).send().await.map_err(|_| ())?.error_for_status().map_err(|_| ())?;
                            let data: serde_json::Value = response.json().await.map_err(|_| ())?;
                            let token = data["access_token"].as_str().ok_or(())?.to_owned();
                            let ttl = data["expires_in"].as_i64().unwrap_or(3600).clamp(60, 3600);
                            cached = Some((token, now + ttl));
                        }
                        let bearer = cached.as_ref().ok_or(())?.0.clone();
                        let mut retry = false;
                        for token in devices {
                            let response = client.post(format!("https://fcm.googleapis.com/v1/projects/{project}/messages:send"))
                                .bearer_auth(&bearer).json(&message(&token, &event.number))
                                .send().await;
                            match response {
                                Ok(r) if r.status().is_success() => {
                                    tracing::info!(no = %event.number, "Notifikasi FCM terkirim");
                                },
                                Ok(r) => {
                                    let status = r.status();
                                    let body: serde_json::Value = r.json().await.unwrap_or_default();
                                    let invalid = token_unregistered(&body);
                                    if invalid {
                                        let t = token.clone();
                                        if db::run(&pool, move |c| db::push::invalid_token(c, &t)).await.is_err() { retry = true; }
                                    } else {
                                        if status.as_u16() == 401 { cached = None; }
                                        tracing::warn!(status = %status, galat = %body["error"]["status"], no = %event.number, "Pengiriman FCM gagal; akan dicoba lagi");
                                        retry = true;
                                    }
                                }
                                Err(_) => { tracing::warn!("Koneksi FCM gagal; akan dicoba lagi"); retry = true; }
                            }
                        }
                        Ok(retry)
                    }.await;
                    let retry = result.unwrap_or(true);
                    if let Err(e) = db::run(&pool, move |c| {
                        db::push::finish(c, event.id, &event.lease, retry)
                    })
                    .await
                    {
                        tracing::error!(error = %e, "Gagal memperbarui antrean notifikasi");
                    }
                }
                Ok(None) => {
                    if last_purge.elapsed() >= Duration::from_secs(3600) {
                        last_purge = std::time::Instant::now();
                        if let Err(e) = db::run(&pool, db::push::purge).await {
                            tracing::warn!(error = %e, "Gagal membersihkan antrean notifikasi lama");
                        }
                    }
                    tokio::time::sleep(Duration::from_secs(5)).await
                }
                Err(e) => {
                    tracing::error!(error = %e, "Gagal membaca antrean notifikasi");
                    tokio::time::sleep(Duration::from_secs(30)).await;
                }
            }
        }
    });
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn pesan_memakai_tag_nomor_transaksi() {
        let m = message("tok", "TRX-1");
        assert_eq!(m["message"]["android"]["notification"]["tag"], "TRX-1");
        assert_eq!(m["message"]["data"]["no"], "TRX-1");
    }

    #[test]
    fn hanya_token_unregistered_dihapus() {
        assert!(token_unregistered(
            &json!({"error":{"details":[{"errorCode":"UNREGISTERED"}]}})
        ));
        assert!(!token_unregistered(
            &json!({"error":{"status":"INVALID_ARGUMENT"}})
        ));
        assert!(!token_unregistered(
            &json!({"error":{"status":"UNAVAILABLE"}})
        ));
    }
}
