using UnityEngine;
using VRuler.Libs;

namespace Habrador_Computational_Geometry
{
    public class InterpolationRK4 : MonoBehaviour
    {
        // Definir la estructura para un punto en 2D con velocidad
        public class MovingPoint
        {
            public float x;
            public float y;
            public float velocityX;
            public float velocityY;

            public MovingPoint(float x, float y, float velocityX, float velocityY)
            {
                this.x = x;
                this.y = y;
                this.velocityX = velocityX;
                this.velocityY = velocityY;
            }

            public MovingPoint()
            {
                this.x = 0;
                this.y = 0;
                this.velocityX = 0;
                this.velocityY = 0;
            }
        }

        // Definir la estructura para la fuerza aplicada al punto
        public struct Force
        {
            public float x;
            public float y;

            public Force(float x, float y)
            {
                this.x = x;
                this.y = y;
            }
        }

        // Método RK4 para interpolación suave entre dos puntos de control con fuerza aplicada
        public static MovingPoint RungeKutta4(MovingPoint point, Force force, float h)
        {
            float k1x, k2x, k3x, k4x;
            float k1y, k2y, k3y, k4y;

            MovingPoint result = new MovingPoint();
            result.velocityX = point.velocityX + force.x*h;
            result.velocityY = point.velocityY + force.y*h;
            
            Vector2 speed = new Vector2(result.velocityX, result.velocityY);
            speed.Normalize();

            result.velocityX = speed.x;
            result.velocityY = speed.y;
            //DefaultNamespace.ArrowDrawer.DrawArrow(new Vector3(point.x, 0, point.y), new Vector3(point.x + result.velocityX*0.3f, 0, point.y + result.velocityY*0.3f), Color.red,0.05f);
            
            // Paso 1
            k1x = h * result.velocityX;
            k1y = h * result.velocityY;

            // Paso 2
            k2x = h * (result.velocityX + k1x / 2);
            k2y = h * (result.velocityY + k1y / 2);

            // Paso 3
            k3x = h * (result.velocityX + k2x / 2);
            k3y = h * (result.velocityY + k2y / 2);

            // Paso 4
            k4x = h * (result.velocityX + k3x);
            k4y = h * (result.velocityY + k3y);

            // Calcular el siguiente punto interpolado con fuerza aplicada
            result.x = point.x + (k1x + 2 * k2x + 2 * k3x + k4x) / 6;
            result.y = point.y + (k1y + 2 * k2y + 2 * k3y + k4y) / 6;

            speed = new Vector2(result.velocityX, result.velocityY);
            speed.Normalize();

            result.velocityX = speed.x;
            result.velocityY = speed.y;
            return result;
        }
        
        public static double RungeKutta4Length(double speed, double dt)
        {
            double k1 = dt * speed;
            double k2 = dt * (speed + 0.5 * k1);
            double k3 = dt * (speed + 0.5 * k2);
            double k4 = dt * (speed + k3);

            // Update position using the weighted average of the four increments
            double positionIncrement = (k1 + 2 * k2 + 2 * k3 + k4) / 6.0;

            return positionIncrement;
        }
    }
}
