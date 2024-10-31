using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public class Line
    {
        private List<List<Vector3>> lineSegments;
        private float width;

        public Line()
        {
            lineSegments = new List<List<Vector3>>();
        }
        
        
        public void AddLineSegment(List<Vector3> segment)
        {
            lineSegments.Add(segment);
        }
        
        public List<List<Vector3>> GetLineSegments()
        {
            return lineSegments;
        }
        
        public List<Vector3> GetAllLinePoints()
        {
            List<Vector3> linePoints = new List<Vector3>();
            foreach (var seg in lineSegments)
            {
                foreach (var p in seg)
                {
                    linePoints.Add(p);
                }
            }

            return linePoints;
        }

        public float GetWidth()
        {
            return width;
        }

        public void SetWidth(float width)
        {
            this.width = width;
        }
    }
}