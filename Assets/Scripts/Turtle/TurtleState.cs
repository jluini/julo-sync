using UnityEngine;
using UnityEngine.Networking;

namespace Turtle
{
    public class TurtleState : MessageBase
    {
        public uint netId;
        public int role;
        public int index;
        public Vector2 position;
        public Quaternion rotation;
        public Vector2 velocity;
        public float angularVelocity;

        public TurtleState()
        {
        }

        public TurtleState(uint netId, int role, int index, Vector2 position, Quaternion rotation, Vector2 velocity, float angularVelocity)
        {
            this.netId = netId;
            this.role = role;
            this.index = index;
            this.position = position;
            this.rotation = rotation;
            this.velocity = velocity;
            this.angularVelocity = angularVelocity;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(netId);
            writer.Write(role);
            writer.Write(index);
            writer.Write(position);
            writer.Write(rotation);
            writer.Write(velocity);
            writer.Write(angularVelocity);
        }

        public override void Deserialize(NetworkReader reader)
        {
            netId = reader.ReadUInt32();
            role = reader.ReadInt32();
            index = reader.ReadInt32();
            position = reader.ReadVector2();
            rotation = reader.ReadQuaternion();
            velocity = reader.ReadVector2();
            angularVelocity = reader.ReadSingle();
        }

    } // class TurtleState

} // namespace Turtle

