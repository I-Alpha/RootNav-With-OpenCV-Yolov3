using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Data;
using System.Windows.Media.Imaging;

namespace RootNav.Core.Measurement
{
    static class RootMeasurement
    {
        public static Dictionary<RootBase, Tuple<double,double>[]> GetCurvatureProfiles(List<RootBase> roots, double resolution)
        {
            Dictionary<RootBase, Tuple<double, double>[]> data = new Dictionary<RootBase, Tuple<double, double>[]>();
            TraverseRootsCurvature(roots, ref data, resolution);
            return data;
        }

        private static void TraverseRootsCurvature(List<RootBase> roots, ref Dictionary<RootBase, Tuple<double, double>[]> data, double resolution)
        {
            foreach (RootBase r in roots)
            {
                data.Add(r, r.GetCurvatureProfile(resolution));
                if (r.Children.Count > 0)
                {
                    List<RootBase> children = new List<RootBase>();
                    foreach (PlantComponent pc in r.Children)
                    {
                        children.Add(pc as RootBase);
                    }

                    TraverseRootsCurvature(children, ref data, resolution);
                }
            }
        }

        public static void GetMapProfiles(List<RootBase> roots, out Dictionary<RootBase, Tuple<double,double>[]> leftData, out Dictionary<RootBase, Tuple<double,double>[]> rightData, double resolution, double travel, double[] probabilityMap, int width, int height)
        {
            leftData = new Dictionary<RootBase, Tuple<double, double>[]>();
            rightData = new Dictionary<RootBase, Tuple<double, double>[]>();
            
            TraverseRootsMap(roots, leftData, rightData, resolution, travel, probabilityMap, width, height);

            return;
        }

        private static void TraverseRootsMap(List<RootBase> roots, Dictionary<RootBase, Tuple<double, double>[]> leftData, Dictionary<RootBase, Tuple<double, double>[]> rightData, double resolution, double travel, double[] probabilityMap, int width, int height)
        {
            foreach (RootBase r in roots)
            {
                Tuple<double, double>[] left, right;
                r.GetMapProfile(resolution, travel, probabilityMap, width, height, out left, out right);

                leftData.Add(r, left);
                rightData.Add(r, right);

                if (r.Children.Count > 0)
                {
                    List<RootBase> children = new List<RootBase>();
                    foreach (PlantComponent pc in r.Children)
                    {
                        children.Add(pc as RootBase);
                    }

                    TraverseRootsMap(children, leftData, rightData, resolution, travel, probabilityMap, width, height);
                }
            }
        }

        public static List<Dictionary<string, string>> GetDataAsStrings(List<RootBase> roots)
        {
            List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();
            TraverseRoots(roots, ref data);
            return data;
        }

        private static void TraverseRoots(List<RootBase> roots, ref List<Dictionary<string, string>> data)
        {
            foreach (RootBase r in roots)
            {
                data.Add(GetDataAsStrings(r));
                if (r.Children.Count > 0)
                {
                    List<RootBase> children = new List<RootBase>();
                    foreach (PlantComponent pc in r.Children)
                    {
                        children.Add(pc as RootBase);
                    }

                    TraverseRoots(children, ref data);
                }
            }
        }

        public static Dictionary<string, string> GetDataAsStrings(RootBase root)
        {
            Dictionary<string, string> outputData = new Dictionary<string, string>();
            Dictionary<string, object> data = GetData(root);

            bool isPlant = data["Order"].ToString() == "-1";
            bool isPrimary = data["Order"].ToString() == "0";

            outputData.Add("ID", data["ID"].ToString());
            outputData.Add("Parent", data["Parent Relative ID"] == null ? "" : data["Parent Relative ID"].ToString());
            outputData.Add("Order", data["Order"].ToString());
            outputData.Add("Length", isPlant ? "" : data["Length"].ToString());
            outputData.Add("Label", root.Label);
            outputData.Add("Emergence Angle", isPlant ? "" : data["Emergence Angle"].ToString());
            outputData.Add("Tip Angle", isPlant ? "" : data["Tip Angle"].ToString());
            outputData.Add("Hull Area", data["Hull Area"] == null ? "" : data["Hull Area"].ToString());
            outputData.Add("Start Distance", isPlant || isPrimary ? "" : data["Start Distance"].ToString());
            outputData.Add("Lateral Count", data["Lateral Count"].ToString());

            return outputData;
        }

