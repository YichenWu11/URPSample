using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BuddyAllocatorTest))]
public class BuddyAllocatorTestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var allocatorTest = target as BuddyAllocatorTest;
        if (GUILayout.Button("Click0"))
        {
            allocatorTest.AllocateTest0();
        }
        if (GUILayout.Button("Click1"))
        {
            allocatorTest.AllocateTest1();
        }
    }
}