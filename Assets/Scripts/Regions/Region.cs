using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;
using Habrador_Computational_Geometry;

public enum ZoneType
{
    Downtown = 0,
    IndustrialArea = 1,
    ResidentialArea = 2,
    FieldCrops = 3
}

public enum ZoneShape
{
    Circle = 0,
    Square = 1,
    Triangle = 2
}

/*
 * This class contains all the necessary information of a region
 * (position of its border points, type of region, ID, materials, etc)
 */
[Serializable]
public class Region
{
    [SerializeField] private int regionID;
    [SerializeField] private List<Vector3> borderPoints;
    private List<int> cornerPoints;
    private HashSet<RandomPoint> interiorPoints;
    [SerializeField] private Streamline streamline;
    [SerializeField] private float area;
    [SerializeField] private int subdivisionLevel;
    [SerializeField] private bool isFinalRegion;
    private Template usedTemplate;
    private Vector3 orientacion;
    private ZoneType zoneType;
    private BuildingType buildingType;
    private CropFieldType cropFieldType;
    [SerializeField] private Zone zone;
    private Mesh triangulatedMesh;
    private Material roofMaterial;
    private Material facadeMaterial;
    private int initialIndex;
    private float longestSide;
    private float shortestSide;
    private string tag;
    
    public void SetTemplate(Template t)
    {
        usedTemplate = t;
    }

    public void SetTag(string tag)
    {
        this.tag = tag;
    }

    public string GetTag()
    {
        return tag;
    }

    public Mesh getTriangulatedMesh()
    {
        return triangulatedMesh;
    }

    public void setTriangulatedMesh(Mesh m)
    {
        triangulatedMesh = m;
    }
    public Template GetTemplate()
    {
        return usedTemplate;
    }
    public int GetRegionID()
    {
        return regionID;
    }

    public void SetRegionID(int id)
    {
        regionID = id;
    }

    public float GetLongestSide()
    {
        return longestSide;
    }
    
    public float GetShortestSide()
    {
        return shortestSide;
    }
    
    public void SetOrientacionRegion()
    {
        // El vector que debemos usar para puntuar las streamlines es el del lado más largo de la región
        Vector3[] contour = GetCornerPointsV3().ToArray();

        if (contour.Length > 2)
        {
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

            // Obtenemos el vector y lo normalizamos
            Vector3 or = contour[(maxIndex + 1) % contour.Length] - contour[maxIndex];
            or.Normalize();
            orientacion = or;
        }
        else
            orientacion = new Vector3(0f, 0f, 0f);
    }

    public Vector3 OrientacionStreamline2(Streamline s)
    {
        List<Vector3> puntosStreamline = s.GetStreamlinePoints();
        Vector3 orientacion = Vector3.Normalize(puntosStreamline.Last() - puntosStreamline.First());

        return orientacion;
    }

    public Vector3 GetOrientacion()
    {
        return orientacion;
    }
    
    public List<Vector3> GetBorderPoints()
    {
        return borderPoints;
    }
    
    public List<Vector2> GetBorderPoints2D()
    {
        return GeometricFunctions.ConvertListVector3ToListVector2XZ(borderPoints);
    }
    
    public List<Vector3> GetBorderPointsWithAngleDecimate(float distanceInPointsBetween)
    {
        return GeometricFunctions.GetDecimatedPerAngleVertices(borderPoints, distanceInPointsBetween);
    }
    
    public void SetBorderPoints(List<Vector3> borderPoints)
    {
        this.borderPoints = borderPoints;
    }
    
    public List<int> GetCornerPoints()
    {
        return cornerPoints;
    }
    
    public List<Vector3> GetCornerPointsV3()
    {
        List<Vector3> cornerPointsV3 = new List<Vector3>();

        for (int i = 0; i < borderPoints.Count; i++)
        {
            foreach (var corner in cornerPoints)
            {
                if (i == corner)
                    cornerPointsV3.Add(borderPoints[i]);
            }
        }
        
        return cornerPointsV3;
    }

    public void SetCornerPoints(List<int> cornerPoints)
    {
        this.cornerPoints = cornerPoints;
    }

    public HashSet<RandomPoint> GetInteriorPoints()
    {
        return interiorPoints;
    }

    public void SetInteriorPoints(HashSet<RandomPoint> interiorPoints)
    {
        this.interiorPoints = interiorPoints;
    }

    public Streamline GetStreamline()
    {
        return streamline;
    }

    public void SetStreamline(Streamline streamLine)
    {
        this.streamline = streamLine;
    }

    public float GetAreaKM()
    {
        return area;
    }

    public float GetAreaM()
    {
        return area * 1000000f;
    }

    public float GetAspectRatio()
    {
        return GeometricFunctions.CheckRegionRatio(GetCornerPointsV3());
    }

    public int GetInitialIndex()
    {
        return initialIndex;
    }

    public void SetInitialIndex(int initialIndex)
    {
        this.initialIndex = initialIndex;
    }

    public bool IsFinalRegion()
    {
        if (zone != null)
        {
            if (area < zone.GetMaximumSubdivisionArea())
                return true;
            else
                return false;
        }
        else
            return true;
    }

