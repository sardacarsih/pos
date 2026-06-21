namespace Migrator
{
    /// <summary>
    /// Satu migrasi: identitas unik + daftar statement SQL yang dijalankan berurutan.
    /// Statement DDL dibungkus agar idempotent (lihat <see cref="Migrations"/>).
    /// </summary>
    public sealed record Migration(string Id, string Description, string[] Statements);
}
