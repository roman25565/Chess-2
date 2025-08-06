using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Board;
using Setting;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

public class ArrangementEntryArrayWithId
{
    public ArrangementEntryArray Arrangement;
    public string FirestoreId;
    public ulong ID;
}

public class MatchBootstrap : NetworkBehaviour
{
    private static ArrangementEntryArrayWithId _player0;
    private static ArrangementEntryArrayWithId _player1;

    [SerializeField] private MatchCore matchCore;

    [Inject] private GameData _gameData;

    [Inject] private Global _global;

    private MatchData _matchData;
    private AdvancedMatchmaking _advancedMatchmaking;
    
    private void Awake()
    {
        ProjectContext.Instance.Container.InjectGameObject(gameObject);
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (IsServer) Camera.main.backgroundColor = Color.blue;
#endif
#if !UNITY_SERVER
        if (!IsOwner || !IsLocalPlayer) return;
        
        var gameMode = _gameData.Mode;
        var myArrangements = _global.MyArrangements;

        var arrangementEntryArray = new ArrangementEntryArray
        {
            ArrangementEntry = new ArrangementEntry[myArrangements.Count]
        };

        for (var index = 0; index < myArrangements.ToArray().Length; index++)
        {
            var arrangementEntry = myArrangements.ToArray()[index];
            arrangementEntryArray.ArrangementEntry[index] = arrangementEntry;
        }

        Debug.Log("gameMode" + gameMode);
        switch (gameMode)
        {
            case GameMode.Online:
            case GameMode.Offline:
            {
                var id = OwnerClientId;
                SendConnectedDataRpc(id, _global.FirestoreManager.MyData.ID, _gameData.TimeControl, arrangementEntryArray.ArrangementEntry);
                break;
            }
            case GameMode.Test:
                SendConnectedDataRpc(OwnerClientId, _global.FirestoreManager.MyData.ID, _gameData.TimeControl,
                    arrangementEntryArray.ArrangementEntry);
                SendConnectedDataRpc(2, "002", _gameData.TimeControl,
                    arrangementEntryArray.ArrangementEntry);
                break;
            case GameMode.Reconnect:
                GetReConnectDataRpc(OwnerClientId, _global.FirestoreManager.MyData.ID);
                break;
            case GameMode.MigrateHost: 
                break;
        }
#endif
    }

    [Rpc(SendTo.Server)]
    private void SendConnectedDataRpc(ulong playerId, string firestoreId, float timeControl, ArrangementEntry[] arrangement,
        RpcParams rpcParams = default)
    {
        if (_player0 == null)
        {
            _player0 = new ArrangementEntryArrayWithId
            {
                ID = playerId,
                FirestoreId = firestoreId,
                Arrangement = new ArrangementEntryArray { ArrangementEntry = arrangement }
            };
        }
        else if (_player1 == null)
        {
            _player1 = new ArrangementEntryArrayWithId
            {
                ID = playerId,
                FirestoreId = firestoreId,
                Arrangement = new ArrangementEntryArray { ArrangementEntry = arrangement }
            };
        }

        if (_player1 != null && _player0 != null)
        {
            _advancedMatchmaking = FindAnyObjectByType<AdvancedMatchmaking>();
            _advancedMatchmaking.CancelMatchmaking(false);
            
            var whitePlayerId = GetWhitePlayerId(_player0.ID, _player1.ID);

            _matchData = CreateMatchData(
                _player0.ID, firestoreId, _player0.Arrangement.ArrangementEntry, timeControl,
                _player1.ID, firestoreId, _player1.Arrangement.ArrangementEntry, timeControl,
                whitePlayerId, timeControl);;
            _gameData.ActiveBoard.ArrangeFigures(_matchData);
            
            _ = StartMatchServer(_matchData);
            

            var matchBootstraps = FindObjectsByType<MatchBootstrap>((FindObjectsSortMode)FindObjectsInactive.Exclude);

            foreach (var matchBootstrap in matchBootstraps)
                matchBootstrap.SendToClientPlayerBootstrapDataRpc(
                    _player0.ID,
                    _player0.FirestoreId,
                    _player0.Arrangement.ArrangementEntry,
                    timeControl,
                    _player1.ID,
                    _player1.FirestoreId,
                    _player1.Arrangement.ArrangementEntry,
                    timeControl,
                    whitePlayerId,
                    timeControl);
        }
    }

