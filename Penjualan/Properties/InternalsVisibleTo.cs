using System.Runtime.CompilerServices;

// Expose internal members (e.g. FakturPenjualan.ValidateCreditLimit) to the test assembly
// so the credit-limit DB path can be exercised end-to-end in integration tests.
[assembly: InternalsVisibleTo("Penjualan.Tests")]
