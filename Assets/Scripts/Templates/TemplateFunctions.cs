using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/**
 * This script contains the functions used to fill the regions of the map
 * with the different generated templates of plots
 */
public class TemplateFunctions : MonoBehaviour
{
    // Function to fill the init region with a default floor (for visual and rendering reasons)
    public static void FillInitRegion(Region r)
    {
        RegionsController regionsController = RegionsController.GetInstance();
        r.SetCornerPoints(GeometricFunctions.CalculateCorners(r.GetBorderPoints(), regionsController.GetMaximumInteriorAngle()));
        r.TriangulateRegion();

        r.SetBuildingType(BuildingType.DefaultFloor);
        r.SetTag("Road");
        BuildingFunctions.GenerateBuildingDependingOnType(r, regionsController.GetBuildingsParent(), true, null, true);
    }

    // Function to fill a region 
    public static void FillRegion(Region r)
    {
        RegionsController regionsController = RegionsController.GetInstance();

        if (r.GetZoneType().Equals(ZoneType.FieldCrops))
        {
            r.SetBuildingType(BuildingType.CropField);
            r.TriangulateRegion();
            r.SetCornerPoints(GeometricFunctions.CalculateCorners(r.GetBorderPoints(), regionsController.GetMaximumCornerAngle()));
            r.OrderPointsByLongestSide(regionsController.GetMaximumCornerAngle());

            // Una vez tenemos todos los edificios como regiones, llamamos a una función u otra para generar la geometría
            Region region = BuildingFunctions.GenerateBuildingDependingOnType(r, regionsController.GetBuildingsParent(), true);
            
            if (region != null)
                regionsController.GetFinalBuildingsRegionsList().Add(region);
        }
        else
        {
            r.SetCornerPoints(GeometricFunctions.CalculateCorners(r.GetBorderPoints(), regionsController.GetMaximumCornerAngle()));
            r.OrderPointsByLongestSide(regionsController.GetMaximumCornerAngle());
            
            r.ReviseRegion(regionsController.GetDistanceDifferenceFactor());

            Vector3 pos = r.Centroide();
            GameObject plantilla = SelectBestTemplateForRegion(r);
            GameObject templateInstance;

            if (plantilla != null)
                templateInstance = Instantiate(plantilla, pos, Quaternion.identity, regionsController.transform);
            else
                return;

            Template template = templateInstance.GetComponent<Template>();

            List<Vector3> newPoints = BuildingFunctions.Transformar(template.points.Select(point => point.GetPosition()).ToList(),
                templateInstance.transform);
            for (int i = 0; i < newPoints.Count; i++)
            {
                template.points[i].SetPosition(newPoints[i]);
            }

            Vector3[] originalPoints = new Vector3[template.getBorderPoints().Count];
            template.getBorderPoints().CopyTo(originalPoints);

            template.WrapTemplate(r);
            template.ReassignBoundaryPoints(r);

            foreach (var p in template.points)
            {
                if (p.GetPointType() == PointType.InnerVertex)
                {
                    p.SetPosition(template.RepositionPoint(p.GetPosition(), originalPoints.ToList(),
                        template.getBorderPoints()));
                }
            }

            r.SetTemplate(template);
            r.SetBuildingType(BuildingType.DefaultFloor);

            // Creamos el suelo de la parcela
            BuildingFunctions.GenerateBuildingDependingOnType(r, regionsController.GetBuildingsParent(), true);

            // Una vez hemos pegado la template, debemos crear las nuevas regiones para generar edificios individuales
            List<Region> regions = template.getBuildingsRegions(r.GetZoneType());

            // Hacemos que la altura varíe un poco y creamos las nuevas regiones
            foreach (var reg in regions)
            {
                reg.TriangulateRegion();
                reg.SetCornerPoints(GeometricFunctions.CalculateCorners(reg.GetBorderPoints(), regionsController.GetMaximumCornerAngle()));
                reg.OrderPointsByLongestSide(regionsController.GetMaximumCornerAngle());

                // Una vez tenemos todos los edificios como regiones, llamamos a una función u otra para generar la geometría
                Region region = BuildingFunctions.GenerateBuildingDependingOnType(reg, regionsController.GetBuildingsParent(), true);
                
                if (region != null)
                    regionsController.GetFinalBuildingsRegionsList().Add(region);

                DestroyImmediate(templateInstance);
            }
        }
    }

    // Function to select the best template for a region (according to the corners and the type of the region)
    private static GameObject SelectBestTemplateForRegion(Region r)
    {
        RegionsController regionsController = RegionsController.GetInstance();
        GameObject go = null;

        // We obtain the prefab of the type of zone
        ZoneType zoneType = r.GetZoneType();

        Transform prefab = null;
        switch (zoneType)
        {
            case ZoneType.Downtown:
                prefab = regionsController.GetDowntownTemplates().transform;
                break;
            case ZoneType.ResidentialArea:
                prefab = regionsController.GetResidentialTemplates().transform;
                break;
            case ZoneType.IndustrialArea:
                prefab = regionsController.GetIndustrialTemplates().transform;
                break;
            case ZoneType.FieldCrops:
                prefab = regionsController.GetCropFieldsTemplates().transform;
                break;
        }

        // We compute the number of corners of the region
        //r.SetCornerPoints(GeometricFunctions.CalculateCorners(r.GetBorderPoints(), maximumCornerAngle));
        int corners = r.GetCornerPoints().Count;

        // We add the different templates that fulfill the conditions to a list
        List<Template> plantillas = new List<Template>();

        if (prefab != null)
        {
            for (int i = 0; i < prefab.childCount; i++)
            {
                Template template = prefab.GetChild(i).GetComponent<Template>();

                int esquinas = template.GetCornerPoints();

                if (esquinas == corners)
                    plantillas.Add(template);
            }
        }

        // We select a random template between the possibilities
        if (plantillas.Count > 0)
            go = plantillas[Random.Range(0, plantillas.Count)].gameObject;

        return go;
    }
}