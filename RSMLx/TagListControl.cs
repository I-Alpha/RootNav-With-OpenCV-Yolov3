using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RSMLx
{
    public partial class TagListControl : UserControl
    {
        public TagListControl()
        {
            InitializeComponent();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            Globals.RSMLAddin.FilterChanged(textBox1.Text);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Globals.RSMLAddin.Measure();
        }
    }
}
