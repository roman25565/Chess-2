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
                Debug.Log(NetworkManager.Singleton.GetInstanceID());
                SendConnectedDataRpc(id, _global.FirestoreManager.PlayerData.ID, _gameData.TimeControl, arrangementEntryArray.ArrangementEntry);
                break;
            }
            case GameMode.Test:
                SendConnectedDataRpc(OwnerClientId, _global.FirestoreManager.PlayerData.ID, _gameData.TimeControl,
                    arrangementEntryArray.ArrangementEntry);
                SendConnectedDataRpc(2, "002", _gameData.TimeControl,
                    arrangementEntryArray.ArrangementEntry);
                break;
            case GameMode.Reconnect:
                GetReConnectDataRpc(OwnerClientId, _global.FirestoreManager.PlayerData.ID);
                break;
        }
#endif
    }

    [Rpc(SendTo.Server)]
    private void SendConnectedDataRpc(ulong playerId, string firestoreId, int timeControl, ArrangementEntry[] arrangement,
        RpcParams rpcParams = default)
    {
        Debug.Log(firestoreId);
        if (_player0 == null)
        {
            Debug.Log("player0");
            _player0 = new ArrangementEntryArrayWithId
            {
                ID = playerId,
                FirestoreId = firestoreId,
                Arrangement = new ArrangementEntryArray { ArrangementEntry = arrangement }
            };
        }
        else if (_player1 == null)
        {
            Debug.Log("player1");
            _player1 = new ArrangementEntryArrayWithId
            {
                ID = playerId,
                FirestoreId = firestoreId,
                Arrangement = new ArrangementEntryArray { ArrangementEntry = arrangement }
            };
        }

        if (_player1 != null && _player0 != null)
        {
            _advancedMatchmaking = FindObjectOfType<AdvancedMatchmaking>();
            _advancedMatchmaking.CancelMatchmaking(false);
            
            
            var whitePlayerId = GetWhitePlayerId(_player0.ID, _player1.ID);

            var player1 = CreatePlayerBootstrapData(_player0.ID, firestoreId, _player0.Arrangement.ArrangementEntry,
                whitePlayerId, timeControl);
            var player2 = CreatePlayerBootstrapData(_player1.ID, firestoreId, _player1.Arrangement.ArrangementEntry,
                whitePlayerId, timeControl);

            Debug.Log($"player1 IsRotate {player1.IsRotate} player2 IsRotate {player2.IsRotate}" );
            _gameData.ActiveBoard.ArrangeFigures(player1, player2);
            _matchData = CreateMatchData(player1, player2, whitePlayerId);
            _ = StartMatchServer(player1, player2, whitePlayerId, timeControl);
            

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
                    whitePlayerId);
        }
    }

    private Task StartMatchServer(PlayerBootstrapData player1, PlayerBootstrapData player2, ulong whitePlayerId, int timeControl)
    {
        var matchData = CreateMatchData(player1, player2, whitePlayerId);
        
        var corePlayer1 = Instantiate(matchCore, transform);
        corePlayer1.GetComponent<NetworkObject>().SpawnWithOwnership(player1.PlayerId);

        var corePlayer2 = Instantiate(matchCore, transform);
        corePlayer2.GetComponent<NetworkObject>().SpawnWithOwnership(player2.PlayerId);

        corePlayer1.SetServerCore(corePlayer1);
        corePlayer2.SetServerCore(corePlayer1);
        corePlayer1.SetServerMatchData(matchData);
        corePlayer2.SetServerMatchData(matchData);
        return Task.CompletedTask;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendToClientPlayerBootstrapDataRpc(
        ulong playerId, string firestoreId, ArrangementEntry[] arrangement, int timeControl,
        ulong playerId2, string firestoreId2, ArrangementEntry[] arrangement2, int timeControl2,
        ulong whitePlayerId)
    {
        if (!IsLocalPlayer) return;

        
        var player1 = new PlayerBootstrapData(playerId, firestoreId, arrangement, playerId != whitePlayerId,
            whitePlayerId == playerId, timeControl);
        var player2 = new PlayerBootstrapData(playerId2, firestoreId2, arrangement2, playerId2 != whitePlayerId,
            whitePlayerId == playerId2, timeControl2);
        Debug.Log($"P1 {player1.PlayerId}, {player1.IsWhite}, P2 {player2.PlayerId}, {player2.IsWhite}");

        InitCore(player1, player2, whitePlayerId, timeControl);
        TryArrangeFigures(player1, player2);
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void InitCore(PlayerBootstrapData player1, PlayerBootstrapData player2, ulong whitePlayerId, int timeControl)
    {
        var allMatchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);

        foreach (var core in allMatchCores)
        {
            if (!core.IsOwner) continue;

            _matchData = CreateMatchData(player1, player2, whitePlayerId);
            
            core.Init(_matchData);
            LoadFirestoreRefreshUI(player1, player2, _matchData, core);
            Debug.Log("matchCore.Init(matchData);");
        }
    }


    private void InitCore(ReconnectData reconnectData)
    {
        var allMatchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);

        foreach (var core in allMatchCores)
        {
            if (!core.IsOwner) continue;

            var matchData = CreateMatchData(reconnectData.Player1, reconnectData.Player2, reconnectData.WhitePlayerId);
            
            matchData.Player1.TimeToMove = reconnectData.MyTimeToMove;
            matchData.Player2.TimeToMove = reconnectData.EnemyTimeToMove;
            matchData.MovingPlayerId = reconnectData.MovingPlayerId;
            
            core.Init(matchData);
            
            LoadFirestoreRefreshUI(reconnectData.Player1,reconnectData.Player2, matchData, core);
            Debug.Log("matchCore.Init(matchData);");
        }
    }
    private void LoadFirestoreRefreshUI(PlayerBootstrapData player1, PlayerBootstrapData player2, MatchData matchData, MatchCore core)
    {
        _ = _global.FirestoreManager.GetPlayerData(player1.FirestoreId, result =>
        {
            matchData.Player1.FirebasePlayer = result;

            core.RefreshPlayerUI(matchData.Player1.PlayerId, matchData.Player1.PlayerId != OwnerClientId);
        });
        _ = _global.FirestoreManager.GetPlayerData(player2.FirestoreId, result =>
        {
            matchData.Player2.FirebasePlayer = result;
            core.RefreshPlayerUI(matchData.Player2.PlayerId, matchData.Player2.PlayerId != OwnerClientId);
        });
    }

    private void TryArrangeFigures(PlayerBootstrapData player1, PlayerBootstrapData player2)
    {
        var board = _gameData.ActiveBoard;
        board.ArrangeFigures(player1, player2);
        var ownerData = FindOwnerData();

        Debug.Log(
            $"player1: {player1.PlayerId} IsRotate: {player1.IsRotate}, player2: {player2.PlayerId} IsRotate: {player2.IsRotate}");

        if (ownerData.IsRotate) board.RotateBoard();

        return;

        PlayerBootstrapData FindOwnerData()
        {
            if (OwnerClientId == player1.PlayerId) return player1;

            if (OwnerClientId == player2.PlayerId) return player2;

            return null;
        }
    }

    private MatchData CreateMatchData(PlayerBootstrapData player1, PlayerBootstrapData player2, ulong whitePlayerId)
    {
        Debug.Log($"whitePlayerId: {whitePlayerId}");
        return new MatchData
        {
            MovingPlayerId = whitePlayerId,
            Player1 = new PlayerData
            {
                FirebasePlayer = new FirebasePlayerData(player1.FirestoreId),
                PlayerId = player1.PlayerId,
                IsMoving = player1.PlayerId == whitePlayerId,
                IsRotate = player1.IsRotate,
                IsWhite = player1.IsWhite,
                StartArrangement = player1.Arrangement,
                TimeToMove = player1.TimeToMove
            },
            Player2 = new PlayerData
            {
                FirebasePlayer = new FirebasePlayerData(player1.FirestoreId),
                PlayerId = player2.PlayerId,
                IsMoving = player2.PlayerId == whitePlayerId,
                IsRotate = player2.IsRotate,
                IsWhite = player2.IsWhite,
                StartArrangement = player2.Arrangement,
                TimeToMove = player1.TimeToMove
            }
        };
    }

    private PlayerBootstrapData CreatePlayerBootstrapData(ulong playerId, string firestoreId,
        ArrangementEntry[] arrangement, ulong whitePlayerId, int timeControl)
    {
        return new PlayerBootstrapData(playerId, firestoreId, arrangement, whitePlayerId != playerId,
            whitePlayerId == playerId, timeControl);
    }
    

    private ulong GetWhitePlayerId(ulong playerId, ulong playerId2)
    {
        var randomValue = Random.value;
        var randomIndex = randomValue < 0.5 ? 1 : 2;

        return randomIndex == 1 ? playerId : playerId2;
    }

    
    private async void OnClientDisconnect(ulong clientId)//clientId not walid if is host disconnected and Equals OwnerClientId
    {
        Debug.Log($"OnClientDisconnect clientId: {clientId}, {OwnerClientId}");//TODO problem work on exit application
        
        Debug.Log($"OwnerId {OwnerClientId}");
        if (IsHost)
        {
            if (clientId == OwnerClientId)
            {
                return;
            }
            SendReConnectRequest(clientId);
            DestroyDisconnectedCoreRpc();
        }
        else
        {
            var data = GetMyCore().GetMatchData();
            _advancedMatchmaking = FindObjectOfType<AdvancedMatchmaking>();
            await _advancedMatchmaking.MigrateHost(data, OwnerClientId);
            var anotherPlayerId = data.GetAnotherPlayerData(0).PlayerId;
            Debug.Log($"anotherPlayerId {anotherPlayerId}, Pl1 {data.Player1.PlayerId}, Pl2 {data.Player2.PlayerId}");
            SendReConnectRequest(anotherPlayerId);
        }
    }

    [Rpc(SendTo.Server)]
    public void OnHostMigratedRpc(
        ulong playerId, string firestoreId, ArrangementEntry[] arrangement, float timeControl,
        ulong playerId2, string firestoreId2, ArrangementEntry[] arrangement2, float timeControl2,
        ulong whitePlayerId, ulong oldId, ulong newId, RpcParams rpcParams = default)
    {

        var player1 = CreatePlayerBootstrapData(playerId, firestoreId, arrangement,
            whitePlayerId, 10);
        var player2 = CreatePlayerBootstrapData(playerId2, firestoreId2, arrangement2,
            whitePlayerId, 10);
        
        _matchData = CreateMatchData(player1, player2, whitePlayerId);
        _matchData.Player1.TimeToMove = timeControl;
        _matchData.Player2.TimeToMove = timeControl2;
        
        var hostId = OwnerClientId == playerId ? playerId : playerId2;
        
        var hostCore = Instantiate(matchCore, transform);
        hostCore.GetComponent<NetworkObject>().SpawnWithOwnership(hostId);
        hostCore.SetServerCore(hostCore);
        
        hostCore.UpdateServerData();
        hostCore.OnHostMigratedRpc(oldId, newId);
        
        

        // SendToClientPlayerBootstrapDataRpc(
        //     _player0.ID,
        //     _player0.FirestoreId,
        //     _player0.Arrangement.ArrangementEntry,
        //     _player1.ID,
        //     _player1.FirestoreId,
        //     _player1.Arrangement.ArrangementEntry,
        //     whitePlayerId,
        //     10);
        
        var hostData = _matchData.GetPlayerData(hostId);
        var anotherPlayerData = _matchData.GetAnotherPlayerData(hostId);
        // SetTimeControlRpc(hostData.TimeToMove, anotherPlayerData.TimeToMove);
        // SetMovingPlayerIdRpc(data.MovingPlayerId);
    }
    
    // public void OnMigrateHost(MatchData data)
    // {
    //     var history = _gameData.ActiveBoard.GetHistory();
    //     Debug.Log("moves Count " + history.Count);
    //     
    //     var hostId = OwnerClientId == data.Player1.PlayerId ? data.Player1.PlayerId : data.Player2.PlayerId;
    //     var hostData = data.GetPlayerData(hostId);
    //     var anotherPlayerData = data.GetAnotherPlayerData(hostId);
    //     var whitePlayerId = data.Player1.IsWhite ?  data.Player1.PlayerId : data.Player2.PlayerId;
    //
    //     var player1 = CreatePlayerBootstrapData(data.Player1.PlayerId, data.Player1.FirebasePlayer.ID, data.Player1.StartArrangement,
    //         whitePlayerId, 10);
    //     var player2 = CreatePlayerBootstrapData(data.Player2.PlayerId, data.Player2.FirebasePlayer.ID, data.Player2.StartArrangement,
    //         whitePlayerId, 10);
    //     
    //     _gameData.ActiveBoard.ArrangeFigures(player1, player2);
    //     _matchData = data;
    //     
    //     var hostCore = Instantiate(matchCore, transform);
    //     hostCore.GetComponent<NetworkObject>().SpawnWithOwnership(hostId);
    //
    //     hostCore.SetServerCore(hostCore);
    //     hostCore.SetServerMatchData(data);
    //
    //     SendToClientPlayerBootstrapDataRpc(
    //         _player0.ID,
    //         _player0.FirestoreId,
    //         _player0.Arrangement.ArrangementEntry,
    //         _player1.ID,
    //         _player1.FirestoreId,
    //         _player1.Arrangement.ArrangementEntry,
    //         whitePlayerId,
    //         10);
    //     
    //     GetMoves(out var from, out var to);
    //     SendReMovesRpc(from.ToArray(), to.ToArray());
    //     SetTimeControlRpc(hostData.TimeToMove, anotherPlayerData.TimeToMove);
    //     SetMovingPlayerIdRpc(data.MovingPlayerId);
    // }
    
    private void SendReConnectRequest(ulong clientId)
    {
        Debug.Log("SendReConnectRequest");
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
    
    // [Rpc(SendTo.Server)]
    // private void GetReConnectDataRpc(ulong connectedPlayerId, string firestoreId, RpcParams rpcParams = default)
    // {
    //     var board = _gameData.ActiveBoard;
    //     var matchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);
    //     
    //     if (matchCores.Length != 2) 
    //     {
    //         Debug.Log("matchCores.Length" + matchCores.Length);
    //         throw new ArgumentOutOfRangeException(nameof(matchCores));
    //     }
    //
    //     var remainingPlayerCore = matchCores[0].IsServerCore ? matchCores[0] : matchCores[1];
    //     var remainingPlayerId = remainingPlayerCore.OwnerClientId;
    //     var oldId = (ulong)(remainingPlayerId == 1 ? 2 : 1);
    //     
    //     var connectedPlayerCore = Instantiate(matchCore, transform);
    //     var matchBootstraps = FindObjectsByType<MatchBootstrap>((FindObjectsSortMode)FindObjectsInactive.Exclude);
    //     var remainingPlayerBootstrap = matchBootstraps[0].OwnerClientId == remainingPlayerId
    //         ? matchBootstraps[0]
    //         : matchBootstraps[1];
    //     var connectedPlayerBootstrap =
    //         matchBootstraps[0].OwnerClientId == connectedPlayerId ? matchBootstraps[0] : matchBootstraps[1];
    //
    //     
    //     var serverCore = GetServerCore();
    //     if (serverCore == null) throw new ArgumentOutOfRangeException(nameof(serverCore));
    //     var remainingPlayerFirestoreId = serverCore.GetFirestoreId(remainingPlayerId); 
    //     
    //     foreach (var core in matchCores)
    //         core.OnClientReConnectRpc(oldId, connectedPlayerId, firestoreId);
    //     serverCore.OnClientReConnect(oldId, connectedPlayerId);
    //     
    //     serverCore.AddCore(connectedPlayerCore);
    //     connectedPlayerCore.GetComponent<NetworkObject>().SpawnWithOwnership(connectedPlayerId);
    //     connectedPlayerCore.SetServerCore(serverCore);
    //     
    //     serverCore.GetMatchData(connectedPlayerId, remainingPlayerId,out float connectedTimeToMove, out float remainingTimeToMove, out ulong movingPlayerId,
    //         out ulong whitePlayerId);
    //     
    //     board.GetPiecesInBoard(connectedPlayerId, remainingPlayerId, out var connectedPlayerPieces, out var remainingPlayerPieces);
    //     Debug.Log("connectedPlayerPieces" + connectedPlayerPieces.Length);
    //     Debug.Log("remainingPlayerPieces" + remainingPlayerPieces.Length);
    //     Debug.Log("Ecuals" + (connectedPlayerPieces[0] == remainingPlayerPieces[0]));;
    //     connectedPlayerBootstrap.SendReConnectDataRpc(connectedTimeToMove, remainingTimeToMove, movingPlayerId, whitePlayerId, connectedPlayerPieces, remainingPlayerPieces, remainingPlayerId, remainingPlayerFirestoreId);
    //
    //     var player = _matchData.GetPlayerData(connectedPlayerId);
    //     connectedPlayerBootstrap.SendStartArrangementRpc(connectedPlayerId,player.FirebasePlayer.ID,player.Arrangement);
    //
    //     return;
    //
    //     MatchCore GetServerCore()
    //     {
    //         MatchCore serverCore1 = null;
    //         foreach (var core in matchCores)
    //         {
    //             if (!core.IsServerCore) continue;
    //             serverCore1 = core;
    //             break;
    //         }
    //
    //         return serverCore1;
    //     }
    // }

    [Rpc(SendTo.Server)]
    private void GetReConnectDataRpc(ulong connectedPlayerId, string connectedFirestoreId, RpcParams rpcParams = default)
    {
        var board = _gameData.ActiveBoard;
        var matchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);

        var hostPlayerCore = matchCores[0].IsServerCore ? matchCores[0] : matchCores[1];
        var hostPlayerId = hostPlayerCore.OwnerClientId;
        var oldId = (ulong)(hostPlayerId == 1 ? 2 : 1);
        var connectedPlayerCore = Instantiate(matchCore, transform);
        var matchBootstraps = FindObjectsByType<MatchBootstrap>((FindObjectsSortMode)FindObjectsInactive.Exclude);
        var connectedPlayerBootstrap =
            matchBootstraps[0].OwnerClientId == connectedPlayerId ? matchBootstraps[0] : matchBootstraps[1];
        var serverCore = GetServerCore();
        var hostPlayerFirestoreId = serverCore.GetFirestoreId(hostPlayerId);

        Debug.Log("vars Complete");

        // 8. Виконання логіки реконекту
        foreach (var core in matchCores)
            core.OnClientReConnectRpc(oldId, connectedPlayerId, connectedFirestoreId);

        serverCore.ChangeDataIP(oldId, connectedPlayerId);
        
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
            out ulong movingPlayerId,
            out ulong whitePlayerId,
            out ArrangementEntry[] connectedArrangement,
            out ArrangementEntry[] hostArrangement);


        connectedPlayerBootstrap.SendToClientPlayerBootstrapDataRpc(hostPlayerId, hostPlayerFirestoreId, hostArrangement, 10, connectedPlayerId, connectedFirestoreId, connectedArrangement, 10, whitePlayerId);
        
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
            if (core.OwnerClientId == OwnerClientId)
            {
                return core;
            }
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
   
