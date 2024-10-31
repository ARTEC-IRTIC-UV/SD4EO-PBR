using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Habrador_Computational_Geometry;
using Unity.VisualScripting;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

/*
 * Script that manages the information of the voronoi diagram that creates an initial division
 * of regions in the map. The cells will be larger or smaller depending on the zones
 */
public class VoronoiController : MonoBehaviour
{
    // ATTRIBUTES
    private RegionsController regionsController = RegionsController.GetInstance();
    private int numberOfPointsPerSide = 20;
    [Range(0.2f, 1f)] private float cellsFactor = 0.3f;
    private float finalRegionsDistanceBetweenpoints = 0.5f;
    private List<Region> regionList;
    private List<List<VoronoiCell2>> voronoiCellsPerRegion;
    [DoNotSerialize]
    List<VoronoiCell2> voronoiCellsToDelete;
    List<VoronoiCell2> voronoiCellsToStay;
    private List<VoronoiCell2> voronoiCellsToCheck;
    private List<VoronoiCell2> voronoiErrorCells;
    public bool drawDeletedCells;
    public bool drawCorrectCells;
    public bool drawToCheckCells;
    public bool drawErrorCells;
    public bool drawCellSites;
    Color[] colores = { Color.red, Color.yellow, Color.green, Color.blue, Color.gray, Color.magenta, Color.white, Color.black, };

    // Function to reset the information of the voronoi diagram
    public void ResetAll()
    {
        regionList = new List<Region>();
        voronoiCellsPerRegion = new List<List<VoronoiCell2>>();
        voronoiCellsToDelete = new List<VoronoiCell2>();
        voronoiCellsToStay = new List<VoronoiCell2>();
        voronoiCellsToCheck = new List<VoronoiCell2>();
        voronoiErrorCells = new List<VoronoiCell2>();
    }

    // Function to draw the gizmos of the voronoi diagram (help to develop)
    private void OnDrawGizmos()
    {
        if (drawCellSites && voronoiCellsPerRegion != null && voronoiCellsPerRegion.Count > 0 && voronoiCellsPerRegion.Last() != null)
        {
            int cellCount = 0;
            foreach (var cell in voronoiCellsPerRegion.Last())
            {
                Gizmos.DrawSphere(cell.sitePos.ToVector3(), 0.05f);
                cellCount++;
            }
        }
        
        if (voronoiCellsToStay != null && voronoiCellsToCheck != null && voronoiCellsToDelete != null && voronoiErrorCells != null)
        {
            if (drawCorrectCells)
            {
                for (int z = 0; z < voronoiCellsToStay.Count; z++)
                {
                    int edgeCount = 0;
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(voronoiCellsToStay[z].sitePos.ToVector3(), 0.01f);
                    foreach (var edge in voronoiCellsToStay[z].edges)
                    {
                        Vector3 p1a = edge.p1.ToVector3();
                        Vector3 p2a = edge.p2.ToVector3();
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(p1a, p2a);
                        edgeCount++;
                    }
                }
            }
            
            if (drawDeletedCells)
            {
                for (int z = 0; z < voronoiCellsToDelete.Count; z++)
                {
                    int edgeCount = 0;
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(voronoiCellsToDelete[z].sitePos.ToVector3(), 0.01f);
                    foreach (var edge in voronoiCellsToDelete[z].edges)
                    {
                        Vector3 p1a = edge.p1.ToVector3();
                        Vector3 p2a = edge.p2.ToVector3();
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(p1a, p2a);
                        edgeCount++;
                    }
                }
            }

            if (drawToCheckCells)
            {
                for (int z = 0; z < voronoiCellsToCheck.Count; z++)
                {
                    int edgeCount = 0;
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(voronoiCellsToCheck[z].sitePos.ToVector3(), 0.01f);
                    foreach (var edge in voronoiCellsToCheck[z].edges)
                    {
                        Vector3 p1a = edge.p1.ToVector3();
                        Vector3 p2a = edge.p2.ToVector3();
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(p1a, p2a);
                        edgeCount++;
                    }
                }
            }
            
            if (drawErrorCells)
            {
                for (int z = 0; z < voronoiErrorCells.Count; z++)
                {
                    int edgeCount = 0;
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(voronoiErrorCells[z].sitePos.ToVector3(), 0.01f);
                    foreach (var edge in voronoiErrorCells[z].edges)
                    {
                        Vector3 p1a = edge.p1.ToVector3();
                        Vector3 p2a = edge.p2.ToVector3();
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawLine(p1a, p2a);
                        edgeCount++;
                    }
                }
            }
        }
    }
    
