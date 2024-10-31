using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;
using VInspector;

public class BuildingRoof : GeometryElement
{
    public float height;
    public float roofHeightMultiplier = 1.0f;
    public float roofFlyLength = 1.0f;
    public Material wallMaterial;
    public Material solarPanelMaterial;
    public float solarPanelProbability  = 50f;

    public bool multipleCornersRoof = true;

    private float roofHeight;
    
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
        BuildingRoofObject buildingRoofObject = (BuildingRoofObject)geometryObject;
        base.Randomize();
        height = UnityEngine.Random.Range(buildingRoofObject.GetBuildingHeightMinMax().x, buildingRoofObject.GetBuildingHeightMinMax().y);
        roofHeightMultiplier = UnityEngine.Random.Range(buildingRoofObject.GetRoofHeightMultiplyerMinMax().x, buildingRoofObject.GetRoofHeightMultiplyerMinMax().y);
        roofFlyLength = UnityEngine.Random.Range(buildingRoofObject.GetRoofFlyLengthMinMax().x, buildingRoofObject.GetRoofFlyLengthMinMax().y);
        wallMaterial = buildingRoofObject.GetWallMaterials()[UnityEngine.Random.Range(0, buildingRoofObject.GetWallMaterials().Count)];
        multipleCornersRoof = UnityEngine.Random.value > 0.5f;
        solarPanelMaterial = buildingRoofObject.GetSolarPanelMaterial();
    }


    override public void CreateGeometry(Region reg, Transform parent)
    {
        GeometryUtils.cleanGameObject(gameObject);
        region = reg;

        // Create the gameobject
        GameObject go = gameObject;
        go.name = "Building Roof";
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
        Vector3[] verticesBase = mesh.vertices;
        
        // Generate walls and roof meshes
        Mesh wallMesh = GenerateWalls(verticesBase, GetHeight());

        // Compute roof height
        //float side = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.z);
        //Debug.Log("side: " + side);

        Mesh roofMesh = GenerateRoofTriangleCenters(verticesBase, mesh.triangles, multipleCornersRoof);

        // Combine meshes
        CombineInstance[] combine = new CombineInstance[2];
        combine[0].mesh = wallMesh;
        combine[0].transform = gameObject.transform.localToWorldMatrix;
        combine[1].mesh = roofMesh;
        combine[1].transform = gameObject.transform.localToWorldMatrix;
        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine, false, false);

        // Add mesh filter
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = combinedMesh;
        
        // Add mesh renderer
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = new Material[] { wallMaterial, material };

        // Generate Solar Panels
        if (UnityEngine.Random.value < solarPanelProbability * 0.01f)
            GenerateSolarPanels(roofMesh);
    }

    Mesh GenerateWalls(Vector3[] verticesBase, float height)
    {
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
            addQuad(p1, p1 + Vector3.up * height, p2, p2 + Vector3.up * height, new Vector2(l,0));
            l += (p2-p1).magnitude;
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

        return wallMesh;
    }

    int searchClosestPoint(Vector3[] points, Vector3 p)
    {
        float minDist = float.MaxValue;
        int minIndex = 0;
        for (int i = 0; i < points.Length; i++)
        {
            float dist = (points[i] - p).magnitude;
            if (dist < minDist)
            {
                minDist = dist;
                minIndex = i;
            }
        }
        return minIndex;
    }

    float distancePointVector(Vector3 p, Vector3 p1, Vector3 p2)
    {
        Vector3 v = p2 - p1;
        Vector3 w = p - p1;
        float c1 = Vector3.Dot(w, v);
        if (c1 <= 0)
            return (p - p1).magnitude;

        float c2 = Vector3.Dot(v, v);
        if (c2 <= c1)
            return (p - p2).magnitude;

        float b = c1 / c2;
        Vector3 pb = p1 + b * v;
        return (p - pb).magnitude;
    }

    Mesh GenerateRoofTriangleCenters(Vector3[] verticesBase, int[] trianglesBase, bool multipleCornersRoof = false)
    {
        Vector3[] triangleCenters;

        if (multipleCornersRoof)
        {
            // Compute mass center of each triangle
            triangleCenters = new Vector3[trianglesBase.Length / 3];
            float z = 0;
            for (int i = 0; i < trianglesBase.Length / 3; i++)
            {
                Vector3 p1 = verticesBase[trianglesBase[i * 3]];
                Vector3 p2 = verticesBase[trianglesBase[i * 3 + 1]];
                Vector3 p3 = verticesBase[trianglesBase[i * 3 + 2]];
                triangleCenters[i] = (p1 + p2 + p3) / 3;
                z += triangleCenters[i].z;
            }

            // Change z value to the mean of the z values
            z /= triangleCenters.Length;
            for (int i = 0; i < triangleCenters.Length; i++)
            {
                triangleCenters[i].z = z;
            }
        }
        else {
            // Compute mass center
            Vector3 massCenter = Vector3.zero;
            for (int i = 0; i < verticesBase.Length; i++)
            {
                massCenter += verticesBase[i];
            }
            massCenter /= verticesBase.Length;
            triangleCenters = new Vector3[1];
            triangleCenters[0] = massCenter;
        }


        // Search closest point to each vertex and move away the vertex
        for (int i = 0; i < verticesBase.Length; i++)
        {
            int closestIndex = searchClosestPoint(triangleCenters, verticesBase[i]);
            Vector3 pCenter = triangleCenters[closestIndex];
            Vector3 p = verticesBase[i];
            Vector3 dir = (p - pCenter).normalized;
            verticesBase[i] += dir * GetRoofFlyLength();
        }

        // Search biggest distance of a vertex to a center to compute the roof height
        float maxDist = 0;
        for (int i = 0; i < verticesBase.Length; i++)
        {
            int closestIndex = searchClosestPoint(triangleCenters, verticesBase[i]);
            Vector3 pCenter = triangleCenters[closestIndex];
            float dist = (pCenter - verticesBase[i]).magnitude;
            if (dist > maxDist)
                maxDist = dist;
        }
        roofHeight = maxDist * 0.5f * GetRoofHeightMultiplyer() / 0.001f;

        // Create roof
        float l = 0;

        vertices = new List<Vector3>();
        triangles = new List<int>();
        uvs = new List<Vector2>();
        normals = new List<Vector3>();
        colors = new List<Color>();

        for (int i=0; i<verticesBase.Length; i++)
        {
            Vector3 p2 = verticesBase[i];
            Vector3 p1 = verticesBase[(i + 1) % verticesBase.Length];

            int closestIndex2 = searchClosestPoint(triangleCenters, p2);
            int closestIndex1 = searchClosestPoint(triangleCenters, p1);

            Vector3 pCenter2 = triangleCenters[closestIndex2];
            Vector3 pCenter1 = triangleCenters[closestIndex1];

            // Project the center of the triangle to the edge
            float u2 = Vector3.Dot((pCenter2 - p1), (p2 - p1)) / (p2 - p1).magnitude;
            float u1 = Vector3.Dot((pCenter1 - p1), (p2 - p1)) / (p2 - p1).magnitude;

            float v2 = distancePointVector(pCenter2, p1, p2);
            float v1 = distancePointVector(pCenter1, p1, p2);

            // Apply height to the points
            p2  += Vector3.up * GetHeight();
            p1  += Vector3.up * GetHeight();
            pCenter2 += Vector3.up * (GetHeight() + roofHeight);
            pCenter1 += Vector3.up * (GetHeight() + roofHeight);
            
            // Add triangle 1
            vertices.Add(p1);
            vertices.Add(p2);
            vertices.Add(pCenter2);

            triangles.Add(vertices.Count - 3);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 1);

            uvs.Add(new Vector2(l, 0));
            uvs.Add(new Vector2(l + (p1-p2).magnitude, 0));
            uvs.Add(new Vector2(l+u2, v2));

            Vector3 normal = Vector3.Cross((p2-p1), (pCenter2-p1)).normalized;
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);

            if (closestIndex1 != closestIndex2)
            {
                // Add triangle 2
                vertices.Add(pCenter1);
                uvs.Add(new Vector2(l + u1, v1));
                normals.Add(normal);
                colors.Add(color);

                triangles.Add(vertices.Count - 2);
                triangles.Add(vertices.Count - 1);
                triangles.Add(vertices.Count - 4);
            }

            l += (p2-p1).magnitude;
        }

        Mesh roofMesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray(),
            normals = normals.ToArray(),
            colors = colors.ToArray()
        };
        roofMesh.RecalculateBounds();
        roofMesh.RecalculateNormals();

        return roofMesh;
    }

    void GenerateSolarPanels(Mesh roofMesh)
    {
        // Group triangles by the angle of the normal vector with the south direction
        Dictionary<int, List<int>> groups = new Dictionary<int, List<int>>();
        Vector3 globalSouth = transform.InverseTransformDirection(new Vector3(0, 0, -1));
        globalSouth.y = 0;
        for (int i = 0; i < roofMesh.triangles.Length; i += 3)
        {
            int i1 = roofMesh.triangles[i];
            Vector3 normal = roofMesh.normals[i1];
            normal.y = 0;
            int angle = (int)Mathf.Floor(Vector3.Angle(normal, globalSouth));
            if (!groups.ContainsKey(angle))
                groups[angle] = new List<int>();
            groups[angle].Add(i);
        }

        // Get the list of triangles with the minimum angle
        int minAngle = int.MaxValue;
        foreach (int angle in groups.Keys)
        {
            if (angle < minAngle)
            {
                minAngle = angle;
            }
        }
        List<int> minAngleTriangles = groups[minAngle];

        // Search the biggest triangle side
        float maxSide = 0;
        Vector3 XVector = Vector3.one;
        Vector3 YVector = Vector3.one;
        Vector3 origin = Vector3.one;
        for (int i = 0; i < minAngleTriangles.Count; i++)
        {
            int i1 = roofMesh.triangles[minAngleTriangles[i]];
            int i2 = roofMesh.triangles[minAngleTriangles[i] + 1];
            int i3 = roofMesh.triangles[minAngleTriangles[i] + 2];
            Vector3 p1 = roofMesh.vertices[i1];
            Vector3 p2 = roofMesh.vertices[i2];
            Vector3 p3 = roofMesh.vertices[i3];
            float side = Mathf.Max((p2 - p1).magnitude, (p3 - p2).magnitude, (p1 - p3).magnitude);
            if (side > maxSide)
            {
                maxSide = side;
                origin = p1;
                XVector = (p2 - p1).normalized;
                YVector = (p3-p1).normalized;
                Vector3 aux = Vector3.Cross(XVector, YVector);
                YVector = Vector3.Cross(aux, XVector).normalized;
            }
        }

        // Generate boxes from the origin in the X and Y directions with the max side
        List<GameObject> solarPannels = new List<GameObject>();
        Vector3 solarPanelSize = new Vector3(0.1f,1,2) * 0.001f;

        origin += XVector * 0.002f + YVector * 0.002f + Vector3.up * 0.0003f;

        int n = (int)Mathf.Floor(maxSide * 1000 / 2.0f);
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                Vector3 p1 = origin + XVector * i * 0.002f + YVector * j * 0.002f;

                // Check if the point is inside minAngleTriangles
                bool inside = true;

                bool inside1 = false;
                for (int k = 0; k < minAngleTriangles.Count; k++)
                {
                    int i1 = roofMesh.triangles[minAngleTriangles[k]];
                    int i2 = roofMesh.triangles[minAngleTriangles[k] + 1];
                    int i3 = roofMesh.triangles[minAngleTriangles[k] + 2];
                    Vector3 pt2 = roofMesh.vertices[i1];
                    Vector3 pt3 = roofMesh.vertices[i2];
                    Vector3 pt4 = roofMesh.vertices[i3];

                    if (GeometryUtils.pointInTriangle(p1 + XVector * 0.002f, pt2, pt3, pt4))
                    {
                        inside1 = true;
                        break;
                    }
                }

                bool inside2 = false;
                for (int k = 0; k < minAngleTriangles.Count; k++)
                {
                    int i1 = roofMesh.triangles[minAngleTriangles[k]];
                    int i2 = roofMesh.triangles[minAngleTriangles[k] + 1];
                    int i3 = roofMesh.triangles[minAngleTriangles[k] + 2];
                    Vector3 pt2 = roofMesh.vertices[i1];
                    Vector3 pt3 = roofMesh.vertices[i2];
                    Vector3 pt4 = roofMesh.vertices[i3];

                    if (GeometryUtils.pointInTriangle(p1 - XVector * 0.002f, pt2, pt3, pt4))
                    {
                        inside2 = true;
                        break;
                    }
                }

                bool inside3 = false;
                for (int k = 0; k < minAngleTriangles.Count; k++)
                {
                    int i1 = roofMesh.triangles[minAngleTriangles[k]];
                    int i2 = roofMesh.triangles[minAngleTriangles[k] + 1];
                    int i3 = roofMesh.triangles[minAngleTriangles[k] + 2];
                    Vector3 pt2 = roofMesh.vertices[i1];
                    Vector3 pt3 = roofMesh.vertices[i2];
                    Vector3 pt4 = roofMesh.vertices[i3];

                    if (GeometryUtils.pointInTriangle(p1 + YVector * 0.002f, pt2, pt3, pt4))
                    {
                        inside3 = true;
                        break;
                    }
                }

                inside = inside1 && inside2 && inside3;
                if (!inside)
                    continue;

                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.parent = transform;
                go.transform.localPosition = p1;
                go.transform.LookAt(transform.TransformPoint(p1 + XVector), transform.TransformPoint(p1 + YVector));
                go.transform.localScale = solarPanelSize;
                go.GetComponent<Renderer>().sharedMaterial = solarPanelMaterial;
                solarPannels.Add(go);
            }
        }

        GameObject combinedPanels = GeometryUtils.mergeGameobjects(solarPannels, "Solar Panels");
        combinedPanels.gameObject.name = "Solar Panels";
        combinedPanels.transform.parent = transform;
        combinedPanels.tag = region.GetTag();
    }
    
    public float GetHeight()
    {
        return height * kmScale;
    }
    
    public float GetRoofHeightMultiplyer()
    {
        return roofHeightMultiplier * kmScale;
    }
    
    public float GetRoofFlyLength()
    {
        return roofFlyLength * kmScale;
    }
}
