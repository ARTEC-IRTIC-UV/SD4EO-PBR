using System;
using System.Collections.Generic;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

/*
 * This script divides a buildings' plot into different individual buildings
 */
public class MultipleBuildingGenerator : MonoBehaviour
{
    public GeometryObject geometryObject;
    private RegionsController regionsController = RegionsController.GetInstance();

    // Function to divide a buildings' region into individual buildings
    public void DivideRegionAndGenerateBuildings(Region region, GeometryObject geomObject)
    {
        // Create the gameobject
        GameObject go = gameObject;
        go.name = "Multiple Buildings Parent";
        go.transform.parent = regionsController.GetBuildingsParent();
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity; 
        go.transform.localScale = Vector3.one;

        // We destroy the RegionController component of this gameobject
        RegionController regionController = go.GetComponent<RegionController>();
        
        if (regionController != null)
            DestroyImmediate(regionController);
        
        geometryObject = geomObject;
        
        if (region.GetCornerPoints().Count == 5)
            region.ReviseRegion(regionsController.GetDistanceDifferenceFactor());

        if (region.GetCornerPoints().Count == 4)
        {
            List<Region> regions = GenerateRegions(region);
        
            foreach (var reg in regions)
            {
                reg.SetTag(region.GetTag());
                BuildingFunctions.GenerateBuildingDependingOnType(reg, go.transform, false);
            }
        }
        else
        {
            region.SetBuildingType(BuildingType.Vegetation);
            region.SetTag("Vegetation");
            BuildingFunctions.GenerateBuildingDependingOnType(region, go.transform, false);
        }
    }

    // Function to generate the different regions of a region for buildings
    public List<Region> GenerateRegions(Region region)
    {
        List<Region> regions = new List<Region>();

        // First, we compute the measure of the long side and the building width that we
        // want, in order to know how many regions we have to create
        Vector2 buildingWidth = geometryObject.GetBuildingWidthMinMax();
        float width = Random.Range(buildingWidth.x, buildingWidth.y) * 0.001f;
        int numberOfRegions = Mathf.FloorToInt(region.GetLongestSide() / ((region.GetShortestSide() + width) / 2f));
        
        // If a region is so small that any building can be created, we set this region as vegetation
        if (numberOfRegions == 0)
        {
            Region veg = new Region();
            veg.SetBorderPoints(region.GetBorderPoints());
            veg.SetCornerPoints(region.GetCornerPoints());
            veg.SetZoneType(region.GetZoneType());
            veg.SetBuildingType(BuildingType.Vegetation);
            veg.TriangulateRegion();
            //Debug.DrawLine(region.Centroide(), region.Centroide() + Vector3.up * 0.2f, Color.yellow, 10f);
            regions.Add(veg);
        }
        else
        {
            // When we know the number of regions, we divide the plot region in that number of regions (one point more than regions)
            List<Vector3> regionPoints = GeneratePoints(region.GetCornerPoints(), region.GetBorderPoints(), numberOfRegions + 1);
            int n = regionPoints.Count;
        
            // We can create know the new regions, each one with 4 points (rectangular buildings)
            for (int i = 0; i < numberOfRegions; i++)
            {
                // First, we save the borders of the region
                List<Vector3> borders = new List<Vector3>();
                List<int> cornersIndices = new List<int>();
                borders.Add(regionPoints[i]);
                borders.Add(regionPoints[i+1]);
                borders.Add(regionPoints[n-i-2]);
                borders.Add(regionPoints[n-i-1]);
                cornersIndices.Add(0);
                cornersIndices.Add(1);
                cornersIndices.Add(2);
                cornersIndices.Add(3);

                // We create the new region and set its elements
                Region r = new Region();
                r.SetBorderPoints(borders);
                r.SetCornerPoints(cornersIndices);
                r.SetZoneType(region.GetZoneType());
                r.SetBuildingType(region.GetBuildingType());
                r.TriangulateRegion();
                regions.Add(r);
            }
        }
        
        return regions;
    }
    
    // Function to generate the points to divide the plot into different buildings
    public static List<Vector3> GeneratePoints(List<int> corners, List<Vector3> borderPoints, int numberOfPoints)
    {
        List<Vector3> points = new List<Vector3>();
        List<Vector3> firstSide = new List<Vector3>();
        List<Vector3> secondSide = new List<Vector3>();

        for (int i = 0; i < borderPoints.Count; i++)
        {
            if (i >= corners[0] && i <= corners[1])
                firstSide.Add(borderPoints[i]);
            
            if (i >= corners[2] && i <= corners[3])
                secondSide.Add(borderPoints[i]);
        }

        if (numberOfPoints < 2)
        {
            throw new ArgumentException("Number of points should be at least 2");
        }

        for (int i = 0; i < numberOfPoints; i++)
        {
            float t = (float)i / (numberOfPoints - 1);
            Vector3 point = GeometricFunctions.InterpolatePolyline(t, firstSide);
            points.Add(point);
        }
        
        for (int i = 0; i < numberOfPoints; i++)
        {
            float t = (float)i / (numberOfPoints - 1);
            Vector3 point = GeometricFunctions.InterpolatePolyline(t, secondSide);
            points.Add(point);
        }
        
        return points;
    }
}