using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DefaultNamespace.Editor
{
    [CustomEditor(typeof(Template))]
    public class TemplateEditor: UnityEditor.Editor
    {
        private Template template;
        private bool selectingMode;
        private void OnEnable()
        {
            template = target as Template;

            //Hide the main GOs move/rot/scale handle
            Tools.hidden = true;
            selectingMode = false;
        }

        private void OnDisable()
        {
            //Un-hide the main GOs move/ rot / scale handle
            Tools.hidden = false;
            selectingMode = true;
        }

        private void OnSceneGUI()
        {
            //So you we cant click on anything else in the scene
            HandleUtility.AddDefaultControl(0);

            //Move the constrains points
            List<Point> points = template.points;

            if (points != null)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    if (!selectingMode)
                    {
                        Vector3 newPos = MovePoint(points[i].GetPosition());
                        points[i].SetPosition(newPos);
                    }
                    Handles.Label(points[i].GetPosition(), i.ToString());
                    Handles.DrawWireCube(points[i].GetPosition(), new Vector3(template.clickRange/3f,template.clickRange/3f,template.clickRange/3f));
                }
            }

            if (template.buildings != null)
            {
                for (var i = 0; i < template.buildings.Count; i++)
                {
                    var building = template.buildings[i];
                    List<Vector3> buildingPoints = new List<Vector3>();
                    foreach (var pointIndex in building.GetIndicesList())
                    {
                        buildingPoints.Add(points[pointIndex].GetPosition());
                    }
                    Handles.Label(GeometricFunctions.Centroid(buildingPoints), i.ToString());
                }
            }

            if(selectingMode)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                Handles.color = Color.white;
                // Asegurarse de que la dirección de la línea esté normalizada
                var lineDirection = ray.direction;
                lineDirection.Normalize();

                Event e = Event.current;
                if (points != null)
                {
                    for (var i = 0; i < points.Count; i++)
                    {
                        var point = points[i];
                        // Vector desde el punto en la línea hasta el punto a verificar
                        Vector3 pointToLinePoint = point.GetPosition() - ray.origin;

                        // Proyección del vector puntoToLinePoint sobre la dirección de la línea
                        Vector3 projection = Vector3.Project(pointToLinePoint, lineDirection);

                        // Vector desde el punto hasta la línea
                        Vector3 closestPointOnLine = ray.origin + projection;
                        Vector3 pointToClosestPoint = point.GetPosition() - closestPointOnLine;

                        // Distancia desde el punto hasta la línea
                        float distance = pointToClosestPoint.magnitude;
                        
                        if (distance <= template.clickRange)
                        {
                            if (e.type == EventType.MouseDown && e.button == 0)
                            {
                                template.addTemporalSelectedPoint(i);
                            }
                            Handles.DrawWireDisc(point.GetPosition(), Vector3.up, template.clickRange);
                        }
                    }
                }
                

                if (template.getTemporalSelectedPoints() != null)
                {
                    var ints = template.getTemporalSelectedPoints();
                    for (var index = 0; index < ints.Count; index++)
                    {
                        var selectedPointIndex = ints[index];
                        var selectedPointIndexPlusOne = ints[(index+1)%ints.Count];
                        Handles.color = Color.cyan;
                        Handles.DrawWireDisc(points[selectedPointIndex].GetPosition(), Vector3.up, template.clickRange);
                        Handles.DrawLine(points[selectedPointIndex].GetPosition(), points[selectedPointIndexPlusOne].GetPosition());
                    }
                }
                
            }
        }

        private Vector3 MovePoint(Vector3 pos)
        {
            if (Tools.current == Tool.Move)
            {
                //Check if we have moved the point
                EditorGUI.BeginChangeCheck();

                //Get the new position and display it
                pos = Handles.PositionHandle(pos, Quaternion.identity);

                if (EditorGUI.EndChangeCheck())
                {
                    //Save the new value
                    EditorUtility.SetDirty(target);
                }
            }

            return pos;
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();

            //Update when changing value in inspector
            if (base.DrawDefaultInspector())
            {
                //triangulatePoints.GenerateTriangulation();
                //EditorUtility.SetDirty(target);
            }
            
            if (GUILayout.Button("Enter/Exit select mode"))
            {
                Tools.hidden = !Tools.hidden;
                
                if(Tools.hidden)
                    selectingMode = true;
                else
                    selectingMode = false;
                
                EditorUtility.SetDirty(target);
            }
            
            if (GUILayout.Button("Generate boundary vertex points"))
            {
                template.ReassignBoundaryPoints(new Region());
                EditorUtility.SetDirty(target);
            }
            
            if (GUILayout.Button("Reset selected points"))
            {
                template.resetTemporalList();
                EditorUtility.SetDirty(target);
            }
        }
    }
}