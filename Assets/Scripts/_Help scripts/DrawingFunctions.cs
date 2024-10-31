using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEditor;
using UnityEngine;


/**
 * This script contains the functions used to draw gizmos that helps to the developer
 * to detect and fix errors of the application
 */
public class DrawingFunctions : MonoBehaviour
{
    // Function that draws the gizmos for the initial polylines
    public static void ShowInitialPolylines(List<InitialStreamline> initialPolylines)
    {
        if (initialPolylines != null && initialPolylines.Count > 0)
        {
            Color color;
            
            foreach (var polyline in initialPolylines)
            {
                if (polyline == null)
                    continue;
                
                switch (polyline.GetType())
                {
                    case Streamline.StreamlineType.River:
                        color = Color.blue;
                        break;
                    case Streamline.StreamlineType.Train:
                        color = new Color(0.8392f, 0.2862f, 0.2f);
                        break;
                    case Streamline.StreamlineType.Street:
                        color = Color.green;
                        break;
                    default:
                        color = Color.black;
                        break;
                }

                if (polyline.GetParent() != null)
                {
                    List<Vector3> polylinePoints = new List<Vector3>();
                    Transform parent = polyline.GetParent();
                    int numPoints = parent.childCount;
                    for (int i = 0; i < numPoints; i++)
                    {
                        polylinePoints.Add(parent.GetChild(i).transform.position);
                    }

                    polyline.SetPoints(polylinePoints);

                    if (polyline.GetPoints() != null && polyline.GetPoints().Count > 1)
                    {
                        List<Vector3> points = polyline.GetPoints();
                        Gizmos.color = color;

                        // We draw the polylines with some thickness (the width of the river, train or highway)
                        for (int i = 0; i < points.Count - 1; i++)
                        {
                            Vector3 startPoint = points[i];
                            Vector3 endPoint = points[i + 1];

                            Vector3 direction = (endPoint - startPoint).normalized;
                            Vector3 perpendicular = new Vector3(direction.z, 0, -direction.x).normalized;

                            // Calculate offsets for the line width
                            Vector3 offset1 = perpendicular * (polyline.GetWidth() / 2f);
                            Vector3 offset2 = -perpendicular * (polyline.GetWidth() / 2f);

                            // Draw two lines for each segment to simulate the width
                            Gizmos.color = color;
                            //Gizmos.DrawSphere(startPoint, 0.0025f);
                            Gizmos.DrawLine(startPoint + offset1, endPoint + offset1);
                            Gizmos.DrawLine(startPoint + offset2, endPoint + offset2);

                            if (i == 0)
                                Gizmos.DrawLine(startPoint + offset1, startPoint + offset2);
                            else if (i == points.Count - 2)
                                Gizmos.DrawLine(endPoint + offset1, endPoint + offset2);
                        }
                    }
                }
            }
        }
    }

    // Function that draws the gizmos for the zones
    public static void ShowZones(List<Zone> zones)
    {
        if (zones != null && zones.Count > 0)
        {
            Color color;

            foreach (var zone in zones)
            {
                switch (zone.GetZoneType())
                {
                    case ZoneType.Downtown:
                        color = new Color(0.5725f, 0.8627f, 0.89804f);
                        break;
                    case ZoneType.IndustrialArea:
                        color = new Color(1f, 0.7294f, 0.03137f);
                        break;
                    case ZoneType.ResidentialArea:
                        color = new Color(0.8157f, 0f, 0f);
                        break;
                    case ZoneType.FieldCrops:
                        color = new Color(0.1f, 0.89f, 0.05f);
                        break;
                    default:
                        color = Color.black;
                        break;
                }

                if (zone.GetParent() != null)
                {
                    Gizmos.color = color;
                    Vector3 position = zone.GetParent().position;
                    Gizmos.DrawSphere(position, 0.05f);

                    switch (zone.GetZoneShape())
                    {
                        case ZoneShape.Circle:
                            DrawWireCircle(position, zone.GetShapeSide(), 15);
                            break;
                        case ZoneShape.Square:
                            DrawWireSquare(zone);
                            break;
                        case ZoneShape.Triangle:
                            DrawWireTriangle(zone);
                            break;
                    }
                }
            }
        }
    }
    
