using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.Regions
{
    public class StreamlineRegion: Region
    {
        public Streamline.StreamlineType streamlineType;
        public List<Vector3> leftStreamline;
        public List<Vector3> rightStreamline;

        public StreamlineRegion()
        {
            streamlineType = Streamline.StreamlineType.Street;
            leftStreamline = new List<Vector3>();
            rightStreamline = new List<Vector3>();
        }
        public StreamlineRegion(Streamline.StreamlineType streamlineType, List<Vector3> leftStreamline, List<Vector3> rightStreamline)
        {
            this.streamlineType = streamlineType;
            this.leftStreamline = leftStreamline;
            this.rightStreamline = rightStreamline;
        }
        public void setLeftStreamline(List<Vector3> list)
        {
            leftStreamline = list;
        }
        
        public void setRightStreamline(List<Vector3> list)
        {
            rightStreamline = list;
        }
        
        public List<Vector3> getLeftStreamline()
        {
            return leftStreamline;
        }
         
        public List<Vector3> getRightStreamline()
        {
            return rightStreamline;
        }

        public void setStreamlineType(Streamline.StreamlineType type)
        {
            streamlineType = type;
        }

        public Streamline.StreamlineType getStreamlineType()
        {
            return streamlineType;
        }
    }
}