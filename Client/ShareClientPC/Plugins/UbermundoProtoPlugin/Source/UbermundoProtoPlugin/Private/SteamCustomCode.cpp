// Copyright 2020 Bahnda. All rights reserved.


#include "SteamCustomCode.h"

#include "steam/steam_api.h"

DEFINE_LOG_CATEGORY(UberMundoSteamLog)

	// create a callback field. Having a field will make sure that the callback
	// handle won't be eaten by garbage collector.
	ShareSteamCallbackHooks* Ubermundo_p2PSessionRequestCallback;

	ShareSteamCallbackHooks::ShareSteamCallbackHooks()
		:
		m_CallbackP2PSessionRequest(this, &ShareSteamCallbackHooks::OnP2PSessionRequest),
		m_CallbackP2PSessionConnectFail(this, &ShareSteamCallbackHooks::OnP2PSessionConnectFail)
	{
	}

	// --------------------------------------------------------------------------------- ShareSteamCallbackHooks
	void ShareSteamCallbackHooks::OnP2PSessionRequest(P2PSessionRequest_t* request)
	{
		UE_LOG(UberMundoSteamLog, Warning, TEXT("OnP2PSessionRequest"));
		CSteamID clientId = request->m_steamIDRemote;
		SteamNetworking()->AcceptP2PSessionWithUser(clientId);
	}

	void ShareSteamCallbackHooks::OnP2PSessionConnectFail(P2PSessionConnectFail_t* reason) {
		UE_LOG(UberMundoSteamLog, Warning, TEXT("OnP2PSessionConnectFail error %d (see isteamnetworking.h, k_EP2PSessionError...)"), (int)reason->m_eP2PSessionError);
		UE_LOG(UberMundoSteamLog, Verbose, TEXT("k_EP2PSessionErrorNone = 0,"));
		UE_LOG(UberMundoSteamLog, Verbose, TEXT("k_EP2PSessionErrorNotRunningApp = 1,			// target is not running the same game"));
		UE_LOG(UberMundoSteamLog, Verbose, TEXT("k_EP2PSessionErrorNoRightsToApp = 2,			// local user doesn't own the app that is running"));
		UE_LOG(UberMundoSteamLog, Verbose, TEXT("k_EP2PSessionErrorDestinationNotLoggedIn = 3,	// target user isn't connected to Steam"));
		UE_LOG(UberMundoSteamLog, Verbose, TEXT("k_EP2PSessionErrorTimeout = 4,					// target isn't responding, perhaps not calling AcceptP2PSessionWithUser()"));
		UE_LOG(UberMundoSteamLog, Verbose, TEXT("												// corporate firewalls can also block this (NAT traversal is not firewall traversal)"));
		UE_LOG(UberMundoSteamLog, Verbose, TEXT("												// make sure that UDP ports 3478, 4379, and 4380 are open in an outbound direction"));
	}

	// ---------------------------------------------------------------------- Debug Hook
	extern "C" void SteamAPIDebugTextHook(int nSeverity, const char* pchDebugText)
	{
		UE_LOG(UberMundoSteamLog, Warning, TEXT("Steam SDK: %d : %s"), nSeverity, ANSI_TO_TCHAR(pchDebugText));
	}

	// -------------------------------------------------------------------------------- USteamCustomCode
	bool USteamCustomCode::InitSteam() {
		UE_LOG(UberMundoSteamLog, Display, TEXT("Starting Steam"));

		bool ret = SteamAPI_Init();
		if (!ret) {
			UE_LOG(UberMundoSteamLog, Warning, TEXT("Steam Init failed"));
			return ret;
		}

		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("Setup P2P Callbacks"));
		Ubermundo_p2PSessionRequestCallback = new ShareSteamCallbackHooks();
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("Hooking Message Hook"));
		SteamClient()->SetWarningMessageHook(&SteamAPIDebugTextHook);
		UE_LOG(UberMundoSteamLog, Warning, TEXT("Steam Init OK"));
		return ret;
	}

	bool USteamCustomCode::ShutdownSteam() {
		UE_LOG(UberMundoSteamLog, Display, TEXT("Stop Steam"));
		SteamAPI_Shutdown();
		return true;
	}

	bool USteamCustomCode::IsSteamRunning() {
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("IsSteamRunning"));
		return SteamAPI_IsSteamRunning();
	}

	bool USteamCustomCode::IsLocalSteamID(int64 id, bool ifNoSteam) {
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("GetLocalSteamID"));
		if (!SteamAPI_IsSteamRunning() || SteamUser() == nullptr) {
			return ifNoSteam;
		}
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("GetLocalSteamID 0x%llX"), SteamUser()->GetSteamID().ConvertToUint64());
		return SteamUser()->GetSteamID().ConvertToUint64() == (uint64)id;
	}

	TArray<uint8> USteamCustomCode::GetLocalSteamID() {
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("GetLocalSteamID"));
		if (!SteamAPI_IsSteamRunning() || SteamUser() == nullptr) {
			return ToBytes(0);
		}
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("GetLocalSteamID 0x%llX"), SteamUser()->GetSteamID().ConvertToUint64());
		return ToBytes(SteamUser()->GetSteamID().ConvertToUint64());
	}

	TArray<uint8> USteamCustomCode::GetLocalSteamIDSafe(UObject* WorldContextObject) {
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("GetLocalSteamIDSafe"));

		if (!WorldContextObject)
			return ToBytes(0xFF01010100000000ULL);

		UWorld* const World = GEngine->GetWorldFromContextObject(WorldContextObject, EGetWorldErrorMode::ReturnNull);
		if (!World)
			return ToBytes(0xFF01010100000000ULL);

		if (World->WorldType == EWorldType::PIE) {

			return ToBytes(0xFF01010100000000ULL);
		}
		else {
			if (!SteamAPI_IsSteamRunning() || SteamUser() == nullptr) {
				return ToBytes(0);
			}
			UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("GetLocalSteamIDSafe 0x%llX"), SteamUser()->GetSteamID().ConvertToUint64());
			return ToBytes(SteamUser()->GetSteamID().ConvertToUint64());
		}

	}
	
	int64 USteamCustomCode::GetLocalSteamIDInt64() {
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("GetLocalSteamIDInt64"));
		if (!SteamAPI_IsSteamRunning() || SteamUser() == nullptr) {
			return 0;
		}
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("GetLocalSteamIDInt64 0x%llX"), SteamUser()->GetSteamID().ConvertToUint64());
		return (int64)SteamUser()->GetSteamID().ConvertToUint64();
	}

	int64 USteamCustomCode::GetLocalSteamIDSafeInt64(UObject* WorldContextObject) {
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("GetLocalSteamIDSafeInt64"));

		if (!WorldContextObject)
			return 0xFF01010100000000ULL;

		UWorld* const World = GEngine->GetWorldFromContextObject(WorldContextObject, EGetWorldErrorMode::ReturnNull);
		if (!World)
			return 0xFF01010100000000ULL;

		if (World->WorldType == EWorldType::PIE) {

			return 0xFF01010100000000ULL;
		}
		else {
			if (!SteamAPI_IsSteamRunning() || SteamUser() == nullptr) {
				return 0;
			}
			UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("GetLocalSteamIDSafeInt64 0x%llX"), SteamUser()->GetSteamID().ConvertToUint64());
			return (int64)SteamUser()->GetSteamID().ConvertToUint64();
		}

	}

	FString USteamCustomCode::GetRemoteSteamName(TArray<uint8> steamUserID) {
		CSteamID id(FromBytes(steamUserID));
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("GetRemoteSteamName for 0x%llX"), FromBytes(steamUserID));
		if (!SteamAPI_IsSteamRunning() || SteamFriends() == nullptr)
			return FString();
		return FString(ANSI_TO_TCHAR(SteamFriends()->GetFriendPersonaName(id)));
	}

	FString USteamCustomCode::GetLocalSteamName() {
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("GetLocalSteamName"));
		if (!SteamAPI_IsSteamRunning() || SteamFriends() == nullptr)
			return FString();
		return FString(ANSI_TO_TCHAR(SteamFriends()->GetPersonaName()));
	}

	bool USteamCustomCode::Tick() {
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("Tick"));
		if (SteamAPI_IsSteamRunning()) {
			UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("SteamAPI_RunCallbacks"));
			SteamAPI_RunCallbacks();
			return true;
		}
		return false;
	}

	bool USteamCustomCode::SendP2PPacket_UnreliableNoDelay(TArray<uint8> targetUserSteamId, TArray<uint8> bytes) {
		CSteamID id(FromBytes(targetUserSteamId));
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("SendP2PPacket_UnreliableNoDelay 0x%llX N=%d"), FromBytes(targetUserSteamId), bytes.Num());
		if (!SteamAPI_IsSteamRunning() || SteamNetworking() == nullptr)
			return false;
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("SendP2PPacket_UnreliableNoDelay 0x%llX N=%d - Sending"), FromBytes(targetUserSteamId), bytes.Num());
		return SteamNetworking()->SendP2PPacket(id, bytes.GetData(), bytes.Num(), EP2PSend::k_EP2PSendUnreliableNoDelay);
	}

	bool USteamCustomCode::SendP2PPacket_Unreliable(TArray<uint8> targetUserSteamId, TArray<uint8> bytes) {
		CSteamID id(FromBytes(targetUserSteamId));
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("SendP2PPacket_Unreliable 0x%llX N=%d"), FromBytes(targetUserSteamId), bytes.Num());
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("SendP2PPacket_ReliableWithBuffered Disected SteamID type 0x%X Universe 0x%X, Instance 0x%X, ID 0x%X"),
			(int)id.GetEAccountType(), (int)id.GetEUniverse(), (int)id.GetUnAccountInstance(), (int)id.GetAccountID());
		if (!SteamAPI_IsSteamRunning() || SteamNetworking() == nullptr)
			return false;
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("SendP2PPacket_Unreliable 0x%llX N=%d - Sending"), FromBytes(targetUserSteamId), bytes.Num());
		return SteamNetworking()->SendP2PPacket(id, bytes.GetData(), bytes.Num(), EP2PSend::k_EP2PSendUnreliable);
	}

	bool USteamCustomCode::SendP2PPacket_Reliable(TArray<uint8> targetUserSteamId, TArray<uint8> bytes) {
		CSteamID id(FromBytes(targetUserSteamId));
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("SendP2PPacket_Reliable 0x%llX N=%d"), FromBytes(targetUserSteamId), bytes.Num());
		if (!SteamAPI_IsSteamRunning() || SteamNetworking() == nullptr)
			return false;
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("SendP2PPacket_Reliable 0x%llX N=%d - Sending"), FromBytes(targetUserSteamId), bytes.Num());
		return SteamNetworking()->SendP2PPacket(id, bytes.GetData(), bytes.Num(), EP2PSend::k_EP2PSendReliable);
	}

	bool USteamCustomCode::SendP2PPacket_ReliableWithBuffered(TArray<uint8> targetUserSteamId, TArray<uint8> bytes) {
		CSteamID id(FromBytes(targetUserSteamId));
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("SendP2PPacket_ReliableWithBuffered 0x%llX N=%d"), FromBytes(targetUserSteamId), bytes.Num());
		if (!SteamAPI_IsSteamRunning() || SteamNetworking() == nullptr)
			return false;
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("SendP2PPacket_ReliableWithBuffered 0x%llX N=%d - Sending"), FromBytes(targetUserSteamId), bytes.Num());
		return SteamNetworking()->SendP2PPacket(id, bytes.GetData(), bytes.Num(), EP2PSend::k_EP2PSendReliableWithBuffering);
	}

	int USteamCustomCode::IsP2PPacketAvailable() {
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("IsP2PPacketAvailable"));
		if (!SteamAPI_IsSteamRunning() || SteamNetworking() == nullptr)
			return 0;
		uint32 N;
		if (SteamNetworking()->IsP2PPacketAvailable(&N)) {
			UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("IsP2PPacketAvailable: N=%d"), N);
			return (int)N;
		}
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("IsP2PPacketAvailable: N=0"));
		return 0;
	}

	bool USteamCustomCode::ReadP2PPacket(TArray<uint8>& bytes, TArray<uint8>& remoteSteamID) {
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("ReadP2PPacket"));
		if (!SteamAPI_IsSteamRunning() || SteamNetworking() == nullptr) {
			remoteSteamID = ToBytes(0);
			return 0;
		}
		uint32 N;
		if (!SteamNetworking()->IsP2PPacketAvailable(&N)) {
			UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("ReadP2PPacket No Data"));
			remoteSteamID = ToBytes(0);
			return false;
		}
		TArray<uint8>buf;
		buf.SetNum(N);
		uint32 N2;
		CSteamID stm;
		bool b = SteamNetworking()->ReadP2PPacket(buf.GetData(), N, &N2, &stm);
		if (b) {
			UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("ReadP2PPacket Data: N=%d From:0x%llX"), N, stm.ConvertToUint64());
			bytes = buf;
			remoteSteamID = ToBytes(stm.ConvertToUint64());
		}
		else {
			remoteSteamID = ToBytes(0);
			UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("ReadP2PPacket Data: No Data"));
		}
		return b;
	}

	bool USteamCustomCode::CloseP2PBySteamID(TArray<uint8> remoteSteamID) {
		UE_LOG(UberMundoSteamLog, VeryVerbose, TEXT("CloseP2PBySteamID Remote:0x%llX"), FromBytes(remoteSteamID));
		if (!SteamAPI_IsSteamRunning() || SteamNetworking() == nullptr)
			return 0;

		CSteamID id(FromBytes(remoteSteamID));
		return SteamNetworking()->CloseP2PSessionWithUser(id);
	}

	bool USteamCustomCode::IsValidSteamID(TArray<uint8> steamID) {
		CSteamID id(FromBytes(steamID));
		return id.IsValid();
	}

	int USteamCustomCode::GetSteamIDAccountType(TArray<uint8> steamID) {
		CSteamID id(FromBytes(steamID));
		return (int)id.GetEAccountType();
	}
	int USteamCustomCode::GetSteamIDUniverse(TArray<uint8> steamID) {
		CSteamID id(FromBytes(steamID));
		return (int)id.GetEUniverse();
	}
	int USteamCustomCode::GetSteamIDInstance(TArray<uint8> steamID) {
		CSteamID id(FromBytes(steamID));
		return (int)id.GetUnAccountInstance();
	}
	int USteamCustomCode::GetSteamIDAccountID(TArray<uint8> steamID) {
		CSteamID id(FromBytes(steamID));
		return (int)id.GetAccountID();
	}

	uint64 USteamCustomCode::FromBytes(TArray<uint8> a) {
		if (a.Num() != 8) {
			UE_LOG(UberMundoSteamLog, Error, TEXT("FromBytes Array of bytes in is not length 8."));
			return 0;
		}
		uint8* src = a.GetData();
		TArray<uint8> t;
		uint64 u = 0;
		uint8* dest = (uint8*)&u;
#ifdef VALVE_BIG_ENDIAN
		dest[0] = src[0];
		dest[1] = src[1];
		dest[2] = src[2];
		dest[3] = src[3];
		dest[4] = src[4];
		dest[5] = src[5];
		dest[6] = src[6];
		dest[7] = src[7];
#else
		dest[0] = src[7];
		dest[1] = src[6];
		dest[2] = src[5];
		dest[3] = src[4];
		dest[4] = src[3];
		dest[5] = src[2];
		dest[6] = src[1];
		dest[7] = src[0];
#endif
		return u;
	}

	TArray<uint8> USteamCustomCode::ToBytes(uint64 i) {
		TArray<uint8> t;
		t.Append((uint8*)&i, 8);
#ifndef VALVE_BIG_ENDIAN
		t.SwapMemory(0, 7);
		t.SwapMemory(1, 6);
		t.SwapMemory(2, 5);
		t.SwapMemory(3, 4);
#endif

		return t;
	}
