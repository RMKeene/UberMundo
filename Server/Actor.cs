using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace UberMundo
{
    public class Actor
    {
        private static bool debugMessages = true;
        public Guid uid;
        public Location Location;
        public Rotation Rotation;

        public Actor()
        {
            if (debugMessages) Console.WriteLine($"Actor: Create");
        }
    }
}
