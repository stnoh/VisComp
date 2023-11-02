using System.Collections.Generic;
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

        // [TEMPORARY] simple initial setting for obj file loading
        public static void SetTriMeshData(ref Mesh mesh, List<Vector3> vertices, List<int> indices, List<Vector3> normals = null, List<Color32> colors = null)
        {
            // set minimum requirements (vertices, faces)
            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);

            // set vertex normal if exists
            if (null != normals)
            {
                if (vertices.Count == normals.Count)
                {
                    mesh.SetNormals(normals);
                }
                else
                {
                    mesh.RecalculateNormals();
                    Debug.Log("invalid vertex normal: calculate vertex normal based on vertices & indices.");
                }
            }

            // set vertex color if exists
            if (null != colors)
            {
                if (vertices.Count == colors.Count)
                {
                    mesh.SetColors(colors);
                }
                else
                {
                    Debug.Log("invalid vertex color: the number of vertex color does not match with vertex.");
                }
            }

            mesh.RecalculateBounds();
        }

        // [TEMPORARY] simple Obj import module (not perfect)
        public static Mesh ReadObj(string filepath)
        {
            // prepare containers
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> indices = new List<int>();

            // read obj file (ASCII format)
            string[] lines = System.IO.File.ReadAllLines(filepath);

            foreach (string line in lines)
            {
                if (0 == line.Length) continue;
                if ('#' == line[0]) continue; // skip the comment

                string[] elem = line.Split(' ');

                if ("f" == elem[0])
                {
                    int[] elem_idx = { 1, 2, 3 };
                    for (int j = 0; j < 3; j++)
                    {
                        int i = elem_idx[j];
                        string[] f_elem = elem[i].Split('/');
                        int vidx = int.Parse(f_elem[0]);
                        //int tidx = int.Parse(f_elem[1]); // [TEMP] 
                        int nidx = int.Parse(f_elem[2]);

                        if (vidx != nidx)
                        {
                            Debug.Log("Unsupported: vertex and normal index is different.");
                        }

                        indices.Add(vidx - 1);
                    }
                }
                else
                {
                    float x = float.Parse(elem[1]);
                    float y = float.Parse(elem[2]);
                    float z = float.Parse(elem[3]);

                    Vector3 vec3 = new Vector3(x, y, z);

                    switch (elem[0])
                    {
                        case "v": vertices.Add(vec3); break;
                        case "vn": normals.Add(vec3); break;
                        default: Debug.LogWarning("Unsupported: " + line); break;
                    }
                }
            }

            // create mesh & set its name as filename
            Mesh mesh = new Mesh();
            SetTriMeshData(ref mesh, vertices, indices, normals);
            mesh.name = System.IO.Path.GetFileNameWithoutExtension(filepath);

            return mesh;
        }
    }
}
