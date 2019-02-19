namespace Julo.Logging
{
    public interface Logger
    {

        void Debug(string message, params object[] args);
        void Info(string message, params object[] args);
        void Warn(string message, params object[] args);
        void Error(string message, params object[] args);

    } // interface Logger

} // namespace Julo.Logging