        public static Dictionary<string, object> GetData(RootBase root)
        {
            Dictionary<string, object> outputData = new Dictionary<string, object>();

            outputData.Add("ID", root.RelativeID);
            outputData.Add("Start", root.Order >= 0 ? root.Start.ToRoundString() : null);
            outputData.Add("End", root.Order >= 0 ? root.End.ToRoundString() : null);
            outputData.Add("Order", root.Order);
            outputData.Add("Length", (double?)Math.Round(root.Length, 2));
            outputData.Add("Label", root.Label);
            outputData.Add("Emergence Angle", root.Order == -1 ? null : (double?)Math.Round(root.EmergenceAngle, 1));
            outputData.Add("Tip Angle", root.Order == -1 ? null : (double?)Math.Round(root.TipAngle, 1));
            outputData.Add("Start Reference", root.Order > 0 ? ObjectToBinary(root.StartReference) : null);
            outputData.Add("Start Distance", root.StartDistance <= 0 ? null : (double?)Math.Round(root.StartDistance, 2));
            outputData.Add("Spline", root.Order >= 0 ? ObjectToBinary(root.Spline) : null);
            outputData.Add("Hull Area", root.Order > 0 ? null : (double?)Math.Round(root.ConvexHullArea, 2));

            outputData.Add("Primary Parent Relative ID", root.PrimaryParent == null ? null : root.PrimaryParent.RelativeID);
            outputData.Add("Parent Relative ID", root.Parent == null ? null : root.Parent.RelativeID);
            
            outputData.Add("Lateral Count", root.Children.Count);

            return outputData;
        }

        public static bool WriteToDatabase(RootNav.Data.IO.Databases.DatabaseManager manager, string tag, bool alllaterals, List<RootBase> roots, BitmapSource sourceImage = null)
        {
            List<Dictionary<String, object>> records = new List<Dictionary<string, object>>();
            foreach (RootBase r in roots)
            {
                records.Add(GetData(r));
            }

            if (manager.IsOpen)
            {
                // Write bitmap if available
                byte[] imageData = null;
                if (sourceImage != null)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        JpegBitmapEncoder encoder = new JpegBitmapEncoder() { QualityLevel = 98 };
                        MemoryStream MS = new MemoryStream();
                        encoder.Frames.Add(BitmapFrame.Create(sourceImage));
                        encoder.Save(MS);
                        imageData = MS.ToArray();
                    }
                }

                bool success = manager.Write(tag, alllaterals, records, imageData);
                return success;
            }

            return false;
        }

        private static byte[] ObjectToBinary(object o)
        {
            MemoryStream oStream = new MemoryStream();
            BinaryFormatter oBF = new BinaryFormatter();
            oBF.Serialize(oStream, o);
            byte[] arrBuffer = new byte[oStream.Length];
            oStream.Seek(0, SeekOrigin.Begin);
            oStream.Read(arrBuffer, 0, arrBuffer.Length);
            oStream.Close();
            return arrBuffer;
        }

        private static object BinaryToObject(byte[] binary)
        {
            if (binary != null)
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return formatter.Deserialize(new MemoryStream(binary));
            }
            else
            {
                return null;
            }
        }

        private static String ToRoundString(this System.Windows.Point p)
        {
            return Math.Round(p.X).ToString() + "," + Math.Round(p.Y).ToString();
        }

    }
}
