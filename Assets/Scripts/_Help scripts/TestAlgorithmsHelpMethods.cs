using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;
using Habrador_Computational_Geometry;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor;
using Random = UnityEngine.Random;

//Display meshes, points, etc so we dont have to do it in each file
public static class TestAlgorithmsHelpMethods
{
    private static int seed;
    //
    // Common help methods
    //

    //Get all child points to a parent transform
    public static List<Vector3> GetPointsFromParent(Transform parentTransform)
    {
        if (parentTransform == null)
        {
            Debug.Log("No parent so cant get children");

            return null;
        }

        //Is not including the parent
        int children = parentTransform.childCount;

        List<Vector3> childrenPositions = new List<Vector3>();

        for (int i = 0; i < children; i++)
        {
            childrenPositions.Add(parentTransform.GetChild(i).position);
        }

        return childrenPositions;
    }

    public static Vector2 CalculateIntersectionBetweenInfiniteLineAndLine(Vector2 infiniteLinePoint1, Vector2 infiniteLinePoint2, Vector2 p3, Vector2 p4)
    {
        double x1 = infiniteLinePoint1.x, y1 = infiniteLinePoint1.y;
        double x2 = infiniteLinePoint2.x, y2 = infiniteLinePoint2.y;
        double x3 = p3.x, y3 = p3.y;
        double x4 = p4.x, y4 = p4.y;

        double denominator = (x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4);

        // Verificar si las líneas son paralelas (denominador cercano a cero)
        if (Math.Abs(denominator) < 1e-6) // Puedes ajustar la tolerancia según sea necesario
        {
            // Devolver un valor especial para indicar que las líneas son paralelas
            return Vector2.positiveInfinity;
        }

        double tNumerator = (x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4);
        double t = tNumerator / denominator;

        double x = x1 + t * (x2 - x1);
        double y = y1 + t * (y2 - y1);

        // Verificar si el punto de intersección cae dentro del segmento de la línea finita
        if (x >= Math.Min(x3, x4) && x <= Math.Max(x3, x4) && y >= Math.Min(y3, y4) && y <= Math.Max(y3, y4))
        {
            return new Vector2((float)x, (float)y);
        }
        else
        {
            // El punto de intersección no está dentro del segmento de la línea finita
            // Devolver un valor especial para indicar que no hay colisión
            return Vector2.positiveInfinity;
        }
        
    }

    
    public static Vector2 CalculateIntersectionBetweenPolygonAndInfiniteLine(Vector2 infiniteLinePoint1, Vector2 infiniteLinePoint2, List<Vector2> polygonPoints)
    {
        List<Vector2> colisions = new List<Vector2>();
        for (var i = 0; i < polygonPoints.Count; i++)
        {
            var p1 = polygonPoints[i];
            var p2 = polygonPoints[(i+1)%polygonPoints.Count];
            var col = CalculateIntersectionBetweenInfiniteLineAndLine(infiniteLinePoint1, infiniteLinePoint2, p1, p2);
            if(col != Vector2.positiveInfinity)
                colisions.Add(col);
        }
        //Ahora debemos filtrar cuál es la que nos interesa
        if (colisions.Count > 0)
        {
            Vector2 goodCollision = colisions.First();
            float goodDistance = Vector2.Distance(infiniteLinePoint1, colisions.First());
            foreach (var col in colisions)
            {
                if (Vector2.Distance(infiniteLinePoint1, col) < goodDistance)
                {
                    goodCollision = col;
                    goodDistance = Vector2.Distance(infiniteLinePoint1, col);
                }
            }

            return goodCollision;
        }
        else
        {
            return Vector2.positiveInfinity;
        }
    }
    //
    // Display shapes with Gizmos
    //

    //Display some points
    public static void DisplayPoints(HashSet<Vector3> points, float radius, Color color)
    {
        if (points == null)
        {
            return;
        }

        Gizmos.color = color;
        foreach (Vector3 p in points)
        {
            //Gizmos.DrawSphere(p, radius);
        }
    }

    public static void DisplayCrosses(HashSet<RandomPoint> h, Color color)
    {
        if (h == null)
            return;
        float width = 0.025f; 
        Gizmos.color = color;
        foreach (RandomPoint r in h)
        {
            Cross c = r.getCross();
            if (c == null)
                continue;
            
            Gizmos.DrawLine((r.getPosition() - c.getV1() * width), (r.getPosition() + c.getV1() * width));
            Gizmos.DrawLine((r.getPosition() - c.getV2() * width), (r.getPosition() + c.getV2() * width));
        }
    }


