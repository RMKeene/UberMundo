#pragma once
#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"

/// There are two targets of packets.  Player to and from the Server (DB) and Player to Player (Pier to Pier).
UENUM(BlueprintType)
enum EUbermundoPacketCodes {
	// ============================ DB MESSAGES ==============
	/// <summary>
	/// The start of Packet Codes for player to server and server to player messages. Never sent as a packet code.
	/// </summary>
	UBERMUNDOPC_DB_START = 0 UMETA(DisplayName = "BD_START"),
	UBERMUNDOPC_DB_Keepalive = 1 UMETA(DisplayName = "DB_KeepAlive"),
	UBERMUNDOPC_DB_KeepAliveResponse = 2 UMETA(DisplayName = "DB_KeepAliveResponse"),
	/// <summary>
	/// A player just connected to this client. Send the world state back.
	/// </summary>
	UBERMUNDOPC_DB_PlayerConnected = 3 UMETA(DisplayName = "DB_PlayerConnected"),
	/// <summary>
	/// A player left this client.
	/// </summary>
	UBERMUNDOPC_DB_PlayerDisconnected = 4 UMETA(DisplayName = "DB_PlayerDisconnected"),
	/// <summary>
	/// This player wants the server to kick a player. It is possible to kick your self.
	/// </summary>
	UBERMUNDOPC_DB_AskPlayerKicked = 5 UMETA(DisplayName = "PlayerKicked"),
	/// <summary>
	/// If the player is YOU then you have been kicked from the client back to your own client.
	/// </summary>
	UBERMUNDOPC_DB_PlayerKicked = 6 UMETA(DisplayName = "DB_PlayerKicked"),
	/// <summary>
	/// A player wants the next UID for an in world object. Usually to spawn an object.
	/// </summary>
	UBERMUNDOPC_DB_NextUID = 7 UMETA(DisplayName = "DB_NextUID"),
	/// <summary>
	/// Answer to DB_NextUID with the UID
	/// </summary>
	UBERMUNDOPC_DB_NextUIDResponse = 8 UMETA(DisplayName = "DB_NextUIDResponse"),
	/// <summary>
	/// Answer to DB_PlayerConnected
	/// </summary>
	UBERMUNDOPC_DB_WorldContentsResponse = 9 UMETA(DisplayName = "DB_WorldContentsResponse"),
	/// <summary>
	/// The player is asking the DB to add an object.  Must have previously gotten a DB_NextUID.
	/// </summary>
	UBERMUNDOPC_DB_AddObject = 10 UMETA(DisplayName = "DB_AddObject"),
	/// <summary>
	/// A player is asking to delete an object from the DB.
	/// </summary>
	UBERMUNDOPC_DB_RemoveObject = 11 UMETA(DisplayName = "DB_RemoveObject"),
	/// <summary>
	/// TBD
	/// </summary>
	UBERMUNDOPC_DB_PlayerWOT = 12 UMETA(DisplayName = "DB_PlayerWOT"),

	// ============================================== PLAYER to PLAYER MESSAGES ===============
	/// <summary>
	/// The start of Player ot Player messages Packet Codes. Never sent as a packet code.
	/// </summary>
	UBERMUNDOPC_P2P_START = 100 UMETA(DisplayName = "P2P_START"),
	/// <summary>
	/// Player's dynamic 3D world state update. Is also in effect a Keap Alive message.
	/// </summary>
	UBERMUNDOPC_P2P_Player3DState = 102 UMETA(DisplayName = "P2P_Player3DState"),
	UBERMUNDOPC_P2P_PlayerGrabbed = 103 UMETA(DisplayName = "P2P_PlayerGrabbed"),
	UBERMUNDOPC_P2P_PlayerReleased = 104 UMETA(DisplayName = "P2P_PlayerReleased"),
	/// <summary>
	/// Simple text chat message in unicode. No response needed.
	/// </summary>
	UBERMUNDOPC_P2P_PlayerTextMsg = 120 UMETA(DisplayName = "P2P_PlayerTextMsg"),
	UBERMUNDOPC_P2P_PlayerImageMsg = 121 UMETA(DisplayName = "P2P_PlayerImageMsg"),
	UBERMUNDOPC_P2P_PlayerVoiceMsg = 122 UMETA(DisplayName = "P2P_PlayerVoiceMsg"),
	UBERMUNDOPC_P2P_PlayerEmoteMsg = 123 UMETA(DisplayName = "P2P_PlayerEmoteMsg"),
};
