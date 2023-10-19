using System.Collections.Generic;
using System.Runtime;
using UnityEngine;

using VisComp;
using OpenCvSharp;

public abstract class MarkerObjectBehaviour : MonoBehaviour
{
    #region PUBLIC_MEMBERS

    [HideInInspector]
    public Material material = null;

    #endregion // PUBLIC_MEMBERS



    #region PROTECTED_METHODS

    protected virtual Mat CreateMarkerMap()
    {
        Debug.LogError("this function should be overrided.");
        throw new System.Exception();
    }

    protected float W_mm, H_mm;

    #endregion // PROTECTED_METHODS



    #region PUBLIC_METHODS

    public virtual bool GetDetectedCorners(Mat image_bgra, out Point3f[] objectPoints, out Point2f[] imagePoints, bool show = false, string winname="detected")
    {
        Debug.LogError("this function should be overrided.");
        throw new System.Exception();
    }

    public void CreateMarkerTexture2D()
    {
        if (null != material) RemoveMarkerTexture2D();

        // create markermap
        Mat img_bgr = CreateMarkerMap();
        //Cv2.ImShow("Chessboard", img_bgr); // [CHECK: OK]

        Texture2D tex2D = img_bgr.ToTexture2D();

        // mesh for markermap: [mm] to [m]
        float half_scale_x = 0.5f * W_mm * 0.001f;
        float half_scale_z = 0.5f * H_mm * 0.001f;

        // add Components to this GameObject
        var mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = VisComp.Helper.GetBoardMesh(half_scale_x, half_scale_z);

        material = new Material(Shader.Find("Unlit/Texture"));
        material.mainTexture = tex2D;

        var mr = gameObject.AddComponent<MeshRenderer>();
        mr.material = material;
    }

    public void ExportPDF(string filepath)
    {
        Mat img_bgra = material.mainTexture.ToMat();
        VisComp.Helper.ExportPDF(filepath, img_bgra, new Vector2(W_mm, H_mm));
    }

    public void RemoveMarkerTexture2D()
    {
        DestroyImmediate(material);

        var mf = gameObject.GetComponent<MeshFilter>();
        if (null != mf) DestroyImmediate(mf);

        var mr = gameObject.GetComponent<MeshRenderer>();
        if (null != mr) DestroyImmediate(mr);
    }

    #endregion // PUBLIC_METHODS
}
