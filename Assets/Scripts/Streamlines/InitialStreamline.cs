using System;
using System.Collections.Generic;
using DefaultNamespace;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VInspector;
[RequireComponent(typeof(GizmosPolygonViewer))]
[Serializable]
public class InitialStreamline : GizmosMonoBehaviour
{
    private List<Vector3> points;
    private List<Vector3> rightPoints;
    private List<Vector3> leftPoints;
    
    private List<Vector3> curvePoints;
    [SerializeField] private Transform parent;
    [SerializeField] private Streamline.StreamlineType type;
    
    [Header("Length unit: metre (m)")]
    [Range(5f, 50f)][SerializeField] private float width = 15f;
    [SerializeField] private const float kmScale = 0.001f;
    private bool usePolyline;
    
    //[Header("Catmull-Rom tension factor")]
    //[Range(0f, 10f)][SerializeField] private float tau = 0.5f;
    [Serializable]
    public class RiverParams
    {
        [SerializeField][Range(20f, 90f)] public float riverWidthPercentage = 21f;
        [SerializeField][Range(0f, 100f)] public float riverMaximumRandomDisplacementPercentage = 20f;
        [SerializeField][Range(0f, 100f)] public float greenZoneMaximumRandomDisplacementPercentage = 20f;
    }
    
    [ShowIf("type", Streamline.StreamlineType.River)][SerializeField] private RiverParams riverParameters;
    
    public void Initialize(Transform parent, List<Vector3> points, Streamline.StreamlineType type, float width)
    {
        this.points = points;
        this.parent = parent;
        this.type = type;
        this.width = width;
        leftPoints = new List<Vector3>();
        rightPoints = new List<Vector3>();
    }
    
    [Button("Recalculate curve")]
    public void RecalculatePoints()
    {
        var transformPoints = GeometricFunctions.getLinePositionsByTransform(transform, 2f, 0);

        //Calculamos la resolución de la línea de Bezier
        float totalDist = 0f;
        for (int i = 0; i < transformPoints.Count-1; i++)
            totalDist += Vector3.Distance(transformPoints[i], transformPoints[i + 1]);
        
        //Si queremos puntos cada 0.3f, por ejemplo, pues haremos
        int nPoints = (int)(totalDist / 0.3f);
        
        if (transformPoints != null && transformPoints.Count >= 2)
            curvePoints = CatmullRomCurve.RecalculateCatmullRomPoints(transformPoints, nPoints);
    }
    
    public Transform GetParent()
    {
        return parent;
    }
    
    public void SetParent(Transform parent)
    {
        this.parent = parent;
    }
    
    public List<Vector3> GetPoints()
    {
        return points;
    }
    
    public void SetRightPoints(List<Vector3> rightPoints)
    {
        this.rightPoints = rightPoints;
    }
    
    public List<Vector3> GetRightPoints()
    {
        return rightPoints;
    }

    public List<Vector3> GetCurvePoints()
    {
        return this.curvePoints;
    }

    public void SetLeftPoints(List<Vector3> leftPoints)
    {
        this.leftPoints = leftPoints;
    }
    
    public List<Vector3> GetLeftPoints()
    {
        return leftPoints;
    }
    
    public void SetPoints(List<Vector3> points)
    {
        this.points = points;
    }
    
    public new Streamline.StreamlineType GetType()
    {
        return type;
    }

    private void OnEnable()
    {
        RecalculatePoints();
    }

    public override void OnPointMoved()
    {
        RecalculatePoints();
    }

    public void SetType(Streamline.StreamlineType type)
    {
        this.type = type;
    }
    
    public float GetWidth()
    {
        return width * kmScale;
    }
    
    public void SetWidth(float width)
    {
        this.width = width;
    }

    public void SetRiverWidthPercentage(float percentage)
    {
        riverParameters.riverWidthPercentage = percentage;
    }
    
    public void SetRiverMaximumRandomDisplacementPercentage(float displacement)
    {
        riverParameters.riverMaximumRandomDisplacementPercentage = displacement;
    }
    
    public void SetGreenZoneMaximumRandomDisplacementPercentage(float greenZone)
    {
        riverParameters.greenZoneMaximumRandomDisplacementPercentage = greenZone;
    }
    
    public bool hasValidDisplacedLines()
    {
        if (leftPoints == null || leftPoints.Count == 0)
            return false;
        if (rightPoints == null || rightPoints.Count == 0)
            return false;

        return true;
    }
    
    public float GetRiverWidthPercentage()
    {
        return riverParameters.riverWidthPercentage / 100f;
    }
    
    public float GetRiverMaximumRandomnessPercentage()
    {
        return riverParameters.riverMaximumRandomDisplacementPercentage / 100f;
    }
    
    public float GetGreenzoneMaximumRandomnessPercentage()
    {
        return riverParameters.greenZoneMaximumRandomDisplacementPercentage / 100f;
    }

    public RiverParams getRiverParameters()
    {
        return riverParameters;
    }

    private void OnDrawGizmos()
    {
        DrawCurve();
    }

    public bool GetUsePolyline()
    {
        return usePolyline;
    }

    public void SetUsePolyline(bool use)
    {
        usePolyline = use;
    }
    
    void DrawCurve()
    {
 
        if (curvePoints == null || curvePoints.Count < 2)
        {
            return;
        }

        Color colorTodraw = new Color(1, 1, 1, 1f);
        if (!OtherFunctions.CheckIfChildrenSelected(gameObject))
            colorTodraw.a = 0.2f;

        Gizmos.color = colorTodraw;
        Vector3 previousPoint = new Vector3(curvePoints[0].x, curvePoints[0].y, curvePoints[0].z);
        for (int i = 1; i < curvePoints.Count; i++)
        {
            Vector3 currentPoint = new Vector3(curvePoints[i].x, curvePoints[i].y, curvePoints[i].z);
            Gizmos.DrawLine(previousPoint, currentPoint);
            Gizmos.DrawSphere(previousPoint, 0.005f);
            previousPoint = currentPoint;
        }
    }
}
