using UnityEngine;
using UnityEngine.Networking;

using Julo.Logging;

namespace Turtle
{
    
    public class Turtle : MonoBehaviour
    {

        public int role = 0;
        public int index = 0;

        public static Color defaultColor = Color.blue;
        public static Color playingColor = Color.red;

        Rigidbody2D rb = null;
        SpriteRenderer renderer;

        void Start()
        {
            var ni = GetComponent<NetworkIdentity>();
            Log.Debug("Turtle::Start({0}, {1})", TurtleClient.instance != null, ni != null ? ni.netId.Value.ToString() : "no netId");
            Log.Debug("$$$$ Can register turtle in client {0}", TurtleClient.instance);

            /* if(TurtleClient.instance != null)
            {
                TurtleClient.instance.RegisterTurtle(this);
            }
            else
            {
                Log.Error("TurtleClient not found");
            } */

            rb = GetComponent<Rigidbody2D>();
            renderer = GetComponent<SpriteRenderer>();
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
                SetColor(defaultColor);
            }
        }

        public void SetColor(Color color)
        {
            renderer.color = color;
        }

        // ...

    } // class Turtle

} // namespace Turtle
