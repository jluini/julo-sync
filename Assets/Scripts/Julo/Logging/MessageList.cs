using System.Collections;
using System.Collections.Generic;
using System.Text;

using UnityEngine;
using UnityEngine.UI;

namespace Julo.Logging
{
    
    public class MessageList : MonoBehaviour
    {
        //public MessageElement messageElementModel;
        //public Transform container;

        public Text textDisplay;
        public ScrollRect scroll;

        List<Message> messages = new List<Message>();

        public void AddMessage(Message message)
        {
            messages.Add(message);
            Redraw();
            ScrollToBottom();
        }

        void ScrollToBottom()
        {
            StartCoroutine("ScrollToBottomDelayed");
        }

        void Redraw()
        {
            var sb = new StringBuilder();
            bool first = true;

            foreach(Message msg in messages)
            {
                if(!first)
                {
                    sb.Append("\n");
                }
                first = false;

                AppendMessage(sb, msg);
            }

            textDisplay.text = sb.ToString();
        }

        void AppendMessage(StringBuilder builder, Message msg)
        {
            Color color = msg.GetColor();
            string text = msg.GetText();

            builder.Append("<color=#");
            builder.Append(ColorUtility.ToHtmlStringRGBA(color));
            builder.Append(">");
            //AppendEscaped(builder, name);
            //builder.Append("</b> says> ");
            AppendEscaped(builder, text);
            builder.Append("</color>");
        }

        static void AppendEscaped(StringBuilder builder, string original)
        {
            for(int i = 0; i < original.Length; i++)
            {
                if(original[i] == '<')
                    builder.Append("&lt;");
                else if(original[i] == '>')
                    builder.Append("&gt;");
                else
                    builder.Append(original[i]);
            }
        }

        IEnumerator ScrollToBottomDelayed()
        {
            // wait a frame
            yield return null;

            // scroll to bottom
            scroll.normalizedPosition = new Vector2(0f, 0f);

            yield break;
        }


    } // class MessageList

} // namespace Julo.Logging