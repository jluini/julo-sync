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

        public TurtleData()
        {

        }

        public TurtleData(Turtle t)
        {
            this.netId = t.GetComponent<NetworkIdentity>().netId.Value;
            this.role = t.role;
            this.index = t.index;
            this.position = t.transform.position;
            this.rotation = t.transform.rotation;
        }

    } // class TurtleData

} // namespace Turtle

