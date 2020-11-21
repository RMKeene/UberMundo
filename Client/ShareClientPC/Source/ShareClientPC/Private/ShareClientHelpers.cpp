// Copyright 2020 Bahnda. All rights reserved.


#include "ShareClientHelpers.h"

void UShareClientHelpers::GetGuidBytes(FGuid guid, uint8& B0, uint8& B1, uint8& B2, uint8& B3,
	uint8& B4, uint8& B5, uint8& B6, uint8& B7,
	uint8& B8, uint8& B9, uint8& B10, uint8& B11,
	uint8& B12, uint8& B13, uint8& B14, uint8& B15) {
	B0 = guid.A & 0x00FF;
	B1 = (guid.A >> 8) & 0x00FF;
	B2 = (guid.A >> 16) & 0x00FF;
	B3 = (guid.A >> 24) & 0x00FF;
	B4 = guid.B & 0x00FF;
	B5 = (guid.B >> 8) & 0x00FF;
	B6 = (guid.B >> 16) & 0x00FF;
	B7 = (guid.B >> 24) & 0x00FF;
	B8 = guid.C & 0x00FF;
	B9 = (guid.C >> 8) & 0x00FF;
	B10 = (guid.C >> 16) & 0x00FF;
	B11 = (guid.C >> 24) & 0x00FF;
	B12 = guid.D & 0x00FF;
	B13 = (guid.D >> 8) & 0x00FF;
	B14 = (guid.D >> 16) & 0x00FF;
	B15 = (guid.D >> 24) & 0x00FF;
}

FGuid UShareClientHelpers::GuidFromBytes(uint8 B0, uint8 B1, uint8 B2, uint8 B3,
	uint8 B4, uint8 B5, uint8 B6, uint8 B7,
	uint8 B8, uint8 B9, uint8 B10, uint8 B11,
	uint8 B12, uint8 B13, uint8 B14, uint8 B15) {
	return FGuid(B0 | ((uint32)B1 << 8) | ((uint32)B2 << 16) | ((uint32)B3 << 24),
		(uint32)B4 | ((uint32)B5 << 8) | ((uint32)B6 << 16) | ((uint32)B7 << 24),
		(uint32)B8 | ((uint32)B9 << 8) | ((uint32)B10 << 16) | ((uint32)B11 << 24),
		(uint32)B12 | ((uint32)B13 << 8) | ((uint32)B14 << 16) | ((uint32)B15 << 24));
}

void UShareClientHelpers::GetLongBytes(int64 i,
	uint8& B0, uint8& B1, uint8& B2, uint8& B3,
	uint8& B4, uint8& B5, uint8& B6, uint8& B7) {
	B0 = (i >> 56) & 0x00FF;
	B1 = (i >> 48) & 0x00FF;
	B2 = (i >> 40) & 0x00FF;
	B3 = (i >> 32) & 0x00FF;;
	B4 = (i >> 24) & 0x00FF;
	B5 = (i >> 16) & 0x00FF;
	B6 = (i >> 8) & 0x00FF;
	B7 = i & 0x00FF;
}

int64 UShareClientHelpers::LongFromBytes(uint8 B0,
	uint8 B1, uint8 B2, uint8 B3,
	uint8 B4, uint8 B5, uint8 B6, uint8 B7) {
	return ((int64)B7) | ((int64)B6 << 8) | ((int64)B5 << 16) | ((int64)B4 << 24),
		((int64)B3 << 32) | ((int64)B2 << 40) | ((int64)B1 << 48) | ((int64)B0 << 56);

}

FString UShareClientHelpers::GetGameSetting(FString key, FString defaultReturn) {
	FString s;
	if (GConfig->GetString(TEXT("ShareSettings"), *key, s, GGameIni))
		return s;
	return defaultReturn;
}

FString UShareClientHelpers::Int64ToHex(int64 i) {
	return FString::Printf(TEXT("%llX"), i);
}