//     [Rpc(SendTo.ClientsAndHost)]
//     private void SendReConnectDataRpc(
//         float myTimeToMove,
//         float enemyTimeToMove,
//         ulong movingPlayerId,
//         ulong whitePlayerId,
//         ArrangementEntry[] myPieces,
//         ArrangementEntry[] enemyPieces,
//         ulong enemyId, string enemyFirestoreId,
//         RpcParams rpcParams = default)
//     {
//         if (!IsLocalPlayer) return;
//         
//         Debug.Log("===== Мої фігури =====");
//         for (int i = 0; i < myPieces.Length; i++)
//         {
//             Debug.Log($"Фігура {i}: Тип: {myPieces[i].pieceType}, Рядок: {myPieces[i].row}, Колонка: {myPieces[i].column}");
//         }
//
//         Debug.Log("===== Фігури противника =====");
//         for (int i = 0; i < enemyPieces.Length; i++)
//         {
//             Debug.Log($"Фігура {i}: Тип: {enemyPieces[i].pieceType}, Рядок: {enemyPieces[i].row}, Колонка: {enemyPieces[i].column}");
//         }
//         
//         var imWhite = enemyId != whitePlayerId;
// #if !UNITY_SERVER
//         var myPlayer = new PlayerBootstrapData(OwnerClientId, _global.FirestoreManager.PlayerData.ID, myPieces,!imWhite, imWhite);
//         var enemyPlayer = new PlayerBootstrapData(enemyId, enemyFirestoreId, enemyPieces, imWhite, !imWhite);
//         var board = _gameData.ActiveBoard;
//         
//         _reconnectData = new ReconnectData
//         {
//             MyTimeToMove = myTimeToMove,
//             EnemyTimeToMove = enemyTimeToMove,
//             MovingPlayerId = movingPlayerId,
//             WhitePlayerId = whitePlayerId,
//             Player1 = myPlayer,
//             Player2 = enemyPlayer
//         };
//
//         board.ArrangeFigures(myPlayer, enemyPlayer, false);
//         
// #endif
//         InitCore(_reconnectData);
//     }
//
//     [Rpc(SendTo.ClientsAndHost)]
//     public void SendBootstrapDataClientRpc(ulong playerId, string firestoreId, ArrangementEntry[] arrangement,
//         ulong playerId2, string firestoreId2, ArrangementEntry[] arrangement2,
//         ulong whitePlayerId,
//         RpcParams rpcParams = default)
//     {
// #if !UNITY_SERVER
//         if (!IsLocalPlayer) return;
//         var imWhite = playerId == _reconnectData.WhitePlayerId;
//         var player1 = new PlayerBootstrapData(OwnerClientId, _global.FirestoreManager.PlayerData.ID,
//             _global.MyArrangements.ToArray(), !imWhite, imWhite);
//         var player2 = new PlayerBootstrapData(playerId, firestoreId, arrangement, imWhite, !imWhite);
//         
//         _reconnectData.Player1 = player1;
//         _reconnectData.Player2 = player2;
//
//         InitCore(_reconnectData);
// #endif
    // }

    private ReconnectData _reconnectData;
    

    public class ReconnectData
    {
        public PlayerBootstrapData Player1;
        public PlayerBootstrapData Player2;
        public float MyTimeToMove;
        public float EnemyTimeToMove;
        public ulong MovingPlayerId;
        public ulong WhitePlayerId;
    }

    public class PlayerBootstrapData
    {
        public readonly ArrangementEntry[] Arrangement;
        public readonly string FirestoreId;
        public readonly bool IsRotate;
        public readonly bool IsWhite;
        public readonly float TimeToMove;
        public readonly ulong PlayerId;

        public PlayerBootstrapData(ulong playerId, string firestoreId, ArrangementEntry[] arrangement, bool isRotate,
            bool isWhite, float timeToMove)
        {
            PlayerId = playerId;
            FirestoreId = firestoreId;
            Arrangement = arrangement;
            IsRotate = isRotate;
            IsWhite = isWhite;
            TimeToMove = timeToMove;
        }
    }
}   