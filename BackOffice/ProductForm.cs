using BackOffice.DataLayer;
using BackOffice.Model;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace BackOffice
{
    public partial class ProductForm : DevExpress.XtraEditors.XtraForm
    {
        List<DTOPRODUCTS> ListItemsBarang;
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

            ListItemsBarang = DaftarBarang();
            gridControl1.DataSource = ListItemsBarang;
            gridView1.Columns["PRODUCTID"].Visible = false;
            //gridView1.Columns["BARCODE"].Visible = false;
            gridView1.Columns["BARCODE"].Width = 150;
            gridView1.Columns["KATEGORI"].Visible = false;
            gridView1.Columns["KATEGORI_ID"].Visible = false;
            //gridView1.Columns["BELI"].Visible = false;
            gridView1.Columns["KODE_ITEM"].Width = 100;
            gridView1.Columns["PRODUCTNAME"].Width = 300;
            gridView1.Columns["SATUAN"].Width = 80;
            gridView1.Columns["PRICE"].Caption= "JUAL";
            gridView1.Columns["PRICE"].Width = 90;
            gridView1.Columns["PRICE"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["PRICE"].DisplayFormat.FormatString = "n0";
            gridView1.Columns["BELI"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["BELI"].DisplayFormat.FormatString = "n0";
            gridView1.Columns["MARGIN"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["MARGIN"].DisplayFormat.FormatString = "n0";
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
                DTOPRODUCTS selectedItem = gridView1.GetRow(selectedHandle) as DTOPRODUCTS;

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

        private void gridControl1_DoubleClick(object sender, EventArgs e)
        {
           // Get_Product_Item();
        }
        private static List<DTOPRODUCTS> DaftarBarang()
        {
            string query = "SELECT * FROM pos_product where AKTIF='Y' ORDER BY productname";

            using OracleConnection connection = new(global.connectionString);
            connection.Open();

            List<DTOPRODUCTS> productList = connection.Query<DTOPRODUCTS>(query).ToList();

            return productList;
        }
    }
}