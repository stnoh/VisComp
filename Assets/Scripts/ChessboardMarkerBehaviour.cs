using System.Collections.Generic;
using UnityEngine;

using VisComp;
using OpenCvSharp;

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
        if (null != material) RemoveMarkerTexture2D();

        // create markermap
        Mat img_bgr = CreateMarkerMap();
        //Cv2.ImShow("Chessboard", img_bgr); // [CHECK: OK]

        Texture2D tex2D = img_bgr.ToTexture2D();

        // mesh for markermap: [mm] to [m]
        float half_scale_x = 0.5f * W_mm * 0.001f;
        float half_scale_z = 0.5f * H_mm * 0.001f;

        // add Components to this GameObject
        var mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = Helper.GetBoardMesh(half_scale_x, half_scale_z);

        material = new Material(Shader.Find("Unlit/Texture"));
        material.mainTexture = tex2D;

        var mr = gameObject.AddComponent<MeshRenderer>();
        mr.material = material;
    }

    public void ExportPDF(string filepath)
    {
        Mat img_bgr = material.mainTexture.ToMat();
        Helper.ExportPDF(filepath, img_bgr, new Vector2(W_mm, H_mm));
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

    Mat CreateMarkerMap()
    {
        // object (unit: [mm])
        W_mm = block_mm * block_width;
        H_mm = block_mm * block_height;

        // image (unit: pixel)
        int H = pixel_per_block * block_height;
        int W = pixel_per_block * block_width;
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