    // Function to calculate and generate the voronoi diagram
    public void calculateVoronoi(List<Region> regions)
    {
        Random.InitState(System.DateTime.Now.Millisecond);
        voronoiCellsPerRegion = new List<List<VoronoiCell2>>();
        regionList = regions;

        foreach (var region in regionList)
        {
            var border3D = region.GetBorderPointsWithAngleDecimate(-1);
            var border2D = GeometricFunctions.ConvertListVector3ToListVector2XZ(border3D);

            // Calcular el rectángulo mínimo que envuelve el triángulo (MBR)
            Vector4 BB = TestAlgorithmsHelpMethods.CalculateBoundingBox(border3D);
            Vector2 mbrMin = new Vector2(BB.x, BB.y);
            Vector2 mbrMax = new Vector2(BB.z, BB.w);

            List<Vector3> box = new List<Vector3>();
            
            // El número de puntos INICIAL de voronoi los sacamos en función del área INICIAL y de la zona con mayor plotySide
            //El multiplicador dependerá además del tamaño máximo de lado de la zona con el mayor valor en el campo maximumPlotSide.
            
            if (regionsController == null)
                regionsController = RegionsController.GetInstance();
                
            float maximumPlotSide = regionsController.GetInitialZones().OrderByDescending(zone => zone.GetMaximumPlotSide()).FirstOrDefault().GetMaximumPlotSide();
            float nX = (mbrMax.x - mbrMin.x);
            float nY = (mbrMax.y - mbrMin.y);
            
            float maximumBBDistance = Mathf.Max(mbrMax.x - mbrMin.x, mbrMax.y - mbrMin.y);
            float numberOfPointsPerSizef = Mathf.Min(nX, nY) / (maximumPlotSide);
            numberOfPointsPerSide = (int)  (numberOfPointsPerSizef * cellsFactor);
            numberOfPointsPerSide = (int) Mathf.Max(numberOfPointsPerSide, 3);
            
            // Calcular el tamaño de la celda basado en el número de puntos
            float cellSize = maximumBBDistance / maximumPlotSide * 0.001f;
            float offset = cellSize*10f;
            Vector3 v1 = new Vector3(mbrMin.x - offset, 0, mbrMax.y + offset);
            Vector3 v2 = new Vector3(mbrMin.x - offset, 0, mbrMin.y - offset);
            Vector3 v3 = new Vector3(mbrMax.x + offset, 0, mbrMin.y - offset);
            Vector3 v4 = new Vector3(mbrMax.x + offset, 0, mbrMax.y + offset);

            box.Add(v1);
            box.Add(v2);
            box.Add(v3);
            box.Add(v4);
            
            Vector3 v5 = new Vector3(mbrMin.x - offset*2f, 0, mbrMax.y + offset*2f);
            Vector3 v6 = new Vector3(mbrMin.x- offset*2f, 0, mbrMin.y - offset*2f);
            Vector3 v7 = new Vector3(mbrMax.x+ offset*2f, 0, mbrMin.y - offset*2f);
            Vector3 v8= new Vector3(mbrMax.x+ offset*2f, 0, mbrMax.y+ offset*2f);
            
            box.Add(v5);
            box.Add(v6);
            box.Add(v7);
            box.Add(v8);
            
            HashSet<Vector3> sites_3d = GetRandomSites(BB);
            
            // We set a random rotation to the voronoi diagram to avoid vertical and horizontal patterns in the maps
            float angle = Random.Range(-45f, 45f);
            sites_3d = GeometricFunctions.RotatePoints(sites_3d, angle);
            
            HashSet<MyVector2> sites_2d = new HashSet<MyVector2>();
            HashSet<Vector3> sites_3dAUX = new HashSet<Vector3>();

            Color c = Random.ColorHSV();

            foreach (Vector3 v in sites_3d)
            {
                sites_3dAUX.Add(v);
                sites_2d.Add(v.ToMyVector2());
            }

            foreach (Vector3 v in box)
            {
                sites_3dAUX.Add(v);
                sites_2d.Add(v.ToMyVector2());
            }

            //Normalize
            Normalizer2 normalizer = new Normalizer2(new List<MyVector2>(sites_2d));

            HashSet<MyVector2> randomSites_2d_normalized = normalizer.Normalize(sites_2d);

            //Generate the voronoi
            voronoiCellsPerRegion.Add(_Voronoi.DelaunayToVoronoi(randomSites_2d_normalized).ToList());

            //Unnormalize
            voronoiCellsPerRegion[voronoiCellsPerRegion.Count - 1] = normalizer.UnNormalize(new HashSet<VoronoiCell2>(voronoiCellsPerRegion.Last())).ToList();

            //Recorremos todas las celdas, de forma que podamos descartarlas
            for(int z = 0; z < voronoiCellsPerRegion.Last().Count; z++)
            {
                VoronoiCell2 cell = voronoiCellsPerRegion.Last()[z];
                List<VoronoiEdge2> orderedCellEdges = new List<VoronoiEdge2>();
                
                //1. Copiar el array de celdas
                for (int j = 0; j < cell.edges.Count; j++)
                {
                    orderedCellEdges.Add(cell.edges[j]);
                }

                //2. Ordenar las aristas
                orderedCellEdges = reorderCellEdges(orderedCellEdges);
                voronoiCellsPerRegion.Last()[z].edges = orderedCellEdges;
                cell.edges = orderedCellEdges;

                //Separemos las celdas que interseccionan, las que están totalmente fuera, y las que están totalmente dentro
                bool completlyInside = true;
                bool completlyOutside = true;
                int edgeCount = 0;
                
                foreach (var orderedCellEdge in orderedCellEdges)
                {
                    Vector2 p1 = orderedCellEdge.p1.ToVector2();
                    Vector2 p2 = orderedCellEdge.p2.ToVector2();
                    
                    //Si P1 está dentro
                    if (_Intersections.IsPointInPolygon(border2D, p1, true))
                        completlyOutside = false;
                    //Si P1 está fuera
                    else
                        completlyInside = false;
                    //Si P2 está dentro
                    if (_Intersections.IsPointInPolygon(border2D, p2, true))
                        completlyOutside = false;
                    //Si P2 está fuera
                    else
                        completlyInside = false;
                    
                    //Comprobamos ahora si está justo en el borde
                    Vector4? p1BorderLine = _Intersections.IsPointInPolygonBorder(border2D, p1);
                    Vector4? p2BorderLine = _Intersections.IsPointInPolygonBorder(border2D, p2);
                    
                    if (p1BorderLine != null || p2BorderLine != null)
                    {
                        Vector2 borderp1 = new Vector2();
                        Vector2 borderp2 = new Vector2();
                        if (p1BorderLine != null)
                        {
                            borderp1 = new Vector2(p1BorderLine.Value.x, p1BorderLine.Value.y);
                            borderp2 = new Vector2(p1BorderLine.Value.z, p1BorderLine.Value.w);
                        }
                        else
                        {
                            borderp1 = new Vector2(p2BorderLine.Value.x, p2BorderLine.Value.y);
                            borderp2 = new Vector2(p2BorderLine.Value.z, p2BorderLine.Value.w);
                        }

                        //saveCell = false;
                        Vector2 aristaDirection = (borderp1 - borderp2);
                        aristaDirection.Normalize();
                        
                        Vector2 normalIzquierda = new Vector2(-aristaDirection.y, aristaDirection.x);
                        float offsetDisplacement = 0.1f;

                        Vector2 center = (borderp1 + borderp2)/2f;

                        Debug.DrawLine(cell.edges[edgeCount].p1.ToVector3(), cell.edges[edgeCount].p2.ToVector3(), Color.cyan, 10f);
                        
                        if (p1BorderLine != null)
                        {
                            cell.edges[edgeCount].p1 = (cell.edges[edgeCount].p1.ToVector2() + normalIzquierda * offsetDisplacement).ToMyVector2();
                        }
                        else
                        {
                            cell.edges[edgeCount].p2 = (cell.edges[edgeCount].p2.ToVector2() + normalIzquierda * offsetDisplacement).ToMyVector2();
                        }
                    }
                    
                    //Comprobamos si intersecciona, de esta forma, esté dentro o fuera implicará que tendremos que comprobarlo
                    var colisiones = _Intersections.GetLineIntersectionsWithPolygon(border2D, p1, p2);
                    if (colisiones.Count > 0)
                    {
                        completlyInside = false;
                        completlyOutside = false;
                    }
                    edgeCount++;
                }

                if (completlyInside)
                    voronoiCellsToStay.Add(cell);
  
                else if (completlyOutside)
                    voronoiCellsToDelete.Add(cell);

                else
                    voronoiCellsToCheck.Add(cell);
            }

            List<int> invalidCells = new List<int>();
            //3. Detectar salidas y entradas al polígono, y las aristas a eliminar
            for (int checkCount = 0; checkCount < voronoiCellsToCheck.Count; checkCount++)
            {
                List<Vector3> exits = new List<Vector3>();
                List<Vector3> enters = new List<Vector3>();
                List<VoronoiEdge2> cutEdges = new List<VoronoiEdge2>();

                for (int edgeCount = 0; edgeCount < voronoiCellsToCheck[checkCount].edges.Count; edgeCount++)
                {
                    var checkingCellEdges = voronoiCellsToCheck[checkCount].edges;
                    Vector3 p1 = checkingCellEdges[edgeCount].p1.ToVector3();
                    Vector3 p2 = checkingCellEdges[edgeCount].p2.ToVector3();
               
                    bool p1Inside = _Intersections.IsPointInPolygon(border2D, p1.XZ() );
                    bool p2Inside = _Intersections.IsPointInPolygon(border2D, p2.XZ());
                    //COMPROBACIÓN DE COLISIONES

                    //______________________Luego comprobamos si uno está fuera y otro no______________________
                    
                    //CASO NORMAL
                    if (p1Inside && p2Inside)
                    {
                        cutEdges.Add(checkingCellEdges[edgeCount]);
                    }
                    else if (p1Inside != p2Inside)
                    {
                        var collisions = _Intersections.GetLineIntersectionsWithPolygon(border2D, p1.XZ(), p2.XZ());
                        
                        if (collisions != null && collisions.Count > 0)
                        {
                            if (p2Inside)
                            {
                                //Esto es que entra al polígono
                                enters.Add( new Vector3(collisions.First().x, collisions.First().y, cutEdges.Count));
                                
                                checkingCellEdges[edgeCount].p1 = new MyVector2(enters.Last().x, enters.Last().y);
                                cutEdges.Add(checkingCellEdges[edgeCount]);
                            }
                            else
                            {
                                //Esto es que se sale del polígono
                                exits.Add( new Vector3(collisions.First().x, collisions.First().y, cutEdges.Count));
                                
                                checkingCellEdges[edgeCount].p2 = new MyVector2(exits.Last().x, exits.Last().y);
                                cutEdges.Add(checkingCellEdges[edgeCount]);
                            }
                        }
                    }
                }

                //Ahora deberemos unir cada salida con cada entrada correspondiente
                if (exits.Count != enters.Count)
                {
                    Debug.LogError("No cuadra!: \n" + "exits.Count: " + exits.Count + "\n" + "enters.Count: " + enters.Count + "\n checkCount: " + checkCount);
                    for (int edgeCount = 0; edgeCount < voronoiCellsToCheck[checkCount].edges.Count; edgeCount++)
                    {
                        var checkingCellEdges = voronoiCellsToCheck[checkCount].edges;
                        Vector3 p1 = checkingCellEdges[edgeCount].p1.ToVector3();
                        Vector3 p2 = checkingCellEdges[edgeCount].p2.ToVector3();
                    }
                    invalidCells.Add(checkCount);
                }
                
                //5. Debemos unir cada salida con su entrada correspondiente
                //Para ello, buscamos antes si existe una esquina entre salida y entrada. En caso de no existir, simplemente se conectan ambas
                
                VoronoiEdge2[] cutEdgesCopy = new VoronoiEdge2[cutEdges.Count];
                cutEdges.CopyTo(cutEdgesCopy);

                for (var index = 0; index < cutEdgesCopy.Length; index++)
                {
                    var edge = cutEdgesCopy[index];
                    var edge2 = cutEdgesCopy[(index+1) % cutEdgesCopy.Length];

                    if (edge.p2.ToVector2() != edge2.p1.ToVector2())
                    {
                        Vector2Int IDs= GeometricFunctions.GetIndexBorderPoint(edge.p2.ToVector3(), edge2.p1.ToVector3(), region.GetBorderPoints());
                        int initialID = IDs.x;
                        int finalID = IDs.y;

                        var borderNotDecimated = region.GetBorderPoints();
                        var cornerPoints = GeometricFunctions.CalculateCorners(borderNotDecimated, regionsController.GetMaximumInteriorAngle());
                        region.SetCornerPoints(cornerPoints);
                        
                        List<int> corners = region.GetCornerPoints();
                        List<int> cornersToAdd = new List<int>();

                        foreach (var corner in corners)
                        {
                            //Caso 1: InitialID < FinalID
                            if (initialID < finalID)
                                //Miramos todos desde el initialID al finalID
                                for (int i = initialID; i < finalID; i++)
                                    if (corner == i)
                                        cornersToAdd.Add(i);
                            
                            //Caso 2: InitialID > FinalID
                            if (finalID < initialID )
                            {
                                //Miramos todos desde el finalID al 0, y desde el último hasta el initial ID   
                                for (int i = finalID; i >= 0; i--)
                                    if (corner == i)
                                        cornersToAdd.Add(i);
                                for (int i = region.GetBorderPoints().Count; i > initialID; i--)
                                    if (corner == i)
                                        cornersToAdd.Add(i);
                            }
                        }
                        
                        if (cornersToAdd.Count == 1)
                        {
                            MyVector2 cornerV2 = borderNotDecimated[cornersToAdd.First()].ToMyVector2();
                            //De la esquina a p1
                            cutEdges.Insert(index+1, new VoronoiEdge2(cornerV2, edge2.p1,edge.sitePos));

                            //De p2 a la esquina
                            cutEdges.Insert(index+1, new VoronoiEdge2(edge.p2, cornerV2,edge.sitePos));
                        }
                        
                        if (cornersToAdd.Count == 0)
                        {
                            cutEdges.Insert(index+1, new VoronoiEdge2(edge.p2, edge2.p1,edge.sitePos));
                        }
                        
                        else if (cornersToAdd.Count > 1)
                        {
                            //Insertamos la arista ÚltimaEsquina->edge2.p1
                            cutEdges.Insert(index+1, new VoronoiEdge2(borderNotDecimated[cornersToAdd.Last()].ToMyVector2(), edge2.p1,edge.sitePos));
                            //Debug.DrawLine(borderNotDecimated[cornersToAdd.Last()], edge2.p1.ToVector3(), Color.yellow, 5f);
                            
                            for (var i = cornersToAdd.Count-1; i > 0; i--)
                            {
                                MyVector2 currentPoint = borderNotDecimated[cornersToAdd[i-1]].ToMyVector2();
                                MyVector2 nextPoint = borderNotDecimated[cornersToAdd[i]].ToMyVector2();
                                
                                //Del punto actual al siguiente
                                cutEdges.Insert(index + 1, new VoronoiEdge2(currentPoint, nextPoint, edge.sitePos));
                                //Debug.DrawLine(currentPoint.ToVector3(), nextPoint.ToVector3(), colores[cornersToAdd.Count-1-i], 5f);
                            }
                            
                            //Insertamos la arista edge1.p2->PrimeraEsquina
                            cutEdges.Insert(index+1,  new VoronoiEdge2(edge.p2,borderNotDecimated[cornersToAdd.First()].ToMyVector2(),edge.sitePos));
                            //Debug.DrawLine(borderNotDecimated[cornersToAdd.First()], edge.p2.ToVector3(), Color.yellow, 5f);
                        }
                    }
                }
                
                //6. Guardar
                voronoiCellsToCheck[checkCount].edges = cutEdges;
            }

            for (int i = invalidCells.Count - 1; i >= 0; i--)
            {
                var cell = voronoiCellsToCheck[invalidCells[i]];
                foreach (var edge in cell.edges)
                {
                    Debug.DrawLine(edge.p1.ToVector3(), edge.p2.ToVector3(), Color.red, 10f);
                }
                voronoiCellsToCheck.RemoveAt(invalidCells[i]);
            }
              
            //Una vez conocidas las celdas que pueden generar conflicto, debemos recortarlas
            voronoiCellsPerRegion.RemoveAt(voronoiCellsPerRegion.Count - 1);
            var finalVoronoiCells= voronoiCellsToStay.Union(voronoiCellsToCheck).ToList();
            voronoiCellsPerRegion.Add(finalVoronoiCells);
        }

        for (var index = 0; index < voronoiCellsPerRegion.Count; index++)
        {
            var v = voronoiCellsPerRegion[index];
            BuildRegions(v, index);
        }
    }

