using System.Collections.Generic;
using UnityEngine;

/*
 * Scriptable object to save the general information about the characteristics of the objects that
 * are going to represent the geometry of the cities/crop fields (the rest of scriptable objects
 * will inherit from this script)
 */
[CreateAssetMenu(fileName = "Geometry", menuName = "ScriptableObjects/Geometry Object", order = 1)]
public class GeometryObject : ScriptableObject
{
    // ATTRIBUTES
    [SerializeField] private List<Material> materials;
    [SerializeField] private List<Color> colors;
    [SerializeField] private Vector2 buildingWidthMinMax = new Vector2(8.0f, 12.0f);

    // GETTERS
    public List<Material> GetMaterials()
    {
        return materials;
    }

    public List<Color> GetColors()
    {
        return colors;
    }
    
    public Vector2 GetBuildingWidthMinMax()
    {
        return buildingWidthMinMax;
    }
}