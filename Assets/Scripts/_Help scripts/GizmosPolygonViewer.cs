using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GizmosPolygonViewer : MonoBehaviour
{
    public bool closeShape = false;
    public bool drawLines = false;
    public UnityEvent onPointMoved = new UnityEvent();
    
    void OnDrawGizmos()
    {
        if(!OtherFunctions.CheckIfChildrenSelected(gameObject))
            return;
        // Recorre cada hijo del GameObject
        for (int i = 0; i < transform.childCount; i++)
        {
            Color color = new Color(1, 1, 1, 1f);
            Transform hijo = transform.GetChild(i);
            Transform hijo2 = transform.GetChild((i+1)%transform.childCount);
            Gizmos.color = color;
            Gizmos.DrawSphere(hijo.position, 0.04f);
            if (drawLines)
            {
                if (!closeShape && i == transform.childCount - 1)
                    continue;
                Gizmos.DrawLine(hijo.position, hijo2.position);
            }
               
        }
    }
}



[CustomEditor(typeof(GizmosPolygonViewer))]
public class GizmosPolygonViewerEditor : Editor
{
    private GizmosMonoBehaviour gizmosMonoBehaviour;
    void OnSceneGUI()
    {
        GizmosPolygonViewer obj = (GizmosPolygonViewer)target;

        for (int i = 0; i < obj.transform.childCount; i++)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newPoint = Handles.DoPositionHandle(obj.transform.GetChild(i).position, Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                if(gizmosMonoBehaviour == null)
                    gizmosMonoBehaviour = obj.GetComponent<GizmosMonoBehaviour>();
                if(gizmosMonoBehaviour)
                    gizmosMonoBehaviour.OnPointMoved();
                Undo.RecordObject(obj, "Move Control Point");
                obj.transform.GetChild(i).position = newPoint;
                obj.onPointMoved.Invoke();
            }
            
        }
    }
}