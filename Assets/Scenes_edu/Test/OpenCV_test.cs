using UnityEngine;
using OpenCvSharp;
using VisComp;

public class OpenCV_test : MonoBehaviour
{
    #region PRIVATE_MEMBERS

    WebCamTextureBehaviour script;

    #endregion // PRIVATE_MEMBERS



    #region MONO_BEHAVIOUR

    void Start()
    {
        script = gameObject.GetComponent<WebCamTextureBehaviour>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Texture2D tex2D = script.webCamTexture.ToTexture2D();
            Mat img = new Mat(tex2D.height, tex2D.width, MatType.CV_8UC4, tex2D.GetRawTextureData());

            Cv2.ImShow("test", img);
        }
    }

    void OnDisable()
    {

    }

    #endregion // MONO_BEHAVIOUR
}
