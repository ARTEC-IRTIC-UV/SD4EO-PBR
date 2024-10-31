using System;
using System.Collections.Generic;
using UnityEngine;
using Habrador_Computational_Geometry;
using DefaultNamespace.Regions;
using Unity.EditorCoroutines.Editor;
using VInspector;
using Debug = UnityEngine.Debug;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

[ExecuteInEditMode]
public class RegionsController : MonoBehaviour
{
    private float kmScale = 0.001f;
    private double totalStreamlinesTime;
    private EditorCoroutine editorCorutine;

    [Foldout("Parent variables")]
    [SerializeField] private Transform polylinesParent;
    [SerializeField] private Transform zonesParent;
    [SerializeField] private Transform buildingsParent;
    [SerializeField] private VoronoiController voronoiController;
    
    [Tab("Initial Region")]
    [SerializeField] private Material defaultFloorMaterial;
    public bool randomInitialRegion;
    [HideIf("randomInitialRegion")] public Transform initialRegionParent;
    [EndIf]
    
    [ShowIf("randomInitialRegion")]
    [Range(10, 50)] [SerializeField] private int numberOfPoints = 20;
    [Range(0, 500)] [SerializeField] public int randomSeed;
    [Header("Length unit: metre (m)")]
    [Range(500f, 2500f)] [SerializeField] private float minimumDistanceToCenter = 1000f;
    [Range(2500f, 5000f)] [SerializeField] private float maximumDistanceToCenter = 3000f;

    [EndIf]
    [Tab("Initial Polylines")]
    [SerializeField] private Material waterMaterial;
    [SerializeField] private Material streetMaterial;
    [SerializeField] private Material railMaterial;
    [Range(100f, 2000f)] [SerializeField] private float maxPointOffset;
    public bool showPolylines;
    public List<InitialStreamline> initialPolylinesOnInspector;
    private List<InitialStreamline> initialPolylinesForRuntime;
    public string newPolylineName;

    [Tab("Zones")] public Material industrialFloorMaterial;
    public Material residentialFloorMaterial;
    public bool showZones;

    [Header("Default (not in a zone) width of the roads")]
    [Range(0.005f, 0.05f)] [HideInInspector] [SerializeField] private float maximumRoadWidth;

    [Header("This value defines if the default (not in a zone) voronoi regions will be Manhattan-like" +
            "(grid, values close to 0) or they will have freedom (values close to 1).")]
    [Range(0f, 1f)] [HideInInspector] [SerializeField] private float voronoiDefaultFreedom;

    [SerializeField] private List<Zone> initialZones;
    public string newZoneName;

    [Tab("Templates")] [SerializeField] private GameObject downtownTemplates;
    [SerializeField] private GameObject industrialTemplates;
    [SerializeField] private GameObject residentialTemplates;
    [SerializeField] private GameObject fieldCropsTemplates;

    [Tab("Scriptable Objects")]
    [SerializeField] private List<GeometryObject> downtownBuildingRoofOptions;
    [SerializeField] private List<GeometryObject> downtownBuildingCorniceOptions;
    [SerializeField] private List<GeometryObject> residentialBuildingRoofOptions;
    [SerializeField] private List<GeometryObject> residentialBuildingCorniceOptions;
    [SerializeField] private List<GeometryObject> industrialBuildingRoofOptions;
    [SerializeField] private List<GeometryObject> industrialBuildingCorniceOptions;
    [SerializeField] private List<GeometryObject> vegetationOptions;
    [SerializeField] private List<GeometryObject> parkingOptions;
    [SerializeField] private List<GeometryObject> cropFieldsOptions;

    [Tab("Rendering materials")]
    [SerializeField] private Material whiteMaterial;
    [SerializeField] private Material greenMaterial;
    [SerializeField] private Material redMaterial;
    [SerializeField] private Material blackMaterial;
    [SerializeField] private Material residentialBuildingMaterial;
    [SerializeField] private Material nonResidentialBuildingMaterial;
    [SerializeField] private Material residentialZoneMaterial;
    [SerializeField] private Material industrialZoneMaterial;
    [SerializeField] private Material riverMaterial;
    [SerializeField] private Material vegetationMaterial;
    [SerializeField] private Material roadMaterial;
    [SerializeField] private Material highwayMaterial;
    [SerializeField] private Material railwayMaterial;
    [SerializeField] private Material alfalfaOrLucerneMaterial;
    [SerializeField] private Material barleyMaterial;
    [SerializeField] private Material fallowAndBareSoilMaterial;
    [SerializeField] private Material oatsMaterial;
    [SerializeField] private Material otherGrainLeguminousMaterial;
    [SerializeField] private Material peasMaterial;
    [SerializeField] private Material sunflowerMaterial;
    [SerializeField] private Material vetchMaterial;
    [SerializeField] private Material wheatMaterial;
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

