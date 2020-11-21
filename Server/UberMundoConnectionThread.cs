using Google.Protobuf;
using LowEntryNetworkCSharp;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UberMundo
{
    public class UberMundoConnectionThread
    {
        static readonly bool debugMessages = true;

        public readonly Thread th;
        /// <summary>
        /// If two threads are going to both read or both write to this you MUST lock in client.
        /// (See ProcessLoop where it calls ActOnMessages, and SendSystemwideMessage)
        /// </summary>
        public readonly TcpClient client;
        public NetworkStream stream = null;
        public Player player;
        private UberMundoTCPListener uberMundoTCPListener;

        public UberMundoConnectionThread(UberMundoTCPListener uberMundoTCPListener, TcpClient client)
        {
            this.uberMundoTCPListener = uberMundoTCPListener;
            this.client = client;
            th = new Thread(ProcessLoop);
        }

        public void Start()
        {
            th.Start();
        }

        internal void ProcessLoop()
        {
            byte[] szbuf = new byte[4];
            try
            {
                stream = client.GetStream();

                while (true)
                {
                    szbuf[0] = szbuf[1] = szbuf[2] = szbuf[3] = 0;

                    int n = stream.Read(szbuf, 0, 1);
                    int sz = -1;
                    if (n == 1)
                    {
                        sz = szbuf[0];
                        // Eight bit flags single byte size or 4 byte size. (big-endian)
                        if ((sz & 0x0080) != 0)
                        {
                            sz &= ~0x0080;
                            szbuf[0] = (byte)sz;
                            // Read the next 3 bytes.
                            n = stream.Read(szbuf, 1, 3);
                            if (n != 3)
                            {
                                Console.WriteLine("Connection dropped or corrupt data.");
                                break;
                            }
                            if (BitConverter.IsLittleEndian)
                                sz = BitConverter.ToInt32(szbuf.Reverse().ToArray());
                            else
                                sz = BitConverter.ToInt32(szbuf);
                        }

                        if (sz > 0)
                        {
                            byte[] ba = new byte[sz];
                            n = stream.Read(ba);
                            if (n == sz)
                            {
                                lock (client)
                                    ActOnMessages(ba);
                            }
                        }
                    }
                    else if (n == 0)
                    {
                        Console.WriteLine("Connection closed.");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex);
            }
            finally
            {
                client.Close();
            }
            TellAllOtherPlayersThisPlayerIsLeavingGame();
            UserDatabase.Instance.DeactivatePlayer(player);
            uberMundoTCPListener.ThreadDone(this);
        }


        private void ActOnMessages(byte[] ba)
        {
            LowEntryByteReader br = new LowEntryByteReader(ba);

            UberMundoEventCode pt = (UberMundoEventCode)br.GetByte();
            if (debugMessages) Console.WriteLine($"Message: {pt}");
            switch (pt)
            {
                case UberMundoEventCode.P2SPlayerAnnounce_Steam:
                    ActOnPlayerAnnounceToServer_Steam(br);
                    break;
                case UberMundoEventCode.P2SPlayerUpdate:
                    ActOnPlayerUpdateToServer(br);
                    break;
                case UberMundoEventCode.P2SLeavingGame:
                    ActOnP2SLeavingGame(br);
                    break;
                case UberMundoEventCode.P2SRequestLevelData:
                    ActOnP2SRequestLevelData(br);
                    break;
                case UberMundoEventCode.P2SSaveLevelData:
                    ActOnP2SSaveLevelData(br);
                    break;
                case UberMundoEventCode.P2SCreateNewWorld:
                    ActOnP2SCreateNewWorld(br);
                    break;
                case UberMundoEventCode.P2SRequestLevelMetadata:
                    ActOnP2SRequestLevelMetadata(br);
                    break;
                case UberMundoEventCode.P2SRequestAllLevelMetas:
                    ActOnP2SRequestRequestAllLevelMetas(br);
                    break;
                case UberMundoEventCode.P2SAddThing:
                    ActOnP2SAddThing(br);
                    break;
                case UberMundoEventCode.P2SRemoveThing:
                    ActOnP2SRemoveThing(br);
                    break;
                case UberMundoEventCode.P2SGetNextObjectID:
                    ActOnP2SGetNextObjectID(br);
                    break;
            }
        }

        internal void ActOnP2SGetNextObjectID(LowEntryByteReader br)
        {
            if (debugMessages) Console.WriteLine($"ActOnP2SGetNextObjectID from player {player.UbermundoId}:");
            int worldID = br.GetInteger();

            WorldData wd = Universe.GetWorld(worldID);
            int newId = 0;
            if (wd != null)
            {
                newId = Universe.GetNextObjectID(wd);
            }

            LowEntryByteWriter bw = new LowEntryByteWriter();
            bw.AddByte((byte)UberMundoEventCode.S2PNextObjectID);
            bw.AddInteger(newId);
            WrapAndSend(bw);

            if (debugMessages)
            {
                if(newId > 0)
                    Console.WriteLine($"ActOnP2SGetNextObjectID from player {player.UbermundoId}: world {worldID}, returned ID {newId}");
                else
                    Console.WriteLine($"ActOnP2SGetNextObjectID from player {player.UbermundoId}: world {worldID}: No such world ID");
            }
        }

        /// <summary>
        /// Sends a S2PPlayerEnteredLevel message to this player that some other player entered, by steam ID.
        /// </summary>
        /// <param name="steamID"></param>
        internal void SendOtherPlayerEnteredWorld(int playerUbermundoID, byte[] steamID)
        {
            if (debugMessages) Console.WriteLine($"SendOtherPlayerEnteredWorld: {playerUbermundoID} {BitConverter.ToUInt64(steamID)}");
            LowEntryByteWriter bw = new LowEntryByteWriter();
            bw.AddByte((byte)UberMundoEventCode.S2PPlayerEnteredLevel);
            bw.AddInteger(playerUbermundoID);
            bw.AddByteArray(steamID);
            WrapAndSend(bw);
        }

        /// <summary>
        /// Sends a S2PPlayervLevel message to this player that some other player left, by steam ID.
        /// </summary>
        /// <param name="steamID"></param>
        internal void SendOtherPlayerLeftWorld(int playerUbermundoID, byte[] steamID)
        {
            if (debugMessages) Console.WriteLine($"SendOtherPlayerLeftWorld: {playerUbermundoID} {BitConverter.ToUInt64(steamID)}");
            LowEntryByteWriter bw = new LowEntryByteWriter();
            bw.AddByte((byte)UberMundoEventCode.S2PPlayerLeftLevel);
            bw.AddInteger(playerUbermundoID);
            bw.AddByteArray(steamID);
            WrapAndSend(bw);
        }

        internal void SendSystemwideMessage(string mess)
        {
            if (debugMessages) Console.WriteLine($"SendSystemwideMessage: {mess}");
            LowEntryByteWriter bw = new LowEntryByteWriter();
            bw.AddByte((byte)UberMundoEventCode.S2PAnnouncementMsg);
            bw.AddStringUtf8(mess);
            WrapAndSend(bw);
        }

        private void ActOnP2SRemoveThing(LowEntryByteReader br)
        {
            if (debugMessages) Console.WriteLine($"ActOnP2SRemoveThing:");
            int thingId = br.GetInteger();
            WorldData wd = Universe.GetWorld(player.CurrentWorldId);
            // TODO - Implement this
        }

        private void ActOnP2SAddThing(LowEntryByteReader br)
        {
            if (debugMessages) Console.WriteLine($"ActOnP2SAddThing:");
            string thingAssetPath = br.GetStringUtf8();
            // TODO - Implement this.
        }

        private void ActOnPlayerAnnounceToServer_Steam(LowEntryByteReader br)
        {
            if (debugMessages) Console.WriteLine($"ActOnPlayerAnnounceToServer_Steam");
            byte[] steamID = br.GetByteArray();
            if (debugMessages) Console.WriteLine($"ActOnPlayerAnnounceToServer_Steam: Steam ID {BitConverter.ToUInt64(steamID)}");
            Debug.Assert(steamID != null && steamID.Length == 8);
            player = UserDatabase.Instance.GetOrCreatePlayerForSteamID(this, steamID);
            player.ConnThread = this;
            LowEntryByteWriter bw = new LowEntryByteWriter();
            bw.AddByte((byte)UberMundoEventCode.S2PYourUbermundoID);
            bw.AddInteger(player.UbermundoId);
            WrapAndSend(bw);
        }

        /// <summary>
        /// The players sends to the server approx where they are.  This is X,Y,Z in 10 meter increments.
        /// Also the player sends current world, animation etc.
        /// If the world changed then sends the world left/entered messages to all relevant players.
        /// </summary>
        /// <param name="br"></param>
        private void ActOnPlayerUpdateToServer(LowEntryByteReader br)
        {
            if (debugMessages) Console.WriteLine($"ActOnPlayerUpdateToServer");
            if (player == null) return;
            int oldWorldId = player.CurrentWorldId;
            int worldID = br.GetInteger();
            // X,Y,Z are in decameters (10 meters per unit)
            Int16 X = ReadInt16(br);
            Int16 Y = ReadInt16(br);
            Int16 Z = ReadInt16(br);
            if (debugMessages) Console.WriteLine($"ActOnPlayerUpdateToServer: ply={player.UbermundoId}, w={worldID} {X * 10.0} {Y * 10.0} {Z * 10.0} meters");

            player.WorldXDecameters = X;
            player.WorldYDecameters = Y;
            player.WorldZDecameters = Z;
            bool needsWorldSwitch = player.TakeCurrentWorldUpdate(worldID);
            player.LastClientContact = DateTime.Now;

            if (needsWorldSwitch)
            {
                uberMundoTCPListener.SendPlayerLeftWorld(player, oldWorldId);
                uberMundoTCPListener.SendPlayerEnteredWorld(player);
                SendAllPlayersInLevelToThisPlayer();
            }
        }

        private void ActOnP2SLeavingGame(LowEntryByteReader br)
        {
            if (debugMessages) Console.WriteLine($"ActOnP2SLeavingGame:");
            int playerUbermundoId = br.GetInteger();
            TellAllOtherPlayersThisPlayerIsLeavingGame();
            UserDatabase.Instance.DeactivatePlayer(playerUbermundoId);
        }

        /// <summary>
        /// Sends the player left world to all players in this level.
        /// </summary>
        private void TellAllOtherPlayersThisPlayerIsLeavingGame()
        {
            if (debugMessages) Console.WriteLine($"TellAllOtherPlayersThisPlayerIsLeavingGame:");
            if (player.CurrentWorldId != 0)
            {
                uberMundoTCPListener.SendPlayerLeftWorld(player, player.CurrentWorldId);
            }
        }

        /// <summary>
        /// Returns a short version of level data, onlky what the player can see.
        /// </summary>
        /// <param name="br"></param>
        private void ActOnP2SRequestRequestAllLevelMetas(LowEntryByteReader br)
        {
            if (debugMessages) Console.WriteLine($"ActOnP2SRequestRequestAllLevelMetas:");
            // No data in the incomming packet other than the packet code.

            var d = Universe.SnapshotAllWorldMetadata();
            LowEntryByteWriter bw = new LowEntryByteWriter();
            bw.AddByte((byte)UberMundoEventCode.S2PAllLevelMetadata);
            bw.AddInteger(d.Count);
            foreach (WorldData m in d)
                m.WriteData(bw);
            WrapAndSend(bw);
        }

        private void ActOnP2SRequestLevelMetadata(LowEntryByteReader br)
        {
            int worldId = br.GetInteger();
            if (debugMessages) Console.WriteLine($"    Request Level Metadata: World Guid: {worldId}");

            WorldData worldData = Universe.GetWorld(worldId);
            LowEntryByteWriter bw = new LowEntryByteWriter();
            bw.AddByte((byte)UberMundoEventCode.S2PLevelMetadata);
            if (worldData == null)
            {
                WorldData.WriteEmptyData(bw);
            }
            else
            {
                worldData.WriteData(bw);
            }

            WrapAndSend(bw);
        }

        private void ActOnP2SCreateNewWorld(LowEntryByteReader br)
        {
            if (debugMessages) Console.WriteLine($"ActOnP2SCreateNewWorld:");
            if (player == null) return;

            WorldData wdIn = new WorldData(br);

            WorldData wd = Universe.CreateWorld(player.UbermundoId, wdIn.WotToSee, wdIn.WorldName, wdIn.WorldVersion, wdIn.PlayerUpdateIntervalFactor);
            // Write the empty world contets file.
            StorageDatabase.Instance.PutWorldData(wd, new byte[0]);


            LowEntryByteWriter bw = new LowEntryByteWriter();
            bw.AddByte((byte)UberMundoEventCode.S2PWorldCreated);
            bw.AddInteger(wd.WorldID);
            WrapAndSend(bw);
            // The client will get this and then Teleport to the new world and edit it.
        }


        private void ActOnP2SRequestLevelData(LowEntryByteReader br)
        {
            int worldId = br.GetInteger();
            if (debugMessages) Console.WriteLine($"    ActOnRequestLevelData: {worldId}");

            // Gets the metadata from the Universe, and the contents data from disk as JSON.
            WorldData md = StorageDatabase.Instance.GetWorldData(worldId, out byte[] worldData);
            LowEntryByteWriter bw = new LowEntryByteWriter();
            bw.AddByte((byte)UberMundoEventCode.S2PLevelData);
            if (md == null)
            {
                Console.WriteLine($"Invalid world Guid, no data: {worldId}");
                WorldData.WriteEmptyData(bw);
                bw.AddByteArray(new byte[0]);
            }
            else
            {
                // The Header is stored in the disk file as the start of the world data so does not
                // need to be added here from md.
                md.WriteData(bw);
                bw.AddByteArray(worldData);
            }

            WrapAndSend(bw);
        }

        private void ActOnP2SSaveLevelData(LowEntryByteReader br)
        {
            // Read the header
            WorldData md = new WorldData(br);

            // Update the SQL database of world metadata to match.
            Universe.StoreWorldMetadata(md);

            // World Data is simple a series of world objects.
            byte[] worldData = br.GetByteArray();
            if (debugMessages) Console.WriteLine($"    Store: World Name: {md.WorldName} : {md.WorldID}");
            StorageDatabase.Instance.PutWorldData(md, worldData);
        }

        public void SendAllPlayersInLevelToThisPlayer()
        {
            if (debugMessages) Console.WriteLine($"SendAllPlayersInLevelToThisPlayer: to player {player.UbermundoId}");
            if (player == null) return;
            if (player.CurrentWorldId == 0) return;
            WorldData wd = Universe.GetWorld(player.CurrentWorldId);
            if (wd == null) return;
            int N = 0;
            lock (wd.players)
            {
                N = wd.players.Values.Count((p) => p.UbermundoId != player.UbermundoId);
                if (debugMessages) Console.WriteLine($"SendAllPlayersInLevelToThisPlayer: Count of non-local players {N}");
                if (N > 0)
                {
                    LowEntryByteWriter bw = new LowEntryByteWriter();
                    bw.AddByte((byte)UberMundoEventCode.S2PAnnouncePlayersToClient_Steam);
                    bw.AddPositiveInteger1(N);
                    foreach (Player p in wd.players.Values)
                    {
                        if (p.UbermundoId != player.UbermundoId)
                        {
                            if (debugMessages) Console.WriteLine($"SendAllPlayersInLevelToThisPlayer: Send player {p.UbermundoId} to Client Player {player.UbermundoId}");
                            bw.AddInteger(p.UbermundoId);
                            Debug.Assert(p.SteamID != null && p.SteamID.Length == 8);
                            bw.AddByteArray(p.SteamID);
                        }
                    }
                    WrapAndSend(bw);
                }
            }
        }

        /// <summary>
        /// Wraps the byte buffer of bw into a LowEntry byte array and sends it over the TCP connection to the client.
        /// </summary>
        /// <param name="bw"></param>
        public void WrapAndSend(LowEntryByteWriter bw)
        {
            LowEntryByteWriter bwOuter = new LowEntryByteWriter();
            // This adds a UInteger array length and then the array
            bwOuter.AddByteArray(bw.buf.ToArray());

            lock (stream)
            {
                stream.Write(bwOuter.buf.ToArray());
                stream.Flush();
            }
        }

        public static Guid ReadGuid(LowEntryByteReader br)
        {
            byte[] guidBytes = new byte[16];
            for (int i = 0; i < 16; i++)
                guidBytes[i] = br.GetByte();
            return new Guid(guidBytes);
        }

        public static void WriteGuid(LowEntryByteWriter bw, Guid g)
        {
            foreach (var b in g.ToByteArray())
            {
                bw.AddByte(b);
            }
        }

        /// <summary>
        /// X, Y, Z as float
        /// </summary>
        /// <param name="bw"></param>
        /// <param name="p"></param>
        public static void WriteLocation(LowEntryByteWriter bw, Location p)
        {
            bw.AddFloat(p.X);
            bw.AddFloat(p.Y);
            bw.AddFloat(p.Z);
        }

        /// <summary>
        /// VX, VY, VZ as float
        /// </summary>
        /// <param name="bw"></param>
        /// <param name="p"></param>
        public static void WriteVelocity(LowEntryByteWriter bw, Velocity p)
        {
            bw.AddFloat(p.VX);
            bw.AddFloat(p.VY);
            bw.AddFloat(p.VZ);
        }

        /// <summary>
        /// Only Yaw as float
        /// </summary>
        /// <param name="bw"></param>
        /// <param name="p"></param>
        public static void WriteRotationYaw(LowEntryByteWriter bw, Rotation p)
        {
            bw.AddFloat(p.Yaw);
        }

        /// <summary>
        /// Only Pitch, Yaw as float
        /// </summary>
        /// <param name="bw"></param>
        /// <param name="p"></param>
        public static void WriteRotationPitchYaw(LowEntryByteWriter bw, Rotation p)
        {
            bw.AddFloat(p.Pitch);
            bw.AddFloat(p.Yaw);
        }

        public static UInt32 ReadUInt24(LowEntryByteReader br)
        {
            // Big endian.
            return (UInt32)(br.GetByte() << 16 | br.GetByte() << 8 | br.GetByte());
        }

        public static void WriteUInt24(LowEntryByteWriter bw, UInt32 i)
        {
            // Big endian.
            bw.AddByte((byte)(i >> 16));
            bw.AddByte((byte)(i >> 8));
            bw.AddByte((byte)i);
        }
        public static Int16 ReadInt16(LowEntryByteReader br)
        {
            // Big endian.
            return (Int16)(br.GetByte() << 8 | br.GetByte());
        }

        public static void WriteInt16(LowEntryByteWriter bw, Int16 i)
        {
            // Big endian.
            bw.AddByte((byte)(i >> 8));
            bw.AddByte((byte)i);
        }

    }
}
