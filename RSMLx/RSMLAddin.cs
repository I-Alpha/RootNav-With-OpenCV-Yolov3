using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Excel = Microsoft.Office.Interop.Excel;
using Office = Microsoft.Office.Core;
using Microsoft.Office.Tools.Excel;

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;


using RootNav.Data;
using RootNav.Data.IO;
using RootNav.Data.IO.RSML;
using RootNav.Measurement;

namespace RSMLx
{
    public partial class RSMLAddin
    {
        private TagListControl tagListControl;
        private Microsoft.Office.Tools.CustomTaskPane tagListTaskPane;
        private IRootReader reader;

        private void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
            tagListControl = new TagListControl();
            tagListTaskPane = this.CustomTaskPanes.Add(tagListControl, "RSML Tag Selection");
            tagListTaskPane.Width = 250;
        }

        private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
        {
        }

        public void LoadFiles(string directory)
        {
            reader = new RootNav.Data.IO.RSML.RSMLRootReader(new ConnectionParams() { Source = RootNav.Data.IO.ConnectionSource.RSMLDirectory, Directory = directory});
            reader.Initialise();
            
            foreach (string s in reader.ReadAllTags())
            {
                tagListControl.tagListView.Items.Add(s);
            }

            tagListTaskPane.Visible = true;
        }

