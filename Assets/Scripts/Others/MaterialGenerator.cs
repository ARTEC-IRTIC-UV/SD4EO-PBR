using UnityEditor;
using UnityEngine;
using VInspector;

public class MaterialGenerator : MonoBehaviour
{
    [SerializeField] private Material materialToCopy;
    [SerializeField] private int numberOfCopies;
    
    [Button("Generate materials")]
    private void GenerateMaterials()
    {
        // Prompt the user to select a folder
        string folderPath = EditorUtility.OpenFolderPanel("Select Folder to Save Materials", "Assets", "");
        
        // Ensure the selected path is within the Assets folder
        if (!folderPath.StartsWith(Application.dataPath))
        {
            Debug.LogError("The selected folder must be within the Assets folder.");
            return;
        }

        // Convert the folder path to a relative path
        folderPath = "Assets" + folderPath.Substring(Application.dataPath.Length);

        // Find an existing material in the project (example: using a known material path)
        if (materialToCopy == null)
        {
            Debug.LogError("Reference material not found. Please check the path.");
            return;
        }

        for (int i = 0; i < numberOfCopies; i++) // Example to create 10 materials
        {
            // Create a new material based on the reference material
            Material newMaterial = new Material(materialToCopy);

            // Set material properties (example: random color)
            newMaterial.color = Color.white;

            // Create a unique name for the material based on the reference material's name
            string materialName = materialToCopy.name + i;

            // Save the material as an asset in the specified folder
            AssetDatabase.CreateAsset(newMaterial, $"{folderPath}/{materialName}.mat");
        }

        // Ensure all assets are saved and updated
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Materials generated and saved in {folderPath}");
    }
}
