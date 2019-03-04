using UnityEngine;
using UnityEngine.Networking;

namespace TurtleGame
{
    public class TurtleData
    {
        public uint netId;
        public int role;
        public int index;
        public Vector2 position;
        public Quaternion rotation;
        public Vector2 velocity;
        public float angularVelocity;

        public TurtleData(uint netId, int role, int index, Vector2 position, Quaternion rotation, Vector2 velocity, float angularVelocity)
        {
            this.netId = netId;
            this.role = role;
            this.index = index;
            this.position = position;
            this.rotation = rotation;
            this.velocity = velocity;
            this.angularVelocity = angularVelocity;
        }

    } // class TurtleData

} // namespace Turtle

