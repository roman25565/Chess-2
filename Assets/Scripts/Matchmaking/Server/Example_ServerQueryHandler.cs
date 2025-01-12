using Unity.Netcode;
using Unity.Services.Multiplay;
using UnityEngine;

/// <summary>
/// An example of how to use SQP from the server using the Multiplay SDK.
/// The ServerQueryHandler reports the given information to the Multiplay Service.
/// </summary>
public class Example_ServerQueryHandler : MonoBehaviour
{
    const ushort k_DefaultMaxPlayers = 2;
    const string k_DefaultServerName = "MyServerExample";
    const string k_DefaultGameType = "MyGameType";
    const string k_DefaultBuildId = "test2";
    const string k_DefaultMap = "MyMap";

    IServerQueryHandler m_ServerQueryHandler;

    async void Start()
    {
        while (MultiplayService.Instance == null)
        {
            await Awaitable.NextFrameAsync();
        }

        m_ServerQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(
            k_DefaultMaxPlayers, k_DefaultServerName, k_DefaultGameType, k_DefaultBuildId, k_DefaultMap);
    }

    void Update()
    {
        if (m_ServerQueryHandler != null)
        {
            if (NetworkManager.Singleton.ConnectedClients.Count != m_ServerQueryHandler.CurrentPlayers)
                m_ServerQueryHandler.CurrentPlayers = (ushort)NetworkManager.Singleton.ConnectedClients.Count;

            m_ServerQueryHandler.UpdateServerCheck();
        }
    }

    public void ChangeQueryResponseValues(ushort maxPlayers, string serverName, string gameType, string buildId)
    {
        m_ServerQueryHandler.MaxPlayers = maxPlayers;
        m_ServerQueryHandler.ServerName = serverName;
        m_ServerQueryHandler.GameType = gameType;
        m_ServerQueryHandler.BuildId = buildId;
    }

    public void PlayerCountChanged(ushort newPlayerCount)
    {
        m_ServerQueryHandler.CurrentPlayers = newPlayerCount;
    }
}