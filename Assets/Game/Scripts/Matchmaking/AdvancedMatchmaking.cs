
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Board;
using Bootstrap;
using Google;
using Setting;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public enum MatchmakingState
{
    Finding,
    Cancelled,
}
public class AdvancedMatchmaking : MonoBehaviour
{
    [Inject] private Global _global;
    [Inject] private GameData _gameData;

    [SerializeField] private UnityTransport transport;
    [SerializeField] private TextMeshProUGUI searchTimeText;

    private float _searchTime;
    private bool _isSearching;
    private bool _initialized;
    
    private MatchmakingState _state;
    public UnityEvent<MatchmakingState> onStateChanged = new();

    public async Task Init()
    {
        if (!_initialized)
        {
            _gameData.Matchmaking = this;
            transport = FindAnyObjectByType<UnityTransport>();
            await UnityServices.InitializeAsync();
            AuthenticationService.Instance.SwitchProfile(Random.Range(0, 1000000).ToString());
            _initialized = true;
        }
    }
    
    public async Task OnSignIn(string userIdToken, SignTypes anonymous)
    {
        Debug.Log("OnSignIn " + userIdToken + " " + anonymous);;
        try
        {
            if (anonymous == SignTypes.Google)
            {
                await AuthenticationService.Instance.SignInWithGoogleAsync(userIdToken);
            }
            else
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Console.WriteLine(e);
            throw;
        }
    }

    #region Lobby
    
    private Lobby _connectedLobby;
    private const int MaxPlayers = 2;
    private const string JoinCodeKey = "j";
    private const string EloKey = "elo";
    private const string TimeControlKey = "timeControl";

    private Coroutine _searchTimerCoroutine;
    private Coroutine _heartbeatCoroutine;

    private bool IsHost => _connectedLobby.HostId == AuthenticationService.Instance.PlayerId;


    public void SearchMatch(GameData gameData)
    {
        _global.FirestoreManager.MyData.GetPlayerRanking(rankingData =>
        {
            var timeControl = gameData.TimeControl;
            StartMatchmaking(rankingData.Elo, timeControl.ToString());
        });
    }

