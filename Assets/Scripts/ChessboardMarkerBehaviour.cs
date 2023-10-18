using System.Collections.Generic;
using UnityEngine;

using VisComp;
using OpenCvSharp;

public class ChessboardMarkerBehaviour : MarkerObjectBehaviour
{
    #region CONSTANTS

    const int pixel_per_block  = 30;

    #endregion // CONSTANTS



    #region PUBLIC_MEMBERS

    public float block_mm = 30.0f; // 30.0 [mm]
    public int   block_width  = 8;
    public int   block_height = 5;

    #endregion // PUBLIC_MEMBERS



    #region MONO_BEHAVIOUR

    Size pattern_size;
    Point3f[] corners;

    void Start()
    {
        // [CAUTION] "Size" and "Point3f[]" is initialized when running
        pattern_size = new Size(block_width - 1, block_height - 1);

        // corners (unit: [mm])
        corners = new Point3f[pattern_size.Width * pattern_size.Height];
        for (int j = 0; j < block_height - 1; j++)
        for (int i = 0; i < block_width  - 1; i++)
        {
            float obj_x = block_mm * i - block_mm * 0.5f * (block_width -2);
            float obj_y = block_mm * j - block_mm * 0.5f * (block_height-2);
            
            // convert [mm] to [m]
            obj_x *= 0.001f;
            obj_y *= 0.001f;

            Point3f pt3d = new Point3f(obj_x, obj_y, 0.0f);

            //Debug.Log(i + "," + j + " : " + pt3d); // [CHECK: OK]
            corners[i + j * (block_width - 1)] = pt3d;
        }
    }

    #endregion // MONO_BEHAVIOUR



    #region SUBROUTINES

    protected override Mat CreateMarkerMap()
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

        return marker_bgr;
    }

    public override bool GetDetectedCorners(Mat image_bgra, out Point3f[] _corners, out Point2f[] _points, bool show, string winname)
    {
        bool detected = Cv2.FindChessboardCorners(image_bgra, pattern_size, out _points, ChessboardFlags.AdaptiveThresh);
        _corners = corners;

        if (show)
        {
            Mat image_clone = image_bgra.Clone();
            Cv2.DrawChessboardCorners(image_clone, pattern_size, _points, detected);
            Cv2.ImShow(winname, image_clone);
        }

        return detected;
    }

    #endregion // SUBROUTINES
}
