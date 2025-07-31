using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VisComp;
using OpenCvSharp;

using Windows.Kinect;

public class ProcamCalibrationBehaviour : MonoBehaviour
{
    #region PUBLIC_MEMBERS

    public GameObject ColorSourceManager;
    public GameObject DepthSourceManager;
    public GameObject MultiSourceManager;

    public GameObject MarkerObject;

    public string calib_filepath;

    #endregion // PUBLIC_MEMBERS



    #region PRIVATE_MEMBERS

    private ColorSourceManager _ColorManager;
    private DepthSourceManager _DepthManager;

    private CoordinateMapper _Mapper;

    private ChessboardMarkerBehaviour _marker_script;
    private FullscreenGameViewBehaviour _screen_script;

    private Size marker_size;

    #endregion // PRIVATE_MEMBERS



    #region USER_INTERFACE

    bool drag = false;
    Vector2 MousePosition_prev;

    #endregion // USER_INTERFACE



    #region MONO_BEHAVIOUR

    void Start()
    {
        var _Sensor = KinectSensor.GetDefault();
        if (_Sensor != null)
        {
            _Mapper = _Sensor.CoordinateMapper;
        }

        _marker_script = MarkerObject.GetComponent<ChessboardMarkerBehaviour>();
        _screen_script = gameObject.GetComponent<FullscreenGameViewBehaviour>();
    }

