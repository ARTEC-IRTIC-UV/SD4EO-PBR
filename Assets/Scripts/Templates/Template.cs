using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using Habrador_Computational_Geometry;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;

public enum PointType
{
    ConcaveCorner,
    ConvexCorner,
    BoundaryVertex,
    InnerVertex
}
[Serializable]
public class Point
{
    [SerializeField] private PointType pointType;
    [SerializeField] private Vector3 position;
    public Point(PointType pointType, Vector3 position)
    {
        this.pointType = pointType;
        this.position = position;
    }

    public PointType GetPointType()
    {
        return pointType;
    }

    public void SetPointType(PointType pointType)
    {
        this.pointType = pointType;
    }

    public Vector3 GetPosition()
    {
        return position;
    }

    public void SetPosition(Vector3 position)
    {
        this.position = position;
    }
    
    public bool isCorner()
    {
        PointType pt = GetPointType();

        if (pt == PointType.ConcaveCorner || pt == PointType.ConvexCorner)
            return true;
        else
            return false;
    }
}

[ExecuteInEditMode]
public class Template: MonoBehaviour
{
    [SerializeField] public List<Point> points;
    [SerializeField] public List<BuildingTemplate> buildings;
    [SerializeField][Range(0.001f,0.5f)] public float clickRange = 0.1f;
    
    private List<Vector3> starterPositions;   //Temporal
    private List<int> temporalSelectedVertices;
    private float cornerAngle;
    private float defaultHeight;

    private void OnEnable()
    {
        if (buildings != null)
        {
            for (var i = 0; i < buildings.Count; i++)
            {
                var b = buildings[i];
                UpdateBuildingMesh(ref b);
            }
        }
    }
    
    

    public Template(List<Point> points, float cornerAngle, float defaultHeight = 0f)
    {
        this.points = points;
        this.cornerAngle = cornerAngle;
        this.defaultHeight = defaultHeight;
    }

