using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OpenCvSharp;

namespace VisComp
{
    public static class ImgProc
    {
        public static Texture2D ToTexture2D(this Texture self)
        {
            var sw = self.width;
            var sh = self.height;
            var format = TextureFormat.RGBA32;
            var result = new Texture2D(sw, sh, format, false);
            var currentRT = RenderTexture.active;
            var rt = new RenderTexture(sw, sh, 32);
            Graphics.Blit(self, rt);
            RenderTexture.active = rt;
            var source = new UnityEngine.Rect(0, 0, rt.width, rt.height);
            result.ReadPixels(source, 0, 0);
            result.Apply();
            RenderTexture.active = currentRT;

            Object.DestroyImmediate(rt); // [CAUTION] prevent memory leak
            return result;
        }

        public static Mat ToMat(this Texture self)
        {
            Texture2D tex2D = ToTexture2D(self); // 1st Quadrant, RGBA

            Mat mat = new Mat(tex2D.height, tex2D.width, MatType.CV_8UC4, tex2D.GetRawTextureData());
            mat = mat.Flip(FlipMode.X); // 1st -> 4th Quadrant
            mat = mat.CvtColor(ColorConversionCodes.RGBA2BGRA); // RGBA -> BGRA for OpenCV

            Object.DestroyImmediate(tex2D); // [CAUTION] prevent memory leak
            return mat;
        }

        public static Texture2D ToTexture2D(this Mat mat)
        {
            var W = mat.Cols;
            var H = mat.Rows;
            Texture2D tex2D = new Texture2D(W, H, TextureFormat.ARGB32, mipChain: false); // 4th Quadrant, BGR

            if (mat.Type() != MatType.CV_8UC3)
            {
                Debug.LogError("ERROR: it only supports 24bit BGR image");
                return tex2D;
            }

            Cv2.Flip(mat, mat, FlipMode.X); // 4th -> 1st Quadrant
            Cv2.CvtColor(mat, mat, ColorConversionCodes.BGR2RGBA); // BGR -> RGBA for Unity

            // convert to Texture2D data
            Color32[] image = new Color32[W * H];
            Color32 c = new Color32();

            // apply each pixel data [FIX ME LATER] faster conversion
            var byteArray = mat.GetGenericIndexer<Vec3b>();
            for (int j = 0; j < H; j++)
            for (int i = 0; i < W; i++)
            {
                c.b = byteArray[j, i].Item0;
                c.g = byteArray[j, i].Item1;
                c.r = byteArray[j, i].Item2;
                c.a = 255;

                image[i + j * tex2D.width] = c;
            }

            tex2D.SetPixels32(image);
            tex2D.Apply();

            return tex2D;
        }
    }
}