    private Task StartMatchServer(MatchData matchData)
    {
        var corePlayer1 = Instantiate(matchCore, transform);
        corePlayer1.GetComponent<NetworkObject>().SpawnWithOwnership(matchData.Player1.PlayerId);

        var corePlayer2 = Instantiate(matchCore, transform);
        corePlayer2.GetComponent<NetworkObject>().SpawnWithOwnership(matchData.Player2.PlayerId);

        corePlayer1.SetServerCore(corePlayer1);
        corePlayer2.SetServerCore(corePlayer1);
        corePlayer1.SetServerMatchData(matchData);
        corePlayer2.SetServerMatchData(matchData);
        return Task.CompletedTask;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendToClientPlayerBootstrapDataRpc(
        ulong playerId, string firestoreId, ArrangementEntry[] arrangement, float timeControl,
        ulong playerId2, string firestoreId2, ArrangementEntry[] arrangement2, float timeControl2,
        ulong whitePlayerId, float startTimeControl)
    {
        if (!IsLocalPlayer) return;

        _matchData = CreateMatchData(
            playerId, firestoreId, arrangement, timeControl,
            playerId2, firestoreId2, arrangement2, timeControl2,
            whitePlayerId, startTimeControl);

        InitCore(_matchData);
        TryArrangeFigures(_matchData);
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void InitCore(MatchData matchData)
    {
        var allMatchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);

        foreach (var core in allMatchCores)
        {
            if (!core.IsOwner) continue;
            
            core.Init(matchData);
            LoadFirestoreRefreshUI(matchData, core);
        }
    }
    
    private void LoadFirestoreRefreshUI(MatchData matchData, MatchCore core)
    {
        _ = _global.FirestoreManager.PlayerDataManager.GetPlayerData(matchData.Player1.FirebasePlayer.ID, result =>
        {
            matchData.Player1.FirebasePlayer = result;

            core.RefreshPlayerUI(matchData.Player1.PlayerId, matchData.Player1.PlayerId != OwnerClientId);
        });
        _ = _global.FirestoreManager.PlayerDataManager.GetPlayerData(matchData.Player2.FirebasePlayer.ID, result =>
        {
            matchData.Player2.FirebasePlayer = result;
            core.RefreshPlayerUI(matchData.Player2.PlayerId, matchData.Player2.PlayerId != OwnerClientId);
        });
    }

    private void TryArrangeFigures(MatchData matchData)
    {
        var board = _gameData.ActiveBoard;
        board.ArrangeFigures(matchData);
        var ownerData = FindOwnerData();

        if (ownerData.IsRotate) board.RotateBoard();

        return;

        PlayerData FindOwnerData()
        {
            if (OwnerClientId == matchData.Player1.PlayerId) return matchData.Player1;

            if (OwnerClientId == matchData.Player2.PlayerId) return matchData.Player2;

            return null;
        }
    }

    private MatchData CreateMatchData(
        ulong playerId, string firestoreId, ArrangementEntry[] arrangement, float timeControl,
        ulong playerId2, string firestoreId2, ArrangementEntry[] arrangement2, float timeControl2,
        ulong whitePlayerId, float startTimeControl)
    {
        return new MatchData
        {
            MovingPlayerId = whitePlayerId,
            Player1 = new PlayerData
            {
                FirebasePlayer = new FirebasePlayerData(firestoreId),
                PlayerId = playerId,
                IsMoving = playerId == whitePlayerId,
                IsRotate = playerId != whitePlayerId,
                IsWhite = playerId == whitePlayerId,
                StartArrangement = arrangement,
                TimeToMove = timeControl,
                StartTimeToMove = startTimeControl
            },
            Player2 = new PlayerData
            {
                FirebasePlayer = new FirebasePlayerData(firestoreId2),
                PlayerId = playerId2,
                IsMoving = playerId2 == whitePlayerId,
                IsRotate = playerId2 != whitePlayerId,
                IsWhite = playerId2 == whitePlayerId,
                StartArrangement = arrangement2,
                TimeToMove = timeControl2,
                StartTimeToMove = startTimeControl
            }
        };
    }
    

    private ulong GetWhitePlayerId(ulong playerId, ulong playerId2)
    {
        var randomValue = Random.value;
        var randomIndex = randomValue < 0.5 ? 1 : 2;

        return randomIndex == 1 ? playerId : playerId2;
    }

    
    private async void OnClientDisconnect(ulong clientId)//clientId not walid if is host disconnected and Equals OwnerClientId
    {
        if (NetworkManager.Singleton.ShutdownInProgress || !NetworkManager.Singleton.IsListening)
        {
            return;
        }
        Debug.Log($"OnClientDisconnect clientId: {clientId}, {OwnerClientId}, IsHost {IsHost}, Count {NetworkManager.ConnectedClients.Count}");
        
        if (IsHost)
        {
            if (clientId == OwnerClientId) return;
            
            SendReConnectRequest(clientId);
            DestroyDisconnectedCoreRpc();
        }
        else
        {
            _gameData.Mode = GameMode.MigrateHost;
            var data = GetMyCoreOnHostMigrate().GetMatchData();
            Debug.Log($"[Host Migrating] Player Data:\n" +
                      $"P1: NetworkID={data.Player1.PlayerId} | FirebaseID={data.Player1.FirebasePlayer?.ID ?? "null"} | IsMoving={data.Player1.IsMoving} | Color={(data.Player1.IsWhite ? "White" : "Black")}\n" +
                      $"P2: NetworkID={data.Player2.PlayerId} | FirebaseID={data.Player2.FirebasePlayer?.ID ?? "null"} | IsMoving={data.Player2.IsMoving} | Color={(data.Player2.IsWhite ? "White" : "Black")}\n" +
                      $"Current Moving Player: {(data.MovingPlayerId == data.Player1.PlayerId ? "P1" : "P2")}\n" +
                      $"Time Remaining: P1={data.Player1.TimeToMove:F1}s | P2={data.Player2.TimeToMove:F1}s");
            _advancedMatchmaking = FindAnyObjectByType<AdvancedMatchmaking>();
            await _advancedMatchmaking.MigrateHost(data, OwnerClientId);
            var anotherPlayerId = data.GetAnotherPlayerData(0).PlayerId; // 0 is host id
            SendReConnectRequest(anotherPlayerId);
        }
    }

    [Rpc(SendTo.Server)]
    public void OnHostMigratedRpc(
        ulong playerId, string firestoreId, ArrangementEntry[] arrangement, float timeControl,
        ulong playerId2, string firestoreId2, ArrangementEntry[] arrangement2, float timeControl2,
        ulong whitePlayerId, ulong oldId, ulong newId, float startTimeControl, RpcParams rpcParams = default)
    {
        _matchData = CreateMatchData(
            playerId, firestoreId, arrangement, timeControl,
            playerId2, firestoreId2, arrangement2, timeControl2,
            whitePlayerId, startTimeControl);
        
        var hostId = OwnerClientId == playerId ? playerId : playerId2;
        
        var hostCore = Instantiate(matchCore, transform);
        hostCore.GetComponent<NetworkObject>().SpawnWithOwnership(hostId);
        hostCore.SetServerCore(hostCore);
        
        hostCore.UpdateServerData();
        hostCore.OnHostMigratedRpc(oldId, newId);
    }
    
    private void SendReConnectRequest(ulong clientId)
    {
        var enemyPlayerId = _matchData.GetPlayerData(clientId).FirebasePlayer.ID;
        _ = _global.FirestoreManager.RealtimeDatabase.ReConnectRequestsManager.SendReConnectRequest(enemyPlayerId, _gameData.RelayJoinCode);
    }

    [Rpc(SendTo.Server)]
    private void DestroyDisconnectedCoreRpc()
    {
        var matchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);
        var serverCore = matchCores[0].IsServerCore ? matchCores[0] : matchCores[1];
        serverCore.DestroyDisconnectedCore();
    }

