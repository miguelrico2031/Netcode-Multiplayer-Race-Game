using Unity.Netcode;
using UnityEngine;

public struct InputPayload : INetworkSerializable
{
    public int Tick;
    public float Acceleration, Steering, Brake;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref Acceleration);
        serializer.SerializeValue(ref Steering);
        serializer.SerializeValue(ref Brake);
    }
}

public struct StatePayload : INetworkSerializable
{
    public int Tick;
    public Vector3 Position, Velocity, AngularVelocity;
    public Quaternion Rotation;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref Velocity);
        serializer.SerializeValue(ref AngularVelocity);
        serializer.SerializeValue(ref Rotation);
    }
    
    public bool IsDefault()
    {
        // Debug.Log($"Default check:\ntick:{Tick == 0}, pos:{Position == Vector3.zero}, rot: {Rotation == new Quaternion(0, 0, 0,0)}, " +
        //           $"vel: {Velocity == Vector3.zero}, ang: {AngularVelocity == Vector3.zero}");
        return Tick == 0 &&
               Position == Vector3.zero &&
               Velocity == Vector3.zero &&
               AngularVelocity == Vector3.zero;
    }
    
    
    public override string ToString() =>
        $"StatePlayload: [Tick: {Tick}, Pos: {Position}, Rot: {Rotation}, Vel:{Velocity}, AngVel: {AngularVelocity}]";
}
