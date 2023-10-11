using UnityEngine;
using VisComp;

public class WebCamTextureBehaviour : CameraTextureBehaviour
{
    #region PUBLIC_MEMBERS

    [HideInInspector]
    public WebCamTexture webCamTexture = null;

    [HideInInspector]
    public int selected_id = 0;

    #endregion // PUBLIC_MEMBERS



    #region MONO_BEHAVIOUR

    void Start()
    {
        webCamTexture = new WebCamTexture(WebCamTexture.devices[selected_id].name, image_width, image_height);
        webCamTexture.Play();

        // set texture as WebCamTexture
        if (null != ImageQuad)
        {
            Renderer renderer = ImageQuad.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            renderer.material.shader = Shader.Find("Unlit/Texture");
            renderer.material.mainTexture = webCamTexture;

            image_width  = webCamTexture.width;
            image_height = webCamTexture.height;

            FitToScreen();
        }
    }

    void Update()
    {
        if (null != ImageQuad && fit) FitToScreen();

        image_bgra = webCamTexture.ToMat();
    }

    void OnDisable()
    {
        if (null != webCamTexture)
        {
            webCamTexture.Stop();
        }
    }

    #endregion // MONO_BEHAVIOUR
}
