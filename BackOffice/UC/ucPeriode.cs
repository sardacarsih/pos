using BackOffice.DataLayer;
using DevExpress.XtraGrid.Views.Base;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace BackOffice.UC
{
    public partial class ucPeriode : UserControl
    {
        DataSet DSperiode;
        OracleDataAdapter sqlAdapter;
        //Using singleton pattern to create an instance to ucModule3
        private static ucPeriode _instance;
        public static ucPeriode Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucPeriode();
                return _instance;
            }
        }
        public ucPeriode()
        {
            InitializeComponent();
        }

        private void ucPeriode_Load(object sender, EventArgs e)
        {
            setahu.Value = DateTime.Now.Year;
        }

        private void LoadList_Periode()
        {
            int p_tahun = Convert.ToInt32(setahu.Value.ToString());
            DSperiode=Periode(p_tahun);
            gridControl1.DataSource = DSperiode;
            gridControl1.DataMember = "Periode";
        }

        private DataSet Periode(int ptahun)
        {

            String selectQuery = "select PERIODE,BULAN,REMISE1,R1DARI,R1SAMPAI,REMISE2,R2DARI,R2SAMPAI,BULANAN,BDARI,BSAMPAI from POS_PERIODE where TAHUN=:tahun order by periode ";
            OracleConnection connection = new(global.connectionString);
            OracleCommand _command = new(selectQuery, connection)
            {
                CommandType = CommandType.Text
            };
            _command.Parameters.Add(":tahun", OracleDbType.Int32).Value = ptahun;
            OracleCommandBuilder sqlcmdbuilder = new OracleCommandBuilder();
            sqlAdapter = new OracleDataAdapter();
            sqlcmdbuilder.DataAdapter = sqlAdapter;
            sqlAdapter.SelectCommand = _command;
            DSperiode = new DataSet();
            //DSperiode.Clear();
            //Get the data in disconnected mode
            sqlAdapter.Fill(DSperiode, "Periode");
            // return dataset result
            return DSperiode;
        }

        private void gridView1_RowUpdated(object sender, DevExpress.XtraGrid.Views.Base.RowObjectEventArgs e)
        {
            ColumnView view = gridControl1.FocusedView as ColumnView;
            view.CloseEditor();
            if (view.UpdateCurrentRow())
            {
                sqlAdapter.Update(DSperiode, "Periode");
            }
        }

        private void setahu_EditValueChanged(object sender, EventArgs e)
        {
            LoadList_Periode();
        }
    }
}
