using UnityEngine;

namespace HoloFastDepth
{
    public class ImageUtil
    {
        // from https://docs.microsoft.com/ja-jp/windows/mixed-reality/locatable-camera
        public static Vector3 UnProjectVector(Matrix4x4 proj, Vector3 to)
        {
            Vector3 from = new Vector3(0, 0, 0);
            var axsX = proj.GetRow(0);
            var axsY = proj.GetRow(1);
            var axsZ = proj.GetRow(2);
            from.z = to.z / axsZ.z;
            from.y = (to.y - (from.z * axsY.z)) / axsY.y;
            from.x = (to.x - (from.z * axsX.z)) / axsX.x;
            return from;
        }

        public static int[] MakeTriangles(int width, int height)
        {
            var triangles = new int[(width - 1) * (height - 1) * 6];
            for (int y = 0; y < height - 1; ++y)
            {
                for (int x = 0; x < width - 1; ++x)
                {
                    var ul = y * width + x;
                    var ur = y * width + x + 1;
                    var ll = (y + 1) * width + x;
                    var lr = (y + 1) * width + x + 1;
                    
                    var offset = (y * (width - 1) + x) * 6;

                    triangles[offset + 0] = ll;
                    triangles[offset + 1] = ul;
                    triangles[offset + 2] = ur;
                    triangles[offset + 3] = ll;
                    triangles[offset + 4] = ur;
                    triangles[offset + 5] = lr;

                }
            }

            return triangles;
        }
    }
}