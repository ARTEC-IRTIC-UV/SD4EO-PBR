using System.Numerics;
using Unity.VisualScripting;
using Vector3 = UnityEngine.Vector3;

namespace DefaultNamespace
{
    public class RandomPoint
    {
        private Vector3 position;
        private Vector3 closestPoint;
        private Cross cross;
        private bool isBorder;

        public RandomPoint(Vector3 position)
        {
            this.position = position;
            closestPoint = new Vector3();
            cross = null;
            isBorder = false;
        }
        
        public Vector3 getPosition()
        {
            return this.position;
        }
        
        public Vector3 getClosestPoint()
        {
            return this.closestPoint;
        }
        
        public Cross getCross()
        {
            return this.cross;
        }
        
        public void setPosition(Vector3 pos)
        {
            this.position = pos;
        }
        
        public void setClosestPoint(Vector3 pos)
        {
            this.closestPoint = pos;
        }
        
        public void setCross(Cross c)
        {
            this.cross = c;
        }

        public void setIsBorder(bool b)
        {
            isBorder = b;
        }

        public bool getIsBorder()
        {
            return isBorder;
        }
    }
}