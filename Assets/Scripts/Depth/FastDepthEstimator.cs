using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_UWP
using System.Threading.Tasks;
using Windows.AI.MachineLearning;
using Windows.Storage;
#endif

namespace HoloFastDepth.Depth
{
#if UNITY_UWP
    /// <summary>
    /// faset-depth の学習済みモデルを利用したデプス推定を行う
    /// </summary>
    public class FastDepthEstimator : IDepthEstimator
    {

        private OnnxModel onnxModel;

        public int InputWidth
        {
            get { return onnxModel.InWidth; }
        }

        public int InputHeight
        {
            get { return onnxModel.InHeight; }
        }

        public FastDepthEstimator(string modelName)
        {
            Task.Run(async () =>
            {
                await LoadModelAsync(modelName);
            }).Wait();
        }

        public IEnumerable<float> EstimateDepth(float[] inTensor)
        {

            var task = Task.Run(async () =>
            {
                return await onnxModel.EvaluateAsync(
                    TensorFloat.CreateFromArray(new long[] { 1, 3, InputHeight, InputWidth }, inTensor));
            });
            
            return task.Result.GetAsVectorView();
        }
        
        private async Task LoadModelAsync(string modelName)
        {

            var onnx = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri($"ms-appx:///Assets/MLModel/{modelName}.onnx"));
            onnxModel = await OnnxModel.CreateFromStreamAsync(onnx);
        }
    }

    public sealed class OnnxModel
    {
        private LearningModel model;
        private LearningModelSession session;
        private LearningModelBinding binding;

        private string inName;
        private string outName;
        public int InWidth { get; private set; }
        public int InHeight { get; private set; }
        public int OutWidth { get; private set; }
        public int OutHeight { get; private set; }

        public static async Task<OnnxModel> CreateFromStreamAsync(StorageFile modelFile)
        {
            OnnxModel learningModel = new OnnxModel();
            learningModel.model = await LearningModel.LoadFromStorageFileAsync(modelFile);
            learningModel.session = new LearningModelSession(learningModel.model);
            learningModel.binding = new LearningModelBinding(learningModel.session);

            var inputFeature = learningModel.model.InputFeatures[0];
            learningModel.inName = inputFeature.Name;

            var inputTensorFeature = inputFeature as TensorFeatureDescriptor;
            learningModel.InWidth = (int) inputTensorFeature.Shape[3];
            learningModel.InHeight = (int) inputTensorFeature.Shape[2];

            var outputFeature = learningModel.model.OutputFeatures[0];
            learningModel.outName = outputFeature.Name;

            var outputTensorFeature = outputFeature as TensorFeatureDescriptor;
            learningModel.OutWidth = (int) outputTensorFeature.Shape[3];
            learningModel.OutHeight = (int) outputTensorFeature.Shape[2];

            Debug.Log(string.Format("Input Feature Name: {0}", learningModel.inName));
            Debug.Log(string.Format("Output Feature Name: {0}", learningModel.outName));
            Debug.Log(string.Format("Input Width, Height: {0}, {1}", learningModel.InWidth, learningModel.InHeight));
            Debug.Log(string.Format("Output Width, Height: {0}, {1}", learningModel.OutWidth, learningModel.OutHeight));
            return learningModel;
        }

        public async Task<TensorFloat> EvaluateAsync(TensorFloat input)
        {
            binding.Bind(inName, input);
            binding.Bind(outName, TensorFloat.Create(new long[] {1, 1, InHeight, OutHeight}));
            var result = await session.EvaluateAsync(binding, inName);
            return result.Outputs[outName] as TensorFloat;

        }
    }
#endif
}
