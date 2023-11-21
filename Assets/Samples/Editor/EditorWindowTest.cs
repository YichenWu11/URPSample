using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class EditorWindowTest : EditorWindow
{
    [MenuItem("EditorWindowTest/EditorWindowTest")]
    public static void ShowExample()
    {
        EditorWindowTest wnd = GetWindow<EditorWindowTest>();
        wnd.titleContent = new GUIContent("EditorWindowTest");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Click"))
        {
            Debug.Log("EditorWindowTest");
        }
    }

    // public void CreateGUI()
    // {
    //     // Each editor window contains a root VisualElement object
    //     VisualElement root = rootVisualElement;
    //
    //     // VisualElements objects can contain other VisualElement following a tree hierarchy
    //     Label label = new Label("Hello World!");
    //     root.Add(label);
    //
    //     // Create button
    //     Button button = new Button();
    //     button.name = "button";
    //     button.text = "Button";
    //     root.Add(button);
    //
    //     // Create toggle
    //     Toggle toggle = new Toggle();
    //     toggle.name = "toggle";
    //     toggle.label = "Toggle";
    //     root.Add(toggle);
    // }
}