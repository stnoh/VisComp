using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using VisComp;
using OpenCvSharp;
using OpenCvSharp.Aruco;

public class ArUcoMarkerBehaviour : MarkerObjectBehaviour
{
    #region CONSTANTS

    public Dictionary ar_dict = CvAruco.GetPredefinedDictionary(OpenCvSharp.Aruco.PredefinedDictionaryName.Dict6X6_250);

    const int block_per_marker = 8;  // Dict6x6 = 8x8 blocks = marker
    const int pixel_per_block  = 16; // [CAUTION] include white spaces between markers

    #endregion // CONSTANTS



    #region PUBLIC_MEMBERS

    public float marker_mm = 30.0f; // 30.0 [mm]
    public int   marker_width  = 8;
    public int   marker_height = 5;

    public Dictionary<int, Point3f[]> markermap_corners; // < marker id, four corners (LT, RT, RB, LB) >

    [HideInInspector]
    public List<int> marker_ids;

    #endregion // PUBLIC_MEMBERS



    #region MONO_BEHAVIOUR

    Point3f[] corners;
    DetectorParameters detector_params;

    void Start()
    {
        detector_params = DetectorParameters.Create();

        int pixel_per_marker = pixel_per_block * block_per_marker;
        W = pixel_per_block * (marker_width  * (block_per_marker + 1) - 1);
        H = pixel_per_block * (marker_height * (block_per_marker + 1) - 1);

        // object (unit: [mm])
        float mm_per_block = marker_mm / block_per_marker;
        W_mm = mm_per_block * (marker_width  * (block_per_marker + 1) - 1);
        H_mm = mm_per_block * (marker_height * (block_per_marker + 1) - 1);

        markermap_corners = new Dictionary<int, Point3f[]>();

        int marker_id = 0;
        for (int j = 0; j < marker_height; j++)
        for (int i = 0; i < marker_width ; i++)
        {
            Mat buf = new Mat();
                
            var rect = new OpenCvSharp.Rect(
                i * pixel_per_block * (block_per_marker + 1),
                j * pixel_per_block * (block_per_marker + 1),
                pixel_per_marker, pixel_per_marker);
                
            // four corners (LT, RT, RB, LB)
            markermap_corners.Add(marker_ids[marker_id], new Point3f[] {
                PixelToObject(rect.Left , rect.Top),
                PixelToObject(rect.Left + rect.Width, rect.Top),
                PixelToObject(rect.Left + rect.Width, rect.Top + rect.Height),
                PixelToObject(rect.Left , rect.Top + rect.Height),
            } );

            marker_id++;
        }
    }

    #endregion // MONO_BEHAVIOUR



    #region SUBROUTINES

    int W, H;

    private Point3f PixelToObject(int img_x, int img_y)
    {
        float obj_x = W_mm / (float)(W - 1) * img_x - 0.5f * W_mm;
        float obj_y = H_mm / (float)(H - 1) * img_y - 0.5f * H_mm;

        // [mm] to [m]
        obj_x *= 0.001f;
        obj_y *= 0.001f;

        Point3f pt3d = new Point3f(obj_x, obj_y, 0.0f);

        //Debug.Log(img_x + "," + img_y + " : " + obj_x + ", " + obj_y); // [CHECK: OK]
        return pt3d;
    }

    protected override Mat CreateMarkerMap()
    {
        // object (unit: [mm])
        float mm_per_block = marker_mm / block_per_marker;
        W_mm = mm_per_block * (marker_width  * (block_per_marker + 1) - 1);
        H_mm = mm_per_block * (marker_height * (block_per_marker + 1) - 1);

        // image (unit: pixel)
        H = pixel_per_block * (marker_height * (block_per_marker + 1) - 1);
        W = pixel_per_block * (marker_width  * (block_per_marker + 1) - 1);
        int pixel_per_marker = pixel_per_block * block_per_marker;

        // marker image
        Mat marker_bgr = new Mat(H, W, MatType.CV_8UC3, new Scalar(255, 255, 255));

        marker_ids = Enumerable.Range(0, 250).ToList(); // maximum: 250
        // [TODO: shuffle] data for randomization
        marker_ids = marker_ids.GetRange(0, marker_width * marker_height); // cut the list

        int marker_id = 0;
        for (int j = 0; j < marker_height; j++)
        for (int i = 0; i < marker_width ; i++)
        {
            Mat buf = new Mat();
            CvAruco.DrawMarker(ar_dict, marker_ids[marker_id], pixel_per_marker, buf);
            Cv2.CvtColor(buf, buf, ColorConversionCodes.GRAY2BGR);
                
            var rect = new OpenCvSharp.Rect(
                i * pixel_per_block * (block_per_marker + 1),
                j * pixel_per_block * (block_per_marker + 1),
                pixel_per_marker, pixel_per_marker);
                
            // append marker on the image
            var pos = new Mat(marker_bgr, rect);
            buf.CopyTo(pos);

            marker_id++;
        }

        return marker_bgr;
    }

    public override bool GetDetectedCorners(Mat image_bgra, out Point3f[] _objectPoints, out Point2f[] _imagePoints, bool show, string winname)
    {
        // ArUco only supports 1- (grayscale) or 3-channels (BGR)
        Mat image_bgr = image_bgra.Clone();
        Cv2.CvtColor(image_bgra, image_bgr, ColorConversionCodes.BGRA2BGR);

        Point2f[][] corners;
        Point2f[][] rejected_corners;
        int[] ids;

        CvAruco.DetectMarkers(image_bgr, ar_dict, out corners, out ids, detector_params, out rejected_corners);

        int N = ids.Length;
        _objectPoints = new Point3f[4 * N];
        _imagePoints  = new Point2f[4 * N];

        if (N == 0) return false;

        // copy detected marker corners and their 3D position
        for (int n = 0; n < N; n++)
        {
            int id = ids[n];
            Point3f[] _objectPoints_this = markermap_corners[id];
            Point2f[] _imagePoints_this  = corners[n];

            for (int i = 0; i < 4; i++)
            {
                _objectPoints[4 * n + i] = _objectPoints_this[i];
                _imagePoints [4 * n + i] = _imagePoints_this [i];
            }
        }

        if (show)
        {
            CvAruco.DrawDetectedMarkers(image_bgr, corners, ids);
            Cv2.ImShow(winname, image_bgr);
        }

        return true;
    }

    #endregion // SUBROUTINES
}
