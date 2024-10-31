using System;
using System.Collections.Generic;
using Habrador_Computational_Geometry;
using UnityEngine;

[Serializable]
public class Zone : MonoBehaviour
{
    private List<Vector2> zonePoints;
    private List<Material> zoneMaterials;
    [Header("Zone information")]
    [SerializeField] private Transform parent;
    [SerializeField] private ZoneType type;
    [SerializeField] private ZoneShape shape;
    [SerializeField] [Range(10f, 10000f)] private float shapeSide = 500f;
    [SerializeField] [Range(0f, 180f)] private float shapeAngle = 0f;
    [Header("Length unit: metre (m)")]
    [SerializeField] [Range(5f, 40f)] private float roadWidth = 0.01f;
    [SerializeField] [Range(30f, 2000f)] private float maximumPlotSide = 50f;
    [Header("Without unit")]
    [SerializeField] [Range(0.001f, 10f)] private float zoneWeight = 1f;
    [SerializeField] [Range(0.0f, 1f)] private float voronoiRandomFreedom = 0.1f;
    [SerializeField] private float kmScale = 0.001f;
    [SerializeField] [HideInInspector] private int voronoiSubdivisionLevel = 1;
    public bool randomize;

    public void Initialize(Transform parent, ZoneType type, ZoneShape shape, float shapeSide, float shapeAngle, float roadWidth, float maximumPlotSide, float zoneWeight, int voronoiSubdivisionLevel, float voronoiRandomFreedom, List<Material> zoneMaterials)
    {
        this.parent = parent;
        this.type = type;
        this.shape = shape;
        this.shapeSide = shapeSide;
        this.shapeAngle = shapeAngle;
        this.roadWidth = roadWidth;
        this.maximumPlotSide = maximumPlotSide;
        this.zoneWeight = zoneWeight;
        this.voronoiSubdivisionLevel = voronoiSubdivisionLevel;
        this.voronoiRandomFreedom = voronoiRandomFreedom;
        this.zoneMaterials = zoneMaterials;
    }

    public List<Vector2> GetPoints()
    {
        return zonePoints;
    }

    public void SetPoints()
    {
        Vector2 center = parent.position.XZ();
        List<Vector2> points = new List<Vector2>();

        switch (shape)
        {
            case ZoneShape.Square:
                Vector2 s1 = GeometricFunctions.RotatePoint(new Vector2(center.x - GetShapeSide()/2f, center.y - GetShapeSide()/2f), center, GetShapeAngle());
                Vector2 s2 = GeometricFunctions.RotatePoint(new Vector2(center.x + GetShapeSide()/2f, center.y - GetShapeSide()/2f), center, GetShapeAngle());
                Vector2 s3 = GeometricFunctions.RotatePoint(new Vector2(center.x + GetShapeSide()/2f, center.y + GetShapeSide()/2f), center, GetShapeAngle());
                Vector2 s4 = GeometricFunctions.RotatePoint(new Vector2(center.x - GetShapeSide()/2f, center.y + GetShapeSide()/2f), center, GetShapeAngle());
                points.Add(s1);
                points.Add(s2);
                points.Add(s3);
                points.Add(s4);
                points.Add(s1);
                break;
            
            case ZoneShape.Triangle:
                Vector2 t1 = GeometricFunctions.RotatePoint(new Vector2(center.x, center.y + GetShapeSide() / Mathf.Sqrt(3)), center, GetShapeAngle());
                Vector2 t2 = GeometricFunctions.RotatePoint(new Vector2(center.x - GetShapeSide() / 2, center.y - GetShapeSide() / (2 * Mathf.Sqrt(3))), center, GetShapeAngle());
                Vector2 t3 = GeometricFunctions.RotatePoint(new Vector2(center.x + GetShapeSide() / 2, center.y - GetShapeSide() / (2 * Mathf.Sqrt(3))), center, GetShapeAngle());
                points.Add(t1);
                points.Add(t2);
                points.Add(t3);
                points.Add(t1);
                break;
        }

        zonePoints = points;
    }

    public Transform GetParent()
    {
        return parent;
    }

    public void SetParent(Transform parent)
    {
        this.parent = parent;
    }
    
    public ZoneType GetZoneType()
    {
        return type;
    }

    public void SetZoneType(ZoneType type)
    {
        this.type = type;
    }
    
    public ZoneShape GetZoneShape()
    {
        return shape;
    }

    public void SetZoneShape(ZoneShape shape)
    {
        this.shape = shape;
    }
    
    public float GetShapeSide()
    {
        return shapeSide * kmScale;
    }

    public void SetShapeSide(float shapeSide)
    {
        this.shapeSide = shapeSide;
    }
    
    public float GetShapeAngle()
    {
        return shapeAngle;
    }

    public void SetShapeAngle(float shapeAngle)
    {
        this.shapeAngle = shapeAngle;
    }
    
    public float GetRoadWidth()
    {
        return roadWidth * kmScale;
    }

    public void SetRoadWidth(float roadWidth)
    {
        this.roadWidth = roadWidth;
    }

    public float GetMaximumPlotSide()
    {
        return maximumPlotSide * kmScale;
    }

    public void SetMaximumPlotSide(float maximumPlotSide)
    {
        this.maximumPlotSide = maximumPlotSide;
    }

    public float GetMaximumSubdivisionArea()
    {
        return maximumPlotSide * maximumPlotSide * kmScale * kmScale;
    }
    
    public float GetZoneWeight()
    {
        return zoneWeight;
    }

    public void SetZoneWeight(float zoneWeight)
    {
        this.zoneWeight = zoneWeight;
    }
    
    public int GetVoronoiSubdivisionLevel()
    {
        return voronoiSubdivisionLevel;
    }

    public void SetVoronoiSubdivisionLevel(int voronoiSubdivisionLevel)
    {
        this.voronoiSubdivisionLevel = voronoiSubdivisionLevel;
    }
    
    public float GetVoronoiRandomFreedom()
    {
        return voronoiRandomFreedom;
    }

    public void SetVoronoiRandomFreedom(float voronoiRandomFreedom)
    {
        this.voronoiRandomFreedom = voronoiRandomFreedom;
    }

    public List<Material> GetZoneMaterials()
    {
        return zoneMaterials;
    }

    public void SetZoneMaterials(List<Material> zoneMaterials)
    {
        this.zoneMaterials = zoneMaterials;
    }
}