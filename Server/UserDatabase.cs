using Microsoft.Data.Sqlite;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UberMundoServer;

namespace UberMundo
{
    public class UserDatabase
    {
        private static bool debugMessages = true;
        private static object lockObj = new object();
        private static readonly Lazy<UserDatabase> lazy = new Lazy<UserDatabase>(() => new UserDatabase());
        public static UserDatabase Instance => lazy.Value;

        private Dictionary<int, Player> UbermundoIDToUserInfo = new Dictionary<int, Player>();

        /// <summary>
        /// Key SteamID, Value PlayerUbermundoId.
        /// </summary>
        private DefaultDictionary<UInt64, int> KnownSteamIDs = new DefaultDictionary<ulong, int>(() => 0);

        private int reapCheckIndex = 0;
        private int reapCheckChunkSize = 50;
        private int reapCheckChunkMinimumFraction = 50;
        private List<int> reapCheckKeys = new List<int>();

        private SqliteConnection dbConn;

        public UserDatabase()
        {
            lock (lockObj)
            {
                SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder();
                builder.DataSource = UbermundoDBCommon.SQLDatabase;
                builder.Mode = SqliteOpenMode.ReadWriteCreate;
                string cs = builder.ConnectionString;

                dbConn = new SqliteConnection(cs);
                dbConn.Open();

                //UbermundoDBCommon.DestroySchemaIf(dbConn);
                //UbermundoDBCommon.CreateSchemaIf(dbConn);

                //UbermundoDBCommon.DeleteAllPlayers(dbConn);

                LoadAllUsersIntoCache();
            }
        }

        public int CreateNewUserRecordInDB(byte[] steamID)
        {
            Debug.Assert(steamID != null);
            Debug.Assert(steamID.Length == 8);

            // Note: Over in the Blueprint on UE4
            // Dummy Steam ID for PIE mode. 0xFF01010100000000 hexadecimal = 18374969058454929408 unsigned decimal
            // Little-Endian so it is 00 00 00 00 01 01 01 FF byte order in the steamID byte array of the Player.
            // Intentionaly uses the high bits FF to test for sign problems.

            using var cmd = new SqliteCommand(
                $"INSERT INTO ubermundo_users (steam_id) VALUES (@i); select last_insert_rowid();",
                dbConn);
            cmd.Parameters.AddWithValue("@i", steamID);
            object o = cmd.ExecuteScalar();
            Int64 ii = (Int64)o;
            return (int)ii;
        }

        public void LoadAllUsersIntoCache()
        {
            using var cmd = new SqliteCommand($"SELECT ID, steam_id from ubermundo_users", dbConn);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int ubermundoId = reader.GetInt32(0);
                byte[] steamIDBytes = new byte[8]; 
                long n = reader.GetBytes(1, 0, steamIDBytes, 0, 8);
                UInt64 steamID = BitConverter.ToUInt64(steamIDBytes, 0);
                Player pl = new Player(null, ubermundoId, steamIDBytes);
                UbermundoIDToUserInfo[ubermundoId] = pl;
                KnownSteamIDs[pl.SteamIDAsUInt64] = ubermundoId;
            }
        }

        /// <summary>
        /// Get an existing player, or make a new one and store in DB.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="ubermundoId"></param>
        /// <param name="worldID"></param>
        /// <param name="steamID"></param>
        /// <returns></returns>
        public Player GetOrMakePlayer(UberMundoConnectionThread conn, int ubermundoId, byte[] steamID = null)
        {
            if (debugMessages) Console.WriteLine($"UserDatabase: GetOrMakeUserInfo Player {ubermundoId}");
            lock (lockObj)
            {
                if(ubermundoId == 0)
                {
                    // Find by Steam ID
                    if(!KnownSteamIDs.TryGetValue(BitConverter.ToUInt64(steamID, 0), out ubermundoId))
                    {
                        // TryGetValue may corrupt the out ubermundoId so make sure it stays 0.
                        ubermundoId = 0;
                        if (debugMessages) Console.WriteLine($"UserDatabase: GetOrMakeUserInfo Player - Not a known Steam ID {steamID}.");
                    }
                }

                if (ubermundoId == 0 || !UbermundoIDToUserInfo.TryGetValue(ubermundoId, out Player u))
                {
                    if (debugMessages) Console.WriteLine($"UserDatabase: GetOrMakeUserInfo - Create");
                    ubermundoId = UserDatabase.Instance.CreateNewUserRecordInDB(steamID);
                    u = new Player(conn, ubermundoId, steamID);
                    UbermundoIDToUserInfo[ubermundoId] = u;
                }
                else
                {
                    if (debugMessages) Console.WriteLine($"UserDatabase: GetOrMakeUserInfo Player - Found player in Database.");
                }

                u.LastClientContact = DateTime.Now;
                return u;
            }
        }

