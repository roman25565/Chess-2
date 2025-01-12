using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class ClientMatchmaker : MonoBehaviour
{
    static bool initialized;

    [SerializeField] private TextMeshProUGUI statusText;
    
    public async void SearchMatch()
    {
        if (!initialized)
        {
            await UnityServices.InitializeAsync();
            AuthenticationService.Instance.SwitchProfile(UnityEngine.Random.Range(0, 1000000).ToString());
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            initialized = true;
        }

        await StartSearch();
    }

    async Task StartSearch()
    {
        var players = new List<Player>
        {
            new(AuthenticationService.Instance.PlayerId, new Dictionary<string, object>())
        };

        var attributes = new Dictionary<string, object>();
        string queueName = "test";
        var options = new CreateTicketOptions(queueName, attributes);

        while (!await FindMatch(players, options)) // if we dont find a match, wait a second and try again
            await Awaitable.WaitForSecondsAsync(1f);
    }

    async Task<bool> FindMatch(List<Player> players, CreateTicketOptions options)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);

        while (true)
        {
            await Awaitable.WaitForSecondsAsync(1f);
            Debug.Log("Polling");
            var ticketStatusResponse = await MatchmakerService.Instance.GetTicketAsync(ticketResponse.Id);
            if (ticketStatusResponse?.Value is MultiplayAssignment assignment)
            {
                Debug.Log("Response " + assignment.Status);
                statusText.SetText("Response " + assignment.Status);
                switch (assignment.Status)
                {
                    case MultiplayAssignment.StatusOptions.Found:
                    {
                        if (assignment.Port.HasValue)
                        {
                            transport.SetConnectionData(assignment.Ip, (ushort) assignment.Port);
                            bool result = NetworkManager.Singleton.StartClient();
                            
                            // Logging and showing on UI
                            Debug.Log("StartClient " + result);
                            statusText.SetText("StartClient " + result);
                            NetworkManager.Singleton.OnConnectionEvent += LogConnectionEvent;

                            return result; // if we fail to connect try again w/ a false result
                        }

                        Debug.LogError("No port found");
                        return false;
                    }
                    case MultiplayAssignment.StatusOptions.Timeout:
                    case MultiplayAssignment.StatusOptions.Failed:
                    {
                        Debug.LogError(assignment.ToString());
                        return false;
                    }
                }
            }
        }
    }

    void LogConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        switch (data.EventType)
        {
            case ConnectionEvent.ClientConnected:
                statusText.SetText("Client connected " + data.ClientId +
                                                          " Count:" +
                                                          NetworkManager.Singleton.ConnectedClientsIds.Count + " Port:" + 
                                                          (manager.NetworkConfig.NetworkTransport as UnityTransport)?.ConnectionData.Port);
                break;
            case ConnectionEvent.ClientDisconnected:
                statusText
                    .SetText("Client disconnected " + data.ClientId + " Count:" +
                             NetworkManager.Singleton.ConnectedClientsIds.Count + " Port:" + 
                             (manager.NetworkConfig.NetworkTransport as UnityTransport)?.ConnectionData.Port);
                break;
        }
    }
}