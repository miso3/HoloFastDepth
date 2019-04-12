using System;
using System.Linq;
using HoloFastDepth.Depth;
using HoloToolkit.Unity.InputModule;
using UnityEngine;
using UnityEngine.XR.WSA.WebCam;
using Rect = UnityEngine.Rect;

namespace HoloFastDepth
{
    public class MeshCreator: MonoBehaviour, IInputClickHandler
    {
        public GameObject CaptView;

        public GameObject DepthView;
        
        public int CropWidth = 1024;
        
        public int CropHeight = 1024;

        public string FastDepthOnnxModel;

        // TODO ちゃんと合わせる
        public float Coef = 1f;
  
        // TODO: PhotoCaptureは他のクラスに切り出した方が良いかも
        private PhotoCapture photoCapture;

        private CameraParameters cameraParameters;

        private IDepthEstimator depthEstimator;

        private Texture2D targetTexture;
        private Texture2D inputTexture;
        private Texture2D depthTexture;

        private Vector2[,] screenPos;
        
        private float[] inputTensor;
        private int[] triangles;
        private Vector3[] vertices;

        private float scaleHorizontalByCrop;
        private float scaleVerticalByCrop;
        private float scaleHorizontalByResize;
        private float scaleVerticalByResize;
        
        // Use this for initialization
        void Start () {
            // DepthEstimator のセットアップ
            
            // TODO: factory等にまとめた方がよさそう
#if UNITY_UWP
            depthEstimator = new FastDepthEstimator(FastDepthOnnxModel);
#else
            depthEstimator = new DummyDepthEstimator();
#endif
 
            // カメラパラメータの取得
            var cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending(res => res.width * res.height).First();
            cameraParameters = new CameraParameters
            {
                hologramOpacity = 0.0f,
                cameraResolutionWidth = cameraResolution.width,
                cameraResolutionHeight = cameraResolution.height,
                pixelFormat = CapturePixelFormat.BGRA32,
            };
            Debug.Log(string.Format("Camera[width:{0}, height:{1}]",
            cameraParameters.cameraResolutionWidth, cameraParameters.cameraResolutionHeight));

            // バッファの確保
            inputTensor = new float[depthEstimator.InputHeight * depthEstimator.InputWidth * 3];
            
            inputTexture = new Texture2D(
                depthEstimator.InputWidth, depthEstimator.InputHeight, TextureFormat.RGB24, false);
            depthTexture = new Texture2D(
                depthEstimator.InputWidth, depthEstimator.InputHeight, TextureFormat.RGB24, false);
            
            //  
            triangles = ImageUtil.MakeTriangles(depthEstimator.InputWidth, depthEstimator.InputHeight);
            vertices = new Vector3[depthEstimator.InputWidth * depthEstimator.InputHeight];
            Debug.Log(string.Format("num mertices: {0} ", vertices.Length));
            
            Debug.Log("Alloc texture buffer.");
            targetTexture = new Texture2D(cameraParameters.cameraResolutionWidth, cameraParameters.cameraResolutionHeight);
            CropWidth = Math.Min(CropWidth, cameraParameters.cameraResolutionWidth);
            CropHeight = Math.Min(CropHeight, cameraParameters.cameraResolutionHeight);
                
            var srcSize = new Vector2(targetTexture.width, targetTexture.height);
            var srcRoi = new Rect(targetTexture.width / 2 - CropWidth / 2, targetTexture.height / 2 - CropHeight / 2, CropWidth, CropHeight);
            var destSize = new Vector2(depthEstimator.InputWidth, depthEstimator.InputHeight);
                
            // テンソル => スクリーン座標 をあらかじめ計算しておく
            screenPos = new Vector2[depthEstimator.InputHeight, depthEstimator.InputWidth];
            for (var y = 0; y < depthEstimator.InputHeight; ++y)
            {
                for (var x = 0; x < depthEstimator.InputWidth; ++x)
                {
                    var invY = depthEstimator.InputHeight - y - 1;
                    CalcSrcPos(srcSize, srcRoi, destSize, x, invY,
                        out screenPos[y, x].x, out screenPos[y, x].y);
                }
            }

            // 切り出しとリサイズによる内部パラメータ変更のための変数を計算しておく
            scaleHorizontalByCrop = CropWidth / Convert.ToSingle(cameraParameters.cameraResolutionWidth);
            scaleVerticalByCrop = CropHeight / Convert.ToSingle(cameraParameters.cameraResolutionHeight);
            scaleHorizontalByResize = depthEstimator.InputWidth / Convert.ToSingle(CropWidth);
            scaleVerticalByResize = depthEstimator.InputHeight / Convert.ToSingle(CropHeight);

            // 何も無い場所をエアタップできるように GlobalListener へ登録
            InputManager.Instance.AddGlobalListener(gameObject);
        }

        public void OnInputClicked(InputClickedEventData e)
        {
            if (photoCapture != null)
            {
                Debug.Log("");
                return;
            }

            Debug.Log("OnInputClicked");
            PhotoCapture.CreateAsync(false, captureObject =>
            {
                photoCapture = captureObject;         
                photoCapture.StartPhotoModeAsync(cameraParameters, result =>
                {
                    photoCapture.TakePhotoAsync(OnCapturedPhotoToMemory);
                });
            });
        }

