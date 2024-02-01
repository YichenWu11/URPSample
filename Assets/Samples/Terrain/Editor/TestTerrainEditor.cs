using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TestTerrain))]
public class TestTerrainEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Output"))
        {
            var testTerrain = (TestTerrain)target;
            testTerrain.DebugOutput();
        }
    }
}