using Julo.Panels;
using Julo.Network;

namespace Menu
{

    public class MainMenuPanel : Panel {


        public GameManager manager;

        public void OnClickStartOffline()
        {
            manager.StartOffline();
        }

        public void OnClickStartOnline()
        {
            manager.StartOnline();
        }

        public void OnClickLANHost()
        {
            manager.StartLANHost();
        }

        public void OnClickLANJoin()
        {
            manager.StartLANJoin();
        }

    } // class MainMenuPanel

} // namespace Menu