    [Rpc(SendTo.Server)]
    private void GetReConnectDataRpc(ulong connectedPlayerId, string connectedFirestoreId, RpcParams rpcParams = default)
    {
        if (!IsOwnedByServer)
        {
            var butt = GetServerBootstrap();
            butt.GetReConnectDataRpc(connectedPlayerId, connectedFirestoreId);
            return;
        }
        var board = _gameData.ActiveBoard;
        var matchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);
        
        var hostPlayerCore = matchCores[0].IsServerCore ? matchCores[0] : matchCores[1];
        var hostPlayerId = hostPlayerCore.OwnerClientId;
        var connectedPlayerOldId = _matchData.GetAnotherPlayerData(hostPlayerId).PlayerId;
        var connectedPlayerCore = Instantiate(matchCore, transform);
        var matchBootstraps = FindObjectsByType<MatchBootstrap>((FindObjectsSortMode)FindObjectsInactive.Exclude);
        var connectedPlayerBootstrap =
            matchBootstraps[0].OwnerClientId == connectedPlayerId ? matchBootstraps[0] : matchBootstraps[1];
        if(connectedPlayerBootstrap.OwnerClientId != connectedPlayerId)
            connectedPlayerBootstrap.NetworkObject.ChangeOwnership(connectedPlayerId);
        var serverCore = GetServerCore();
        var hostPlayerFirestoreId = serverCore.GetFirestoreId(hostPlayerId);

        hostPlayerCore.ChangeDataIPRpc(connectedPlayerOldId, connectedPlayerId, connectedFirestoreId);
        
        if (!serverCore.AddCore(connectedPlayerCore))
        {
            Debug.LogError("Failed to add core to server");
            Destroy(connectedPlayerCore);
            return;
        }
        
