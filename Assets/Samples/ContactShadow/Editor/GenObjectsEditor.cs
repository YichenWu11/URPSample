using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GenObjects))]
public class GenObjectsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var genObjects = (GenObjects)target;

        if (GUILayout.Button("Generate"))
        {
            genObjects.Generate();
        }

        if (GUILayout.Button("Clear"))
        {
            genObjects.Clear();
        }
    }
}