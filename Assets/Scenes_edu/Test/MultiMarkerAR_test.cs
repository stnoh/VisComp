using System.Collections.Generic;
using UnityEngine;

using VisComp;
using OpenCvSharp;

public class MultiMarkerAR_test : MonoBehaviour
{
    #region PUBLIC_MEMBERS

    public GameObject CameraTextureObject;
    public GameObject ImageQuadObject;

    public GameObject CameraObject;
    public List<GameObject> MarkerObjects;

    public string calib_filepath;

    public bool fit = true;

    #endregion // PUBLIC_MEMBERS



    #region MONO_BEHAVIOUR

    CameraTextureBehaviour camera_script = null;

    Camera  virtualCamera = null;
    Renderer rendererQuad = null;

    Mat image_bgra;
    Mat image_bgr;

    Mat distCoeffs_zero;

    void Start()
    {
        camera_script = CameraTextureObject.GetComponentOrPause<CameraTextureBehaviour>("ERROR: there is no CameraTextureBehaviour in the CameraObject.");

        rendererQuad = ImageQuadObject.GetComponentOrPause<Renderer>("ERROR: there is no Renderer in the ImageQuadObject.");
        rendererQuad.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rendererQuad.receiveShadows = false;
        rendererQuad.material.shader = Shader.Find("Unlit/Texture");

        virtualCamera = CameraObject.GetComponentOrPause<Camera>("ERROR: there is no Camera in the CameraObject.");

        if (!LoadCalibrationData(calib_filepath))
        {
            UnityEditor.EditorApplication.isPaused = true;
        }

        image_bgra = new Mat(camera_script.image_height, camera_script.image_width, MatType.CV_8UC4);
        image_bgr  = new Mat(camera_script.image_height, camera_script.image_width, MatType.CV_8UC3);
        distCoeffs_zero = new Mat(5, 1, MatType.CV_64F);

        virtualCamera.projectionMatrix = Scene3D.ProjectionMatrixFromCameraParameters(
            (float)newCameraMatrix.At<double>(0, 0),
            (float)newCameraMatrix.At<double>(1, 1),
            (float)newCameraMatrix.At<double>(0, 2),
            (float)newCameraMatrix.At<double>(1, 2),
            camera_script.image_width, camera_script.image_height);

        FitToScreen();
    }

    void Update()
    {
        if (null != camera_script.image_bgra)
        {
            Cv2.Undistort(camera_script.image_bgra, image_bgra, cameraMatrix, distCoeffs, newCameraMatrix);

            Cv2.CvtColor(image_bgra, image_bgr, ColorConversionCodes.BGRA2RGB);
            rendererQuad.material.mainTexture = image_bgr.ToTexture2D();

            foreach (var go in MarkerObjects)
            {
                Point3f[] _objectPoints;
                Point2f[] _imagePoints;

                var marker_script = go.GetComponent<MarkerObjectBehaviour>();

                if (null == marker_script) continue;

                bool detected = marker_script.GetDetectedCorners(camera_script.image_bgra, out _objectPoints, out _imagePoints);

                if (detected)
                {
                    go.SetActive(true);

                    MatOfPoint3f objectPoints = new MatOfPoint3f(_objectPoints.Length, 1, _objectPoints);
                    MatOfPoint2f imagePoints  = new MatOfPoint2f(_imagePoints.Length, 1, _imagePoints);

                    Matrix4x4 pose = Matrix4x4.identity;
                    pose = Calib3d.GetCameraPose(objectPoints, imagePoints, newCameraMatrix, distCoeffs_zero);

                    Scene3D.Matrix2RigidTransform(pose.inverse, go.transform);
                }
                else
                {
                    go.SetActive(false);
                }
            }

            if (fit) FitToScreen();
        }
    }

    #endregion // MONO_BEHAVIOUR



    #region SUBROUTINES

    protected Mat cameraMatrix;
    protected Mat newCameraMatrix;
    protected Mat distCoeffs;

    bool LoadCalibrationData(string calib_filepath)
    {
        if (!System.IO.File.Exists(calib_filepath))
        {
            Debug.LogWarning("WARNING: there is no calibration file.");
            return false;
        }

        Size image_size;
        using (var fs = new FileStorage(calib_filepath, FileStorage.Mode.Read))
        {
            image_size = fs["image_size"].ReadSize();

            cameraMatrix = fs["camera_matrix"].ReadMat();
            distCoeffs = fs["dist_coeffs"].ReadMat();
        }

        OpenCvSharp.Rect roi;
        newCameraMatrix = Cv2.GetOptimalNewCameraMatrix(cameraMatrix, distCoeffs, image_size, 1.0, image_size, out roi);

        return true;
    }

    protected void FitToScreen()
    {
        float image_aspect = (float)camera_script.image_width / (float)camera_script.image_height;

        ////////////////////////////////////////
        // rescale ImageQuad to fill screen area
        ////////////////////////////////////////
        float scale_x = 1.0f;
        float scale_y = 1.0f;
        float screen_aspect = (float)Screen.width / (float)Screen.height;

        if (image_aspect > 1.0f)
        {
            scale_x *= image_aspect;
        }
        else
        {
            scale_y /= image_aspect;
        }

        if (screen_aspect < image_aspect)
        {
            scale_x *= screen_aspect / image_aspect;
            scale_y *= screen_aspect / image_aspect;
        }

        if (null != ImageQuadObject) ImageQuadObject.transform.localScale = new Vector3(scale_x, scale_y, 1.0f);
    }

    #endregion // SUBROUTINES
}