        public void FilterChanged(string filter)
        {
            List<string> filtered = reader.FilterTags(filter.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries), false);
            if (filtered.Count != tagListControl.tagListView.Items.Count)
            {
                try
                {
                    tagListControl.tagListView.BeginUpdate();
                    tagListControl.tagListView.Items.Clear();
                    foreach (string s in filtered)
                    {
                    
                        tagListControl.tagListView.Items.Add(s);
                    }
                }
                finally
                {
                    tagListControl.tagListView.EndUpdate();
                }
                
            }
        }

        public void MeasureSingleFile(string file)
        {
            RSMLRootReader temporaryReader = new RootNav.Data.IO.RSML.RSMLRootReader(new ConnectionParams() { Source = RootNav.Data.IO.ConnectionSource.RSMLDirectory, Directory = "" });
            WriteData(temporaryReader.ReadFromFile(file, false));
        }

        public void Measure()
        {
            if (tagListControl.tagListView.SelectedItems.Count == 1)
            {
                string tag = tagListControl.tagListView.SelectedItems[0].Text;
                WriteData(reader.Read(tag, false));
            }
            else
            {
                // Obtain all necessary data

                List<Tuple<SceneMetadata, SceneInfo>> data = new List<Tuple<SceneMetadata, SceneInfo>>();
                foreach (var item in tagListControl.tagListView.SelectedItems)
                {
                    if (item is System.Windows.Forms.ListViewItem)
                    {
                        string tag = ((System.Windows.Forms.ListViewItem)item).Text;

                        Tuple<SceneMetadata, SceneInfo, byte[]> scn = reader.Read(tag, false);

                        data.Add(new Tuple<SceneMetadata, SceneInfo>(scn.Item1, scn.Item2));
                    }
                }

                // Handlers
                var totalLengthHandler = new TotalLengthPlantHandler();
                var averageLengthPrimaryHandler = new AverageLengthPrimaryPlantHandler();
                var averageLengthLateralHandler = new AverageLengthLateralPlantHandler();
                var primaryCountHandler = new PrimaryCountPlantHandler();
                var lateralCountHandler = new LateralCountPlantHandler();
                var maxWidthHandler = new MaximumWidthPlantHandler();
                var maxDepthHandler = new MaximumDepthPlantHandler();
                var tortuosityHandler = new TortuosityPlantHandler();
                var convexHullHandler = new ConvexHullHandler();

                // Calculate measurements
                List<object[]> measurements = new List<object[]>();

                for(int i = 0; i < data.Count; i++)
                { 
                    foreach (PlantInfo p in data[i].Item2.Plants)
                    {
                        object[] rowdata = new object[11];

                        rowdata[0] = data[i].Item1.Key;
                        rowdata[1] = p.RelativeID;
                        rowdata[2] = totalLengthHandler.MeasurePlant(p);
                        rowdata[3] = averageLengthPrimaryHandler.MeasurePlant(p);
                        rowdata[4] = averageLengthLateralHandler.MeasurePlant(p);
                        rowdata[5] = primaryCountHandler.MeasurePlant(p);
                        rowdata[6] = lateralCountHandler.MeasurePlant(p);
                        rowdata[7] = maxWidthHandler.MeasurePlant(p);
                        rowdata[8] = maxDepthHandler.MeasurePlant(p);
                        rowdata[9] = tortuosityHandler.MeasurePlant(p);
                        rowdata[10] = convexHullHandler.MeasurePlant(p);

                        measurements.Add(rowdata);
                    }
                
                }
                
                // Write to excel worksheet
                // Freeze workbook
                Application.ScreenUpdating = false;

                Excel.Worksheet currentSheet = (Excel.Worksheet)Application.ActiveSheet;
                if (currentSheet != null)
                {
                    if (currentSheet.UsedRange.Address != "$A$1")
                    {
                        // Currentsheet is not blank, create a new one.
                        currentSheet = (Excel.Worksheet)Application.ActiveWorkbook.Worksheets.Add();
                    }
                }

                int row = 2, col = 2;

                Excel.Range range = currentSheet.SelectCell(row, col);
                range.Value2 = measurements.First().First() + " - " + measurements.Last().First() + ": " + data.Count.ToString() + " files, " + measurements.Count() + " plants";
                range.Font.Size += 2;
                range.Font.Bold = true;
                row += 2;

                int startRow = row;

                range = currentSheet.SelectCells(row++, col, 10, 0);
                range.Value = new object[] { "Tag", "Plant", "Total Length", "Avg Primary Length", "Avg Lateral Length", "Primary Count", "Lateral Count", "Max Width", "Max Depth", "Tortuosity", "Convex Hull Area" };
                string currentTag = "";
                foreach (var rowdata in measurements)
                {
                    if (currentTag != "" && currentTag != rowdata[0].ToString())
                    {
                        DashedBorderBelow(currentSheet, row - 1, col, 10);
                    }

                    range = currentSheet.SelectCells(row++, col, 10, 0);
                    range.Value = rowdata;

                    currentTag = rowdata[0].ToString();
                }

                int endRow = row - 1;

                range = currentSheet.SelectCells(row, col+1, 9, 0);
                range.EntireColumn.AutoFit();

                range = currentSheet.SelectCells(startRow, col, 0, endRow - startRow);
                range.Columns.AutoFit();


                StyleTable(currentSheet, startRow, col, 10, endRow - startRow);

                Application.ScreenUpdating = true;
                
            }
        }

        public void WriteData(Tuple<SceneMetadata, SceneInfo, byte[]> data)
        {
            // Freeze workbook
            Application.ScreenUpdating = false;

            // Obtain plant data
            
            SceneMetadata metadata = data.Item1;
            SceneInfo scene = data.Item2;

            Excel.Worksheet currentSheet = (Excel.Worksheet)Application.ActiveSheet;
            if (currentSheet != null)
            {
                if (currentSheet.UsedRange.Address != "$A$1")
                {
                    // Currentsheet is not blank, create a new one.
                    currentSheet = (Excel.Worksheet)Application.ActiveWorkbook.Worksheets.Add();
                }
            }

            int row = 2, col = 2;

            // Write Key
            //Excel.Range metaRange = currentSheet.Range[currentSheet.Cells[row, col], currentSheet.Cells[row, col]];

            Excel.Range metaRange = currentSheet.SelectCell(row, col);
            metaRange.Value2 = metadata.Key;
            metaRange.Font.Size += 2;
            metaRange.Font.Bold = true;

            // Metadata title
            row += 2;

            int metaEndRow, metaEndCol;

            WriteMetadata(currentSheet, metadata, row, col, out metaEndRow, out metaEndCol);

            metaRange = currentSheet.SelectCell(metaEndRow, metaEndCol);
            metaRange.Select();

            metaRange = metaRange.Offset[1, 1];

            int plantEndRow, dataEndRow, plantEndColumn;

            List<Point> sourcePositions;
            List<ImageSource> plantImages = CreateSeparatePlantImages(scene.Plants, out sourcePositions);

            WritePlants(currentSheet, scene, plantImages, metaEndRow + 2, col, col + 4, out plantEndRow, out plantEndColumn, out dataEndRow);

            // Plant Image
            if (scene.Plants.Count > 1)
            {
                try
                {
                    Excel.Range pictureRange = currentSheet.SelectCells(row, metaEndCol + 2, 0, metaEndRow - row);
                    pictureRange.Select();
                    Point sourcePosition;
                    ImageSource source = CreatePlantImage(scene.Plants, out sourcePosition);

                    InsertImageIntoRange(source, currentSheet, pictureRange);
                }
                catch
                {
                    // Could not create image
                }
            }

            Application.ScreenUpdating = true;
        }

        private void InsertImageIntoRange(ImageSource source, Excel.Worksheet currentSheet, Excel.Range range, int maxHeight = -1)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            var encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(source as System.Windows.Media.Imaging.BitmapSource));
            encoder.Save(ms);
            ms.Flush();

            System.Windows.Forms.Clipboard.SetDataObject(System.Drawing.Image.FromStream(ms), true);
            currentSheet.Paste(range);

            // Resize image to fit range
            Excel.Shape shape = (Excel.Shape)currentSheet.Shapes.Item(currentSheet.Shapes.Count);
            shape.LockAspectRatio = Office.MsoTriState.msoCTrue;

            if (maxHeight == -1)
            {
                shape.Height = (int)range.Height;
            }
            else
            {
                shape.Height = (int)((source.Height / (double)maxHeight) * range.Height);
            }
            
            shape.Line.Visible = Office.MsoTriState.msoTrue;
            ms.Close();
        }

        private ImageSource CreatePlantImage(List<PlantInfo> plants, out Point sourcePosition)
        {
            sourcePosition = default(Point);

            Pen p = new Pen(Brushes.Black, 6.0);
            p.StartLineCap = PenLineCap.Round;
            p.EndLineCap = PenLineCap.Round;

            Dictionary<String, RootInfo> plantComponents = new Dictionary<string, RootInfo>();

            foreach (PlantInfo plant in plants)
            {
                foreach (RootInfo root in plant)
                {
                    // Start of first primary is the plant source
                    if (root.RelativeID == "1.1")
                    {
                        sourcePosition = new Point(root.Spline.Start.X, root.Spline.Start.Y);
                    }
                    plantComponents.Add(root.RelativeID, root);
                }
            }

            // Find global plant bounding box
            double left = double.MaxValue, right = double.MinValue, top = double.MaxValue, bottom = double.MinValue;

            foreach (var kvp in plantComponents)
            {
                SampledSpline spline = kvp.Value.Spline as SampledSpline;
                if (spline == null)
                {
                    continue;
                }

                Rect r = spline.BoundingBox;

                if (r.Left < left)
                {
                    left = r.Left;
                }

                if (r.Right > right)
                {
                    right = r.Right;
                }

                if (r.Top < top)
                {
                    top = r.Top;
                }

                if (r.Bottom > bottom)
                {
                    bottom = r.Bottom;
                }
            }

            int width = (int)right - (int)left + 8;
            int height = (int)bottom - (int)top + 8;

            RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

            DrawingVisual drawingVisual = new DrawingVisual();
            DrawingContext drawingContext = drawingVisual.RenderOpen();

            drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

            // Draw splines
            StreamGeometry geometry = new StreamGeometry();

            using (StreamGeometryContext sgc = geometry.Open())
            {
                foreach (var kvp in plantComponents)
                {
                    SampledSpline spline = kvp.Value.Spline as SampledSpline;
                    if (spline != null)
                    {
                        Point[] points = spline.SampledPoints;

                        // Optional line from start position on parent

                        RootInfo parent = plantComponents.ContainsKey(kvp.Key.Substring(0, kvp.Key.LastIndexOf("."))) ? plantComponents[kvp.Key.Substring(0, kvp.Key.LastIndexOf("."))] : null;

                        if (parent != null)
                        {
                            Point start = parent.Spline.GetPoint(kvp.Value.StartReference);
                            sgc.BeginFigure(new Point(start.X - left + 4, start.Y - top + 4), false, false);
                            sgc.LineTo(new Point(points[0].X - left + 4, points[0].Y - top + 4), true, true);
                        }
                        else
                        {
                            sgc.BeginFigure(new Point(points[0].X - left + 4, points[0].Y - top + 4), false, false);
                        }

                        for (int i = 1; i < points.Length; i++)
                        {
                            sgc.LineTo(new Point(points[i].X - left + 4, points[i].Y - top + 4), true, true);
                        }
                    }
                }
            }

            if (geometry.CanFreeze)
            {
                geometry.Freeze();
            }

            drawingContext.DrawGeometry(null, p, geometry);

            sourcePosition.X = sourcePosition.X - left + 4;
            sourcePosition.Y = sourcePosition.Y - top + 4;

            drawingContext.Close();

            rtb.Render(drawingVisual);

            if (rtb.CanFreeze)
            {
                rtb.Freeze();
            }

            return rtb;
        }

        private List<ImageSource> CreateSeparatePlantImages(List<PlantInfo> plants, out List<Point> sourcePositions)
        {
            sourcePositions = new List<Point>();
            var outputImages = new List<ImageSource>();

            Pen p = new Pen(Brushes.Black, 6.0);
            p.StartLineCap = PenLineCap.Round;
            p.EndLineCap = PenLineCap.Round;

            List<Dictionary<String, RootInfo>> plantComponents = new List<Dictionary<string, RootInfo>>();

            int index = 0;
            foreach (PlantInfo plant in plants)
            {
                plantComponents.Insert(index, new Dictionary<string, RootInfo>());
                foreach (RootInfo root in plant)
                {
                    // Start of first primary is the plant source
                    if (root.RelativeID == plant.RelativeID + ".1")
                    {
                        sourcePositions.Add(new Point(root.Spline.Start.X, root.Spline.Start.Y));
                    }
                    plantComponents[index].Add(root.RelativeID, root);
                }
                index++;
            }

            // For each plant, create a new image
            for (int i = 0; i < plantComponents.Count; i++)
            {
                var plantComponentDictionary = plantComponents[i];

                // Find global plant bounding box
                double left = double.MaxValue, right = double.MinValue, top = double.MaxValue, bottom = double.MinValue;

                foreach (var components in plantComponentDictionary.Values)
                {
                    SampledSpline spline = components.Spline as SampledSpline;
                    if (spline == null)
                    {
                        continue;
                    }

                    Rect r = spline.BoundingBox;

                    if (r.Left < left)
                    {
                        left = r.Left;
                    }

                    if (r.Right > right)
                    {
                        right = r.Right;
                    }

                    if (r.Top < top)
                    {
                        top = r.Top;
                    }

                    if (r.Bottom > bottom)
                    {
                        bottom = r.Bottom;
                    }
                }

                int width = (int)right - (int)left + 8;
                int height = (int)bottom - (int)top + 8;

                RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);

                DrawingVisual drawingVisual = new DrawingVisual();
                DrawingContext drawingContext = drawingVisual.RenderOpen();

                drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

                // Draw splines
                StreamGeometry geometry = new StreamGeometry();

                using (StreamGeometryContext sgc = geometry.Open())
                {
                    foreach (var kvp in plantComponentDictionary)
                    {
                        SampledSpline spline = kvp.Value.Spline as SampledSpline;
                        if (spline != null)
                        {
                            Point[] points = spline.SampledPoints;

                            // Optional line from start position on parent
                            RootInfo parent = plantComponentDictionary.ContainsKey(kvp.Key.Substring(0, kvp.Key.LastIndexOf("."))) ? plantComponentDictionary[kvp.Key.Substring(0, kvp.Key.LastIndexOf("."))] : null;

                            if (parent != null)
                            {
                                Point start = parent.Spline.GetPoint(kvp.Value.StartReference);
                                sgc.BeginFigure(new Point(start.X - left + 4, start.Y - top + 4), false, false);
                                sgc.LineTo(new Point(points[0].X - left + 4, points[0].Y - top + 4), true, true);
                            }
                            else
                            {
                                sgc.BeginFigure(new Point(points[0].X - left + 4, points[0].Y - top + 4), false, false);
                            }

                            for (int i2 = 1; i2 < points.Length; i2++)
                            {
                                sgc.LineTo(new Point(points[i2].X - left + 4, points[i2].Y - top + 4), true, true);
                            }
                        }
                    }
                }

                if (geometry.CanFreeze)
                {
                    geometry.Freeze();
                }

                drawingContext.DrawGeometry(null, p, geometry);

                sourcePositions[i] = new Point(sourcePositions[i].X - left + 4, sourcePositions[i].Y - top + 4);

                drawingContext.Close();

                rtb.Render(drawingVisual);

                if (rtb.CanFreeze)
                {
                    rtb.Freeze();
                }

                outputImages.Add(rtb);
            }

            return outputImages;
        }

        private void WriteMetadata(Excel.Worksheet sheet, SceneMetadata metadata, int startRow, int startColumn, out int finishRow, out int finishColumn)
        {
            // Variables
            int row = startRow, col = startColumn;
            Excel.Range metaRange = sheet.SelectCell(row, col); 
            
            // Metadata title
            metaRange = sheet.SelectCell(row, col);
            metaRange.Value2 = "RSML Metadata";

            row += 1;

            // Version
            metaRange = sheet.SelectCells(row++, col, 2, 0);
            metaRange.Value = new object[] { "Version", "", metadata.Version == "" ? "1" : metadata.Version };

            // Unit
            metaRange = sheet.SelectCells(row++, col, 2, 0);
            metaRange.Value = new object[] { "Unit", "", metadata.Unit };

            // Resolution
            metaRange = sheet.SelectCells(row++, col, 2, 0);
            metaRange.Value = new object[] { "Resolution", "", metadata.Resolution.ToString() };

            // Last modified
            metaRange = sheet.SelectCells(row++, col, 2, 0);
            metaRange.Value = new object[] { "Last Modified", "", metadata.LastModified.ToString("d") };
            // Set to date

            // Software
            metaRange = sheet.SelectCells(row++, col, 2, 0);
            metaRange.Value = new object[] { "Software", "", metadata.Software };

            // User
            metaRange = sheet.SelectCells(row++, col, 2, 0);
            metaRange.Value = new object[] { "User", "", metadata.User };

            finishRow = row - 1;
            finishColumn = col + 2;

            // Border and resize
            sheet.SelectCell(finishRow, finishColumn).EntireColumn.AutoFit();
            StyleTable(sheet, startRow, startColumn, finishColumn - startColumn, finishRow - startRow);
        }

        private void WritePlants(Excel.Worksheet sheet, SceneInfo scene, List<ImageSource> images, int startRow, int startColumn, int startDataColumn, out int finishRow, out int finishColumn, out int finishDataColumn)
        {
            // Constants
            const int INDEX_WIDTH = 2;
            const int DATA_WIDTH = 3;
            const int PLANT_SUMMARY_WIDTH = 1;
            const int PLANT_SUMMARY_HEIGHT = 7;

            // Calculate minimum row count for plants
            int minRowCount = int.MaxValue;
            foreach (PlantInfo p in scene.Plants)
            {
                int count = p.Count();
                if (minRowCount > count) minRowCount = count;
            }

            int imageCellHeight = Math.Min(10, minRowCount);
            
            // Calculate maximum image height
            int maxImageHeight = int.MinValue;
            foreach (ImageSource i in images)
            {
                if (i.Height > maxImageHeight) maxImageHeight = (int)i.Height;
            }

            // Handlers
            RootNav.Measurement.MeasurementHandler[] plantHandlers = new RootNav.Measurement.MeasurementHandler[]
            {
                new TotalLengthPlantHandler(), new TortuosityPlantHandler(), new ConvexHullHandler(), new PrimaryCountPlantHandler(), new LateralCountPlantHandler(), new AverageLengthPrimaryPlantHandler(), new AverageLengthLateralPlantHandler()
            };

            RootNav.Measurement.MeasurementHandler[] rootHandlers = new RootNav.Measurement.MeasurementHandler[]
            {
                new TotalLengthRootHandler(), new EmergenceAngleRootHandler(), new TipAngleRootHandler(), new TortuosityRootHandler()
            };

            // Headers
            Excel.Range range = sheet.SelectCells(startRow, startDataColumn, DATA_WIDTH, 0);
            range.Value2 = new object[] { "Length", "Emergence Angle", "Tip Angle", "Tortuosity" };

            range = sheet.SelectCells(startRow, startColumn, INDEX_WIDTH, 0);
            range.Value2 = new object[] { "Plant", "Primary", "Lateral" };

            int plantRow = startRow, plantCol = startColumn;

            int index = 0;
            foreach (PlantInfo p in scene.Plants)
            {
                // Plant header
                range = sheet.SelectCell(plantRow, plantCol);
                range.Value2 = "Plant " + p.RelativeID;
                range.Font.Bold = true;
                BorderAboveAndBelow(sheet, plantRow, startColumn, (startDataColumn + DATA_WIDTH) - startColumn);
                
                // Plant Summary
                range = sheet.SelectCells(plantRow, startDataColumn + DATA_WIDTH + 2, PLANT_SUMMARY_WIDTH, PLANT_SUMMARY_HEIGHT);
                range.Value = new object[,]
                {
                    { "Plant Summary", "" },
                    { "Total Length", plantHandlers[0].MeasurePlant(p) },
                    { "Primary Root Count", plantHandlers[3].MeasurePlant(p) },
                    { "Lateral Root Count", plantHandlers[4].MeasurePlant(p) },
                    { "Average Primary Length", plantHandlers[5].MeasurePlant(p) },
                    { "Average Lateral Length", plantHandlers[6].MeasurePlant(p) },
                    { "Tortuosity", plantHandlers[1].MeasurePlant(p) },
                    { "Convex Hull Area", plantHandlers[2].MeasurePlant(p) }
                };

                StyleTable(sheet, plantRow, startDataColumn + DATA_WIDTH + 2, PLANT_SUMMARY_WIDTH, PLANT_SUMMARY_HEIGHT);

                // Plant Image
                ImageSource img = images[index++];
                range = sheet.SelectCells(plantRow, startDataColumn + DATA_WIDTH + 2 + PLANT_SUMMARY_WIDTH + 2, 0, imageCellHeight);
                InsertImageIntoRange(img, sheet, range, maxImageHeight);

                // Roots
                foreach (RootInfo r in p)
                {
                    plantRow++;
                    
                    // Obtain ID
                    string trim = r.RelativeID.Substring(r.RelativeID.LastIndexOf('.') + 1);
                    bool isPrimary = r.RelativeID.IndexOf('.') == r.RelativeID.LastIndexOf('.');

                    // Write ID
                    int offset = isPrimary ? 1 : 2;
                    range = sheet.SelectCell(plantRow, plantCol + offset);
                    range.Value2 = trim;

                    // Measurements
                    range = sheet.SelectCells(plantRow, startDataColumn, DATA_WIDTH, 0);
                    range.Value = new object[] { rootHandlers[0].MeasureRoot(r), rootHandlers[1].MeasureRoot(r), rootHandlers[2].MeasureRoot(r), rootHandlers[3].MeasureRoot(r) };
                }

                // One Row padding
                plantRow += 1;
            }
            
            // Back to end of last plant
            plantRow -= 1;

            // Output variables
            finishRow = plantRow;
            finishColumn = plantCol + 2;
            finishDataColumn = startDataColumn + DATA_WIDTH;
            
            // Resize columns
            range = sheet.SelectCells(startRow, startDataColumn, DATA_WIDTH + 1 + PLANT_SUMMARY_WIDTH, 0);
            range.EntireColumn.AutoFit();

            // Table Border
            StyleTable(sheet, startRow, plantCol, finishDataColumn - plantCol, finishRow - startRow);

        }

        private void BorderAround(Excel.Range range)
        {
            Excel.Borders borders = range.Borders;
            borders[Excel.XlBordersIndex.xlEdgeLeft].LineStyle = Excel.XlLineStyle.xlContinuous;
            borders[Excel.XlBordersIndex.xlEdgeTop].LineStyle = Excel.XlLineStyle.xlContinuous;
            borders[Excel.XlBordersIndex.xlEdgeBottom].LineStyle = Excel.XlLineStyle.xlContinuous;
            borders[Excel.XlBordersIndex.xlEdgeRight].LineStyle = Excel.XlLineStyle.xlContinuous;
            borders[Excel.XlBordersIndex.xlInsideVertical].LineStyle = Excel.XlLineStyle.xlLineStyleNone;
            borders[Excel.XlBordersIndex.xlInsideHorizontal].LineStyle = Excel.XlLineStyle.xlLineStyleNone;
            borders[Excel.XlBordersIndex.xlDiagonalUp].LineStyle = Excel.XlLineStyle.xlLineStyleNone;
            borders[Excel.XlBordersIndex.xlDiagonalDown].LineStyle = Excel.XlLineStyle.xlLineStyleNone;
            borders = null;
        }

        private void StyleTable(Excel.Worksheet sheet, int row, int col, int width, int height)
        {
            Excel.Range topRow = sheet.SelectCells(row, col, width, 0);
            Excel.Borders topBorder = topRow.Borders;
            topBorder[Excel.XlBordersIndex.xlEdgeTop].LineStyle = Excel.XlLineStyle.xlContinuous;
            topBorder[Excel.XlBordersIndex.xlEdgeBottom].LineStyle = Excel.XlLineStyle.xlContinuous;
            topRow.Font.Bold = true;

            Excel.Range bottomRow = sheet.SelectCells(row + height, col, width, 0);
            Excel.Borders bottomBorder = bottomRow.Borders;
            bottomBorder[Excel.XlBordersIndex.xlEdgeBottom].LineStyle = Excel.XlLineStyle.xlContinuous;
        }

        private void BorderAboveAndBelow(Excel.Worksheet sheet, int row, int col, int width)
        {
            Excel.Range range = sheet.SelectCells(row, col, width, 0);
            Excel.Borders borders = range.Borders;
            borders[Excel.XlBordersIndex.xlEdgeTop].LineStyle = Excel.XlLineStyle.xlContinuous;
            borders[Excel.XlBordersIndex.xlEdgeBottom].LineStyle = Excel.XlLineStyle.xlContinuous;
        }

        private void DashedBorderBelow(Excel.Worksheet sheet, int row, int col, int width)
        {
            Excel.Range range = sheet.SelectCells(row, col, width, 0);
            Excel.Borders borders = range.Borders;
            borders[Excel.XlBordersIndex.xlEdgeBottom].LineStyle = Excel.XlLineStyle.xlDash;
        }

        #region VSTO generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisAddIn_Startup);
            this.Shutdown += new System.EventHandler(ThisAddIn_Shutdown);
        }
        
        #endregion
    }
}
