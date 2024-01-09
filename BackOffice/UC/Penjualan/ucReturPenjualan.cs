using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BackOffice.UC
{
    public partial class ucReturPenjualan : DevExpress.XtraEditors.XtraUserControl
    {
        //Using singleton pattern to create an instance to ucModule3
        private static ucReturPenjualan _instance;
        public static ucReturPenjualan Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucReturPenjualan();
                return _instance;
            }
        }
        public ucReturPenjualan()
        {
            InitializeComponent();
        }
    }
}
