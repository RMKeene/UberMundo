#pragma once
#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"

UENUM(BlueprintType)
enum EUbermundoPacketCodes {
	/// <summary>
	/// If this packet is received, send back a Pong packet.
	/// </summary>
	UBERMUNDOP2P_Ping UMETA(DisplayName = "Ping"),
	/// <summary>
	/// If received, send back a KeepAliveResponse.
	/// </summary>
	UBERMUNDOP2P_Keepalive UMETA(DisplayName = "KeepAlive"),
	UBERMUNDOP2P_KeepAliveResponse UMETA(DisplayName = "KeepAliveResponse"),
	/// <summary>
	/// A player just connected to this client. Send the world state back.
	/// </summary>
	UBERMUNDOP2P_PlayerConnected UMETA(DisplayName = "PlayerConnected"),
	/// <summary>
	/// A player left this client.
	/// </summary>
	UBERMUNDOP2P_PlayerDisconnected UMETA(DisplayName = "PlayerDisconnected"),
	/// <summary>
	/// If the player is YOU then you have been kicked forom the client back to the player's own client.
	/// </summary>
	UBERMUNDOP2P_PlayerKicked UMETA(DisplayName = "PlayerKicked"),
	/// <summary>
	/// Player's dynamic 3D world state update.
	/// </summary>
	UBERMUNDOP2P_Player3DState UMETA(DisplayName = "Player3DState"),
	/// <summary>
	/// Simple text chat message in unicode. No response needed.
	/// </summary>
	UBERMUNDOP2P_PlayerTextMsg UMETA(DisplayName = "PlayerTextMsg"),
	UBERMUNDOP2P_PlayerImageMsg UMETA(DisplayName = "PlayerImageMsg"),
	UBERMUNDOP2P_PlayerVoiceMsg UMETA(DisplayName = "PlayerVoiceMsg"),
	/// <summary>
	/// This client is receiving the player's known other players WOT data.
	/// </summary>
	UBERMUNDOP2P_PlayerWOT UMETA(DisplayName = "PlayerWOT"),

	/// <summary>
	/// If this packet is received, do nothing. Sent as a response to a Ping.
	/// </summary>
	UBERMUNDOP2P_Pong UMETA(DisplayName = "Pong"),

};
