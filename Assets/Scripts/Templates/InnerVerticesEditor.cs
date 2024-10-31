using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VRuler.Libs;

[ExecuteInEditMode]
public class InnerVerticesEditor : MonoBehaviour
{
    /*
    public Template template;
    private List<Vector3> shapePoints;
    private Ray ray;
    public Vector3 closestPoint;
    public Vector3 intersectionPoint;
    public bool creating;
    private Vector3 savedPoint;
    private Coroutine c;
    
    private void OnEnable()
    {
        creating = false;
        SceneView.duringSceneGui += MySceneGUI;
        getTemplatePoints();
    }
    
    private void getTemplatePoints()
    {
        List<Point> points = template.points;
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i].isCorner())
            {
                shapePoints.Add(points[i].GetPosition());
            }
        }
    }
    
    public void MySceneGUI(SceneView sceneView){
        Vector3 mousePosition = Event.current.mousePosition;
        ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        
        // Calcular el punto de intersección
        float t = -ray.origin.y / ray.direction.y;
        intersectionPoint = ray.origin + t * ray.direction;
        closestPoint = TestAlgorithmsHelpMethods.FindClosestPointOnEdges(intersectionPoint, shapePoints);
        
        if (Event.current.mouseDown())
        {
            creating = !creating;
            if (creating)
            {
                savedPoint = closestPoint;
                template.points.Add(new Point(PointType.BoundaryVertex, closestPoint));
            }

            if (!creating)
            {
                savedPoint = new Vector3();
                template.points.Add(new Point(PointType.BoundaryVertex, closestPoint));
            }
            
        }
        
        sceneView.Repaint();
    }


    private void OnDrawGizmos()
    {
        if (shapePoints == null)
            return;

        // Calcular el punto de intersección
        float t = -ray.origin.y / ray.direction.y;
        intersectionPoint = ray.origin + t * ray.direction;
        closestPoint = TestAlgorithmsHelpMethods.FindClosestPointOnEdges(intersectionPoint, shapePoints);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(intersectionPoint, 0.05f);

        //DIBUJADO DEL PUNTO MÁS CERCANO
        Vector3 sphereCenter = closestPoint;
        float sphereRadius = 0.1f;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(sphereCenter, sphereRadius);
        if (creating)
            Gizmos.DrawLine(savedPoint, intersectionPoint);
    }
    */
}
