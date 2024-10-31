using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MaterialFinder : MonoBehaviour
{
    public static List<Material> GetMaterialsInFolder(string folderPath)
    {
        // Ensure the folder path starts with "Assets/"
        if (!folderPath.StartsWith("Assets/"))
        {
            Debug.LogError("Folder path must start with 'Assets/'.");
            return null;
        }

        // Get all asset paths in the folder
        string[] assetPaths = AssetDatabase.FindAssets("t:Material", new[] { folderPath });

        List<Material> materials = new List<Material>();

        // Load each asset as a Material and add it to the list
        foreach (string assetPath in assetPaths)
        {
            string path = AssetDatabase.GUIDToAssetPath(assetPath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                materials.Add(material);
            }
        }

        return materials;
    }
}
