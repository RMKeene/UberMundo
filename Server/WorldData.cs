using LowEntryNetworkCSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UberMundo
{
    /// <summary>
    /// All the in-memory data about a world.  This is the "metadata" like WorldID, WotToSee, OwningPlayerId, WorldName, etc.
    /// Also there is the not-meta-data like a list of players currently in the level.
    /// On disk is the world detail object data like trees and rocks, in a Low Entry Byte Reader/Writer style binary file.
    /// See StorageDatabase.cs.
    /// (TODO - Use multiple files in locale chunks.)
    /// </summary>
    public class WorldData
    {
        private readonly bool debugMessages = true;
        /// Does not have to be unique
        public string WorldName;
        /// <summary>
        /// Must be unique.
        /// </summary>
        public readonly int WorldID;

        // Range 0 to 100
        public int WotToSee = 10;
        public int OwningPlayerId = 0;
        public int WorldVersion = 1;
        /// <summary>
        /// How often we update the player.  Lower numvers are more frequent updates.
        /// NOT Client to Server updates, this is P2P position updates, the fast ones.
        /// Will be lower (faster) for PvP levels, higher (less frequent) for more social levels.
        /// Currnetly the client just multiplies thius by 0.1 to get 10 updates per second if this is the default 1.0f value.
        /// Higer numbes reduce P2P bandwidth but increases lagginess.
        /// Future: Have this a more dynamic algorithm.
        /// </summary>
        public float PlayerUpdateIntervalFactor = 1.0f;

        /// <summary>
        /// The next unique ID for objects created by players in a world.  Gets retrieved from the database
        /// with careful thread locking and incrementd in place.
        /// </summary>
        public int NextObjectId = 1;

        /// <summary>
        /// The players currently in this World.  Very dynamic, not stored to disk nor to the DB.
        /// </summary>
        public Dictionary<int, Player> players = new Dictionary<int, Player>();

        /// <summary>
        /// Lock and get a clone of players as a list.
        /// </summary>
        /// <returns></returns>
        public List<Player> GetSafeCopyOfPlayers()
        {
            lock (players)
            {
                return players.Values.ToList();
            }
        }

        /// <summary>
        /// Create the data in memory with given uid. worldId may be 0, used as a data container just before a database read.
        /// </summary>
        /// <param name="worldId"></param>
        public WorldData(int worldId)
        {
            if (debugMessages) Console.WriteLine($"WorldData: {worldId} Created");
            this.WorldID = worldId;
        }

        /// <summary>
        /// Create a copy of wd but with uid instead as the this.uid
        /// </summary>
        /// <param name="worldId"></param>
        /// <param name="wd"></param>
        internal WorldData(int worldId, WorldData wd) : this(worldId)
        {
            this.OwningPlayerId = wd.OwningPlayerId;
            this.WorldName = wd.WorldName;
            this.WotToSee = wd.WotToSee;
            this.WorldVersion = wd.WorldVersion;
            this.PlayerUpdateIntervalFactor = wd.PlayerUpdateIntervalFactor;
            this.NextObjectId = wd.NextObjectId;
        }

        /// <summary>
        /// Reads the values in from br.  Does not read players, only the metadata.
        /// </summary>
        /// <param name="br"></param>
        public WorldData(LowEntryByteReader br)
        {
            // Must match over in Build P2S Save Level Data Header in blueprint ServerTCPConnection in the client game.
            this.WorldID = br.GetInteger();
            this.WorldName = br.GetStringUtf8();
            this.OwningPlayerId = br.GetInteger();
            this.WotToSee = br.GetByte();
            this.WorldVersion = br.GetPositiveInteger1();
            this.PlayerUpdateIntervalFactor = br.GetFloat();
            // Next Object ID is server side only.
        }

        /// <summary>
        /// Only removes from this world, not from the Univers or Game.
        /// </summary>
        /// <param name="player"></param>
        public void RemovePlayer(Player player)
        {
            if (debugMessages) Console.WriteLine($"WorldData: RemovePlayer: w:{WorldID} ply:{player.UbermundoId}");
            lock (players)
            {
                players.Remove(player.UbermundoId);
                player.CurrentWorldId = 0;
            }
        }

        /// <summary>
        /// Only adds to this World, not to Univers or Game.
        /// </summary>
        /// <param name="player"></param>
        internal void AddPlayer(Player player)
        {
            lock (players)
            {
                if (debugMessages) Console.WriteLine($"WorldData: AddPlayer: w:{WorldID} ply:{player.UbermundoId}");
                player.CurrentWorldId = WorldID;
                players[player.UbermundoId] = player;
            }
        }

        public void WriteData(LowEntryByteWriter bw)
        {
            bw.AddInteger(WorldID);
            bw.AddStringUtf8(WorldName);
            bw.AddInteger(OwningPlayerId);
            bw.AddByte((byte)WotToSee);
            bw.AddPositiveInteger1(WorldVersion);
            bw.AddFloat(PlayerUpdateIntervalFactor);
            // Next object ID is server side only and only in the database.
        }

        /// <summary>
        /// This writes empty data that is also completely invalid zero values.
        /// E.g. Version is 0, Player UpdateIntervalFactor is 0.0f, world ID is 0 etc.
        /// </summary>
        /// <param name="bw"></param>
        public static void WriteEmptyData(LowEntryByteWriter bw)
        {
            bw.AddInteger(0);
            bw.AddStringUtf8("");
            bw.AddInteger(0); 
            bw.AddByte((byte)0);
            bw.AddPositiveInteger1(0);
            bw.AddFloat(0.0f);
            // No NextObjectID
        }

    }
}
