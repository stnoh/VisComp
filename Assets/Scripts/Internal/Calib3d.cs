using UnityEngine;
using OpenCvSharp;

namespace VisComp
{
    public static class Calib3d
    {
       public static Matrix4x4 GetCameraPose(MatOfPoint3f obj_points, MatOfPoint2f img_points,
       Mat cameraMatrix, Mat distortionCoefficients)
       {
            if (MatType.CV_64FC1 != cameraMatrix.Type() ||
                MatType.CV_64FC1 != distortionCoefficients.Type())
            {
                Debug.LogWarning("WARNING: camera parameters are not double.");
            }

            // estimate 6-DoF marker pose (CV)
            Mat _rvec = new Mat();
            Mat _tvec = new Mat();
            Cv2.SolvePnP(obj_points, img_points, cameraMatrix, distortionCoefficients, _rvec, _tvec);

            // convert 6-DoF marker pose to camera pose (CV)
            // https://stackoverflow.com/questions/16265714/camera-pose-estimation-opencv-pnp
            Mat RotCV_T = new Mat();
            Cv2.Rodrigues(_rvec, RotCV_T);
            Mat _R = -RotCV_T.T();
            Mat _T = _R * _tvec;

            var R_CV = _R.GetGenericIndexer<double>();
            var T_CV = _T.GetGenericIndexer<double>();

            // convert coordinate system from CV to DX
            Matrix4x4 PoseRT = Matrix4x4.identity;

            PoseRT[0, 0] = -(float)R_CV[0, 0];
            PoseRT[1, 0] = +(float)R_CV[2, 0];
            PoseRT[2, 0] = +(float)R_CV[1, 0];

            PoseRT[0, 1] = +(float)R_CV[0, 1];
            PoseRT[1, 1] = -(float)R_CV[2, 1];
            PoseRT[2, 1] = -(float)R_CV[1, 1];

            PoseRT[0, 2] = -(float)R_CV[0, 2];
            PoseRT[1, 2] = +(float)R_CV[2, 2];
            PoseRT[2, 2] = +(float)R_CV[1, 2];

            PoseRT[0, 3] = +(float)T_CV[0];
            PoseRT[1, 3] = -(float)T_CV[2];
            PoseRT[2, 3] = -(float)T_CV[1];

            return PoseRT;
        }
    }
}
