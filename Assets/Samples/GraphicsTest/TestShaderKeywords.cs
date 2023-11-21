using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestShaderKeywords : MonoBehaviour
{
    private Material m_Mat;

    // Start is called before the first frame update
    void Start()
    {
        m_Mat = GetComponent<Renderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        m_Mat.EnableKeyword("_TEST_");
        foreach (var keyWord in m_Mat.shaderKeywords)
        {
            Debug.Log(keyWord);
        }
    }
}