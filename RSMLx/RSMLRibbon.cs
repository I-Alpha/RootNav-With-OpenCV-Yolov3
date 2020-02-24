using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Office.Tools.Ribbon;

namespace RSMLx
{
    public partial class RSMLRibbon
    {
        private void RSMLRibbon_Load(object sender, RibbonUIEventArgs e)
        {

        }

        private void openFile_Click(object sender, RibbonControlEventArgs e)
        {
            if (openRSMLFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Globals.RSMLAddin.MeasureSingleFile(openRSMLFileDialog.FileName);
            }
        }

        private void openDirectory_Click(object sender, RibbonControlEventArgs e)
        {
            if (rsmlFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Globals.RSMLAddin.LoadFiles(rsmlFolderBrowserDialog.SelectedPath);
            }
        }
    }
}
