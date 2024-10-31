using System.Collections.Generic;
using UnityEngine;

/*
 * This script contains some functions that helps to create the geometry of the application
 */
public class GeometryUtils : MonoBehaviour
{
    // Function to merge N gameobjects into one in order to have less gameobjects in a Unity scene
    public static GameObject mergeGameobjects(List<GameObject> gameobjects, string name = "MergeGameobjects")
    {
        MeshFilter[] meshFilters = new MeshFilter[gameobjects.Count];
        for (int i = 0; i < gameobjects.Count; i++)
        {
            meshFilters[i] = gameobjects[i].GetComponent<MeshFilter>();
        }

        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        for (int i = 0; i < meshFilters.Length; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            meshFilters[i].gameObject.SetActive(false);
        }

        GameObject gameObject = new GameObject(name);
        if (meshFilters.Length > 0)
        {
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = new Mesh();
            meshFilter.sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            meshFilter.sharedMesh.CombineMeshes(combine);
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = meshFilters[0].gameObject.GetComponent<MeshRenderer>().sharedMaterial;
        }

        // Destroy the gameobjects
        for (int i = 0; i < gameobjects.Count; i++)
        {
            DestroyImmediate(gameobjects[i]);
        }

        return gameObject;
    }

    // Function that computes if a world point is inside a triangle
    static public bool pointInTriangle(Vector3 p, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 e0 = p0 - p1;
        Vector3 e1 = p1 - p2;
        Vector3 e2 = p2 - p0;

        Vector3 v0 = p - p0;
        Vector3 v1 = p - p1;
        Vector3 v2 = p - p2;

        float a0 = e0.x * v0.z - e0.z * v0.x;
        float a1 = e1.x * v1.z - e1.z * v1.x;
        float a2 = e2.x * v2.z - e2.z * v2.x;

        if (a0 >= 0 && a1 >= 0 && a2 >= 0)
        {
            return true;
        }

        return false;
    }