    private async void StartMatchmaking(int playerElo, string timeControl)
    {
        if (_isSearching) CancelMatchmaking();
        
        onStateChanged.Invoke(MatchmakingState.Finding);
        _isSearching = true;
        _searchTime = 0f;
        _searchTimerCoroutine = StartCoroutine(UpdateSearchTimer());

        try
        {
            _connectedLobby = await QuickJoinLobby(playerElo, timeControl) ??
                              await CreateLobby(playerElo, timeControl);

            if (_connectedLobby == null)
                return;

            SubscribeToLobbyEvents(_connectedLobby);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async Task SubscribeToLobbyEvents(Lobby lobby)
    {
        try
        {
            Debug.Log("subscribed to lobby" + lobby.HostId + " " + lobby.AvailableSlots);
            var callbacks = new LobbyEventCallbacks();
            callbacks.LobbyChanged += OnLobbyChanged;
            callbacks.LobbyEventConnectionStateChanged += OnLobbyEventConnectionStateChanged;
            callbacks.PlayerJoined += OnPlayerJoined;
            callbacks.LobbyDeleted += OnLobbyDeleted;
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, callbacks);
            Debug.Log("Successfully subscribed to lobby eventss.");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            Debug.Log(e);
            throw;
        }

        return;

        async void OnLobbyChanged(ILobbyChanges changes)
        {
            if (IsHost) return;
            Debug.Log($"[LobbyChanged] AvailableSlots: {changes.AvailableSlots.Value}");

            if (changes.Data.Value != null && changes.Data.Value.TryGetValue(JoinCodeKey, out var relayCodeUpdate))
            {
                string relayCode = relayCodeUpdate.Value.Value;
                await ConnectToMatch(relayCode);
            }
        }

        void OnPlayerJoined(List<LobbyPlayerJoined> players)
        {
            Debug.Log($"Player joined: {players[0].Player.Id}");
            Debug.Log("players.Count" + players.Count);
            Debug.Log("lobby.HostId" + lobby.HostId);
            _connectedLobby.Players.Add(players[0].Player);
            if (_connectedLobby.HostId == AuthenticationService.Instance.PlayerId &&
                _connectedLobby.Players.Count == MaxPlayers)
            {
                Debug.Log("All players ready - starting game");
                try
                { 
                    _ = HostMatch(joinCode => _ = SendRelayCode(_connectedLobby, joinCode));
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        void OnLobbyEventConnectionStateChanged(LobbyEventConnectionState obj)
        {
            //throw new NotImplementedException();

        }
        void OnLobbyDeleted()
        {
            _isSearching = false;
            _connectedLobby = null;
        }
    }
    
    private async Task<Lobby> QuickJoinLobby(int playerElo, string timeControl)
    {
        try
        {
            var filters = new List<QueryFilter>
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0"),

                new QueryFilter(
                    field: QueryFilter.FieldOptions.S1,
                    op: QueryFilter.OpOptions.EQ,
                    value: timeControl),

                new QueryFilter(
                    field: QueryFilter.FieldOptions.N1,
                    op: QueryFilter.OpOptions.GT,
                    value: (playerElo - 500).ToString()),

                new QueryFilter(
                    field: QueryFilter.FieldOptions.N1,
                    op: QueryFilter.OpOptions.LT,
                    value: (playerElo + 500).ToString())
            };

            var quickJoinOptions = new QuickJoinLobbyOptions
            {
                Filter = filters,
                Player = new Player(AuthenticationService.Instance.PlayerId),
            };

            var lobby = await LobbyService.Instance.QuickJoinLobbyAsync(quickJoinOptions);
            return lobby;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error finding lobby: {e.Message}");
        }

        return null;
    }

    private async Task<Lobby> CreateLobby(int playerElo, string timeControl)
    {
        Debug.Log("Creating lobby");
        try
        {
            var options = new CreateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { JoinCodeKey, new DataObject(DataObject.VisibilityOptions.Member, null) },
                    {
                        TimeControlKey,
                        new DataObject(DataObject.VisibilityOptions.Public, timeControl, DataObject.IndexOptions.S1)
                    },
                    {
                        EloKey,
                        new DataObject(DataObject.VisibilityOptions.Public, playerElo.ToString(),
                            DataObject.IndexOptions.N1)
                    },
                }
            };

            var lobby = await LobbyService.Instance.CreateLobbyAsync("Chess Lobby", MaxPlayers, options);
            _heartbeatCoroutine = StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));

            return lobby;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed creating lobby: {e.Message}");
        }