    void Update()
    {
        ////////////////////////////////////////////////////////////
        // mouse: middle button & wheel to scale & move marker
        ////////////////////////////////////////////////////////////
        if (true)
        {
            Vector2 MousePosition_this = Input.mousePosition;

            // change on the marker position
            if (drag)
            {
                Vector3 diff = MousePosition_this - MousePosition_prev;
                diff.z = diff.y;
                diff.y = 0.0f;

                MarkerObject.transform.Translate(0.004f * diff);

                MousePosition_prev = MousePosition_this;
            }

            if (!drag && Input.GetMouseButtonDown(2))
            {
                //Debug.Log("middle pressed");
                MousePosition_prev = MousePosition_this;
                drag = true;
            }
            if (Input.GetMouseButtonUp(2))
            {
                drag = false;
            }

            // change on the marker scale
            if (Input.mouseScrollDelta.y != 0.0f)
            {
                if (Input.mouseScrollDelta.y > 0.0f)
                {
                    Vector3 scale = MarkerObject.transform.localScale;
                    scale *= 1.05f;
                    MarkerObject.transform.localScale = scale;
                }
                else
                {
                    Vector3 scale = MarkerObject.transform.localScale;
                    scale /= 1.05f;
                    MarkerObject.transform.localScale = scale;
                }
            }
        }

        ////////////////////////////////////////////////////////////
        // keyboard interaction
        ////////////////////////////////////////////////////////////
        if (true)
        {
            // C: "calibrate"
            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log("run procam calibration");

                double reproj_error = RunCalibration();

                if (reproj_error > 1.0)
                {
                    Debug.LogWarning("WARNING: reprojection error exceeds 1.0");
                }
            }

            // R: "reset"
            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("reset calibration data");
                ResetCalibrationData();
            }
        }

        ////////////////////////////////////////////////////////////
        // capture checker board image
        ////////////////////////////////////////////////////////////
        _ColorManager = ColorSourceManager.GetComponent<ColorSourceManager>();
        _DepthManager = DepthSourceManager.GetComponent<DepthSourceManager>();
        if (null == _ColorManager || null == _DepthManager)
        {
            return;
        }

        // Spacebar: capture data
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Mat image_bgra = VisComp.ImgProc.ToMat(_ColorManager.GetColorTexture());

            Point3f[] _corners; // never used
            Point2f[] _points;
            if (_marker_script.GetDetectedCorners(image_bgra, out _corners, out _points, true, "detected"))
            {
                // PERFORMANCE ISSUE: API enforces this inefficient way to get 3D points
                ushort[] depthData = _DepthManager.GetData();
                CameraSpacePoint[] color2camera = new CameraSpacePoint[1920 * 1080];
                _Mapper.MapColorFrameToCameraSpace(depthData, color2camera);

                List<Point3f> _objectPoint = new List<Point3f>();
                List<Point2f> _imagePoint  = new List<Point2f>();

                foreach (var _p2d in _points)
                {
                    int idx = (int)(_p2d.X + _p2d.Y * 1920);
                    CameraSpacePoint _p3d = color2camera[idx];

                    Point3f p3d = new Point3f(_p3d.X, _p3d.Y, _p3d.Z);
                    Point2f p2d = new Point2f(_p2d.X, _p2d.Y);

                    _objectPoint.Add(p3d);
                    _imagePoint.Add(p2d);
                }

                objectPoints.Add(new Mat(_objectPoint.Count, 1, MatType.CV_32FC3, _objectPoint.ToArray()));
                imagePoints.Add( new Mat(_imagePoint.Count , 1, MatType.CV_32FC2, _imagePoint.ToArray()));

                images.Add(image_bgra);
            }
        }
    }

    #endregion // MONO_BEHAVIOUR



    #region SUBROUTINES

    List<Mat> objectPoints;
    List<Mat> imagePoints;
    List<Mat> images;

    void ResetCalibrationData()
    {
        objectPoints = new List<Mat>();

        imagePoints = new List<Mat>();
        images = new List<Mat>();
    }

    double RunCalibration()
    {
        int W = _screen_script.resolution.x;
        int H = _screen_script.resolution.y;
        Size imageSize = new Size(W, H);

        // initial guess for camera matrix
        Mat cameraMatrix = Mat.Eye(3, 3, MatType.CV_64F);
        cameraMatrix.Set<double>(0, 0, W);
        cameraMatrix.Set<double>(1, 1, H);
        cameraMatrix.Set<double>(0, 2, W / 2);
        cameraMatrix.Set<double>(1, 2, H / 2);

        // initial guess for distortion coefficients
        Mat distCoeffs = Mat.Eye(5, 1, MatType.CV_64F);
        Mat[] rvecs;
        Mat[] tvecs;

        // add other flags if you need
        CalibrationFlags flags = CalibrationFlags.UseIntrinsicGuess | CalibrationFlags.FixAspectRatio | CalibrationFlags.ZeroTangentDist;

        double reproj_error = Cv2.CalibrateCamera(objectPoints, imagePoints, imageSize, cameraMatrix, distCoeffs, out rvecs, out tvecs, flags);

        // [CHECK: OK]
        Debug.Log("reprojection error = " + reproj_error);

        // fx, fy, cx, cy
        Debug.Log("camera parameters = ("
            + cameraMatrix.At<double>(0, 0) + ", "
            + cameraMatrix.At<double>(1, 1) + ", "
            + cameraMatrix.At<double>(0, 2) + ", "
            + cameraMatrix.At<double>(1, 2) + ")");

        // create folder to save data
        string dir = System.DateTime.Now.ToString("yyyyMMdd_hhmmss");
        System.IO.Directory.CreateDirectory(dir);

        // export calibration data to XML file
        calib_filepath = dir + "/calib_procam.xml";
        using (var fs = new FileStorage(calib_filepath, FileStorage.Mode.Write))
        {
            fs.Add("image_size").Add(imageSize); // add image size
            fs.Write("camera_matrix", cameraMatrix);
            fs.Write("dist_coeffs", distCoeffs);
            fs.Write("reproj_error", reproj_error);

            // export images, also
            int cnt = 0;
            foreach (Mat img_bgra in images)
            {
                string filename = "images" + cnt.ToString("D2") + ".png";
                Cv2.ImWrite(dir + "/" + filename, img_bgra,
                    new ImageEncodingParam(ImwriteFlags.PngCompression, 0)); // save as .png without compression

                cnt++;
            }
        }

        return reproj_error;
    }

    #endregion // SUBROUTINES
}
