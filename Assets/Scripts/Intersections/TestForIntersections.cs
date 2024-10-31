using System.Collections;
using System.Collections.Generic;
using Habrador_Computational_Geometry;
using UnityEngine;
using VInspector;

public class TestForIntersections : MonoBehaviour
{
    public List<Transform> objetos;

    public Transform p1;
    public Transform p2;
    // Start is called before the first frame update
    [Button("Test")]
    public void Test()
    {
        List<Vector3> puntos = new List<Vector3>();
        foreach (var o in objetos)
        {
            puntos.Add(o.position);
        }

        Vector2 col = TestAlgorithmsHelpMethods.CalculateIntersectionBetweenPolygonAndInfiniteLine(p1.position.XZ(),
            p2.position.XZ(), GeometricFunctions.ConvertListVector3ToListVector2XZ(puntos));

        Debug.DrawLine(col.XYZ(), col.XYZ() + Vector3.up, Color.cyan, 10f);
    }
    
}
