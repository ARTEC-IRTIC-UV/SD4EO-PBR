using System.Collections.Generic;
using UnityEngine;

/*
 * Scriptable object to save information about the characteristics of the objects that
 * are going to represent the terrains of vegetation
 */
[CreateAssetMenu(fileName = "Vegetation", menuName = "ScriptableObjects/Vegetation Object")]
public class VegetationObject : GeometryObject
{
    // ATTRIBUTES
    [SerializeField] private Vector2 vegetationProbabilityMinMax = new Vector2(0.2f, 0.8f);
    [SerializeField] private Vector2 vegetationHeightMinMax = new Vector2(1.5f, 4.0f);
    [SerializeField] private Vector2 vegetationWidthMinMax = new Vector2(1.5f, 4.0f);
    [SerializeField] private List<Color> vegetationColors;
    [SerializeField] private Material vegetationMaterial;
    
    // GETTERS
    public Vector2 GetVegetationProbabilityMinMax()
    {
        return vegetationProbabilityMinMax;
    }
    
    public Vector2 GetVegetationHeightMinMax()
    {
        return vegetationHeightMinMax;
    }
    
    public Vector2 GetVegetationWidthMinMax()
    {
        return vegetationWidthMinMax;
    }

    public List<Color> GetVegetationColors()
    {
        return vegetationColors;
    }

    public Material GetVegetationMaterial()
    {
        return vegetationMaterial;
    }
}