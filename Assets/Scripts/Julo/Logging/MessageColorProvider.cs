using UnityEngine;

namespace Julo.Logging
{
    public interface MessageColorProvider
    {

        Color GetDebugColor();
        Color GetInfoColor();
        Color GetWarningColor();
        Color GetErrorColor();

    } // interface MessageColorProvider

} // namespace Julo.Logging