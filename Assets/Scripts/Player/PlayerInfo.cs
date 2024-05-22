using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct PlayerInfo : INetworkSerializable, IEquatable<PlayerInfo>
{ //struct para guardar informacion sobre cada jugador

    public ulong ID;
    public FixedString64Bytes Name;
    public Player.PlayerColor Color;

    public PlayerInfo(ulong id, FixedString64Bytes playerName, Player.PlayerColor color)
    {
        ID = id;
        Name = playerName;
        Color = color;
    }
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ID);
        serializer.SerializeValue(ref Name);
        serializer.SerializeValue(ref Color);
    }

    public bool Equals(PlayerInfo other) => ID == other.ID && Name.Equals(other.Name) && Color == other.Color;

    public override bool Equals(object obj) => obj is PlayerInfo other && Equals(other);

    public override int GetHashCode() =>HashCode.Combine(ID, Name, (int)Color);
    
}
