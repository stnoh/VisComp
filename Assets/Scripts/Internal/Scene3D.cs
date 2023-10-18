using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VisComp
{
    public static class Scene3D
    {
        public static void SetLocalTransformAsIdentity(GameObject go)
        {
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = Vector3.one;
        }

        public static Quaternion QuaternionFromMatrix4x4(Matrix4x4 m)
        {
            return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
        }

        public static void Matrix2RigidTransform(Matrix4x4 mat4x4_source, Transform transform_target)
        {
            transform_target.localPosition = mat4x4_source.GetColumn(3);
            transform_target.localRotation = QuaternionFromMatrix4x4(mat4x4_source);
            transform_target.localScale = Vector3.one; // assume unit scale (1,1,1)
        }

        public static Matrix4x4 ProjectionMatrixFromFrustum(float L, float R, float B, float T, float z_n, float z_f)
        {
            Matrix4x4 mat = Matrix4x4.zero;

            mat[0, 0] = 2.0f * z_n / (R - L);
            mat[0, 2] = (R + L) / (R - L);

            mat[1, 1] = 2.0f * z_n / (T - B);
            mat[1, 2] = (T + B) / (T - B);

            mat[2, 2] = -(z_f + z_n) / (z_f - z_n);
            mat[2, 3] = -2.0f * (z_f * z_n) / (z_f - z_n);

            mat[3, 2] = -1.0f;

            return mat;
        }

        public static float GetApproxFOV(Matrix4x4 proj, bool isVertical = true)
        {
            // fovy (field-of-view in y direction) as default
            float value = (isVertical) ? proj[1, 1] : proj[0, 0];
            return 2.0f * Mathf.Atan2(1.0f, value) * Mathf.Rad2Deg;
        }

        public static float[] FrustumParameters(Matrix4x4 proj)
        {
            Debug.Assert(proj[3, 2] == -1.0f); // only works for perspective matrix

            // compute clipping plane values
            float z_n = proj[2, 3] / (proj[2, 2] - 1.0f);
            float z_f = proj[2, 3] / (proj[2, 2] + 1.0f);

            // compute x and y
            float x_diff_inv = 0.5f / z_n * proj[0, 0];
            float y_diff_inv = 0.5f / z_n * proj[1, 1];
            float x_diff = 1.0f / x_diff_inv;   // x_r - x_l
            float y_diff = 1.0f / y_diff_inv;   // y_t - y_b
            float x_plus = x_diff * proj[0, 2]; // x_r + x_l
            float y_plus = y_diff * proj[1, 2]; // y_t + y_b

            // compute frustum x and y values
            float x_r = 0.5f * (x_plus + x_diff);
            float x_l = 0.5f * (x_plus - x_diff);
            float y_t = 0.5f * (y_plus + y_diff);
            float y_b = 0.5f * (y_plus - y_diff);

            return new float[] { x_l, x_r, y_b, y_t, z_n, z_f };
        }

        public static float[] NormalizedCameraParameters(Matrix4x4 proj)
        {
            Debug.Assert(proj[3, 2] == -1.0f); // only works for perspective matrix

            // compute clipping plane values
            float z_n = proj[2, 3] / (proj[2, 2] - 1.0f);
            float z_f = proj[2, 3] / (proj[2, 2] + 1.0f);

            // compute x and y
            float x_diff_inv = 0.5f / z_n * proj[0, 0];
            float y_diff_inv = 0.5f / z_n * proj[1, 1];
            float x_diff = 1.0f / x_diff_inv;
            float y_diff = 1.0f / y_diff_inv;
            float x_plus = x_diff * proj[0, 2];
            float y_plus = y_diff * proj[1, 2];

            // convert them to normalized camera parameters (width==1 and height==1)
            float cx = +0.5f * (1.0f - x_plus / x_diff);
            float cy = +0.5f * (1.0f + y_plus / y_diff); // [CAUTION] DX: Y-up / CV: Y-down
            float fx = z_n * x_diff_inv;
            float fy = z_n * y_diff_inv;

            return new float[] { fx, fy, cx, cy };
        }

        public static float[] CameraParameters(Matrix4x4 proj, int width, int height)
        {
            float[] values = NormalizedCameraParameters(proj);
            float fx = values[0];
            float fy = values[1];
            float cx = values[2];
            float cy = values[3];

            return new float[] { width * fx, height * fy, width * cx, height * cy };
        }

        public static Matrix4x4 ProjectionMatrixFromNormalizedCameraParameters(float fx, float fy, float cx, float cy, float z_n = 0.01f, float z_f = 1000.0f)
        {
            Matrix4x4 mat = Matrix4x4.zero;

            // convert camera matrix to frustum values
            float L = -z_n / fx * cx;
            float R = +z_n / fx * (1.0f - cx);
            float B = -z_n / fy * (1.0f - cy); // [CAUTION] CV: y-down / DX: y-up
            float T = +z_n / fy * cy;

            return ProjectionMatrixFromFrustum(L, R, B, T, z_n, z_f);
        }

        public static Matrix4x4 ProjectionMatrixFromCameraParameters(float fx, float fy, float cx, float cy, int width, int height, float z_n = 0.01f, float z_f = 1000.0f)
        {
            return ProjectionMatrixFromNormalizedCameraParameters(fx/(float)width, fy/ (float)height, cx/ (float)width, cy/ (float)height, z_n, z_f);
        }
    }
}
