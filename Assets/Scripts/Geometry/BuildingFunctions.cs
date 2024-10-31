using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Random = UnityEngine.Random;

public class BuildingFunctions : MonoBehaviour
{
    public static void GenerateBuilding(Region region, GameObject go, float height, Transform parent, float displacement = 0f, Material material = null, List<Vector2> forcedUVs = null)
    {
        // Create the base mesh
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        if (forcedUVs != null)
            uv = forcedUVs;
        List<int> trianglesRoof = new List<int>();
        List<int> trianglesSides = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        // Add vertices from the points along with UV coordinates
        var currentRegionMesh = region.getTriangulatedMesh();
        var regionMeshVertices = currentRegionMesh.vertices;
        var regionMeshTriangles = currentRegionMesh.triangles;

        Vector4 BB = TestAlgorithmsHelpMethods.CalculateBoundingBox(regionMeshVertices.ToList());
        Vector2 minBB = new Vector2(BB.x, BB.y);
        Vector2 maxBB = new Vector2(BB.z, BB.w);

        for (var i = 0; i < regionMeshTriangles.Length; i += 3)
        {
            //Los triangulos de la tapa inferior deben ir del revés para apuntar hacia abajo
            var tr3 = regionMeshTriangles[(i + 2) % regionMeshTriangles.Length];
            trianglesRoof.Add(tr3);
            var tr1 = regionMeshTriangles[i];
            trianglesRoof.Add(tr1);
            var tr2 = regionMeshTriangles[(i + 1) % regionMeshTriangles.Length];
            trianglesRoof.Add(tr2);
        }

        float transformation = 10f;

        //Sacamos el multiplicador de coordenadas de textura
        float nX = Math.Abs(maxBB.x - minBB.x) * transformation;
        float nY = Math.Abs(maxBB.y - minBB.y) * transformation;

        foreach (Vector3 point in regionMeshVertices)
        {
            vertices.Add(new Vector3(point.x, height - displacement, point.z));
            normals.Add(Vector3.up);
            if (forcedUVs == null)
            {
                uv.Add(new Vector2(
                    Mathf.InverseLerp(minBB.x, maxBB.x, point.x) * nX,
                    Mathf.InverseLerp(minBB.y, maxBB.y, point.z) * nY
                ));
            }
        }

        List<Vector3> extrudedVertices = new List<Vector3>();

        // Define triangles for the sides of the building

        if (height > 0f)
        {
            extrudedVertices.Clear();
            for (var i = 0; i < regionMeshVertices.Length; i++)
            {
                var point = regionMeshVertices[i];
                var nextPoint = regionMeshVertices[(i + 1) % regionMeshVertices.Length];

                Vector3 up = new Vector3(point.x, height, point.z);
                Vector3 right = new Vector3(nextPoint.x, 0f, nextPoint.z);
                Vector3 rightUp = new Vector3(nextPoint.x, height, nextPoint.z);
                // Primero añadimos los puntos como nuevos vértices con sus coordenadas de textura y normales (en ambas tapas)
                extrudedVertices.Add(point);
                extrudedVertices.Add(up);
                extrudedVertices.Add(right);
                extrudedVertices.Add(rightUp);

                //Sacamos el multiplicador de coordenadas de textura

                nX = (Vector3.Distance(point, right) * transformation);
                nY = (height * transformation);

                //nX = Mathf.Max(1, nX);
                //nY = Mathf.Max(1, nY);
                uv.Add(new Vector2(0f, 0f));
                uv.Add(new Vector2(0f, 1f * nY));
                uv.Add(new Vector2(1f * nX, 0f));
                uv.Add(new Vector2(1f * nX, 1f * nY));
                Vector3 n1 = right - point;
                Vector3 n2 = up - point;
                Vector3 normal = Vector3.Cross(n1, n2);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
            }

            int topBaseStartIndex = vertices.Count + 0;
            foreach (var extruded in extrudedVertices)
            {
                vertices.Add(extruded);
            }

            //Para cada nuevo vértice, creamos los triángulos
            for (int i = topBaseStartIndex; i < vertices.Count - 3; i += 4)
            {
                trianglesSides.Add(i);
                trianglesSides.Add(i + 1);
                trianglesSides.Add(i + 2);

                trianglesSides.Add(i + 1);
                trianglesSides.Add(i + 3);
                trianglesSides.Add(i + 2);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uv.ToArray();

        if (height > 0f)
            mesh.subMeshCount = 2;

        mesh.SetTriangles(trianglesRoof.ToArray(), 0);

        if (height > 0f)
            mesh.SetTriangles(trianglesSides.ToArray(), 1);

        // Create GameObject and assign mesh
        GameObject building = go;
        building.transform.position = new Vector3(); // Set position
        building.transform.parent = parent; // Set parent

        MeshFilter meshFilter = building.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        // Add MeshRenderer component for visibility
        building.AddComponent<MeshRenderer>();

        // Select a random material from the list and apply it to the build
        List<Material> roofMaterials = new List<Material>();
        List<Material> facadeMaterials = new List<Material>();
        if (material == null)
        {
            Material roofMaterial = region.GetRoofMaterial();
            Material facadeMaterial = region.GetFacadeMaterial();
            Renderer renderer = building.GetComponent<Renderer>();

            if (height == 0f)
            {
                if (roofMaterial == null && roofMaterials != null && roofMaterials.Count > 0)
                {
                    int randomIndex = Random.Range(0, roofMaterials.Count);
                    renderer.material = roofMaterials[randomIndex];
                }
                else if (roofMaterial != null)
                    renderer.material = roofMaterial;
                else
                    Debug.LogWarning("No materials assigned to the BuildGenerator script.");
            }
            else
            {
                Material roof = null;
                Material facade = null;

                if (roofMaterial == null && roofMaterials != null && roofMaterials.Count > 0)
                {
                    int randomIndex = Random.Range(0, roofMaterials.Count);
                    roof = roofMaterials[randomIndex];
                }
                else if (roofMaterial != null)
                    roof = roofMaterial;

                if (facadeMaterial == null && facadeMaterials != null && facadeMaterials.Count > 0)
                {
                    int randomIndex = Random.Range(0, facadeMaterials.Count);
                    facade = facadeMaterials[randomIndex];
                }
                else if (facadeMaterial != null)
                    facade = facadeMaterial;

                if (roof != null && facade != null)
                {
                    renderer.materials = new Material[] { roof, facade };
                }
                else
                {
                    Debug.LogWarning("No materials assigned to the BuildGenerator script.");
                }
            }
        }
        else
        {
            Renderer renderer = building.GetComponent<Renderer>();
            renderer.material = material;
        }
    }
    
    public static Region GenerateBuildingDependingOnType(Region reg, Transform parent, bool buildingsParent, List<Vector2> forcedUVs = null, bool initialReg = false, bool vegTrees = true)
    {
        RegionsController regionsController = RegionsController.GetInstance();
        // Asignamos la clase general Region Controller
        GameObject go = new GameObject();
        RegionController regController = go.AddComponent<RegionController>();
        regController.SetRegion(reg);
        regController.SetBuildingType(reg.GetBuildingType());
        regController.SetComponentBuildingType(reg.GetBuildingType());

        // Una vez tenemos todos los edificios como regiones, llamamos a una función u otra para generar la geometría
        switch (reg.GetBuildingType())
        {
            case BuildingType.BuildingRoof:
                List<GeometryObject> buildingRoofOptions = new List<GeometryObject>();
                switch (reg.GetZoneType())
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

                GeometryObject geometryRoofObject = buildingRoofOptions[Random.Range(0, buildingRoofOptions.Count)];
                if (buildingsParent && !reg.GetZoneType().Equals(ZoneType.IndustrialArea))
                {
                    MultipleBuildingGenerator multipleBuildingGenerator = go.AddComponent<MultipleBuildingGenerator>();
                    multipleBuildingGenerator.DivideRegionAndGenerateBuildings(reg, geometryRoofObject);
                }
                else
                {
                    GeometryElement newBuildingRoof = go.AddComponent<BuildingRoof>();
                    newBuildingRoof.geometryObject = geometryRoofObject;
                    newBuildingRoof.Randomize();
                    
                    if (reg.GetZoneType().Equals(ZoneType.IndustrialArea))
                        reg.SetTag("Non Residential Building");
                    else
                        reg.SetTag("Residential Building");
                    
                    newBuildingRoof.CreateGeometry(reg, parent);
                }
                break;
            case BuildingType.BuildingCornice:
                List<GeometryObject> buildingCorniceOptions = new List<GeometryObject>();
                switch (reg.GetZoneType())
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

                GeometryObject geometryCorniceObject =
                    buildingCorniceOptions[Random.Range(0, buildingCorniceOptions.Count)];
                if (buildingsParent && !reg.GetZoneType().Equals(ZoneType.IndustrialArea))
                {
                    MultipleBuildingGenerator multipleBuildingGenerator = go.AddComponent<MultipleBuildingGenerator>();
                    multipleBuildingGenerator.DivideRegionAndGenerateBuildings(reg, geometryCorniceObject);
                }
                else
                {
                    BuildingCornice newBuildingCornice = go.AddComponent<BuildingCornice>();
                    newBuildingCornice.geometryObject = geometryCorniceObject;
                    newBuildingCornice.Randomize();
                    
                    if (reg.GetZoneType().Equals(ZoneType.IndustrialArea))
                        reg.SetTag("Non Residential Building");
                    else
                        reg.SetTag("Residential Building");
                    
                    newBuildingCornice.CreateGeometry(reg, parent);
                }

                break;
            case BuildingType.Parking:
                GeometryElement newParking = go.AddComponent<Parking>();
                List<GeometryObject> parkingOptions = regionsController.GetParkingOptions();
                newParking.geometryObject = parkingOptions[Random.Range(0, parkingOptions.Count)];
                newParking.Randomize();
                reg.SetTag("Industrial Zone");
                newParking.CreateGeometry(reg, parent);
                break;
            case BuildingType.Vegetation:
                GeometryElement newVegetation = go.AddComponent<Vegetation>();
                List<GeometryObject> vegetationOptions = regionsController.GetVegetationOptions();
                newVegetation.geometryObject = vegetationOptions[Random.Range(0, vegetationOptions.Count)];
                newVegetation.Randomize();
                reg.SetTag("Vegetation");
                newVegetation.GetComponent<Vegetation>().SetGenerateTrees(vegTrees);
                newVegetation.CreateGeometry(reg, parent);
                break;
            case BuildingType.Water:
                reg.SetRoofMaterial(regionsController.GetWaterMaterial());
                go.name = "Water";
                GenerateBuilding(reg, go, 0f, parent);
                break;
            case BuildingType.Train:
                reg.SetRoofMaterial(regionsController.GetTrainMaterial());
                go.name = "Railway";
                GenerateBuilding(reg, go, 0f, parent, forcedUVs: forcedUVs);
                break;
            case BuildingType.Street:
                reg.SetRoofMaterial(regionsController.GetStreetMaterial());
                go.name = "Street";
                GenerateBuilding(reg, go, 0f, parent, forcedUVs: forcedUVs);
                break;
            case BuildingType.DefaultFloor:
                if (initialReg)
                {
                    reg.SetRoofMaterial(regionsController.GetDefaultFloorMaterial());
                    go.name = "Default Floor";
                    GenerateBuilding(reg, go, 0f, parent, 0.0025f);
                }
                else
                {
                    switch (reg.GetZoneType())
                    {
                        case ZoneType.Downtown:
                        case ZoneType.ResidentialArea:
                            reg.SetRoofMaterial(regionsController.GetResidentialFloorMaterial());
                            go.name = "Residential Floor";
                            reg.SetTag("Residential Zone");
                            GenerateBuilding(reg, go, 0f, parent, 0.00125f);
                            break;
                        case ZoneType.IndustrialArea:
                            reg.SetRoofMaterial(regionsController.GetIndustrialFloorMaterial());
                            go.name = "Industrial Floor";
                            reg.SetTag("Industrial Zone");
                            GenerateBuilding(reg, go, 0f, parent, 0.00125f);
                            break;
                    }
                }
                break;
            case BuildingType.CropField:
                CropField newCropField = go.AddComponent<CropField>();
                List<GeometryObject> cropFieldsOptions = regionsController.GetCropFieldOptions();
                newCropField.geometryObject = cropFieldsOptions[Random.Range(0, cropFieldsOptions.Count)];
                if (regionsController.GetCropFields())
                    newCropField.SetMonth(regionsController.GetMonth());
                else
                    newCropField.SetMonth(regionsController.GetDefaultMonth());
                newCropField.SetTexture(regionsController.GetCropFieldTexture());
                newCropField.Randomize();
                reg.SetTag(newCropField.GetCropFieldType() + "");
                reg.SetCropFieldType(newCropField.GetCropFieldType());
                newCropField.CreateGeometry(reg, parent);
                break;
            default:
                GenerateBuilding(reg, go, 0f, parent);
                break;
        }

        if (reg.GetTag() != null && !reg.GetTag().Equals("") && go != null)
            go.tag = reg.GetTag();

        return reg;
    }
    
    public static List<Vector3> Transformar(List<Vector3> puntos, Transform parentTransform)
    {
        List<Vector3> newPuntos = new List<Vector3>();

        foreach (Vector3 punto in puntos)
        {
            // Mover el punto
            Vector3 puntoMovido = parentTransform.TransformPoint(punto);

            // Escalar el punto
            Vector3 puntoEscalado = new Vector3(
                puntoMovido.x * parentTransform.localScale.x,
                puntoMovido.y * parentTransform.localScale.y,
                puntoMovido.z * parentTransform.localScale.z
            );

            // Rotar el punto
            Quaternion rotacion = parentTransform.rotation;
            Vector3 puntoRotado = rotacion * puntoEscalado;

            newPuntos.Add(puntoRotado);
        }

        return newPuntos;
    }
    
    public void ShowOrHideBuildings(bool showBuildings)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform building = transform.GetChild(i);
            if (building != transform)
            {
                building.gameObject.SetActive(showBuildings);
            }
        }
    }
    
    public void HideBuildings()
    {
        ShowOrHideBuildings(false);
    }
    
    public void ShowBuildings()
    {
        ShowOrHideBuildings(true);
    }
}
