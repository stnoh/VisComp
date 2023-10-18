using UnityEngine;

using VisComp;
using OpenCvSharp;

public class StereoDepth_test : MonoBehaviour
{
    #region PUBLIC_MEMBERS

    public GameObject CameraLeftObject;
    public GameObject CameraRightObject;

    public GameObject ImageQuadLeftObject;
    public GameObject ImageQuadRightObject;

    public string calib_filepath;

    public float new_focal_length = 500.0f;
    public int new_image_width  = 320; // 640:320 = 2:1
    public int new_image_height = 240; // 480:240 = 2:1

    #endregion // PUBLIC_MEMBERS



    #region MONO_BEHAVIOUR

    CameraTextureBehaviour cameraL_script = null;
    CameraTextureBehaviour cameraR_script = null;

    Renderer rendererQuadL = null;
    Renderer rendererQuadR = null;

    Size new_image_size;
    Mat imageL_undistorted_bgra;
    Mat imageR_undistorted_bgra;
    Mat imageL_undistorted_rgb;
    Mat imageR_undistorted_rgb;

    void Start()
    {
        cameraL_script = CameraLeftObject.GetComponentOrPause <CameraTextureBehaviour>("ERROR: cannot get the CameraTextureBehaviour from CameraLeftObject");
        cameraR_script = CameraRightObject.GetComponentOrPause<CameraTextureBehaviour>("ERROR: cannot get the CameraTextureBehaviour from CameraRightObject");

        rendererQuadL = ImageQuadLeftObject.GetComponentOrPause <Renderer>("ERROR: cannot get the Renderer from ImageQuad");
        rendererQuadL.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rendererQuadL.receiveShadows = false;
        rendererQuadL.material.shader = Shader.Find("Unlit/Texture");

        rendererQuadR = ImageQuadRightObject.GetComponentOrPause<Renderer>("ERROR: cannot get the Renderer from ImageQuad");
        rendererQuadR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rendererQuadR.receiveShadows = false;
        rendererQuadR.material.shader = Shader.Find("Unlit/Texture");

        // check image size
        if (cameraL_script.image_width  != cameraR_script.image_width ||
            cameraL_script.image_height != cameraR_script.image_height)
        {
            Debug.LogError("ERROR: image size is different.");
            UnityEditor.EditorApplication.isPaused = true;
        }

        new_image_size = new Size(new_image_width, new_image_height);
        imageL_undistorted_bgra = new Mat(new_image_size, MatType.CV_8UC4);
        imageR_undistorted_bgra = new Mat(new_image_size, MatType.CV_8UC4);
        imageL_undistorted_rgb  = new Mat(new_image_size, MatType.CV_8UC3);
        imageR_undistorted_rgb  = new Mat(new_image_size, MatType.CV_8UC3);

        // calibration
        if (!LoadCalibrationData(calib_filepath))
        {
            Debug.LogError("ERROR: cannot load calibration data.");
            UnityEditor.EditorApplication.isPaused = true;
        }

        FitToScreen(cameraL_script, ImageQuadLeftObject);
        FitToScreen(cameraR_script, ImageQuadRightObject);
    }

    void Update()
    {
        if (null != cameraL_script.image_bgra)
        {
            Cv2.Remap(cameraL_script.image_bgra, imageL_undistorted_bgra, mapLx, mapLy, InterpolationFlags.Linear);
            //Cv2.ImShow("left-raw", cameraL_script.image_bgra);  // [CHECK: OK]
            //Cv2.ImShow("left-undistorted", imageL_undistorted); // [CHECK: OK]

            Cv2.CvtColor(imageL_undistorted_bgra, imageL_undistorted_rgb, ColorConversionCodes.BGRA2RGB);
            rendererQuadL.material.mainTexture = imageL_undistorted_rgb.ToTexture2D();
        }

        if (null != cameraR_script.image_bgra)
        {
            Cv2.Remap(cameraR_script.image_bgra, imageR_undistorted_bgra, mapRx, mapRy, InterpolationFlags.Linear);
            //Cv2.ImShow("right-raw", cameraR_script.image_bgra);  // [CHECK: OK]
            //Cv2.ImShow("right-undistorted", imageR_undistorted); // [CHECK: OK]

            Cv2.CvtColor(imageR_undistorted_bgra, imageR_undistorted_rgb, ColorConversionCodes.BGRA2RGB);
            rendererQuadR.material.mainTexture = imageR_undistorted_rgb.ToTexture2D();
        }
    }

