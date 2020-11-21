using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace UberMundo
{
    public class Thing : Actor
    {
        private static bool debugMessages = true;
        public Thing()
        {
            if (debugMessages) Console.WriteLine($"Thing: Create");
        }
    }
}