        return null;
    }

    private async Task SendRelayCode(Lobby connectedLobby, string joinCode)
    {
        if (connectedLobby == null) throw new Exception("connectedLobby == null");
        if (joinCode == null) throw new Exception("joinCode == null");
        Debug.Log("Sending relay code: " + joinCode);

        try
        {
            var options = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { JoinCodeKey, new DataObject(DataObject.VisibilityOptions.Member, joinCode) }
                }
            };
            await LobbyService.Instance.UpdateLobbyAsync(connectedLobby.Id, options);
            Debug.Log("relay code updated successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending relay code: {e.Message}");
            Console.WriteLine(e);
            throw;
        }
    }

    private static IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
    {
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);
        while (true)
        {
            Debug.Log("Sending heartbeat");
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return delay;
        }
    }

    #endregion
    private void OnDestroy()
    {
        CancelMatchmaking();
    }

    private void OnApplicationQuit()
    {
        CancelMatchmaking();
        OnApplicationQuit2();
    }

    #region Reconnect

    private MatchData _matchData;
    public MatchData GetMigretedMatchData()
    {
        return _matchData;
    }
    
    public async Task MigrateHost(MatchData matchData, ulong oldId)
    {
        _matchData = matchData;
     
        Debug.Log($"MovingPlayerId {matchData.MovingPlayerId}");
        var player1 = matchData.Player1;
        var player2 = matchData.Player2;
        var whitePlayerId = player1.IsWhite ?  player1.PlayerId : player2.PlayerId;

        await HostMatch(null, false);
        var matchBootstrap = FindAnyObjectByType<MatchStarter>();
        matchBootstrap.OnHostMigratedRpc(
            player1.PlayerId,player1.FirebasePlayer.ID, player1.StartArrangement, player1.TimeToMove,
            player2.PlayerId,player2.FirebasePlayer.ID, player2.StartArrangement, player2.TimeToMove,
            whitePlayerId, oldId, 0, player1.StartTimeToMove);
    }

    public async Task ReConnectToMatch(string relayJoinCode)
    {
        Debug.Log("()ReConnectToMatch");
        try
        {
            _gameData.Mode = GameMode.Reconnect;
            await ConnectToMatch(relayJoinCode);

        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Console.WriteLine(e);
            throw;
        }
    }

    #endregion
    
    public async Task HostMatch(Action<string> callback = null, bool needLoadGameScene = true)
    {
        try
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            var ip = allocation.RelayServer.IpV4;
            var port = (ushort)allocation.RelayServer.Port;

            transport.SetHostRelayData(
                ip,
                port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData);
            _gameData.RelayJoinCode = joinCode;

            if (needLoadGameScene)
            {
                SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            else
            {
                NetworkManager.Singleton.StartHost();
                callback?.Invoke(joinCode);
            }

            return;

            void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                NetworkManager.Singleton.StartHost();
                callback?.Invoke(joinCode);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Console.WriteLine(e);
            throw;
        }

    }

    public async Task ConnectToMatch(string joinCode)
    {
        if (joinCode == null) return;
        
        Debug.Log("JoinCode " + joinCode);
        _gameData.RelayJoinCode = joinCode;
        var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
        SetTransformAsClient(allocation);
        NetworkManager.Singleton.StartClient();

    }

    private void SetTransformAsClient(JoinAllocation allocation)
    {
        transport.SetClientRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.HostConnectionData);
    }
    
    private IEnumerator UpdateSearchTimer()
    {
        while (_isSearching)
        {
            Debug.Log("UpdateSearchTimer");
            _searchTime += 1f;
            searchTimeText.text = $"Search time: {_searchTime}s";
            yield return new WaitForSeconds(1f);
        }
    }
    
    public void CancelMatchmaking(bool needDisconnect = true)
    {
        if (!_isSearching) return;

        onStateChanged.Invoke(MatchmakingState.Cancelled);
        _isSearching = false;

        if (_searchTimerCoroutine != null)
        {
            StopCoroutine(_searchTimerCoroutine);
            _searchTimerCoroutine = null;
        }

        if (_heartbeatCoroutine != null)
        {
            StopCoroutine(_heartbeatCoroutine);
            _heartbeatCoroutine = null;
        }

        if (_connectedLobby != null)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                LobbyService.Instance.DeleteLobbyAsync(_connectedLobby.Id);
                Debug.Log("DeleteLobbyAsync");
                if (needDisconnect)
                {
                    NetworkManager.Singleton.Shutdown();
                }
            }
            else
            {
                Debug.Log("RemovePlayerAsyncLobby");
                LobbyService.Instance.RemovePlayerAsync(_connectedLobby.Id, AuthenticationService.Instance.PlayerId);
            }
            // LobbyService.Instance.DeleteLobbyAsync(_connectedLobby.Id);
            _connectedLobby = null;
        }

        searchTimeText.text = "Search cancelled";
        Debug.Log("CancelMatchmaking");
    }
    
    
        public void OnApplicationQuit2()
        {
            Debug.Log("Application quitting...");
#if ANDROID
            GoogleSignIn.DefaultInstance.Disconnect();
#endif
            // Закриває Unity-процес повністю
            Application.Quit();

            Debug.Log(Process.GetCurrentProcess().ProcessName);
#if UNITY_STANDALONE_WIN &&  !UNITY_EDITOR
            System.Diagnostics.Process.GetCurrentProcess().Kill();
#endif
        }
    
}