    // Function to reorder a list of points according to the distance to an objective point
    public List<Vector3> ReorderListByDistance(List<Vector3> linePoints)
    {
        Vector3 targetPoint = linePoints[0];
        // Diccionario para almacenar la distancia de cada Vector3 al punto objetivo
        Dictionary<Vector3, float> distances = new Dictionary<Vector3, float>();

        // Calcular la distancia de cada Vector3 al punto objetivo y almacenarla en el diccionario
        foreach (Vector3 point in linePoints)
        {
            float distance = Vector3.Distance(point, targetPoint);
            if (!distances.TryAdd(point, distance))
            {
                string s = "LinePoints: ";
                foreach (var p in linePoints)
                {
                    s += p + ", ";
                }
                Debug.Log(s);
            }
        }

        // Ordenar la lista de Vector3 basándose en las distancias almacenadas en el diccionario
        linePoints.Sort((point1, point2) => distances[point1].CompareTo(distances[point2]));

        return linePoints;
    }
    
    // Function to reorder the edges of the cells of the voronoi diagram
    private List<VoronoiEdge2> reorderCellEdges(List<VoronoiEdge2> notOrderedEdges)
    {
        List<VoronoiEdge2> orderedEdges = new List<VoronoiEdge2>();

        if (notOrderedEdges.Count == 0)
            return orderedEdges;

        VoronoiEdge2 currentEdge = notOrderedEdges.First();
        notOrderedEdges = CleanFailedEdges(notOrderedEdges);

        orderedEdges.Add(currentEdge);
        
        for(int i = 0; i < notOrderedEdges.Count-1; i++)
        {
            VoronoiEdge2 nextEdge = notOrderedEdges.FirstOrDefault(e => e.p1.ToVector2() == currentEdge.p2.ToVector2());

            if (nextEdge == null)
            {
                nextEdge = notOrderedEdges.FirstOrDefault(e =>  e.p2.ToVector2() == currentEdge.p2.ToVector2());
                if (nextEdge != null)
                {
                    if (nextEdge.p1.ToVector2() != currentEdge.p2.ToVector2())
                    {
                        (nextEdge.p1, nextEdge.p2) = (nextEdge.p2, nextEdge.p1);
                    }

                    orderedEdges.Add(nextEdge);
                    currentEdge = nextEdge;
                }
            }
            else
            {
                orderedEdges.Add(nextEdge);
                currentEdge = nextEdge;
            }
           
        }

        orderedEdges = CheckEdgesContinuity(orderedEdges);

        return orderedEdges;
    }
    
