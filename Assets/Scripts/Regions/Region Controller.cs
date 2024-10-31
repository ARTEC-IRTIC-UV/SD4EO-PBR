using System.Collections.Generic;
using UnityEngine;
using VInspector;
using Random = UnityEngine.Random;

public class RegionController : MonoBehaviour
{
    [SerializeField] private BuildingType buildingType;
    private ScriptableObject scriptableObject;
    private RegionsController regionsController = RegionsController.GetInstance();
    private BuildingType componentBuildingType;
    private Region region;

    [Button("Change region type")]
    public void ChangeRegionType()
    {
        // If we change to a different region type, we need to clean the actual scripts of the gameobject
        bool sameRegionType = ManageScriptsAndComponents();

        if (!sameRegionType)
        {
            GeometryElement geometryElement = null;

            switch (buildingType)
            {
                case BuildingType.BuildingRoof:
                    geometryElement = gameObject.AddComponent<BuildingRoof>();
                    List<GeometryObject> buildingRoofOptions = new List<GeometryObject>();
                    switch (region.GetZoneType())
                    {
                        case ZoneType.Downtown:
                            buildingRoofOptions = regionsController.GetDowntownBuildingRoofOptions();
                            break;
                        case ZoneType.ResidentialArea:
                            buildingRoofOptions = regionsController.GetResidentialBuildingRoofOptions();
                            break;
                        case ZoneType.IndustrialArea:
                            buildingRoofOptions = regionsController.GetIndustrialBuildingRoofOptions();
                            break;
                    }
                    geometryElement.geometryObject = buildingRoofOptions[Random.Range(0, buildingRoofOptions.Count)];
                    break;
                case BuildingType.BuildingCornice:
                    geometryElement = gameObject.AddComponent<BuildingCornice>();
                    List<GeometryObject> buildingCorniceOptions = new List<GeometryObject>();
                    switch (region.GetZoneType())
                    {
                        case ZoneType.Downtown:
                            buildingCorniceOptions = regionsController.GetDowntownBuildingCorniceOptions();
                            break;
                        case ZoneType.ResidentialArea:
                            buildingCorniceOptions = regionsController.GetResidentialBuildingCorniceOptions();
                            break;
                        case ZoneType.IndustrialArea:
                            buildingCorniceOptions = regionsController.GetIndustrialBuildingCorniceOptions();
                            break;
                    }
                    geometryElement.geometryObject = buildingCorniceOptions[Random.Range(0, buildingCorniceOptions.Count)];
                    break;
                case BuildingType.Parking:
                    geometryElement = gameObject.AddComponent<Parking>();
                    List<GeometryObject> parkingOptions = regionsController.GetParkingOptions();
                    geometryElement.geometryObject = parkingOptions[Random.Range(0, parkingOptions.Count)];
                    break;
                case BuildingType.Vegetation:
                    geometryElement = gameObject.AddComponent<Vegetation>();
                    List<GeometryObject> vegetationOptions = regionsController.GetVegetationOptions();
                    geometryElement.geometryObject = vegetationOptions[Random.Range(0, vegetationOptions.Count)];
                    break;
                case BuildingType.CropField:
                    geometryElement = gameObject.AddComponent<CropField>();
                    List<GeometryObject> cropFieldOptions = regionsController.GetCropFieldOptions();
                    geometryElement.geometryObject = cropFieldOptions[Random.Range(0, cropFieldOptions.Count)];
                    break;
                case BuildingType.Water:
                    break;
                case BuildingType.Street:
                    break;
                default:
                    break;
            }

            if (geometryElement != null)
            {
                geometryElement.Randomize();
                geometryElement.CreateGeometry(region, gameObject.transform);
            }
            
            componentBuildingType = buildingType;
        }
    }
    
    public bool ManageScriptsAndComponents()
    {
        bool sameType = false;
        Component[] allComponents = gameObject.GetComponents<Component>();

        // Primero comprobamos que se haya cambiado el tipo de región
        if (componentBuildingType == buildingType)
            sameType = true;
        
        // Si se ha cambiado, borramos, si no, lo dejamos como está
        if (!sameType)
        {
            foreach (var c in allComponents)
            {
                if (!(c is Transform) && c != this)
                {
                    DestroyImmediate(c);
                }
            }
        }

        return sameType;
    }

    public Region GetRegion()
    {
        return region;
    }

    public void SetRegion(Region region)
    {
        this.region = region;
    }

    public BuildingType GetBuildingType()
    {
        return buildingType;
    }

    public void SetBuildingType(BuildingType buildingType)
    {
        this.buildingType = buildingType;
    }
    
    public BuildingType GetComponentBuildingType()
    {
        return componentBuildingType;
    }
    
    public void SetComponentBuildingType(BuildingType componentBuildingType)
    {
        this.componentBuildingType = componentBuildingType;
    }
}