using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using Habrador_Computational_Geometry;
using JetBrains.Annotations;
using UnityEngine;
using VInspector.Libs;
using Random = UnityEngine.Random;

public class GeometricFunctions : MonoBehaviour
{
    // Convert Vector3 (x,0,z) to Vector2 (x,z)
    public static Vector2 ConvertVector3ToVector2XZ(Vector3 vector3)
    {
        return new Vector2(vector3.x, vector3.z);
    }

    // Convert Vector3 list (x,0,z) to Vector2 list (x,z)
    public static List<Vector2> ConvertListVector3ToListVector2XZ(List<Vector3> vector3s)
    {
        List<Vector2> vector2s = new List<Vector2>();
        foreach (var v in vector3s)
        {
            vector2s.Add(v.XZ());
        }

        return vector2s;
    }
    
    // Convert Vector2 list (x,z) to Vector3 list (x,0,z)
    public static List<Vector3> ConvertListVector2ToListVector3(List<Vector2> vector2s)
    {
        List<Vector3> vector3s = new List<Vector3>();
        foreach (var v in vector2s)
        {
            vector3s.Add(v.XYZ());
        }

        return vector3s;
    }

    // Convert Vector2 (x,z) to Vector3 (x,0,z)
    public static Vector3 ConvertVector2ToVector3XZ(Vector2 vector2)
    {
        return new Vector3(vector2.x, 0, vector2.y);
    }

    // Convert List<Vector3> to List<float[]>
    public static List<float[]> ConvertVector3ToFloat2(List<Vector3> list)
    {
        List<float[]> newList = new List<float[]>();

        for (int i = 0; i < list.Count; i++)
        {
            float[] punto = new float[2];
            punto[0] = list[i].x;
            punto[1] = list[i].z;
            newList.Add(punto);
        }

        return newList;
    }

    // Compute the centroid of a list of points
    public static Vector3 Centroid(List<Vector3> vList)
    {
        Vector3 suma = new Vector3();
        foreach (var v in vList)
        {
            suma += v;
        }

        suma /= vList.Count;

        return suma;
    }

    // Function to get the vectors of the closest neighbour points
    public static List<Cross> GetClosestNeighborVectors(RandomPoint point, List<RandomPoint> allPoints,
        int neighborsCount)
    {
        List<(float d, Cross c)> neighborVectors = new List<(float, Cross)>();

        foreach (var otherPoint in allPoints)
        {
            if (point != otherPoint)
            {
                float distance = Vector3.Distance(point.getPosition(), otherPoint.getPosition());
                neighborVectors.Add((distance, otherPoint.getCross()));
            }
        }

        // Order by distance and take N closest neighbours
        neighborVectors.Sort((v1, v2) => v1.d.CompareTo(v2.d));
        neighborVectors = neighborVectors.Take(neighborsCount).ToList();
        List<Cross> neighborVectorsCrosses = neighborVectors.Select(tuple => tuple.c).ToList();

        return neighborVectorsCrosses;
    }

    public static Cross AverageNeighbourVectors(RandomPoint point, List<RandomPoint> allPoints, int neighborsCount,
        float originalWeight, float neighborsWeight, int mode)
    {
        List<Cross> neighborVectors = GeometricFunctions.GetClosestNeighborVectors(point, allPoints, neighborsCount);

        Vector3 averageVectorV1 = point.getCross().getV1() * originalWeight;
        Vector3 averageVectorV2 = point.getCross().getV2() * originalWeight;

        if (mode == 1)
        {
            foreach (var neighborVector in neighborVectors)
                averageVectorV1 += neighborVector.getV1() * neighborsWeight;

            averageVectorV1.Normalize();
            averageVectorV2 = new Vector3(-averageVectorV1.z, averageVectorV1.y, averageVectorV1.x);
        }

        else if (mode == 2)
        {
            foreach (var neighborVector in neighborVectors)
                averageVectorV2 += neighborVector.getV2() * neighborsWeight;

            averageVectorV2.Normalize();
            averageVectorV1 = new Vector3(-averageVectorV2.z, averageVectorV2.y, averageVectorV2.x);
        }


        return new Cross(averageVectorV1, averageVectorV2); // Normalizar para obtener un vector unitario
    }

    public static List<Vector3> DisplacePoints(List<Vector3> points, float maxDisplacement)
    {
        List<Vector3> displacedPoints = new List<Vector3>();

        foreach (Vector3 point in points)
        {
            // Generate a random direction vector in the XZ plane
            Vector2 randomDirectionXZ = Random.insideUnitCircle.normalized;
            Vector3 randomDirection = new Vector3(randomDirectionXZ.x, 0f, randomDirectionXZ.y);

            // Scale the random direction vector by a random magnitude up to maxDisplacement
            float randomMagnitude = Random.Range(0f, maxDisplacement);
            Vector3 displacement = randomDirection * randomMagnitude;

            // Displace the point
            Vector3 displacedPoint = point + displacement;

            displacedPoints.Add(displacedPoint);
        }

        return displacedPoints;
    }

    public static Vector3 GetVectorAtPosition(Vector3 position, Vector3 orientation, RandomPoint randomPoint, float minDistance)
    {
        if (Vector3.Distance(position, randomPoint.getPosition()) < minDistance)
        {
            Cross c = randomPoint.getCross();
            Vector3 v = c.getMostSimilar(orientation);
            v.Normalize();
            return v;
        }
        else
        {
            return orientation;
        }
    }

