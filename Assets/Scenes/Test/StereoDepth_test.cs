using UnityEngine;

using VisComp;
using OpenCvSharp;

public class StereoDepth_test : MonoBehaviour
{
    #region PUBLIC_MEMBERS

    public GameObject CameraLeftTextureObject;
    public GameObject CameraRightTextureObject;

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

    Camera cameraL = null;
    Camera cameraR = null;

    ParticleSystem ps = null;

    Size new_image_size;
    Mat imageL_undistorted_bgra;
    Mat imageR_undistorted_bgra;
    Mat imageL_undistorted_rgb;
    Mat imageR_undistorted_rgb;
    Mat imageL_undistorted_gray;
    Mat imageR_undistorted_gray;

    void Start()
    {
        cameraL_script = CameraLeftTextureObject.GetComponentOrPause <CameraTextureBehaviour>("ERROR: cannot get the CameraTextureBehaviour from CameraLeftTextureObject");
        cameraR_script = CameraRightTextureObject.GetComponentOrPause<CameraTextureBehaviour>("ERROR: cannot get the CameraTextureBehaviour from CameraRightTextureObject");

        rendererQuadL = ImageQuadLeftObject.GetComponentOrPause <Renderer>("ERROR: cannot get the Renderer from ImageQuad");
        rendererQuadL.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rendererQuadL.receiveShadows = false;
        rendererQuadL.material.shader = Shader.Find("Unlit/Texture");

        rendererQuadR = ImageQuadRightObject.GetComponentOrPause<Renderer>("ERROR: cannot get the Renderer from ImageQuad");
        rendererQuadR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rendererQuadR.receiveShadows = false;
        rendererQuadR.material.shader = Shader.Find("Unlit/Texture");

        cameraL = CameraLeftObject.GetComponentOrPause<Camera> ("ERROR: cannot get the Camera from CameraLeftObject");
        cameraR = CameraRightObject.GetComponentOrPause<Camera>("ERROR: cannot get the Camera from CameraRightObject");

        // ParticleSystem for point cloud rendering
        ps = gameObject.AddComponent<ParticleSystem>();
        Renderer renderer = ps.GetComponent<Renderer>();
        renderer.material.shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        ps.Stop(); // for update with script

        // check image size
        if (cameraL_script.image_width  != cameraR_script.image_width ||
            cameraL_script.image_height != cameraR_script.image_height)
        {
            Debug.LogError("ERROR: image size is different.");
            UnityEditor.EditorApplication.isPaused = true;
        }

        // [TODO] check image ratio for consistency

        // buffers for stereo rectification
        new_image_size = new Size(new_image_width, new_image_height);
        imageL_undistorted_bgra = new Mat(new_image_size, MatType.CV_8UC4);
        imageR_undistorted_bgra = new Mat(new_image_size, MatType.CV_8UC4);
        imageL_undistorted_rgb  = new Mat(new_image_size, MatType.CV_8UC3);
        imageR_undistorted_rgb  = new Mat(new_image_size, MatType.CV_8UC3);
        imageL_undistorted_gray = new Mat(new_image_size, MatType.CV_8UC1);
        imageR_undistorted_gray = new Mat(new_image_size, MatType.CV_8UC1);

        // calibration
        if (!LoadCalibrationData(calib_filepath))
        {
            Debug.LogError("ERROR: cannot load calibration data.");
            UnityEditor.EditorApplication.isPaused = true;
        }

        FitToScreen(cameraL_script, ImageQuadLeftObject);
        FitToScreen(cameraR_script, ImageQuadRightObject);

        InitStereo();
    }

