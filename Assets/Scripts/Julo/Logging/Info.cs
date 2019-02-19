using System.Collections.Generic;

using UnityEngine;

namespace Julo.Logging
{
    public class Info
    {

        static List<InfoDisplay> _displays = new List<InfoDisplay>();

        public static void AddInfoDisplay(InfoDisplay display)
        {
            _displays.Add(display);
        }

        public static void Set(string key, string value)
        {
            foreach(InfoDisplay display in _displays)
            {
                display.Set(key, value);
            }
        }

    } // class Info

} // namespace Julo.Logging
