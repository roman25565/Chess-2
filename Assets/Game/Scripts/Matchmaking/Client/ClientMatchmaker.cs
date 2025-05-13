#if !UNITY_SERVER
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Setting;
using TMPro;
using UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Zenject;
using Random = UnityEngine.Random;

public class ClientMatchmaker : MonoBehaviour
{
    [Inject] private Global _global;
    [Inject] private GameData _gameData;
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

    private void Start()
    {
        _ = Initialize();
    }

    private async Task Initialize()
    {
        if (!initialized)
        {
            await UnityServices.InitializeAsync();
            AuthenticationService.Instance.SwitchProfile(Random.Range(0, 1000000).ToString());
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            initialized = true;
        }
    }

#if !UNITY_SERVER
    public async void SearchMatch(GameData gameModeSelector)
    {
        mainMenu.EnableFindMatchPanel();
        cts = new CancellationTokenSource();
        await StartSearch(cts.Token, gameModeSelector);
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
    
    private const string EloKey = "ELO";
    private const string TimeControlKey = "TimeControl";
    
    private async Task StartSearch(CancellationToken ct, GameData gameModeData)
    {
        if (ct.IsCancellationRequested || isSearching)
        {
            Debug.LogWarning("Search already in progress");
            return;
        }
        ResetSearchState();
        isSearching = true;
        cts = new CancellationTokenSource();
        var elo =  _global.FirestoreManager.PlayerData.Elo;
        var playerData = new Dictionary<string, object>
        {
            { EloKey, elo },
            { TimeControlKey, gameModeData.TimeControl}
        };
        var players = new List<Player>
        {
            new(AuthenticationService.Instance.PlayerId, playerData)
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
#endif
    public async Task<bool> FindMatch(string ticketId,UnityAction<string,ushort> action = null)
    {
        for (var attempt = 0; attempt < 60 * 10; attempt++)
        {
            Debug.Log("isCancelled:" + isCancelled);
            await Awaitable.WaitForSecondsAsync(1f);
            if (isCancelled)
            {
                Debug.Log("Match search was cancelled");
                return false;
            }
            
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
                                action?.Invoke(assignment.Ip, (ushort)assignment.Port.Value);

                                var result = ConnectToMatch(assignment.Ip, (ushort)assignment.Port.Value);

                                Debug.Log("IP " + assignment.Ip + " Port " + assignment.Port);;

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
    #region FriendMatch

    public async Task StartFriendMatch(UnityAction<string,ushort> action)
    {
        var ct = new CancellationTokenSource();
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
            new(AuthenticationService.Instance.PlayerId),
            new("132")
        };

        var attributes = new Dictionary<string, object>();
        var queueName = "Invite";
        var options = new CreateTicketOptions(queueName, attributes);

        try
        {
            var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);
            currentTicketId = ticketResponse.Id;
            Debug.Log("Ticket created with ID: " + ticketResponse.Id);
            
            var matchFound = await FindMatch(ticketResponse.Id, action);
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

    public void JoinFriendMatch(string ip, ushort port)
    {
        ConnectToMatch(ip, port);
    }
    #endregion

    public void ReConnectToMatch(string ip, ushort port)
    {
        Debug.Log("ReConnectToMatch");
        Debug.Log("ip " + ip + " port " + port);
        _gameData.Mode = GameMode.Reconnect;
        var result = ConnectToMatch(ip, port);
        Debug.Log("StartClient result: " + result);
    }
    public void ReConnectTest()
    {
        _gameData.Mode = GameMode.Reconnect;
        NetworkManager.Singleton.StartClient();
    }

    private bool ConnectToMatch(string ip, ushort port)
    {
#if !UNITY_SERVER
        try
        {

            Debug.Log("ConnectToMatch");
            Debug.Log("ip " + ip + " port " + port);
            _gameData.SetConnectionData(ip, port);
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetConnectionData(ip, port);
            return NetworkManager.Singleton.StartClient();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            throw;
        }
#endif
        throw new NotImplementedException();
    }
}
#endif