using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ClientMatchmaker : MonoBehaviour
{
    private static bool initialized;

    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button cancelSearchMatchButton;
    [SerializeField] private MainMenu mainMenu;

    private string currentTicketId;
    private bool isSearching;
    private bool isCancelled;

    private CancellationTokenSource cts;
    
    private void OnDestroy()
    {
        CancelAllOperations();
    }

    private void OnApplicationQuit()
    {
        CancelAllOperations();
    }

    private void CancelAllOperations()
    {
        Debug.Log("Cancelling all operations...");
        isCancelled = true;
        cts?.Cancel();
        cts?.Dispose();
        cts = null;
    }

    public async void SearchMatch()
    {
        mainMenu.EnableFindMatchPanel();
        if (!initialized)
        {
            await UnityServices.InitializeAsync();
            AuthenticationService.Instance.SwitchProfile(Random.Range(0, 1000000).ToString());
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            initialized = true;
        }
        cts = new CancellationTokenSource();
        await StartSearch(cts.Token);
    }

    public void Init()
    {
        cancelSearchMatchButton.onClick.AddListener(CancelSearchMatch);
    }

    private async void CancelSearchMatch()
    {
        mainMenu.DisableFindMatchPanel();
        if (!isSearching || string.IsNullOrEmpty(currentTicketId))
        {
            Debug.Log("No active search to cancel");
            return;
        }

        isCancelled = true;
        Debug.Log("isCancelledA:" + isCancelled);
        statusText.SetText("Cancelling match search...");

        try
        {
            await MatchmakerService.Instance.DeleteTicketAsync(currentTicketId);
            Debug.Log("Ticket cancelled successfully");
            statusText.SetText("Match search cancelled");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error cancelling ticket: " + ex.Message);
            statusText.SetText("Error cancelling search: " + ex.Message);
        }
        finally
        {
        }
    }

    private async Task StartSearch(CancellationToken ct)
    {
        if (ct.IsCancellationRequested || isSearching)
        {
            Debug.LogWarning("Search already in progress");
            return;
        }
        ResetSearchState();
        isSearching = true;
        cts = new CancellationTokenSource();
        
        var players = new List<Player>
        {
            new(AuthenticationService.Instance.PlayerId, new Dictionary<string, object>())
        };

        var attributes = new Dictionary<string, object>();
        var queueName = "test";
        var options = new CreateTicketOptions(queueName, attributes);

        try
        {
            var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);
            currentTicketId = ticketResponse.Id;
            Debug.Log("Ticket created with ID: " + ticketResponse.Id);
            
            var matchFound = await FindMatch(ticketResponse.Id);
            if (!matchFound && !isCancelled)
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
        finally
        {
            if (!isCancelled)
            {
                ResetSearchState();
            }
        }
    }

    private async Task<bool> FindMatch(string ticketId)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        for (var attempt = 0; attempt < 60 * 10; attempt++)
        {
            Debug.Log("isCancelled:" + isCancelled);
            await Awaitable.WaitForSecondsAsync(1f);
            if (isCancelled)
            {
                Debug.Log("Match search was cancelled");
                return false;
            }
            
            Debug.Log("Polling attempt: " + (attempt + 1));
            statusText.SetText("Polling attempt: " + (attempt + 1));

            try
            {
                var ticketStatusResponse = await MatchmakerService.Instance.GetTicketAsync(ticketId);
                if (ticketStatusResponse?.Value is MultiplayAssignment assignment)
                {
                    Debug.Log("Response: " + assignment.Status);

                    switch (assignment.Status)
                    {
                        case MultiplayAssignment.StatusOptions.Found:
                            if (assignment.Port.HasValue)
                            {
                                transport.SetConnectionData(assignment.Ip, (ushort)assignment.Port);
                                var result = NetworkManager.Singleton.StartClient();

                                Debug.Log("StartClient result: " + result);
                                statusText.SetText("Connecting to server...");

                                ResetSearchState();
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
    
    private void ResetSearchState()
    {
        Debug.Log("ResetSearchState");
        isSearching = false;
        isCancelled = false;
        currentTicketId = null;
        cts?.Dispose();
        cts = null;
    }
}