using UnityEngine;

namespace Julo.Logging
{

    public interface Message
    {

        string GetText();

        Color GetColor();

        // TODO timestamp? frameCount?

    } // interface Message

} // namespace Julo.Logging