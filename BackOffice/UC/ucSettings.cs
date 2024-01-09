using BackOffice.DataLayer;
using DevExpress.XtraCharts.Native;
using DevExpress.XtraGrid.Views.Base;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BackOffice.UC
{
    public partial class ucSettings : UserControl
    {
        DataSet DSSetting;
        OracleDataAdapter sqlAdapter;
        //Using singleton pattern to create an instance to ucModule3
        private static ucSettings _instance;
        public static ucSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucSettings();
                return _instance;
            }
        }
        public ucSettings()
        {
            InitializeComponent();
        }

        private void ucSettings_Load(object sender, EventArgs e)
        {
            gridControl1.DataSource = Setting();
            gridControl1.DataMember = "CONFIG";
            gridView1.Columns["CONFIG"].Visible = false;
                gridView1.Columns["JUMLAH"].DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            gridView1.Columns["JUMLAH"].DisplayFormat.FormatString = "N2";
        }


        private DataSet Setting()
        {

            String selectQuery = "select CONFIG,KETERANGAN,JUMLAH FROM FIN_SETTINGS ";
            OracleConnection connection = new(global.connectionString);
            OracleCommand _command = new(selectQuery, connection)
            {
                CommandType = CommandType.Text
            };
            OracleCommandBuilder sqlcmdbuilder = new OracleCommandBuilder();
            sqlAdapter = new OracleDataAdapter();
            sqlcmdbuilder.DataAdapter = sqlAdapter;
            sqlAdapter.SelectCommand = _command;
            DSSetting = new DataSet();
            //DSperiode.Clear();
            //Get the data in disconnected mode
            sqlAdapter.Fill(DSSetting, "CONFIG");
            // return dataset result
            return DSSetting;
        }

        private void gridView1_RowUpdated(object sender, DevExpress.XtraGrid.Views.Base.RowObjectEventArgs e)
        {
            ColumnView view = gridControl1.FocusedView as ColumnView;
            view.CloseEditor();
            if (view.UpdateCurrentRow())
            {
                sqlAdapter.Update(DSSetting, "CONFIG");
            }
        }

        private void gridView1_ValidatingEditor(object sender, DevExpress.XtraEditors.Controls.BaseContainerValidateEditorEventArgs e)
        {
            if (gridView1.FocusedColumn.FieldName == "JUMLAH") // Replace "JUMLAH" with the actual column name
            {
                if (!decimal.TryParse(e.Value.ToString(), out decimal result))
                {
                    e.Valid = false;
                    e.ErrorText = "Input hanya angka.";
                }
                if (!decimal.TryParse(e.Value.ToString(), out decimal result1) || result1 < 0)
                {
                    e.Valid = false;
                    e.ErrorText = "Hanya nilai positif.";
                }
            }
        }
    }
}
