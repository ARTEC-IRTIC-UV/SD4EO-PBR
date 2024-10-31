using System.Collections.Generic;
using UnityEngine;

/*
 * Scriptable object to save information about the characteristics of the objects that
 * are going to represent the buildings with roof
 */
[CreateAssetMenu(fileName = "BuildingRoof", menuName = "ScriptableObjects/Building Roof Object", order = 1)]
public class BuildingRoofObject : GeometryObject
{
    // ATTRIBUTES
    [SerializeField] private Vector2 buildingHeightMinMax = new Vector2(5.0f, 20.0f);
    [SerializeField] private Vector2 roofHeightMultiplyerMinMax = new Vector2(0.5f, 1.5f);
    [SerializeField] private Vector2 roofFlyLengthMinMax = new Vector2(0.5f, 2f);
    [SerializeField] private List<Material> wallMaterials;
    [SerializeField] private Material solarPanelMaterial;
    
    // GETTERS
    public Vector2 GetBuildingHeightMinMax()
    {
        return buildingHeightMinMax;
    }
    
    public Vector2 GetRoofHeightMultiplyerMinMax()
    {
        return roofHeightMultiplyerMinMax;
    }
    
    public Vector2 GetRoofFlyLengthMinMax()
    {
        return roofFlyLengthMinMax;
    }
    
    public List<Material> GetWallMaterials()
    {
        return wallMaterials;
    }

    public Material GetSolarPanelMaterial()
    {
        return solarPanelMaterial;
    }
}