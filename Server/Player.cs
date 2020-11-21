using LowEntryNetworkCSharp;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using UberMundoServer;

namespace UberMundo
{
    public class Player : IDisposable
    {
        /// <summary>
        /// Players that are not connected will have a null ConnThread.
        /// </summary>
        public UberMundoConnectionThread ConnThread;
        public int UbermundoId = 0;
        public byte[] SteamID = new byte[8];
        public int CurrentWorldId = 0;
        public DateTime LastClientContact = DateTime.MinValue;
        public bool IsActive = false;
        /// <summary>
        /// Where the player is within 10 meters in the current world.
        /// NOT DECIMETERS!
        /// </summary>
        public Int32 WorldXDecameters = 0;
        public Int32 WorldYDecameters = 0;
        public Int32 WorldZDecameters = 0;

        public ulong SteamIDAsUInt64 => BitConverter.ToUInt64(SteamID);

        public Player(UberMundoConnectionThread tcpThread, int ubermundoId, byte[] steamID)
        {
            this.ConnThread = tcpThread;
            this.UbermundoId = ubermundoId;
            this.SteamID = steamID;
        }

        public override string ToString()
        {
            return $"Player {UbermundoId} : World {CurrentWorldId} : SteamID {SteamID} : {WorldXDecameters*10.0} {WorldYDecameters*10.0} {WorldZDecameters*10.0} m.";
        }

        public void Dispose()
        {
            Universe.RemovePlayer(this);
        }

        /// <summary>
        /// Returns true if the player just entered a different World.
        /// False if same world, or no world.
        /// </summary>
        /// <param name="worldID"></param>
        /// <returns></returns>
        internal bool TakeCurrentWorldUpdate(int worldID)
        {
            if(CurrentWorldId != worldID)
            {
                WorldData oldWorld = null;
                if (Universe.worlds.ContainsKey(CurrentWorldId))
                {
                    oldWorld = Universe.worlds[CurrentWorldId];
                    oldWorld.RemovePlayer(this);
                }
                WorldData newWorld = null;
                if (Universe.worlds.ContainsKey(worldID))
                {
                    newWorld = Universe.worlds[worldID];
                    newWorld.AddPlayer(this);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Send to THIS player that some other plyser by steamID left THIS player's world.
        /// </summary>
        /// <param name="otherPlayerSteamID"></param>
        internal void SendPlayerLeftWorld(int otherPlayerUbermundoID, byte[] otherPlayerSteamID)
        {
            if(ConnThread != null)
            {
                ConnThread.SendOtherPlayerLeftWorld(otherPlayerUbermundoID, otherPlayerSteamID);
            }
        }

        /// <summary>
        /// Send to THIS player that some other player by steamID entered THIS player's world.
        /// </summary>
        /// <param name="otherPlayerSteamID"></param>
        internal void SendPlayerEnteredWorld(int otherPlayerUbermundoID, byte[] otherPlayerSteamID)
        {
            if (ConnThread != null)
            {
                ConnThread.SendOtherPlayerEnteredWorld(otherPlayerUbermundoID, otherPlayerSteamID);
            }
        }
    }
}
