using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using VisComp;
using OpenCvSharp;

public class StereoCalibrationBehaviour : MonoBehaviour
{
    #region PUBLIC_MEMBERS

    public GameObject CameraLeftObject;
    public GameObject CameraRightObject;
    public GameObject MarkerObject;

    public string calib_filepath;

    #endregion // PUBLIC_MEMBERS



    #region MONO_BEHAVIOUR

    CameraTextureBehaviour cameraL_script = null;
    CameraTextureBehaviour cameraR_script = null;
    MarkerObjectBehaviour  marker_script  = null;

    void Start()
    {
        cameraL_script = CameraLeftObject.GetComponentOrPause<CameraTextureBehaviour> ("ERROR: cannot get the CameraTextureBehaviour from CameraLeftObject");
        cameraR_script = CameraRightObject.GetComponentOrPause<CameraTextureBehaviour>("ERROR: cannot get the CameraTextureBehaviour from CameraRightObject");

        // check image size
        if (cameraL_script.image_width  != cameraR_script.image_width ||
            cameraL_script.image_height != cameraR_script.image_height )
        {
            Debug.LogError("ERROR: image size is different.");
            UnityEditor.EditorApplication.isPaused = true;
        }

        marker_script = MarkerObject.GetComponentOrPause<MarkerObjectBehaviour>("ERROR: cannot get the MarkerObjectBehaviour from MarkerObject");

        // initialize data in advance
        ResetCalibrationData();
    }

    void Update()
    {
        // capture image
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Point3f[] _objectPoints; // object points should be same
            Point2f[] _imagePointsL, _imagePointsR;
            Mat imageL_bgra = cameraL_script.image_bgra;
            Mat imageR_bgra = cameraR_script.image_bgra;

            bool detectedL = marker_script.GetDetectedCorners(imageL_bgra, out _objectPoints, out _imagePointsL, true, "cameraLeft");
            bool detectedR = marker_script.GetDetectedCorners(imageR_bgra, out _objectPoints, out _imagePointsR, true, "cameraRight");

            // when marker is detected from both images
            if (detectedL && detectedR)
            {
                objectPoints.Add(_objectPoints);
                imagePointsL.Add(_imagePointsL);
                imagePointsR.Add(_imagePointsR);

                imagesL.Add(imageL_bgra);
                imagesR.Add(imageR_bgra);
            }
        }

        // C: "calibrate"
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("run stereo calibration");

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

    #endregion // MONO_BEHAVIOUR




    #region SUBROUTINES

    List<IEnumerable<Point3f>> objectPoints;

    List<IEnumerable<Point2f>> imagePointsL;
    List<IEnumerable<Point2f>> imagePointsR;
    List<Mat> imagesL;
    List<Mat> imagesR;

    void ResetCalibrationData()
    {
        objectPoints = new List<IEnumerable<Point3f>>();

        imagePointsL = new List<IEnumerable<Point2f>>();
        imagePointsR = new List<IEnumerable<Point2f>>();
        imagesL = new List<Mat>();
        imagesR = new List<Mat>();
    }

    double RunCalibration()
    {
        // already checked two images have same size.
        int W = cameraL_script.image_width;
        int H = cameraL_script.image_height;
        Size imageSize = new Size(W, H);

        // initial guess for camera matrix
        double[,] _cameraMatrixL = new double[3, 3];
        double[,] _cameraMatrixR = new double[3, 3];
        _cameraMatrixL[0, 0] = _cameraMatrixR[0, 0] = W;
        _cameraMatrixL[1, 1] = _cameraMatrixR[1, 1] = H;
        _cameraMatrixL[0, 2] = _cameraMatrixR[0, 2] = W / 2;
        _cameraMatrixL[1, 2] = _cameraMatrixR[1, 2] = H / 2;

        double[] _distCoeffsL = new double[5];
        double[] _distCoeffsR = new double[5];

        // add other flags if you need
        CalibrationFlags flags = CalibrationFlags.UseIntrinsicGuess;

        // output values
        var R = new Mat(); // [R|t] rotation 
        var T = new Mat(); // [R|t] translation
        var E = new Mat(); // essential matrix
        var F = new Mat(); // fundamental matrix

        double reproj_error = Cv2.StereoCalibrate(objectPoints, imagePointsL, imagePointsR,
            _cameraMatrixL, _distCoeffsL, _cameraMatrixR, _distCoeffsR, imageSize,
            R, T, E, F, flags);

        // conversion to Cv.Mat
        System.Func<double[,], Mat> TransferCameraMatrix = (array) =>
        {
            Mat mat = Mat.Eye(3, 3, MatType.CV_64F);
            for (int j = 0; j < 3; j++)
            for (int i = 0; i < 3; i++)
            {
                mat.Set(j, i, array[j, i]);
            }
            return mat;
        };
        Mat cameraMatrixL = TransferCameraMatrix(_cameraMatrixL);
        Mat cameraMatrixR = TransferCameraMatrix(_cameraMatrixR);

        System.Func<double[], Mat> TransferDistCoeffs = (array) =>
        {
            Mat mat = Mat.Eye(5, 1, MatType.CV_64F);
            for (int i = 0; i < 5; i++)
            {
                mat.Set(i, 0, array[i]);
            }
            return mat;
        };
        Mat distCoeffsL = TransferDistCoeffs(_distCoeffsL);
        Mat distCoeffsR = TransferDistCoeffs(_distCoeffsR);

        Debug.Log("reprojection error = " + reproj_error);

        // create folder to save data
        string dir = System.DateTime.Now.ToString("yyyyMMdd_hhmmss");
        System.IO.Directory.CreateDirectory(dir);

        // export calibration data to XML file
        calib_filepath = dir + "/calib_stereo.xml";
        using (var fs = new FileStorage(calib_filepath, FileStorage.Mode.Write))
        {
            fs.Add("image_size").Add(imageSize); // add image size

            fs.Write("camera_matrix_left", cameraMatrixL);
            fs.Write("dist_coeffs_left", distCoeffsL);
            fs.Write("camera_matrix_right", cameraMatrixR);
            fs.Write("dist_coeffs_right", distCoeffsR);

            fs.Write("rotation_matrix", R);
            fs.Write("translation_vector", T);
            fs.Write("essential_matrix", E);
            fs.Write("fundamental_matrix", F);

            fs.Write("reproj_error", reproj_error);

            // export images, also
            int cnt = 0;
            foreach (Mat img_bgra in imagesL)
            {
                string filename = "images_left" + cnt.ToString("D2") + ".png";
                Cv2.ImWrite(dir + "/" + filename, img_bgra,
                    new ImageEncodingParam(ImwriteFlags.PngCompression, 0)); // save as .png without compression

                cnt++;
            }

            cnt = 0;
            foreach (Mat img_bgra in imagesR)
            {
                string filename = "images_right" + cnt.ToString("D2") + ".png";
                Cv2.ImWrite(dir + "/" + filename, img_bgra,
                    new ImageEncodingParam(ImwriteFlags.PngCompression, 0)); // save as .png without compression

                cnt++;
            }
        }

        return reproj_error;
    }

    #endregion // SUBROUTINES
}
