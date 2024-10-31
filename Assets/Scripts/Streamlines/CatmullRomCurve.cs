using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[ExecuteInEditMode]
public class CatmullRomCurve
{
    // Función para calcular un punto en la curva de Catmull-Rom
    public static Vector3 CalculateCatmullRomPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // Parámetros para Catmull-Rom
        float t2 = t * t;
        float t3 = t2 * t;

        
        // Fórmula de Catmull-Rom
        Vector3 point = 0.5f *(
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );

        return point;
    }

    // Función para recalcular los puntos de la curva de Catmull-Rom
    public static List<Vector3> RecalculateCatmullRomPoints(List<Vector3> controlPoints, int resolution)
    {
        List<Vector3> catmullRomPoints = new List<Vector3>();

        if (controlPoints.Count < 2)
        {
            return catmullRomPoints; // No se pueden generar puntos si hay menos de 2 puntos de control
        }

        // Agregar el primer punto
        catmullRomPoints.Add(controlPoints[0]);

        // Añadir puntos duplicados en los extremos para que la curva pase por los primeros y últimos puntos de control
        Vector3 p0, p1, p2, p3;
        for (int i = 0; i < controlPoints.Count - 1; i++)
        {
            p0 = i == 0 ? controlPoints[0] : controlPoints[i - 1];
            p1 = controlPoints[i];
            p2 = controlPoints[i + 1];
            p3 = (i + 2 < controlPoints.Count) ? controlPoints[i + 2] : controlPoints[controlPoints.Count - 1];

            for (int j = 1; j <= resolution; j++)
            {
                float t = j / (float)resolution;
                Vector3 point = CalculateCatmullRomPoint(t, p0, p1, p2, p3);
                catmullRomPoints.Add(point);
            }
        }

        // Agregar el último punto
        catmullRomPoints.Add(controlPoints[controlPoints.Count - 1]);

        return catmullRomPoints;
    }
}