    // Function to draw the UV coordinates of a gameobject
    public static void ShowUVs(GameObject gameObject)
    {
        if (gameObject != null)
        {
            MeshFilter mf = gameObject.GetComponent<MeshFilter>();
            if(!mf)
                return;
            Mesh m = mf.sharedMesh;
            if(!m || m.uv == null || m.uv.Length == 0)
                return;
            for (var i = 0; i < m.vertices.Length; i++)
            {
                var vertex = m.vertices[i];
                Gizmos.color = Color.black;
                Gizmos.DrawLine(vertex, vertex + Vector3.up*0.03f);
                Handles.color = Color.white;
                Handles.Label(vertex + Vector3.up*0.03f,m.uv[i].ToString());
            }
        }
    }

    // Function to draw some region information as ID, area and other elements
    public static void ShowRegionsInformation(List<Region> regions, bool showAllRegions, int regionId, bool debugRegionMesh, bool debugInteriorPoints, bool debugRegionsNumber, bool debugRegionBorder, Color borderColor)
    {
        if (regions != null && regions.Count > 0)
        {
            for (int i = 0; i < regions.Count; i++)
            {
                Region r = regions[i];
                
                //__________________ DRAWING OF REGIONS' ID __________________
                if (debugRegionsNumber)
                {
                    Handles.Label(r.Centroide() + Vector3.up * 0.1f, i.ToString());
                }
                
                if (showAllRegions || i == regionId)
                {
                    Color color = new Color();
                    switch (r.GetZoneType())
                    {
                        case ZoneType.Downtown:
                            color = new Color(0.5725f, 0.8627f, 0.89804f);
                            break;
                        case ZoneType.IndustrialArea:
                            color = new Color(1f, 0.7294f, 0.03137f);
                            break;
                        case ZoneType.ResidentialArea:
                            color = new Color(0.8157f, 0f, 0f);
                            break;
                        case ZoneType.FieldCrops:
                            color = new Color(0.1f, 0.89f, 0.05f);
                            break;
                        default:
                            color = Color.black;
                            break;
                    }

                    Gizmos.color = color;
                    
                    //__________________ DRAWING OF INTERIOR POINTS __________________
                    if (r.GetInteriorPoints() != null && debugInteriorPoints)
                    {
                        List<RandomPoint> randomPointsList = r.GetInteriorPoints().ToList();
                        TestAlgorithmsHelpMethods.DisplayCrosses(Enumerable.ToHashSet(randomPointsList),
                            Gizmos.color);
                    }

                    //__________________ DRAWING OF MESHES __________________
                    if (r.getTriangulatedMesh() != null && debugRegionMesh)
                    {
                        Random.InitState(i);
                        TestAlgorithmsHelpMethods.DisplayMesh(r.getTriangulatedMesh(), Random.ColorHSV());
                    }

                    //__________________ DRAWING OF BORDERS __________________
                    if (r.GetBorderPoints() != null)
                    {
                        List<Vector3> bordes = r.GetBorderPoints();

                        for (int j = 0; j < bordes.Count; j++)
                        {
                            float radio;
                            if (r.GetCornerPoints() != null)
                            {
                                if (r.GetCornerPoints().Contains(j))
                                {
                                    radio = 0.0025f;
                                    Gizmos.color = color;
                                    Gizmos.DrawSphere(bordes[j], radio);
                                }
                                else
                                {
                                    radio = 0.0015f;
                                    Gizmos.color = color;
                                    Gizmos.DrawSphere(bordes[j], radio);
                                }
                            }

                            Gizmos.color = borderColor;
                            Gizmos.DrawLine(bordes[j], bordes[(j + 1) % bordes.Count]);
                        }
                    }

                    //__________________ DRAWING OF BORDERS' ID __________________
                    if (debugRegionBorder)
                    {
                        int count = 0;
                        foreach (var b in r.GetBorderPoints())
                        {
                            Handles.Label(b + Vector3.up * 0.05f, count.ToString());
                            count++;
                        }
                    }
                }
            }
        }
    }

