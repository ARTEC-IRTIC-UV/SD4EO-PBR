using System;
using System.ComponentModel.Design.Serialization;
using UnityEngine;

namespace DefaultNamespace
{
    public class Cross
    { 
        private Vector3 v1;
        private Vector3 v2;
        private bool[] enabled;
        public Cross(Vector3 v1, Vector3 v2)
        {
            this.v1 = v1;
            this.v2 = v2;
            enabled = new []{false, false};
        }

        public void setCrossed(int i, bool b)
        {
            enabled[i - 1] = b;
        }
        
        public bool getCrossed(int i)
        {
            return enabled[i - 1];
        }

        public Vector3 getV1()
        {
            return v1;
        }

        public Vector3 getV2()
        {
            return v2;
        }
        
        public Vector3 getMostSimilar(Vector3 referenceVector)
        {
            // Deducción de D y E a partir de B y C
            Vector3 v3 = -v1; // Invertir el sentido de B
            Vector3 v4 = -v2; // Invertir el sentido de C

            // Calcular los ángulos entre el vector A y los vectores B, C, D, E
            float anglev1 = Math.Abs(Vector3.Angle(referenceVector, v1));
            float anglev2 = Math.Abs(Vector3.Angle(referenceVector, v2));
            float anglev3 = Math.Abs(Vector3.Angle(referenceVector, v3));
            float anglev4 = Math.Abs(Vector3.Angle(referenceVector, v4));

            // Encontrar el ángulo mínimo entre B, C, D, E
            float minAngle = Mathf.Min(anglev1, anglev2, anglev3, anglev4);
            
            // Devolver el vector correspondiente al ángulo mínimo
            if (minAngle == anglev1)
            {
                return v1.normalized;
            }
            else if (minAngle == anglev2)
            {
                return v2.normalized;
            }
            else if (minAngle == anglev3)
            {
                return v3.normalized;
            }
            else
            {
                return v4.normalized;
            }
        }
    }
    
    
}