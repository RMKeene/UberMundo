using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UberMundo
{
    public class UberMundoTCPListener
    {
        public Int32 port;
        public IPAddress localAddr;

        private Timer reapTickTimer;

        public List<UberMundoConnectionThread> allPlayerThreads = new List<UberMundoConnectionThread>();

        public string[] Args { get; private set; }

        public UberMundoTCPListener(string[] args)
        {
            string listenIP = ConfigurationManager.AppSettings["ListenIP"] ?? "10.0.0.220";
            string listenPort = ConfigurationManager.AppSettings["ListenPort"] ?? "13000";
            if (listenIP.Equals("any", StringComparison.InvariantCultureIgnoreCase))
            {
                localAddr = IPAddress.Any;
            }
            else
            {
                localAddr = IPAddress.Parse(listenIP);
            }
            port = Int32.Parse(listenPort);
            if (localAddr == IPAddress.Any)
            {
                Console.WriteLine($"Share Server : listen IP Any (meaning all IPs of this machine), port {port}");
            }
            else
            {
                Console.WriteLine($"Share Server : listen IP {localAddr}, port {port}");
            }
            var host = Dns.GetHostEntry(Dns.GetHostName());
            Console.WriteLine($"Host Name: {host}");
            Console.WriteLine("Available IP Address on this machine.");
            Console.WriteLine("  127.0.0.1 (localhost for local machine only debug or private game.)");
            foreach (var ip in GetAllLocalIPv4())
            {
                Console.WriteLine($"  {ip}");
            }

            this.Args = args;

            // Get these into memory early before any other threads are started.
            var sdb = StorageDatabase.Instance;
            var udb = UserDatabase.Instance;
        }

#nullable enable
        private void ReapTickTimerCallback(object? state)
        {
            //UberMundoTCPListener sv = state as UberMundoTCPListener;
            UserDatabase.Instance.ReapStalePlayerInfo();
        }
#nullable disable

        public void Run()
        {
            reapTickTimer = new Timer(ReapTickTimerCallback, this, 1000, 1000);

            TcpListener server = null;
            try
            {
                server = new TcpListener(localAddr, port);
                server.Start();

                while (true)
                {
                    ProcessNextConnection(server);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                server.Stop();
            }

            Console.WriteLine("\nHit enter to continue...");
            Console.Read();
        }

        public void SendSystemwideMessage(string mess)
        {
            lock (allPlayerThreads)
            {
                foreach (var th in allPlayerThreads)
                {
                    th.SendSystemwideMessage(mess);
                }
            }
        }

        private void ProcessNextConnection(TcpListener server)
        {

            TcpClient client = server.AcceptTcpClient();
            client.ReceiveTimeout = 1000000;
            Console.WriteLine("Connection Accepted");

            UberMundoConnectionThread th = new UberMundoConnectionThread(this, client);
            lock (allPlayerThreads)
            {
                allPlayerThreads.Add(th);
            }
            th.Start();
        }

        internal void ThreadDone(UberMundoConnectionThread uberMundoConnectionThread)
        {
            lock(allPlayerThreads)
            {
                allPlayerThreads.Remove(uberMundoConnectionThread);
                if(uberMundoConnectionThread.player != null)
                    uberMundoConnectionThread.player.ConnThread = null;
            }
        }

        /// <summary>
        /// From https://stackoverflow.com/questions/6803073/get-local-ip-address. Is smart and only gets IPs that are V4
        /// and are not disabled (status Up).
        /// </summary>
        /// <param name="_type"></param>
        /// <returns></returns>
        public static string[] GetAllLocalIPv4(NetworkInterfaceType _type = NetworkInterfaceType.Ethernet)
        {
            List<string> ipAddrList = new List<string>();
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipAddrList.Add(ip.Address.ToString());
                        }
                    }
                }
            }
            return ipAddrList.ToArray();
        }

        /// <summary>
        /// Tell all other players in the oldWorld that this player left.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="oldWorldID"></param>
        internal void SendPlayerLeftWorld(Player player, int oldWorldID)
        {
            if (player == null || oldWorldID == 0) return;
            lock (Universe.worlds) {
                WorldData wd = Universe.GetWorld(oldWorldID);
                if(wd != null)
                {
                    lock(wd.players)
                    {
                        foreach(Player p in wd.players.Values)
                        {
                            if (p.UbermundoId != player.UbermundoId)
                                p.SendPlayerLeftWorld(player.UbermundoId, player.SteamID);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Tell all the players in player.CurrentWorldID world that this player arrived.
        /// </summary>
        /// <param name="player"></param>
        internal void SendPlayerEnteredWorld(Player player)
        {
            if (player == null || player.CurrentWorldId == 0) return;
            lock (Universe.worlds)
            {
                WorldData wd = Universe.GetWorld(player.CurrentWorldId);
                if (wd != null)
                {
                    lock (wd.players)
                    {
                        foreach (Player p in wd.players.Values)
                        {
                            if (p.UbermundoId != player.UbermundoId)
                                p.SendPlayerEnteredWorld(player.UbermundoId, player.SteamID);
                        }
                    }
                }
            }
        }
    }
}