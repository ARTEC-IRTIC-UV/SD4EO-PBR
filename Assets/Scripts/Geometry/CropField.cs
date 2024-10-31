using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

// Enum to divide between the different types of crop fields
public enum CropFieldType
{
    AlfalfaOrLucerne,
    Barley,
    FallowAndBareSoil,
    Oats,
    OtherGrainLeguminous,
    Peas,
    Sunflower,
    Vetch,
    Wheat,
    Default
}

// Enum to divide between the different months of the year
public enum Month
{
    January,
    February,
    March,
    April,
    May,
    June,
    July,
    August,
    September,
    October,
    November,
    December
}

// Enum to divide between the different type of textures of the crop fields
public enum CropFieldTexture
{
    SAR,
    Color,
    B08,
    B11
}

[Serializable]
public struct CropFieldOption
{
    [SerializeField] public CropFieldType cropFieldType;
    [SerializeField] public float weight;
}

/**
 * This script contains the different elements that compose a crop field
 * in the application (dimensions, materials and other characteristics)
 */
public class CropField : GeometryElement
{
    private int randomMaterial;
    public Month month;
    public CropFieldType cropFieldType;
    public CropFieldTexture cropFieldTexture;

    override public void Randomize()
    {
        randomMaterial = Random.Range(0, 10);
        color = Color.white;
        CropFieldObject cropFieldObject = (CropFieldObject)geometryObject;
        List<CropFieldOption> cropFieldOptions = cropFieldObject.GetCropFieldOptionsList().GetList();
        SetCropFieldType(SelectWeightedRandomOption(cropFieldOptions));
    }
    
    override public void CreateGeometry(Region reg, Transform parent)
    {
        // Elegimos un número al azar para el material
        randomMaterial = Random.Range(0, 10);
        AssignMaterial(true);
        base.CreateGeometry(reg, parent);
        gameObject.name = "Crop Field";
        gameObject.tag = "Vegetation";
    }

    public void SetMonth(Month m)
    {
        month = m;
    }
    
    public void SetTexture(CropFieldTexture cft)
    {
        cropFieldTexture = cft;
    }
    
    public void AssignMaterial(bool changeMaterial)
    {
        List<Material> materials = MaterialFinder.GetMaterialsInFolder("Assets/Materials/Crop Fields");

        if (materials != null && changeMaterial)
        {
            foreach (var mat in materials)
            {
                string matSuffix;

                if (cropFieldTexture.Equals(CropFieldTexture.SAR))
                    matSuffix = "_SAR";
                else
                    matSuffix = randomMaterial.ToString();
                
                if (mat.name.Equals(cropFieldType + matSuffix))
                {
                    material = mat;
                    break;
                }
            }

            if (material == null && materials.Count > 0)
            {
                material = materials.First();
            }

            Renderer renderer = GetComponent<Renderer>();
            
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;
        }
        
        string stringMonth = StringFunctions.GetMonthNumberAsString(month.ToString());

        if (cropFieldTexture.Equals(CropFieldTexture.SAR))
        {
            SetCropValues.Root data = SetCropValues.GetJsonData();

            Vector2 meanMinMax = new Vector2(data.ranges.minMean, data.ranges.maxMean);

            List<SetCropValues.CropEntry> meanVariance = new List<SetCropValues.CropEntry>();

            switch (cropFieldType)
            {
                case CropFieldType.Barley:
                    meanVariance = data.crops.Barley;
                    break;
                case CropFieldType.Wheat:
                    meanVariance = data.crops.Wheat;
                    break;
                case CropFieldType.Oats:
                    meanVariance = data.crops.Oats;
                    break;
                case CropFieldType.Peas:
                    meanVariance = data.crops.Peas;
                    break;
                case CropFieldType.Sunflower:
                    meanVariance = data.crops.Sunflower;
                    break;
                case CropFieldType.Vetch:
                    meanVariance = data.crops.Vetch;
                    break;
                case CropFieldType.AlfalfaOrLucerne:
                    meanVariance = data.crops.AlfalfaOrLucerne;
                    break;
                case CropFieldType.OtherGrainLeguminous:
                    meanVariance = data.crops.OtherGrainLeguminous;
                    break;
                case CropFieldType.FallowAndBareSoil:
                    meanVariance = data.crops.FallowAndBareSoil;
                    break;
            }

            int currentMonth = Math.Min(int.Parse(stringMonth), meanVariance.Count) - 1;
            currentMonth = Math.Max(currentMonth, 0);
            material.SetFloat("_Mean", (meanVariance[currentMonth]._mean - meanMinMax.x) / (meanMinMax.y - meanMinMax.x));
            material.SetFloat("_Variance", Mathf.Abs(meanVariance[currentMonth]._varia / (meanMinMax.y - meanMinMax.x)));
        }
        else
        {
            string path = "CropField/" + cropFieldType + randomMaterial + "_" + stringMonth + "_" + cropFieldTexture;

            Texture texture = Resources.Load<Texture>(path);

            if (texture != null)
            {
                material.mainTexture = texture;
            }
        }
    }
    
    public CropFieldType SelectWeightedRandomOption(List<CropFieldOption> cropFieldOptions)
    {
        // Calculate the total weight
        float totalWeight = 0f;
        foreach (var option in cropFieldOptions)
        {
            totalWeight += option.weight;
        }

        // Generate a random number between 0 and totalWeight
        float randomNumber = Random.Range(0f, totalWeight);
        
        // Determine which option the random number corresponds to
        float cumulativeWeight = 0f;
        foreach (var option in cropFieldOptions)
        {
            cumulativeWeight += option.weight;
            if (randomNumber < cumulativeWeight)
            {
                return option.cropFieldType;
            }
        }

        // Fallback (should not reach here if weights are set correctly)
        return CropFieldType.Wheat;
    }

    public void SetCropFieldType(CropFieldType cft)
    {
        cropFieldType = cft;
    }

    public CropFieldType GetCropFieldType()
    {
        return cropFieldType;
    }
}

[Serializable]
public class SerializableCropFieldOptionList
{
    [SerializeField] private List<CropFieldOption> cropFieldOptionsList;

    public SerializableCropFieldOptionList(List<CropFieldOption> cropFieldOptions)
    {
        cropFieldOptionsList = cropFieldOptions;
    }

    public List<CropFieldOption> GetList()
    {
        return cropFieldOptionsList;
    }
}