    [Tab("Rendering options")]
    [Range(1, 100)] [SerializeField] private int numberOfRandomMaps = 1;
    [SerializeField] private Material denoiseMaterial;
    [SerializeField] private int mainCameraRenderResolution;
    [SerializeField] private int mainCameraMetresPixelFactor;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private int zoomCamerasRenderResolution;
    [SerializeField] private int zoomCamerasMetresPixelFactor;
    [SerializeField] private Camera[] zoomCameras;
    [SerializeField] private int renderInitialNumber;
    [SerializeField] private bool cropFields;
    [SerializeField] private bool makeGeojson;
    private Month month;
    private CropFieldTexture cropFieldTexture;
    [SerializeField] private bool renderColor;
    [SerializeField] private bool renderB8;
    [ShowIf("cropFields")]
    [SerializeField] private bool renderSar;
    [SerializeField] private bool renderB11;
    [EndIf] [HideIf("cropFields")]
    [SerializeField] private bool renderLayers;
    [SerializeField] private bool renderSolarPanelsMask;
    [SerializeField] private bool renderBuildingsMask;
    [EndIf]
    // Dictionary to link month names with numbers
    private Dictionary<string, int> monthDictionary;

    [Tab("Debug")]
    [Foldout("Configuration variables")]
    [Range(10, 50)] [SerializeField] private int streamlinesPerRegion = 20;
    [Range(5f, 50f)] [SerializeField] private float clippingMinimumPlotSide = 20f;
    [Range(3f, 5f)] [SerializeField] private float distanceDifferenceFactor = 3f;
    [Range(5f, 25f)] [SerializeField] private float clearDistance = 10f;
    [Tooltip("This value defines how many points will be generated for the shape MBR.")]
    [Range(0, 1500)] [SerializeField] private int totalRandomPointsPerKm2 = 500;
    [Tooltip("This value defines how much a point is allowed to move from the center of its cell.")]
    [Range(0, 1f)] [SerializeField] private float randomFreedom = 0.9f;
    
    [SerializeField][Range(1, 10)] private int interpolationForceMultiplyer = 3;
    [SerializeField] private float maximumCornerAngle = 135f;
    [SerializeField] private float maximumInteriorAngle = 179f;
    private float rotationAngle = 0f;
    private InterpolationRK4.MovingPoint movingPoint;
    private List<Vector3> shapePoints;
    private List<Streamline> streamlines;
    private List<Vector3> initialRegionBorders;
    private Streamline finalStreamline;
    [SerializeField] [HideInInspector] private List<Region> parcialRegionList;
    [SerializeField] [HideInInspector] private List<Region> finalRegionList;
    [SerializeField] [HideInInspector] private List<Region> finalBuildingRegionList;

    private List<StreamlineRegion> streamlineRegions;
    private Region initialRegion;
    private List<Streamline> templateStreamlines;
    private int contadorPolilineas = 10;
    [EndFoldout]
    [Tab("Debug")]
    public bool showRegions;
    [ShowIf("showRegions")]
    public bool debugRegionsNumberHandles;
    public bool debugRegionBorderHandles;
    public bool debugInteriorPoints;
    public bool debugRegionMesh;
    public bool debugStreamlines;
    public int actualRegionID;
    public bool showAllRegions;
    [EndIf]

    [Tab("Debug")]
    [Range(0, 1f)] private float timeBetweenSteps = 0.5f;

    private CustomYieldInstructions.CustomCorutine customCorutine;
    private bool corutineIsWorking;
    
    /*
     * ################################# SINGLETON PATTERN ######################################
     */
    private static RegionsController _instance;

    public static RegionsController GetInstance()
    {
        return _instance;
    }
    