    public static List<Vector3> PointsBetween(Vector3 point1, Vector3 point2, float distance, int? minimumPoints = 0)
    {
        List<Vector3> nuevosBordes = new List<Vector3>();

        float totalDistance = Vector3.Distance(point1, point2);
        int numPoints = Mathf.CeilToInt(totalDistance / distance);

        for (int i = 1; i < numPoints; i++)
        {
            float t = Mathf.InverseLerp(0, numPoints, i);
            Vector3 newPoint = Vector3.Lerp(point1, point2, t);
            nuevosBordes.Add(newPoint);
        }

        if (minimumPoints != null && nuevosBordes.Count < minimumPoints)
        {
            nuevosBordes.Clear();
            
            for (int i = 1; i < minimumPoints; i++)
            {
                float t = Mathf.InverseLerp(0, (int)minimumPoints, i);
                Vector3 newPoint = Vector3.Lerp(point1, point2, t);
                nuevosBordes.Add(newPoint);
            }
        }

        return nuevosBordes;
    }

    public static List<Vector3> CutPolylineInRegion(List<Vector2> regionPoints, List<Vector3> polyline)
    {
        List<int> pointsToDelete = new List<int>();
        
        List<Vector3> auxLinea = new List<Vector3>(polyline);

        //Primero debemos encontrar el primer punto de la polilínea que entra a la región
        for (int i = auxLinea.Count-1; i > 0; i--)
        {
            // Si hay colisiones, creamos un punto dentro del polígono
            var colisiones = _Intersections.GetLineIntersectionsWithPolygon(regionPoints, auxLinea[i-1].XZ(), auxLinea[i].XZ());

            if (colisiones != null && colisiones.Count == 2)
            {
                // Obtenemos el punto intermedio de los cortes
                Vector2 puntoMedio = (colisiones[0] + colisiones[1]) / 2f;
                polyline.Insert(i, ConvertVector2ToVector3XZ(puntoMedio));
            }

            if (colisiones != null && colisiones.Count > 2)
            {
                Debug.LogError("Más de dos colisiones");
            }
        }
        
        for (int i = 0; i < polyline.Count; i++)
        {
            bool isPointInPolygon = _Intersections.IsPointInPolygon(regionPoints, polyline[i].XZ(), false);

            if (!isPointInPolygon)
            {
                pointsToDelete.Add(i);
            }
        }

        List<Vector3> auxiliarLine = null;

        for (int i = 0; i < polyline.Count; i++)
        {
            if (!pointsToDelete.Contains(i))
            {
                //EN CASO DE ENTRAR DE NUEVO AL POLÍGONO, EL PRIMER PUNTO DEL SEGMENTO SERÁ EL DE LA COLISIÓN
                if (auxiliarLine == null)
                {
                    auxiliarLine = new List<Vector3>();
                    Vector2? v2 = null;
                    if (i != 0)
                        v2 = _Intersections.IsLineIntersectingPolygon(regionPoints, polyline[i].XZ(), polyline[i - 1].XZ());

                    if (v2 != null)
                        auxiliarLine.Add(ConvertVector2ToVector3XZ(v2.Value));
                }
                
                auxiliarLine.Add(polyline[i]);
            }

            //EN CASO DE SALIR DEL POLÍGONO, EL ÚLTIMO PUNTO DEL SEGMENTO SERÁ EL DE LA COLISIÓN
            else if (pointsToDelete.Contains(i) && auxiliarLine != null)
            {
                Vector2? v2 = _Intersections.IsLineIntersectingPolygon(regionPoints, polyline[i].XZ(), polyline[i - 1].XZ());
                if (v2 != null)
                {
                    auxiliarLine.Add(ConvertVector2ToVector3XZ(v2.Value));
                }
            }
        }
        
        return auxiliarLine;
    }

    public static RandomPoint FindClosestPoint(Vector3 position, HashSet<RandomPoint> rList)
    {
        RandomPoint closestPoint = new RandomPoint(new Vector3(-Single.NegativeInfinity, -Single.NegativeInfinity,
            -Single.NegativeInfinity));
        float closestDistance = float.MaxValue;

        foreach (RandomPoint rpoint in rList)
        {
            float distance = Vector3.Distance(position, rpoint.getPosition());
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = rpoint;
            }
        }