    // Function to delete the edges of the cells that have failed
    static List<VoronoiEdge2> CleanFailedEdges(List<VoronoiEdge2> edges)
    {
        return edges.Where(edge => edge.p1.ToVector2() != edge.p2.ToVector2()).ToList();
    }

    // Function to check the continuity of the edges of the voronoi diagram
    public List<VoronoiEdge2> CheckEdgesContinuity(List<VoronoiEdge2> edges)
    {
        VoronoiEdge2[] edgesCopyArray = new VoronoiEdge2[edges.Count];
        List<VoronoiEdge2> edgesCopyList = new List<VoronoiEdge2>();
        edges.CopyTo(edgesCopyArray);
        edgesCopyList = edgesCopyArray.ToList();
        int cont = 0;
        
        for (int i = 0; i < edges.Count; i++)
        {
            var currentEdge = edges[i];
            var nextEdge = edges[(i+1) % edges.Count];
            if (currentEdge.p2.ToVector2() != nextEdge.p1.ToVector2())
            {
                edgesCopyList.Insert(i+1,new VoronoiEdge2(currentEdge.p2, nextEdge.p1, currentEdge.sitePos));
                cont++;
            }
        }

        if (cont > 0)
        {
            var auxcell = new VoronoiCell2(edgesCopyList[0].sitePos);
            auxcell.edges = edgesCopyList;
            voronoiErrorCells.Add(auxcell);
        }
        
        return edgesCopyList;
    }
    
