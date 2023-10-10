using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Frustum_test : MonoBehaviour
{
    #region PUBLIC_MEMBERS

    public GameObject CameraObject = null;

    #endregion // PUBLIC_MEMBERS



    #region PROTECTED_MEMBERS

    Camera renderCamera = null;
    RenderTexture renderTexture = null;

    #endregion //PROTECTED_MEMBERS



    #region MONO_BEHAVIOUR

    void Start()
    {
        renderCamera = CameraObject.GetComponent<Camera>();
        renderTexture = new RenderTexture(renderCamera.pixelWidth, renderCamera.pixelHeight, 32);

        var mr = gameObject.GetComponent<MeshRenderer>();
        mr.material.mainTexture = renderTexture;
    }

    void Update()
    {
        Matrix4x4 proj = Matrix4x4.zero;

        // [change these parts]
        float x_l = -0.5f;
        float x_r = +0.5f;
        float y_t = +0.5f;
        float y_b = -0.5f;
        float z_n = -CameraObject.transform.position.z;
        float z_f = 1000.0f;

        // set projection matrix
        proj[0, 0] = 2.0f * z_n / (x_r - x_l);
        proj[0, 2] = (x_r + x_l) / (x_r - x_l);
        proj[1, 1] = 2.0f * z_n / (y_t - y_b); ;
        proj[1, 2] = (y_t + y_b) / (y_t - y_b);
        proj[2, 2] = -(z_f + z_n) / (z_f - z_n);
        proj[2, 3] = -(2.0f * z_f * z_n) / (z_f - z_n);
        proj[3, 2] = -1.0f;

        // text output to Console (if you need)
        /*
        Debug.Log(
            proj[0, 0] + " " + proj[0, 1] + " " + proj[0, 2] + " " + proj[0, 3] + "\n" +
            proj[1, 0] + " " + proj[1, 1] + " " + proj[1, 2] + " " + proj[1, 3] + "\n" +
            proj[2, 0] + " " + proj[2, 1] + " " + proj[2, 2] + " " + proj[2, 3] + "\n" +
            proj[3, 0] + " " + proj[3, 1] + " " + proj[3, 2] + " " + proj[3, 3] );
        //*/
        renderCamera.projectionMatrix = proj;

        // rendering to set texture
        renderCamera.targetTexture = renderTexture;
        renderCamera.Render();
        renderCamera.targetTexture = null;
    }

    #endregion // MONO_BEHAVIOUR
}
