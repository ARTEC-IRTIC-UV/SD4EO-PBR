using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

/**
 * This script contains some functions without classification that have helped
 * to the development of the application
 */
public class OtherFunctions : MonoBehaviour
{
    // Function to get a random element between the options of an enum
    public static T GetRandomEnumElement<T>()
    {
        Array values = Enum.GetValues(typeof(T));
        int randomIndex = Random.Range(0, values.Length);
        return (T)values.GetValue(randomIndex);
    }
    
    // Function to check if the children of a gameobject is active or not
    public static bool CheckIfChildrenSelected(GameObject g)
    {
        if (Selection.activeGameObject == g)
        {
            return true;
        }

        Transform t = g.transform;
        for (int i = 0; i < t.childCount; i++)
        {
            if (t.GetChild(i).gameObject == Selection.activeGameObject)
            {
                return true;
            }
        }
        
        return false;
    }
    
    // Function to change the month of a crop field
    public static void ChangeCropFieldsMonth(Month m, bool changeMaterial)
    {
        CropField[] cropFields = FindObjectsOfType<CropField>();

        foreach (var crop in cropFields)
        {
            crop.SetMonth(m);
            crop.SetTexture(RegionsController.GetInstance().GetCropFieldTexture());
            crop.AssignMaterial(changeMaterial);
        }
    }
}
