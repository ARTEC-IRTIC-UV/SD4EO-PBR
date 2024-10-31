using UnityEditor;
using UnityEngine;

/*
 * This script contains a function that set all the textures of the project as Read & Write
 */
public class TextureReadWriteSetter : EditorWindow
{
    [MenuItem("Tools/Set All Textures Read&Write")]
    public static void SetTexturesReadWrite()
    {
        string[] textureGUIDs = AssetDatabase.FindAssets("t:Texture");

        foreach (string textureGUID in textureGUIDs)
        {
            string texturePath = AssetDatabase.GUIDToAssetPath(textureGUID);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.isReadable = true;
                AssetDatabase.ImportAsset(texturePath);
                Debug.Log("Set Read/Write for texture: " + texturePath);
            }
        }

        Debug.Log("Finished setting Read/Write for all textures.");
    }
}