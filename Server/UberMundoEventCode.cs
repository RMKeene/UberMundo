namespace UberMundo
{
    /// <summary>
    /// Single byte packet codes. P2S is Player to Server, S2P is Server to Player.
    /// Note that P2P messages are over in the client blueprints ServerTCPConnection.
    /// </summary>
    public enum UberMundoEventCode
    {
        /// <summary>
        /// On connecting the player send only their Steam ID and nothing else. Only valid for Steam based clients.
        /// If the player has never been seen before, they get created and given a unique int UbermundoId.
        /// This message does not tell which World they are in.
        /// </summary>
        P2SPlayerAnnounce_Steam = 6,
        /// <summary>
        /// The server responds to PlayerAnnounceToServer_Steam with the players player number (int).
        /// </summary>
        S2PYourUbermundoID = 1,
        /// Tells server where the player is in 16 bit signed integer 10 meter granularity.
        /// This gives us a max of 10m. X +-32767 = +-327 Killometers size world max.
        /// Also causes the last contact time to get updated.
        /// (Some worlds such as outer space worlds may have other scales than 1:1)
        P2SPlayerUpdate = 2,
        /// <summary>
        /// The player left the Universe. No data in packet.
        /// </summary>
        P2SLeavingGame = 3,
        /// <summary>
        /// Just the players ID, to all players in world the player left the world to go to another world.
        /// </summary>
        S2PPlayerLeftLevel = 4,
        /// <summary>
        /// The player is now in the given world.
        /// Just player Ubermundo ID and level 4 byte integer ID.
        /// </summary>
        S2PPlayerEnteredLevel = 5,

        /// <summary>
        /// Send a count, then each player's Steam ID to a single client.
        /// May be one or more players in the list. All the players in the World the
        /// player is in.
        /// Later the clientes may use P2P to communicat the player's Unbermundo ID
        /// </summary>
        S2PAnnouncePlayersToClient_Steam = 10,

        /// <summary>
        /// Ask the server to send an entire level of data.
        /// TODO - Future feature: Sub blocks and LOD of world details.
        /// </summary>
        P2SRequestLevelData = 30,
        /// <summary>
        /// Response to RequestLevelData
        /// </summary>
        S2PLevelData = 31,
        /// <summary>
        /// Send level data to server to update disk.
        /// </summary>
        P2SSaveLevelData = 32,
        /// <summary>
        /// Ask for a new world ID and get an empty world.
        /// In params are playerId (int), wotToSee (byte), worldName (string).
        /// TODO - Add flags for world attributes.
        /// </summary>
        P2SCreateNewWorld = 33,
        /// <summary>
        /// Response with new world id.  The client then should teleport to the world.
        /// </summary>
        S2PWorldCreated = 34,

        /// <summary>
        /// Get level metatdata from the database.
        /// </summary>
        P2SRequestLevelMetadata = 40,
        /// <summary>
        /// Response to RequestLevelMetadata
        /// </summary>
        S2PLevelMetadata = 42,
        P2SRequestAllLevelMetas = 43,
        S2PAllLevelMetadata = 44,

        // Not implemented
        P2SAddThing = 50,
        // Not implemented
        P2SRemoveThing = 51,
        P2SGetNextObjectID = 52,
        S2PNextObjectID = 53,

        // Note: Chat messages do not go through the server.  They are P2P.

        S2PAnnouncementMsg = 200,

    };
}
