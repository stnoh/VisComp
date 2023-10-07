using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WebCamTextureBehaviour))]
public class WebCamTextureEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WebCamTextureBehaviour myScript = (WebCamTextureBehaviour)target;

        DrawDefaultInspector();

        // select WebCam from dropdown list
        string[] devices = new string[WebCamTexture.devices.Length];

        for (int i = 0; i < devices.Length; i++)
        {
            devices[i] = WebCamTexture.devices[i].name;
        }

        myScript.selected_id = EditorGUILayout.Popup("device name", myScript.selected_id, devices);
    }
}
