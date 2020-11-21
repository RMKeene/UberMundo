using System;
using System.Collections.Generic;
using System.Text;

namespace UberMundo
{
    public struct Rotation
    {
        public float Roll;
        public float Pitch;
        public float Yaw;

        public Rotation(float roll, float pitch, float yaw)
        {
            Roll = roll;
            Pitch = pitch;
            Yaw = yaw;
        }
        public Rotation(float pitch, float yaw)
        {
            Roll = 0.0f;
            Pitch = pitch;
            Yaw = yaw;
        }
        public Rotation(float yaw)
        {
            Roll = 0.0f;
            Pitch = 0.0f;
            Yaw = yaw;
        }
    }

}
