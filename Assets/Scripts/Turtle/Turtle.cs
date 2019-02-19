using UnityEngine;

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
            Log.Debug("Registering turtle in client");

            if(TurtleClient.instance != null)
                TurtleClient.instance.RegisterTurtle(this);

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
