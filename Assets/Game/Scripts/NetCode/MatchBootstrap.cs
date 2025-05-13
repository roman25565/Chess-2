using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            var whitePlayerId = GetWhitePlayerId(_player0.ID, _player1.ID);

            var player1 = CreatePlayerBootstrapData(_player0.ID, firestoreId, _player0.Arrangement.ArrangementEntry,
                whitePlayerId, timeControl);
            var player2 = CreatePlayerBootstrapData(_player1.ID, firestoreId, _player1.Arrangement.ArrangementEntry,
                whitePlayerId, timeControl);

            _gameData.ActiveBoard.ArrangeFigures(player1, player2);

            _ = StartMatchServer(player1, player2, whitePlayerId, timeControl);

            var matchBootstraps = FindObjectsByType<MatchBootstrap>((FindObjectsSortMode)FindObjectsInactive.Exclude);

            foreach (var matchBootstrap in matchBootstraps)
                matchBootstrap.SendToClientPlayerBootstrapDataRpc(
                    _player0.ID,
                    _player0.FirestoreId,
                    _player0.Arrangement.ArrangementEntry,
                    _player1.ID,
                    _player1.FirestoreId,
                    _player1.Arrangement.ArrangementEntry,
                    whitePlayerId,
                    timeControl);
        }
    }

    private Task StartMatchServer(PlayerBootstrapData player1, PlayerBootstrapData player2, ulong whitePlayerId, int timeControl)
    {
        var coreServer = Instantiate(matchCore);

        var corePlayer1 = Instantiate(matchCore, transform);
        corePlayer1.GetComponent<NetworkObject>().SpawnWithOwnership(player1.PlayerId);
        corePlayer1.SetServerCore(coreServer);

        var corePlayer2 = Instantiate(matchCore, transform);
        corePlayer2.GetComponent<NetworkObject>().SpawnWithOwnership(player2.PlayerId);
        corePlayer2.SetServerCore(coreServer);

        coreServer.GetComponent<NetworkObject>().Spawn();

        var matchData = CreateMatchData(player1, player2, whitePlayerId, timeControl);

        coreServer.Init(matchData);
        coreServer.SetServerCore(coreServer);
        return Task.CompletedTask;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendToClientPlayerBootstrapDataRpc(
        ulong playerId, string firestoreId, ArrangementEntry[] arrangement,
        ulong playerId2, string firestoreId2, ArrangementEntry[] arrangement2,
        ulong whitePlayerId, int timeControl)
    {
        if (!IsLocalPlayer) return;
        var player1 = new PlayerBootstrapData(playerId, firestoreId, arrangement, playerId != whitePlayerId,
            whitePlayerId == playerId);
        var player2 = new PlayerBootstrapData(playerId2, firestoreId2, arrangement2, playerId2 != whitePlayerId,
            whitePlayerId == playerId2);

        InitCore(player1, player2, whitePlayerId, timeControl);
        TryArrangeFigures(player1, player2);
    }

    private void InitCore(PlayerBootstrapData player1, PlayerBootstrapData player2, ulong whitePlayerId, int timeControl)
    {
        var allMatchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);

        foreach (var core in allMatchCores)
        {
            if (!core.IsOwner) continue;

            var matchData = CreateMatchData(player1, player2, whitePlayerId, timeControl);
            
            core.Init(matchData);
            LoadFirestoreRefreshUI(player1, player2, matchData, core);
            Debug.Log("matchCore.Init(matchData);");
        }
    }


    private void InitCore(ReconnectData reconnectData)
    {
        var allMatchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);

        foreach (var core in allMatchCores)
        {
            if (!core.IsOwner) continue;

            var matchData = CreateMatchData(reconnectData.Player1, reconnectData.Player2, reconnectData.WhitePlayerId, 404);
            
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
#if !UNITY_SERVER
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
#endif
    }

    private void TryArrangeFigures(PlayerBootstrapData player1, PlayerBootstrapData player2)
    {
        if(_gameData.Mode == GameMode.Reconnect) return;
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

    private MatchData CreateMatchData(PlayerBootstrapData player1, PlayerBootstrapData player2, ulong whitePlayerId, int timeControl)
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
                Arrangement = player1.Arrangement,
                TimeToMove = timeControl * 60f
            },
            Player2 = new PlayerData
            {
                FirebasePlayer = new FirebasePlayerData(player1.FirestoreId),
                PlayerId = player2.PlayerId,
                IsMoving = player2.PlayerId == whitePlayerId,
                IsRotate = player2.IsRotate,
                IsWhite = player2.IsWhite,
                Arrangement = player2.Arrangement,
                TimeToMove = timeControl * 60f
            }
        };
    }

    private PlayerBootstrapData CreatePlayerBootstrapData(ulong playerId, string firestoreId,
        ArrangementEntry[] arrangement, ulong whitePlayerId, int timeControl)
    {
        return new PlayerBootstrapData(playerId, firestoreId, arrangement, whitePlayerId != playerId,
            whitePlayerId == playerId);
    }

    private ulong GetWhitePlayerId(ulong playerId, ulong playerId2)
    {
        Debug.Log("GetWhitePlayerId");
        Debug.Log("playerId" + playerId);
        Debug.Log("playerId2" + playerId2);
        var randomValue = Random.value;
        Debug.Log("randomValue" + randomValue);
        var randomIndex = randomValue < 0.5 ? 1 : 2;

        return randomIndex == 1 ? playerId : playerId2;
    }

    [Rpc(SendTo.Server)]
    private void GetReConnectDataRpc(ulong connectedPlayerId, string firestoreId, RpcParams rpcParams = default)
    {
        var board = _gameData.ActiveBoard;
        var matchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);
        
        if (matchCores.Length != 2) 
        {
            Debug.Log("matchCores.Length" + matchCores.Length);
            throw new ArgumentOutOfRangeException(nameof(matchCores));
        }

        var remainingPlayerCore = matchCores[0].IsServerCore ? matchCores[1] : matchCores[0];
        var remainingPlayerId = remainingPlayerCore.OwnerClientId;
        var oldId = (ulong)(remainingPlayerId == 1 ? 2 : 1);
        
        var connectedPlayerCore = Instantiate(matchCore, transform);
        var matchBootstraps = FindObjectsByType<MatchBootstrap>((FindObjectsSortMode)FindObjectsInactive.Exclude);
        var remainingPlayerBootstrap = matchBootstraps[0].OwnerClientId == remainingPlayerId
            ? matchBootstraps[0]
            : matchBootstraps[1];
        var connectedPlayerBootstrap =
            matchBootstraps[0].OwnerClientId == connectedPlayerId ? matchBootstraps[0] : matchBootstraps[1];

        
        var serverCore = GetServerCore();
        if (serverCore == null) throw new ArgumentOutOfRangeException(nameof(serverCore));
        var remainingPlayerFirestoreId = serverCore.GetFirestoreId(remainingPlayerId); 
        
        foreach (var core in matchCores)
            core.OnClientReConnectRpc(oldId, connectedPlayerId, firestoreId);
        serverCore.OnClientReConnect(oldId, connectedPlayerId);
        
        serverCore.AddCore(connectedPlayerCore);
        connectedPlayerCore.GetComponent<NetworkObject>().SpawnWithOwnership(connectedPlayerId);
        connectedPlayerCore.SetServerCore(serverCore);
        
        serverCore.GetMatchData(connectedPlayerId, remainingPlayerId,out float connectedTimeToMove, out float remainingTimeToMove, out ulong movingPlayerId,
            out ulong whitePlayerId);
        
        board.GetPiecesInBoard(connectedPlayerId, remainingPlayerId, out var connectedPlayerPieces, out var remainingPlayerPieces);
        Debug.Log("connectedPlayerPieces" + connectedPlayerPieces.Length);
        Debug.Log("remainingPlayerPieces" + remainingPlayerPieces.Length);
        Debug.Log("Ecuals" + (connectedPlayerPieces[0] == remainingPlayerPieces[0]));;
        connectedPlayerBootstrap.SendReConnectDataRpc(connectedTimeToMove, remainingTimeToMove, movingPlayerId, whitePlayerId, connectedPlayerPieces, remainingPlayerPieces, remainingPlayerId, remainingPlayerFirestoreId);
        
        remainingPlayerBootstrap.GetStartArrangementRpc();

        return;

        MatchCore GetServerCore()
        {
            MatchCore serverCore1 = null;
            foreach (var core in matchCores)
            {
                if (!core.IsServerCore) continue;
                serverCore1 = core;
                break;
            }

            return serverCore1;
        }
    }



    [Rpc(SendTo.ClientsAndHost)]
    public void GetStartArrangementRpc()
    {
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

#if !UNITY_SERVER

        var id = OwnerClientId;
        Debug.Log("GetArrangementsRpc: " + NetworkManager.Singleton.GetInstanceID());
        ReSendStartArrangementRpc(id, _global.FirestoreManager.PlayerData.ID, arrangementEntryArray.ArrangementEntry);
#endif
    }
    
    [Rpc(SendTo.Server)]
    public void ReSendStartArrangementRpc(ulong targetPlayerId, string firestoreId, ArrangementEntry[] arrangement,
        RpcParams rpcParams = default)
    {
        var matchBootstraps = FindObjectsByType<MatchBootstrap>((FindObjectsSortMode)FindObjectsInactive.Exclude);
        var remainingPlayerBootstrap= matchBootstraps[0].OwnerClientId == targetPlayerId ? matchBootstraps[0] : matchBootstraps[1];
        remainingPlayerBootstrap.SendStartArrangementRpc(targetPlayerId, firestoreId, arrangement);
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
   
    [Rpc(SendTo.ClientsAndHost)]
    private void SendReConnectDataRpc(
        float myTimeToMove,
        float enemyTimeToMove,
        ulong movingPlayerId,
        ulong whitePlayerId,
        ArrangementEntry[] myPieces,
        ArrangementEntry[] enemyPieces,
        ulong enemyId, string enemyFirestoreId,
        RpcParams rpcParams = default)
    {
        if (!IsLocalPlayer) return;
        
        Debug.Log("===== Мої фігури =====");
        for (int i = 0; i < myPieces.Length; i++)
        {
            Debug.Log($"Фігура {i}: Тип: {myPieces[i].pieceType}, Рядок: {myPieces[i].row}, Колонка: {myPieces[i].column}");
        }

        Debug.Log("===== Фігури противника =====");
        for (int i = 0; i < enemyPieces.Length; i++)
        {
            Debug.Log($"Фігура {i}: Тип: {enemyPieces[i].pieceType}, Рядок: {enemyPieces[i].row}, Колонка: {enemyPieces[i].column}");
        }
        
        var imWhite = enemyId != whitePlayerId;
#if !UNITY_SERVER
        var myPlayer = new PlayerBootstrapData(OwnerClientId, _global.FirestoreManager.PlayerData.ID, myPieces,!imWhite, imWhite);
        var enemyPlayer = new PlayerBootstrapData(enemyId, enemyFirestoreId, enemyPieces, imWhite, !imWhite);
        var board = _gameData.ActiveBoard;
        
        _reconnectData = new ReconnectData
        {
            MyTimeToMove = myTimeToMove,
            EnemyTimeToMove = enemyTimeToMove,
            MovingPlayerId = movingPlayerId,
            WhitePlayerId = whitePlayerId,
            Player1 = myPlayer,
            Player2 = enemyPlayer
        };

        board.ArrangeFigures(myPlayer, enemyPlayer, false);
        
#endif
        InitCore(_reconnectData);
    }
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

        public readonly ulong PlayerId;

        public PlayerBootstrapData(ulong playerId, string firestoreId, ArrangementEntry[] arrangement, bool isRotate,
            bool isWhite)
        {
            PlayerId = playerId;
            FirestoreId = firestoreId;
            Arrangement = arrangement;
            IsRotate = isRotate;
            IsWhite = isWhite;
        }
    }
}   