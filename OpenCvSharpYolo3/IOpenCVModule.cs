using System.Collections.Generic;
using System.Windows;

namespace OpenCvSharpYolo3
{
    public interface IOpenCVModule
    {
        List<Point> Centerpoints { get; }
        List<int> ClassIds { get; }
        List<float> Probabilities { get; }
        void worker(string image);
    }
}