using System.Collections.Generic;

using UnityEngine.Networking;

namespace Julo.Network
{
    /// <summary>
    /// Used on the server to keep track of client connections.
    /// In offline mode there is a single Client with null NetworkConnection.
    /// In online mode there are multiple clients (including the local one) referencing the corresponding NetworkConnection.
    /// </summary>

    public class Client
    {

        public NetworkConnection connection;
        public List<DNMPlayer> players;
        //public bool readyToStart;

        public DualNetworkManager.GameState stateInServer;

        public Client(NetworkConnection conn)
        {
            connection = conn;
            players = new List<DNMPlayer>();
            //readyToStart = false;
            stateInServer = DualNetworkManager.GameState.Lobby;
        }

        public void AddPlayer(DNMPlayer player)
        {
            players.Add(player);
        }

        public bool ConnectionIsReady()
        {
            return connection.isReady;
        }
        /*
        public bool IsReadyToStart()
        {
            return readyToStart;
        }

        public void SetReadyToStart(bool ready)
        {
            readyToStart = ready;
        }
        */

    } // class Client

} // namespace Julo.Network