using UnityEngine;

/*
 * Scriptable object to save information about the characteristics of the objects that
 * are going to represent the crop fields
 */
[CreateAssetMenu(fileName = "Crop Field", menuName = "ScriptableObjects/Crop Field Object")]
public class CropFieldObject : GeometryObject
{
    // ATTRIBUTES
    [SerializeField] private SerializableCropFieldOptionList cropFieldOptions;

    // GETTERS
    public SerializableCropFieldOptionList GetCropFieldOptionsList()
    {
        return cropFieldOptions;
    }
}