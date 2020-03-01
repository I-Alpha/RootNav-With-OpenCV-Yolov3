using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using OpenCvSharp;
using OpenCvSharp.Dnn;



namespace OpenCvSharpYolo3
{
    public class OpenCVY3 : IOpenCVModule
    {
        #region const/readonly
        //YOLOv3
        //https://github.com/pjreddie/darknet/blob/master/cfg/yolov3.cfg
        private const string Cfg = "yolov3.cfg";

        //https://pjreddie.com/media/files/yolov3.weights
        private const string Weight = "yolov3.weights";

        //https://github.com/pjreddie/darknet/blob/master/data/coco.names
        private const string Names = "obj.names";

        //file location

        private const string Location = @"..\..\..\..\OpenCvSharpYolo3\Content\";
        //random assign color to each label
        private static readonly Scalar[] Colors = Enumerable.Repeat(false, 80).Select(x => Scalar.RandomColor()).ToArray();

        //get labels from coco.names
        private static readonly string[] Labels = File.ReadAllLines(Path.Combine(Location, Names)).ToArray();
        #endregion

        #region results field
        private List<int> classIds = new List<int>();
        private List<float> confidences = new List<float>();
        private List<float> probabilities = new List<float>();
        private List<Rect2d> boxes = new List<Rect2d>();
        private List<System.Windows.Point> centerpoints = new List<System.Windows.Point>();

        public string run_t { get; private set; }

        public List<int> ClassIds { get => classIds; private set => classIds = value; }
        public List<float> Confidences { get => confidences; private set => confidences = value; }
        public List<float> Probabilities { get => probabilities; private set => probabilities = value; }
        public List<Rect2d> Boxes { get => boxes; private set => boxes = value; }
        public List<System.Windows.Point> Centerpoints { get => centerpoints; private set => centerpoints = value; }

        #endregion

        public OpenCVY3() { }

        public OpenCVY3(string img) { worker(img); }

        public void worker(string image)
        {

            #region parameter  

            var cfg = Path.Combine(Location, Cfg);
            var model = Path.Combine(Location, Weight);
            const float threshold = 0.3f;       //for confidence 
            const float nmsThreshold = 0.3f;    //threshold for nms
            #endregion

            //get image
            var org = new Mat(image);

            //setting blob, size can be:320/416/608
            //opencv blob setting can check here https://github.com/opencv/opencv/tree/master/samples/dnn#object-detection
            var blob = CvDnn.BlobFromImage(org, 1.0 / 255, new OpenCvSharp.Size(416, 416), new Scalar(), true, false);

            //load model and config, if you got error: "separator_index < line.size()", check your cfg file, must be something wrong.
            var net = CvDnn.ReadNetFromDarknet(cfg, model);
            #region set preferable
            net.SetPreferableBackend(3);
            /*
            0:DNN_BACKEND_DEFAULT 
            1:DNN_BACKEND_HALIDE 
            2:DNN_BACKEND_INFERENCE_ENGINE
            3:DNN_BACKEND_OPENCV 
             */
            net.SetPreferableTarget(0);
            /*
            0:DNN_TARGET_CPU 
            1:DNN_TARGET_OPENCL
            2:DNN_TARGET_OPENCL_FP16
            3:DNN_TARGET_MYRIAD 
            4:DNN_TARGET_FPGA 
             */
            #endregion

            //input data
            net.SetInput(blob);

            //get output layer name
            var outNames = net.GetUnconnectedOutLayersNames();
            //create mats for output layer
            var outs = outNames.Select(_ => new Mat()).ToArray();

            #region forward model
            Stopwatch sw = new Stopwatch();
            sw.Start();

            net.Forward(outs, outNames);

            sw.Stop();
            string run_time;
            run_time = $"Runtime:{sw.ElapsedMilliseconds} ms";
            #endregion

            //get result from all output
            GetResult(outs, org, threshold, nmsThreshold);
            run_t = run_time;

        }


        /// <summary>
        /// Get result form all output
        /// </summary>
        /// <param name="output"></param>
        /// <param name="image"></param>
        /// <param name="threshold"></param>
        /// <param name="nmsThreshold">threshold for nms</param>
        /// <param name="nms">Enable Non-maximum suppression or not</param>
        private void GetResult(IEnumerable<Mat> output, Mat image, float threshold, float nmsThreshold, bool nms = true)
        {
            classIds.Clear();
            confidences.Clear();
            probabilities.Clear();
            boxes.Clear();
            centerpoints.Clear();
            //for nms


            var w = image.Width;
            var h = image.Height;
            /*
             YOLO3 COCO trainval output
             0 1 : center                    2 3 : w/h
             4 : confidence                  5 ~ 84 : class probability 
            */
            const int prefix = 5;   //skip 0~4

            foreach (var prob in output)
            {
                for (var i = 0; i < prob.Rows; i++)
                {
                    var confidence = prob.At<float>(i, 4);
                    if (confidence > threshold)
                    {
                        //get classes probability
                        Cv2.MinMaxLoc(prob.Row[i].ColRange(prefix, prob.Cols), out _, out OpenCvSharp.Point max);
                        var classes = max.X;
                        var probability = prob.At<float>(i, classes + prefix);

                        if (probability > threshold) //more accuracy, you can cancel it
                        {
                            //get center and width/height
                            var centerX = prob.At<float>(i, 0) * w;
                            var centerY = prob.At<float>(i, 1) * h;
                            var width = prob.At<float>(i, 2) * w;
                            var height = prob.At<float>(i, 3) * h;


                            //put data to list for NMSBoxes
                            classIds.Add(classes);
                            confidences.Add(confidence);
                            probabilities.Add(probability);
                            boxes.Add(new Rect2d(centerX, centerY, width, height));
                            centerpoints.Add(new System.Windows.Point(centerX, centerY));
                        }

                        //arrange data for output and send

                    }
                }
            }

            if (!nms) return;

        }
    }
}


