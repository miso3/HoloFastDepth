using System.Collections.Generic;

namespace HoloFastDepth.Depth
{
    public interface IDepthEstimator
    {
        int InputHeight { get; }
        int InputWidth { get; }
        
        IEnumerable<float> EstimateDepth(float[] inTensor);
    }
}