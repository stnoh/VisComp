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

        // set texture as WebCamTexture
        if (null != ImageQuad)
        {
            Renderer renderer = ImageQuad.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            renderer.material.shader = Shader.Find("Unlit/Texture");
            renderer.material.mainTexture = renderTexture;

            FitToScreen();
        }
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
