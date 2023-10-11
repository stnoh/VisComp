using UnityEngine;

using OpenCvSharp;
using VisComp;

[RequireComponent(typeof(Camera))]
public class VirtualCameraBehaviour : CameraTextureBehaviour
{
    #region PRIVATE_MEMBERS

    Camera        renderCamera  = null;
    RenderTexture renderTexture = null;

    #endregion // PRIVATE_MEMBERS



    #region MONO_BEHAVIOUR

    void Awake()
    {
        renderCamera = gameObject.GetComponent<Camera>();
        renderTexture = new RenderTexture(image_width, image_height, 32);
    }

    void Update()
    {
        // render camera view on RenderTexture
        renderCamera.targetTexture = renderTexture;
        renderCamera.Render();
        renderCamera.targetTexture = null;

        // convert RenderTexture to Mat
        image_bgra = renderTexture.ToMat();
        //Cv2.ImShow("rendered", image_bgra); // [CHECK: OK]
    }

    void OnDisable()
    {

    }

    #endregion // MONO_BEHAVIOUR
}