    // Function to calculate the centroid of a list of points
    public Vector3 Centroid(List<Vector3> vList)
    {
        Vector3 suma = new Vector3();
        foreach (var v in vList)
        {
            suma += v;
        }

        suma /= vList.Count;

        return suma;
    }
    
    // Function to create the regions according to the voronoi diagram created
    private void BuildRegions(List<VoronoiCell2> cells, int cont)
    {
        List<VoronoiEdge2> allEdges = new List<VoronoiEdge2>();
        List<Vector3> allVerticesFromEdges = new List<Vector3>();
        foreach (var cell in cells)
        {
            allEdges.AddRange(cell.edges);

            foreach (var edge in cell.edges)
            {
                allVerticesFromEdges.Add(edge.p1.ToVector3());
            }
        }

        List<VoronoiCell2> voronoiCell2S = new List<VoronoiCell2>();
        foreach (VoronoiCell2 cell in cells)
        {
            VoronoiCell2 voronoiCellAux = new VoronoiCell2(cell.sitePos);
            
            for (int j = 0; j < cell.edges.Count; j++)
            {
                voronoiCellAux.edges.Add(cell.edges[j]);
            }

            voronoiCell2S.Add(voronoiCellAux);
        }

        voronoiCellsPerRegion[cont] = new List<VoronoiCell2>(voronoiCell2S);

        RegionsFunctions.DeleteRegion(0);

        int smallRegionsCount = 0;
        foreach (var cell in voronoiCell2S)
        {
            List<Vector3> borderVertices = new List<Vector3>();
            List<int> cornerVertices = new List<int>();

            if (cell.edges.Count > 0)
            {
                foreach (var edge in cell.edges)
                {
                    Vector3 p2 = edge.p1.ToVector3();
                    Vector3 p3 = edge.p2.ToVector3();

                    borderVertices.Add(p2);
                    //cornerVertices.Add(borderVertices.Count - 1);
                    List<Vector3> puntosIntermedios = GeometricFunctions.PointsBetween(p2, p3, finalRegionsDistanceBetweenpoints);
                    borderVertices.AddRange(puntosIntermedios);
                }

                if (borderVertices.Count > 2)
                {
                    Region r = new Region();
                    r.SetBorderPoints(borderVertices);
                    cornerVertices = GeometricFunctions.CalculateCorners(borderVertices, regionsController.GetMaximumInteriorAngle());
                    r.SetCornerPoints(cornerVertices);
                    r.SetArea(GeometricFunctions.CalculatePolylineArea(borderVertices));
                    Zone z = GeometricFunctions.GetZoneOfPoint(regionsController.GetInitialZones(),r.Centroide());

                    if (r.GetAreaKM() < z.GetMaximumSubdivisionArea())
                    {
                        smallRegionsCount++;
                    }
                    
                    r.SetOrientacionRegion();
                    RegionsFunctions.InsertNewRegionByArea(r, regionsController.GetParcialRegionsList());
                }
            }
        }
    }
    
