using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

using LowEntryNetworkCSharp;

using Google.Protobuf;

namespace UberMundo
{
    /// <summary>
    /// A singleton design pattern. Handles low level I/O to world **data** files.
    /// Thread safe. (Single global lock is used.).
    /// See WorldData and Universe for world metadata storage (e.g. Name, Owner etc.) via. the SQL Database.
    /// </summary>
    public class StorageDatabase
    {
        private static bool debugMessages = true;
        private static readonly object IOLock = new object();

        private static readonly Lazy<StorageDatabase> lazy = new Lazy<StorageDatabase>(() => new StorageDatabase());

        private StorageDatabase()
        {
            lock (IOLock)
            {
                if (debugMessages) Console.WriteLine($"StorageDatabase: Created Singleton");
                WorldStorageDirectory = ConfigurationManager.AppSettings["WorldStoragePath"];
                Directory.CreateDirectory(WorldStorageDirectory);
            }
        }

        public static StorageDatabase Instance => lazy.Value;

        public string WorldStorageDirectory = @"E:\UbermundoData\Worlds";
        public string WorldFileEnding = ".shr";

        /// <summary>
        /// Get both world data and world metadata. World Metadata is stored in the SQL database. 
        /// World object data is stored in Low Entry Reader/Writer format in a disk file, one file per world (so far).
        /// </summary>
        /// <param name="worldId"></param>
        /// <param name="worldData"></param>
        /// <returns></returns>
        public WorldData GetWorldData(int worldId, out byte[] worldData)
        {
            if (debugMessages) Console.WriteLine($"StorageDatabase: GetWorldData {worldId}");
            lock (IOLock)
            {
                WorldData wd = Universe.GetWorld(worldId);
                if (wd == null)
                {
                    if (debugMessages) Console.WriteLine($"StorageDatabase: GetWorldData {worldId} - No such world Metadata");
                    worldData = new byte[0];
                    return null;
                }

                string fname = GetWorldDataFileNameAndPath(worldId);
                if (File.Exists(fname) == false)
                {
                    if (debugMessages) Console.WriteLine($"StorageDatabase: GetWorldData {worldId} - No such world file, returning empty World content.");
                    worldData = new byte[0];
                    return wd;
                }

                worldData = File.ReadAllBytes(fname);
                //string utfString = Encoding.UTF8.GetString(worldData, 0, worldData.Length);
                if (debugMessages)
                {
                    if (worldData.Length > 2000000)
                        Console.WriteLine($"StorageDatabase: GetWorldData {worldId} - SUCCESS - Data Size {worldData.Length / (1024.0f * 1024.0f):F1} M.");
                    else if (worldData.Length > 2048)
                        Console.WriteLine($"StorageDatabase: GetWorldData {worldId} - SUCCESS - Data Size {worldData.Length / 1024.0f:F1} k.");
                    else
                        Console.WriteLine($"StorageDatabase: GetWorldData {worldId} - SUCCESS - Data Size {worldData.Length}");
                }

                return wd;
            }
        }

        /// <summary>
        /// Put world data to the world data file. Just a raw binary byte write to the appropriate file on disk.
        /// </summary>
        /// <param name="md"></param>
        /// <param name="worldData"></param>
        /// <param name="startOffset"></param>
        public void PutWorldData(WorldData md, byte[] worldData = null)
        {
            if (debugMessages) Console.WriteLine($"StorageDatabase: PutWorldData {md.WorldID} : {md.WorldName}");
            lock (IOLock)
            {
                if(worldData == null)
                {
                    worldData = new byte[0];
                }

                string fname = GetWorldDataFileNameAndPath(md);
                // Directory.CreateDirectory(WorldStorageDirectory);
                // Do not use OpenWrite, it does not truncate and may leave garbage on the end of the file 
                // if the world data is shorter than before.
                using (FileStream binWriter = File.Create(fname))
                {
                    binWriter.Write(worldData, 0, worldData.Length);
                }
            }
            if (debugMessages)
            {
                if (worldData.Length > 2000000)
                    Console.WriteLine($"StorageDatabase: PutWorldData {md.WorldID} - SUCCESS - Data Size {worldData.Length / (1024.0f * 1024.0f):F1} M.");
                else if (worldData.Length > 2048)
                    Console.WriteLine($"StorageDatabase: PutWorldData {md.WorldID} - SUCCESS - Data Size {worldData.Length / 1024.0f:F1} k.");
                else
                    Console.WriteLine($"StorageDatabase: PutWorldData {md.WorldID} - SUCCESS - Data Size {worldData.Length}");
            }
        }

        public void RemoveWorldData(int worldId)
        {
            if (debugMessages) Console.WriteLine($"StorageDatabase: RemoveWorldData {worldId}");
            lock (IOLock)
            {
                DeleteFileIf(GetWorldDataFileNameAndPath(worldId));
            }
        }

        public string GetWorldDataFileNameAndPath(int worldId) => Path.Combine(WorldStorageDirectory, worldId.ToString()) + WorldFileEnding;
        public string GetWorldDataFileNameAndPath(WorldData wd) => Path.Combine(WorldStorageDirectory, wd.WorldID.ToString()) + WorldFileEnding;

        private static void DeleteFileIf(string fname)
        {
            if (debugMessages) Console.WriteLine($"StorageDatabase: DeleteFileIf {fname}");
            if (File.Exists(fname))
                File.Delete(fname);
        }
    }
}
