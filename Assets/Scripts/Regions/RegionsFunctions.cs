using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DefaultNamespace;
using DefaultNamespace.Regions;
using Habrador_Computational_Geometry;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class RegionsFunctions : MonoBehaviour
{
    public static void InsertNewRegionByArea(Region r, List<Region> regions)
    {
        float area = r.GetAreaKM();

        for (int i = 0; i < regions.Count; i++)
        {
            if (regions[i].GetAreaKM() < area)
            {
                regions.Insert(i, r);
                return;
            }
        }

        // Si no nos hemos salido del bucle, esta región será la última (la de mayor área)
        regions.Add(r);
    }
    
    public static void DefineZoneOfRegions()
    {
        RegionsController regionsController = RegionsController.GetInstance();
        List<Region> regions = regionsController.GetParcialRegionsList();
        List<Zone> initialZones = regionsController.GetInitialZones();
        // Una región va a ser de una zona u otra en función de la distancia y la fuerza de la zona
        foreach (var region in regions)
        {
            Zone zone = GeometricFunctions.GetZoneOfPoint(initialZones, region.Centroide());

            // Si la región no cae en ninguna zona indicamos una zona default
            if (zone != null)
            {
                region.SetZone(zone);
                region.SetZoneType(zone.GetZoneType());
            }
        }
    }

    public static void OrderRegions()
    {
        RegionsController.GetInstance().GetParcialRegionsList().Sort((region, region1) => region1.GetAreaKM().CompareTo(region.GetAreaKM()));
    }

    public static void FillAllRegionsInteriorPoints()
    {
        RegionsController regionsController = RegionsController.GetInstance();
        List<Region> regions = regionsController.GetParcialRegionsList();
        
        for (var index = 0; index < regions.Count; index++)
        {
            // Realizamos el proceso de dividir la región con mayor área
            if (regions[index].GetInteriorPoints() == null ||
                regions[index].GetInteriorPoints().Count == 0)
                regions[index].SetInteriorPoints(StreamlinesFunctions.GenerateRandomPoints(regions[index]));
        }
    }

    public static void DeleteRegion(int i)
    {
        // Si no nos hemos salido del bucle, esta región será la última (la de menor área)
        RegionsController.GetInstance().GetParcialRegionsList().RemoveAt(i);
    }
    
    public static void TriangulateAllRegions()
    {
        RegionsController regionsController = RegionsController.GetInstance();
        List<Region> finalRegionList = regionsController.GetFinalRegionsList();
        for (int i = 0; i < finalRegionList.Count; i++)
        {
            finalRegionList[i].TriangulateRegion();
        }
    }

    public static void InitRegionsTest(List<Region> regions, List<InitialStreamline> initialPolylinesOnInspector, List<InitialStreamline> initialPolylinesForRuntime, bool callingForInitialPolyLines = false)
    {
        RegionsController regionsController = RegionsController.GetInstance();
        //ResetAll();
        
        if (regions.Count == 0)
            regionsController.InitGeneralRegion();

        List<InitialStreamline> initialPolylinesCopy = new List<InitialStreamline>();

        for (var index = 0; index < initialPolylinesOnInspector.Count; index++)
        {
            var line = initialPolylinesOnInspector[index];
            // Nos aseguramos de que la línea inicial corte a la región y la añadimos a la lista de polilíneas iniciales
            List<Vector3> linea = GeometricFunctions.getLinePositionsByTransform(line.GetParent(), 1f, 2);
            initialPolylinesOnInspector[index].SetPoints(linea);
        }

        foreach (var p in initialPolylinesOnInspector)
        {
            initialPolylinesForRuntime.Add(p);
        }

        // Ahora toca generar las regiones con las polilíneas interiores
        if (initialPolylinesForRuntime.Count > 0)
        {
            // Primero, obtenemos los puntos de la línea con puntos intermedios
            for (var index = 0; index < initialPolylinesForRuntime.Count; index++)
            {
                var line = initialPolylinesForRuntime[index];
                // Nos aseguramos de que la línea inicial corte a la región y la añadimos a la lista de polilíneas iniciales

                // Antes de hacer la división, comprobamos que initialID sea menor que finalID.
                // En caso contrario, invertimos la streamline para tener un único caso.

                var tempIDs = GeometricFunctions.GetIndexBorderPoint(line.GetPoints().First(), line.GetPoints().Last(),
                    regions.First().GetBorderPoints());
                if (tempIDs.y < tempIDs.x)
                {
                    Debug.Log("INVIRTIENDO");
                    line.SetPoints(GeometricFunctions.InvertList(line.GetPoints()));
                }

                var displacedPolylines = GeometricFunctions.DisplacePolyline(line.GetPoints(), line.GetWidth());
                if (displacedPolylines.Count == 2)
                {
                    initialPolylinesForRuntime[index].SetLeftPoints(displacedPolylines[0]);
                    initialPolylinesForRuntime[index].SetRightPoints(displacedPolylines[1]);
                }
            }

            // Una vez tenemos las líneas, las iteramos para ver con qué regiones colisionan
            for (int j = 0; j < initialPolylinesForRuntime.Count; j++)
            {
                if (!initialPolylinesForRuntime[j].hasValidDisplacedLines())
                {
                    continue;
                }

                var linea = initialPolylinesForRuntime[j].GetPoints();
                var rightFullLinePoints = initialPolylinesForRuntime[j].GetRightPoints();
                var leftFullLinePoints = initialPolylinesForRuntime[j].GetLeftPoints();
                List<Vector2> linea2D = GeometricFunctions.ConvertListVector3ToListVector2XZ(linea);
                List<Vector2> leftFullLine2D = GeometricFunctions.ConvertListVector3ToListVector2XZ(leftFullLinePoints);
                List<Vector2> rightFullLine2D =
                    GeometricFunctions.ConvertListVector3ToListVector2XZ(rightFullLinePoints);
                Line leftLine = new Line();
                Line rightLine = new Line();
                Line centerLine = new Line();
                leftLine.SetWidth(0.01f);
                rightLine.SetWidth(0.01f);
                centerLine.SetWidth(0.01f);
                //Calculamos las colisiones de la linea izquierda
                for (int z = 0; z < regions.Count; z++)
                {
                    List<Vector2> border2D = regions[z].GetBorderPoints2D();
                    List<int> indices = new List<int>();
                    var intersections =
                        _Intersections.GetPolygonIntersectionsWithPolygon(leftFullLine2D, border2D, false, true,
                            out indices);

                    if (intersections.Count != indices.Count)
                    {
                        Debug.LogError("Son distintos!:  " + intersections.Count + "  " + indices.Count);
                    }

                    for (var index = 0; index < intersections.Count - 1; index++)
                    {
                        var startIntersection = intersections[index];
                        var endIntersection = intersections[index + 1];

                        //Ahora crearemos un segmento por cada una de las intersecciones
                        if (index >= leftFullLinePoints.Count)
                        {
                            Debug.Log("LEFTLINE: " + leftFullLine2D.Count + "  INDEX: " + index + " INDICES: " +
                                      indices.Count + " INDICES[INDEX]: " + indices[index]);
                        }

                        List<Vector2> segment =
                            leftFullLine2D.GetRange(indices[index], indices[index + 1] - indices[index]);
                        segment.Insert(0, startIntersection);
                        segment.Add(endIntersection);

                        leftLine.AddLineSegment(GeometricFunctions.ConvertListVector2ToListVector3(segment));
                    }
                }

                //Calculamos las colisiones de la linea derecha
                for (int z = 0; z < regions.Count; z++)
                {
                    List<Vector2> border2D = regions[z].GetBorderPoints2D();
                    List<int> indices = new List<int>();
                    var intersections =
                        _Intersections.GetPolygonIntersectionsWithPolygon(rightFullLine2D, border2D, false, true,
                            out indices);

                    if (intersections.Count != indices.Count)
                    {
                        Debug.LogError("Son distintos!:  " + intersections.Count + "  " + indices.Count);
                    }

                    for (var index = 0; index < intersections.Count - 1; index++)
                    {
                        var startIntersection = intersections[index];
                        var endIntersection = intersections[index + 1];

                        //Ahora crearemos un segmento por cada una de las intersecciones
                        if (index >= rightFullLinePoints.Count)
                        {
                            Debug.Log("RIGHTLINE: " + rightFullLine2D.Count + "  INDEX: " + index + " INDICES: " +
                                      indices.Count + " INDICES[INDEX]: " + indices[index]);
                        }

                        List<Vector2> segment =
                            rightFullLine2D.GetRange(indices[index], indices[index + 1] - indices[index]);
                        segment.Insert(0, startIntersection);
                        segment.Add(endIntersection);

                        rightLine.AddLineSegment(GeometricFunctions.ConvertListVector2ToListVector3(segment));
                    }
                }

                //Calculamos las colisiones de la linea central
                for (int z = 0; z < regions.Count; z++)
                {
                    List<Vector2> border2D = regions[z].GetBorderPoints2D();
                    List<int> indices = new List<int>();
                    var intersections =
                        _Intersections.GetPolygonIntersectionsWithPolygon(linea2D, border2D, false, true, out indices);

                    if (intersections.Count != indices.Count)
                    {
                        Debug.LogError("Son distintos!:  " + intersections.Count + "  " + indices.Count);
                    }

                    for (var index = 0; index < intersections.Count - 1; index++)
                    {
                        var startIntersection = intersections[index];
                        var endIntersection = intersections[index + 1];

                        //Ahora crearemos un segmento por cada una de las intersecciones
                        if (index >= linea2D.Count)
                        {
                            Debug.Log("CENTERLINE: " + linea2D.Count + "  INDEX: " + index + " INDICES: " +
                                      indices.Count + " INDICES[INDEX]: " + indices[index]);
                        }

                        List<Vector2> segment = linea2D.GetRange(indices[index], indices[index + 1] - indices[index]);
                        segment.Insert(0, startIntersection);
                        segment.Add(endIntersection);

                        centerLine.AddLineSegment(GeometricFunctions.ConvertListVector2ToListVector3(segment));
                    }
                }

                Debug.Log("leftline.count: " + leftLine.GetLineSegments().Count + "     centerline.count: " +
                          centerLine.GetLineSegments().Count + "    rightLine.count: " +
                          rightLine.GetLineSegments().Count);

                //NO tendremos el mismo número de segmentos para la línea izquierda y derecha
                int segmentsCount = leftLine.GetLineSegments().Count;

                List<Region> temporalRegionList = new List<Region>();
                List<int> indicesToDeleteLeft = new List<int>();
                List<int> indicesToDeleteRight = new List<int>();

                for (int segmentIndex = 0; segmentIndex < segmentsCount; segmentIndex++)
                {
                    var leftSegment = leftLine.GetLineSegments()[segmentIndex];
                    for (int w = 0; w < regions.Count; w++)
                    {
                        Region reg = regions[w];

                        List<Vector2> border2D =
                            GeometricFunctions.ConvertListVector3ToListVector2XZ(
                                reg.GetBorderPointsWithAngleDecimate(-1));

                        bool leftLineIsInRegion = false;

                        //Contemplamos los distintos casos para el lado izquierdo
                        if (leftSegment.Count == 2)
                            leftLineIsInRegion = _Intersections.IsPointInPolygon(border2D,
                                (leftSegment.First().XZ() + leftSegment.Last().XZ()) / 2f);
                        else if (leftSegment.Count < 2)
                            leftLineIsInRegion = false;
                        else
                            leftLineIsInRegion = _Intersections.IsPointInPolygon(border2D,
                                leftSegment[(leftSegment.Count / 2)].XZ());

                        if (leftLineIsInRegion)
                        {
                            foreach (var st in leftLine.GetLineSegments())
                            {
                                for (var i = 0; i < st.Count - 1; i++)
                                {
                                    var pt = st[i];
                                    var ptplusOne = st[i + 1];
                                    Debug.DrawLine(pt, ptplusOne, Color.yellow, 10f);
                                }
                            }

                            Region newRegion = DivideRegionTest(regions[w], leftSegment,
                                Streamline.StreamlineSide.Left);

                            if (newRegion != null)
                            {
                                temporalRegionList.Add(newRegion);
                                indicesToDeleteLeft.Add(w);
                            }
                        }
                    }
                }

                segmentsCount = rightLine.GetLineSegments().Count;
                for (int segmentIndex = 0; segmentIndex < segmentsCount; segmentIndex++)
                {
                    var rightSegment = rightLine.GetLineSegments()[segmentIndex];
                    for (int w = 0; w < regions.Count; w++)
                    {
                        Region reg = regions[w];

                        List<Vector2> border2D =
                            GeometricFunctions.ConvertListVector3ToListVector2XZ(
                                reg.GetBorderPointsWithAngleDecimate(-1));

                        bool rightLineInRegion = false;

                        //Contemplamos los distintos casos para el lado izquierdo
                        if (rightSegment.Count == 2)
                            rightLineInRegion = _Intersections.IsPointInPolygon(border2D,
                                (rightSegment.First().XZ() + rightSegment.Last().XZ()) / 2f);
                        else if (rightSegment.Count < 2)
                            rightLineInRegion = false;
                        else
                            rightLineInRegion = _Intersections.IsPointInPolygon(border2D,
                                rightSegment[(rightSegment.Count / 2)].XZ());


                        if (rightLineInRegion)
                        {
                            foreach (var st in rightLine.GetLineSegments())
                            {
                                for (var i = 0; i < st.Count - 1; i++)
                                {
                                    var pt = st[i];
                                    var ptplusOne = st[i + 1];
                                    Debug.DrawLine(pt, ptplusOne, Color.magenta, 10f);
                                }
                            }

                            Region newRegion = DivideRegionTest(regions[w], rightSegment,
                                Streamline.StreamlineSide.Right);

                            if (newRegion != null)
                            {
                                temporalRegionList.Add(newRegion);
                                indicesToDeleteRight.Add(w);
                            }
                        }
                    }
                }

                if (!callingForInitialPolyLines)
                {
                    for (int i = regions.Count - 1; i >= 0; i--)
                    {
                        if (indicesToDeleteLeft.Contains(i) || indicesToDeleteRight.Contains(i))
                            regions.RemoveAt(i);
                    }

                    foreach (var newRegion in temporalRegionList)
                    {
                        InsertNewRegionByArea(newRegion, regions);
                    }
                }
            }
        }

        if (callingForInitialPolyLines)
            initialPolylinesForRuntime = initialPolylinesCopy;
    }
    
    public static Region DivideRegionTest(Region actualRegion, List<Vector3> line, Streamline.StreamlineSide side)
    {
        RegionsController regionsController = RegionsController.GetInstance();
        //List<Region> parcialRegionList = regionsController.GetParcialRegionsList();
        //List<Region> finalRegionList = regionsController.GetFinalRegionsList();
        //List<StreamlineRegion> streamlineRegions = regionsController.GetStreamlineRegionsList();
        
        if (line == null)
        {
            //finalRegionList.Add(actualRegion);
            //parcialRegionList.Remove(actualRegion);
            return null;
        }

        //Variables
        Vector3[] borderPointsReg = new Vector3[actualRegion.GetBorderPoints().Count];
        actualRegion.GetBorderPoints().CopyTo(borderPointsReg);
        var borderPointsRegion = borderPointsReg.ToList();

        var IDsLine = GeometricFunctions.GetIndexBorderPoint(line.First(), line.Last(), borderPointsRegion);

        int initialID = IDsLine.x;
        int finalID = IDsLine.y;

        // Antes de hacer la división, comprobamos que initialID sea menor que finalID.
        // En caso contrario, invertimos la streamline para tener un único caso.
        if (initialID > finalID)
        {
            Debug.Log("INVIRTIENDO");
            line = GeometricFunctions.InvertList(line);
            // Muy importante que no se nos olvide invertir los IDs también
            (initialID, finalID) = (finalID, initialID);

            if (side.Equals(Streamline.StreamlineSide.Left))
                side = Streamline.StreamlineSide.Right;
            else
                side = Streamline.StreamlineSide.Left;
        }

        // Añadimos los puntos de corte de la streamline a los bordes de la región nueva
        borderPointsRegion.Insert(finalID, line.Last());
        borderPointsRegion.Insert(initialID, line.First());
        finalID++;

        // Convertimos los puntos de la streamline a puntos de bordes de las nuevas regiones
        List<Vector3> borderPoints = new List<Vector3>();

        if (side == Streamline.StreamlineSide.Left)
        {
            for (int i = 0; i < borderPointsRegion.Count - 1; i++)
            {
                Vector3 punto = borderPointsRegion[i];

                if (i == initialID)
                {
                    //Añadimos la streamline a la región izquierda
                    for (int j = 0; j < line.Count; j++)
                        borderPoints.Add(line[j]);

                    if (borderPoints.Last() != line.Last())
                        borderPoints.Add(line.Last());
                }
                else if (i < initialID || i > finalID)
                {
                    //Debug.Log("initialID : " + initialID + "  finalID " + finalID + " i: " + i);
                    borderPoints.Add(punto);
                }
            }
        }
        else
        {
            for (int i = 0; i < borderPointsRegion.Count - 1; i++)
            {
                Vector3 punto = borderPointsRegion[i];

                if (i == finalID)
                {
                    // Añadimos la rightPolyline a la región derecha
                    for (int j = line.Count - 1; j >= 0; j--)
                        borderPoints.Add(line[j]);

                    if (borderPoints.Last() != line.First())
                        borderPoints.Add(line.First());
                }
                else if (i > initialID && i < finalID)
                {
                    //Debug.Log("initialID : " + initialID + "  finalID " + finalID + " i: " + i);
                    borderPoints.Add(punto);
                }
            }
        }

        List<int> cornerPoints = GeometricFunctions.CalculateCorners(borderPoints, regionsController.GetMaximumInteriorAngle());
        if (cornerPoints.Count > 0)
        {
            if (borderPoints.Last() != borderPoints.First())
                borderPoints.Add(borderPoints.First());

            // Vemos si hay que recortar la región por la presencia de algún corte entre la región
            borderPoints = GeometricFunctions.ClippingRegion(borderPoints);

            Region returnedRegion = new Region();
            cornerPoints = GeometricFunctions.CalculateCorners(borderPoints, regionsController.GetMaximumInteriorAngle());
            returnedRegion.SetBorderPoints(borderPoints);
            returnedRegion.SetCornerPoints(cornerPoints);
            returnedRegion.ClearAndCreateIntermediatePoints(regionsController.GetStreamlinesPerRegion(), regionsController.GetClearDistance(),
                regionsController.GetMaximumCornerAngle());
            returnedRegion.SetArea(GeometricFunctions.CalculatePolylineArea(borderPoints));
            returnedRegion.SetOrientacionRegion();
            returnedRegion.SetInteriorPoints(StreamlinesFunctions.GenerateRandomPoints(returnedRegion));
            var regionZone = GeometricFunctions.GetZoneOfPoint(regionsController.GetInitialZones(), returnedRegion.Centroide());
            returnedRegion.SetZone(regionZone);
            returnedRegion.SetZoneType(regionZone.GetZoneType());

            if (returnedRegion.GetAreaKM() > regionsController.GetClippingMinimumArea())
            {
                return returnedRegion;
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }
    }
    
    // Función para dividir una región en 2 regiones hijas
    public static void DivideRegion(Region actualRegion, Streamline regionStreamline, bool dividingPolylines = false,
        float? width = null, bool callingForInitialPolyLines = false, int initialPolyLineIndex = -1,
        InitialStreamline.RiverParams riverParams = null)
    {
        RegionsController regionsController = RegionsController.GetInstance();
        List<Region> parcialRegionList = regionsController.GetParcialRegionsList();
        List<Region> finalRegionList = regionsController.GetFinalRegionsList();
        List<StreamlineRegion> streamlineRegions = regionsController.GetStreamlineRegionsList();

        if (regionStreamline == null || regionStreamline.GetStreamlinePoints() == null)
        {
            finalRegionList.Add(actualRegion);
            parcialRegionList.Remove(actualRegion);
            return;
        }

        //Variables
        Vector3[] borderPointsReg = new Vector3[actualRegion.GetBorderPoints().Count];
        actualRegion.GetBorderPoints().CopyTo(borderPointsReg);
        List<Vector3> borderPointsRegion = borderPointsReg.ToList();
        List<Vector3> streamlinePoints = new List<Vector3>();
        int initialID = regionStreamline.GetInitialID();
        int finalID = regionStreamline.GetFinalID();

        // Antes de hacer la división, comprobamos que initialID sea menor que finalID.
        // En caso contrario, invertimos la streamline para tener un único caso.
        if (initialID < finalID)
            streamlinePoints = regionStreamline.GetStreamlinePoints();
        else
        {
            streamlinePoints = GeometricFunctions.InvertList(regionStreamline.GetStreamlinePoints());

            // Muy importante que no se nos olvide invertir los IDs también
            (initialID, finalID) = (finalID, initialID);
        }

        Vector2Int IDsLeft = new Vector2Int();
        Vector2Int IDsRight = new Vector2Int();
        int initialLeftID = -1, initialRightID = -1, finalLeftID = -1, finalRightID = -1;

        float displaceWidth = 0;
        if (actualRegion.GetZone() != null)
        {
            displaceWidth = actualRegion.GetZone().GetRoadWidth();
        }

        if (width != null)
            displaceWidth = width.Value;

        List<List<Vector3>> polylines = new List<List<Vector3>>();

        List<Vector3> leftPolyline = new List<Vector3>();
        List<Vector3> rightPolyline = new List<Vector3>();

        if (dividingPolylines)
        {
            if (callingForInitialPolyLines)
            {
                if (regionStreamline.GetStreamlineType() == Streamline.StreamlineType.River)
                {
                    polylines = GeometricFunctions.DisplacePolyline(streamlinePoints, displaceWidth,
                        (riverParams.greenZoneMaximumRandomDisplacementPercentage * 0.01f) * displaceWidth);
                }
                else
                    polylines = GeometricFunctions.DisplacePolyline(streamlinePoints, displaceWidth);
            }
            else
            {
                polylines.Add(streamlineRegions[initialPolyLineIndex].getLeftStreamline());
                polylines.Add(streamlineRegions[initialPolyLineIndex].getRightStreamline());
            }
        }
        else
        {
            polylines = GeometricFunctions.DisplacePolyline(streamlinePoints, displaceWidth);
        }

        if (polylines != null && polylines.Count == 2)
        {
            leftPolyline = polylines[0];
            rightPolyline = polylines[1];
        }

        // Extendemos las polilíneas para asegurarnos el corte con la región
        leftPolyline = GeometricFunctions.ExtendPolyline(leftPolyline, borderPointsRegion, regionsController.GetContadorPolilineas());
        rightPolyline = GeometricFunctions.ExtendPolyline(rightPolyline, borderPointsRegion, regionsController.GetContadorPolilineas());
        
        // Una vez hemos alargado las líneas para asegurar que corten a la región, recortamos las líneas
        List<Vector2> regionV2 = GeometricFunctions.ConvertListVector3ToListVector2XZ(borderPointsRegion);
        leftPolyline = GeometricFunctions.CutPolylineInRegion(regionV2, leftPolyline);
        rightPolyline = GeometricFunctions.CutPolylineInRegion(regionV2, rightPolyline);

        bool rightPart = true, leftPart = true;

        if (leftPolyline == null || leftPolyline.Count < 2)
            leftPart = false;

        if (rightPolyline == null || rightPolyline.Count < 2)
            rightPart = false;

        // Una vez sabemos si hay división por izquierda y/o derecha, tenemos 3 casos:
        // 1. División por ambos lados
        // 2. División por derecha
        // 3. División por izquierda


        if (dividingPolylines && callingForInitialPolyLines)
        {
            StreamlinesFunctions.CreateConstraintRegionsWithPolylines(leftPolyline, rightPolyline, regionStreamline, streamlinePoints,
                displaceWidth, riverParams);
        }

        // Primer caso
        if (leftPart && rightPart)
        {
            Vector2Int IDs = GeometricFunctions.GetIndexBorderPoint(streamlinePoints.First(), streamlinePoints.Last(),
                borderPointsRegion);
            IDsLeft = GeometricFunctions.GetIndexBorderPoint(leftPolyline.First(), leftPolyline.Last(),
                borderPointsRegion);
            IDsRight = GeometricFunctions.GetIndexBorderPoint(rightPolyline.First(), rightPolyline.Last(),
                borderPointsRegion);

            initialID = IDs.x;
            initialLeftID = IDsLeft.x;
            initialRightID = IDsRight.x;
            finalID = IDs.y;
            finalLeftID = IDsLeft.y;
            finalRightID = IDsRight.y;

            if (IDs.x < IDs.y && (IDsRight.x > IDsRight.y || IDsLeft.x > IDsLeft.y))
            {
                if (!callingForInitialPolyLines)
                {
                    (leftPolyline, rightPolyline) = (GeometricFunctions.InvertList(rightPolyline),
                        GeometricFunctions.InvertList(leftPolyline));
                    IDsLeft = GeometricFunctions.GetIndexBorderPoint(leftPolyline.First(), leftPolyline.Last(),
                        borderPointsRegion);
                    IDsRight = GeometricFunctions.GetIndexBorderPoint(rightPolyline.First(), rightPolyline.Last(),
                        borderPointsRegion);
                    initialLeftID = IDsLeft.x;
                    initialRightID = IDsRight.x;
                    finalLeftID = IDsLeft.y;
                    finalRightID = IDsRight.y;
                }
            }
            else if (IDs.x > IDs.y && (IDsRight.x < IDsRight.y || IDsLeft.x < IDsLeft.y))
            {
                Debug.LogError("IDs MAL, IDsRight y IDsLeft BIEN" + "\n" +
                               "IDs: " + IDs.x + ", " + IDs.y + ",\n" +
                               "IDsRight: " + IDsRight.x + ", " + IDsRight.y + ",\n" +
                               "IDsLeft: " + IDsLeft.x + ", " + IDsLeft.y + ",\n");
            }

            if (initialLeftID > initialID)
                borderPointsRegion.Insert(initialLeftID, leftPolyline.First());

            borderPointsRegion.Insert(finalLeftID, leftPolyline.Last());
            borderPointsRegion.Insert(finalRightID, rightPolyline.Last());
            borderPointsRegion.Insert(initialRightID, rightPolyline.First());


            if (initialID >= initialLeftID)
                borderPointsRegion.Insert(initialLeftID, leftPolyline.First());

            if (initialLeftID > initialID)
            {
                initialLeftID += 3;

                // Cuando pase esto, reordenamos los bordes para que no ocurra
                borderPointsRegion = GeometricFunctions.ShiftPolygonIndices(borderPointsRegion, initialLeftID);

                IDsLeft = GeometricFunctions.GetIndexBorderPoint(leftPolyline.First(), leftPolyline.Last(),
                    borderPointsRegion);

                IDsRight = GeometricFunctions.GetIndexBorderPoint(rightPolyline.First(), rightPolyline.Last(),
                    borderPointsRegion);

                initialLeftID = IDsLeft.x;
                initialRightID = IDsRight.x;
                finalLeftID = IDsLeft.y;
                finalRightID = IDsRight.y;
            }
            else
            {
                finalLeftID += 3;
                finalRightID += 2;
                initialRightID++;
            }
        }
        else if (leftPart) // Segundo caso
        {
            IDsLeft = GeometricFunctions.GetIndexBorderPoint(leftPolyline.First(), leftPolyline.Last(),
                borderPointsRegion);

            initialID = IDsLeft.x;
            finalID = IDsLeft.y;


            borderPointsRegion.Insert(finalID, leftPolyline.Last());
            borderPointsRegion.Insert(initialID, leftPolyline.First());

            finalID++;

            // Repasar caso initialID > finalID (solapamiento)
        }
        else if (rightPart) // Tercer caso
        {
            IDsRight = GeometricFunctions.GetIndexBorderPoint(rightPolyline.First(), rightPolyline.Last(),
                borderPointsRegion);

            initialID = IDsRight.x;
            finalID = IDsRight.y;

            borderPointsRegion.Insert(finalID, rightPolyline.Last());
            borderPointsRegion.Insert(initialID, rightPolyline.First());

            finalID++;
            // Repasar caso initialID > finalID (solapamiento)
        }
        else
        {
            //Este caso implica que no hay parte izquierda ni derecha, lo que simplemente derivará en dejar esta región como final
            parcialRegionList.Remove(actualRegion);
            finalRegionList.Add(actualRegion);
            return;
        }

        // Convertimos los puntos de la streamline a puntos de bordes de las nuevas regiones
        List<Vector3> borderLeftPoints = new List<Vector3>();
        List<Vector3> borderRightPoints = new List<Vector3>();
        
        if (rightPart && leftPart)
        {
            for (int i = 0; i < borderPointsRegion.Count - 1; i++)
            {
                Vector3 punto = borderPointsRegion[i];

                if (i == initialLeftID)
                {
                    // Añadimos la leftPolyline a la región izquierda
                    for (int j = 0; j < leftPolyline.Count; j++)
                        borderLeftPoints.Add(leftPolyline[j]);

                    if (borderLeftPoints.Last() != leftPolyline.Last())
                        borderLeftPoints.Add(leftPolyline.Last());
                }
                else if (i == finalRightID)
                {
                    // Añadimos la rightPolyline a la región derecha
                    for (int j = rightPolyline.Count - 1; j >= 0; j--)
                        borderRightPoints.Add(rightPolyline[j]);

                    if (borderRightPoints.Last() != rightPolyline.First())
                        borderRightPoints.Add(rightPolyline.First());
                }
                else if (i < initialLeftID || i > finalLeftID)
                {
                    borderLeftPoints.Add(punto);
                }
                else if (i > initialRightID && i < finalRightID)
                {
                    borderRightPoints.Add(punto);
                }
            }
        }
        else if (leftPart)
        {
            for (int i = 0; i < borderPointsRegion.Count - 1; i++)
            {
                Vector3 punto = borderPointsRegion[i];

                if (i == initialID)
                {
                    //Añadimos la streamline a la región izquierda
                    for (int j = 0; j < leftPolyline.Count; j++)
                        borderLeftPoints.Add(leftPolyline[j]);

                    if (borderLeftPoints.Last() != leftPolyline.Last())
                        borderLeftPoints.Add(leftPolyline.Last());
                }
                else if (i < initialID || i > finalID)
                {
                    borderLeftPoints.Add(punto);
                }
            }
        }
        else if (rightPart)
        {
            for (int i = 0; i < borderPointsRegion.Count - 1; i++)
            {
                Vector3 punto = borderPointsRegion[i];

                if (i == finalID)
                {
                    // Añadimos la rightPolyline a la región derecha
                    for (int j = rightPolyline.Count - 1; j >= 0; j--)
                        borderRightPoints.Add(rightPolyline[j]);

                    if (borderRightPoints.Last() != rightPolyline.First())
                        borderRightPoints.Add(rightPolyline.First());
                }
                else if (i > initialID || i < finalID)
                {
                    borderRightPoints.Add(punto);
                }
            }
        }

        // Separamos el código de la región derecha y la izquierda por los casos donde solo corta por una parte
        if (leftPart)
        {
            List<int> cornerLeftPoints = GeometricFunctions.CalculateCorners(borderLeftPoints, regionsController.GetMaximumInteriorAngle());
            if (cornerLeftPoints.Count > 0)
            {
                if (borderLeftPoints.Last() != borderLeftPoints.First())
                    borderLeftPoints.Add(borderLeftPoints.First());

                // Vemos si hay que recortar la región por la presencia de algún corte entre la región
                borderLeftPoints = GeometricFunctions.ClippingRegion(borderLeftPoints);

                Region left = new Region();
                cornerLeftPoints = GeometricFunctions.CalculateCorners(borderLeftPoints, regionsController.GetMaximumInteriorAngle());
                left.SetBorderPoints(borderLeftPoints);
                left.SetCornerPoints(cornerLeftPoints);

                if (dividingPolylines && !callingForInitialPolyLines)
                {
                    
                }
                else
                {
                    left.ClearAndCreateIntermediatePoints(regionsController.GetStreamlinesPerRegion(), regionsController.GetClearDistance(), regionsController.GetMaximumInteriorAngle()/*, leftPolyline*/);
                }
                
                left.SetArea(GeometricFunctions.CalculatePolylineArea(borderLeftPoints));
                left.SetOrientacionRegion();
                left.SetInteriorPoints(StreamlinesFunctions.GenerateRandomPoints(left));
                var leftRegionZone = GeometricFunctions.GetZoneOfPoint(regionsController.GetInitialZones(), left.Centroide());

                left.SetZone(leftRegionZone);
                left.SetZoneType(leftRegionZone.GetZoneType());
                if (left.GetAreaKM() > regionsController.GetClippingMinimumArea())
                {
                    if (left.IsFinalRegion() && !dividingPolylines)
                        finalRegionList.Add(left);
                    else
                        InsertNewRegionByArea(left, parcialRegionList);
                }
            }
        }

        if (rightPart)
        {
            List<int> cornerRightPoints = GeometricFunctions.CalculateCorners(borderRightPoints, regionsController.GetMaximumInteriorAngle());

            if (cornerRightPoints.Count > 0)
            {
                if (borderRightPoints.Last() != borderRightPoints.First())
                    borderRightPoints.Add(borderRightPoints.First());

                // Vemos si hay que recortar la región por la presencia de algún corte entre la región
                borderRightPoints = GeometricFunctions.ClippingRegion(borderRightPoints);

                Region right = new Region();
                right.SetBorderPoints(borderRightPoints);
                cornerRightPoints = GeometricFunctions.CalculateCorners(borderRightPoints, regionsController.GetMaximumInteriorAngle());

                right.SetCornerPoints(cornerRightPoints);
                //Si marcamos como esquinas todos los puntos de la streamline, deberían no eliminarse
                if (dividingPolylines && !callingForInitialPolyLines)
                {
                    
                }
                else
                {
                    right.ClearAndCreateIntermediatePoints(regionsController.GetStreamlinesPerRegion(), regionsController.GetClearDistance(), regionsController.GetMaximumInteriorAngle()/*, rightPolyline*/);
                }
                
                right.SetArea(GeometricFunctions.CalculatePolylineArea(borderRightPoints));
                right.SetOrientacionRegion();
                right.SetInteriorPoints(StreamlinesFunctions.GenerateRandomPoints(right));
                var rightRegionZone = GeometricFunctions.GetZoneOfPoint(regionsController.GetInitialZones(), right.Centroide());
                right.SetZone(rightRegionZone);
                right.SetZoneType(rightRegionZone.GetZoneType());

                if (right.GetAreaKM() > regionsController.GetClippingMinimumArea())
                {
                    if (right.IsFinalRegion() && !dividingPolylines)
                        finalRegionList.Add(right);
                    else
                        InsertNewRegionByArea(right, parcialRegionList);
                }
            }
        }

        // Eliminamos la región que acabamos de dividir
        parcialRegionList.Remove(actualRegion);
    }
    
    public static List<Vector3> GenerateRegionRandomPoints(Vector3 centerPoint, int numPoints, float minDistance,
        float maxDistance, int randomSeed)
    {
        List<Vector3> points = new List<Vector3>();

        if (numPoints <= 0)
        {
            Debug.LogError("Number of points should be greater than zero.");
            return points;
        }

        // Calculate the angle separation between points
        float angleStep = 360f / numPoints;

        Random.InitState(randomSeed);
        float randomMultiplyer = Random.Range(0f, 100f);
        // Generate points
        for (int i = 0; i < numPoints; i++)
        {
            float angle = i * angleStep;

            // Convert angle to radians
            float angleRad = Mathf.Deg2Rad * angle;

            // Calculate position offset using polar coordinates
            float perlin = Mathf.PerlinNoise((i + 1f) / numPoints + angleRad, randomMultiplyer);

            float diff = maxDistance - minDistance;
            float distance = minDistance + diff * perlin;

            float offsetX = distance * Mathf.Cos(angleRad);
            float offsetZ = distance * Mathf.Sin(angleRad);

            // Calculate final position
            Vector3 point = centerPoint + new Vector3(offsetX, 0f, offsetZ);

            points.Add(point);
        }

        return points;
    }
    
    public static void ClearRegions(List<Region> regions)
    {
        ClearRegionsByCorners(regions);
        ClearRegionsByArea(RegionsController.GetInstance().GetClippingMinimumArea(), regions);
        ClearRegionsByRightHandRule(regions);
    }

    public static void ClearRegionsByCorners(List<Region> regions)
    {
        int deletedCount = 0;
        List<int> regionsToDelete = new List<int>();
        
        for (int i = 0; i < regions.Count; i++)
        {
            if (regions[i].GetCornerPoints().Count <= 2)
            {
                deletedCount++;
                regionsToDelete.Add(i);
            }
        }

        for (int i = deletedCount - 1; i >= 0; i--)
            regions.RemoveAt(regionsToDelete[i]);

        if (deletedCount > 0)
            Debug.Log("Borradas corners: " + deletedCount);
    }

    public static void ClearRegionsByArea(float minimumArea, List<Region> regions)
    {
        int deletedCount = 0;
        List<int> regionsToDelete = new List<int>();

        for (int i = 0; i < regions.Count; i++)
        {
            int isClosingPointRepeated = 0;
            if (regions.Count > 1 && (regions.First() == regions.Last()))
            {
                isClosingPointRepeated = 1;
            }

            if (regions[i].GetAreaKM() < minimumArea || regions[i].GetBorderPoints().Count < 3 + isClosingPointRepeated)
            {
                deletedCount++;
                regionsToDelete.Add(i);
            }
        }

        for (int i = deletedCount - 1; i >= 0; i--)
            regions.RemoveAt(regionsToDelete[i]);

        if (deletedCount > 0)
            Debug.Log("Borradas area: " + deletedCount);
    }
    
    public static void ClearRegionsByRightHandRule(List<Region> regions)
    {
        int deletedCount = 0;
        List<int> regionsToDelete = new List<int>();

        for (int i = 0; i < regions.Count; i++)
        {
            if (!GeometricFunctions.IsPolygonClockwise(regions[i].GetBorderPoints2D()))
            {
                var points = regions[i].GetBorderPoints();

                for (var index = 0; index < points.Count; index++)
                {
                    var p = points[index];
                    var pPlusOne = points[(index + 1) % points.Count];
                    //Debug.DrawLine(p, pPlusOne, Color.green, 10f);
                }

                regionsToDelete.Add(i);
                deletedCount++;
            }
        }

        for (int i = deletedCount - 1; i >= 0; i--)
            regions.RemoveAt(regionsToDelete[i]);

        if (deletedCount > 0)
            Debug.Log("Borradas sentido horario: " + deletedCount);
    }

    public static void InitRegions(bool callingForInitialPolyLines = false)
    {
        RegionsController regionsController = RegionsController.GetInstance();
        List<Region> parcialRegionList = regionsController.GetParcialRegionsList();
        List<InitialStreamline> initialPolylinesOnInspector = regionsController.GetInitialPolylinesOnInspector();
        List<InitialStreamline> initialPolylinesForRuntime = regionsController.GetInitialPolylinesForRuntime();

        if (parcialRegionList.Count == 0)
            regionsController.InitGeneralRegion();

        List<InitialStreamline> initialPolylinesCopy = new List<InitialStreamline>();
        List<Streamline.StreamlineType> initialLinesType = new List<Streamline.StreamlineType>();
        List<float> initialLinesWidth = new List<float>();

        if (callingForInitialPolyLines)
        {
            for (var index = 0; index < initialPolylinesOnInspector.Count; index++)
            {
                if (initialPolylinesOnInspector[index].GetUsePolyline())
                {
                    var line = initialPolylinesOnInspector[index];
                    // Nos aseguramos de que la línea inicial corte a la región y la añadimos a la lista de polilíneas iniciales
                    line.RecalculatePoints();
                    List<Vector3> linea = line.GetCurvePoints();
                    initialPolylinesOnInspector[index].SetPoints(linea);
                }
            }

            foreach (var p in initialPolylinesOnInspector)
            {
                if (p.GetUsePolyline())
                {
                    initialPolylinesForRuntime.Add(p);
                }
            }

            regionsController.UpdateInitialPolylinesForRuntime(initialPolylinesForRuntime);
        }

        // Ahora toca generar las regiones con las polilíneas interiores
        if (initialPolylinesForRuntime.Count > 0)
        {
            List<List<Vector3>> lineasIniciales = new List<List<Vector3>>();
            // Primero, obtenemos los puntos de la línea con puntos intermedios
            foreach (var line in initialPolylinesForRuntime)
            {
                // Nos aseguramos de que la línea inicial corte a la región y la añadimos a la lista de polilíneas iniciales
                var linea = GeometricFunctions.ExtendPolyline(line.GetPoints(), regionsController.GetInitialRegionBorders(), regionsController.GetContadorPolilineas());
                lineasIniciales.Add(linea);
                initialLinesWidth.Add(line.GetWidth());
                initialLinesType.Add(line.GetType());
            }

            List<Vector3> borderPoints = new List<Vector3>();

            // Una vez tenemos las líneas, las iteramos para ver con qué regiones colisionan
            for (int j = 0; j < lineasIniciales.Count; j++)
            {
                var linea = lineasIniciales[j];
                Line l = new Line();
                l.SetWidth(0.01f);

                for (int z = 0; z < parcialRegionList.Count; z++)
                {
                    List<int> pointsToDelete = new List<int>();

                    Region r = parcialRegionList[z];
                    borderPoints = r.GetBorderPoints();
                    List<Vector2> border2D = GeometricFunctions.ConvertListVector3ToListVector2XZ(borderPoints);

                    List<Vector3> auxLinea = new List<Vector3>(linea);

                    //Primero debemos encontrar el primer punto de la polilínea que entra a la región
                    for (int i = auxLinea.Count - 1; i > 0; i--)
                    {
                        // Si hay colisiones, creamos un punto dentro del polígono
                        var colisiones =
                            _Intersections.GetLineIntersectionsWithPolygon(border2D, auxLinea[i - 1].XZ(),
                                auxLinea[i].XZ());

                        if (colisiones != null && colisiones.Count == 2)
                        {
                            // Obtenemos el punto intermedio de los cortes
                            Vector2 puntoMedio = (colisiones[0] + colisiones[1]) / 2f;
                            linea.Insert(i, GeometricFunctions.ConvertVector2ToVector3XZ(puntoMedio));
                        }
                    }

                    for (int i = 0; i < linea.Count; i++)
                    {
                        bool isPointInPolygon = _Intersections.IsPointInPolygon(border2D, linea[i].XZ());

                        if (!isPointInPolygon)
                        {
                            pointsToDelete.Add(i);
                        }
                    }

                    List<Vector3> auxiliarLine = null;

                    for (int i = 0; i < linea.Count; i++)
                    {
                        if (!pointsToDelete.Contains(i))
                        {
                            //EN CASO DE ENTRAR DE NUEVO AL POLÍGONO, EL PRIMER PUNTO DEL SEGMENTO SERÁ EL DE LA COLISIÓN
                            if (auxiliarLine == null)
                            {
                                auxiliarLine = new List<Vector3>();
                                Vector2? v2 = null;
                                if (i != 0)
                                    v2 = _Intersections.IsLineIntersectingPolygon(border2D, linea[i].XZ(),
                                        linea[i - 1].XZ());
                                //else
                                //Debug.LogWarning("CUIDADO PORQUE I == 0");

                                if (v2 != null)
                                    auxiliarLine.Add(GeometricFunctions.ConvertVector2ToVector3XZ(v2.Value));
                            }

                            auxiliarLine.Add(linea[i]);
                        }

                        //EN CASO DE SALIR DEL POLÍGONO, EL ÚLTIMO PUNTO DEL SEGMENTO SERÁ EL DE LA COLISIÓN
                        else if (pointsToDelete.Contains(i) && auxiliarLine != null)
                        {
                            Vector2? v2 =
                                _Intersections.IsLineIntersectingPolygon(border2D, linea[i].XZ(), linea[i - 1].XZ());
                            if (v2 != null)
                            {
                                auxiliarLine.Add(GeometricFunctions.ConvertVector2ToVector3XZ(v2.Value));
                            }

                            Vector3[] auxiliarLineToCopy = new Vector3[auxiliarLine.Count];
                            auxiliarLine.CopyTo(auxiliarLineToCopy);
                            Random.InitState(auxiliarLine.Count);
                            
                            GameObject streamlineObject = new GameObject("InitialStreamline");
                            InitialStreamline newStreamline = streamlineObject.AddComponent<InitialStreamline>();
                            newStreamline.Initialize(initialPolylinesForRuntime[j].GetParent(), 
                                auxiliarLineToCopy.ToList(), 
                                initialPolylinesForRuntime[j].GetType(), 
                                initialPolylinesForRuntime[j].GetWidth());
                            initialPolylinesCopy.Add(newStreamline);
                            l.AddLineSegment(auxiliarLineToCopy.ToList());

                            DestroyImmediate(streamlineObject);
                            auxiliarLine = null;
                        }
                    }
                }
                
                for (int i = 0; i < l.GetLineSegments().Count; i++)
                {
                    Region[] originalRegionListCopy = new Region[parcialRegionList.Count];
                    parcialRegionList.CopyTo(originalRegionListCopy);
                    List<Vector3> segmento = l.GetLineSegments()[i];
                    //Para cada segmento, debemos mirar en qué parte de las regiones actuales está, y entonces dividir

                    for (int w = 0; w < parcialRegionList.Count; w++)
                    {
                        Region reg = parcialRegionList[w];
                        borderPoints = reg.GetBorderPoints();
                        List<Vector2> border2D =
                            GeometricFunctions.ConvertListVector3ToListVector2XZ(
                                reg.GetBorderPointsWithAngleDecimate(-1));
                        //Cogemos un punto aleatorio para comprobar que está en esta región
                        if (_Intersections.IsPointInPolygon(border2D, segmento[(segmento.Count / 2)].XZ()))
                        {
                            Vector2Int IDs = GeometricFunctions.GetIndexBorderPoint(segmento.First(),
                                segmento.Last(), borderPoints);

                            int initialID = IDs.x;
                            int finalID = IDs.y;

                            Streamline s = new Streamline(segmento, initialID, finalID, 0f, initialLinesType[j],
                                initialLinesWidth[j]);

                            reg.SetStreamline(s);
                            DivideRegion(reg, s, true, s.GetStreamlineWidth(), callingForInitialPolyLines,
                                initialPolyLineIndex: (j), initialPolylinesForRuntime[j].getRiverParameters());
                            break;
                        }
                    }
                }
            }
        }

        if (callingForInitialPolyLines)
            regionsController.UpdateInitialPolylinesForRuntime(initialPolylinesCopy);
    }
    
    public static void AdjustRegions()
    {
        RegionsController regionsController = RegionsController.GetInstance();
        List<Region> parcialRegionList = regionsController.GetParcialRegionsList();
        
        Region[] originalRegionListCopy = new Region[parcialRegionList.Count];
        parcialRegionList.CopyTo(originalRegionListCopy);

        for (int j = 0; j < originalRegionListCopy.Length; j++)
        {
            Region region = originalRegionListCopy[j];

            List<Vector3> insideRegion = GeometricFunctions.DisplacePolygonInside(region.GetBorderPoints(),
                region.GetCornerPoints(), region.GetZone().GetRoadWidth(), regionsController.GetStreamlinesPerRegion());
            
            insideRegion = GeometricFunctions.ClippingRegion(insideRegion);

            if (insideRegion != null)
            {
                List<int> insideCorners = GeometricFunctions.CalculateCorners(insideRegion, regionsController.GetMaximumInteriorAngle());
                Region inside = new Region();
                inside.SetBorderPoints(insideRegion);
                inside.SetCornerPoints(insideCorners);
                inside.SetArea(GeometricFunctions.CalculatePolylineArea(insideRegion));
                inside.SetOrientacionRegion();
                inside.SetInteriorPoints(StreamlinesFunctions.GenerateRandomPoints(inside));
                inside.SetZone(region.GetZone());
                inside.SetZoneType(region.GetZoneType());
                InsertNewRegionByArea(inside, parcialRegionList);

                parcialRegionList.Remove(region);
            }
        }
    }
    
    public static void GenerateMap()
    {
        RegionsController regionsController = RegionsController.GetInstance();
        
        if (regionsController != null)
            regionsController.UpdateTotalStreamlinesTime(0f);

        try
        {
            regionsController.ResetAll();
            StreamlinesFunctions.CreateInitialPolylinesMeshes();

            regionsController.InitGeneralRegion();
            List<Region> parcialRegionList = regionsController.GetParcialRegionsList();
            List<Region> finalRegionList = regionsController.GetFinalRegionsList();
            regionsController.CalculateVoronoi();

            AdjustRegions();

            InitRegions();
            //RegionsFunctions.InitRegionsTest();
            
            ClearRegions(parcialRegionList);

            OrderRegions();

            FillAllRegionsInteriorPoints();

            do
            {
                // Realizamos el proceso de dividir la región con mayor área
                if (parcialRegionList[0].GetInteriorPoints() == null ||
                    parcialRegionList[0].GetInteriorPoints().Count == 0)
                    parcialRegionList[0].SetInteriorPoints(StreamlinesFunctions.GenerateRandomPoints(parcialRegionList[0]));

                //parcialRegionList[0].SetCornerPoints(GeometricFunctions.CalculateCorners(borderPoints, maximumInteriorAngle));

                StreamlinesFunctions.GenerateStreamlines(false);

                if (parcialRegionList.Count > 0 && parcialRegionList[0] != null)
                {
                    if (parcialRegionList[0].GetStreamline() != null)
                    {
                        DivideRegion(parcialRegionList[0], parcialRegionList[0].GetStreamline());
                    }
                    else if (parcialRegionList[0].GetStreamline() == null)
                    {
                        finalRegionList.Add(parcialRegionList[0]);
                        parcialRegionList.RemoveAt(0);
                    }
                }
            }
            while (parcialRegionList.Count > 0);

            ClearRegions(finalRegionList);
            TriangulateAllRegions();

            StreamlinesFunctions.FillStreamlineRegions();
            for (var index = 0; index < finalRegionList.Count; index++)
            {
                var region = finalRegionList[index];
                region.OrderPointsByLongestSide(regionsController.GetMaximumCornerAngle(), regionsController.GetDistanceDifferenceFactor());
                TemplateFunctions.FillRegion(region);
            }

            // Renderizar mapa
            RenderFunctions.RenderCaller();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        Debug.Log("totalStreamlinesTime: " + regionsController.GetTotalStreamlinesTime());
    }
    
    public static IEnumerator GenerateMapWithSteps(CustomYieldInstructions.CustomCorutine coroutine)
    {
        RegionsController regionsController = RegionsController.GetInstance();
        CustomYieldInstructions.CustomCorutine customCorutine = new CustomYieldInstructions.CustomCorutine();

        customCorutine.SetStarted();
        regionsController.UpdateCorutineIsWorking(true);
        regionsController.ResetAll();
        StreamlinesFunctions.CreateInitialPolylinesMeshes();
        yield return new CustomYieldInstructions.WaitForSecondsCustom(1f);
        regionsController.InitGeneralRegion();
        List<Region> parcialRegionList = regionsController.GetParcialRegionsList();
        List<Region> finalRegionList = regionsController.GetFinalRegionsList();
        yield return new CustomYieldInstructions.WaitForSecondsCustom(1f);
        regionsController.CalculateVoronoi();
        yield return new CustomYieldInstructions.WaitForSecondsCustom(1f);
        AdjustRegions();
        yield return new CustomYieldInstructions.WaitForSecondsCustom(1f);
        InitRegions();
        //RegionsFunctions.InitRegionsTest();
        yield return new CustomYieldInstructions.WaitForSecondsCustom(1f);
        ClearRegions(parcialRegionList);

        OrderRegions();
        FillAllRegionsInteriorPoints();

        Stopwatch timeBetweenFrames = new Stopwatch();
        timeBetweenFrames.Start();
        while (parcialRegionList.Count > 0 && customCorutine.IsWorking == true)
        {
            regionsController.MakeAStepInGeneratingMultipleRegions();
            if (timeBetweenFrames.ElapsedMilliseconds > 1)
            {
                yield return new CustomYieldInstructions.WaitForSecondsCustom(regionsController.GetTimeBetweenSteps());
                timeBetweenFrames.Restart();
            }
            //Debug.Log("Regiones -> " + parcialRegionList.Count);
        }

        ClearRegions(finalRegionList);
        TriangulateAllRegions();
        StreamlinesFunctions.FillStreamlineRegions();

        for (var index = 0; index < finalRegionList.Count; index++)
        {
            var region = finalRegionList[index];
            if (!customCorutine.IsWorking)
            {
                coroutine.SetComplete();
                break;
            }
            
            // Aplicamos la plantilla
            TemplateFunctions.FillRegion(region);
            yield return new CustomYieldInstructions.WaitForSecondsCustom(regionsController.GetTimeBetweenSteps());
        }

        coroutine.SetComplete();
        regionsController.UpdateCorutineIsWorking(false);
    }
}
