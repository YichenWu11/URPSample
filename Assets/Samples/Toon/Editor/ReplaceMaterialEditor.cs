using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ReplaceMaterial))]
public class ReplaceMaterialEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var replaceMaterial = target as ReplaceMaterial;

        if (GUILayout.Button("ReplaceAll"))
        {
            replaceMaterial.ReplaceAll();
        }
    }
}