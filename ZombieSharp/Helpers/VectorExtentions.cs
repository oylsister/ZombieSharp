using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZombieSharp.Helpers
{
    public static class VectorExtentions
    {
        public static Vector NormalizeVector(this Vector vector)
        {
            var x = vector.X;
            var y = vector.Y;
            var z = vector.Z;

            var magnitude = MathF.Sqrt(x * x + y * y + z * z);
            
            if(magnitude != 0.0)
            {
                x /= magnitude;
                y /= magnitude;
                z /= magnitude;
            }

            return new Vector(x, y, z);
        }
    }
}