    void Update()
    {
        bool both = true;

        if (null != cameraL_script.image_bgra)
        {
            Cv2.Remap(cameraL_script.image_bgra, imageL_undistorted_bgra, mapLx, mapLy, InterpolationFlags.Linear);
            //Cv2.ImShow("left-raw", cameraL_script.image_bgra);  // [CHECK: OK]
            //Cv2.ImShow("left-undistorted", imageL_undistorted); // [CHECK: OK]

            Cv2.CvtColor(imageL_undistorted_bgra, imageL_undistorted_rgb, ColorConversionCodes.BGRA2RGB);
            rendererQuadL.material.mainTexture = imageL_undistorted_rgb.ToTexture2D();
        }
        else
        {
            both = false;
        }

        if (null != cameraR_script.image_bgra)
        {
            Cv2.Remap(cameraR_script.image_bgra, imageR_undistorted_bgra, mapRx, mapRy, InterpolationFlags.Linear);
            //Cv2.ImShow("right-raw", cameraR_script.image_bgra);  // [CHECK: OK]
            //Cv2.ImShow("right-undistorted", imageR_undistorted); // [CHECK: OK]

            Cv2.CvtColor(imageR_undistorted_bgra, imageR_undistorted_rgb, ColorConversionCodes.BGRA2RGB);
            rendererQuadR.material.mainTexture = imageR_undistorted_rgb.ToTexture2D();
        }
        else
        {
            both = false;
        }

        if (both)
        {
            Cv2.CvtColor(imageL_undistorted_rgb, imageL_undistorted_gray, ColorConversionCodes.RGB2GRAY);
            Cv2.CvtColor(imageR_undistorted_rgb, imageR_undistorted_gray, ColorConversionCodes.RGB2GRAY);
            ComputeStereo(imageL_undistorted_gray, imageR_undistorted_gray);
            ps.SetParticles(pts3d, Length);
        }
    }

    #endregion // MONO_BEHAVIOUR



    #region SUBROUTINES

    // precomputed stereo rectification maps
    Mat mapLx, mapLy;
    Mat mapRx, mapRy;

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

