using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReplaceMaterial : MonoBehaviour
{
    public GameObject go;

    private const string k_toonShaderName = "SimpleURPToonLitExample(With Outline)";

    public void ReplaceAll()
    {
        if (go == null)
            return;

        var renderers = go.GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (var r in renderers)
        {
            // Debug.Log(r.sharedMaterial.mainTexture.name);
            Material sharedMaterial = r.sharedMaterial;
            var mainTex = sharedMaterial.mainTexture;
            sharedMaterial.shader = Shader.Find(k_toonShaderName);
            sharedMaterial.mainTexture = mainTex;
        }
    }
}