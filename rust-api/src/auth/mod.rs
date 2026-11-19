pub mod password;
pub mod throttle;
pub mod token;

use std::str::FromStr;

#[derive(Debug, Clone, Copy, PartialEq, Eq, serde::Serialize, serde::Deserialize)]
#[serde(rename_all = "UPPERCASE")]
pub enum Role {
    Kasir,
    Admin,
    Anggota,
}

impl Role {
    pub fn as_str(self) -> &'static str {
        match self {
            Role::Kasir => "KASIR",
            Role::Admin => "ADMIN",
            Role::Anggota => "ANGGOTA",
        }
    }
}

impl FromStr for Role {
    type Err = String;
    fn from_str(s: &str) -> Result<Self, String> {
        match s.to_ascii_uppercase().as_str() {
            "KASIR" => Ok(Role::Kasir),
            "ADMIN" => Ok(Role::Admin),
            "ANGGOTA" => Ok(Role::Anggota),
            _ => Err(format!("role tidak dikenal: {s}")),
        }
    }
}
