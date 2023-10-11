﻿using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using OpenCvSharp;

public class IntrinsicCalibrationBehaviour : MonoBehaviour
{
    #region PUBLIC_MEMBERS

    public GameObject CameraObject;
    public GameObject MarkerObject;

    #endregion // PUBLIC_MEMBERS



    #region MONO_BEHAVIOUR

    CameraTextureBehaviour camera_script = null;
    MarkerObjectBehaviour  marker_script = null;

    void Start()
    {
        camera_script = CameraObject.GetComponent<CameraTextureBehaviour>();

        if (null == camera_script)
        {
            Debug.LogError("ERROR: there is no CameraTextureBehaviour in the CameraObject.");
            UnityEditor.EditorApplication.isPaused = true;
        }

        marker_script = MarkerObject.GetComponent<MarkerObjectBehaviour>();

        if (null == marker_script)
        {
            Debug.LogError("ERROR: there is no MarkerObjectBehaviour in the MarkerObject.");
            UnityEditor.EditorApplication.isPaused = true;
        }

        // initialize data in advance
        ResetCalibrationData();
    }

    void Update()
    {
        // capture image
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Point3f[] _objectPoints;
            Point2f[] _imagePoints;
            Mat image_bgra = camera_script.image_bgra;

            if (marker_script.GetDetectedCorners(image_bgra, out _objectPoints, out _imagePoints, true))
            {
                objectPoints.Add(new Mat(_objectPoints.Length, 1, MatType.CV_32FC3, _objectPoints));
                imagePoints.Add(new Mat(_imagePoints.Length, 1, MatType.CV_32FC2, _imagePoints));
                images.Add(image_bgra);
            }
        }

        // C: "calibrate"
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("run intrinsic calibration");
            double reproj_err = RunCalibration();
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

    List<Mat> objectPoints;
    List<Mat> imagePoints;
    List<Mat> images;

    void ResetCalibrationData()
    {
        objectPoints = new List<Mat>();
        imagePoints  = new List<Mat>();
        images = new List<Mat>();
    }

    double RunCalibration()
    {
        int W = camera_script.image_width;
        int H = camera_script.image_width;
        Size imageSize = new Size(W, H);

        // initial guess for camera matrix
        Mat cameraMatrix = Mat.Eye(3, 3, MatType.CV_64F);
        cameraMatrix.Set<double>(0, 0, W);
        cameraMatrix.Set<double>(1, 1, H);
        cameraMatrix.Set<double>(0, 2, W / 2);
        cameraMatrix.Set<double>(1, 2, H / 2);

        // add other flags if you need
        CalibrationFlags flags = CalibrationFlags.UseIntrinsicGuess;

        Mat distCoeffs = Mat.Eye(5, 1, MatType.CV_64F);
        Mat[] rvecs;
        Mat[] tvecs;

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
        using (var fs = new FileStorage(dir + "/calibration.xml", FileStorage.Mode.Write))
        {
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
