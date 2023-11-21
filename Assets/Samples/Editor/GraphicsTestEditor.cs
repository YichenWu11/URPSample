using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GraphicsTest))]
public class GraphicsTestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Click"))
        {
            Debug.Log("OnInspectorGUI Clicked");
        }
    }
}