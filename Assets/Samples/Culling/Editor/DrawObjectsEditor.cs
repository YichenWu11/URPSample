using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DrawObjects))]
public class DrawObjectsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Generate"))
        {
            var drawObjectsTarget = (DrawObjects)target;
            drawObjectsTarget.GenerateBuffers();
        }
    }
}