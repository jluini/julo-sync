using UnityEngine;

namespace Julo.Logging
{

    public class SimpleMessage : Message
    {

        string text;
        Color color;

        public SimpleMessage(string text, Color color)
        {
            this.text = text;
            this.color = color;
        }

        public string GetText()
        {
            return text;
        }

        public Color GetColor()
        {
            return color;


        }

    } // class SimpleMessage

} // namespace Julo.Logging