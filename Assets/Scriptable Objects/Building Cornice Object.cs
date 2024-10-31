using System.Collections.Generic;
using UnityEngine;

/*
 * Scriptable object to save information about the characteristics of the objects that
 * are going to represent the buildings with cornice
 */
[CreateAssetMenu(fileName = "BuildingCornice", menuName = "ScriptableObjects/Building Cornice Object", order = 1)]
public class BuildingCorniceObject : GeometryObject
{
    // ATTRIBUTES
    [SerializeField] private Vector2 buildingHeightMinMax = new Vector2(5.0f, 20.0f);
    [SerializeField] private Vector2 corniceHeightMinMax = new Vector2(0.5f, 1.5f);
    [SerializeField] private Vector2 corniceWidthMinMax = new Vector2(0.3f, 1f);
    [SerializeField] private List<Material> wallMaterials;
    [SerializeField][Range(0f, 100f)] private float solarPanelRoofProbability  = 50f;
    [SerializeField][Range(20f, 100f)] private float solarPanelDensity = 50f;
    [SerializeField]private Material solarPanelRoofMaterial;
    
    // GETTERS
    public Vector2 GetBuildingHeightMinMax()
    {
        return buildingHeightMinMax;
    }

    public Vector2 GetCorniceHeightMinMax()
    {
        return corniceHeightMinMax;
    }
    
    public Vector2 GetCorniceWidthMinMax()
    {
        return corniceWidthMinMax;
    }
    
    public List<Material> GetWallMaterials()
    {
        return wallMaterials;
    }
    
    public float GetSolarPanelProbability()
    {
        return solarPanelRoofProbability;
    }
    
    public float GetSolarPanelDensity()
    {
        return solarPanelDensity * 0.01f;
    }
    
    public Material GetSolarPanelMaterial()
    {
        return solarPanelRoofMaterial;
    }
}