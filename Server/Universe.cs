using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UberMundoServer;

namespace UberMundo
{
    public static class Universe
    {
        private static bool debugMessages = true;
        public static Dictionary<int, WorldData> worlds = new Dictionary<int, WorldData>();
        private static SqliteConnection dbConn;

        static Universe()
        {
            SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder();
            builder.DataSource = UbermundoDBCommon.SQLDatabase;
            builder.Mode = SqliteOpenMode.ReadWriteCreate;
            string cs = builder.ConnectionString;

            dbConn = new SqliteConnection(cs);
            dbConn.Open();
            LoadWorldCache();
        }

        public static void RemovePlayer(Player player)
        {
            if (debugMessages) Console.WriteLine($"Universe: RemovePlayer {player.UbermundoId}");
            // Lock order rule: Always lock outer (Universe) first, then lock World, then player or thing inward in the hierarchy.
            lock (worlds)
            {
                foreach (WorldData wd in worlds.Values)
                {
                    if (debugMessages) Console.WriteLine($"Universe: RemovePlayer {player.UbermundoId} - Removed");
                    wd.RemovePlayer(player);
                }
            }
        }

        public static bool TransferPlayer(Player player, int srcWorldUid, int dstWorldUid)
        {
            if (debugMessages) Console.WriteLine($"Universe: TransferPlayer ply:{player.UbermundoId} : {srcWorldUid} --> {dstWorldUid}");
            WorldData src;
            WorldData dst;
            lock (worlds)
            {
                if (worlds.TryGetValue(srcWorldUid, out src) && worlds.TryGetValue(dstWorldUid, out dst))
                {
                    src.RemovePlayer(player);
                    dst.AddPlayer(player);
                    if (debugMessages) Console.WriteLine($"Universe: TransferPlayer ply:{player.UbermundoId} : {srcWorldUid} --> {dstWorldUid} SUCCESS");
                    return true;
                }
            }
            if (debugMessages) Console.WriteLine($"Universe: TransferPlayer ply:{player.UbermundoId} : {srcWorldUid} --> {dstWorldUid} FAIL");
            return false;
        }

        /// <summary>
        /// Find a world.
        /// Return null if no such world.
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public static WorldData GetWorld(int uid)
        {
            if (debugMessages) Console.WriteLine($"Universe: GetWorld w:{uid}");
            WorldData w;
            lock (worlds)
            {
                if (!worlds.TryGetValue(uid, out w))
                {
                    return null;
                }
            }
            return w;
        }

        /// <summary>
        /// Creates world data and inserts to DB. Returns the WorldData with worldId filled in.
        /// The worldName may get sanitized so the return worldName may be different.
        /// Does NOT create the empty world data file.
        /// </summary>
        /// <param name="ownerPlayerId"></param>
        /// <param name="wotToSee"></param>
        /// <param name="worldName"></param>
        /// <returns></returns>
        public static WorldData CreateWorld(int ownerPlayerId = 0, int wotToSee = 10,
            string worldName = "", int worldVersion = 1, float playerUpdateIntervalSec = 0.1f)
        {
            if (debugMessages) Console.WriteLine($"Universe: CreateWorld owner player: {ownerPlayerId}, name:{worldName}");
            WorldData w;
            lock (worlds)
            {
                WorldData wd = new WorldData(0);
                wd.WorldName = worldName;
                wd.WotToSee = wotToSee;
                wd.OwningPlayerId = ownerPlayerId;
                wd.WorldVersion = worldVersion;
                wd.PlayerUpdateIntervalFactor = playerUpdateIntervalSec;

                int wid = StoreWorldMetadata(wd);
                w = new WorldData(wid, wd);
                Debug.Assert(wid != 0, "World ID is Zero on insert into Universe?");

                StorageDatabase.Instance.PutWorldData(w);

                worlds.Add(wid, w);
                if (debugMessages) Console.WriteLine($"Universe: CreateOrGetWorld w:{wid} name:{worldName} Added");

            }
            return w;
        }

