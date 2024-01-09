using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Penjualan.UC
{
    public partial class ucLaporan : UserControl
    {
        //Using singleton pattern to create an instance to ucModule3
        private static ucLaporan _instance;
        public static ucLaporan Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ucLaporan();
                return _instance;
            }
        }
        public ucLaporan()
        {
            InitializeComponent();
        }
    }
}