    public Vector3 RepositionPoint(Vector3 point, List<Vector3> originalVertices,List<Vector3> newVertices)
    {
        var originalVerticesV2 = GeometricFunctions.ConvertListVector3ToListVector2XZ(originalVertices);
        //Tenemos que triangulizar los bordes de la plantilla original
        Triangulator tr = new Triangulator(originalVerticesV2.ToArray());
        int[] indices = tr.Triangulate();
 
        // Create the Vector3 vertices
        Vector3[] vertices = new Vector3[originalVerticesV2.Count];
        for (int j=0; j<vertices.Length; j++) {
            vertices[j] = new Vector3(originalVerticesV2[j].x,0,  originalVerticesV2[j].y);
        }

        var meshVertices = vertices;
        var meshTriangles = indices;

        Vector3Int? currentTriangleIndices = null;
        Triangle2? currentTriangle = null;
        
        //Primero obtenemos en qué triángulo se encuentra el punto
        for (int i = 0; i < meshTriangles.Length; i+=3)
        {
            var p1 = meshVertices[meshTriangles[i]];
            var p2 = meshVertices[meshTriangles[(i+1)%meshTriangles.Length]];
            var p3 = meshVertices[meshTriangles[(i+2)%meshTriangles.Length]];

            List<Vector2> pointList = new List<Vector2>();
            pointList.Add(p1.XZ());
            pointList.Add(p2.XZ());
            pointList.Add(p3.XZ());
            if (_Intersections.IsPointInTriangle(p1.XZ(), p2.XZ(), p3.XZ(), point.XZ()) ||  _Intersections.PointPolygon(pointList,point.XZ()))
            {
                currentTriangleIndices = new Vector3Int(meshTriangles[i], meshTriangles[(i+1)%meshTriangles.Length], meshTriangles[(i+2)%meshTriangles.Length]);
                currentTriangle = new Triangle2(p1.ToMyVector2(), p2.ToMyVector2(), p3.ToMyVector2());
                break;
            }
        }

        // Calcula las coordenadas baricéntricas

        if (currentTriangleIndices == null || currentTriangle == null)
        {
            Debug.LogError("Error");
            for (int i = 0; i < meshTriangles.Length; i+=3)
            {
                var p1 = newVertices[meshTriangles[i]];
                var p2 = newVertices[meshTriangles[(i+1)%meshTriangles.Length]];
                var p3 = newVertices[meshTriangles[(i+2)%meshTriangles.Length]];

                Debug.DrawLine(p1, p2, Color.red, 10f);
                Debug.DrawLine(p2, p3, Color.red, 10f);
                Debug.DrawLine(p3, p1, Color.red, 10f);
                
                Debug.DrawLine(point, point + Vector3.up * 0.3f, Color.yellow, 10f);
            }
            
            if (_Intersections.IsPointInPolygon(GeometricFunctions.ConvertListVector3ToListVector2XZ(originalVertices), point.XZ()))
                Debug.LogError("El punto está fuera");
        }
        else
        {
            Color c = Color.cyan;

            var pANew = newVertices[currentTriangleIndices.Value.x].XZ();
            var pBNew = newVertices[currentTriangleIndices.Value.y].XZ();
            var pCNew = newVertices[currentTriangleIndices.Value.z].XZ();
            
            var pAOld = originalVertices[currentTriangleIndices.Value.x].XZ();
            var pBOld = originalVertices[currentTriangleIndices.Value.y].XZ();
            var pCOld = originalVertices[currentTriangleIndices.Value.z].XZ();

            // Calcula el área total del triángulo ABC
            float triangleArea = CalculateTriangleArea(pAOld, pBOld, pCOld);

            // Calcula las áreas de los subtriángulos TBC, TAC y TAB
            float areaTBC = CalculateTriangleArea(pBOld, pCOld, point.XZ());
            float areaTAC = CalculateTriangleArea(pAOld, pCOld, point.XZ());
            float areaTAB = CalculateTriangleArea(pAOld, pBOld, point.XZ());

            // Calcula las coordenadas baricéntricas (u, v, w)
            float u = areaTBC / triangleArea;
            float v = areaTAC / triangleArea;
            float w = areaTAB / triangleArea;

            Vector2 pointP = u * pANew + v * pBNew + w * pCNew;
            
            return pointP.XYZ();
        }

        return point;
    }
    float CalculateTriangleArea(Vector2 vertex1, Vector2 vertex2, Vector2 vertex3)
    {
        return Mathf.Abs(0.5f * ((vertex2.x - vertex1.x) * (vertex3.y - vertex1.y) - (vertex3.x - vertex1.x) * (vertex2.y - vertex1.y)));
    }

    public void setHeight(float height)
    {
        defaultHeight = height;
    }
    private void OnDrawGizmos()
    {
        //List<Point> cornerList = points.Where(point => point.isCorner()).ToList();
        //DisplayConnectedPoints(cornerList, Color.black, true);
        if (buildings != null)
        {
            foreach (var b in buildings)
            {
                if (b.DebugMesh != null)
                {
                    Gizmos.color = b.GetBuildingColor();
                    Gizmos.DrawMesh(b.DebugMesh);
                }
            }
            
            foreach (var b in buildings)
            {
                DisplayConnectedPoints(getIndexedPositions(b.GetIndicesList(), b.GetInitialIndex()), Color.white, Color.black);
            }
        }
        
        /*foreach (var p in points)
        {
            Color c = Color.black;
            switch (p.GetPointType())
            {
                case PointType.BoundaryVertex:
                    c = Color.red;
                    break;
                case PointType.ConcaveCorner:
                case PointType.ConvexCorner:
                    c = Color.magenta;
                    break;
                case PointType.InnerVertex:
                    c = Color.yellow;
                    break;
            }

            Gizmos.color = c;
            Gizmos.DrawSphere(p.GetPosition(),0.05f);
        }*/
    }

    public List<Vector3> getIndexedPositions(List<int> indices, int initialIndex)
    {
        List<Vector3> positions = new List<Vector3>();
        
        // Antes de añadir las posiciones, vamos a hacer que el initialIndex sea el primero de la lista
        List<int> orderedIndices = GeometricFunctions.ReorderList(indices, initialIndex);

        foreach (var i in orderedIndices)
        {
            positions.Add(points[i].GetPosition());
        }

        return positions;
    }
    
    public List<Vector3> getBorderPoints()
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (var p in points)
        {
            if(p.GetPointType() != PointType.InnerVertex)
                positions.Add(p.GetPosition());
        }