    // Function to get random positions inside the minimum bounding rectangle of a region
    private HashSet<Vector3> GetRandomSites(Vector4 MBR)
    {
        HashSet<Vector3> randomSites = new HashSet<Vector3>();
        Vector2 mbrMin = new Vector2(MBR.x, MBR.y);
        Vector2 mbrMax = new Vector2(MBR.z , MBR.w);
        
        Vector3 topLeft = new Vector3(mbrMin.x, 0, mbrMax.y);
        Vector3 topRight = new Vector3(mbrMax.x, 0, mbrMax.y);
        Vector3 bottomRight = new Vector3(mbrMax.x, 0, mbrMin.y);
        Vector3 bottomLeft = new Vector3(mbrMin.x, 0, mbrMin.y);

        List<Vector3> rectangleBorder = new List<Vector3>();
        rectangleBorder.Add(topLeft);
        rectangleBorder.Add(topRight);
        rectangleBorder.Add(bottomRight);
        rectangleBorder.Add(bottomLeft);

        var randomSitesRandomPoints = TestAlgorithmsHelpMethods.GenerateRandomPointsFromPolygon(numberOfPointsPerSide * numberOfPointsPerSide, rectangleBorder, regionsController.GetVoronoiDefaultFreedom(), regionsController.GetInitialZones());
        randomSites = new HashSet<Vector3>(randomSitesRandomPoints.Select(point => point.getPosition()).ToList());
        
        return randomSites;
    }
}