using System;
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
using Random = UnityEngine.Random;

public class ClientMatchmaker : MonoBehaviour
{
    private static bool initialized;

    [SerializeField] private TextMeshProUGUI statusText;

    public async void SearchMatch()
    {
        if (!initialized)
        {
            await UnityServices.InitializeAsync();
            AuthenticationService.Instance.SwitchProfile(Random.Range(0, 1000000).ToString());
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            initialized = true;
        }

        await StartSearch();
    }

    private async Task StartSearch()
    {
        var players = new List<Player>
        {
            new(AuthenticationService.Instance.PlayerId, new Dictionary<string, object>())
        };

        var attributes = new Dictionary<string, object>();
        var queueName = "test";
        var options = new CreateTicketOptions(queueName, attributes);

        try
        {
            // Create a ticket and start polling
            var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);
            Debug.Log("Ticket created with ID: " + ticketResponse.Id);
            statusText.SetText("Ticket created. Searching for match...");

            var matchFound = await FindMatch(ticketResponse.Id);
            if (!matchFound)
            {
                Debug.LogError("Failed to find a match.");
                statusText.SetText("Failed to find a match.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error during matchmaking: " + ex.Message);
            statusText.SetText("Matchmaking error: " + ex.Message);
        }
    }

    private async Task<bool> FindMatch(string ticketId)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        for (var attempt = 0; attempt < 60 * 10; attempt++)
        {
            await Awaitable.WaitForSecondsAsync(1f);
            Debug.Log("Polling attempt: " + (attempt + 1));
            statusText.SetText("Polling attempt: " + (attempt + 1));

            try
            {
                var ticketStatusResponse = await MatchmakerService.Instance.GetTicketAsync(ticketId);
                if (ticketStatusResponse?.Value is MultiplayAssignment assignment)
                {
                    Debug.Log("Response: " + assignment.Status);
                    statusText.SetText("Match status: " + assignment.Status);

                    switch (assignment.Status)
                    {
                        case MultiplayAssignment.StatusOptions.Found:
                            if (assignment.Port.HasValue)
                            {
                                transport.SetConnectionData(assignment.Ip, (ushort)assignment.Port);
                                var result = NetworkManager.Singleton.StartClient();

                                Debug.Log("StartClient result: " + result);
                                statusText.SetText("Connecting to server...");

                                NetworkManager.Singleton.OnConnectionEvent += LogConnectionEvent;
                                return result; // Successfully connected
                            }

                            Debug.LogError("No port found in assignment.");
                            statusText.SetText("Error: No port found.");
                            return false;

                        case MultiplayAssignment.StatusOptions.Timeout:
                        case MultiplayAssignment.StatusOptions.Failed:
                            Debug.LogError("Matchmaking failed: " + assignment.Status);
                            statusText.SetText("Matchmaking failed: " + assignment.Status);
                            return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error polling ticket status: " + ex.Message);
                statusText.SetText("Error: " + ex.Message);
            }
        }

        Debug.LogError("Max polling attempts reached. No match found.");
        statusText.SetText("Matchmaking timed out.");
        return false;
    }


    private void LogConnectionEvent(NetworkManager manager, ConnectionEventData data)
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