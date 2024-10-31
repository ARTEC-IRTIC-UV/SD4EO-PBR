using System.Collections.Generic;
using UnityEngine;

/**
 * This script contains the different elements that compose a vegetation plot
 * in the application (dimensions, materials and other characteristics)
 */
public class Vegetation : GeometryElement
{
    public float vegetationProbability = 0.2f;
    public List<Color> vegetationColors;
    public Material vegetationMaterial;
    public Vector2 vegetationHeightMinMax = new Vector2(1.5f, 6.0f);
    public Vector2 vegetationWidthMinMax = new Vector2(1.5f, 3.0f);
    private bool generateTrees;

    override public void Randomize()
    {
        VegetationObject vegetationObject = (VegetationObject)geometryObject;
        material = vegetationObject.GetMaterials()[Random.Range(0, vegetationObject.GetMaterials().Count)];
        vegetationProbability = Random.Range(vegetationObject.GetVegetationProbabilityMinMax().x, vegetationObject.GetVegetationProbabilityMinMax().y);
        vegetationMaterial = vegetationObject.GetVegetationMaterial();
        vegetationMaterial.SetColor("_Color2D", new Color(0f, 1f, 0f));
        vegetationColors = vegetationObject.GetVegetationColors();
        vegetationHeightMinMax = vegetationObject.GetVegetationHeightMinMax();
        vegetationWidthMinMax = vegetationObject.GetVegetationWidthMinMax();

        float r = UnityEngine.Random.Range(0.7f, 1.0f);
        color = new Color(r, r, r, 1.0f);
    }

    override public void CreateGeometry(Region reg, Transform parent)
    {
        base.CreateGeometry(reg, parent);
        if (gameObject)
        {
            gameObject.name = "Vegetation";
            gameObject.tag = "Vegetation";

            MeshFilter mf = gameObject.GetComponent<MeshFilter>();
            if(mf)
                GenerateTrees(gameObject, mf.sharedMesh);
        }
       
    }

    // Function to generate trees inside a vegetation plot
    void GenerateTrees(GameObject parent, Mesh mesh)
    {
        VegetationObject vegetationObject = (VegetationObject)geometryObject;
        Vector3 randomPoint = new Vector3(Random.Range(0.0f, 100.0f), 0, Random.Range(0.0f, 100.0f));
        // Get the bounds of the mesh
        Bounds rect = mesh.bounds;

        
        int numberOfTrees;
        if (generateTrees)
            numberOfTrees = (int)( rect.size.x * rect.size.z * vegetationProbability * 30000);
        else
            numberOfTrees = (int)( rect.size.x * rect.size.z * vegetationProbability * 1000);

        List<GameObject> trees = new List<GameObject>();

        for (int i=0; i<numberOfTrees; i++)
        {
            Vector3 p = new Vector3(Random.Range(rect.min.x, rect.max.x), 0, Random.Range(rect.min.z, rect.max.z));

            if (!GeometryUtils.pointIn2DMesh(p, mesh))
            {
                continue;
            }

            GameObject tree = new GameObject();
            MeshFilter meshFilter = tree.AddComponent<MeshFilter>();
            Mesh treeMesh = GeometryUtils.generateSphere(4, 0.75f, 100);
            GeometryUtils.addColorToMesh(treeMesh, vegetationColors[UnityEngine.Random.Range(0, vegetationColors.Count)]);
            meshFilter.sharedMesh = treeMesh;
            MeshRenderer meshRenderer = tree.AddComponent<MeshRenderer>();

            meshRenderer.sharedMaterial = vegetationMaterial;
            tree.transform.parent = parent.transform;
            tree.transform.localPosition = p ;
            tree.transform.localRotation = Quaternion.Euler(0, UnityEngine.Random.Range(0,360), 0);

            float width = UnityEngine.Random.Range(GetVegetationWidthMinMax().x, GetVegetationWidthMinMax().y);
            float height = UnityEngine.Random.Range(GetVegetationHeightMinMax().x, GetVegetationHeightMinMax().y);
            Vector3 scaleVector = new Vector3(width, height, width);
            tree.transform.localScale = scaleVector;

            trees.Add(tree);
        }

        if (trees.Count > 0)
        {
            GameObject merged = GeometryUtils.mergeGameobjects(trees);
            merged.name = "Vegetation objetcs";
            merged.tag = "Vegetation";
            merged.transform.parent = parent.transform;
        }
    }
    
    // GETTERS AND SETTERS
    
    public Vector2 GetVegetationHeightMinMax()
    {
        return vegetationHeightMinMax * kmScale;
    }
    
    public Vector2 GetVegetationWidthMinMax()
    {
        return vegetationWidthMinMax * kmScale;
    }

    public void SetGenerateTrees(bool trees)
    {
        generateTrees = trees;
    }
}
