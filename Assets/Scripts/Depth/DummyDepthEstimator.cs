using System.Collections.Generic;

namespace HoloFastDepth.Depth
{
    /// <summary>
    /// ダミーのデプス推定クラス
    /// </summary>
    public class DummyDepthEstimator : IDepthEstimator
    {
        
        public int InputHeight { get; }
        public int InputWidth { get; }
        public IEnumerable<float> EstimateDepth(float[] inTensor)
        {
            throw new System.NotImplementedException();
        }
    }
}