        public static void LoadWorldCache()
        {
            lock (worlds)
            {
                if (worlds.Count > 0)
                    return;
                using var cmd = new SqliteCommand($"SELECT ID, owner_player, wot_to_see, world_name, world_version, world_update_period, next_object_id from ubermundo_worlds", dbConn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int worldId = reader.GetInt32(0);
                    int ownerPlayerId = reader.GetInt32(1);
                    int wotToSee = reader.GetInt32(2);
                    string worldName = reader.GetString(3);
                    int worldVersion = reader.GetInt32(4);
                    float world_update_period = reader.GetFloat(5);
                    int nextObjectId = reader.GetInt32(6);

                    WorldData wd = new WorldData(worldId);
                    wd.WorldName = worldName;
                    wd.WotToSee = wotToSee;
                    wd.OwningPlayerId = ownerPlayerId;
                    wd.WorldVersion = worldVersion;
                    wd.PlayerUpdateIntervalFactor = world_update_period;
                    wd.NextObjectId = nextObjectId;

                    Debug.Assert(worldId != 0, "Invalid zero world ID.");
                    worlds[worldId] = wd;
                }
            }
        }

        /// <summary>
        /// Create a new record and put it in the databse, returns the new world ID.
        /// </summary>
        /// <param name="wd"></param>
        /// <returns></returns>
        public static int StoreWorldMetadata(WorldData wd)
        {
            lock (worlds)
            {
                if (wd.WorldID == 0)
                {
                    using var cmd = new SqliteCommand(
                        $"INSERT INTO ubermundo_worlds (owner_player, wot_to_see, world_name, world_version, world_update_period, next_object_id) VALUES (@ow, @wot, @n, @v, @udt, @nid); select last_insert_rowid();",
                        dbConn);
                    cmd.Parameters.AddWithValue("@ow", wd.OwningPlayerId);
                    cmd.Parameters.AddWithValue("@wot", wd.WotToSee);
                    cmd.Parameters.AddWithValue("@n", wd.WorldName);
                    cmd.Parameters.AddWithValue("@v", wd.WorldVersion);
                    cmd.Parameters.AddWithValue("@udt", wd.PlayerUpdateIntervalFactor);
                    cmd.Parameters.AddWithValue("@nid", wd.NextObjectId);
                    object o = cmd.ExecuteScalar();
                    Int64 ii = (Int64)o;
                    return (int)ii;
                }
                else
                {
                    // Do not try to update the nextObjectID here.  See function GetNextObjectID(WorldData wd).
                    using var cmd = new SqliteCommand(
                        $"UPDATE ubermundo_worlds SET owner_player = @ow, wot_to_see = @wot, world_name = @n, world_version = @v, " +
                        "world_update_period = @udt WHERE ID = @uid",
                        dbConn);
                    cmd.Parameters.AddWithValue("@ow", wd.OwningPlayerId);
                    cmd.Parameters.AddWithValue("@wot", wd.WotToSee);
                    cmd.Parameters.AddWithValue("@n", wd.WorldName);
                    cmd.Parameters.AddWithValue("@v", wd.WorldVersion);
                    cmd.Parameters.AddWithValue("@udt", wd.PlayerUpdateIntervalFactor);
                    cmd.Parameters.AddWithValue("@uid", wd.WorldID);
                    int linesChanged = cmd.ExecuteNonQuery();
                    return wd.WorldID;
                }
            }
        }

        public static int GetNextObjectID(WorldData wd)
        {
            lock (worlds)
            {
                SqliteTransaction trans = null;
                Int64 ii = 0;
                try
                {
                    trans = dbConn.BeginTransaction(System.Data.IsolationLevel.Serializable);
                    var cmd = new SqliteCommand(
                        $"UPDATE ubermundo_worlds SET next_object_id = next_object_id + 1 WHERE ID = @id;",
                        dbConn, trans);
                    cmd.Parameters.AddWithValue("@id", wd.WorldID);
                    cmd.ExecuteNonQuery();

                    cmd = new SqliteCommand(
                         $"SELECT next_object_id FROM ubermundo_worlds;",
                         dbConn, trans);
                    var o = cmd.ExecuteScalar();
                    ii = (Int64)o;

                    trans.Commit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database transaction failed to get next ID for world {wd.WorldID}. {ex}");
                    trans.Rollback();
                }
                return (int)ii;
            }
        }

        internal static List<WorldData> SnapshotAllWorldMetadata()
        {
            lock (worlds)
            {
                return worlds.Values.ToList();
            }
        }
    }
}
