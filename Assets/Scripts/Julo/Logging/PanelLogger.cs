using UnityEngine;

namespace Julo.Logging
{
    /*
     * TODO add severity levels
     */
    public class PanelLogger : MonoBehaviour, Logger, MessageColorProvider
    {
        public bool showInConsole = true;

        public MessageList messageList;

        public Color debugColor = Color.gray;
        public Color infoColor = Color.green;
        public Color warningColor = Color.yellow;
        public Color errorColor = Color.red;

        void OnEnable()
        {
            Log.SetLogger(this);
        }

        public void Debug(string messageBody, params object[] args)
        {
            string message = System.String.Format(messageBody, args);

            messageList.AddMessage(new DebugMessage(message, this));
            if(showInConsole)
            {
                UnityEngine.Debug.Log(Utils.Escaped(message));
            }
        }
        public void Info(string messageBody, params object[] args)
        {
            string message = System.String.Format(messageBody, args);

            messageList.AddMessage(new InfoMessage(message, this));
            if(showInConsole)
            {
                UnityEngine.Debug.Log(Utils.Escaped(message));
            }
        }
        public void Warn(string messageBody, params object[] args)
        {
            string message = System.String.Format(messageBody, args);

            messageList.AddMessage(new WarningMessage(message, this));
            if(showInConsole)
            {
                UnityEngine.Debug.LogWarning(Utils.Escaped(message));
            }
        }
        public void Error(string messageBody, params object[] args)
        {
            string message = System.String.Format(messageBody, args);

            messageList.AddMessage(new ErrorMessage(message, this));
            if(showInConsole)
            {
                UnityEngine.Debug.LogError(Utils.Escaped(message));
            }
        }

        // Color provider

        public Color GetDebugColor()
        {
            return debugColor;
        }
        public Color GetInfoColor()
        {
            return infoColor;
        }
        public Color GetWarningColor()
        {
            return warningColor;
        }
        public Color GetErrorColor()
        {
            return errorColor;
        }

    } // class PanelLogger

} // namespace Julo.Logging
