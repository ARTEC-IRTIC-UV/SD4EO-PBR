using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[ExecuteInEditMode]

public class BezierCurve : MonoBehaviour
{
    // Función para calcular un punto en la curva de Bezier
    public static Vector3 CalculateBezierPoint(float t, List<Vector3> controlPoints)
    {
        // Aplicar la fórmula de Bezier
        int n = controlPoints.Count - 1;
        Vector3 point = Vector3.zero;

        for (int i = 0; i <= n; i++)
        {
            float basis = BinomialCoefficient(n, i) * Mathf.Pow(1 - t, n - i) * Mathf.Pow(t, i);
            point += controlPoints[i] * (basis);
        }

        
        return point;
    }

    // Función para calcular el coeficiente binomial
    public static int BinomialCoefficient(int n, int k)
    {
        if (k < 0 || k > n)
            return 0; // Manejar casos fuera de los límites

        // Calcular n! / (k! * (n - k)!)
        int numerator = Factorial(n);
        int denominator = Factorial(k) * Factorial(n - k);
        return numerator / denominator;
    }

    // Función para calcular el factorial de un número
    public static int Factorial(int number)
    {
        if (number <= 1)
            return 1;

        int result = 1;
        for (int i = 2; i <= number; i++)
        {
            result *= i;
        }

        return result;
    }

    public static List<Vector3> RecalculateBezierPoints(List<Vector3> controlPoints, int resolution)
    {
        List<Vector3> bezierPoints = new List<Vector3>();

        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            Vector3 point = CalculateBezierPoint(t, controlPoints);
            bezierPoints.Add(point);
        }

        return bezierPoints;
    }
}

