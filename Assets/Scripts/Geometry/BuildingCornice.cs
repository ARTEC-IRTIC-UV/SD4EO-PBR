using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/**
 * This script contains the different elements that compose a building with cornice
 * in the application (dimensions, materials and other characteristics)
 */
public class BuildingCornice : GeometryElement
{
    public float height;
    public float corniceHeight;
    public float corniceWidth;
    public Material wallMaterial;
    public Material solarPanelMaterial;
    public float solarPanelProbability;
    public float solarPanelDensity;
    List<Vector3> vertices;
    List<int> triangles;
    List<Vector2> uvs;
    List<Vector3> normals;

    List<Color> colors;

    void addQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector2 uvOffset)
    {
        int v1 = vertices.Count;
        vertices.Add(p1);
        int v2 = vertices.Count;
        vertices.Add(p2);
        int v3 = vertices.Count;
        vertices.Add(p3);
        int v4 = vertices.Count;
        vertices.Add(p4);

        triangles.Add(v3);
        triangles.Add(v2);
        triangles.Add(v1);

        triangles.Add(v3);
        triangles.Add(v4);
        triangles.Add(v2);

        uvs.Add(new Vector2(0, 0) + uvOffset);
        uvs.Add(new Vector2(0, (p2-p1).magnitude) + uvOffset);
        uvs.Add(new Vector2((p3-p1).magnitude, 0) + uvOffset);
        uvs.Add(new Vector2((p3-p1).magnitude, (p2-p1).magnitude) + uvOffset);

        Vector3 normal = Vector3.Cross((p2-p1), (p3-p1)).normalized;
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);

        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
        colors.Add(color);
    }

    public override void Randomize()
    {
        BuildingCorniceObject buildingCorniceObject = (BuildingCorniceObject)geometryObject;

        base.Randomize();

        height = Random.Range(buildingCorniceObject.GetBuildingHeightMinMax().x, buildingCorniceObject.GetBuildingHeightMinMax().y);
        corniceHeight = Random.Range(buildingCorniceObject.GetCorniceHeightMinMax().x, buildingCorniceObject.GetCorniceHeightMinMax().y);
        corniceWidth = Random.Range(buildingCorniceObject.GetCorniceWidthMinMax().x, buildingCorniceObject.GetCorniceWidthMinMax().y);
        wallMaterial = buildingCorniceObject.GetWallMaterials()[Random.Range(0, buildingCorniceObject.GetWallMaterials().Count)];
        solarPanelProbability = buildingCorniceObject.GetSolarPanelProbability();
        solarPanelDensity = buildingCorniceObject.GetSolarPanelDensity();
        solarPanelMaterial = buildingCorniceObject.GetSolarPanelMaterial();
    }


    override public void CreateGeometry(Region reg, Transform parent)
    {
        GeometryUtils.cleanGameObject(gameObject);
        region = reg;

        // Create the gameobject
        GameObject go = gameObject;
        go.name = "Building Cornice";
        go.transform.parent = parent;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity; 
        go.transform.localScale = new Vector3(0.9f, 1f, 0.9f);

        // Compute ground mesh with offset and rotation aligned with the longest edge
        Mesh mesh;
        Vector3 offset;
        Quaternion rotation;
        Quaternion rotationInv;
        GeometryUtils.computeRegionMesh(region, out mesh, out offset, out rotation, out rotationInv);

        if (mesh == null)
        {
            DestroyImmediate(go);
        }
        else
        {
            GeometryUtils.addColorToMesh(mesh, color);

            go.transform.position = offset;
            go.transform.rotation = rotationInv;
        
            // Generate the building and its decorations
            GenerateBuildingGeometry(mesh);
        }
    }
    
    void GenerateBuildingGeometry(Mesh mesh)
    {
        // Add mesh filter
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        
        // Add mesh renderer
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;

        Vector3[] verticesBase = mesh.vertices;
        int[] trianglesBase = mesh.triangles;

        // Compute inner Normals
        Vector3[] cornerInnerNormal = new Vector3[verticesBase.Length];
        for (int i = 0; i < verticesBase.Length; i++)
        {
            Vector3 p3 = verticesBase[i];
            Vector3 p2 = verticesBase[(i + 1) % verticesBase.Length];
            Vector3 p1 = verticesBase[(i + 2) % verticesBase.Length];
            
            Vector3 vp1 = (p1 - p2).normalized;
            Vector3 vp2 = (p3 - p2).normalized;

            Vector3 pMiddle = (p2 + vp1 + p2 + vp2) / 2;
            pMiddle.y = 0;
            cornerInnerNormal[(i+1) % verticesBase.Length] = (pMiddle-p2).normalized;

            // Check if the corner is concave
            Vector3 a1 = Vector3.Cross(p2 - p1, cornerInnerNormal[(i + 1) % verticesBase.Length]);
            if (a1.y < 0)
            {
                cornerInnerNormal[(i+1) % verticesBase.Length] *= -1;
            }
        }
        
        // Create walls
        float l = 0;

        vertices = new List<Vector3>();
        triangles = new List<int>();
        uvs = new List<Vector2>();
        normals = new List<Vector3>();
        colors = new List<Color>();

        for (int i=0; i<verticesBase.Length; i++)
        {
            Vector3 p2 = verticesBase[i];
            Vector3 p1 = verticesBase[(i + 1) % verticesBase.Length] ;
            addQuad(p1, p1 + Vector3.up * GetHeight(), p2, p2 + Vector3.up * GetHeight(), new Vector2(l,0));
            l += (p2-p1).magnitude;
        }

        // Create cornice  
        l = 0;
        for (int i = 0; i < verticesBase.Length; i++)
        {
            Vector3 p2 = verticesBase[i] + Vector3.up * GetHeight();
            Vector3 p1 = verticesBase[(i + 1) % verticesBase.Length] + Vector3.up * GetHeight();
            Vector3 n2 = cornerInnerNormal[i];
            Vector3 n1 = cornerInnerNormal[(i + 1) % verticesBase.Length];

            addQuad(
                p1, 
                p1 + n1 * GetCorniceWidth(), 
                p2, 
                p2 + n2 * GetCorniceWidth(),
                new Vector2(l, 0));

            addQuad(
                p1 + n1 * GetCorniceWidth(), 
                p1 + n1 * GetCorniceWidth() - Vector3.up * GetCorniceHeight(), 
                p2 + n2 * GetCorniceWidth(), 
                p2 + n2 * GetCorniceWidth() - Vector3.up * GetCorniceHeight(),
                new Vector2(l, 0));

            l += (p2 - p1).magnitude;
        }

        Mesh wallMesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray(),
            normals = normals.ToArray(),
            colors = colors.ToArray()
        };
        wallMesh.RecalculateBounds();
        wallMesh.RecalculateNormals();

        // Create roof
        List<Vector3> verticesRoof = new List<Vector3>();
        List<Vector2> uvRoof = new List<Vector2>();
        List<Vector3> normalsRoof = new List<Vector3>();
        List<Color> colorsRoof = new List<Color>();
        for (int i = 0; i < verticesBase.Length; i++)
        {
            Vector3 p = verticesBase[i] + Vector3.up * (GetHeight() - GetCorniceHeight()) + cornerInnerNormal[i] * GetCorniceWidth();
            verticesRoof.Add(p);
            uvRoof.Add(new Vector2(p.x, p.z) - new Vector2(verticesBase[0].x, verticesBase[0].z));
            normalsRoof.Add(Vector3.up);
            colorsRoof.Add(new Color(color.r, color.r, color.r, 1));
        }

        Mesh roofMesh = new Mesh
        {
            vertices = verticesRoof.ToArray(),
            triangles = trianglesBase,
            uv = uvRoof.ToArray(),
            normals = normalsRoof.ToArray(),
            colors = colorsRoof.ToArray()
        };
        roofMesh.RecalculateBounds();
        roofMesh.RecalculateNormals();

        // Combine meshes
        CombineInstance[] combine = new CombineInstance[2];
        combine[0].mesh = wallMesh;
        combine[0].transform = gameObject.transform.localToWorldMatrix;
        combine[1].mesh = roofMesh;
        combine[1].transform = gameObject.transform.localToWorldMatrix;

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine, false, false);
        meshFilter.sharedMesh = combinedMesh;
        meshRenderer.sharedMaterials = new Material[] { wallMaterial, material };

        // Generate roof boxes
        GenerateRoofBoxes(gameObject, roofMesh);
        
        if(Random.Range(0f, 100f) < solarPanelProbability)
            GenerateRoofPanels(gameObject, roofMesh);
    }

    void GenerateRoofBoxes(GameObject parent, Mesh mesh)
    {
        int numberOfBoxes = Random.Range(1, 5);
        Vector3 offset = new Vector3(Random.Range(5.0f, 20.0f),0,Random.Range(5.0f, 20.0f));
        Vector3 boxSize = new Vector3(1.5f, 2.0f, 3.0f) * 0.001f;
        
        float separation = Random.Range(10.0f, 30.0f);

        for (int i=0; i<numberOfBoxes; i++)
        {
            Vector3 p = (Vector3.right * i * separation + offset) * 0.001f;

            if (GeometryUtils.pointIn2DMesh(p, mesh))
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.tag = region.GetTag();
                go.transform.parent = parent.transform;
                go.transform.localPosition = p + Vector3.up * (GetHeight() - GetCorniceHeight() + boxSize.y / 2f);
                go.transform.localScale = boxSize;
                go.transform.localRotation = Quaternion.Euler(0, 0, 0);
            }
        }
    }
    
    // Function to generate the solar panels on the roof
    void GenerateRoofPanels(GameObject parent, Mesh mesh)
    {
        float solarPanelWidth = 2f;
        float solarPanelLength = solarPanelWidth * 1.5f;
        Vector3 solarPanelSpace = new Vector3(0, 0.05f, 0);
        Vector3 solarPanelSize = new Vector3((solarPanelLength) - 0.2f, 0.05f, solarPanelWidth-0.2f) * 0.001f;

        //Analizaremos la bounding box del tejado, y con ello tendremos la posibilidad de obtener N puntos en forma de grid
        var BB = TestAlgorithmsHelpMethods.CalculateBoundingBox(mesh.vertices.ToList());
        float minX = BB.x;
        float minY = BB.y;
        float maxX = BB.z;
        float maxY = BB.w;
        
        int nCellsX = Mathf.FloorToInt((maxX - minX) / solarPanelSize.x);
        int nCellsY = Mathf.FloorToInt((maxY - minY) / solarPanelSize.z);
        
        //Con esto tendríamos la cantidad de filas y columnas que tendrá el tejado. Configurando densidad debería ser:
        solarPanelSpace.x = solarPanelSize.x;
        solarPanelSpace.z = ((maxY - minY) / Mathf.Max( nCellsY * solarPanelDensity, 2));

        List<Vector3> gridPoints = TestAlgorithmsHelpMethods.GenerateGridPointsInSquare(minX, minY, maxX, maxY, solarPanelSpace.x, solarPanelSpace.z);
        List<GameObject> solarPannels = new List<GameObject>();
        foreach (var g in gridPoints)
        {
            Vector3 cornerLeftDown = g + new Vector3(-solarPanelLength/2f*0.001f, 0f, -solarPanelWidth/2f*0.001f);
            Vector3 cornerLeftUp = g + new Vector3(-solarPanelLength/2f*0.001f, 0f, solarPanelWidth/2f*0.001f);
            Vector3 cornerRightDown = g + new Vector3(solarPanelLength/2f*0.001f, 0f, -solarPanelWidth/2f*0.001f);
            Vector3 cornerRightUp = g + new Vector3(solarPanelLength/2f*0.001f, 0f, solarPanelWidth/2f*0.001f);

            List<Vector3> cornersToCheck = new List<Vector3>{cornerLeftDown, cornerLeftUp, cornerRightDown, cornerRightUp};
            bool isInside = true;
            foreach (var corner in cornersToCheck)
            {
                if (!GeometryUtils.pointIn2DMesh(corner, mesh))
                {
                    isInside = false;
                    break;
                }
            }
            if (isInside)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.tag = region.GetTag();
                go.transform.parent = parent.transform;
                go.transform.localPosition = g + Vector3.up * (GetHeight() - GetCorniceHeight() + solarPanelSize.y / 2f);
                go.transform.localScale = solarPanelSize;
                go.transform.localRotation = Quaternion.Euler(0, 0, 0);
                go.GetComponent<Renderer>().sharedMaterial = solarPanelMaterial;
                List<Vector3> meshVertices = go.GetComponent<MeshFilter>().sharedMesh.vertices.ToList();
                RotateAroundBestLocalAxis(15f, go.transform,meshVertices);
                solarPannels.Add(go);
            }
        }

        GameObject combinedPanels = GeometryUtils.mergeGameobjects(solarPannels, "Solar Panels");
        combinedPanels.gameObject.name = "Solar Panels";
        combinedPanels.transform.parent = parent.transform;
        combinedPanels.tag = region.GetTag();
    }
    
    void RotateAroundBestLocalAxis(float degrees, Transform solarPanelTransform, List<Vector3> meshVertices)
    {
        // Crear un vector que represente el eje X global
        Vector3 globalZAxis = Vector3.forward;

        // Rotación alrededor del eje local X
        Quaternion rotationLocalX = Quaternion.AngleAxis(degrees, solarPanelTransform.right);
        // Calcula el vector X global después de la rotación local X
        Vector3 resultingGlobalXFromLocalX = rotationLocalX * solarPanelTransform.up;

        // Rotación alrededor del eje local Z
        Quaternion rotationLocalZ = Quaternion.AngleAxis(degrees, solarPanelTransform.forward);
        // Calcula el vector X global después de la rotación local Z
        Vector3 resultingGlobalXFromLocalZ = rotationLocalZ * solarPanelTransform.up;

        /// Compara cuál de las dos rotaciones es más cercana al eje X global o su opuesto
        float errorLocalX1 = Vector3.Angle(globalZAxis, resultingGlobalXFromLocalX);
        float errorLocalX2 = Vector3.Angle(-globalZAxis, resultingGlobalXFromLocalX);
        float errorLocalZ1 = Vector3.Angle(globalZAxis, resultingGlobalXFromLocalZ);
        float errorLocalZ2 = Vector3.Angle(-globalZAxis, resultingGlobalXFromLocalZ);

        float errorLocalX = Mathf.Min(errorLocalX1, errorLocalX2);
        float errorLocalZ = Mathf.Min(errorLocalZ1, errorLocalZ2);
        
        Vector3 idealHeight = solarPanelTransform.localPosition;
        
        // Elige la rotación con el menor error
        if (errorLocalX < errorLocalZ)
        {
            solarPanelTransform.rotation = rotationLocalX * solarPanelTransform.rotation;
            if (errorLocalX == errorLocalX2)
                solarPanelTransform.Rotate(0, 180f, 0, Space.World);
        }
        else
        {
            solarPanelTransform.rotation = rotationLocalZ * solarPanelTransform.rotation;
            if (errorLocalZ == errorLocalZ2)
                solarPanelTransform.Rotate(0, 180f, 0, Space.World);
        }
        
        List<Vector3> pointsAux = new List<Vector3>();
        for (int i = 0; i < meshVertices.Count; i++)
            pointsAux.Add(solarPanelTransform.TransformPoint(meshVertices[i]));

        float minimumHeight = pointsAux.OrderBy(v => v.y).First().y;
        solarPanelTransform.Translate(0f,idealHeight.y - minimumHeight,0f, Space.World);
    }
    
    // GETTERS
    
    public float GetHeight()
    {
        return height * kmScale;
    }

    public float GetCorniceHeight()
    {
        return corniceHeight * kmScale;
    }
    
    public float GetCorniceWidth()
    {
        return corniceWidth * kmScale;
    }
}