        if (rList.Count > 0)
            return closestPoint;
        else
            return null;
    }
    
    public static Vector3 InterpolatePolyline(float t, List<Vector3> polyline)
    {
        // Check if t is within the range [0, 1]
        if (t < 0 || t > 1)
        {
            throw new ArgumentException("Parameter t must be between 0 and 1");
        }

        // Edge case: if t is 0, return the first point
        if (t == 0)
        {
            return polyline[0];
        }
        // Edge case: if t is 1, return the last point
        else if (t == 1)
        {
            return polyline[polyline.Count - 1];
        }

        // Calculate total length of the polyline
        float totalLength = 0;
        List<float> segmentLengths = new List<float>();
        for (int i = 0; i < polyline.Count - 1; i++)
        {
            float length = Vector3.Distance(polyline[i], polyline[i + 1]);
            totalLength += length;
            segmentLengths.Add(length);
        }

        // Find the segment where the point lies
        float cumulativeLength = 0;
        for (int i = 0; i < segmentLengths.Count; i++)
        {
            cumulativeLength += segmentLengths[i];
            if (t * totalLength <= cumulativeLength)
            {
                // Interpolate between the points
                float ratio = (t * totalLength - (cumulativeLength - segmentLengths[i])) / segmentLengths[i];
                Vector3 interpolatedPoint = new Vector3(
                    polyline[i].x + ratio * (polyline[i + 1].x - polyline[i].x),
                    polyline[i].y + ratio * (polyline[i + 1].y - polyline[i].y),
                    polyline[i].z + ratio * (polyline[i + 1].z - polyline[i].z)
                );
                return interpolatedPoint;
            }
        }

        // If t is 1, return the last point
        return polyline[polyline.Count - 1];
    }

    // Función para detectar si un punto es una esquina (1 -> esquina convexa; -1 -> esquina cóncava; 0 -> no es esquina)
    public static int PointIsCorner(Vector3 point, Vector3 prevPoint, Vector3 nextPoint, float maximumCornerAngle)
    {
        Vector3 prevDir = (prevPoint - point).normalized;
        Vector3 nextDir = (nextPoint - point).normalized;

        float angle = Vector3.Angle(prevDir, nextDir);
        
        if (angle < maximumCornerAngle)
        {
            // Determine the direction of rotation using cross product
            float cross = Vector3.Cross(prevDir, nextDir).y;

            // If cross product is positive, the rotation is counterclockwise
            // If cross product is negative, the rotation is clockwise
            if (cross > 0)
                return 1;
            else
                return -1;
        }
        else
            return 0;
    }

    public static List<Vector3> DisplaceBorderPoints(List<Vector3> borderPoints, float displacement)
    {
        List<Vector3> displacedBorderPoints = new List<Vector3>();

        foreach (var b in borderPoints)
        {
            Vector3 newBorderPoint = new Vector3(b.x, b.y - displacement, b.z);
            displacedBorderPoints.Add(newBorderPoint);
        }

        return displacedBorderPoints;
    }

    public static int PointIsOmitible(Vector3 point, Vector3 prevPoint, Vector3 nextPoint, float tolerance)
    {
        Vector3 prevDir = (prevPoint - point).normalized;
        Vector3 nextDir = (nextPoint - point).normalized;

        float angle = Vector3.Angle(prevDir, nextDir);

        if (angle > 180 - tolerance && angle < 180 + tolerance)
        {
            // Determine the direction of rotation using cross product
            float cross = Vector3.Cross(prevDir, nextDir).y;

            // If cross product is positive, the rotation is counterclockwise
            // If cross product is negative, the rotation is clockwise
            if (cross > 0)
                return 1;
            else
                return -1;
        }
        else
            return 0;
    }
    
    public static Vector3 RotatePoint(Vector3 point, float angleInDegrees)
    {
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;
        float cosAngle = Mathf.Cos(angleInRadians);
        float sinAngle = Mathf.Sin(angleInRadians);

        // Apply 2D rotation matrix in the XY plane
        float xNew = point.x * cosAngle - point.z * sinAngle;
        float zNew = point.x * sinAngle + point.z * cosAngle;

        return new Vector3(xNew, point.y, zNew);
    }
    
    public static HashSet<Vector3> RotatePoints(HashSet<Vector3> points, float angleInDegrees)
    {
        HashSet<Vector3> rotatedPoints = new HashSet<Vector3>();

        foreach (Vector3 point in points)
        {
            rotatedPoints.Add(RotatePoint(point, angleInDegrees));
        }

        return rotatedPoints;
    }

    public static List<Vector3> GetDecimatedPerAngleVertices(List<Vector3> points, float distanceInPointsBetween)
    {
        List<Vector3> newVertices = new List<Vector3>();

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 point = points[i];
            Vector3 prevPoint;
            Vector3 nextPoint;

            if (i == 0)
            {
                //Comprobemos que el polígono no está ya cerrado repitiendo último y primero. En caso de ser así, ignoramos el último
                if (point == points.Last())
                    prevPoint = points[points.Count - 2];
                else
                    prevPoint = points.Last();

                nextPoint = points[i + 1];
            }
            else if (i == points.Count - 1)
            {
                //Comprobemos que el polígono no está ya cerrado repitiendo último y primero. En caso de ser así, ignoramos el último
                if (point == points.First())
                    nextPoint = points[1];
                else
                    nextPoint = points.First();

                prevPoint = points[i - 1];

            }
            else
            {
                prevPoint = points[i - 1];
                nextPoint = points[i + 1];
            }

            int corner = PointIsOmitible(point, prevPoint, nextPoint, 1f);

            // Si es esquina añadimos
            if (corner == 0)
            {
                newVertices.Add(points[i]);
            }
        }

        List<Vector3> finalVertices = new List<Vector3>();

        for (int i = 0; i < newVertices.Count; i++)
        {
            int iPlusOne = MathUtility.ClampListIndex(i + 1, newVertices.Count);

            finalVertices.Add(newVertices[i]);
            if (distanceInPointsBetween > 0)
            {
                finalVertices.AddRange(PointsBetween(newVertices[i], newVertices[(i + 1) % newVertices.Count], distanceInPointsBetween));
            }
                
            //finalVertices.Add(newVertices[iPlusOne]);
        }
        
        return finalVertices;
    }
    
    public static List<int> CalculateCorners(List<Vector3> points, float maximumCornerAngle)
    {
        List<int> cornersChecked = new List<int>();
        if (points.Count < 2)
        {
            return new List<int>();
        }
        else
        {
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 point = points[i];
                Vector3 prevPoint;
                Vector3 nextPoint;

                if (i == 0)
                {
                    //Comprobemos que el polígono no está ya cerrado repitiendo último y primero. En caso de ser así, ignoramos el último
                    if (point == points.Last())
                        prevPoint = points[points.Count - 2];
                    else
                        prevPoint = points.Last();

                    nextPoint = points[i + 1];
                }
                else if (i == points.Count - 1)
                {
                    //Comprobemos que el polígono no está ya cerrado repitiendo último y primero. En caso de ser así, ignoramos el último
                    if (point == points.First())
                        continue;
                    else
                        nextPoint = points.First();

                    prevPoint = points[i - 1];
                }
                else
                {
                    prevPoint = points[i - 1];
                    nextPoint = points[i + 1];
                }

                int corner = PointIsCorner(point, prevPoint, nextPoint, maximumCornerAngle);

                // Si es esquina y no es el último punto repetido añadimos
                if (corner != 0)
                {
                    //Debug.DrawLine(point, point + Vector3.up * 0.1f, Color.yellow, 2f);
                    cornersChecked.Add(i);
                }
            }
        
            // Por último, comprobamos si el primer y último punto coinciden como esquinas. En caso afirmativo, borramos la última
            if (cornersChecked.Count > 0)
            {
                if (points[cornersChecked.First()] == points[cornersChecked.Last()])
                    cornersChecked.RemoveLast();
            }
        }

        return cornersChecked;
    }
    
   public static Vector2Int GetIndexBorderPoint(Vector3 p1, Vector3 p2, List<Vector3> borderPoints)
   {
       Vector2Int result = new Vector2Int(-1, -1);
       int index = 0;
       float closestDistance = float.MaxValue;

       for (int i = 0; i < borderPoints.Count; i++)
       {
           float distance = Vector3.Distance(p1, borderPoints[i]);
           if (distance < closestDistance)
           {
               closestDistance = distance;
               index = i;
           }
       }
       
       // Comprobamos si el índice es el anterior o el posterior para que sea siempre el posterior, ya que queremos insertarlo
       Vector3 direccionRegion = Vector3.Normalize(borderPoints[(index + 1) % borderPoints.Count] - borderPoints[index]);
       Vector3 direccionPunto = Vector3.Normalize(borderPoints[index] - p1);
       float dotProduct = Vector3.Dot(direccionPunto, direccionRegion);

       // Si el producto escalar es -1 (con un margen de error)
       if (dotProduct < -0.9f)
           index++;

       result.x = index;
       
       index = 0;
       closestDistance = float.MaxValue;

       for (int i = 0; i < borderPoints.Count; i++)
       {
           float distance = Vector3.Distance(p2, borderPoints[i]);
           if (distance < closestDistance)
           {
               closestDistance = distance;
               index = i;
           }
       }
       
       // Comprobamos si el índice es el anterior o el posterior para que sea siempre el posterior, ya que queremos insertarlo
       direccionRegion = Vector3.Normalize(borderPoints[(index + 1) % borderPoints.Count] - borderPoints[index]);
       direccionPunto = Vector3.Normalize(borderPoints[index] - p2);
       dotProduct = Vector3.Dot(direccionPunto, direccionRegion);

       // Si el producto escalar es -1 (con un margen de error)
       if (dotProduct < -0.9f)
           index++;

       result.y = index;
       return result;
   }
   
   public static List<Vector3> InvertList(List<Vector3> originalList)
   {
       List<Vector3> invertedList = new List<Vector3>();

       for (int i = originalList.Count - 1; i >= 0; i--)
       {
           invertedList.Add(originalList[i]);
       }

       return invertedList;
   }
   
   public static List<Vector3> ShiftPolygonIndices(List<Vector3> originalPoints, int shiftAmount)
   {
       int pointCount = originalPoints.Count;
       List<Vector3> shiftedPoints = new List<Vector3>();

       // Ensure shiftAmount is within bounds of the point count
       shiftAmount %= pointCount;

       for (int i = 0; i < pointCount; i++)
       {
           int newIndex = (i + shiftAmount) % pointCount;
           shiftedPoints.Add(originalPoints[newIndex]);
       }

       return shiftedPoints;
   }

    public static float CalculatePolylineArea(List<Vector3> polyline)
    {
        double area = 0;
        int n = polyline.Count;

        for (int i = 0; i < n; i++)
        {
            Vector3 p1 = polyline[i];
            Vector3 p2 = polyline[(i + 1) % n]; // To handle the wrap-around to the first point

            area += (p2.x + p1.x) * (p2.z - p1.z);
        }

        return Math.Abs((float)area / 2f);
    }

    public static List<Vector3> getLinePositionsByTransform(Transform line, float borderPointsDistance, int minimumPoints = 0)
    {
        List<Vector3> linea = new List<Vector3>();

        if (line == null)
            return null;
        for (int i = 0; i < line.childCount; i++)
        {
            Vector3 newPosition = line.GetChild(i).transform.position;
            List<Vector3> newPoints = new List<Vector3>();
            if (i != 0)
            {
                if (borderPointsDistance > 0)
                {
                    newPoints = PointsBetween(linea.Last(), newPosition, borderPointsDistance, minimumPoints);

                    foreach (var point in newPoints)
                        linea.Add(point);
                }
            }

            linea.Add(newPosition);
        }

        return linea;
    }

    [CanBeNull]
    public static Zone GetZoneOfPoint(List<Zone> InitialZones, Vector3 point, bool smoothZones = true)
    {
        float maximumScore = 0f;
        Zone currentZone = null;
        Dictionary<Zone, float> zoneScores = new Dictionary<Zone, float>();

        foreach (var zone in InitialZones)
        {
            bool regionInsideZone = false;
            float zoneDistance = Vector3.Distance(point, zone.GetParent().position);

            switch (zone.GetZoneShape())
            {
                case ZoneShape.Circle:
                    if (zoneDistance < zone.GetShapeSide()/2f)
                        regionInsideZone = true;
                    break;

                case ZoneShape.Square: case ZoneShape.Triangle:
                    if (IsPointInsideZone(point.XZ(), zone))
                        regionInsideZone = true;
                    break;
            }
                
            // Solo tenemos en cuenta esta puntuación si la región está dentro de la zona
            if (regionInsideZone)
            {
                // La puntuación depende de 2 factores: distancia al núcleo de zona y fuerza del núcleo
                float actualScore = zone.GetZoneWeight() / zoneDistance;

                zoneScores.Add(zone, actualScore);
                    
                if (actualScore > maximumScore)
                {
                    maximumScore = actualScore;
                    currentZone = zone;
                }
            }
        }

        if (smoothZones)
        {
            // Search zone with probability
            if (zoneScores.Count > 0)
            {
                float sumScores = zoneScores.Values.Sum();
                if (sumScores > 0)
                {
                    for (int i=0; i<zoneScores.Count; i++)
                    {
                        zoneScores[zoneScores.ElementAt(i).Key] /= sumScores;
                    }

                    float randomValue = Random.Range(0f, 1f);
                    for (int i=0; i<zoneScores.Count; i++)
                    {
                        randomValue -= zoneScores[zoneScores.ElementAt(i).Key];
                        if (randomValue < 0)
                        {
                            currentZone = zoneScores.ElementAt(i).Key;
                            break;
                        }
                    }
                }
            }
        }
        
        // Si no cae el punto en ninguna zona, lo calculamos por distancia (sin ajuste de pesos)
        if (currentZone == null)
        {
            float minDistance = float.MaxValue;
            
            foreach (var zone in InitialZones)
            {
                float zoneDistance = Vector3.Distance(point, zone.GetParent().position);
                
                if (zoneDistance < minDistance)
                {
                    minDistance = zoneDistance;
                    currentZone = zone;
                }
            }
        }

        if (currentZone == null)
            currentZone = InitialZones.First();

        return currentZone;
    }

    public static bool CheckIfDistanceIsSatisfyed(List<Vector3> line, Vector3 point, float minimumDistance)
    {
        foreach (var p in line)
        {
            if (Vector3.Distance(p, point) < minimumDistance)
                return false;
        }

        return true;
    }
    
    // Function to displace a polyline in 2D (XZ plane)
    public static List<List<Vector3>> DisplacePolyline(List<Vector3> polyline, float distance, float maximumRandomness = 0f)
    {
        List<Vector3> leftPolyline = new List<Vector3>();
        List<Vector3> rightPolyline = new List<Vector3>();

        if (maximumRandomness > 0f)
        {
            //Vamos a modificar la polilínea para que tenga más puntos
            List<Vector3> newPolyLine = new List<Vector3>();
            for (var index = 0; index < polyline.Count-1; index++)
            {
                var p = polyline[index];
                var pPlusOne = polyline[index+1];
                float distanceBetweenPoints = Mathf.Max(maximumRandomness * 3f, 0.01f);
                var newPoints = GeometricFunctions.PointsBetween(p, pPlusOne, distanceBetweenPoints);
                newPolyLine.Add(p);
                newPolyLine.AddRange(newPoints);
            }
            newPolyLine.Add(polyline.Last());

            polyline = newPolyLine;
        }

        float arcLengthTraveled = 0f; 
        for (int i = 0; i < polyline.Count; i++)
        {
            // Get the current point
            Vector3 point = polyline[i];

            if (i > 0)
                arcLengthTraveled += Vector3.Distance(point, polyline[i - 1]);
            // Calculate the tangent direction (between neighboring points)
            Vector3 tangent;
            if (i == 0)
                tangent = (polyline[i + 1] - point).normalized;
            else if (i == polyline.Count - 1)
                tangent = (point - polyline[i - 1]).normalized;
            else
                tangent = ((polyline[i + 1] - point).normalized + (point - polyline[i - 1]).normalized).normalized;

            // Calculate the normal direction in 2D (perpendicular to the tangent in the XZ plane)
            Vector3 normal = new Vector3(-tangent.z, 0f, tangent.x);
            normal.Normalize();
            // Add random deviation to the normal direction
            float randomness = Mathf.PerlinNoise1D(arcLengthTraveled*10f) * maximumRandomness;
            //normal += normal * randomness;

            // Displace the point along the normal direction
            Vector3 leftPoint = point + normal * (distance / 2f);
            Vector3 rightPoint = point - normal * (distance / 2f);

            leftPoint += normal * randomness;
            rightPoint -= normal * randomness;
            
            leftPolyline.Add(leftPoint);
            rightPolyline.Add(rightPoint);
        }


        // Construct and return the displaced polylines
        List<List<Vector3>> displacedPolylines = new List<List<Vector3>>();
        displacedPolylines.Add(leftPolyline);
        displacedPolylines.Add(rightPolyline);
        
        
        return displacedPolylines;
    }
    
    public static Vector3 CalculateBisector(Vector3 prev, Vector3 current, Vector3 next)
    {
        Vector3 nextEdge = next - current;
        Vector3 previousEdge = current - prev;

        // Calculate the bisector direction
        Vector3 bisector = ((prev - current).normalized + (next - current).normalized) / 2f;

        // Consider only the x and z components for the cross product
        float crossProduct = previousEdge.x * nextEdge.z - previousEdge.z * nextEdge.x;

        if (crossProduct < 0)
            bisector.Scale(new Vector3(-1f, -1f, -1f));
                
        // Normalize the bisector
        bisector.Normalize();

        return bisector;
    }
    
    public static List<Vector3> GetOutsidePolygonRegion(List<Vector3> polygon, List<int> corners, float distance)
    {
        List<Vector3> insideCorners = new List<Vector3>();
        List<Vector3> insidePolygon = new List<Vector3>();

        // Remove the last point, since it's repeated
        if (polygon.First() == polygon.Last())
            polygon.RemoveAt(polygon.Count - 1);

        int numPoints = polygon.Count;
        
        for (int i = 0; i < numPoints; i++)
        {
            // Solo tenemos en cuenta las esquinas
            if (corners.Contains(i))
            {
                Vector3 point = polygon[i];
                Vector3 prevPoint = polygon[(i + numPoints - 1) % numPoints];
                Vector3 nextPoint = polygon[(i + 1) % numPoints];

                Vector3 nextEdge = nextPoint - point;
                Vector3 previousEdge = point - prevPoint;

                // Calculate the bisector direction
                Vector3 bisector = ((prevPoint - point).normalized + (nextPoint - point).normalized) / 2f;

                // Consider only the x and z components for the cross product
                float crossProduct = previousEdge.x * nextEdge.z - previousEdge.z * nextEdge.x;

                if (crossProduct < 0)
                    bisector.Scale(new Vector3(-1f, -1f, -1f));
                
                // Normalize the bisector
                bisector.Normalize();

                Vector3 newPoint = point + bisector * distance/2f;
                
                insideCorners.Add(newPoint);
            }
        }
        
        insidePolygon = GetUpdatedPolygonWithIntermediatePoints(0, insideCorners);

        // Close the regions -> En este caso debemos recorrer el polígono interior, y salir al polígono exterior para recorrerla y formar el borde.
        if (insidePolygon.Count > 0)
        {
            List<Vector3> contorno = new List<Vector3>();
            
            if(insidePolygon.Last() != insidePolygon.First())
                insidePolygon.Add(insidePolygon[0]);

            insidePolygon.Reverse();
            contorno.AddRange(insidePolygon);
            
            Vector3 finalInteriorPoint = new Vector3(insidePolygon.Last().x, insidePolygon.Last().y, insidePolygon.Last().z);
            int connectingInteriorToExteriorPoint = insidePolygon.Count;
            
            for (var i = 0; i < polygon.Count; i++)
            {
                var p = polygon[i];
                var pPlusOne = polygon[(i+1/polygon.Count)];
                contorno.Add(p);
            }

            contorno.Add(polygon.First());
            contorno.Add(finalInteriorPoint);


            int startingInteriorPoint = 0;
            
            
            //El vértice por el que hayamos empezado estará añadido 3 veces como vértice, mientras que el vértice exterior que se une al interior estará referenciado dos veces. Todos los demás estarán añadidos una única vez.
            
            //Creamos los dos primeros triángulos.
            Triangle3 tri1 = new Triangle3(contorno[startingInteriorPoint], contorno[1], contorno[connectingInteriorToExteriorPoint]);
            Triangle3 tri2 = new Triangle3(contorno[connectingInteriorToExteriorPoint], contorno[1], contorno[connectingInteriorToExteriorPoint + 1]);
            
            Debug.DrawLine(tri1.p1.ToVector3(), tri1.p2.ToVector3(), Color.black, 10f);
            Debug.DrawLine(tri1.p2.ToVector3(), tri1.p3.ToVector3(), Color.black, 10f);
            Debug.DrawLine(tri1.p3.ToVector3(), tri1.p1.ToVector3(), Color.black, 10f);
            
            //Debug.DrawLine(tri2.p1.ToVector3(), tri2.p2.ToVector3(), Color.green, 10f);
            //Debug.DrawLine(tri2.p2.ToVector3(), tri2.p3.ToVector3(), Color.green, 10f);
            //Debug.DrawLine(tri2.p3.ToVector3(), tri2.p1.ToVector3(), Color.green, 10f);
            return contorno;
        }
        else
        {
            Debug.Log("No se ha hecho región exterior");
            return null;
        }
    }
    public static List<Vector3> DisplacePolygonInside(List<Vector3> polygon, List<int> corners, float distance, int nStreamlines, bool debug = false)
    {
        List<Vector3> insideCorners = new List<Vector3>();
        List<Vector3> insidePolygon = new List<Vector3>();

        // Remove the last point, since it's repeated
        if (polygon.First() == polygon.Last())
            polygon.RemoveAt(polygon.Count - 1);

        int numPoints = polygon.Count;
        
        for (int i = 0; i < numPoints; i++)
        {
            // Solo tenemos en cuenta las esquinas
            if (corners.Contains(i))
            {
                Vector3 point = polygon[i];
                Vector3 prevPoint = polygon[(i + numPoints - 1) % numPoints];
                Vector3 nextPoint = polygon[(i + 1) % numPoints];

                Vector3 nextEdge = nextPoint - point;
                Vector3 previousEdge = point - prevPoint;

                // Calculate the bisector direction
                Vector3 bisector = ((prevPoint - point).normalized + (nextPoint - point).normalized) / 2f;

                // Consider only the x and z components for the cross product
                float crossProduct = previousEdge.x * nextEdge.z - previousEdge.z * nextEdge.x;

                if (crossProduct < 0)
                    bisector.Scale(new Vector3(-1f, -1f, -1f));
                
                // Normalize the bisector
                bisector.Normalize();

                Vector3 newPoint = point + bisector * distance/2f;
                
                insideCorners.Add(newPoint);
            }
        }

        insidePolygon = GetUpdatedPolygonWithIntermediatePoints(nStreamlines, insideCorners);

        // Close the regions
        if (insidePolygon.Count > 0)
        {
            if(insidePolygon.Last() != insidePolygon.First())
                insidePolygon.Add(insidePolygon[0]);
            return insidePolygon;
        }
        else
        {
            Debug.Log("No se ha hecho región interior");
            return null;
        }
    }
    
    public static List<int> ReorderList(List<int> originalList, int targetValue)
    {
        // Find the index of the target value in the list
        int index = originalList.IndexOf(targetValue);

        // If the target value is not found, return the original list
        if (index == -1)
        {
            //Debug.LogWarning("Target value not found in the list.");
            return originalList;
        }

        // Create a new list to hold the reordered elements
        List<int> reorderedList = new List<int>();

        // Add the elements starting from the target value to the end of the original list
        for (int i = index; i < originalList.Count; i++)
        {
            reorderedList.Add(originalList[i]);
        }

        // Add the elements from the beginning of the original list up to the target value
        for (int i = 0; i < index; i++)
        {
            reorderedList.Add(originalList[i]);
        }

        return reorderedList;
    }

    // Check what ratio the region has (larger side divided shorter side)
    public static float CheckRegionRatio(List<Vector3> corners)
    {
        float ratio = 1f;
        
        if (corners.Count > 2)
        {
            float maxLength = (corners[0] - corners[1]).magnitude;
            float minLength = (corners[0] - corners[1]).magnitude;

            for (int i = 1; i < corners.Count; i++)
            {
                float length = (corners[i] - corners[(i + 1) % corners.Count]).magnitude;
                
                // Check larger side
                if (length > maxLength)
                    maxLength = length;
                
                // Check shorter side
                if (length < minLength)
                    minLength = length;
            }

            ratio = maxLength / minLength;
        }

        return ratio;
    }

    public static List<Vector3> ExtendPolyline(List<Vector3> polyline, List<Vector3> region, int contadorPolilineas)
    {
        if (polyline.Count > 1)
        {
            List<Vector2> regionV2 = ConvertListVector3ToListVector2XZ(region);

            // Obtenemos los puntos clave para cada extremo de la polilínea
            Vector3 firstPoint = polyline[0];
            Vector3 secondPoint = polyline[1];
            Vector3 secondLastPoint = polyline[polyline.Count - 2];
            Vector3 lastPoint = polyline[polyline.Count - 1];
        
            // Primero, comprobamos el inicio de la línea
            if (_Intersections.IsPointInPolygon(regionV2, firstPoint.XZ()))
            {
                // Calculamos la distancia al punto de borde más cercano e insertamos un nuevo punto para cortar la línea
                Vector3 border = TestAlgorithmsHelpMethods.FindClosestPointOnEdges(firstPoint, region);
                
                float dist = Vector3.Distance(border, firstPoint);
                Vector3 direction = Vector3.Normalize(firstPoint - secondPoint);
                Vector3 newPoint = firstPoint + direction * dist;
                int contador = 0;

                // Mientras no haya intersección (salida de la polilínea fuera de la región)
                while (_Intersections.IsLineIntersectingPolygon(regionV2, newPoint.XZ(), (newPoint + direction * dist).XZ()) == null && contador < contadorPolilineas)
                {
                    contador++;
                    newPoint += direction * dist;
                }

                polyline.Insert(0, newPoint);
            }
            
            // Después, comprobamos el final de la línea con el mismo proceso
            if (_Intersections.IsPointInPolygon(regionV2, lastPoint.XZ()))
            {
                // Calculamos la distancia al punto de borde más cercano e insertamos un nuevo punto para cortar la línea
                Vector3 border = TestAlgorithmsHelpMethods.FindClosestPointOnEdges(lastPoint, region);
                float dist = Vector3.Distance(border, lastPoint);
                Vector3 direction = Vector3.Normalize(lastPoint - secondLastPoint);
                Vector3 newPoint = lastPoint + direction * dist;
                int contador = 0;

                // Mientras no haya intersección (salida de la polilínea fuera de la región)
                while (_Intersections.IsLineIntersectingPolygon(regionV2, newPoint.XZ(), (newPoint + direction * dist).XZ()) == null && contador < contadorPolilineas)
                {
                    contador++;
                    newPoint += direction * dist;
                }

                polyline.Add(newPoint);
            }
        }

        return polyline;
    }
    
    public static List<Vector3> GetUpdatedPolygonWithIntermediatePoints(int nStreamlines, List<Vector3> cornerPoints)
    {
        //Para ello, simplemente debemos recorrer el perímetro y decidir, para cada segmento, cuántos puntos se generarán
        float totalDistance = 0f;

        List<Vector3> auxVertexList = new List<Vector3>();
        //Calculamos la lonmgitud total del perímetro
        for (var i = 0; i < cornerPoints.Count; i++)
        {
            var corner = cornerPoints[i];
            var nextCorner = cornerPoints[(i+1)%cornerPoints.Count] ;

            totalDistance += Vector3.Distance(corner, nextCorner);
        }

        float distanceBetweenPointsAdapted = -1;
        if (nStreamlines > 0)
        {
            distanceBetweenPointsAdapted = totalDistance / nStreamlines;
        }
        
        for (var i = 0; i < cornerPoints.Count; i++)
        {
            var corner = cornerPoints[i];
            var nextCorner = cornerPoints[(i+1)%cornerPoints.Count] ;
            
            auxVertexList.Add(corner);
            if (distanceBetweenPointsAdapted > -1)
            {
                List<Vector3> newPoints = PointsBetween(corner, nextCorner, distanceBetweenPointsAdapted, 1);
                auxVertexList.AddRange(newPoints);
            }
            else
            {
                auxVertexList.Add(corner);
            }
            
        }

        return auxVertexList;
    }

    public static bool IsPointInsideZone(Vector2 point, Zone zone)
    {
        // Check if the point is inside the triangle
        List<Vector2> points = zone.GetPoints();
        if (points == null || points.Count == 0)
            return false;
        bool insideZone = _Intersections.IsPointInPolygon(points, point, false);
        
        return insideZone;
    }

    public static Vector2 RotatePoint(Vector2 point, Vector2 pivot, float angle)
    {
        float cosAngle = Mathf.Cos(Mathf.Deg2Rad * angle);
        float sinAngle = Mathf.Sin(Mathf.Deg2Rad * angle);
        float x = cosAngle * (point.x - pivot.x) - sinAngle * (point.y - pivot.y) + pivot.x;
        float y = sinAngle * (point.x - pivot.x) + cosAngle * (point.y - pivot.y) + pivot.y;
        return new Vector2(x, y);
    }

    public static List<Vector3> ClippingRegion(List<Vector3> region)
    {
        int[] indices = new int[4];
        Vector2? point = null;
        
        bool interseccion = IsPolygonIntersectingItself(ConvertListVector3ToListVector2XZ(region), out indices, out point);

        while (interseccion)
        {
            //Debug.Log("HAY RECORTE");
            int regionPointsCount = region.Count;
            int corte1_1 = indices[0];
            int corte1_2 = indices[1];
            int corte2_1 = indices[2];
            int corte2_2 = indices[3];
            Vector3 corte = new Vector3(point.Value.x, 0f, point.Value.y);
            //Debug.DrawLine(corte, corte + Vector3.up * 0.5f, Color.yellow, 5f);
            
            // Para ver qué área debemos cortar, vamos a comparar las dos polilíneas que se crean con el corte.
            // Como los índices siempre van a estar ordenados de mayor a menor, se crean fácilmente
            List<Vector3> firstRegion = new List<Vector3>();
            List<Vector3> secondRegion = new List<Vector3>();
            
            for (int i = 0; i < regionPointsCount; i++)
            {
                if (i <= corte1_1 || i >= corte2_2)
                {
                    firstRegion.Add(region[i]);
                    
                    // Añadimos el punto de corte
                    if (i == corte1_1)
                        firstRegion.Add(corte);
                }
                else
                {
                    // Añadimos el punto de corte
                    if (i == corte1_2)
                        secondRegion.Add(corte);
                    
                    secondRegion.Add(region[i]);
                    
                    // Añadimos el punto de corte para cerrar la región
                    if (i == corte2_1)
                        secondRegion.Add(corte);
                }
            }

            // Una vez tenemos las dos regiones comparamos las áreas, nos quedamos con la mayor
            float firstArea = CalculatePolylineArea(firstRegion);
            float secondArea = CalculatePolylineArea(secondRegion);

            if (firstArea >= secondArea)
                region = firstRegion;
            else
                region = secondRegion;
            
            // Comprobamos que no haya más intersecciones
            interseccion = IsPolygonIntersectingItself(ConvertListVector3ToListVector2XZ(region), out indices, out point);
        }

        return region;
    }

    public static bool IsPolygonIntersectingItself(List<Vector2> polygon, out int[] intersectingIndexes, out Vector2? intersectingPoint)
    {
        intersectingIndexes = null; // Initialize the output array
        intersectingPoint = null;

        int pointCount = polygon.Count;

        // Iterate through each edge of the polygon
        for (int i = 0; i < pointCount - 3; i++)
        {
            Vector2 p1 = polygon[i];
            Vector2 p2 = polygon[i + 1];

            // Check for intersections with segments after the current segment
            for (int j = i + 2; j < pointCount - 1; j++)
            {
                Vector2 p3 = polygon[j];
                Vector2 p4 = polygon[(j + 1) % pointCount]; // Wrap around to the first point

                // Check if the edges intersect and get the intersection point
                Vector2? intersectionPoint = _Intersections.AreLinesIntersecting(p1, p2, p3, p4,true);

                if (intersectionPoint != null)
                {
                    if (i == 0 && j + 1 == pointCount - 1)
                    {
                    }
                    else
                    {
                        // Intersection found, store the indexes of the intersecting points
                        intersectingIndexes = new int[] { i, i + 1, j, (j + 1) % pointCount };
                        intersectingPoint = intersectionPoint;
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    public static bool IsPolygonClockwise(List<Vector2> polygonVertices)
    {
        float sum = 0f;
        int numVertices = polygonVertices.Count;

        for (int i = 0; i < numVertices; i++)
        {
            Vector2 currentVertex = polygonVertices[i];
            Vector2 nextVertex = polygonVertices[(i + 1) % numVertices]; // Next vertex

            sum += (nextVertex.x - currentVertex.x) * (nextVertex.y + currentVertex.y);
        }

        return sum < 0f; // If the total cross product is negative, the polygon is counterclockwise
    }
}