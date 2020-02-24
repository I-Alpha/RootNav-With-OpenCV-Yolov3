using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Excel = Microsoft.Office.Interop.Excel;
using Office = Microsoft.Office.Core;
using Microsoft.Office.Tools.Excel;

namespace RSMLx
{
    static class SheetNavigator
    {
        public static Excel.Range SelectCell(this Excel.Worksheet sheet, int row, int col)
        {
            return sheet.Range[sheet.Cells[row, col], sheet.Cells[row, col]];
        }

        public static Excel.Range SelectCells(this Excel.Worksheet sheet, int row, int col, int width, int height)
        {
            return sheet.Range[sheet.Cells[row, col], sheet.Cells[row + height, col + width]];
        }
    }
}
