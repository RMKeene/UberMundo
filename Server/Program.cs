using System;

namespace UberMundo
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Ubermundo Server 0.0.1");
            UberMundoTCPListener s = new UberMundoTCPListener(args);
            s.Run();
        }
    }
}