    public void CalculateRegionArea()
    {
        float a;
        float sum_x_to_z = 0;
        float sum_z_to_x = 0;
        for (int i = 0; i < borderPoints.Count - 1; i++)
        {
            sum_x_to_z += borderPoints[i].x * borderPoints[i + 1].z;
            sum_z_to_x += borderPoints[i].z * borderPoints[i + 1].x;
        }

        a = (sum_z_to_x - sum_x_to_z) / 2.0f;
        area = Mathf.Abs(a);
    }

    public void ClearAndCreateIntermediatePoints(int nStreamlines, float clearDistance, float cornerAngle, List<Vector3> excludedPoints = null)
    {
        //Para ello, simplemente debemos recorrer el perímetro y decidir, para cada segmento, cuántos puntos se generarán
        float totalDistance = 0f;
        float minimumPoints = nStreamlines;
        List<Vector3> auxVertexList = new List<Vector3>();
        List<int> cornersToDelete = new List<int>();
        List<int> newCorners = new List<int>();

        if (excludedPoints == null)
            excludedPoints = new List<Vector3>();
        //marcamos para eliminar las esquinas muy cercanas
        for (var i = 0; i < cornerPoints.Count; i++)
        {
            var corner = borderPoints[cornerPoints[i]];
            var nextCorner = borderPoints[cornerPoints[(i+1)%cornerPoints.Count]];
            var distance = Vector3.Distance(corner, nextCorner);
            totalDistance += distance;

            if(distance < clearDistance)
            {
                if (i == 0)
                    cornersToDelete.Add(i+1);
                else
                    cornersToDelete.Add(i);
            }
            else
            {
                newCorners.Add(i);
            }
        }
        
        float distanceBetweenPointsAdapted = totalDistance / minimumPoints;
        
        //  Tenemos una lista de esquinas, otra de esquinas marcadas para eliminar, y otra de excepciones

        foreach (var corner in cornersToDelete)
        {
            if (corner < cornerPoints.Count-1)
            {
                //Debug.DrawLine(borderPoints[cornerPoints[corner]], borderPoints[cornerPoints[corner]] + Vector3.up * 0.05f * cornerPoints.Count, Color.green, 10f);
                borderPoints.RemoveAt(cornerPoints[corner]);
                cornerPoints.RemoveAt(corner);
            }
            
            for (int i = corner; i < cornerPoints.Count; i++)
            {
                cornerPoints[i]--;
            }
        }

        
        for (var i = 0; i < cornerPoints.Count; i++)
        {
            var corner = borderPoints[cornerPoints[i]];
            var nextCorner = borderPoints[cornerPoints[(i+1)%cornerPoints.Count]];
            
            auxVertexList.Add(corner);
            
            List<Vector3> newPoints = GeometricFunctions.PointsBetween(corner, nextCorner, distanceBetweenPointsAdapted, 1);

            auxVertexList.AddRange(newPoints);
        }
        
        borderPoints = auxVertexList;
        cornerPoints = GeometricFunctions.CalculateCorners(borderPoints, cornerAngle);
    }

    public void OrderPointsByLongestSide(float cornerAngle, float distanceDifferenceFactor = 0f)
    {
        List<Vector3> orderedPoints = new List<Vector3>();

        // Primero calculamos el lado largo y definimos la primera esquina de la región (nuevo primer punto)
        float longSide = float.MinValue;
        float shortSide = float.MaxValue;
        int idLongCorner = -1;

        for (int i = 0; i < cornerPoints.Count; i++)
        {
            int secondID = (i + 1) % cornerPoints.Count;
            float distance = Vector3.Distance(borderPoints[cornerPoints[i]], borderPoints[cornerPoints[secondID]]);

            if (distance > longSide)
            {
                longSide = distance;
                idLongCorner = cornerPoints[i];
            }

            if (distance < shortSide)
                shortSide = distance;
        }

        longestSide = longSide;
        shortestSide = shortSide;
        
        // Reordenamos los bordes
        for (int i = idLongCorner; i < borderPoints.Count; i++)
            orderedPoints.Add(borderPoints[i]);
        
        for (int i = 0; i < idLongCorner; i++)
            orderedPoints.Add(borderPoints[i]);
        
        borderPoints = orderedPoints;
        
        // Reordenamos las esquinas y revisamos la región
        cornerPoints = GeometricFunctions.CalculateCorners(orderedPoints, cornerAngle);
        
        if (distanceDifferenceFactor != 0f)
            ReviseRegion(distanceDifferenceFactor);
    }
    
    public void ReviseRegion(float distanceFactor)
    {
        List<Vector3> corners = GetCornerPointsV3();

        if (corners.Count > 2)
        {
            float suma = 0f;
            float minDistance = float.MaxValue;
            int minCorner = -1;
            
            for (int i = 0; i < corners.Count; i++)
            {
                float distance = (corners[i] - corners[(i + 1) % corners.Count]).magnitude;
                suma += distance;

                if (distance < minDistance)
                {
                    minDistance = distance;
                    minCorner = i;
                }
            }

            float mediumDistance = suma / corners.Count;

            if (mediumDistance / minDistance > distanceFactor)
            {
                cornerPoints.RemoveAt(minCorner);
            }
        }
    }

