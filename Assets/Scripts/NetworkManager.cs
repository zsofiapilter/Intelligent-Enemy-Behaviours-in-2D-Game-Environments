using Mirror;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Transform start = GetStartPosition();

        if (start == null)
        {
            Debug.LogError("No start position found! Add a NetworkStartPosition to the scene.");
            return;
        }

        GameObject player = Instantiate(playerPrefab, start.position, Quaternion.identity);
        NetworkServer.AddPlayerForConnection(conn, player);
    }

}
