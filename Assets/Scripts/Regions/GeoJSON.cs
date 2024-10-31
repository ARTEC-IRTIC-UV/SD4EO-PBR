using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public enum BuildingType
{
    BuildingRoof,
    BuildingCornice,
    Vegetation,
    Parking,
    Water,
    Train,
    Street,
    CropField,
    DefaultFloor,
    IndustrialFloor,
    ResidentialFloor
}

[Serializable]
public class Coordinates
{
    [SerializeField] public float[] coordinates;

    public Coordinates(float[] coordinates)
    {
        this.coordinates = coordinates;
    }

    public float[] GetCoordinates()
    {
        return coordinates;
    }
}

[Serializable]
public class Geometry
{
    [SerializeField] public string type;
    [SerializeField] public List<Coordinates> coordinates;

    public Geometry(string type, List<Coordinates> coordinates)
    {
        this.type = type;
        this.coordinates = coordinates;
    }
    
    // Custom method to convert to GeoJSON format
    public string ToGeoJson()
    {
        string geoJson = "[ [";

        for (int i = 0; i < coordinates.Count; i++)
        {
            geoJson += "[" + coordinates[i].coordinates[0].ToString(CultureInfo.InvariantCulture) + "," + coordinates[i].coordinates[1].ToString(CultureInfo.InvariantCulture) + "]";
            if (i < coordinates.Count - 1)
                geoJson += ",";
        }

        geoJson += "] ]";

        return geoJson;
    }
}

[Serializable]
public class Properties
{
    [SerializeField] public string type;

    public Properties(string type)
    {
        this.type = type;
    }
}

[Serializable]
public class Feature
{
    [SerializeField] public string type;
    [SerializeField] public Geometry geometry;
    [SerializeField] public Properties properties;

    public Feature(Geometry geometry, Properties properties)
    {
        type = "Feature";
        this.geometry = geometry;
        this.properties = properties;
    }

    // Method to convert Feature to GeoJSON format
    public string ToGeoJson()
    {
        return "{\"type\":\"" + type + "\",\"geometry\":{\"type\":\"" + geometry.type + "\",\"coordinates\":" + geometry.ToGeoJson() + "},\"properties\":{\"type\":\"" + properties.type + "\"}}";
    }
}

[Serializable]
public class FeatureCollection
{
    [SerializeField] public string type;
    [SerializeField] public List<Feature> features;

    public FeatureCollection(List<Feature> features)
    {
        type = "FeatureCollection";
        this.features = features;
    }

    // Method to convert FeatureCollection to GeoJSON format
    public string ToGeoJson()
    {
        string geoJson = "{\"type\":\"" + type + "\",\"features\":[";

        for (int i = 0; i < features.Count; i++)
        {
            geoJson += features[i].ToGeoJson();
            if (i < features.Count - 1)
                geoJson += ",";
        }

        geoJson += "]}";

        return geoJson;
    }
}

public class GeoJSON : MonoBehaviour
{
    /*
     * ################################# FUNCIONES DE GUARDADO DE INFORMACIÓN EN GEOJSON ######################################
     */
    
    // Function to save the regions' information recorded to the json file
    public static void SaveToJson(List<Region> regions, string path, string name)
    {
        List<Feature> features = new List<Feature>();

        foreach (var region in regions)
        {
            List<float[]> coord = GeometricFunctions.ConvertVector3ToFloat2(region.GetBorderPoints());
            List<Coordinates> coordinates = new List<Coordinates>();
            foreach (var c in coord)
            {
                if (!float.IsNaN(c[0]) && !float.IsNaN(c[1]))
                    coordinates.Add(new Coordinates(c));
            }
            
            if (coordinates.First() != coordinates.Last())
                coordinates.Add(coordinates.First());

            if (coordinates.Count < 4)
                continue;
        
            Geometry geometry = new Geometry("Polygon", coordinates);
            Properties properties;
            if (region.GetBuildingType().Equals(BuildingType.CropField))
                properties = new Properties(region.GetCropFieldType() + "");
            else if (region.GetBuildingType().Equals(BuildingType.BuildingRoof))
            {
                if (region.GetZoneType().Equals(ZoneType.IndustrialArea))
                    properties = new Properties("Non_Residential_Building_Roof");
                else
                    properties = new Properties("Residential_Building_Roof");
            }
            else if (region.GetBuildingType().Equals(BuildingType.BuildingCornice))
            {
                if (region.GetZoneType().Equals(ZoneType.IndustrialArea))
                    properties = new Properties("Non_Residential_Building_Cornice");
                else
                    properties = new Properties("Residential_Building_Cornice");
            }
            else
                properties = new Properties(region.GetBuildingType() + "");
                
            Feature f = new Feature(geometry, properties);
            features.Add(f);
        }
        
        FeatureCollection geojson = new FeatureCollection(features);
        
        string json = geojson.ToGeoJson();

        path += "/" + name + ".json";

        // Guardamos el JSON en el archivo
        File.WriteAllText(path, json);
    }
}