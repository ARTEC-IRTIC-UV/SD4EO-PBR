using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TemplateTest : MonoBehaviour
{
    // Por ahora estan en publicos para comprobar que este bien
    public List<Vector3> puntos;
    public int[] triangulos;
    public List<GameObject> quads;
    public float area;
    
    /*void OnValidate()
    {
        puntos = new List<Vector3>();
        triangulos = new int[0];
        quads = new List<GameObject>();

        MeshFilter[] allMeshFilters = GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter meshFilter in allMeshFilters)
        {
            if (meshFilter != null)
            {
                if (meshFilter.sharedMesh.vertexCount == 4)
                    quads.Add(meshFilter.gameObject);
            }
        }
        
        foreach (GameObject quad in quads)
        {
            Vector3[] vertices = quad.GetComponent<MeshFilter>().sharedMesh.vertices;
            triangulos = quad.GetComponent<MeshFilter>().sharedMesh.triangles;

            for (int i = 0; i < vertices.Length; i++)
                puntos.Add(quad.transform.TransformPoint(vertices[i]));
        }
        area = CalculateSurfaceArea();  
    }*/

    // codigo de un foro de stackoverflow, no lo he probado
    public bool TemplateCoincide(List<Vector3> puntos2, List<Vector3> triangulos2, List<Vector3> aristas2)
    {
        bool res = true;
        Vector3 u = Vector3.Cross(puntos[0] - puntos[3], puntos[0] - puntos[4]);
        Vector3 v = Vector3.Cross(puntos[0] - puntos[1], puntos[0] - puntos[4]);
        Vector3 w = Vector3.Cross(puntos[0] - puntos[1], puntos[0] - puntos[3]);
        
        foreach (Vector3 punto in puntos2)
        {
            if (Vector3.Dot(u, punto - puntos[0]) < 0 || Vector3.Dot(v, punto - puntos[0]) < 0 || Vector3.Dot(w, punto - puntos[0]) < 0)
                res = false;
        }

        Vector3 i = puntos[1] - puntos[0];
        Vector3 j = puntos[3] - puntos[0];
        Vector3 k = puntos[4] - puntos[0];
        Vector3 vv = puntos[5] - puntos[0];

        float vi = Vector3.Dot(i, vv);
        float vj = Vector3.Dot(j, vv);
        float vk = Vector3.Dot(k, vv);
        
        if(vi > 0 && vi < Vector3.Dot(i,i))
            if(vj > 0 && vj < Vector3.Dot(j, j))
                if (vk > 0 && vk < Vector3.Dot(k, k))
                    res = false;

        return res;
    }
    
    public string GetName()
    {
        return gameObject.name;
    }
    
    public float CalculateSurfaceArea() {
        double sum = 0.0;
        foreach (GameObject quad in quads)
        {
            for (int i = 0; i < triangulos.Length; i += 3)
            {
                Vector3 corner = puntos[triangulos[i]];
                Vector3 a = puntos[triangulos[i + 1]] - corner;
                Vector3 b = puntos[triangulos[i + 2]] - corner;

                sum += Vector3.Cross(a, b).magnitude;
            }
        }
        Debug.Log("saliendo de calculatesurfc");
        return (float)(sum/2.0);
    }

    public float CalculateScore(Region r)
    {
        if (area > r.GetAreaKM()) // si el area del tempalte supera al de la region devuelve 0 que seria el peor caso
            return 0;
        else
            return area; // si no devuelve el area del template, cuanto mas mejor 
    }
}