using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestShaderKeywords : MonoBehaviour
{
    private Material m_Mat;

    private void OnValidate()
    {
        if (m_Mat == null)
            m_Mat = GetComponent<Renderer>().material;
    }

    // Start is called before the first frame update
    void Start()
    {
        m_Mat = GetComponent<Renderer>().material;

        m_Mat.SetColor("_OutputColor", new Color(1, 1, 0, 1));
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