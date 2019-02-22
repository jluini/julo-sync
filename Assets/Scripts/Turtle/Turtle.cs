using UnityEngine;
using UnityEngine.Networking;

using Julo.Logging;

namespace Turtle
{

    public class Turtle : MonoBehaviour
    {

        public int role = 0;
        public int index = 0;

        public static Color[] playerColors =
        {
            Color.blue,
            Color.red
        };

        public static Color playingColor = Color.yellow;

        Rigidbody2D rb = null;
        SpriteRenderer renderer;

        void Start()
        {
            /*/ if it client-only registering here won't work (no data yet)
            if(TurtleClient.instance != null)
            {
                TurtleClient.instance.RegisterTurtle(this);
            }
            else
            {
                Log.Error("TurtleClient not found");
            }*/

            if(TurtleServer.instance)
            {
                var ni = GetComponent<NetworkIdentity>();
                uint netId = ni == null ? 10000 : ni.netId.Value;
                TurtleServer.instance.RegisterInServer(this);
            }
            else
            {
                var ni = GetComponent<NetworkIdentity>();
                uint netId = ni == null ? 10000 : ni.netId.Value;
                Log.Debug("In remote host my netId on Start() is: {0}", netId);
            }

            rb = GetComponent<Rigidbody2D>();
            renderer = GetComponent<SpriteRenderer>();

            if(role > 0)
            {
                SetMyColor();
            }
            else
            {
                SetColor(Color.grey);
            }
        }

        public TurtleData GetState()
        {
            return new TurtleData(
                GetComponent<NetworkIdentity>().netId.Value,
                role,
                index,
                transform.position,
                transform.rotation,
                rb.velocity
            );

        }

        public void SetBasicData(int role, int index)
        {
            if(role < 1 || index < 1)
            {
                Log.Error("Invalid basic data: {0}, {1}", role, index);
                return;
            }

            if(this.role != 0)
            {
                if(this.role != role)
                {
                    Log.Error("I had another role!!");
                }
                else
                {
                    // Log.Debug("Already know that");
                }
                return;
            }

            this.role = role;
            this.index = index;

            SetMyColor();
        }

        void SetMyColor()
        {
            if(playerColors.Length < role)
            {
                Log.Warn("No color for role {0}", this.role);
                return;
            }

            SetColor(playerColors[role - 1]);
        }

        public void SetState(TurtleData d)
        {
            // TODO only needed first time in non-hosted clients
            //role = d.role;
            //index = d.index;

            transform.position = d.position;
            transform.rotation = d.rotation;
            rb.velocity = d.velocity;
        }
        public void AddTorque(float value)
        {
            rb.AddTorque(value);
        }

        public void SetPlaying(bool isPlaying)
        {
            if(isPlaying)
            {
                SetColor(playingColor);
            }
            else
            {
                SetMyColor();
            }
        }

        public void SetColor(Color color)
        {
            renderer.color = color;
        }

        // ...

    } // class Turtle

} // namespace Turtle
