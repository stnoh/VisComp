using UnityEngine;
using OpenCvSharp;
using VisComp;

public class CameraTextureBehaviour : MonoBehaviour
{
    #region PUBLIC_MEMBERS

    public GameObject ImageQuad = null;

    public Mat image_bgra;

    public int image_width  = 640;
    public int image_height = 480;

    public bool fit = false;

    #endregion // PUBLIC_MEMBERS



    #region PROTECTED_METHODS

    protected void FitToScreen()
    {
        float image_aspect = (float)image_width / (float)image_height;

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

        ImageQuad.transform.localScale = new Vector3(scale_x, scale_y, 1.0f);
    }

    #endregion // PROTECTED_METHODS
}
