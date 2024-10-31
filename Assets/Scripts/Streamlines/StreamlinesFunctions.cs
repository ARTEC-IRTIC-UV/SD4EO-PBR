using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BurstGridSearch;
using DefaultNamespace;
using DefaultNamespace.Regions;
using Habrador_Computational_Geometry;
using JetBrains.Annotations;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class StreamlinesFunctions : MonoBehaviour
{
    public static float StreamlineScore(List<Vector3> s, Vector3 padre, float leftArea, float rightArea, List<Region> regions)
    {
        float score = 0.0f; //, accumulateDB = 0.0f;

        float diferenciaAreas;

        if (leftArea >= rightArea)
            diferenciaAreas = rightArea / leftArea;
        else
            diferenciaAreas = leftArea / rightArea;

        score += diferenciaAreas;

        // Evitamos hacer esto para la región inicial
        if (regions.Count > 1)
        {
            Vector3 orientacionHijo = Vector3.Normalize(s.Last() - s.First());

            // Una vez tenemos ambas orientaciones, damos una puntuación extra a esta streamline si cumple la diferencia de umbral de ángulo
            float dotProduct = Vector3.Dot(padre, orientacionHijo);
            score += Mathf.Abs(1.0f - Mathf.Abs(dotProduct));
        }

        return score;
    }

    public static int ObtainMaximumStreamlineScore(List<Streamline> streamlines)
    {
        int index = 0;
        float maxScore = float.MinValue;

        for (int i = 0; i < streamlines.Count; i++)
        {
            float score = streamlines[i].GetScore();
            if (score > maxScore)
            {
                index = i;
                maxScore = score;
            }
        }

        return index;
    }
    
    public static void FillStreamlineRegions()
    {
        RegionsController regionsController = RegionsController.GetInstance();
        foreach (var stRegion in regionsController.GetStreamlineRegionsList())
        {
            if (stRegion is River)
            {
                River river = (River)stRegion;
                StreamlineRegion riverRegion = river.getRiverRegion();
                StreamlineRegion leftRegion = river.getLeftRegion();
                StreamlineRegion rightRegion = river.getRightRegion();

                riverRegion.TriangulateRegion();
                leftRegion.TriangulateRegion();
                rightRegion.TriangulateRegion();

                leftRegion.SetBuildingType(BuildingType.Vegetation);
                leftRegion.SetTag("Vegetation");
                rightRegion.SetBuildingType(BuildingType.Vegetation);
                rightRegion.SetTag("Vegetation");
                riverRegion.SetBuildingType(BuildingType.Water);
                riverRegion.SetTag("Water");

                BuildingFunctions.GenerateBuildingDependingOnType(riverRegion, regionsController.GetBuildingsParent(), true);
                BuildingFunctions.GenerateBuildingDependingOnType(leftRegion, regionsController.GetBuildingsParent(), true, vegTrees:false);
                BuildingFunctions.GenerateBuildingDependingOnType( rightRegion, regionsController.GetBuildingsParent(), true, vegTrees:false);
            }
            else
            {
                stRegion.TriangulateRegion();

                //Debemos asignar las coordenadas de textura según la longitud de arco. Para ello, se asignará unas coordenadas en la V (por ejemplo) que dependerán
                // de la longitud de arco en un punto concreto. Como los vértices están a los lados, el lado izquierdo será U=0 y el lado derecho U = 1
                Mesh m = stRegion.getTriangulatedMesh();
                List<Vector3> vertices = m.vertices.ToList();
                List<Vector2> newUVs = new List<Vector2>();
                List<Vector3> centralPolylinePoints = stRegion.GetStreamline().GetStreamlinePoints();
                List<Vector2> centralPolylinePoints2D = GeometricFunctions.ConvertListVector3ToListVector2XZ(centralPolylinePoints);
                int side = 0; //Suponemos que está a la izquierda

                for (int i = 0; i < vertices.Count; i++)
                {
                    if (_Intersections.IsPointInPolyline(stRegion.getRightStreamline(), vertices[i], 0.001f))
                        side = 1;

                    //Miramos la longitud de arco del punto
                    Vector3 pointOnPolyline =
                        TestAlgorithmsHelpMethods.FindClosestPointOnEdges(vertices[i], centralPolylinePoints, false);

                    //Debemos saber la "distancia" recorrida, es decir, la longitud de arco, desde el inicio de la línea hasta el punto
                    float totalDistance = 0f;
                    bool found = false;
                    for (int j = 0; j < centralPolylinePoints.Count - 1 && !found; j++)
                    {
                        if (_Intersections.IsPointOnLine(centralPolylinePoints[j].XZ(),
                                centralPolylinePoints[j + 1].XZ(), pointOnPolyline.XZ(), 0.001f))
                        {
                            totalDistance += Vector3.Distance(centralPolylinePoints[j], pointOnPolyline);
                            found = true;
                        }
                        else
                        {
                            totalDistance += Vector3.Distance(centralPolylinePoints[j], centralPolylinePoints[j + 1]);
                        }
                    }

                    if (!found)
                    {
                        Debug.DrawLine(pointOnPolyline, pointOnPolyline + Vector3.up, Color.red, 10f);
                        Debug.DrawLine(vertices[i], pointOnPolyline, Color.red, 10f);
                    }

                    newUVs.Add(new Vector2(side, totalDistance));
                }

                m.SetUVs(0, newUVs);
                stRegion.setTriangulatedMesh(m);

                if (stRegion.getStreamlineType().Equals(Streamline.StreamlineType.Street))
                {
                    stRegion.SetBuildingType(BuildingType.Street);
                    stRegion.SetTag("Highway");
                }
                else
                {
                    stRegion.SetBuildingType(BuildingType.Train);
                    stRegion.SetTag("Railway");
                }

                BuildingFunctions.GenerateBuildingDependingOnType(stRegion, regionsController.GetBuildingsParent(),true, newUVs);
            }
        }
    }
    
    public static void CreateConstraintRegionsWithPolylines(List<Vector3> leftPolyline, List<Vector3> rightPolyline,
        Streamline regionStreamline, List<Vector3> streamlinePoints, float displaceWidth,
        InitialStreamline.RiverParams riverParams = null)
    {
        RegionsController regionsController = RegionsController.GetInstance();
        List<StreamlineRegion> streamlineRegions = regionsController.GetStreamlineRegionsList();

        if (regionStreamline.GetStreamlineType() == Streamline.StreamlineType.River)
        {
            if (riverParams == null)
            {
                Debug.LogError("River parameters can't be null");
            }

            //Creamos ahora con la región central, una región
            StreamlineRegion centerRegion = new StreamlineRegion();
            StreamlineRegion leftGreenRegion = new StreamlineRegion();
            StreamlineRegion rightGreenRegion = new StreamlineRegion();

            float internalWidth = displaceWidth * (riverParams.riverWidthPercentage * 0.01f);

            //Línea exterior izquierda
            List<Vector3> externalRightLine = new List<Vector3>(rightPolyline);
            //Línea exterior derecha
            List<Vector3> externalLeftLine = new List<Vector3>(leftPolyline);

            //TestAlgorithmsHelpMethods.debugDrawLines(externalRightLine, Color.red, 10f, 0f);
            
            //En este caso la anchura dependerá del peso que le hayamos dado a la anchura del río
            //Además, la desviación máxima dependerá de que no se salga de los límites establecidos por las líneas exteriores
            var greenZonesInternalLines = GeometricFunctions.DisplacePolyline(streamlinePoints, internalWidth,
                (displaceWidth - internalWidth) / 2f * (riverParams.riverMaximumRandomDisplacementPercentage * 0.01f));

            //Línea interior izquierda
            List<Vector3> internalLeftLine = greenZonesInternalLines[0];
            //Línea interior derecha
            List<Vector3> internalRightLine = greenZonesInternalLines[1];

            
            //Deberemos tener en cuenta que la extensión final de los puntos finales
            //de las líneas centrales dependerá del final de las líneas exteriores
            var internalLeftLineInitialPos = TestAlgorithmsHelpMethods.CalculateIntersectionBetweenInfiniteLineAndLine(
                internalLeftLine[0].XZ(), internalLeftLine[1].XZ(),
                externalLeftLine.First().XZ(), externalRightLine.First().XZ());
            var internalRightLineInitialPos = TestAlgorithmsHelpMethods.CalculateIntersectionBetweenInfiniteLineAndLine(
                internalRightLine[0].XZ(), internalRightLine[1].XZ(),
                externalLeftLine.First().XZ(), externalRightLine.First().XZ());

            var internalLeftLineFinalPos = TestAlgorithmsHelpMethods.CalculateIntersectionBetweenInfiniteLineAndLine(
                internalLeftLine.Last().XZ(), internalLeftLine[^2].XZ(),
                externalLeftLine.Last().XZ(), externalRightLine.Last().XZ());
            var internalRightLineFinalPos = TestAlgorithmsHelpMethods.CalculateIntersectionBetweenInfiniteLineAndLine(
                internalRightLine.Last().XZ(), internalRightLine[^2].XZ(),
                externalLeftLine.Last().XZ(), externalRightLine.Last().XZ());
            
            for (var index = 0; index < internalRightLine.Count-1; index++)
            {
                var linePoint1 = internalRightLine[index];
                var linePoint2 = internalRightLine[index+1];
                //Con esto comprobamos a partir de qué índice hay que usar la polilínea
                if (_Intersections.IsPointOnLine(linePoint1.XZ(), linePoint2.XZ(), internalRightLineInitialPos, 0.001f))
                {
                    internalRightLine.RemoveRange(0, index+1);
                    break;
                }
            }
            internalRightLine[0] = internalRightLineInitialPos.XYZ();
            
            for (var index = internalRightLine.Count-1; index > 0; index--)
            {
                var linePoint1 = internalRightLine[index];
                var linePoint2 = internalRightLine[index-1];
                //Con esto comprobamos a partir de qué índice hay que usar la polilínea
                if (_Intersections.IsPointOnLine(linePoint1.XZ(), linePoint2.XZ(), internalRightLineFinalPos, 0.001f))
                {
                    internalRightLine.RemoveRange(index, internalRightLine.Count - index);
                    break;
                }
            }
            internalRightLine.Add(internalRightLineFinalPos.XYZ());

            for (var index = 0; index < internalLeftLine.Count-1; index++)
            {
                var linePoint1 = internalLeftLine[index];
                var linePoint2 = internalLeftLine[index+1];
                //Con esto comprobamos a partir de qué índice hay que usar la polilínea
                if (_Intersections.IsPointOnLine(linePoint1.XZ(), linePoint2.XZ(), internalLeftLineInitialPos, 0.001f))
                {
                    internalLeftLine.RemoveRange(0, index+1);
                    break;
                }
            }
            internalLeftLine[0] = internalLeftLineInitialPos.XYZ();
            
            for (var index = internalLeftLine.Count-1; index > 0; index--)
            {
                var linePoint1 = internalLeftLine[index];
                var linePoint2 = internalLeftLine[index-1];
                //Con esto comprobamos a partir de qué índice hay que usar la polilínea
                if (_Intersections.IsPointOnLine(linePoint1.XZ(), linePoint2.XZ(), internalLeftLineFinalPos, 0.001f))
                {
                    internalLeftLine.RemoveRange(index, internalLeftLine.Count - index);
                    break;
                }
            }
            internalLeftLine.Add(internalLeftLineFinalPos.XYZ());
            
            
            List<Vector3> auxiliarRegionPoints = new List<Vector3>();
            List<int> auxiliarCornerPoints = new List<int>();
            
            //Ahora simplemente deben unirse las 4 líneas para formar correctamente las tres zonas.

            //1. Empezando por la zona verde izquierda -> Línea exterior izquierda e interior izquierda
            //      Deberemos invertir la línea exterior izquierda
            List<Vector3> reversedList1 = new List<Vector3>();
            for (int i = externalLeftLine.Count - 1; i >= 0; i--)
                reversedList1.Add(externalLeftLine[i]);

            auxiliarRegionPoints.AddRange(reversedList1);
            auxiliarRegionPoints.AddRange(internalLeftLine);
            leftGreenRegion.SetBorderPoints(auxiliarRegionPoints);
            auxiliarCornerPoints = GeometricFunctions.CalculateCorners(auxiliarRegionPoints, regionsController.GetMaximumInteriorAngle());
            leftGreenRegion.SetCornerPoints(auxiliarCornerPoints);
            leftGreenRegion.setLeftStreamline(externalLeftLine);
            leftGreenRegion.setRightStreamline(internalLeftLine);
            leftGreenRegion.setStreamlineType(Streamline.StreamlineType.Train);
            //2. Siguiendo por la zona central
            //      Deberemos invertir la línea interior izquierda
            List<Vector3> reversedList2 = new List<Vector3>();
            for (int i = internalLeftLine.Count - 1; i >= 0; i--)
                reversedList2.Add(internalLeftLine[i]);

            
            for (var index = 0; index < internalRightLine.Count; index++)
            {
                var r = internalRightLine[index];
                //Debug.DrawLine(r, r + Vector3.up / 100f * index, Color.blue, 10f);
            }

            auxiliarRegionPoints = new List<Vector3>();
            auxiliarRegionPoints.AddRange(reversedList2);
            auxiliarRegionPoints.AddRange(internalRightLine);
            centerRegion.SetBorderPoints(auxiliarRegionPoints);
            auxiliarCornerPoints = GeometricFunctions.CalculateCorners(auxiliarRegionPoints, regionsController.GetMaximumInteriorAngle());
            centerRegion.SetCornerPoints(auxiliarCornerPoints);
            centerRegion.setLeftStreamline(internalLeftLine);
            centerRegion.setRightStreamline(internalRightLine);
            centerRegion.setStreamlineType(Streamline.StreamlineType.River);
            //3. Acabando por la zona verde derecha
            //      Deberemos invertir la línea interior derecha
            List<Vector3> reversedList3 = new List<Vector3>();
            for (int i = internalRightLine.Count - 1; i >= 0; i--)
                reversedList3.Add(internalRightLine[i]);

            auxiliarRegionPoints = new List<Vector3>();
            auxiliarRegionPoints.AddRange(reversedList3);
            auxiliarRegionPoints.AddRange(externalRightLine);
            rightGreenRegion.SetBorderPoints(auxiliarRegionPoints);
            auxiliarCornerPoints = GeometricFunctions.CalculateCorners(auxiliarRegionPoints, regionsController.GetMaximumInteriorAngle());
            rightGreenRegion.SetCornerPoints(auxiliarCornerPoints);
            rightGreenRegion.setLeftStreamline(internalRightLine);
            rightGreenRegion.setRightStreamline(externalRightLine);
            rightGreenRegion.setStreamlineType(Streamline.StreamlineType.Train);

            River river = new River(centerRegion, leftGreenRegion, rightGreenRegion);
            river.setLeftStreamline(leftPolyline);
            river.setRightStreamline(rightPolyline);
            if (streamlineRegions == null)
                streamlineRegions = new List<StreamlineRegion>();
            streamlineRegions.Add(river);
        }
        else
        {
            //Creamos ahora con la región central, una región
            StreamlineRegion centerRegion = new StreamlineRegion();

            //Línea exterior izquierda
            List<Vector3> externalRightLine = new List<Vector3>(rightPolyline);
            //Línea exterior derecha
            List<Vector3> externalLeftLine = new List<Vector3>(leftPolyline);

            List<Vector3> auxiliarRegionPoints = new List<Vector3>();
            List<int> auxiliarCornerPoints = new List<int>();

            List<Vector3> reversedList2 = new List<Vector3>();
            for (int i = externalLeftLine.Count - 1; i >= 0; i--)
                reversedList2.Add(externalLeftLine[i]);

            auxiliarRegionPoints = new List<Vector3>();
            auxiliarRegionPoints.AddRange(reversedList2);
            auxiliarRegionPoints.AddRange(externalRightLine);
            centerRegion.SetBorderPoints(auxiliarRegionPoints);
            auxiliarCornerPoints = GeometricFunctions.CalculateCorners(auxiliarRegionPoints, regionsController.GetMaximumInteriorAngle());
            centerRegion.SetCornerPoints(auxiliarCornerPoints);
            centerRegion.setLeftStreamline(externalLeftLine);
            centerRegion.setRightStreamline(externalRightLine);
            centerRegion.setStreamlineType(regionStreamline.GetStreamlineType());
            centerRegion.SetStreamline(regionStreamline);
            if (streamlineRegions == null)
                streamlineRegions = new List<StreamlineRegion>();
            streamlineRegions.Add(centerRegion);
        }
    }
    
    public static HashSet<RandomPoint> GenerateRandomPoints([CanBeNull] Region r)
    {
        RegionsController regionsController = RegionsController.GetInstance();
        List<Region> parcialRegionList = regionsController.GetParcialRegionsList();
        List<Vector3> shapePoints = new List<Vector3>();
        HashSet<RandomPoint> rpHashSet = new HashSet<RandomPoint>();
        
        if (parcialRegionList != null && parcialRegionList.Count > 0)
        {
            float area;
            if (r == null)
            {
                area = parcialRegionList[0].GetAreaKM();
                shapePoints = parcialRegionList[0].GetBorderPoints();
            }
            else
            {
                area = r.GetAreaKM();
                shapePoints = r.GetBorderPoints();
            }

            int totalN = Mathf.CeilToInt(regionsController.GetTotalRandomPointsPerKm2() * area);
            totalN = Mathf.Max(10, totalN);
            rpHashSet = TestAlgorithmsHelpMethods.GenerateRandomPointsFromPolygon(totalN, shapePoints, regionsController.GetRandomFreedom(), null);

            if (rpHashSet != null)
            {
                foreach (var v in rpHashSet)
                {
                    Vector3 closestPoint =
                        TestAlgorithmsHelpMethods.FindClosestPointOnEdges(v.getPosition(), shapePoints);
                    v.setClosestPoint(closestPoint);

                    //Ahora se buscan los dos vectores correspondientes, que son pf-p y su perpendicular
                    Vector3 normalizedVector = (v.getClosestPoint() - v.getPosition()).normalized;
                    // Intercambiamos las componentes y cambiamos el signo de una de ellas para obtener un vector perpendicular
                    Vector3 perpendicularVector =
                        new Vector3(-normalizedVector.z, normalizedVector.y, normalizedVector.x);

                    v.setCross(new Cross(normalizedVector, perpendicularVector));
                }
            }
        }

        return rpHashSet;
    }
    
    public static void GenerateStreamlines(bool generateTestLine)
    {
        RegionsController regionsController = RegionsController.GetInstance();
        Transform initialRegionParent = regionsController.GetInitialParent();
        List<Region> parcialRegionList = regionsController.GetParcialRegionsList();
        List<Region> finalRegionList = regionsController.GetFinalRegionsList();
        List<Vector3> borderPoints = parcialRegionList[0].GetBorderPoints();
        HashSet<RandomPoint> randomPoints = parcialRegionList[0].GetInteriorPoints();
        List<Streamline> streamlines = new List<Streamline>();

        int childCount = initialRegionParent.childCount;
        List<Vector2> shapePoints2D = new List<Vector2>();
        int safeInterpNumber = 750;

        //Si es la región inicial
        if (parcialRegionList.Count == 1)
        {
            for (int i = 0; i < childCount; i++)
            {
                shapePoints2D.Add(initialRegionParent.GetChild(i).transform.position.XZ());
            }
        }
        //Si es cualquier otra región
        else
        {
            shapePoints2D =
                GeometricFunctions.ConvertListVector3ToListVector2XZ(parcialRegionList[0]
                    .GetBorderPointsWithAngleDecimate(-1));
        }

        int starterInt = 0;

        if (generateTestLine)
            starterInt = Random.Range(0, borderPoints.Count);

        List<Vector3> streamlineOriginPoints = borderPoints;
        
        float h = 0.02f;
        
        var randomPointsList = randomPoints.ToList();

        var positions = randomPointsList.Select(point => point.getPosition()).ToArray();

        GridSearchBurst gsb = new GridSearchBurst(-1f, 12);
        Stopwatch stopWatch = new Stopwatch();


        gsb.initGrid(positions);
        // Detener la medición del tiempo


        //##################################################################### LANZAMIENTO DE STREAMLINES ##################################################################;
        //Lanzamos una streamline desde cada punto de los creados en streamlineOriginPoints
        for (int puntoExterior = starterInt; puntoExterior < streamlineOriginPoints.Count; puntoExterior++)
        {
            List<Vector3> pathPoints = new List<Vector3>();

            //Obtenemos un punto auxiliar
            Vector3 rp = streamlineOriginPoints[puntoExterior];

            //Añadimos el primer punto a la streamline
            pathPoints.Add(rp);


            if (randomPoints == null || randomPoints.Count == 0)
            {
                finalRegionList.Add(parcialRegionList[0]);
                parcialRegionList.RemoveAt(0);
                return;
            }

            //Interpolamos
            Vector3 force = -GeometricFunctions.FindClosestPoint(rp, randomPoints).getCross().getV1();

            InterpolationRK4.Force f = new InterpolationRK4.Force(force.x, force.z);
            InterpolationRK4.MovingPoint movingPoint = new InterpolationRK4.MovingPoint(rp.x, rp.z, f.x, f.y);
            Vector3 movingPointv3 = new Vector3(movingPoint.x, 0, movingPoint.y);

            bool validstreamline = true;
            double totalStreamlinesTime = regionsController.GetTotalStreamlinesTime();

            for (int i = 0; i < safeInterpNumber; i++)
            {
                //Hacemos las gestión de vectores en ese punto
                if (i > 0)
                {
                    Vector3 movingPointVelocityv3 = new Vector3(movingPoint.velocityX, 0, movingPoint.velocityY);
                    // closest point query
                    Vector3[] queries = new Vector3[1];
                    queries[0] = new Vector3(movingPointv3.x, movingPointv3.y, movingPointv3.z);
                    stopWatch = new Stopwatch();
                    stopWatch.Start();
                    int[] result = gsb.searchClosestPoint(queries);
                    stopWatch.Stop();
                    double elapsedTime = stopWatch.Elapsed.TotalMilliseconds;
                    totalStreamlinesTime += elapsedTime / 1000f;
                    force = GeometricFunctions.GetVectorAtPosition(movingPointv3, movingPointVelocityv3, randomPointsList[result[0]], 500);

                    f = new InterpolationRK4.Force(force.x * regionsController.GetInterpolationForceMultiplyer(), force.z * regionsController.GetInterpolationForceMultiplyer());
                }

                movingPoint = InterpolationRK4.RungeKutta4(movingPoint, f, h);

                movingPointv3 = new Vector3(movingPoint.x, 0, movingPoint.y);

                pathPoints.Add(movingPointv3);

                Vector2? lastPoint = null;
                Vector2 nowPoint = GeometricFunctions.ConvertVector3ToVector2XZ(pathPoints.Last());
                if (pathPoints.Count > 2)
                {
                    lastPoint = GeometricFunctions.ConvertVector3ToVector2XZ(pathPoints[pathPoints.Count - 2]);
                }

                if (lastPoint != null)
                {
                    Vector2? collisionPoint =
                        _Intersections.IsLineIntersectingPolygon(shapePoints2D, nowPoint, lastPoint.Value);
                    /*CollisionPoint:
                        - null -> No ha salido del polígono, se sigue interpolando
                        - vector2 estándar -> Ha salido del polígono, y devuelve el punto de colisión
                    */
                    if (collisionPoint != null)
                    {
                        //En caso de detección de la colisión, se corta la streamline
                        pathPoints.RemoveAt(pathPoints.Count - 1);
                        pathPoints.Add(new Vector3(collisionPoint.Value.x, 0, collisionPoint.Value.y));
                        break;
                    }
                }

                if (i + 1 == safeInterpNumber)
                {
                    validstreamline = false;
                }
            }
            
            regionsController.UpdateTotalStreamlinesTime(totalStreamlinesTime);

            if (!validstreamline)
                continue;
            // Unimos la streamline con el borde final
            int initialID = puntoExterior;
            Vector2Int vector2Int =
                GeometricFunctions.GetIndexBorderPoint(pathPoints.First(), pathPoints.Last(), borderPoints);
            int finalID = vector2Int.y;

            if (finalID == -1)
                Debug.LogError("FINAL ID ES -1");
            if (borderPoints.Count == 0)
                Debug.LogError("BORDERPOINTS = 0");


            /*
             * Vamos a calcular las dos regiones que haría cada streamline de forma previa a la elección para poder comparar
             * las áreas y darle mayor puntuación a las streamlines que generen regiones con áreas similares
             */

            List<Vector3> borderPointsRegion = borderPoints;

            // Convertimos los puntos de la streamline a puntos de bordes de las nuevas regiones
            List<Vector3> streamlinePoints = pathPoints;
            List<Vector3> borderLeftPoints = new List<Vector3>();
            List<Vector3> borderRightPoints = new List<Vector3>();

            for (int i = 0; i < borderPointsRegion.Count; i++)
            {
                Vector3 punto = borderPointsRegion[i];

                if (i == initialID)
                {
                    //Añadimos la streamline a la región izquierda
                    for (int j = 0; j < streamlinePoints.Count; j++)
                        borderLeftPoints.Add(streamlinePoints[j]);

                    if (borderLeftPoints.Last() != streamlinePoints.Last())
                        borderLeftPoints.Add(streamlinePoints.Last());
                }
                else if (i == finalID)
                {
                    // Añadimos la streamline a la región derecha
                    for (int j = streamlinePoints.Count - 1; j >= 0; j--)
                        borderRightPoints.Add(streamlinePoints[j]);

                    if (borderRightPoints.Last() != streamlinePoints.First())
                        borderRightPoints.Add(streamlinePoints.First());
                }
                else if (i < initialID || i > finalID)
                {
                    borderLeftPoints.Add(punto);
                }
                else // i < finalID && i > initialID
                    borderRightPoints.Add(punto);
            }

            float leftArea = GeometricFunctions.CalculatePolylineArea(borderLeftPoints);
            float rightArea = GeometricFunctions.CalculatePolylineArea(borderRightPoints);

            // Añadimos una streamline por borde, y calculamos su puntuación
            streamlines.Add(new Streamline(pathPoints, initialID, finalID,
                StreamlineScore(pathPoints, parcialRegionList[0].GetOrientacion(), leftArea, rightArea, parcialRegionList),
                Streamline.StreamlineType.Street, regionsController.GetMaximumRoadWidth()));

            if (generateTestLine)
                break;
        }
        
        regionsController.UpdateStreamlines(streamlines);

        gsb.clean(); //Free up the native arrays !

        //############################################################################################################################################################################


        //############################################################### MANEJO DE ERRORES #####################################################################
        if (borderPoints == null)
            Debug.LogError("borderPoints == null");
        else if (borderPoints.Count == 0)
            Debug.LogError("borderPoints.Count == 0");
        
        if (parcialRegionList == null)
            Debug.LogError("regionList == null");
        else if (parcialRegionList.Count == 0)
            Debug.LogError("regionList.Count == 0");
        //############################################################################################################################################################################

        if (streamlines.Count > 0)
        {
            parcialRegionList[0].SetStreamline(streamlines[ObtainMaximumStreamlineScore(streamlines)]);
            regionsController.UpdateFinalStreamline(parcialRegionList[0].GetStreamline());
        }
    }
    
    public static void CreateInitialPolylinesMeshes()
    {
        RegionsController regionsController = RegionsController.GetInstance();
        List<StreamlineRegion> streamlineRegions = regionsController.GetStreamlineRegionsList();
        List<InitialStreamline> initialPolylinesForRuntime = regionsController.GetInitialPolylinesForRuntime();
        RegionsFunctions.InitRegions(true);
        //RegionsFunctions.InitRegionsTest(true);

        StreamlineRegion[] auxiliarStreamlineRegionList = new StreamlineRegion[streamlineRegions.Count];
        streamlineRegions.CopyTo(auxiliarStreamlineRegionList);

        InitialStreamline[] auxiliarInitialStreamlines = new InitialStreamline[initialPolylinesForRuntime.Count];
        initialPolylinesForRuntime.CopyTo(auxiliarInitialStreamlines);

        regionsController.ResetAll();

        regionsController.UpdateStreamlineRegionsList(auxiliarStreamlineRegionList.ToList());
        regionsController.UpdateInitialPolylinesForRuntime(auxiliarInitialStreamlines.ToList());
    }

    public static void CreateInitialPolylinesMeshesTesting()
    {
        RegionsController regionsController = RegionsController.GetInstance();
        List<StreamlineRegion> streamlineRegions = regionsController.GetStreamlineRegionsList();
        List<InitialStreamline> initialPolylinesForRuntime = regionsController.GetInitialPolylinesForRuntime();
        RegionsFunctions.InitRegions(true);

        StreamlineRegion[] auxiliarStreamlineRegionList = new StreamlineRegion[streamlineRegions.Count];
        streamlineRegions.CopyTo(auxiliarStreamlineRegionList);

        InitialStreamline[] auxiliarInitialStreamlines = new InitialStreamline[initialPolylinesForRuntime.Count];
        initialPolylinesForRuntime.CopyTo(auxiliarInitialStreamlines);

        regionsController.ResetAll();
        
        regionsController.UpdateStreamlineRegionsList(auxiliarStreamlineRegionList.ToList());
        regionsController.UpdateInitialPolylinesForRuntime(auxiliarInitialStreamlines.ToList());

        FillStreamlineRegions();
    }
}
