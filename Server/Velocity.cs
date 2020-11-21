using System;
using System.Collections.Generic;
using System.Text;

namespace UberMundo
{
    public struct Velocity
    {
        public float VX;
        public float VY;
        public float VZ;

        public Velocity(float vX, float vY, float vZ)
        {
            VX = vX;
            VY = vY;
            VZ = vZ;
        }
    }
}