    // Awake method to initialize the instance
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            DestroyImmediate(gameObject);
        }
    }

    /*
     * ################################# FUNCIONES DE CORUTINA ######################################
     */
    private void OnEnable()
    {
        customCorutine = new CustomYieldInstructions.CustomCorutine();
        customCorutine.OnVariableChanged += UpdateCorutineWorkingVariable;
    }

    private void UpdateCorutineWorkingVariable(bool value)
    {
        Debug.Log("UPDATING VARIABLE!");
        corutineIsWorking = value;
    }

    /*
     * ################################# FUNCIONES PARA LA INTERFAZ ######################################
     */

    [Tab("Initial Polylines")]
    [Button("Create new polyline")]
    public void CreateNewPolyline()
    {
        GameObject newPolylineParent = new GameObject();
        GameObject firstPolylinePoint = new GameObject();
        firstPolylinePoint.transform.position = new Vector3(-1f, 0f, 1f);
        GameObject mediumPolylinePoint = new GameObject();
        mediumPolylinePoint.transform.position = new Vector3(0f, 0f, 0f);
        GameObject secondPolylinePoint = new GameObject();
        secondPolylinePoint.transform.position = new Vector3(1f, 0f, -1f);
        newPolylineParent.transform.parent = polylinesParent;
        newPolylineParent.name = newPolylineName;
        firstPolylinePoint.transform.parent = newPolylineParent.transform;
        mediumPolylinePoint.transform.parent = newPolylineParent.transform;
        secondPolylinePoint.transform.parent = newPolylineParent.transform;
        InitialStreamline newStreamline = newPolylineParent.AddComponent<InitialStreamline>();
        newStreamline.Initialize(newPolylineParent.transform, null, 
            Streamline.StreamlineType.Street, maximumRoadWidth);
        initialPolylinesOnInspector.Add(newStreamline);
    }

    [Tab("Zones")]
    [Button("Create new zone")]
    public void CreateNewZone()
    {
        GameObject newZoneParent = new GameObject();
        newZoneParent.transform.parent = zonesParent;
        newZoneParent.name = newZoneName;
        Zone newZone = newZoneParent.AddComponent<Zone>();
        newZone.Initialize(newZoneParent.transform, ZoneType.Downtown,ZoneShape.Circle, 500f, 0f, 10f, 50f,
            1f, 1, 1f, new List<Material>());
        initialZones.Add(newZone);
    }
    
    /*
     * ################################# FUNCIONES DE STREAMLINE-BASED SPLITTING ######################################
     */

    [Tab("Debug")]
    //[Button("Init principal region")]
    public void InitGeneralRegionCaller()
    {
        // Reseteamos cualquier dato que haya del mapa
        ResetAll();
        InitGeneralRegion();
    }

    [Tab("Debug")]
    //[Button("Init principal region with NO RESET")]
    public void InitGeneralRegion()
    {
        // Reseteamos cualquier dato que haya del mapa
        //ResetAll();

        // Definimos 2 tipos de creación de región inicial, uno aleatorio y otro definido
        if (randomInitialRegion)
        {
            List<Vector3> initialPoints = RegionsFunctions.GenerateRegionRandomPoints(Vector3.zero, numberOfPoints,
                GetMinimumDistanceToCenter(), GetMaximumDistanceToCenter(), randomSeed);
            shapePoints.AddRange(initialPoints);
        }
        else
        {
            int childCount = initialRegionParent.childCount;

            for (int i = 0; i < childCount; i++)
            {
                shapePoints.Add(initialRegionParent.GetChild(i).transform.position);
            }
        }

        // Una vez hemos definido la forma de la región inicial, la creamos
        Region principalRegion = new Region();
        principalRegion.SetArea(GeometricFunctions.CalculatePolylineArea(shapePoints));
        principalRegion.SetBorderPoints(shapePoints);
        principalRegion.SetCornerPoints(GeometricFunctions.CalculateCorners(shapePoints, maximumInteriorAngle));
        principalRegion.SetOrientacionRegion();
        principalRegion.ClearAndCreateIntermediatePoints(streamlinesPerRegion, GetClearDistance(), GetMaximumCornerAngle());
        initialRegion = principalRegion;
        initialRegion.SetBorderPoints(GeometricFunctions.DisplaceBorderPoints(principalRegion.GetBorderPoints(), 0.01f));
        initialRegion.TriangulateRegion();
        TemplateFunctions.FillInitRegion(initialRegion);
        principalRegion.SetInteriorPoints(StreamlinesFunctions.GenerateRandomPoints(principalRegion));
        parcialRegionList.Add(principalRegion);
    }
    
    [Tab("Debug")]
    //[Button("Init constraint regions for meshes")]
    public void InitRegionsCallerForMeshes()
    {
        StreamlinesFunctions.CreateInitialPolylinesMeshesTesting();
    }
    
    public void CalculateVoronoi()
    {
        voronoiController.calculateVoronoi(parcialRegionList);
        RegionsFunctions.DefineZoneOfRegions();
    }

    [Tab("Debug")]
    //[Button("Calculate Voronoi")]
    public void CalculateVoronoiCaller()
    {
        ResetAll();
        InitGeneralRegion();
        voronoiController.calculateVoronoi(parcialRegionList);
        
        RegionsFunctions.DefineZoneOfRegions();
    }

    // Para cada región, creamos carreteras exteriores
    [Tab("Debug")]
    //[Button("Adjust Regions")]
    public void AdjustRegionsCaller()
    {
        RegionsFunctions.AdjustRegions();
    }

    [Tab("Debug")]
    //[Button("Init constraint regions")]
    public void InitRegionsCaller()
    {
        //RegionsFunctions.InitRegions();
        //RegionsFunctions.InitRegionsTest(false);
    }
    
    public void RandomizeInitialRegion()
    {
        randomSeed = Random.Range(1, 500);
        numberOfPoints = Random.Range(20, 50);
        minimumDistanceToCenter = Random.Range(1000f, 1250f);
        maximumDistanceToCenter = Random.Range(1500f, 1750f);
    }

    [Tab("Initial Polylines")]
    //[Button("Randomize initial polylines")]
    public void RandomizeInitialPolylines()
    {
        foreach (var polyline in initialPolylinesOnInspector)
        {
            polyline.SetType(OtherFunctions.GetRandomEnumElement<Streamline.StreamlineType>());

            Random.InitState(DateTime.Now.Millisecond * Random.Range(0,10));
            float random = Random.Range(0f, 1f);
            bool usePolyline;
            if (random < 0.5f)
                usePolyline = true;
            else
                usePolyline = false;
            
            polyline.SetUsePolyline(usePolyline);

            switch (polyline.GetType())
            {
                case Streamline.StreamlineType.River:
                    polyline.SetWidth(Random.Range(20f, 50f));
                    polyline.SetRiverWidthPercentage(Random.Range(20f, 90f));
                    polyline.SetRiverMaximumRandomDisplacementPercentage(Random.Range(0f, 100f));
                    polyline.SetGreenZoneMaximumRandomDisplacementPercentage(Random.Range(0f, 100f));
                    break;
                case Streamline.StreamlineType.Train:
                    polyline.SetWidth(Random.Range(20f, 30f));
                    break;
                case Streamline.StreamlineType.Street:
                    polyline.SetWidth(Random.Range(15f, 30f));
                    break;
            }

            if (polyline.GetParent() != null)
            {
                List<Vector3> polylinePoints = new List<Vector3>();
                Transform parent = polyline.GetParent();
                int numPoints = parent.childCount;
                for (int i = 0; i < numPoints; i++)
                {
                    Transform child = parent.GetChild(i);
                    float offsetZ = Random.Range(-GetMaxPointOffset(), GetMaxPointOffset());
                    Vector3 offset = new Vector3(child.localPosition.x, 0f, offsetZ);
                    child.localPosition = offset;
                    polylinePoints.Add(parent.GetChild(i).position);
                }

                polyline.SetPoints(polylinePoints);
            }
        }
        
        polylinesParent.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
    }

    [Tab("Zones")]
    //[Button("Randomize zones")]
    public void RandomizeZones()
    {

        foreach (var zone in initialZones)
        {
            if (zone.randomize)
            {
                zone.SetZoneShape(OtherFunctions.GetRandomEnumElement<ZoneShape>());
                zone.SetShapeAngle(Random.Range(0f, 180f));
                
                float radius, angle, angleRad, offsetX, offsetZ;
                Vector3 p;
                
                switch (zone.GetZoneType())
                {
                    case ZoneType.Downtown:
                        radius = Random.Range(0f, 0.25f);
                        angle = Random.Range(0, 360f);
                        angleRad = Mathf.Deg2Rad * angle;
                        offsetX = radius * Mathf.Cos(angleRad);
                        offsetZ = radius * Mathf.Sin(angleRad);
                        p = Vector3.zero + new Vector3(offsetX, 0f, offsetZ);
                        zone.GetParent().position = p;
                        zone.SetShapeSide(Random.Range(1000, 1500f));
                        zone.SetRoadWidth(Random.Range(5f, 10f));
                        zone.SetMaximumPlotSide(Random.Range(40f, 60f));
                        zone.SetZoneWeight(Random.Range(7.5f, 10f));
                        zone.SetVoronoiRandomFreedom(Random.Range(0.7f, 1f));
                        break;
                    case ZoneType.ResidentialArea:
                        radius = Random.Range(1f, 1.5f);
                        angle = Random.Range(0, 360);
                        angleRad = Mathf.Deg2Rad * angle;
                        offsetX = radius * Mathf.Cos(angleRad);
                        offsetZ = radius * Mathf.Sin(angleRad);
                        p = Vector3.zero + new Vector3(offsetX, 0f, offsetZ);
                        zone.GetParent().position = p;
                        zone.SetShapeSide(Random.Range(1500f, 2000f));
                        zone.SetRoadWidth(Random.Range(10f, 15f));
                        zone.SetMaximumPlotSide(Random.Range(60f, 80f));
                        zone.SetZoneWeight(Random.Range(1f, 6f));
                        zone.SetVoronoiRandomFreedom(Random.Range(0.25f, 0.75f));
                        break;
                    case ZoneType.IndustrialArea:
                        radius = Random.Range(1f, 1.5f);
                        angle = Random.Range(0, 360);
                        angleRad = Mathf.Deg2Rad * angle;
                        offsetX = radius * Mathf.Cos(angleRad);
                        offsetZ = radius * Mathf.Sin(angleRad);
                        p = Vector3.zero + new Vector3(offsetX, 0f, offsetZ);
                        zone.GetParent().position = p;
                        zone.SetShapeSide(Random.Range(1000f, 2500f));
                        zone.SetRoadWidth(Random.Range(15f, 20f));
                        zone.SetMaximumPlotSide(Random.Range(100f, 150f));
                        zone.SetZoneWeight(Random.Range(1f, 6f));
                        zone.SetVoronoiRandomFreedom(Random.Range(0.1f, 0.5f));
                        break;
                    case ZoneType.FieldCrops:
                        radius = Random.Range(1f, 7f);
                        angle = Random.Range(0, 360);
                        angleRad = Mathf.Deg2Rad * angle;
                        offsetX = radius * Mathf.Cos(angleRad);
                        offsetZ = radius * Mathf.Sin(angleRad);
                        p = Vector3.zero + new Vector3(offsetX, 0f, offsetZ);
                        zone.GetParent().position = p;
                        zone.SetShapeSide(Random.Range(5000f, 10000f));
                        zone.SetRoadWidth(Random.Range(5f, 10f));
                        zone.SetMaximumPlotSide(Random.Range(300f, 1000f));
                        zone.SetZoneWeight(Random.Range(1f, 6f));
                        zone.SetVoronoiRandomFreedom(Random.Range(0.5f, 1f));
                        break;
                }
            }
        }
    }

    [Tab("Debug")]
    //[Button("Make a step in generate map")]
    public void MakeAStepInGeneratingMultipleRegions()
    {
        if (parcialRegionList == null || parcialRegionList.Count == 0)
            return;
        
        // Realizamos el proceso de dividir la región con mayor área
        if (parcialRegionList[0].GetInteriorPoints() == null || parcialRegionList[0].GetInteriorPoints().Count == 0)
            parcialRegionList[0].SetInteriorPoints(StreamlinesFunctions.GenerateRandomPoints(parcialRegionList[0]));
        
        // Realizamos el proceso de dividir la región con mayor área
        StreamlinesFunctions.GenerateStreamlines(false);

        if (parcialRegionList.Count > 0 && parcialRegionList[0] != null)
        {
            if (parcialRegionList[0].GetStreamline() != null)
            {
                RegionsFunctions.DivideRegion(parcialRegionList[0], parcialRegionList[0].GetStreamline());
                streamlines = new List<Streamline>();
            }
            else if (parcialRegionList[0].GetStreamline() == null || parcialRegionList[0].GetStreamline().GetStreamlinePoints().Count == 0)
            {
                finalRegionList.Add(parcialRegionList[0]);
                parcialRegionList.RemoveAt(0);
            }
        }
    }

    [ButtonSpace(10f)]
    [Button("Generate dataset")]
    public void GenerateMultipleMaps()
    {
        Awake();
        ChangeCamerasAndMapSize();
        for (int i = 0; i < numberOfRandomMaps; i++)
        {
            RandomizeInitialPolylines();
            RandomizeZones();
            RegionsFunctions.GenerateMap();
        }
    }

    //[Button("Generate map in loop")]
    //[HideIf("corutineIsWorking", true)]
    public void GenerateMultipleRegionsInLoop()
    {
        ChangeCamerasAndMapSize();
        if (customCorutine == null)
        {
            customCorutine = new CustomYieldInstructions.CustomCorutine();
            customCorutine.OnVariableChanged += UpdateCorutineWorkingVariable;
        }

        editorCorutine = EditorCoroutineUtility.StartCoroutineOwnerless(RegionsFunctions.GenerateMapWithSteps(customCorutine));
    }

    //[Button("Stop corutine")]
    //[HideIf("corutineIsWorking", false)]
    public void StopGenerateMultipleRegionsInLoop()
    {
        if (customCorutine != null && editorCorutine != null)
        {
            EditorCoroutineUtility.StopCoroutine(editorCorutine);
            customCorutine.SetComplete();
            corutineIsWorking = false;
        }
    }

    /*
     * ################################# FUNCIÓN DE RESET ######################################
     */
    
    [Button("Reset scene")]
    public void ResetAll()
    {
        initialPolylinesForRuntime = new List<InitialStreamline>();
        streamlineRegions = new List<StreamlineRegion>();
        shapePoints = new List<Vector3>();
        parcialRegionList = new List<Region>();
        finalRegionList = new List<Region>();
        finalBuildingRegionList = new List<Region>();
        initialRegion = null;
        streamlines = new List<Streamline>();
        originalMaterials.Clear();
        
        foreach (Transform child in buildingsParent)
        {
            DestroyImmediate(child.gameObject);
        }

        voronoiController.ResetAll();
        int childcount = buildingsParent.childCount;
        List<GameObject> objectsToDelete = new List<GameObject>();

        for (int i = 0; i < childcount; i++)
        {
            objectsToDelete.Add(buildingsParent.GetChild(i).gameObject);
        }

        foreach (var o in objectsToDelete)
            DestroyImmediate(o);
    }

    //[Button("Change cameras size")]
    // 2000f is a magic number because we take Unity metres as kilometres (1000) and camera size is measured for each side (2)
    public void ChangeCamerasAndMapSize()
    {
        if (mainCameraRenderResolution == 0)
            mainCameraRenderResolution = 1024;
        
        if (mainCameraMetresPixelFactor == 0)
            mainCameraMetresPixelFactor = 10;
        
        if (zoomCamerasRenderResolution == 0)
            zoomCamerasRenderResolution = 512;
        
        if (zoomCamerasMetresPixelFactor == 0)
            zoomCamerasMetresPixelFactor = 1;
        
        float size = mainCameraRenderResolution * mainCameraMetresPixelFactor / 2000f;
        float clampedSize = Mathf.Min(size, 5f);
        Debug.Log(size + " " + clampedSize);
        
        if (mainCamera != null && mainCamera.orthographic && mainCameraRenderResolution > 0 && mainCameraMetresPixelFactor > 0)
            mainCamera.orthographicSize = clampedSize;

        if (zoomCameras != null && zoomCameras.Length > 0 && zoomCamerasRenderResolution > 0 && zoomCamerasMetresPixelFactor > 0)
        {
            foreach (var zoom in zoomCameras)
            {
                if (zoom.orthographic)
                    zoom.orthographicSize = zoomCamerasRenderResolution * zoomCamerasMetresPixelFactor / 2000f;
            }
        }
        
        // Also, we have to adapt the map to the camera, in order to have a map with the same size as the camera
        SetSquarePositions(clampedSize*2f);
    }
    
    public void SetSquarePositions(float squareSize)
    {
        float halfSize = squareSize / 2.0f;
        Vector3 center = transform.position;  // Center of the square is the parent GameObject's position

        // Set positions of the corners
        initialRegionParent.GetChild(0).position = center + new Vector3(-halfSize, 0, halfSize);
        initialRegionParent.GetChild(1).position = center + new Vector3(-halfSize, 0, -halfSize);
        initialRegionParent.GetChild(2).position = center + new Vector3(halfSize, 0, -halfSize);
        initialRegionParent.GetChild(3).position = center + new Vector3(halfSize, 0, halfSize);
    }

    /*
     * ################################# DIBUJADO DE GIZMOS ######################################
     */

    private void OnDrawGizmos()
    {
        Awake();
        
        if (showPolylines)
        {
            DrawingFunctions.ShowInitialPolylines(initialPolylinesOnInspector);
        }

        if (showZones)
        {
            DrawingFunctions.ShowZones(initialZones);
        }

        if (showRegions)
        {
            DrawingFunctions.ShowRegionsInformation(GetParcialRegionsList(), showAllRegions, actualRegionID, debugRegionMesh, debugInteriorPoints, debugRegionsNumberHandles, debugRegionBorderHandles, Color.white);
            DrawingFunctions.ShowRegionsInformation(GetFinalRegionsList(), showAllRegions, actualRegionID, debugRegionMesh, debugInteriorPoints, debugRegionsNumberHandles, debugRegionBorderHandles, Color.black);
        }

        if (debugStreamlines)
        {
            DrawingFunctions.ShowStreamlines(parcialRegionList, streamlines, finalStreamline);
        }
    }

    /*
     * ################################# GETTERS ######################################
     */

    public List<Region> GetParcialRegionsList()
    {
        return parcialRegionList;
    }

    public List<Region> GetFinalRegionsList()
    {
        return finalRegionList;
    }

    public List<Region> GetFinalBuildingsRegionsList()
    {
        return finalBuildingRegionList;
    }

    public List<StreamlineRegion> GetStreamlineRegionsList()
    {
        return streamlineRegions;
    }

    public void UpdateStreamlineRegionsList(List<StreamlineRegion> sr)
    {
        streamlineRegions = sr;
    }

    public List<InitialStreamline> GetInitialPolylinesOnInspector()
    {
        return initialPolylinesOnInspector;
    }

    public List<InitialStreamline> GetInitialPolylinesForRuntime()
    {
        return initialPolylinesForRuntime;
    }
    
    public void UpdateInitialPolylinesForRuntime(List<InitialStreamline> initialPolylines)
    {
        initialPolylinesForRuntime = initialPolylines;
    }

    public List<Vector3> GetInitialRegionBorders()
    {
        return initialRegion.GetBorderPoints();
    }

    public Transform GetBuildingsParent()
    {
        return buildingsParent;
    }

    public Transform GetInitialParent()
    {
        return initialRegionParent;
    }

    public List<Zone> GetInitialZones()
    {
        return initialZones;
    }

    public void UpdateStreamlines(List<Streamline> streamlines)
    {
        this.streamlines = streamlines;
    }

    public float GetMaxPointOffset()
    {
        return maxPointOffset * kmScale;
    }

    public int GetTotalRandomPointsPerKm2()
    {
        return totalRandomPointsPerKm2;
    }

    public float GetRandomFreedom()
    {
        return randomFreedom;
    }

    public double GetTotalStreamlinesTime()
    {
        return totalStreamlinesTime;
    }

    public void UpdateTotalStreamlinesTime(double time)
    {
        totalStreamlinesTime = time;
    }

    public void UpdateFinalStreamline(Streamline streamline)
    {
        finalStreamline = streamline;
    }
    
    public float GetClippingMinimumArea()
    {
        return clippingMinimumPlotSide * clippingMinimumPlotSide * kmScale * kmScale;
    }

    public int GetStreamlinesPerRegion()
    {
        return streamlinesPerRegion;
    }

    public float GetDistanceDifferenceFactor()
    {
        return distanceDifferenceFactor;
    }

    public float GetClearDistance()
    {
        return clearDistance * kmScale;
    }

    public float GetMinimumDistanceToCenter()
    {
        return minimumDistanceToCenter * kmScale;
    }

    public float GetMaximumDistanceToCenter()
    {
        return maximumDistanceToCenter * kmScale;
    }

    public int GetContadorPolilineas()
    {
        return contadorPolilineas;
    }

    public float GetVoronoiDefaultFreedom()
    {
        return voronoiDefaultFreedom;
    }

    public float GetMaximumInteriorAngle()
    {
        return maximumInteriorAngle;
    }
    
    public float GetMaximumCornerAngle()
    {
        return maximumCornerAngle;
    }

    public float GetTimeBetweenSteps()
    {
        return timeBetweenSteps;
    }

    public float GetMaximumRoadWidth()
    {
        return maximumRoadWidth;
    }

    public int GetInterpolationForceMultiplyer()
    {
        return interpolationForceMultiplyer;
    }

    public string GetRenderInitialNumber()
    {
        return renderInitialNumber.ToString("D3");
    }

    public void NextInitialNumber()
    {
        renderInitialNumber++;
    }

    public Material GetDenoiseMaterial()
    {
        return denoiseMaterial;
    }

    public Material GetDefaultFloorMaterial()
    {
        return defaultFloorMaterial;
    }

    public Material GetWaterMaterial()
    {
        return waterMaterial;
    }
    
    public Material GetStreetMaterial()
    {
        return streetMaterial;
    }
    
    public Material GetTrainMaterial()
    {
        return railMaterial;
    }
    
    public Material GetIndustrialFloorMaterial()
    {
        return industrialFloorMaterial;
    }

    public Material GetResidentialFloorMaterial()
    {
        return residentialFloorMaterial;
    }

    public List<GeometryObject> GetDowntownBuildingRoofOptions()
    {
        return downtownBuildingRoofOptions;
    }

    public List<GeometryObject> GetDowntownBuildingCorniceOptions()
    {
        return downtownBuildingCorniceOptions;
    }

    public List<GeometryObject> GetResidentialBuildingRoofOptions()
    {
        return residentialBuildingRoofOptions;
    }

    public List<GeometryObject> GetResidentialBuildingCorniceOptions()
    {
        return residentialBuildingCorniceOptions;
    }

    public List<GeometryObject> GetIndustrialBuildingRoofOptions()
    {
        return industrialBuildingRoofOptions;
    }

    public List<GeometryObject> GetIndustrialBuildingCorniceOptions()
    {
        return industrialBuildingCorniceOptions;
    }

    public List<GeometryObject> GetParkingOptions()
    {
        return parkingOptions;
    }

    public List<GeometryObject> GetVegetationOptions()
    {
        return vegetationOptions;
    }

    public List<GeometryObject> GetCropFieldOptions()
    {
        return cropFieldsOptions;
    }
    
    public Material GetRenderWhiteMaterial()
    {
        return whiteMaterial;
    }
    
    public Material GetRenderGreenMaterial()
    {
        return greenMaterial;
    }
    
    public Material GetRenderRedMaterial()
    {
        return redMaterial;
    }
    
    public Material GetRenderBlackMaterial()
    {
        return blackMaterial;
    }

    public Material GetRenderResidentialBuildingMaterial()
    {
        return residentialBuildingMaterial;
    }
    
    public Material GetRenderNonResidentialBuildingMaterial()
    {
        return nonResidentialBuildingMaterial;
    }
    
    public Material GetRenderResidentialMaterial()
    {
        return residentialZoneMaterial;
    }
    
    public Material GetRenderIndustrialMaterial()
    {
        return industrialZoneMaterial;
    }
    
    public Material GetRenderRiverMaterial()
    {
        return riverMaterial;
    }
    
    public Material GetRenderVegetationMaterial()
    {
        return vegetationMaterial;
    }
    
    public Material GetRenderRoadMaterial()
    {
        return roadMaterial;
    }
    
    public Material GetRenderHighwayMaterial()
    {
        return highwayMaterial;
    }
    
    public Material GetRenderRailwayMaterial()
    {
        return railwayMaterial;
    }
    
    public Material GetRenderAlfalfaOrLucerneMaterial()
    {
        return alfalfaOrLucerneMaterial;
    }
    
    public Material GetRenderBarleyMaterial()
    {
        return barleyMaterial;
    }
    
    public Material GetRenderFallowAndBareSoilMaterial()
    {
        return fallowAndBareSoilMaterial;
    }
    
    public Material GetRenderOatsMaterial()
    {
        return oatsMaterial;
    }
    
    public Material GetRenderOtherGrainLeguminousMaterial()
    {
        return otherGrainLeguminousMaterial;
    }
    
    public Material GetRenderPeasMaterial()
    {
        return peasMaterial;
    }
    
    public Material GetRenderSunflowerMaterial()
    {
        return sunflowerMaterial;
    }
    
    public Material GetRenderVetchMaterial()
    {
        return vetchMaterial;
    }
    
    public Material GetRenderWheatMaterial()
    {
        return wheatMaterial;
    }

    public Dictionary<Renderer, Material[]> GetOriginalMaterials()
    {
        return originalMaterials;
    }

    public GameObject GetDowntownTemplates()
    {
        return downtownTemplates;
    }
    
    public GameObject GetResidentialTemplates()
    {
        return residentialTemplates;
    }
    
    public GameObject GetIndustrialTemplates()
    {
        return industrialTemplates;
    }
    
    public GameObject GetCropFieldsTemplates()
    {
        return fieldCropsTemplates;
    }

    public Month GetDefaultMonth()
    {
        return Month.April;
    }
    
    public Month GetMonth()
    {
        return month;
    }

    public CropFieldTexture GetCropFieldTexture()
    {
        return cropFieldTexture;
    }

    public void UpdateCropFieldTexture(CropFieldTexture cft)
    {
        cropFieldTexture = cft;
    }

    public int GetMainCameraRenderResolution()
    {
        return mainCameraRenderResolution;
    }

    public int GetMainCameraMetresPixelFactor()
    {
        return mainCameraMetresPixelFactor;
    }
    
    public int GetZoomCamerasRenderResolution()
    {
        return zoomCamerasRenderResolution;
    }
    
    public int GetZoomCamerasMetresPixelFactor()
    {
        return zoomCamerasMetresPixelFactor;
    }

    public Camera GetRenderMainCamera()
    {
        return mainCamera;
    }

    public Camera[] GetRenderZoomCameras()
    {
        return zoomCameras;
    }

    public bool GetCropFields()
    {
        return cropFields;
    }

    public bool GetRenderSar()
    {
        return renderSar;
    }
    
    public bool GetRenderB2B3B4()
    {
        return renderColor;
    }
    
    public bool GetRenderB8()
    {
        return renderB8;
    }
    
    public bool GetRenderB11()
    {
        return renderB11;
    }

    public bool GetRenderLayers()
    {
        return renderLayers;
    }
    
    public bool GetRenderSolarPanelsMask()
    {
        return renderSolarPanelsMask;
    }
    
    public bool GetRenderBuildingsMask()
    {
        return renderBuildingsMask;
    }

    public bool GetMakeGeojson()
    {
        return makeGeojson;
    }

    public float GetRotationAngle()
    {
        return rotationAngle;
    }

    public void UpdateCorutineIsWorking(bool isWorking)
    {
        corutineIsWorking = isWorking;
    }
}