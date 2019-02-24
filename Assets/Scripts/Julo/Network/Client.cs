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

        public DualNetworkManager.GameState stateInServer;

        public Client(NetworkConnection conn)
        {
            connection = conn;
            players = new List<DNMPlayer>();
            stateInServer = DualNetworkManager.GameState.NoGame;
        }

        public void AddPlayer(DNMPlayer player)
        {
            players.Add(player);
        }

        public bool ConnectionIsReady()
        {
            return connection.isReady;
        }

    } // class Client

} // namespace Julo.Network