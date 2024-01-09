using Dapper;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraEditors; // Make sure to include this namespace for FormatConditionRuleAppearance
using System.Drawing;
using Oracle.ManagedDataAccess.Client;
using Penjualan.Model;
using System.Data;
using DevExpress.XtraGrid;

namespace Penjualan
{
    public partial class ProductForm : DevExpress.XtraEditors.XtraForm
    {
        List<DTOPRODUCT_WSTOCK> ListItemsBarang;
        private int productid;
        private string kode_item;
        private string barcode;
        private string productname;
        private string satuan;
        private decimal price;
        private decimal hpp;
        public void SetSearchPanelValue(string searchValue)
        {
            gridView1.ApplyFindFilter(searchValue);
        }

        public Int32 ProductId
        {
            get { return productid; }
        }
        public string Kode_Item
        {
            get { return kode_item; }
        }
        public string Barcode
        {
            get { return barcode; }
        }
        public string ProductName
        {
            get { return productname; }
        }
        public string Satuan
        {
            get { return satuan; }
        }

        public decimal Price
        {
            get { return price; }
        }
        public decimal Hpp
        {
            get { return hpp; }
        }


        public ProductForm()
        {
            InitializeComponent();
        }

        private void ProductForm_Load(object sender, EventArgs e)
        {
            this.KeyPreview = true;
            this.KeyPress += new KeyPressEventHandler(ProductForm_KeyPress);

            var startdate = new DateTime(DateTime.Today.Year, 1, 1);
            var enddate = DateTime.Today;
            ListItemsBarang = GetProductList(startdate,enddate);
            gridControl1.DataSource = ListItemsBarang;
            gridView1.Columns["PRODUCTID"].Visible = false;
            //gridView1.Columns["BARCODE"].Visible = false;
            gridView1.Columns["BARCODE"].Width = 150;
            gridView1.Columns["BELI"].Visible = false;
            gridView1.Columns["KODE_ITEM"].Width = 100;
            gridView1.Columns["PRODUCTNAME"].Width = 300;
            gridView1.Columns["SATUAN"].Width = 80;
            gridView1.Columns["PRICE"].Width = 90;
            gridView1.Columns["PRICE"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["PRICE"].DisplayFormat.FormatString = "n0";

            GridFormatRule gridFormatRule = new ();
            FormatConditionRuleExpression formatConditionRuleExpression = new FormatConditionRuleExpression();

            // Replace "colStock" with the correct DataColumn
            gridFormatRule.Column = gridView1.Columns["STOCK"]; // Assuming "Stock" is the correct column name
            gridFormatRule.ApplyToRow = true;

            formatConditionRuleExpression.PredefinedName = "Red Fill, Red Text";
            formatConditionRuleExpression.Expression = "[STOCK] < 0";  // Update the condition to check if "Stock" is less than 0

            gridFormatRule.Rule = formatConditionRuleExpression;
            gridView1.FormatRules.Add(gridFormatRule);


        }
        private void gridView1_DoubleClick(object sender, EventArgs e)
        {
            Get_Product_Item();
        }

        private void Get_Product_Item()
        {
            // FROM FILTERED LIST
            if (gridView1.SelectedRowsCount > 0)
            {
                int selectedIndex = gridView1.GetSelectedRows()[0];
                int selectedHandle = gridView1.GetVisibleRowHandle(selectedIndex);
                DTOPRODUCT_WSTOCK selectedItem = gridView1.GetRow(selectedHandle) as DTOPRODUCT_WSTOCK;

                // Rest of the code remains the same
                productid = selectedItem.PRODUCTID;
                barcode = selectedItem.BARCODE;
                kode_item = selectedItem.KODE_ITEM;
                productname = selectedItem.PRODUCTNAME;
                satuan = selectedItem.SATUAN;
                price = selectedItem.PRICE;
                hpp = selectedItem.BELI;
                this.DialogResult = DialogResult.OK;
            }
        }

        private void gridView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Get_Product_Item();
            }
         }

        private void ProductForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)27) // Escape key
            {
                this.Close(); // Close the form
            }
        }

        public List<DTOPRODUCT_WSTOCK> GetProductList(DateTime startDate, DateTime endDate)
        {
            using IDbConnection dbConnection = new OracleConnection(global.connectionString);
            dbConnection.Open();

            string sqlQuery = @"
                SELECT 
                    all_data.PRODUCTID, all_data.BARCODE, all_data.KODE_ITEM, all_data.PRODUCTNAME, all_data.SATUAN,
                    all_data.PRICE, all_data.BELI,
                    COALESCE(stock.quantity, 0) + COALESCE(purchases.quantity, 0) + COALESCE(STOCKOPNAME.jumlah_barang, 0) - (COALESCE(sales.jumlah_barang, 0)+COALESCE(RUSAK.jumlah_barang, 0)) AS STOCK
                FROM
                    (SELECT * FROM POS_PRODUCT WHERE AKTIF = 'Y') all_data
                LEFT JOIN
                    (SELECT kode_barang, quantity FROM pos_stock WHERE tanggal = :StartDate)
                    stock ON all_data.kode_item = stock.kode_barang
                LEFT JOIN
                    (SELECT d.kode_barang, SUM(d.quantity) AS quantity
                     FROM pos_pembeliandetail d
                     JOIN pos_pembelian m ON m.purchase_id = d.purchase_id
                     WHERE m.tanggal BETWEEN :StartDate AND :EndDate
                     GROUP BY d.kode_barang) purchases ON all_data.kode_item = purchases.kode_barang
                LEFT JOIN
                    (SELECT d.kode_barang, SUM(d.jumlah_barang) AS jumlah_barang
                     FROM pos_penjualan_detail d
                     JOIN pos_penjualan m ON m.no_transaksi = d.no_transaksi
                     WHERE m.tanggal BETWEEN :StartDate AND :EndDate
                     GROUP BY d.kode_barang) sales ON all_data.kode_item = sales.kode_barang
                LEFT JOIN
                    (SELECT KODE_BARANG, SUM(JUMLAHFISIK) AS jumlah_barang
                     FROM POS_BARANGRUSAK
                     WHERE TANGGAL BETWEEN :StartDate AND :EndDate
                     GROUP BY KODE_BARANG) RUSAK ON all_data.kode_item = RUSAK.kode_barang
                LEFT JOIN
                    (SELECT KODE_BARANG, SUM(SELISIH) AS jumlah_barang
                     FROM POS_STOCKOPNAME
                     WHERE TANGGAL BETWEEN :StartDate AND :EndDate
                     GROUP BY KODE_BARANG) STOCKOPNAME ON all_data.kode_item = STOCKOPNAME.kode_barang
                ORDER BY all_data.PRODUCTNAME ASC";

            // Dapper will automatically map the result to the Product class
            var productList = dbConnection.Query<DTOPRODUCT_WSTOCK>(sqlQuery, new { StartDate = startDate, EndDate = endDate }).ToList();

            return productList;
        }
    }
}