        return positions;
    }
    
    public List<Vector3> getAllPoints()
    {
        List<Vector3> positions = points.Select(point => point.GetPosition()).ToList();

        return positions;
    }
    
    public List<Region> getBuildingsRegions(ZoneType zoneType)
    {
        List<Region> regions = new List<Region>();
        foreach (var b in buildings)
        {
            Region r = new Region();
            List<Vector3> puntosv3 = getIndexedPositions(b.GetIndicesList(), b.GetInitialIndex());
            puntosv3.Add(puntosv3[0]);
            r.SetBorderPoints(puntosv3);
            r.SetZoneType(zoneType);
            r.SetBuildingType(b.GetBuildingType());
            regions.Add(r);
        }

        return regions;
    }
    public static void DisplayConnectedPoints(List<Vector3> points, Color color, Color colorAux, bool showDirection = false)
    {
        if (points == null)
        {
            return;
        }

        Gizmos.color = color;

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 p1 = points[MathUtility.ClampListIndex(i - 1, points.Count)];
            Vector3 p2 = points[MathUtility.ClampListIndex(i + 0, points.Count)];

            //Direction is important so we should display an arrow show the order of the points
            if (i == 0 && showDirection)
            {
                TestAlgorithmsHelpMethods.DisplayArrow(p1, p2, 0.2f, color);
            }
            else if (i == 1)
            {
                Gizmos.color = colorAux;
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawSphere(p1, 0.1f);
                Gizmos.DrawSphere(p2, 0.1f);
            }
            else
            {
                Gizmos.color = color;
                Gizmos.DrawLine(p1, p2);
            }
        }
    }
    //Display a connected set of points, like a convex hull
    //Can also show direction by displaying a tiny arrow
    public static void DisplayConnectedPoints(List<Point> points, Color color, bool showDirection = false)
    {
        if (points == null)
        {
            return;
        }

        Gizmos.color = color;

        for (int i = 0; i < points.Count; i++)
        {
            Point point1 = points[MathUtility.ClampListIndex(i - 1, points.Count)];
            Point point2 = points[MathUtility.ClampListIndex(i, points.Count)];

            if (point1 == null || point2 == null)
            {
                return;
            }
            Vector3 p1 = points[MathUtility.ClampListIndex(i - 1, points.Count)].GetPosition();
            Vector3 p2 = points[MathUtility.ClampListIndex(i, points.Count)].GetPosition();
            
            //Direction is important so we should display an arrow show the order of the points
            if (i == 0 && showDirection)
            {
                TestAlgorithmsHelpMethods.DisplayArrow(p1, p2, 0.2f, color);
            }
            else
            {
                Gizmos.DrawLine(p1, p2);
            }

            if(points[i].GetPointType() == PointType.BoundaryVertex)
                Gizmos.DrawWireSphere(p2, 0.05f);
            else if (points[i].GetPointType() == PointType.ConcaveCorner || points[i].GetPointType() == PointType.ConvexCorner)
                Gizmos.DrawWireSphere(p2, 0.1f);
        }
    }

    public void CreateBuilding(List<int> pointsIndices, int initialIndex)
    {
        if (buildings == null)
        {
            buildings = new List<BuildingTemplate>();
        }

        BuildingTemplate b = new BuildingTemplate(pointsIndices, initialIndex);

        List<Vector3> puntos = new List<Vector3>();
        foreach (var index in pointsIndices)
            puntos.Add(points[index].GetPosition());
        
        b.DebugMesh = TriangulateBuilding(puntos);
        buildings.Add(b);
    }

    public void UpdateBuildingMesh(ref BuildingTemplate building)
    {

        List<Vector3> puntos = new List<Vector3>();
        foreach (var index in building.GetIndicesList())
            puntos.Add(points[index].GetPosition());
        
        building.DebugMesh = TriangulateBuilding(puntos);
    }
    public Mesh TriangulateBuilding(List<Vector3> points)
    {
        var points2D = GeometricFunctions.ConvertListVector3ToListVector2XZ(points);
        // Use the triangulator to get indices for creating triangles
        Triangulator tr = new Triangulator(points2D.ToArray());
        int[] indices = tr.Triangulate();
 
        // Create the Vector3 vertices
        Vector3[] vertices = new Vector3[points.Count];
        for (int j=0; j<vertices.Length; j++) {
            vertices[j] = new Vector3(points2D[j].x,0,  points2D[j].y);
        }
 
        // Create the mesh
        Mesh msh = new Mesh();
        msh.vertices = vertices;
        msh.triangles = indices;
        msh.RecalculateNormals();
        msh.RecalculateBounds();

        return msh;
    }
    
    public int GetCornerPoints()
    {
        int corners = 0;

        foreach (var p in points)
        {
            if (p.GetPointType() == PointType.ConcaveCorner || p.GetPointType() == PointType.ConvexCorner)
                corners++;
        }
        
        return corners;
    }
    
    public List<Vector3> GetCornersPositions()
    {
        List<Vector3> list = new List<Vector3>();
        foreach (var p in points)
        {
            if(p.isCorner())
                list.Add(p.GetPosition());
        }

        return list;
    }

    public void ClearBuildings()
    {
        buildings = new List<BuildingTemplate>();
    }
    public void ReassignBoundaryPoints(Region r)
    {
        List<int> corners = r.GetCornerPoints();
        List<Vector3> borderPoints = r.GetBorderPoints();
        
        //ClearBoundaryPoints();
        
        List<Point> cornersCopy = new List<Point>();
        List<int> cornersIndex = new List<int>();

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i].isCorner())
            {
                cornersCopy.Add(points[i]);
                cornersIndex.Add(i);
            }
        }

        int indexCount = 0;

        for (int i = 0; i < cornersCopy.Count; i++)
        {
            if(!cornersCopy[i].isCorner())
                continue;
            
            int currentIndex = cornersIndex[i];
            int nextIndex = cornersIndex[(i + 1) % cornersCopy.Count];
            
            List<Point> templatePoints = new List<Point>();
            List<Vector3> regionPoints = new List<Vector3>();

            // Añadimos los puntos entre esquinas
            for (int j = currentIndex; j <= nextIndex; j++)
            {
                if (points[j].GetPointType() != PointType.InnerVertex)
                {
                    templatePoints.Add(points[j]);
                }
            }

            if (i == cornersCopy.Count - 1)
            {
                for (int j = currentIndex; j < points.Count; j++)
                {
                    if (points[j].GetPointType() != PointType.InnerVertex)
                    {
                        templatePoints.Add(points[j]);
                    }
                }
                templatePoints.Add(points[0]);
            }
            
            if (i == corners.Count - 1)
            {
                for (int j = corners[cornersCopy.Count - 1]; j < borderPoints.Count; j++)
                    regionPoints.Add(borderPoints[j]);

                if (corners[0] > 0)
                {
                    for (int j = 0; j <= corners[0]; j++)
                        regionPoints.Add(borderPoints[j]);
                }
            }
            else
            {
                for (int j = corners[i]; j <= corners[i + 1]; j++)
                    regionPoints.Add(borderPoints[j]);
            }
            indexCount += WrapBoundaryPoints(indexCount, regionPoints, templatePoints) - 1;
        }
    }
    
    public int WrapBoundaryPoints(int index, List<Vector3> regionBorderPoints, List<Point> templateBorderPoints)
    {
        for (int i = 0; i < templateBorderPoints.Count; i++)
        {
            if (templateBorderPoints[i].GetPointType() == PointType.InnerVertex)
            {
                continue;
            }
        
            float t = Mathf.InverseLerp(0, templateBorderPoints.Count - 1, i);

            if (index + i < points.Count)
            {
                if (points[index+i].GetPointType() != PointType.InnerVertex)
                {
                    if (!points[index+i].isCorner())
                        points[index+i].SetPosition(GeometricFunctions.InterpolatePolyline(t, regionBorderPoints));
                }
            }
        }

        return templateBorderPoints.Count;
    }

    public List<Vector3> Reordenar(int indice1, int indice2, List<Vector3> vector)
    {
        int n = vector.Count;
        List<Vector3> nuevoVector = new List<Vector3>();

        // Agregar los elementos en los índices indicados al principio del nuevo vector
        nuevoVector.AddRange(vector.GetRange(indice1, n - indice1));
        nuevoVector.AddRange(vector.GetRange(0, indice1));

        // Agregar los elementos que estaban antes de los índices indicados
        nuevoVector.AddRange(vector.GetRange(indice2, n - indice2));
        nuevoVector.AddRange(vector.GetRange(indice1, indice2 - indice1));
        
        // Reemplazar el vector original con el nuevo vector
        return nuevoVector;
    }
    private List<Vector3> getOrderedPointsByPairDistance(List<Vector3> corners)
    {
        List<Vector3> aux = new List<Vector3>();
        //Guardamos los dos índices y en el tercer valor, la distancia entre ellos
        Vector3 largestIndices = new Vector3(0,1, -1);
        for (int i = 0; i < corners.Count; i++)
        {
            int iPlusOne = i + 1;
            if (i == corners.Count - 1)
                iPlusOne = 0;
            
            float dist = Vector3.Distance(corners[iPlusOne],corners[i]);
            if (dist > largestIndices.z)
            {
                largestIndices = new Vector3(i, iPlusOne, dist);
            }
        }
        //Debug.DrawLine(corners[(int)largestIndices.x], corners[(int)largestIndices.y], Color.magenta, 10f);
        //aux = Reordenar((int)largestIndices.x, (int)largestIndices.y, corners);
        return corners;
    }

    public void resetTemporalList()
    {
        if (temporalSelectedVertices != null)
            temporalSelectedVertices = new List<int>();
    }

    public void addTemporalSelectedPoint(int pointIndex)
    {
        if (temporalSelectedVertices == null)
            temporalSelectedVertices = new List<int>();
        //Si no está previamente añadido, añadimos
        if(!temporalSelectedVertices.Contains(pointIndex))
            temporalSelectedVertices.Add(pointIndex);
        
        //Si está ya, cerramos la figura
        else
        {
            //Si es el último, lo contaremos como "borrado"
            if (temporalSelectedVertices.Count > 0 && pointIndex == temporalSelectedVertices.Last())
            {
                temporalSelectedVertices.Remove(pointIndex);
            }
            else if (temporalSelectedVertices.Count > 2)
            {
                CreateBuilding(temporalSelectedVertices, temporalSelectedVertices[0]);
                resetTemporalList();
            }
        }
    }
    
    public List<int> getTemporalSelectedPoints()
    {
        return temporalSelectedVertices;
    }
    public void WrapTemplate(Region r)
    {
        List<Point> cornerPoints = points.Where(point => point.isCorner()).ToList();
        List<Vector3> corners = r.GetCornerPointsV3();
        List<Vector3> orderedList = getOrderedPointsByPairDistance(corners);
        
        if (corners == null || corners.Count == 0)
            return;
        
        if (cornerPoints.Count != corners.Count)
        {
            Debug.Log("Cannot wrap a template in a region with different number of corners. Region has " + corners.Count);
            return;
        }

        for (int i = 0; i < orderedList.Count; i++)
        {
            cornerPoints[i].SetPosition(orderedList[i]);
        }
    }
    
    public int PointsBetween(Vector3 point1, Vector3 point2, float distance)
    {
        float totalDistance = Vector3.Distance(point1, point2);
        int numPoints = Mathf.CeilToInt(totalDistance / distance);
        
        return numPoints;
    }

    public void ClearBoundaryPoints()
    {
        //Borramos los puntos que no sean esquinas para regenerarlos
        List<Point> auxList = new List<Point>();
        for(int i = 0; i < points.Count; i++)
        {
            if (points[i].isCorner())
                auxList.Add(points[i]);
        }

        points = auxList;
    }
    
    // Función para separar los puntos
    public void DetectCorners()
    {
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 prevPoint = points[(i - 1 + points.Count) % points.Count].GetPosition();
            Vector3 currentPoint = points[i].GetPosition();
            Vector3 nextPoint = points[(i + 1) % points.Count].GetPosition();

            Vector3 prevDir = (prevPoint - currentPoint).normalized;
            Vector3 nextDir = (nextPoint - currentPoint).normalized;

            float angle = Vector3.Angle(prevDir, nextDir);

            // Check if the angle is convex
            if (angle > cornerAngle)
                points[i].SetPointType(PointType.ConvexCorner);
        }
    }
}