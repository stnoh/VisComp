using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ChessboardMarkerBehaviour))]
public class ChessboardMarkerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // draw all public objects at first

        ChessboardMarkerBehaviour myScript = (ChessboardMarkerBehaviour)target;

        if (GUILayout.Button("Create marker"))
        {
            myScript.CreateMarkerTexture2D();
        }

        if (null != myScript.material)
        {
            if (GUILayout.Button("Export marker"))
            {
                string path = EditorUtility.SaveFilePanel("Save markermap as a PDF file", "./", "Chessboard", "pdf");

                if (path.Length != 0)
                {
                    myScript.ExportPDF(path);
                }
            }

            if (GUILayout.Button("Remove marker"))
            {
                myScript.RemoveMarkerTexture2D();
            }
        }
    }
}
