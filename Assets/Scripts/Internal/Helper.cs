using UnityEngine;

using OpenCvSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace VisComp
{
    public static class Helper
    {
        public static void ExportPDF(string filepath, Mat img_bgr, Vector2 size_mm)
        {
            var document = new PdfDocument();
            document.Info.Title = System.IO.Path.GetFileName(filepath);

            // A4 with landscape: 842 x 595 = 297[mm] x 210[mm]
            PdfPage page = document.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            page.Orientation = PdfSharp.PageOrientation.Landscape;

            // compute the size
            float scale = (float)page.Height / 210.0f;
            float W = scale * size_mm.x;
            float H = scale * size_mm.y;

            // draw image on PDF
            XGraphics gfx = XGraphics.FromPdfPage(page);
            XImage xImage = XImage.FromStream(img_bgr.ToMemoryStream());
            xImage.Interpolate = false;
            //gfx.DrawImage(xImage, 0, 0, W, H); // [CHECK: OK] no offset to check size
            gfx.DrawImage(xImage, 0.5 * page.Width - 0.5 * W, 0.5 * page.Height - 0.5 * H, W, H); // centering

            document.Save(filepath);
        }

        public static Mesh GetBoardMesh(float half_scale_x, float half_scale_z)
        {
            // clone primitive mesh (PrimitiveType.Quad)
            Mesh m = Object.Instantiate(Resources.GetBuiltinResource<Mesh>("Quad.fbx"));

            // fix vertices position
            Vector3[] verts = m.vertices;
            verts[0] = new Vector3(-half_scale_x, 0.0f, -half_scale_z);
            verts[1] = new Vector3(+half_scale_x, 0.0f, -half_scale_z);
            verts[2] = new Vector3(-half_scale_x, 0.0f, +half_scale_z);
            verts[3] = new Vector3(+half_scale_x, 0.0f, +half_scale_z);
            m.vertices = verts;

            m.RecalculateNormals();
            return m;
        }

        public static T GetComponentOrPause<T>(this GameObject self, string message)
        {
            if (null == self)
            {
                Debug.LogError(message);
                UnityEditor.EditorApplication.isPaused = true;
                return default;
            }

            T component = self.GetComponent<T>();

            if (null == component)
            {
                Debug.LogError(message);
                UnityEditor.EditorApplication.isPaused = true;
                return default;
            }

            return component;
        }

        public static bool IsFinite(Vec3f v)
        {
            return
                !float.IsInfinity(v.Item0) && !float.IsNaN(v.Item0) &&
                !float.IsInfinity(v.Item1) && !float.IsNaN(v.Item1) &&
                !float.IsInfinity(v.Item2) && !float.IsNaN(v.Item2);
        }
    }
}
