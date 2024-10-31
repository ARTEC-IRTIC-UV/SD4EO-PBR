using System.Collections.Generic;
using UnityEngine;

/**
 * This script contains the different elements that compose a parking plot
 * in the application (dimensions, materials and other characteristics)
 */
public class Parking : GeometryElement
{
    public float vehicleProbability = 0.2f;
    public Material vehicleMaterial;
    public List<Color> vehicleColors;
    public int numberOfCarsPerUnit = 3;

    override public void Randomize()
    {
        ParkingObject parkingObject = geometryObject as ParkingObject;

        material = parkingObject.GetMaterials()[Random.Range(0, parkingObject.GetMaterials().Count)];
        vehicleProbability = Random.Range(parkingObject.GetVehicleProbabilityMinMax().x, parkingObject.GetVehicleProbabilityMinMax().y);
        vehicleMaterial = parkingObject.GetVehicleMaterial();
        vehicleColors = parkingObject.GetVehicleColors();
        
        float r = Random.Range(0.7f, 1.0f);
        color = new Color(r, r, r, 1.0f);
    }

    override public void CreateGeometry(Region reg, Transform parent)
    {
        // Create Base Ground
        base.CreateGeometry(reg, parent);
        gameObject.name = "Parking";
        gameObject.tag = "Industrial Zone";
        
        MeshFilter mf = gameObject.GetComponent<MeshFilter>();
        if(mf)
            GenerateCars(gameObject, mf.sharedMesh);
    }
    
    void GenerateCars(GameObject parent, Mesh mesh)
    {
        ParkingObject parkingObject = geometryObject as ParkingObject;

        Vector3 randomPoint = new Vector3(Random.Range(0.0f, 1000.0f), 0, Random.Range(0.0f, 100.0f));
        // Get the bounds of the mesh
        Bounds rect = mesh.bounds;

        Vector2 tiling = material.GetTextureScale("_MainTex");

        float streetWidth = 1000.0f / tiling.y * 0.001f;
        float vehicleWidth = 1000.0f / tiling.x / numberOfCarsPerUnit * 0.001f;
        Vector3 vehicleSize = new Vector3(1.8f, 1.5f, 4.5f) * 0.001f;
        Vector3 parkingOffset = new Vector3(2.0f, 0, 3f) * 0.001f;

        // Rotate parking Offset
        if (parkingObject != null)
            parkingOffset.x += parkingOffset.z * Mathf.Sin(parkingObject.GetVehicleRotation() * Mathf.Deg2Rad);

        List<GameObject> vehicles = new List<GameObject>();

        for (float x = 0; x < rect.max.x; x += vehicleWidth)
        {
            for (float z = 0, side = 0; z < rect.max.z; z += streetWidth, side = 1 - side)
            {
                Vector3 p = new Vector3(x, 0, z);
                p += parkingOffset;
                p += new Vector3(0, vehicleSize.y / 2, 0);

                //p += new Vector3(0, 0, side * (streetWidth/2.0f - 2*parkingOffset.z));

                // Check if p in the mesh
                if (!GeometryUtils.pointIn2DMesh(p, mesh))
                {
                    continue;
                }

                float random = Mathf.PerlinNoise(p.x * 5000 + randomPoint.x, p.z * 5000 + randomPoint.z);
                if (random > vehicleProbability)
                {
                    continue;
                }

                GameObject vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                
                // Apply color
                Mesh vehicleMesh = GeometryUtils.cloneMesh(vehicle.GetComponent<MeshFilter>().sharedMesh);
                GeometryUtils.addColorToMesh(vehicleMesh, vehicleColors[Random.Range(0, vehicleColors.Count)]);
                vehicle.GetComponent<MeshFilter>().sharedMesh = vehicleMesh;

                vehicle.GetComponent<MeshRenderer>().sharedMaterial = vehicleMaterial;
                vehicle.transform.parent = parent.transform;
                //vehicle.transform.localPosition = p + new Vector3(0, 0, UnityEngine.Random.Range(-0.0005f, 0.0005f));
                vehicle.transform.localPosition = p;
                vehicle.transform.localRotation = Quaternion.Euler(0, parkingObject.GetVehicleRotation() + Random.Range(-5, 5), 0);
                vehicle.transform.localScale = vehicleSize;

                vehicles.Add(vehicle);
            }
        }

        if (vehicles.Count == 0)
        {
            return;
        }

        GameObject merged = GeometryUtils.mergeGameobjects(vehicles);
        merged.name = "Vehicles";
        merged.tag = "Industrial Zone";
        merged.transform.parent = parent.transform;
    }
}