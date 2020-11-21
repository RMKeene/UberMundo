using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;

namespace UberMundoServer
{
    public class UbermundoDBCommon
    {
        public static string SQLDatabase = @"E:\UbermundoData\Ubermundo.db";

        public static void CreateSchemaIf(SqliteConnection dbConn)
        {
            // Note, SQLite INTEGER is a signed Int64.
            using var cmdu = new SqliteCommand("CREATE TABLE IF NOT EXISTS ubermundo_users (" +
                    // This is the ubermundo player unique ID. First player insert will be ID 1.
                    "ID INTEGER NOT NULL PRIMARY KEY," +
                    // Always exaclty 8 bytes being the steam ID 64 bit unsigend number, little endian order.
                    "steam_id BLOB" +
                ")", dbConn);
            cmdu.ExecuteScalar();

            using var cmdw = new SqliteCommand("CREATE TABLE IF NOT EXISTS ubermundo_worlds (" +
                    // This is the ubermundo player unique ID.
                    "ID INTEGER NOT NULL PRIMARY KEY," +
                    "owner_player INT NOT NULL," +
                    "wot_to_see INT NOT NULL," +
                    "world_name CHAR(64) NOT NULL," +
                    "world_version INT NOT NULL," +
                    "world_update_period REAL NOT NULL," +
                    "next_object_id INT NOT NULL" +
                ")", dbConn);
            cmdw.ExecuteScalar();
        }

        public static void DestroySchemaIf(SqliteConnection dbConn)
        {
            DropTable(dbConn, "ubermundo_users");
            DropTable(dbConn, "ubermundo_worlds");
        }

        public static void DropTable(SqliteConnection dbConn, string tableName)
        {
            using var cmdu = new SqliteCommand($"DROP TABLE IF EXISTS {tableName}", dbConn);
            cmdu.ExecuteScalar();
        }

        internal static void DeleteAllPlayers(SqliteConnection dbConn)
        {
            using var cmdu = new SqliteCommand($"DELETE FROM ubermundo_users", dbConn);
            cmdu.ExecuteScalar();
        }
    }
}
