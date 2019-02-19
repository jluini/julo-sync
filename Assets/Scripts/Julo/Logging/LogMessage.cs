using UnityEngine;

namespace Julo.Logging
{

    public abstract class LogMessage : Message
    {

        string text;
        //Color color;
        protected MessageColorProvider colorProvider;

        public LogMessage(string text, MessageColorProvider colorProvider)
        {
            this.text = text;
            //this.color = color;
            this.colorProvider = colorProvider;
        }

        public string GetText()
        {
            return text;
        }

        public abstract Color GetColor();

    } // class LogMessage

    public class DebugMessage : LogMessage
    {
        public DebugMessage(string text, MessageColorProvider colorProvider) : base(text, colorProvider)
        {
            // noop
        }

        public override Color GetColor()
        {
            return colorProvider.GetDebugColor();
        }
    } // class DebugMessage

    public class InfoMessage : LogMessage
    {
        public InfoMessage(string text, MessageColorProvider colorProvider) : base(text, colorProvider)
        {
            // noop
        }

        public override Color GetColor()
        {
            return colorProvider.GetInfoColor();
        }
    } // class InfoMessage

    public class WarningMessage : LogMessage
    {
        public WarningMessage(string text, MessageColorProvider colorProvider) : base(text, colorProvider)
        {
            // noop
        }

        public override Color GetColor()
        {
            return colorProvider.GetWarningColor();
        }
    } // class WarningMessage

    public class ErrorMessage : LogMessage
    {
        public ErrorMessage(string text, MessageColorProvider colorProvider) : base(text, colorProvider)
        {
            // noop
        }

        public override Color GetColor()
        {
            return colorProvider.GetErrorColor();
        }
    } // class ErrorMessage

} // namespace Julo.Logging