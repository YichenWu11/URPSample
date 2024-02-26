using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class AssetDatabaseIOExample
{
    [MenuItem("AssetDatabase/FileOperationsExample")]
    static void Example()
    {
        string ret;

        // 创建
        Material material = new Material(Shader.Find("Standard"));
        AssetDatabase.CreateAsset(material, "Assets/MyMaterial.mat");
        if (AssetDatabase.Contains(material))
            Debug.Log("Material asset created");

        // 重命名
        ret = AssetDatabase.RenameAsset("Assets/MyMaterial.mat", "MyMaterialNew");
        if (ret == "")
            Debug.Log("Material asset renamed to MyMaterialNew");
        else
            Debug.Log(ret);

        // 创建文件夹
        ret = AssetDatabase.CreateFolder("Assets", "NewFolder");
        if (AssetDatabase.GUIDToAssetPath(ret) != "")
            Debug.Log("Folder asset created");
        else
            Debug.Log("Couldn't find the GUID for the path");

        // 移动
        ret = AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(material), "Assets/NewFolder/MyMaterialNew.mat");
        if (ret == "")
            Debug.Log("Material asset moved to NewFolder/MyMaterialNew.mat");
        else
            Debug.Log(ret);

        // 复制
        if (AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(material), "Assets/MyMaterialNew.mat"))
            Debug.Log("Material asset copied as Assets/MyMaterialNew.mat");
        else
            Debug.Log("Couldn't copy the material");
        // 手动刷新数据库以通知更改
        AssetDatabase.Refresh();
        Material MaterialCopy = AssetDatabase.LoadAssetAtPath("Assets/MyMaterialNew.mat", typeof(Material)) as Material;

        // 移到垃圾箱
        if (AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(MaterialCopy)))
            Debug.Log("MaterialCopy asset moved to trash");

        // 删除
        if (AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(material)))
            Debug.Log("Material asset deleted");
        if (AssetDatabase.DeleteAsset("Assets/NewFolder"))
            Debug.Log("NewFolder deleted");

        // 进行所有更改后刷新 AssetDatabase
        AssetDatabase.Refresh();
    }
}