        public Player GetExistingPlayer(int ubermundoId)
        {
            if (ubermundoId == 0)
                throw new Exception("Ubermundo Player ID of zero not valid.");
            if (!UbermundoIDToUserInfo.TryGetValue(ubermundoId, out Player u))
                throw new InvalidDataException("No such user.");
            return u;
        }

        public Player GetExistingPlayerOrNull(int ubermundoId)
        {
            if (ubermundoId == 0)
                throw new Exception("Ubermundo Player ID of zero not valid.");
            if (!UbermundoIDToUserInfo.TryGetValue(ubermundoId, out Player u))
                return null;
            return u;
        }

        public bool TryGetPlayer(int ubermundoId, out Player p)
        {
            if (ubermundoId == 0)
                throw new Exception("Ubermundo Player ID of zero not valid.");
            if (UbermundoIDToUserInfo.TryGetValue(ubermundoId, out p))
                return true;
            return false;
        }

        public Player GetOrCreatePlayerForSteamID(UberMundoConnectionThread connThread, byte[] steamID)
        {
            if (debugMessages) Console.WriteLine($"UserDatabase: GetOrCreatePlayerForSteamID SteamID {steamID}");
            lock (lockObj)
            {
                Player p = GetOrMakePlayer(connThread, KnownSteamIDs[BitConverter.ToUInt64(steamID, 0)], steamID);
                return p;
            }
        }

        /// <summary>
        /// Players may leave the game gracefully, or may crash out and later rejoin.
        /// So we progressively reap stale players that have crasehd out.  In theory
        /// a dropped TCP/IP connection will destroy the playter, but even so there could
        /// be orphaned player records.
        /// </summary>
        public void ReapStalePlayerInfo()
        {
            //if (debugMessages) Console.WriteLine($"UserDatabase: ReapStalePlayerInfo : Player Count {UbermundoIDToUserInfo.Count()}");
            lock (lockObj)
            {
                // We throttle reaping so it is not all at once.  We check maximum reapCheckChunkSize
                // per call, but if the list is large then instead one reapCheckChunkMinimumFraction-th of the keys.
                if (reapCheckKeys == null || reapCheckKeys.Count == 0)
                {
                    reapCheckKeys = UbermundoIDToUserInfo.Keys.ToList();
                    reapCheckIndex = 0;
                }

                int reapCheckCount = Math.Max(reapCheckKeys.Count / reapCheckChunkMinimumFraction, reapCheckChunkSize);
                DateTime maxOldTime = DateTime.Now - TimeSpan.FromMinutes(2);
                while (reapCheckIndex < reapCheckKeys.Count && reapCheckCount > 0)
                {
                    //if (debugMessages) Console.WriteLine($"UserDatabase: ReapStalePlayerInfo: Loop Idx {reapCheckIndex}");
                    int k = reapCheckKeys[reapCheckIndex];
                    if (UbermundoIDToUserInfo.TryGetValue(k, out Player u) && u.LastClientContact < maxOldTime && u.CurrentWorldId != 0)
                    {
                        DeactivatePlayer(k);
                    }
                    reapCheckCount++;
                    reapCheckIndex++;
                }
                if (reapCheckIndex >= reapCheckKeys.Count)
                {
                    reapCheckIndex = 0;
                    reapCheckKeys = null;
                }
            }
        }

        /// <summary>
        /// Remove from Universe, Current World, entire Game.
        /// </summary>
        /// <param name="playerGuid"></param>
        public void DeactivatePlayer(int playerId)
        {
            if (debugMessages) Console.WriteLine($"UserDatabase: DeactivatePlayer: {playerId}");
            if (playerId == 0)
                return;
            if (UbermundoIDToUserInfo.TryGetValue(playerId, out Player pl))
            {
                if (Universe.worlds.TryGetValue(pl.CurrentWorldId, out WorldData w))
                {
                    w.players.Remove(playerId);
                    if (debugMessages) Console.WriteLine($"UserDatabase: DeactivatePlayer: removed from world info.");
                }
            }
            pl.CurrentWorldId = 0;
            if (debugMessages) Console.WriteLine($"UserDatabase: DeactivatePlayer: deactivated.");
        }

        public void DeactivatePlayer(Player player)
        {
            if (player != null) 
                DeactivatePlayer(player.UbermundoId);
        }
    }
}