    // Function that computes if a point is in a 2D mesh
    static public bool pointIn2DMesh(Vector3 p, Mesh m)
    {
        Vector3[] vertices = m.vertices;
        int[] triangles = m.triangles;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 p0 = vertices[triangles[i]];
            Vector3 p1 = vertices[triangles[i + 1]];
            Vector3 p2 = vertices[triangles[i + 2]];

            if (pointInTriangle(p, p0, p1, p2))
            {
                return true;
            }
        }
        return false;
    }

    // Function that destroys all the children gameobjects of a gameobject
    static public void cleanGameObject(GameObject go)
    {
        // Remove children
        Transform transform = go.transform;
        List<GameObject> objectsToDelete = new List<GameObject>();

        for (int i = 0; i < transform.childCount; i++)
            objectsToDelete.Add(transform.GetChild(i).gameObject);

        foreach (var o in objectsToDelete)
            DestroyImmediate(o);

        // Remove MeshFilter and MeshRenderer
        MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            DestroyImmediate(meshRenderer);

        MeshFilter meshFilter = go.GetComponent<MeshFilter>();
        if (meshFilter != null)
            DestroyImmediate(meshFilter);
    }

    // Function to clone a mesh
    static public Mesh cloneMesh(Mesh m)
    {
        Mesh mesh = new Mesh()
        {
            vertices = m.vertices,
            triangles = m.triangles,
            uv = m.uv,
            normals = m.normals,
            colors = m.colors,
            tangents = m.tangents
        };
        return mesh;
    }

    // Function to compute the mesh of a region
    static public void computeRegionMesh(Region region, out Mesh mesh, out Vector3 offset, out Quaternion rotation, out Quaternion rotationInv)
    {
        // Search the longest edge of the region
        Vector3[] contour = region.GetCornerPointsV3().ToArray();

        float maxLength = (contour[0] - contour[1]).magnitude;
        int maxIndex = 0;

        for (int i = 1; i < contour.Length; i++)
        {
            float length = (contour[i] - contour[(i + 1) % contour.Length]).magnitude;
            if (length > maxLength)
            {
                maxLength = length;
                maxIndex = i;
            }
        }

        // Calculate the offset and the directions
        offset = contour[maxIndex];

        Vector3 dirY = contour[(maxIndex + 1) % contour.Length] - contour[maxIndex];
        //Vector3 dirY = borderContour[1] - borderContour[0];
        dirY.Normalize();
        float angle = Vector3.Angle(dirY, Vector3.right);
        if (Vector3.Cross(dirY, Vector3.right).y > 0)
        {
            angle = -angle;
        }
        rotation = Quaternion.Euler(0, -angle, 0);
        rotationInv = Quaternion.Euler(0, angle, 0);

        // Compute the mesh vertices and UVs with the offset and the rotation
        Vector3[] vertices = region.getTriangulatedMesh().vertices;
        List<Vector2> uv = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 p = vertices[i];
            
            // Correct point with offset and rotation
            p -= offset;
            p = rotation * p;

            // Save vertex information
            vertices[i] = p;
            uv.Add(new Vector2(p.x, p.z));
            normals.Add(Vector3.up);
        }

        // Create the mesh

        bool infinito = false;
        foreach (var v in vertices)
        {
            if (!float.IsFinite(v.x) || float.IsNaN(v.x) || !float.IsFinite(v.y) || float.IsNaN(v.y) ||
                !float.IsFinite(v.z) || float.IsNaN(v.z))
            {
                infinito = true;
                break;
            }
        }

        if (infinito)
            mesh = null;
        else
        {
            mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.uv = uv.ToArray();
            mesh.normals = normals.ToArray();
            mesh.triangles = region.getTriangulatedMesh().triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }
    }

    // Function that generates a sphere with noise for creating trees in the vegetation
    static public Mesh generateSphere(int nPartitions, float noiseAmp, float noiseFreq)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        List<Color> colors = new List<Color>();

        float step = 1.0f / nPartitions;

        int currIndex = 0;
        int tamTira = nPartitions + 1;
        float minY = 0.0f;

        float random = Random.Range(0.0f, 1000.0f);
        float randomColor = Random.Range(0.8f, 1.0f);

        for (int i=0; i<nPartitions; i++) {
            float u = step * (i%nPartitions);
            for (int j=0; j<=nPartitions; j++) {
                float v = step * j;

                // Add noise
                float noise = noiseAmp * (Mathf.PerlinNoise(noiseFreq * u + random, noiseFreq * v + random) );

                if (j==0 || j==nPartitions)
                {
                    noise = 0;
                }

                u += noise * step ;
                v += noise * step ;

                Vector3 aux = new Vector3(
                    Mathf.Cos(2*Mathf.PI*u) * Mathf.Sin(Mathf.PI * v),
                    Mathf.Cos(Mathf.PI*v),
                    Mathf.Sin(2*Mathf.PI*u) * Mathf.Sin(Mathf.PI * v)
                );

                vertices.Add(aux * (1+noise));
                normals.Add(aux);
                uv.Add(new Vector2(u,1.0f-v));
                colors.Add(new Color(randomColor, randomColor, randomColor, 1));

                if (i>0 && j>0) {
                    triangles.Add(currIndex);
                    triangles.Add(currIndex - tamTira);
                    triangles.Add(currIndex - tamTira - 1);

                    triangles.Add(currIndex);
                    triangles.Add(currIndex - tamTira - 1);
                    triangles.Add(currIndex - 1);

                    if (i==nPartitions-1)
                    {
                        triangles.Add(currIndex);
                        triangles.Add(j - 1);
                        triangles.Add(j);
                        
                        triangles.Add(currIndex);
                        triangles.Add(currIndex - 1);
                        triangles.Add(j - 1);
                    }
                }

                

                // if (j == nPartitions)
                // {
                //     triangles.Add(currIndex);
                //     triangles.Add(currIndex - tamTira);
                //     triangles.Add(currIndex - tamTira - 1);
                // }

                // Save MinY
                if (vertices[currIndex].y < minY)
                {
                    minY = vertices[currIndex].y;
                }

                // Incrementar el contador de vertices
                currIndex ++;
            }
        }
        
        // Move the sphere to the origin
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] -= new Vector3(0, minY, 0);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.normals = normals.ToArray();
        mesh.colors = colors.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }

    // Function that set the color to a mesh
    static public void addColorToMesh(Mesh mesh, Color color, float randomAmp = 0.0f)
    {
        Color[] colors = new Color[mesh.vertices.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            float r = 1 - Random.Range(0, randomAmp);
            colors[i] = new Color(
                color.r * r,
                color.g * r,
                color.b * r,
                color.a);
        }
        mesh.colors = colors;
    }
}