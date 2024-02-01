using UnityEngine;
using UnityEditor;

public class TestShaderGUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);

        if (GUILayout.Button("Button"))
        {
            foreach (var property in properties)
            {
                Debug.Log($"{property.name} : {property.type}");
            }
        }
    }
}