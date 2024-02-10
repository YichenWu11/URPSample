using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenObjects : MonoBehaviour
{
    public GameObject cubePrefab;
    public int size;

    private GameObject[] m_GameObjects;

    public void Generate()
    {
        Random.InitState(0);

        int actualSize = size * size;
        float interval = 100.0f / (size - 1);
        float startX = -50.0f;
        float startZ = -50.0f;
        m_GameObjects = new GameObject[actualSize];

        for (int i = 0; i < size; ++i)
        {
            for (int j = 0; j < size; ++j)
            {
                int idx = i * size + j;
                m_GameObjects[idx] = Instantiate(cubePrefab);
                m_GameObjects[idx].transform.position =
                    new Vector3(startX + i * interval, 2.0f, startZ + j * interval);
                float scaleVal = Random.Range(1.0f, 3.0f);
                m_GameObjects[idx].transform.localScale =
                    new Vector3(scaleVal, scaleVal, scaleVal);
                m_GameObjects[idx].transform.parent = this.gameObject.transform;
            }
        }
    }

    public void Clear()
    {
        if (m_GameObjects == null || m_GameObjects.Length == 0)
            return;

        foreach (var go in m_GameObjects)
        {
            DestroyImmediate(go);
        }
    }
}