    //Display an arrow at the end of vector from a to b
    public static void DisplayArrow(Vector3 a, Vector3 b, float size, Color color)
    {
        //We also need to know the direction of the vector, so we need to draw a small arrow
        Vector3 vecDir = (b - a).normalized;

        Vector3 vecDirPerpendicular = new Vector3(vecDir.z, 0f, -vecDir.x);

        Vector3 arrowTipStart = b - vecDir * size;

        //Draw the arrows 4 lines
        Gizmos.color = color;

        //Arrow tip
        Gizmos.DrawLine(arrowTipStart, arrowTipStart + vecDirPerpendicular * size);
        Gizmos.DrawLine(arrowTipStart + vecDirPerpendicular * size, b);
        Gizmos.DrawLine(b, arrowTipStart - vecDirPerpendicular * size);
        Gizmos.DrawLine(arrowTipStart - vecDirPerpendicular * size, arrowTipStart);

        //Arrow line
        Gizmos.DrawLine(a, arrowTipStart);
    }


    //Display triangle
    public static void DisplayTriangleEdges(Vector3 a, Vector3 b, Vector3 c, Color color)
    {
        Gizmos.color = color;

        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, a);
    }


    //Display a plane
    public static void DrawPlane(MyVector2 planePos_2d, MyVector2 planeNormal, Color color)
    {
        Vector3 planeDir = new Vector3(planeNormal.y, 0f, -planeNormal.x);

        Vector3 planePos = planePos_2d.ToVector3();

        //Draw the plane which is just a long line
        float infinite = 100f;

        Gizmos.color = color;

        Gizmos.DrawRay(planePos, planeDir * infinite);
        Gizmos.DrawRay(planePos, -planeDir * infinite);

        //Draw the plane normal
        Gizmos.DrawLine(planePos, planePos + planeNormal.ToVector3() * 1f);
    }


    //Display the edges of a mesh's triangles with some color
    public static void DisplayMeshEdges(Mesh mesh, Color sideColor)
    {
        if (mesh == null)
        {
            return;
        }

        //Display the triangles with a random color
        int[] meshTriangles = mesh.triangles;

        Vector3[] meshVertices = mesh.vertices;

        for (int i = 0; i < meshTriangles.Length; i += 3)
        {
            Vector3 p1 = meshVertices[meshTriangles[i + 0]];
            Vector3 p2 = meshVertices[meshTriangles[i + 1]];
            Vector3 p3 = meshVertices[meshTriangles[i + 2]];

            Gizmos.color = sideColor;

            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p1);
        }
    }

    //
    // Display shapes with Mesh
    //

    //Display some mesh where each triangle could have a random color
    private static void DisplayMesh(Mesh mesh, bool useRandomColor, int seed, Color meshColor)
    {
        if (mesh == null)
        {
            Debug.Log("Cant display the mesh because there's no mesh!");

            return;
        }

        //Display the entire mesh with a single color
        if (!useRandomColor)
        {
            Gizmos.color = meshColor;

            mesh.RecalculateNormals();

            Gizmos.DrawMesh(mesh);

            Gizmos.color = Color.black;
            //Gizmos.DrawWireMesh(mesh);
        }
        //Display the individual triangles with a random color
        else
        {
            int[] meshTriangles = mesh.triangles;

            Vector3[] meshVertices = mesh.vertices;

            Random.InitState(seed);


            for (int i = 0; i < meshTriangles.Length; i += 3)
            {
                //Make a single mesh triangle
                Vector3 p1 = meshVertices[meshTriangles[i + 0]];
                Vector3 p2 = meshVertices[meshTriangles[i + 1]];
                Vector3 p3 = meshVertices[meshTriangles[i + 2]];

                Mesh triangleMesh = new Mesh();

                triangleMesh.vertices = new Vector3[] {p1, p2, p3};

                triangleMesh.triangles = new int[] {0, 1, 2};

                triangleMesh.RecalculateNormals();


                //Color the triangle
                Gizmos.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);

                //float grayScale = Random.Range(0f, 1f);

                //Display it
                Gizmos.DrawMesh(triangleMesh);
            }
        }
    }

    //Just one color
    public static void DisplayMesh(Mesh mesh, Color meshColor)
    {
        seed = 0;

        DisplayMesh(mesh, false, seed, meshColor);
    }

    public static HashSet<RandomPoint> GenerateRandomPointsFromPolygon(int totalN, List<Vector3> shapePoints, float randomFreedom, [CanBeNull] List<Zone> initialZones)
    {
        if (shapePoints != null && shapePoints.Count > 0)
        {
            List<Vector2> shapePoints2D = GeometricFunctions.ConvertListVector3ToListVector2XZ(shapePoints);

            HashSet<RandomPoint> points = new HashSet<RandomPoint>();

            // Calcular el rectángulo mínimo que envuelve el triángulo (MBR)
            Vector4 BB = CalculateBoundingBox(shapePoints);
            Vector2 mbrMin = new Vector2(BB.x, BB.y);
            Vector2 mbrMax = new Vector2(BB.z, BB.w);

            float squareRaw = Mathf.Sqrt(totalN);
            int square = (int) squareRaw;

            // Calcular el tamaño de la celda basado en el número de puntos
            float cellSizeX = (mbrMax.x - mbrMin.x) / square;
            float cellSizeZ = (mbrMax.y - mbrMin.y) / square;

            //Corregiremos square
            RandomPoint[][] borderPoints = new RandomPoint[square][];

            for (int index = 0; index < square; index++)
            {
                borderPoints[index] = new RandomPoint[square];
            }


            bool[][] deletedBorderPoints = new bool[square][];
            for (int index = 0; index < square; index++)
            {
                deletedBorderPoints[index] = new bool[square];
            }

            int i = 0;
            
            //Debemos comparar todas las zonas y obtener aquella con máximo GetMinimumSubdivisionArea
            float maximumPlotSide = -1f;
            if (initialZones != null)
            {
                maximumPlotSide = initialZones.OrderByDescending(zone => zone.GetMaximumPlotSide()).FirstOrDefault().GetMaximumPlotSide();
            }

            // Generar puntos en cada celda
            for (float x = mbrMin.x; x < mbrMax.x; x += cellSizeX)
            {
                int j = 0;
                for (float y = mbrMin.y; y < mbrMax.y; y += cellSizeZ)
                {
                    Vector3 cellCenter = new Vector3(x + cellSizeX / 2, shapePoints[0].y /*Por ejemplo*/, y + cellSizeZ / 2);
                    
                    float multiplyer = 1;
                    float freedom = randomFreedom;
                    
                    //En caso de llamar a la función sin zonas, simplemente se queda: multiplicador = 1
                    if (initialZones != null)
                    {
                        var mulZone = GeometricFunctions.GetZoneOfPoint(initialZones, cellCenter, false);
                        if (mulZone != null)
                        {
                            if(maximumPlotSide > 0f)
                                multiplyer =  maximumPlotSide / mulZone.GetMaximumPlotSide();
                            else
                                multiplyer = 1;
                            freedom = mulZone.GetVoronoiRandomFreedom();
                        }
                    }

                    multiplyer = Mathf.Max(multiplyer, 1);
                    //Bucle para la subdivisión

                    if (multiplyer > 1)
                    {
                        List<Vector3> squareCorners = new List<Vector3>();
                        squareCorners.Add(cellCenter + new Vector3(cellSizeX/2f, 0, cellSizeZ/2f)); //Superior derecha
                        squareCorners.Add(cellCenter + new Vector3(cellSizeX/2f, 0, -cellSizeZ/2f)); //Inferior derecha
                        squareCorners.Add(cellCenter + new Vector3(-cellSizeX/2f, 0, -cellSizeZ/2f)); //Inferior izquierda
                        squareCorners.Add(cellCenter + new Vector3(-cellSizeX/2f, 0, cellSizeZ/2f)); //Superior izquierda
                        points.AddRange(GenerateRandomPointsFromPolygon((int)Math.Pow((int)multiplyer,2),squareCorners,freedom,null));
                    }
                    else if (multiplyer == 1)
                    {
                        Vector3 randomPoint = GenerateRandomPointInSquare(cellCenter, cellSizeX, cellSizeZ, freedom);
                        RandomPoint newR = new RandomPoint(randomPoint);
                        if (i < square && j < square)
                        {
                            if (_Intersections.IsPointInPolygon(shapePoints2D, randomPoint.XZ()))
                            {
                                points.Add(newR);
                            }
                        }
                    }
 
                    j++;
                }

                i++;
            }

            return points;
        }
        else
        {
            Debug.Log("Interior points null");
            return null;
        }
    }
    
    public static Vector4 CalculateBoundingBox(List<Vector3> polygonPoints)
    {
        if (polygonPoints == null || polygonPoints.Count == 0)
        {
            throw new ArgumentException("La lista de puntos del polígono no puede ser nula o vacía.");
        }

        // Inicializar con el primer punto del polígono
        float minX = polygonPoints[0].x;
        float maxX = polygonPoints[0].x;
        float minY = polygonPoints[0].z;
        float maxY = polygonPoints[0].z;

        // Encontrar los límites del polígono
        for (int i = 1; i < polygonPoints.Count; i++)
        {
            Vector3 point = polygonPoints[i];
            minX = Mathf.Min(minX, point.x);
            maxX = Mathf.Max(maxX, point.x);
            minY = Mathf.Min(minY, point.z);
            maxY = Mathf.Max(maxY, point.z);
        }

        // Construir el rectángulo mínimo que envuelve el polígono
        Vector4 boundingBox = new Vector4(minX, minY, maxX, maxY);

        return boundingBox;
    }
    
    public static Vector4 CalculateOrientedBoundingBox(List<Vector3> polygonPoints)
    {
        if (polygonPoints == null || polygonPoints.Count == 0)
        {
            throw new ArgumentException("La lista de puntos del polígono no puede ser nula o vacía.");
        }

        // Calcular el centroide del polígono
        Vector3 centroid = Vector3.zero;
        foreach (Vector3 point in polygonPoints)
        {
            centroid += point;
        }
        centroid /= polygonPoints.Count;

        // Calcular la matriz de covarianza
        Matrix4x4 covarianceMatrix = new Matrix4x4();
        foreach (Vector3 point in polygonPoints)
        {
            Vector3 deviation = point - centroid;
            covarianceMatrix.m00 += deviation.x * deviation.x;
            covarianceMatrix.m01 += deviation.x * deviation.y;
            covarianceMatrix.m02 += deviation.x * deviation.z;
            covarianceMatrix.m10 += deviation.y * deviation.x;
            covarianceMatrix.m11 += deviation.y * deviation.y;
            covarianceMatrix.m12 += deviation.y * deviation.z;
            covarianceMatrix.m20 += deviation.z * deviation.x;
            covarianceMatrix.m21 += deviation.z * deviation.y;
            covarianceMatrix.m22 += deviation.z * deviation.z;
        }
        covarianceMatrix = covarianceMatrix.Multiply(1f/polygonPoints.Count);

        // Calcular los eigenvalores y eigenvectores de la matriz de covarianza
        Vector3 eigenValues;
        Matrix4x4 eigenVectors = new Matrix4x4();
        
        covarianceMatrix.Diagonalize(out eigenValues, out eigenVectors);

        // Encontrar los ejes principales (eigenvectores) y sus longitudes (eigenvalores)
        Vector3 mainAxisX = eigenVectors.GetColumn(0).normalized;
        Vector3 mainAxisY = eigenVectors.GetColumn(1).normalized;
        Vector3 mainAxisZ = eigenVectors.GetColumn(2).normalized;
        float lengthX = Mathf.Sqrt(eigenValues[0]);
        float lengthY = Mathf.Sqrt(eigenValues[1]);
        float lengthZ = Mathf.Sqrt(eigenValues[2]);

        // Calcular los puntos extremos del OBB
        Vector3 corner1 = centroid + mainAxisX * lengthX + mainAxisY * lengthY + mainAxisZ * lengthZ;
        Vector3 corner2 = centroid - mainAxisX * lengthX + mainAxisY * lengthY + mainAxisZ * lengthZ;
        Vector3 corner3 = centroid - mainAxisX * lengthX - mainAxisY * lengthY + mainAxisZ * lengthZ;
        Vector3 corner4 = centroid + mainAxisX * lengthX - mainAxisY * lengthY + mainAxisZ * lengthZ;
        Vector3 corner5 = centroid + mainAxisX * lengthX + mainAxisY * lengthY - mainAxisZ * lengthZ;
        Vector3 corner6 = centroid - mainAxisX * lengthX + mainAxisY * lengthY - mainAxisZ * lengthZ;
        Vector3 corner7 = centroid - mainAxisX * lengthX - mainAxisY * lengthY - mainAxisZ * lengthZ;
        Vector3 corner8 = centroid + mainAxisX * lengthX - mainAxisY * lengthY - mainAxisZ * lengthZ;

        // Proyectar los puntos extremos en el plano XY
        Vector2[] corners2D = new Vector2[]
        {
            new Vector2(corner1.x, corner1.y),
            new Vector2(corner2.x, corner2.y),
            new Vector2(corner3.x, corner3.y),
            new Vector2(corner4.x, corner4.y),
            new Vector2(corner5.x, corner5.y),
            new Vector2(corner6.x, corner6.y),
            new Vector2(corner7.x, corner7.y),
            new Vector2(corner8.x, corner8.y)
        };

        // Encontrar los límites del OBB en el plano XY
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        foreach (Vector2 point in corners2D)
        {
            minX = Mathf.Min(minX, point.x);
            minY = Mathf.Min(minY, point.y);
            maxX = Mathf.Max(maxX, point.x);
            maxY = Mathf.Max(maxY, point.y);
        }

        // Construir el OBB en forma de Vector4 (minX, minY, maxX, maxY)
        return new Vector4(minX, minY, maxX, maxY);
    }
    
    public static bool Diagonalize(this Matrix4x4 matrix, out Vector3 eigenValues, out Matrix4x4 eigenVectors)
    {
        eigenValues = Vector3.zero;
        eigenVectors = Matrix4x4.identity;

        // Check if the matrix is symmetric
        if (!IsSymmetric(matrix))
            return false;

        // Calculate eigenvalues using Jacobi method
        if (!Jacobi(matrix, out eigenValues, out eigenVectors))
            return false;

        // Sort eigenvalues and eigenvectors
        SortEigenValuesAndVectors(ref eigenValues, ref eigenVectors);

        return true;
    }

    private static bool IsSymmetric(Matrix4x4 matrix)
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (Mathf.Abs(matrix[i, j] - matrix[j, i]) > 1e-5f)
                    return false;
            }
        }
        return true;
    }

    private static bool Jacobi(Matrix4x4 matrix, out Vector3 eigenValues, out Matrix4x4 eigenVectors)
    {
        eigenValues = Vector3.zero;
        eigenVectors = Matrix4x4.identity;

        // Max number of iterations
        int maxIterations = 100;
        // Threshold for convergence
        float threshold = 1e-5f;

        Matrix4x4 A = matrix;
        Matrix4x4 V = Matrix4x4.identity;
        Vector4 offdiag = new Vector4(A[1, 0], A[2, 0], A[2, 1], A[3, 0]);
        Vector4 diag = new Vector4(A[0, 0], A[1, 1], A[2, 2], A[3, 3]);

        for (int i = 0; i < maxIterations; i++)
        {
            float maxOffdiag = Mathf.Max(Mathf.Abs(offdiag[0]), Mathf.Abs(offdiag[1]), Mathf.Abs(offdiag[2]), Mathf.Abs(offdiag[3]));
            if (maxOffdiag < threshold)
                break;

            int p, q;
            if (Mathf.Abs(offdiag[0]) > Mathf.Abs(offdiag[1]) && Mathf.Abs(offdiag[0]) > Mathf.Abs(offdiag[2]) && Mathf.Abs(offdiag[0]) > Mathf.Abs(offdiag[3]))
            {
                p = 0; q = 1;
            }
            else if (Mathf.Abs(offdiag[1]) > Mathf.Abs(offdiag[2]) && Mathf.Abs(offdiag[1]) > Mathf.Abs(offdiag[3]))
            {
                p = 1; q = 2;
            }
            else if (Mathf.Abs(offdiag[2]) > Mathf.Abs(offdiag[3]))
            {
                p = 2; q = 3;
            }
            else
            {
                p = 3; q = 0;
            }

            float apq = offdiag[p];
            float app = diag[p];
            float aqq = diag[q];
            float theta = 0.5f * Mathf.Atan2(2 * apq, aqq - app);
            float c = Mathf.Cos(theta);
            float s = Mathf.Sin(theta);
            Matrix4x4 R = Matrix4x4.identity;
            R[p, p] = c;
            R[q, q] = c;
            R[p, q] = s;
            R[q, p] = -s;
            A = R.transpose * A * R;
            V *= R;

            offdiag[p] = A[p, q];
            diag[p] = A[p, p];
            diag[q] = A[q, q];
            offdiag[q] = 0f;
        }

        eigenValues = new Vector3(diag.x, diag.y, diag.z);
        eigenVectors = V;

        return true;
    }

    private static void SortEigenValuesAndVectors(ref Vector3 eigenValues, ref Matrix4x4 eigenVectors)
    {
        // Bubble sort eigenvalues and eigenvectors
        for (int i = 0; i < 3; i++)
        {
            for (int j = i + 1; j < 3; j++)
            {
                if (eigenValues[j] > eigenValues[i])
                {
                    // Swap eigenvalues
                    float tempValue = eigenValues[i];
                    eigenValues[i] = eigenValues[j];
                    eigenValues[j] = tempValue;

                    // Swap eigenvectors
                    Vector4 tempVector = eigenVectors.GetColumn(i);
                    eigenVectors.SetColumn(i, eigenVectors.GetColumn(j));
                    eigenVectors.SetColumn(j, tempVector);
                }
            }
        }
    }
    public static Vector3 FindClosestPointOfGroup(Vector3 point, List<Vector3> points)
    {
        Vector3 closestPoint = Vector3.zero;
        float minDistance = float.MaxValue;

        for (int i = 0; i < points.Count; i++)
        {
            float currentDistance = Vector3.Distance(point, points[i]);

            if (currentDistance < minDistance)
            {
                minDistance = currentDistance;
                closestPoint = points[i];
            }
        }

        return closestPoint;
    }
    
    public static VoronoiEdge2 FindClosestVoronoiEdge(Vector3 point, List<VoronoiEdge2> edges)
    {
        VoronoiEdge2 closestEdge = new VoronoiEdge2(new MyVector2(0,0),new MyVector2(0,0), new MyVector2(0,0));
        float minDistance = float.MaxValue;

        for (int i = 0; i < edges.Count; i++)
        {
            Vector3 currentDistancePoint = FindClosestPointOnEdge(point, edges[i].p1.ToVector3(), edges[i].p2.ToVector3());
            float currentDistance = Vector3.Distance(currentDistancePoint, point);

            if (currentDistance < minDistance)
            {
                minDistance = currentDistance;
                closestEdge = edges[i];
            }
        }

        return closestEdge;
    }
    
    public static Vector3 FindClosestPointOnEdges(Vector3 point, List<Vector3> vertices, bool closeShape = true)
    {
        Vector3 closestPoint = Vector3.zero;
        float minDistance = float.MaxValue;

        for (int i = 0; i < vertices.Count; i++)
        {
            Vector3 edgeVertex1 = vertices[i];
            Vector3 edgeVertex2;
            if(!closeShape && i == vertices.Count - 1)
                continue;
            if (i == vertices.Count - 1)
                edgeVertex2 = vertices[0];
            else
                edgeVertex2 = vertices[i + 1];

            Vector3 currentClosestPoint = FindClosestPointOnEdge(point, edgeVertex1, edgeVertex2);

            ////Debug.DrawLine(point, currentClosestPoint, Color.cyan, 20, false);
            float currentDistance = Vector3.Distance(point, currentClosestPoint);

            if (currentDistance < minDistance)
            {
                minDistance = currentDistance;
                closestPoint = currentClosestPoint;
            }
        }

        return closestPoint;
    }

    public static void debugDrawLines(List<Vector3> positions, Color c, float time, float YOffset = 0f, bool closeShape = false)
    {
        for (var index = 0; index < positions.Count; index++)
        {
            if(index == positions.Count-1 && closeShape == false)
                continue;
            var p1 = positions[index];
            var p2 = positions[(index+1)%positions.Count];
            Debug.DrawLine(p1 + Vector3.up * YOffset, p2 + Vector3.up * YOffset, c, time);
        }
    }
    public static Vector3 FindClosestPointOnEdge(Vector3 point, Vector3 edgeVertex1, Vector3 edgeVertex2)
    {
        Vector3 edgeDirection = edgeVertex2 - edgeVertex1;
        Vector3 pointToVertex1 = point - edgeVertex1;

        float t = Vector3.Dot(pointToVertex1, edgeDirection) / Vector3.Dot(edgeDirection, edgeDirection);

        ////Debug.DrawLine(edgeVertex1, edgeVertex2, Color.yellow, 20, false);
        ////Debug.DrawLine(edgeVertex2 + (edgeVertex2 - edgeVertex1).normalized, edgeVertex2, Color.magenta, 20, false);
        if (t < 0f)
        {
            // El punto proyectado está más allá de vertex1
            return edgeVertex1;
        }
        else if (t > 1f)
        {
            // El punto proyectado está más allá de vertex2
            return edgeVertex2;
        }
        else
        {
            // El punto proyectado está entre vertex1 y vertex2
            return edgeVertex1 + t * edgeDirection;
        }
    }

    public static Vector3 GenerateRandomPointInSquare(Vector3 center, float width, float height, float randomFreedom)
    {
        width *= randomFreedom;
        height *= randomFreedom;
        // Calcular los límites del cuadrado
        float minX = -width / 2f;
        float maxX = width / 2f;
        float minY = -height / 2f;
        float maxY = height / 2f;

        // Elegir coordenadas aleatorias dentro del cuadrado
        float randomX = Random.Range(minX, maxX);
        float randomY = Random.Range(minY, maxY);

        Vector3 finalPos = center + new Vector3(randomX, 0, randomY);
        return finalPos;
    }
    
    public static List<Vector3> GenerateGridPointsInSquare(float minX, float minY, float maxX, float maxY, float cellsizeX, float cellsizeY)
    {
        List<Vector3> gridPoints = new List<Vector3>();

        int nCellsX = Mathf.FloorToInt((maxX - minX) / cellsizeX);
        int nCellsY = Mathf.FloorToInt((maxY - minY) / cellsizeY);

        float adaptedCellSizeX = (maxX - minX) / nCellsX;
        float adaptedCellSizeY = (maxY - minY) / nCellsY;
        for (int x = 0; x < nCellsX; x++)
        {
            for (int y = 0; y < nCellsY; y++)
            {
                Vector3 pos = new Vector3( adaptedCellSizeX * x + adaptedCellSizeX / 2, 0f, adaptedCellSizeY * y + adaptedCellSizeY / 2);
                gridPoints.Add(pos);
            }
        }

        return gridPoints;
    }

    public static Vector3? GenerateRandomPointInTriangle(Vector3 v1, Vector3 v2, Vector3 v3,
        HashSet<RandomPoint> existingPoints, float minDistance)
    {
        int auxSeed = System.DateTime.Now.GetHashCode() + existingPoints.Count;

        if (auxSeed == seed)
            seed++;

        seed = Mathf.Abs(auxSeed);

        Random.InitState(seed);

        Vector3 randomPoint;
        int attempt = 0;
        int maxAttempts = 5000;
        do
        {
            // Generar coordenadas bariocéntricas
            float r1 = Random.Range(0f, 1f);
            float r2 = Random.Range(0f, 1f);

            // Asegurarse de que la suma sea menor o igual a 1
            if (r1 + r2 > 1)
            {
                r1 = 1 - r1;
                r2 = 1 - r2;
            }

            float r3 = 1 - r1 - r2;

            // Calcular las coordenadas del punto dentro del triángulo
            float x = r1 * v1.x + r2 * v2.x + r3 * v3.x;
            float y = r1 * v1.y + r2 * v2.y + r3 * v3.y;
            float z = r1 * v1.z + r2 * v2.z + r3 * v3.z;

            randomPoint = new Vector3(x, y, z);
            attempt++;
        } while (!IsMinDistanceSatisfied(existingPoints, randomPoint, minDistance));

        if (attempt >= maxAttempts)
        {
            Debug.Log("No se ha podido encontrar más ubicaciones para puntos en el triángulo compuesto por " + v1 +
                      " " + v2 + " " + v3);
            return null;
        }

        return randomPoint;
    }

    private static bool IsMinDistanceSatisfied(HashSet<RandomPoint> existingPoints, Vector3 newPoint, float minDistance)
    {
        foreach (RandomPoint existingPoint in existingPoints)
        {
            if (Vector3.Distance(existingPoint.getPosition(), newPoint) < minDistance)
            {
                return false; // No cumple con la distancia mínima
            }
        }

        return true; // Cumple con la distancia mínima
    }


    //Generate random points within a square located at (0,0), so 2d space
    public static HashSet<Vector2> GenerateRandomPoints2D(int seed, float halfSquareSize, int numberOfPoints)
    {
        HashSet<Vector2> randomPoints = new HashSet<Vector2>();

        //Generate random numbers with a seed
        Random.InitState(seed);

        float max = halfSquareSize;
        float min = -halfSquareSize;

        for (int i = 0; i < numberOfPoints; i++)
        {
            float randomX = Random.Range(min, max);
            float randomY = Random.Range(min, max);

            randomPoints.Add(new Vector2(randomX, randomY));
        }

        return randomPoints;
    }
  

    //Generate random points within a cube located at (0,0,0), so 3d space
    public static HashSet<Vector3> GenerateRandomPoints3D(int seed, float halfCubeSize, int numberOfPoints)
    {
        HashSet<Vector3> randomPoints = new HashSet<Vector3>();

        //Generate random numbers with a seed
        Random.InitState(seed);

        float max = halfCubeSize;
        float min = -halfCubeSize;

        for (int i = 0; i < numberOfPoints; i++)
        {
            float randomX = Random.Range(min, max);
            float randomY = Random.Range(min, max);
            float randomZ = Random.Range(min, max);

            randomPoints.Add(new Vector3(randomX, randomY, randomZ));
        }

        return randomPoints;
    }


    //Generate random points on a sphere located at (0,0,0)
    public static HashSet<Vector3> GenerateRandomPointsOnSphere(int seed, float radius, int numberOfPoints)
    {
        HashSet<Vector3> randomPoints = new HashSet<Vector3>();

        //Generate random numbers with a seed
        Random.InitState(seed);

        for (int i = 0; i < numberOfPoints; i++)
        {
            Vector3 posOnSphere = Random.onUnitSphere * radius;

            randomPoints.Add(posOnSphere);
        }

        return randomPoints;
    }


    //
    // Display shapes with //Debug.DrawLine()
    //

    //Display a circle, which doesnt exist built-in - only DrawLine and DrawRay
    public static void DebugDrawCircle(Vector3 center, float radius, Color color)
    {
        Vector3 pos = center + Vector3.right * radius;

        int segments = 12;

        float anglePerSegment = (Mathf.PI * 2f) / (float) segments;

        float angle = anglePerSegment;

        for (int i = 0; i < segments; i++)
        {
            float nextPosX = center.x + Mathf.Cos(angle) * radius;
            float nextPosZ = center.z + Mathf.Sin(angle) * radius;

            Vector3 nextPos = new Vector3(nextPosX, center.y, nextPosZ);

            //Debug.DrawLine(pos, nextPos, color, 2f);

            pos = nextPos;

            angle += anglePerSegment;
        }
    }


    //Display a circle in 3d
    public static void DebugDrawCircle3D(Vector3 center, float radius, Color color)
    {
        Vector3 posR = center + Vector3.right * radius;
        Vector3 posF = center + Vector3.forward * radius;
        Vector3 posU = center + Vector3.right * radius;

        int segments = 12;

        float anglePerSegment = (Mathf.PI * 2f) / (float) segments;

        float angle = anglePerSegment;

        for (int i = 0; i < segments; i++)
        {
            float nextPosX_R = center.x + Mathf.Cos(angle) * radius;
            float nextPosZ_R = center.z + Mathf.Sin(angle) * radius;

            Vector3 nextPosR = new Vector3(nextPosX_R, center.y, nextPosZ_R);

            float nextPosZ_F = center.z + Mathf.Cos(angle) * radius;
            float nextPosY_F = center.y + Mathf.Sin(angle) * radius;

            Vector3 nextPosF = new Vector3(center.x, nextPosY_F, nextPosZ_F);

            float nextPosX_U = center.x + Mathf.Cos(angle) * radius;
            float nextPosY_U = center.y + Mathf.Sin(angle) * radius;

            Vector3 nextPosU = new Vector3(nextPosX_U, nextPosY_U, center.z);

            //Debug.DrawLine(posR, nextPosR, color, 2f);
            //Debug.DrawLine(posF, nextPosF, color, 2f);
            //Debug.DrawLine(posU, nextPosU, color, 2f);

            posR = nextPosR;
            posF = nextPosF;
            posU = nextPosU;

            angle += anglePerSegment;
        }
    }


    //Display a triangle with a normal at the center
    public static void DebugDrawTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal, Color lineColor,
        Color normalColor)
    {
        //Debug.DrawLine(p1, p2, lineColor, 2f);
        //Debug.DrawLine(p2, p3, lineColor, 2f);
        //Debug.DrawLine(p3, p1, lineColor, 2f);

        Vector3 center = _Geometry.CalculateTriangleCenter(p1.ToMyVector3(), p2.ToMyVector3(), p3.ToMyVector3())
            .ToVector3();

        //Debug.DrawLine(center, center + normal, normalColor, 2f);
    }


    //Display a face which we know is a triangle with its normal at the center
    public static void DebugDrawTriangle(HalfEdgeFace3 f, Color lineColor, Color normalColor,
        Normalizer3 normalizer = null)
    {
        MyVector3 p1 = f.edge.v.position;
        MyVector3 p2 = f.edge.nextEdge.v.position;
        MyVector3 p3 = f.edge.nextEdge.nextEdge.v.position;

        if (normalizer != null)
        {
            p1 = normalizer.UnNormalize(p1);
            p2 = normalizer.UnNormalize(p2);
            p3 = normalizer.UnNormalize(p3);
        }

        Vector3 normal = f.edge.v.normal.ToVector3();

        TestAlgorithmsHelpMethods.DebugDrawTriangle(p1.ToVector3(), p2.ToVector3(), p3.ToVector3(), normal * 0.5f,
            Color.white, Color.red);

        //Debug.Log("Displayed Triangle");

        //To test the the triangle is clock-wise
        //TestAlgorithmsHelpMethods.DebugDrawCircle(p1_test, 0.1f, Color.red);
        //TestAlgorithmsHelpMethods.DebugDrawCircle(p2_test, 0.2f, Color.blue);
    }


    //
    // Display data structures
    //

    public static void DisplayMyVector3(MyVector3 v)
    {
        Debug.Log($"({v.x}, {v.y}, {v.z})");
    }
}