            Tx = (float)Translation.At<double>(0);
        }

        // stereo retification
        Mat R1 = new Mat(); // 3x3 (rotation matrix)
        Mat R2 = new Mat(); // 3x3 (rotation matrix)
        Mat P1 = new Mat(); // 3x4
        Mat P2 = new Mat(); // 3x4
        Mat _Q = new Mat(); // 4x4 [FIX ME LATER] can be used for projectTo3D()

        Cv2.StereoRectify(cameraMatrixL, distCoeffsL, cameraMatrixR, distCoeffsR,
            imageSize, Rotation, Translation,
            R1, R2, P1, P2, _Q);

        // forceful centering for stereo matching
        Mat newCameraMatrix = Mat.Eye(3, 3, MatType.CV_32F);
        newCameraMatrix.Set(0, 0, new_focal_length);
        newCameraMatrix.Set(1, 1, new_focal_length);
        newCameraMatrix.Set(0, 2, new_image_width  * 0.5f);
        newCameraMatrix.Set(1, 2, new_image_height * 0.5f);

        // update 3D information based on the 
        Matrix4x4 proj = Scene3D.ProjectionMatrixFromCameraParameters(new_focal_length, new_focal_length, new_image_width / 2, new_image_height / 2, new_image_width, new_image_height);
        cameraL.projectionMatrix = proj;
        cameraR.projectionMatrix = proj;

        // [FIX ME LATER] 
        Vector3 tr = new Vector3((float)Translation.At<double>(0), (float)Translation.At<double>(1), (float)Translation.At<double>(2));
        //Vector3 tr = (float)Translation.At<double>(0) * Vector3.right;
        CameraRightObject.transform.position = -tr;

        // [FIX ME LATER] initialize rectification map & append mapping data
        mapLx = new Mat(new_image_size, MatType.CV_32FC1); mapLy = new Mat(new_image_size, MatType.CV_32FC1);
        mapRx = new Mat(new_image_size, MatType.CV_32FC1); mapRy = new Mat(new_image_size, MatType.CV_32FC1);
        Cv2.InitUndistortRectifyMap(cameraMatrixL, distCoeffsL, R1, newCameraMatrix, new_image_size, MatType.CV_32FC1, mapLx, mapLy);
        Cv2.InitUndistortRectifyMap(cameraMatrixR, distCoeffsR, R2, newCameraMatrix, new_image_size, MatType.CV_32FC1, mapRx, mapRy);

        return true;
    }

    StereoBM sbm;
    Mat disparity_16s;
    Mat disparity_32f;

    float Tx = 0.0f;
    Mat Q;
    Mat image3d;

    int Length = 0;
    ParticleSystem.Particle[] pts3d; // for visualization purpose

    void InitStereo()
    {
        // StereoBM: stereo block matcher in OpenCV
        sbm = StereoBM.Create(0, 5);
        sbm.MinDisparity = 0;
        sbm.Disp12MaxDiff = new_image_width;
        sbm.UniquenessRatio = 0;
        sbm.SpeckleRange = 50;
        sbm.SpeckleWindowSize = 150;

        // buffers for depth computation
        disparity_16s = new Mat(new_image_size, MatType.CV_16SC1);
        disparity_32f = new Mat(new_image_size, MatType.CV_32FC1);
        image3d       = new Mat(new_image_size, MatType.CV_32FC3);

        pts3d = new ParticleSystem.Particle[new_image_size.Width * new_image_size.Height];

        // [FIX ME LATER] projection matrix for stereo matching: https://stackoverflow.com/a/28317841
        Q = new Mat(4, 4, MatType.CV_64FC1, new double[] {
                1.0, 0.0,  0.0, -0.5*new_image_size.Width,
                0.0, 1.0,  0.0, -0.5*new_image_size.Height,
                0.0, 0.0,  0.0,  new_focal_length,
                0.0, 0.0, -1.0/Tx, 0.0 // suppose cx-cx' == 0
            });
    }

    void ComputeStereo(Mat img_L_8u, Mat img_R_8u, Mat mask_8u = null)
    {
        // check image data type (8bit, 1-channel)
        if (img_L_8u.Type() != img_R_8u.Type() || img_L_8u.Type() != MatType.CV_8UC1)
        {
            Debug.LogError("ERROR: input image is not compatible with StereoBM()");
            return;
        }

        // check image size
        if (img_L_8u.Size() != img_R_8u.Size() || img_L_8u.Size() != new_image_size)
        {
            Debug.LogError("ERROR: input image is not compatible with StereoBM()");
            return;
        }

        // scale raw disparity map with 1/16: https://stackoverflow.com/questions/28959440
        sbm.Compute(img_L_8u, img_R_8u, disparity_16s);
        disparity_16s.ConvertTo(disparity_32f, MatType.CV_32FC1, 1.0f / 16.0f);

        // convert disparity map to point cloud
        Cv2.ReprojectImageTo3D(disparity_32f, image3d, Q, false);

        // reserve point cloud in dynamic array
        Length = 0;

        var color_indices = imageL_undistorted_rgb.GetGenericIndexer<Vec3b>();
        var indices = image3d.GetGenericIndexer<Vec3f>();
        for (int j = 0; j < new_image_size.Height; j++)
        for (int i = 0; i < new_image_size.Width ; i++)
        {
            Vec3f pt3d = indices[j, i];
            if (VisComp.Helper.IsFinite(pt3d) && 0.0f < pt3d.Item2)
            {
                float x = +pt3d.Item0;
                float y = +pt3d.Item1;
                float z = +pt3d.Item2;

                // get the pixel color
                var color = color_indices[j, i];
                byte B = color.Item0;
                byte G = color.Item1;
                byte R = color.Item2;

                pts3d[Length].position = new Vector3(x, y, z);
                pts3d[Length].startColor = new Color32(R,G,B,255);
                pts3d[Length].startSize = 0.002f; // 1[mm]= 0.2[cm]
                Length++;
            }
        }
    }

    #endregion // SUBROUTINES



    #region MISCELLANEOUS

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

    #endregion // MISCELLANEOUS
}
