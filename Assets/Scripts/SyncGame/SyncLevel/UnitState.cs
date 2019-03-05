using UnityEngine;
using UnityEngine.Networking;

namespace SyncGame
{
    public class UnitState : MessageBase
    {
        public int role;
        public int index;
        public Vector2 position;
        public Quaternion rotation; // TODO change to float ??
        public Vector2 velocity;
        public float angularVelocity;
        public bool dead;

        public UnitState()
        {
        }

        public UnitState(
            int role,
            int index,
            Vector2 position,
            Quaternion rotation,
            Vector2 velocity,
            float angularVelocity,
            bool dead
        ) {
            this.role = role;
            this.index = index;
            this.position = position;
            this.rotation = rotation;
            this.velocity = velocity;
            this.angularVelocity = angularVelocity;
            this.dead = dead;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(role);
            writer.Write(index);
            writer.Write(position);
            writer.Write(rotation);
            writer.Write(velocity);
            writer.Write(angularVelocity);
            writer.Write(dead);
        }

        public override void Deserialize(NetworkReader reader)
        {
            role = reader.ReadInt32();
            index = reader.ReadInt32();
            position = reader.ReadVector2();
            rotation = reader.ReadQuaternion();
            velocity = reader.ReadVector2();
            angularVelocity = reader.ReadSingle();
            dead = reader.ReadBoolean();
        }

    } // class UnitState

} // namespace SyncGame

