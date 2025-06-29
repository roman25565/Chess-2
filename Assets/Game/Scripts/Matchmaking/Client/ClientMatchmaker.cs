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
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Zenject;
using Random = UnityEngine.Random;

public class ClientMatchmaker : MonoBehaviour
{
//     [Inject] private Global _global;
//     [Inject] private GameData _gameData;
//     private static bool initialized;
//
//     [SerializeField] private TextMeshProUGUI statusText;
//     [SerializeField] private Button cancelSearchMatchButton;
//
//     private string currentTicketId;
//     private bool isSearching;
//     private bool isCancelled;
//
//     private CancellationTokenSource cts;
//     
//     private void OnDestroy()
//     {
//         CancelAllOperations();
//     }
//
//     private void OnApplicationQuit()
//     {
//         CancelAllOperations();
//     }
//
//     private void CancelAllOperations()
//     {
//         Debug.Log("Cancelling all operations...");
//         isCancelled = true;
//         cts?.Cancel();
//         cts?.Dispose();
//         cts = null;
//     }
//
//     private void Start()
//     {
//         _ = Initialize();
//     }
//
//     private async Task Initialize()
//     {
//         if (!initialized)
//         {
//             await UnityServices.InitializeAsync();
//             AuthenticationService.Instance.SwitchProfile(Random.Range(0, 1000000).ToString());
//             await AuthenticationService.Instance.SignInAnonymouslyAsync();
//             initialized = true;
//         }
//     }
//
// #if !UNITY_SERVER
//     public async void SearchMatch(GameData gameModeSelector)
//     {
//         cts = new CancellationTokenSource();
//         await StartSearch(cts.Token, gameModeSelector);
//     }
//
//     public void Init()
//     {
//         cancelSearchMatchButton.onClick.AddListener(CancelSearchMatch);
//         
//     }
//
//     
//     private const string EloKey = "ELO";
//     private const string TimeControlKey = "TimeControl";
//     
//     private async Task StartSearch(CancellationToken ct, GameData gameModeData)
//     {
//         if (ct.IsCancellationRequested || isSearching)
//         {
//             Debug.LogWarning("Search already in progress");
//             return;
//         }
//         ResetSearchState();
//         isSearching = true;
//         cts = new CancellationTokenSource();
//         var elo =  _global.FirestoreManager.PlayerData.Elo;
//         var playerData = new Dictionary<string, object>
//         {
//             { EloKey, elo },
//             { TimeControlKey, gameModeData.TimeControl}
//         };
//         
//         // var players = new List<Player>
//         // {
//         //     new(AuthenticationService.Instance.PlayerId, playerData)
//         // };
//         //
//         // var attributes = new Dictionary<string, object>();
//         // var queueName = "test";
//         // var options = new CreateTicketOptions(queueName, attributes);
//         //
//         // try
//         // {
//         //     var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options);
//         //     currentTicketId = ticketResponse.Id;
//         //     Debug.Log("Ticket created with ID: " + ticketResponse.Id);
//         //     var matchFound = await FindMatch(ticketResponse.Id);
//         //     if (!matchFound && !isCancelled)
//         //     {
//         //         Debug.LogError("Failed to find a match.");
//         //         statusText.SetText("Failed to find a match.");
//         //     }
//         // }
//         // catch (Exception ex)
//         // {
//         //     Debug.LogError("Error during matchmaking: " + ex.Message);
//         //     statusText.SetText("Matchmaking error: " + ex.Message);
//         // }
//         // finally
//         // {
//         //     if (!isCancelled)
//         //     {
//         //         ResetSearchState();
//         //     }
//         // }
//     }
//     private async void CancelSearchMatch()
//     {
//         
//     }
// #endif
//  
//     private void ResetSearchState()
//     {
//         Debug.Log("ResetSearchState");
//         isSearching = false;
//         isCancelled = false;
//         currentTicketId = null;
//         cts?.Dispose();
//         cts = null;
//     }
//     #region FriendMatch
//
//     public async Task StartFriendMatch(UnityAction<string,ushort> action)
//     {
//         var ct = new CancellationTokenSource();
//         if (ct.IsCancellationRequested || isSearching)
//         {
//             Debug.LogWarning("Search already in progress");
//             return;
//         }
//         ResetSearchState();
//         isSearching = true;
//         cts = new CancellationTokenSource();
//         //TODO
//     }
//
//     public void JoinFriendMatch(string ip, ushort port)
//     {
//         ConnectToMatch(ip, port);
//     }
//     #endregion
//
//     public void ReConnectToMatch(string relayJoinCode)
//     {
//         Debug.Log("ReConnectToMatch");
//         Debug.Log("relayJoinCode " + relayJoinCode);
//         _gameData.Mode = GameMode.Reconnect;
//         var result = ConnectToMatch(relayJoinCode);
//         Debug.Log("StartClient result: " + result);
//     }
//     public void ReConnectTest()
//     {
//         _gameData.Mode = GameMode.Reconnect;
//         NetworkManager.Singleton.StartClient();
//     }
//
//     private bool ConnectToMatch(string ip, ushort port)
//     {
// #if !UNITY_SERVER
//         try
//         {
//
//             Debug.Log("ConnectToMatch");
//             Debug.Log("ip " + ip + " port " + port);
//             _gameData.SetConnectionData(ip, port);
//             NetworkManager.Singleton.GetComponent<UnityTransport>()
//                 .SetConnectionData(ip, port);
//             return NetworkManager.Singleton.StartClient();
//         }
//         catch (Exception e)
//         {
//             Debug.LogError(e);
//             throw;
//         }
// #endif
//         throw new NotImplementedException();
//     }
}
#endif