        var netObj = connectedPlayerCore.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(connectedPlayerId);
        connectedPlayerCore.SetServerCore(serverCore);

        serverCore.GetReconnectData(connectedPlayerId, hostPlayerId,
            out float connectedTimeToMove,
            out float hostTimeToMove,
            out float startTimeControl,
            out ulong movingPlayerId,
            out ulong whitePlayerId,
            out ArrangementEntry[] connectedArrangement,
            out ArrangementEntry[] hostArrangement);


        connectedPlayerBootstrap.SendToClientPlayerBootstrapDataRpc(hostPlayerId, hostPlayerFirestoreId, hostArrangement, 10,
            connectedPlayerId, connectedFirestoreId, connectedArrangement, 10,
            whitePlayerId, startTimeControl);;
        
        Debug.Log($"whitePlayerId {whitePlayerId} remainingPlayerId {hostPlayerId}");

        GetMoves(out var from, out var to);
        connectedPlayerBootstrap.SendReMovesRpc(from.ToArray(), to.ToArray());
        connectedPlayerBootstrap.SetTimeControlRpc(hostTimeToMove, connectedTimeToMove);
        connectedPlayerBootstrap.SetMovingPlayerIdRpc(movingPlayerId);
        
        Debug.Log("Player1Id" + _matchData.Player1.PlayerId + "Player2Id" + _matchData.Player2.PlayerId );
        
        
        return;

        MatchCore GetServerCore()
        {
            foreach (var core in matchCores)
            {
                if (core.IsServerCore)
                    return core;
            }

            return null;
        }

    }

    private MatchBootstrap GetServerBootstrap()
    {
        var matchBootstraps = FindObjectsByType<MatchBootstrap>((FindObjectsSortMode)FindObjectsInactive.Exclude);
        var serverBootstrap = matchBootstraps[0].IsOwnedByServer ? matchBootstraps[0] : matchBootstraps[1];
        return serverBootstrap;
    }

    private void GetMoves(out List<Vector2Int> from,out List<Vector2Int> to)
    {
        var history = _gameData.ActiveBoard.GetHistory();
        from = new List<Vector2Int>();
        to = new List<Vector2Int>();
        
        foreach (var move in history)
        {
            from.Add(new Vector2Int(move.From.Row,  move.From.Column));
            to.Add(new Vector2Int(move.To.Row, move.To.Column));
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetMovingPlayerIdRpc(ulong movingPlayerId)
    {
        if (!IsLocalPlayer) return;
        var myCore = GetMyCore();
        if (myCore == null) new NullReferenceException(nameof(myCore));
        
        myCore.SetMovingPlayerId(movingPlayerId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetTimeControlRpc(float hostTimeToMove, float myTimeToMove)
    {
        if (!IsLocalPlayer) return;
        var myCore = GetMyCore();
        if (myCore == null) new NullReferenceException(nameof(myCore));
        
        myCore.SetTimeControl(hostTimeToMove, myTimeToMove);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendReMovesRpc(Vector2Int[] from, Vector2Int[] to)
    {
        if (!IsLocalPlayer) return;
        var myCore = GetMyCore();
        if (myCore == null) new NullReferenceException(nameof(myCore));
        
        Debug.Log("SendReMovesRpc");

        var movingPlayerId = myCore.GetMovingPlayerId();
        for (int i = 0; i < from.Length; i++)
        {
            myCore.UseMove(from[i], to[i],movingPlayerId);
        }
        
        return;

    }

    private MatchCore GetMyCore()
    {
        var matchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);
        foreach (var core in matchCores)
        {
            Debug.Log("OwnerClientId " + OwnerClientId + core.IsServerCore);
            if (core.OwnerClientId == OwnerClientId)
            {
                return core;
            }
        }

        return null;
    }
    private MatchCore GetMyCoreOnHostMigrate()
    {
        var matchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);
        Debug.Log($"1 {matchCores[0].IsServerCore} 2 {matchCores[1].IsServerCore}" +
                  $"{(matchCores[0].GetMatchData() == null)} 2 {(matchCores[1].GetMatchData() == null)}");
        
        foreach (var core in matchCores)
        {
            if (core.GetMatchData() == null)
            {
                continue;
            }
            return core;
        }

        return null;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SendStartArrangementRpc(ulong targetPlayerId, string firestoreId, ArrangementEntry[] arrangement,
        RpcParams rpcParams = default)
    {
        if (!IsLocalPlayer) return;
        if (OwnerClientId != targetPlayerId) return;
        var matchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);
        foreach (var core in matchCores)
        {
            if (core.OwnerClientId == OwnerClientId)
            {
                core.SetEnemyStartArrangement(arrangement);
            }
        }
    }
   
}   