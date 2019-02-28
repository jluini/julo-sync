using System;

using UnityEngine;

using Julo.Logging;

namespace Turtle
{
    public class Turtle : MonoBehaviour
    {
        public int role = 0;
        public int index = 0;
        public bool dead = false;

        public DateTime lastUse;

        public static Color[] playerColors =
        {
            Color.blue,
            Color.red
        };

        public static Color playingColor = Color.yellow;
        public static Color deadColor = Color.gray;

        Rigidbody2D _rb = null;
        Rigidbody2D rb {
            get
            {
                if(_rb == null)
                {
                    _rb = GetComponent<Rigidbody2D>();
                }
                return _rb;
            }
        }

        SpriteRenderer _renderer = null;
        new SpriteRenderer renderer
        {
            get
            {
                if(_renderer == null)
                {
                    _renderer = GetComponent<SpriteRenderer>();
                }
                return _renderer;
            }
        }

        void Start()
        {
            SetMyColor();
        }

        public TurtleState GetState()
        {
            return new TurtleState(
                role,
                index,
                transform.position,
                transform.rotation,
                rb.velocity,
                rb.angularVelocity,
                dead
            );
        }

        public void SetState(TurtleState d)
        {
            transform.position = d.position;
            transform.rotation = d.rotation;
            rb.velocity = d.velocity;
            rb.angularVelocity = d.angularVelocity;
            
            SetDead(d.dead);
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
            lastUse = DateTime.Now;
        }

        public void SetColor(Color color)
        {
            if(renderer)
                renderer.color = color;
            else
                Log.Warn("No renderer");
        }

        public void SetDead(bool dead)
        {
            if(this.dead != dead)
            {
                this.dead = dead;
                if(dead)
                {
                    SetColor(deadColor);
                }
            }
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

    } // class Turtle

} // namespace Turtle
