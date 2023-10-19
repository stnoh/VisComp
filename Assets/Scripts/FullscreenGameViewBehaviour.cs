using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class FullscreenGameViewBehaviour : MonoBehaviour
{
    #region STATIC_MEMBERS

    static readonly Type GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
    static readonly PropertyInfo ShowToolbarProperty = GameViewType.GetProperty("showToolbar", BindingFlags.Instance | BindingFlags.NonPublic);

    #endregion // STATIC_MEMBERS



    #region PUBLIC_MEMBERS

    public Vector2Int offset     = Vector2Int.zero;
    public Vector2Int resolution = new Vector2Int(1920, 1080);

    #endregion // PUBLIC_MEMBERS



    #region MONO_BEHAVIOUR

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            Toggle();
        }
    }

    void OnDisable()
    {
        if (instance != null)
        {
            instance.Close();
            instance = null;
        }
    }

    #endregion // MONO_BEHAVIOUR



    #region SUBROUTINE

    EditorWindow instance;

    void Toggle()
    {
        if (GameViewType == null)
        {
            Debug.LogError("GameView type not found.");
            return;
        }

        if (ShowToolbarProperty == null)
        {
            Debug.LogWarning("GameView.showToolbar property not found.");
        }

        if (instance != null)
        {
            instance.Close();
            instance = null;
        }
        else
        {
            instance = (EditorWindow)ScriptableObject.CreateInstance(GameViewType);

            ShowToolbarProperty?.SetValue(instance, false);

            var fullscreenRect = new Rect(offset, resolution);

            instance.ShowPopup();
            instance.position = fullscreenRect;
            instance.Focus();
        }
    }

    #endregion // SUBROUTINE
}
