using UnityEngine;

using VisComp;
using OpenCvSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

public class ChessboardMarkerBehaviour : MonoBehaviour
{
    #region CONSTANTS

    const int pixel_per_block  = 30;

    #endregion // CONSTANTS



    #region PUBLIC_MEMBERS

    public float block_mm = 30.0f; // 30.0 [mm]
    public int   block_width  = 8;
    public int   block_height = 5;

    [HideInInspector]
    public Material     material = null;
    public MatOfPoint3f chessboard_corners; // 3D points of corners

    #endregion // PUBLIC_MEMBERS



    #region PUBLIC_METHODS

    public void CreateMarkerTexture2D()
    {
        RemoveMarkerTexture2D();

        // create markermap
        Mat img_bgr = CreateMarkerMap();
        //Cv2.ImShow("marker", img_bgr); // check image

        Texture2D tex2D = img_bgr.ToTexture2D();

        // mesh for markermap
        float half_scale_x = 0.5f * block_mm * block_width  * 0.01f;
        float half_scale_z = 0.5f * block_mm * block_height * 0.01f;

        Mesh m = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        Vector3[] verts = m.vertices;
        verts[0] = new Vector3(-half_scale_x, 0.0f, -half_scale_z);
        verts[1] = new Vector3(+half_scale_x, 0.0f, -half_scale_z);
        verts[2] = new Vector3(-half_scale_x, 0.0f, +half_scale_z);
        verts[3] = new Vector3(+half_scale_x, 0.0f, +half_scale_z);
        m.vertices = verts;
        m.RecalculateNormals();

        // add Components to this GameObject
        var mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = m;

        material = new Material(Shader.Find("Unlit/Texture"));
        material.mainTexture = tex2D;

        var mr = gameObject.AddComponent<MeshRenderer>();
        mr.material = material;
    }

    public void ExportPDF(string filepath)
    {
        var document = new PdfDocument();
        document.Info.Title = "Chessboard";

        // A4 with landscape: 842 x 595 = 297[mm] x 210[mm]
        PdfPage page = document.AddPage();
        page.Size = PdfSharp.PageSize.A4;
        page.Orientation = PdfSharp.PageOrientation.Landscape;

        // compute the size
        float scale = (float)page.Height / 210.0f;
        double W = scale * block_width * block_mm;
        double H = scale * block_height * block_mm;

        // draw image on PDF
        XGraphics gfx = XGraphics.FromPdfPage(page);
        Mat img_bgr = material.mainTexture.ToMat();
        XImage xImage = XImage.FromStream(img_bgr.ToMemoryStream());
        xImage.Interpolate = false;
        gfx.DrawImage(xImage, 0.5 * page.Width - 0.5 * W, 0.5 * page.Height - 0.5 * H, W, H); // centering
        //gfx.DrawImage(xImage, 0, 0, W, H); // [CHECK: OK]

        document.Save(filepath);
    }

    public void RemoveMarkerTexture2D()
    {
        DestroyImmediate(material);

        var mf = gameObject.GetComponent<MeshFilter>();
        if (null != mf) DestroyImmediate(mf);

        var mr = gameObject.GetComponent<MeshRenderer>();
        if (null != mr) DestroyImmediate(mr);
    }

    #endregion // PUBLIC_METHODS



    #region SUBROUTINES

    float W_mm, H_mm;
    int   W, H;

    Mat CreateMarkerMap()
    {
        // object (unit: [mm])
        W_mm = block_mm * block_width;
        H_mm = block_mm * block_height;

        // image (unit: pixel)
        H = pixel_per_block * block_height;
        W = pixel_per_block * block_width;
        Mat marker_bgr = new Mat(H, W, MatType.CV_8UC3, new Scalar(255, 255, 255));

        for (int block_j = 0; block_j < block_height; block_j++)
        for (int block_i = 0; block_i < block_width ; block_i++)
        {
            byte c = ((block_i % 2 == 0 && block_j % 2 == 0) ||
                      (block_i % 2 == 1 && block_j % 2 == 1)) ? (byte)0 : (byte)255;
            Mat buf = new Mat(pixel_per_block, pixel_per_block, MatType.CV_8UC3, new Scalar(c, c, c));

            var rect = new OpenCvSharp.Rect(
                block_i * pixel_per_block, block_j * pixel_per_block,
                pixel_per_block, pixel_per_block);

            // append black/white block on the image
            var pos = new Mat(marker_bgr, rect);
            buf.CopyTo(pos);
        }

        // corners (unit: [mm])
        chessboard_corners = new MatOfPoint3f();
        for (int j = 0; j < block_height - 1; j++)
        for (int i = 0; i < block_width  - 1; i++)
        {
            float obj_x = block_mm * i - block_mm * 0.5f * (block_width -2);
            float obj_y = block_mm * j - block_mm * 0.5f * (block_height-2);
            
            // convert [mm] to [m]
            obj_x *= 0.001f;
            obj_y *= 0.001f;

            Point3f pt3d = new Point3f(obj_x, obj_y, 0.0f);

            //Debug.Log(pt3d);
            chessboard_corners.Add(pt3d);
        }

        return marker_bgr;
    }

    #endregion // SUBROUTINES
}
