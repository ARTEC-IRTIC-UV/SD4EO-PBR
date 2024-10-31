using UnityEngine;
using VInspector;

/**
 * This script contains the different elements that compose a general geometry element
 * in the application (dimensions, materials and other characteristics)
 */
public class GeometryElement : MonoBehaviour
{
    public GeometryObject geometryObject;
    public Material material;
    public Color color;
    public float width;
    protected Region region;
    protected float kmScale = 0.001f;

    [Button("Regenerate Geometry")]
    public void CreateGeometry()
    {
        CreateGeometry(region, transform.parent);
    }

    [Button("Randomize")]
    public void RandomizeButton()
    {
        Randomize();
        CreateGeometry();
    }

    virtual public void Randomize()
    {
        width = Random.Range(geometryObject.GetBuildingWidthMinMax().x, geometryObject.GetBuildingWidthMinMax().y);
        material = geometryObject.GetMaterials()[Random.Range(0, geometryObject.GetMaterials().Count)];
        color = geometryObject.GetColors()[Random.Range(0, geometryObject.GetColors().Count)];
    }

    virtual public void CreateGeometry(Region reg, Transform parent)
    {
        GeometryUtils.cleanGameObject(gameObject);
        region = reg;

        // Create the gameobject
        GameObject go = gameObject;
        go.name = "Region Geometry";
        go.transform.parent = parent;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity; 
        go.transform.localScale = Vector3.one;
        
        // Compute ground mesh with offset and rotation aligned with the longest edge
        Mesh mesh;
        Vector3 offset;
        Quaternion rotation;
        Quaternion rotationInv;
        GeometryUtils.computeRegionMesh(region, out mesh, out offset, out rotation, out rotationInv);

        if (mesh == null)
        {
            //DestroyImmediate(go);
        }
        else
        {
            // Set the color
            GeometryUtils.addColorToMesh(mesh, color);

            // Set up gameobject transform
            go.transform.position = offset;
            go.transform.rotation = rotationInv;
        
            // Add mesh filter
            MeshFilter meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;
        
            // Add mesh renderer
            MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
        }
    }
}