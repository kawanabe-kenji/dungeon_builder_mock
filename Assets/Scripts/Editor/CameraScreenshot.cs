using System;
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(Camera))]
public class CameraScreenshot : Editor
{
    string exportFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    public override void OnInspectorGUI()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            GUILayout.Label("Screenshot");
            exportFolderPath = EditorGUILayout.TextField("Folder Path", exportFolderPath);
            if (GUILayout.Button("Export and Save"))
            {
                if (!Directory.Exists(exportFolderPath)) Directory.CreateDirectory(exportFolderPath);
                string exportPath = $"{exportFolderPath}/ss_{DateTime.Now:yyyyMMddHHmmss}.png";
                ScreenCapture.CaptureScreenshot(exportPath);
                File.Exists(exportPath);
                Debug.Log("Exported the screenshot.");
            }
        }
        EditorGUILayout.EndVertical();
        base.OnInspectorGUI();
    }
}