using System;
using System.Collections.Generic;
using UnityEngine;

public class SetCropValues : MonoBehaviour
{
    // This classes contains the information of the crop fields in the json
    [Serializable]
    public class Ranges
    {
        public float minMean;
        public float maxMean;
    }

    [Serializable]
    public class CropEntry
    {
        public float _mean;
        public float _varia;
    }

    [Serializable]
    public class Crops
    {
        public List<CropEntry> Barley;
        public List<CropEntry> Peas;
        public List<CropEntry> Wheat;
        public List<CropEntry> OtherGrainLeguminous;
        public List<CropEntry> Vetch;
        public List<CropEntry> AlfalfaOrLucerne;
        public List<CropEntry> Sunflower;
        public List<CropEntry> Oats;
        public List<CropEntry> FallowAndBareSoil;
    }

    [Serializable]
    public class Root
    {
        public Ranges ranges;
        public Crops crops;
    }
    
    // ATTRIBUTES
    public Material material;
    public Vector2[] meanVariance;
    public Vector2 meanMinMax = new Vector2(0.0f, 1.0f);
    // public float animationSpeed = 1.0f;
    public int currentMonth = 0;
    // Start is called before the first frame update

    void OnValidate()
    {
        SetCurrentMonth(currentMonth);
    }

    // Function to get the data from the json
    public static Root GetJsonData()
    {
        TextAsset jsonTextAsset = Resources.Load<TextAsset>("CropsData");
        if (jsonTextAsset == null)
        {
            Debug.LogError("Failed to load CropsData.json from Resources");
            return null;
        }
        
        Root data = JsonUtility.FromJson<Root>(jsonTextAsset.text);
        return data;
    }

    // Update is called once per frame
    // void Update()
    // {
    //     float t = Time.time * animationSpeed;

    //     currentMonth = (int)t % meanVariance.Length;
    //     float lerp = t % 1.0f;

    //     Vector2 meanVariance0 = meanVariance[currentMonth];
    //     Vector2 meanVariance1 = meanVariance[(currentMonth + 1) % meanVariance.Length];
    //     Vector2 meanVarianceLerp = Vector2.Lerp(meanVariance0, meanVariance1, lerp);

    //     material.SetFloat("_Mean", (meanVarianceLerp.x - meanMinMax.x) / (meanMinMax.y - meanMinMax.x));
    //     material.SetFloat("_Variance", Mathf.Abs(meanVarianceLerp.y / (meanMinMax.y - meanMinMax.x)));
    //     //material.SetFloat("_Seed", t*1000%134);
    // }

    // Function to set the current month of the crop field
    public void SetCurrentMonth(int month)
    {
        currentMonth = Math.Min(month, meanVariance.Length);
        currentMonth = Math.Max(currentMonth, 0);
        material.SetFloat("_Mean", (meanVariance[currentMonth].x - meanMinMax.x) / (meanMinMax.y - meanMinMax.x));
        material.SetFloat("_Variance", Mathf.Abs(meanVariance[currentMonth].y / (meanMinMax.y - meanMinMax.x)));
    }
}