    #endregion // MONO_BEHAVIOUR


    #region SUBROUTINES

    protected Mat mapLx, mapLy;
    protected Mat mapRx, mapRy;

    bool LoadCalibrationData(string calib_filepath)
    {
        if (!System.IO.File.Exists(calib_filepath))
        {
            Debug.LogWarning("WARNING: there is no calibration file.");
            return false;
        }

        Size imageSize;
        Mat cameraMatrixL, cameraMatrixR;
        Mat distCoeffsL  , distCoeffsR;

        Mat Rotation, Translation;

        using (var fs = new FileStorage(calib_filepath, FileStorage.Mode.Read))
        {
            imageSize = fs["image_size"].ReadSize();

            cameraMatrixL = fs["camera_matrix_left"].ReadMat();
            distCoeffsL   = fs["dist_coeffs_left"].ReadMat();

            cameraMatrixR = fs["camera_matrix_right"].ReadMat();
            distCoeffsR   = fs["dist_coeffs_right"].ReadMat();

            Rotation    = fs["rotation_matrix"].ReadMat();
            Translation = fs["translation_vector"].ReadMat();
        }

        // stereo retification
        Mat R1 = new Mat(); // 3x3
        Mat R2 = new Mat(); // 3x3
        Mat P1 = new Mat(); // 3x4
        Mat P2 = new Mat(); // 3x4
        Mat Q  = new Mat(); // 4x4

        Cv2.StereoRectify(cameraMatrixL, distCoeffsL, cameraMatrixR, distCoeffsR,
            imageSize, Rotation, Translation,
            R1, R2, P1, P2, Q);

        // forceful centering for stereo matching
        Mat newCameraMatrixL = Mat.Eye(3, 3, MatType.CV_32F);
        newCameraMatrixL.Set(0, 0, new_focal_length);
        newCameraMatrixL.Set(1, 1, new_focal_length);
        newCameraMatrixL.Set(0, 2, new_image_width  * 0.5f);
        newCameraMatrixL.Set(1, 2, new_image_height * 0.5f);

        Mat newCameraMatrixR = Mat.Eye(3, 3, MatType.CV_32F);
        newCameraMatrixR.Set(0, 0, new_focal_length);
        newCameraMatrixR.Set(1, 1, new_focal_length);
        newCameraMatrixR.Set(0, 2, new_image_width  * 0.5f);
        newCameraMatrixR.Set(1, 2, new_image_height * 0.5f);

        Mat Identity = Mat.Eye(3, 3, MatType.CV_32F);

        mapLx = new Mat(new_image_size, MatType.CV_32FC1); mapLy = new Mat(new_image_size, MatType.CV_32FC1);
        mapRx = new Mat(new_image_size, MatType.CV_32FC1); mapRy = new Mat(new_image_size, MatType.CV_32FC1);
        Cv2.InitUndistortRectifyMap(cameraMatrixL, distCoeffsL, Identity, newCameraMatrixL, new_image_size, MatType.CV_32FC1, mapLx, mapLy);
        Cv2.InitUndistortRectifyMap(cameraMatrixR, distCoeffsR, Rotation, newCameraMatrixR, new_image_size, MatType.CV_32FC1, mapRx, mapRy);

        return true;
    }

    protected void FitToScreen(CameraTextureBehaviour camera_script, GameObject ImageQuadObject)
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