    // Function to draw the streamlines while they are being generated
    public static void ShowStreamlines(List<Region> regions, List<Streamline> streamlines, Streamline finalStreamline)
    {
        if (streamlines != null && streamlines.Count > 0)
        {
            Gizmos.color = Color.magenta;
            foreach (var st in streamlines)
            {
                var pts = st.GetStreamlinePoints();
                for (var i = 0; i < pts.Count - 1; i++)
                {
                    var pt = pts[i];
                    var ptplusOne = pts[i + 1];
                    Gizmos.DrawLine(pt, ptplusOne);
                }
            }

            Gizmos.color = Color.yellow;
            var ptsa = (finalStreamline.GetStreamlinePoints());
            for (var i = 0; i < ptsa.Count - 1; i++)
            {
                var pt = ptsa[i];
                var ptplusOne = ptsa[i + 1];
                Gizmos.DrawLine(pt, ptplusOne);
                Gizmos.DrawSphere(ptplusOne, 0.001f);
                Handles.Label(pt, "" + i);
            }

            if (regions != null && regions.Count > 0 &&
                regions[0].GetStreamline() != null)
            {
                var currentRegionStreamline = regions[0].GetStreamline();
                if (currentRegionStreamline.GetStreamlinePoints() != null)
                {
                    for (var i = 0; i < currentRegionStreamline.GetStreamlinePoints().Count - 1; i++)
                    {
                        var pt = currentRegionStreamline.GetStreamlinePoints()[i];
                        var ptplusOne = currentRegionStreamline.GetStreamlinePoints()[i + 1];
                        Gizmos.DrawLine(pt, ptplusOne);
                    }
                }
            }
        }
    }
    
    // Function to draw a custom circle with 'segments' sides
    public static void DrawWireCircle(Vector3 center, float diameter, int segments)
    {
        // Ensure that the radius is not negative
        float radius = Mathf.Max(0f, diameter / 2f);

        // Calculate the angle between segments
        float angleIncrement = 360f / segments;

        // Draw each segment of the circle
        Vector3 lastPoint = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleIncrement;
            float x = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            float z = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            Vector3 currentPoint = center + new Vector3(x, 0f, z);

            // Draw a line from the last point to the current point
            Gizmos.DrawLine(lastPoint, currentPoint);

            lastPoint = currentPoint;
        }
    }

    // Function to draw a custom square
    public static void DrawWireSquare(Zone zone)
    {
        zone.SetPoints();
        List<Vector2> points = zone.GetPoints();

        if (points.Count == 5)
        {
            // Define the four vertices of the square
            Vector3 topLeft = GeometricFunctions.ConvertVector2ToVector3XZ(points[0]);
            Vector3 topRight = GeometricFunctions.ConvertVector2ToVector3XZ(points[1]);
            Vector3 bottomRight = GeometricFunctions.ConvertVector2ToVector3XZ(points[2]);
            Vector3 bottomLeft = GeometricFunctions.ConvertVector2ToVector3XZ(points[3]);

            // Draw the square by connecting the corners
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
        }
    }
    
    // Function to draw a custom triangle
    public static void DrawWireTriangle(Zone zone)
    {
        zone.SetPoints();
        List<Vector2> points = zone.GetPoints();

        if (points.Count == 4)
        {
            // Define the three vertices of the equilateral triangle
            Vector3 topVertex = GeometricFunctions.ConvertVector2ToVector3XZ(points[0]);
            Vector3 bottomLeftVertex = GeometricFunctions.ConvertVector2ToVector3XZ(points[1]);
            Vector3 bottomRightVertex = GeometricFunctions.ConvertVector2ToVector3XZ(points[2]);

            // Draw the triangle by connecting the vertices
            Gizmos.DrawLine(topVertex, bottomLeftVertex);
            Gizmos.DrawLine(bottomLeftVertex, bottomRightVertex);
            Gizmos.DrawLine(bottomRightVertex, topVertex);
        }
    }
}