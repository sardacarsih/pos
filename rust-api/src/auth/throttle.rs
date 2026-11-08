use std::{
    collections::{HashMap, VecDeque},
    sync::Mutex,
    time::{Duration, Instant},
};

/// Kunci akun sementara setelah gagal berturut-turut: 5 gagal -> 15 menit, lalu berlipat
/// (30, 60, 120, 240 menit, maks 240). Di bawah 5 gagal tidak dikunci.
pub fn lock_minutes(failed: i32) -> Option<i64> {
    if failed < 5 {
        return None;
    }
    Some(15 * (1i64 << (failed - 5).min(4)))
}

/// Pembatas laju per IP (jendela geser) untuk endpoint login/refresh.
pub struct IpLimiter {
    max: usize,
    window: Duration,
    hits: Mutex<HashMap<String, VecDeque<Instant>>>,
}

impl IpLimiter {
    pub fn new(max: usize, window: Duration) -> Self {
        Self {
            max,
            window,
            hits: Mutex::new(HashMap::new()),
        }
    }

    /// True bila permintaan diizinkan (dan dicatat).
    pub fn allow(&self, ip: &str) -> bool {
        self.allow_at(ip, Instant::now())
    }

    fn allow_at(&self, ip: &str, now: Instant) -> bool {
        let mut map = self.hits.lock().unwrap_or_else(|e| e.into_inner());
        if map.len() > 10_000 {
            map.retain(|_, q| {
                q.back()
                    .is_some_and(|t| now.duration_since(*t) < self.window)
            });
        }
        let q = map.entry(ip.to_string()).or_default();
        while q
            .front()
            .is_some_and(|t| now.duration_since(*t) >= self.window)
        {
            q.pop_front();
        }
        if q.len() >= self.max {
            return false;
        }
        q.push_back(now);
        true
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn lock_policy() {
        assert_eq!(lock_minutes(4), None);
        assert_eq!(lock_minutes(5), Some(15));
        assert_eq!(lock_minutes(6), Some(30));
        assert_eq!(lock_minutes(20), Some(240));
    }

    #[test]
    fn limiter_window() {
        let l = IpLimiter::new(2, Duration::from_secs(60));
        let t = Instant::now();
        assert!(l.allow_at("1.1.1.1", t));
        assert!(l.allow_at("1.1.1.1", t));
        assert!(!l.allow_at("1.1.1.1", t));
        assert!(l.allow_at("2.2.2.2", t));
        assert!(l.allow_at("1.1.1.1", t + Duration::from_secs(61)));
    }
}
