namespace Penjualan.Model
{
    /// <summary>One POS_PERIODE row: date ranges per remise/bulanan plus their closed flags.</summary>
    public class PeriodeRow
    {
        public int Periode { get; set; }
        public DateTime? R1Dari { get; set; }
        public DateTime? R1Sampai { get; set; }
        public DateTime? R2Dari { get; set; }
        public DateTime? R2Sampai { get; set; }
        public DateTime? BDari { get; set; }
        public DateTime? BSampai { get; set; }
        public bool Remise1Tutup { get; set; }
        public bool Remise2Tutup { get; set; }
        public bool BulananTutup { get; set; }
    }
}