    public Vector3 Centroide()
    {
        if (borderPoints.Count > 0)
        {
            Vector3 primero = borderPoints[0];
            Vector3 ultimo = borderPoints[borderPoints.Count - 1];
            if(primero.x != ultimo.x || primero.z != ultimo.z) borderPoints.Add(primero);
            float dobleArea = 0, f;
            Vector3 centroide = new Vector3(0, 0, 0), punto1, punto2;
            int nPts = borderPoints.Count;
            for (int i = 0, j = nPts - 1; i < nPts; j = i++)
            {
                punto1 = borderPoints[i]; punto2 = borderPoints[j];
                f = punto1.x * punto2.z - punto2.x * punto1.z;
                dobleArea += f;
                centroide.x += (punto1.x + punto2.x) * f;
                centroide.z += (punto1.z + punto2.z) * f;
            }
            f = dobleArea * 3;
            return new Vector3(centroide.x / f, 0.01f, centroide.z / f);
        }
        else
        {
            return new Vector3();
        }
    }
    
    public float Acotan(float x)
    {
        return Mathf.Atan(1.0f / x);
    }

    public void SetArea(float area)
    {
        this.area = area;
    }

    public int GetDivisionLevel()
    {
        return subdivisionLevel;
    }

    public void SetDivisionLevel(int subdivisionLevel)
    {
        this.subdivisionLevel = subdivisionLevel;
    }
    
    public bool GetIsFinalArea()
    {
        return isFinalRegion;
    }

    public void SetIsFinalArea(bool isFinalRegion)
    {
        this.isFinalRegion = isFinalRegion;
    }

    public ZoneType GetZoneType()
    {
        return zoneType;
    }

    public void SetZoneType(ZoneType zoneType)
    {
        this.zoneType = zoneType;
    }
    
    public BuildingType GetBuildingType()
    {
        return buildingType;
    }

    public void SetBuildingType(BuildingType buildingType)
    {
        this.buildingType = buildingType;
    }

    public CropFieldType GetCropFieldType()
    {
        if (buildingType.Equals(BuildingType.CropField))
            return cropFieldType;
        else
            return CropFieldType.Default;
    }

    public void SetCropFieldType(CropFieldType cft)
    {
        cropFieldType = cft;
    }

    public Zone GetZone()
    {
        return zone;
    }

    public void SetZone(Zone zone)
    {
        this.zone = zone;
    }

    public Material GetRoofMaterial()
    {
        return roofMaterial;
    }

    public void SetRoofMaterial(Material mat)
    {
        roofMaterial = mat;
    }
    
    public Material GetFacadeMaterial()
    {
        return facadeMaterial;
    }
    
    public void SetFacadeMaterial(Material mat)
    {
        facadeMaterial = mat;
    }

    public void TriangulateRegion()
    {
        List<Vector2> borderPointsNotRepeated = new List<Vector2>();
        List<Vector3> borderPoints3D = GetBorderPoints();
        //Evitamos repetir el primero y el último
        for (int point = 0; point <borderPoints3D.Count; point++)
        {
            Vector2 pointv2 = borderPoints3D[point].XZ();
            if(!borderPointsNotRepeated.Contains(pointv2))
            {
                borderPointsNotRepeated.Add(pointv2);
            }
        }

        // Use the triangulator to get indices for creating triangles
        Triangulator tr = new Triangulator(borderPointsNotRepeated.ToArray());
        int[] indices = tr.Triangulate();
 
        // Create the Vector3 vertices
        Vector3[] vertices = new Vector3[borderPointsNotRepeated.Count];
        for (int j=0; j<vertices.Length; j++) {
            vertices[j] = new Vector3(borderPointsNotRepeated[j].x, borderPoints3D[0].y,  borderPointsNotRepeated[j].y);
        }
 
        // Create the mesh
        Mesh msh = new Mesh();
        msh.vertices = vertices;

        for (var index = 0; index < indices.Length; index+=3)
        {
            var t1 = indices[index];
            var t2 = indices[index+1];
            var t3 = indices[index+2];

            if(!_Geometry.IsTriangleOrientedClockwise(vertices[t1].ToMyVector2(), vertices[t2].ToMyVector2(), vertices[t3].ToMyVector2()))
            {
                //Debug.DrawLine(vertices[t1], vertices[t2], Color.red, 10f);
                //Debug.DrawLine(vertices[t2], vertices[t3], Color.red, 10f);
                //Debug.DrawLine(vertices[t3], vertices[t1], Color.red, 10f);
            }
                //(indices[index], indices[index+2]) = (indices[index+2], indices[index]);
        }

        msh.triangles = indices;
        msh.RecalculateNormals();
        msh.RecalculateBounds();

        setTriangulatedMesh(msh);
    }

}

[Serializable]
public class RegionsList
{
    [SerializeField] private List<Region> regions;

    public RegionsList(List<Region> regions)
    {
        this.regions = regions;
    }

    public List<Region> GetRegionsList()
    {
        return regions;
    }
}