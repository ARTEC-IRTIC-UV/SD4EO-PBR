using System;
using System.Collections.Generic;
using Habrador_Computational_Geometry;
using Unity.VisualScripting;
using UnityEngine;

namespace DefaultNamespace
{
    public class LineRendererController: MonoBehaviour
    {
        [SerializeField] LineRenderer lineRenderer;

        public void showLine()
        {
            // Obtén una referencia al LineRenderer adjunto a este GameObject
            if(!lineRenderer)
                lineRenderer = gameObject.GetComponent<LineRenderer>();
            if (!lineRenderer)
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 0;
            // Comprueba si hay al menos un hijo en el GameObject
            if (transform.childCount > 0)
            {
                // Recorre cada hijo y añade un punto en la posición de cada hijo
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform hijo = transform.GetChild(i);
                    if (hijo != transform)
                    {
                        lineRenderer.positionCount++;
                        lineRenderer.SetPosition(i, hijo.position);
                    }
                }
            }
            else
            {
                Debug.LogError("No hay hijos en el GameObject");
            }
        }
    }
}