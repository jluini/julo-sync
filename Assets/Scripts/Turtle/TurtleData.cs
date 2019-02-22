using UnityEngine;
using UnityEngine.Networking;

namespace Turtle
{
    public class TurtleData
    {
        public uint netId;
        public int role;
        public int index;
        public Vector3 position;
        public Quaternion rotation;
        public Vector2 velocity;

        public TurtleData(uint netId, int role, int index, Vector3 position, Quaternion rotation, Vector2 velocity)
        {
            this.netId = netId;
            this.role = role;
            this.index = index;
            this.position = position;
            this.rotation = rotation;
            this.velocity = velocity;
        }


        /*
        public TurtleData(Turtle t)
        {
            this.netId = t.GetComponent<NetworkIdentity>().netId.Value;
            this.role = t.role;
            this.index = t.index;
            this.position = t.transform.position;
            this.rotation = t.transform.rotation;
            this.velocity = t.transform.rotation;
        }
        */

    } // class TurtleData

} // namespace Turtle

