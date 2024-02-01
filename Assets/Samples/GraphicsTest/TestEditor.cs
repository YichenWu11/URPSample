using UnityEngine;

public class TestEditor : MonoBehaviour
{
    void Start()
    {
    }

    void OnGUI()
    {
        if (GUILayout.Button("Button"))
        {
            Debug.Log("ok");
        }
    }
}