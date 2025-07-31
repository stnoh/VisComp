using System.Collections.Generic;
using UnityEngine;

using OpenCvSharp;

public class ICP_test : MonoBehaviour
{
    #region PUBLIC_MEMBERS

    public GameObject pointCloud1 = null;
    public GameObject pointCloud2 = null;

    public string filepath = "";

    #endregion PUBLIC_MEMBERS


    #region MONO_BEHAVIOUR

    void Start()
    {
        Mesh m1 = VisComp.Helper.ReadObj(filepath);
        MeshFilter mf1 = pointCloud1.GetComponent<MeshFilter>();
        mf1.mesh = m1;

        Mesh m2 = VisComp.Helper.ReadObj(filepath);
        MeshFilter mf2 = pointCloud2.GetComponent<MeshFilter>();
        mf2.mesh = m2;

        // initial offset for testing
        pointCloud2.transform.position = new Vector3(-0.04f, 0.0f, -0.02f);
        pointCloud2.transform.rotation = Quaternion.Euler(-20.0f, -20.0f, -20.0f);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector3[] verts1 = pointCloud1.GetComponent<MeshFilter>().mesh.vertices;

            Vector3[] _verts2 = pointCloud2.GetComponent<MeshFilter>().mesh.vertices;
            Matrix4x4 RT2 = Matrix4x4.TRS(pointCloud2.transform.position, pointCloud2.transform.rotation, Vector3.one);
            Vector3[] verts2 = Convert(RT2, _verts2);

            IDictionary<int, int> correspondences = GetTheCorrespondences(verts1, verts2);

            Matrix4x4 PoseRT = Matrix4x4.identity;
            if (RunICP(verts1, verts2, correspondences, ref PoseRT))
            {
                PoseRT = PoseRT * RT2;
                pointCloud2.transform.rotation = VisComp.Scene3D.QuaternionFromMatrix4x4(PoseRT);
                pointCloud2.transform.position = PoseRT.GetColumn(3);
            }

        }
    }

    #endregion // MONO_BEHAVIOUR



    #region SUBROUTINE

    Vector3[] Convert(Matrix4x4 RT, Vector3[] verts)
    {
        Vector3[] verts_new = new Vector3[verts.Length];

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 v = verts[i];
            Vector4 v_new = RT * new Vector4(v.x, v.y, v.z, 1.0f);
            verts_new[i] = v_new;
        }

        return verts_new;
    }

    Vector3 ComputeAverage(Vector3[] verts)
    {
        Vector3 mu = Vector3.zero;

        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 v = verts[i];
            mu += v;
        }
        mu /= verts.Length;

        return mu;
    }

    IDictionary<int, int> GetTheCorrespondences(Vector3[] verts1, Vector3[] verts2)
    {
        // compute distances between two point clouds
        SortedList<float, List<(int, int)>> distance_v2v = new SortedList<float, List<(int, int)>>();

        for (int i = 0; i < verts1.Length - 1; i++)
            for (int j = i; j < verts2.Length; j++)
            {
                float dist = Vector3.Distance(verts1[i], verts2[j]);

                List<(int, int)> v2v_list;
                if (!distance_v2v.TryGetValue(dist, out v2v_list))
                {
                    v2v_list = new List<(int, int)>();
                    distance_v2v.Add(dist, v2v_list);
                }

                v2v_list.Add((i, j));
            }

        // simple point correspondences based on the sorted distances
        SortedDictionary<int, int> correspondence_v2v = new SortedDictionary<int, int>();
        ISet<int> v1_exist = new SortedSet<int>();
        ISet<int> v2_exist = new SortedSet<int>();

        foreach (var v2v_list in distance_v2v)
        {
            foreach (var kv in v2v_list.Value)
            {
                int vidx1 = kv.Item1;
                int vidx2 = kv.Item2;

                if (!v1_exist.Contains(vidx1) && !v2_exist.Contains(vidx2))
                {
                    correspondence_v2v.Add(vidx1, vidx2);
                    v1_exist.Add(vidx1);
                    v2_exist.Add(vidx2);
                    Debug.DrawLine(verts1[vidx1], verts2[vidx2], Color.yellow, 3.0f); // show 3 [sec]
                }
            }
        }

        return correspondence_v2v;
    }

    bool RunICP(Vector3[] verts1, Vector3[] verts2, IDictionary<int, int> correspondences, ref Matrix4x4 PoseRT)
    {
        // compute center-of-masses
        Vector3 mu1 = ComputeAverage(verts1);
        Vector3 mu2 = ComputeAverage(verts2);

        // compute accumulated error matrix
        Mat M = new Mat(3, 3, MatType.CV_64FC1);

        foreach (var kv in correspondences)
        {
            int vidx1 = kv.Key;
            int vidx2 = kv.Value;

            Vector3 v1 = verts1[vidx1] - mu1;
            Vector3 v2 = verts2[vidx2] - mu2;

            M.Set<double>(0, 0, M.At<double>(0, 0) + v1.x * v2.x);
            M.Set<double>(0, 1, M.At<double>(0, 1) + v1.x * v2.y);
            M.Set<double>(0, 2, M.At<double>(0, 2) + v1.x * v2.z);
            M.Set<double>(1, 0, M.At<double>(1, 0) + v1.y * v2.x);
            M.Set<double>(1, 1, M.At<double>(1, 1) + v1.y * v2.y);
            M.Set<double>(1, 2, M.At<double>(1, 2) + v1.y * v2.z);
            M.Set<double>(2, 0, M.At<double>(2, 0) + v1.z * v2.x);
            M.Set<double>(2, 1, M.At<double>(2, 1) + v1.z * v2.y);
            M.Set<double>(2, 2, M.At<double>(2, 2) + v1.z * v2.z);
        }

        // Singular Value Decomposition
        Mat W  = new Mat(3, 1, MatType.CV_64FC1); // only diagonal values
        Mat U  = new Mat(3, 3, MatType.CV_64FC1);
        Mat Vt = new Mat(3, 3, MatType.CV_64FC1);
        Cv2.SVDecomp(M, W, U, Vt, SVD.Flags.ModifyA);

        Debug.LogWarning("SVD: " + W.At<double>(0) + ", " + W.At<double>(1) + ", " + W.At<double>(2));

        if (double.IsInfinity(W.At<double>(0)) || double.IsNaN(W.At<double>(0)) ||
            double.IsInfinity(W.At<double>(1)) || double.IsNaN(W.At<double>(1)) ||
            double.IsInfinity(W.At<double>(2)) || double.IsNaN(W.At<double>(2)))
        {
            return false;
        }

        if (Mathf.Abs((float)W.At<double>(0)) < 1e-3 || Mathf.Abs((float)W.At<double>(0)) > 1.0 ||
            Mathf.Abs((float)W.At<double>(1)) < 1e-3 || Mathf.Abs((float)W.At<double>(1)) > 1.0 ||
            Mathf.Abs((float)W.At<double>(2)) > 1e+3 || Mathf.Abs((float)W.At<double>(2)) > 1.0)
        {
            return false;
        }

        Mat R_mat = Mat.Eye(3, 3, MatType.CV_64FC1);
        //R_mat = ; // [TODO] compute rotation matrix
        Mat T_vec = new Mat(3, 1, MatType.CV_64FC1);

        Mat mu1_vec = new Mat(3, 1, MatType.CV_64FC1);
        for (int i = 0; i < 3; i++)
        {
            mu1_vec.Set<double>(i, mu1[i]);
        }

        Mat mu2_vec = new Mat(3, 1, MatType.CV_64FC1);
        for (int i = 0; i < 3; i++)
        {
            mu2_vec.Set<double>(i, mu2[i]);
        }

        T_vec = mu1_vec - R_mat * mu2_vec;

        // convert to Unity's data structure
        for (int j = 0; j < 3; j++)
        for (int i = 0; i < 3; i++)
        {
            PoseRT[j, i] = (float)R_mat.At<double>(j,i);
        }
        for (int i = 0; i < 3; i++)
        {
            PoseRT[i, 3] = (float)T_vec.At<double>(i);
        }

        return true;
    }

    #endregion // SUBROUTINE
}