        private void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
        {
            Debug.Log("OnCapturedPhotoToMemory");
            if (!result.success)
            {
                return;
            }
            Debug.Log("Captured.");
            photoCaptureFrame.UploadImageDataToTexture(targetTexture);

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            
            // キャプチャした画像を中心から指定サイズで切り抜き、入力用のテンソルを作成
            for (var y = 0 ; y < depthEstimator.InputHeight; ++y)
            {
                for (var x = 0; x < depthEstimator.InputWidth; ++x)
                {
                    var invY = depthEstimator.InputHeight - y - 1;
                    
                    //Debug.Log(string.Format("sx, sy: {0}, {1}", sx, sy));
                    var color = targetTexture.GetPixelBilinear(screenPos[y, x].x, screenPos[y, x].y);
                    inputTexture.SetPixel(x, invY, color);

                    inputTensor[
                              y * depthEstimator.InputWidth + x] = color.r;
                    inputTensor[
                            depthEstimator.InputWidth * depthEstimator.InputHeight
                            + y * depthEstimator.InputWidth + x] = color.g;
                    inputTensor[
                            depthEstimator.InputWidth * depthEstimator.InputHeight * 2 
                            + y * depthEstimator.InputWidth + x] = color.b;
                }
            }
            inputTexture.Apply();
            SetTexture(CaptView, inputTexture);
            
            var tConvTensor = sw.ElapsedMilliseconds;
             
            // 推論
            var depth = depthEstimator.EstimateDepth(inputTensor);

            var tPred = sw.ElapsedMilliseconds;

            // 推論したデプスからワールド座標系へ変換
            Matrix4x4 camToWorldMatrix, projMatrix;
            photoCaptureFrame.TryGetCameraToWorldMatrix(out camToWorldMatrix);
            photoCaptureFrame.TryGetProjectionMatrix(out projMatrix);

            Matrix4x4 modifiedProjMatrix = ModifyProjectionMatrix(projMatrix);

            var min = depth.Min();
            var max = depth.Max();
            foreach (var pix in depth.Select((v, i) => new { v, i }))
            {
                var x = pix.i % depthEstimator.InputWidth;
                var y = pix.i / depthEstimator.InputWidth;
                var invY = depthEstimator.InputHeight - y - 1;
                var val =(pix.v - min) / (max - min);
                depthTexture.SetPixel(x, invY, new Color(val, val, val, 1.0f));

                var worldPos = ImageUtil.screenPosToWorldPos(
                    camToWorldMatrix, modifiedProjMatrix,
                    screenPos[y, x].x, screenPos[y, x].y,
                    Convert.ToSingle(Math.Pow(pix.v, Coef)));
                vertices[y * depthEstimator.InputWidth + x] = worldPos;
                
            }
            depthTexture.Apply();
            SetTexture(DepthView, depthTexture);

            // Mesh生成
            var mesh = new Mesh();
            mesh.SetVertices(vertices.ToList());
            mesh.SetTriangles(triangles, 0);
            GetComponent<MeshFilter>().mesh = mesh;

            // 終了処理
            photoCapture.StopPhotoModeAsync(OnStoppedPhotoMode);

            var tEnd = sw.ElapsedMilliseconds;
            Debug.Log(string.Format("Time\n  pic to tensor : {0}\n  pred : {1}\n  tensor to depth : {2}",
                arg0: tConvTensor, arg1: tPred - tConvTensor, arg2: tEnd - tPred));
        }

        /// <summary>
        /// 切り出しとリサイズによるprojection matrixの補正
        /// </summary>
        /// <param name="projMatrix"></param>
        /// <returns></returns>
        private Matrix4x4 ModifyProjectionMatrix(Matrix4x4 projMatrix) {
            Debug.Log("Original projection matrix:");
            Debug.Log(projMatrix);
            // 1.52283 0.00000 -0.00518 0.00000
            // 0.00000 2.70851  0.02438 0.00000
            // 0.00000 0.00000 -1.00000 0.00000
            // 0.00000 0.00000 -1.00000 0.00000

            var fx = projMatrix.m00;
            var fy = projMatrix.m11;
            var cx = projMatrix.m02;
            var cy = projMatrix.m12;
            Debug.Log($"fx = {fx}, fy = {fy}, cx = {cx}, cy = {cy}");

            // fxよりfyの方が大きいのでY成分が長辺を表している気がする
            fx *= scaleVerticalByCrop;
            fy *= scaleHorizontalByCrop;
            cx *= scaleVerticalByResize;
            cy *= scaleHorizontalByResize;

            var column0 = new Vector4(fx, 0, 0, 0);
            var column1 = new Vector4(0, fy, 0, 0);
            var column2 = new Vector4(cx, cy, -1, -1);
            
            var modifiedProjMatrix = new Matrix4x4(column0, column1, column2, Vector4.zero);
            Debug.Log("Modified projection matrix:");
            Debug.Log(modifiedProjMatrix);

            return modifiedProjMatrix;
        }

        private void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
        {
            // Shutdown the photo capture resource
            photoCapture.Dispose();
            photoCapture = null;
        }
        
        void CalcSrcPos(Vector2 srcSize, Rect srcRoi, Vector2 destSize, int destX, int destY, out float srcU, out float srcV) 
        {
            srcU = destX / (destSize.x - 1) * (srcRoi.width - 1) / (srcSize.x - 1) + srcRoi.x / (srcSize.x - 1);
            srcV = destY / (destSize.y - 1) * (srcRoi.height - 1) / (srcSize.y - 1) + srcRoi.y / (srcSize.y - 1);
        }

        void SetTexture(GameObject target, Texture2D texture)
        {
            var renderer = target.GetComponent<Renderer>();
            renderer.material.SetTexture("_MainTex", texture);
        }

    }
}