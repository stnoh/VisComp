using UnityEngine;

public class WebCamTextureBehaviour : MonoBehaviour
{
    #region PUBLIC_MEMBERS

    public GameObject WebcamTextureQuad = null;

    [HideInInspector]
    public WebCamTexture webCamTexture = null;

    [HideInInspector]
    public int selected_id = 0;

    public int image_width = 640;
    public int image_height = 480;

    public bool fit = false;

    #endregion // PUBLIC_MEMBERS



    #region PROTECTED_METHODS

    void FitToScreen()
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

        WebcamTextureQuad.transform.localScale = new Vector3(scale_x, scale_y, 1.0f);
    }

    #endregion // PROTECTED_METHODS



    #region MONO_BEHAVIOUR

    void Start()
    {
        webCamTexture = new WebCamTexture(WebCamTexture.devices[selected_id].name, image_width, image_height);
        webCamTexture.Play();

        // set texture as WebCamTexture
        if (null != WebcamTextureQuad)
        {
            Renderer renderer = WebcamTextureQuad.GetComponent<Renderer>();
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
        if (null != WebcamTextureQuad && fit) FitToScreen();
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
