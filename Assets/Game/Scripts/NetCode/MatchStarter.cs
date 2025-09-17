using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Board;
using Board.Piece;
using Game.Scripts.Matchmaking;
using Setting;
using Statistics;
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

public class MatchStarter : NetworkBehaviour
{
    private static ArrangementEntryArrayWithId _player0;
    private static ArrangementEntryArrayWithId _player1;

    [SerializeField] private MatchCore matchCore;

    public void SetMatchCore(MatchCore matchCore)
    {
        this.matchCore = matchCore;
    }

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
        if ((!IsOwner || !IsLocalPlayer) && _gameData.Mode != GameMode.SinglePlayVsBot) return;
            Debug.Log("MatchStarter Start");
        
        var gameMode = _gameData.Mode;
        var myArrangements = _global.MyArrangements;
        Debug.Log("myArrangements.Count" + myArrangements.Count);

        var arrangementsArray = myArrangements.ToArray();
        var arrangementEntryArray = new ArrangementEntryArray
        {
            ArrangementEntry = new ArrangementEntry[arrangementsArray.Length]
        };

        for (var index = 0; index < arrangementsArray.Length; index++)
        {
            arrangementEntryArray.ArrangementEntry[index] = arrangementsArray[index];
        }
        Debug.Log("ArrangementEntry.Length" + arrangementEntryArray.ArrangementEntry.Length);

        Debug.Log("gameMode" + gameMode);
        switch (gameMode)
        {
            case GameMode.Online:
            case GameMode.Offline:
            {
                var id = OwnerClientId;
                SendConnectedDataRpc(id, _global.BackendManager.MyData.ID, _gameData.TimeControl, arrangementEntryArray.ArrangementEntry);
                break;
            }
            case GameMode.Reconnect:
                GetReConnectDataRpc(OwnerClientId, _global.BackendManager.MyData.ID);
                break;
            case GameMode.SinglePlayVsBot:
                StartMatchVsBot();
                break;
            case GameMode.MigrateHost: 
                break;
        }
    }

    [Rpc(SendTo.Server)]
    private void SendConnectedDataRpc(ulong playerId, string firestoreId, float timeControl, ArrangementEntry[] arrangement,
        RpcParams rpcParams = default)
    {
        Debug.Log("SendConnectedDataRpc " + arrangement.Length);
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

        Debug.Log("SendConnectedDataRpc2 " + arrangement.Length);
        if (_player1 != null && _player0 != null)
        {
            _advancedMatchmaking = FindAnyObjectByType<AdvancedMatchmaking>();
            _advancedMatchmaking.CancelMatchmaking(false);
            
            var whitePlayerId = GetWhitePlayerId(_player0.ID, _player1.ID);

            Debug.Log("_player1 " + _player1.Arrangement.ArrangementEntry.Length + " _player0 " + _player0.Arrangement.ArrangementEntry.Length);
            Debug.Log("SendConnectedDataRpc3 " + arrangement.Length);
            _matchData = CreateMatchData(
                _player0.ID, firestoreId, _player0.Arrangement.ArrangementEntry, timeControl,
                _player1.ID, firestoreId, _player1.Arrangement.ArrangementEntry, timeControl,
                whitePlayerId, timeControl);
            Debug.Log("1MatchData" + _matchData.Player1.StartArrangement.Length + "" + _matchData.Player2.StartArrangement.Length);
            _gameData.ActiveBoard.ArrangeFigures(_matchData);
            
            _ = StartMatchServer(_matchData);
            

            var matchBootstraps = FindObjectsByType<MatchStarter>((FindObjectsSortMode)FindObjectsInactive.Exclude);

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
            
            _player0 = null;
            _player1 = null;
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

        Debug.Log("2MatchData" + _matchData.Player1.StartArrangement + "" + _matchData.Player2.StartArrangement);
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
        _global.BackendManager.LoadPlayerData(matchData.Player1.FirebasePlayer.ID, (_,result) =>
        {
            matchData.Player1.FirebasePlayer = new FirebasePlayerData(result.ID, result.Name,
                new PlayerRankingData { Elo = result.PlayerRanking.Elo, Position = result.PlayerRanking.Position },
                result.Icon, result.HistoryMatchIDs, result.FriendIds);

            core.RefreshPlayerUI(matchData.Player1.PlayerId, matchData.Player1.PlayerId != OwnerClientId);
        });
        _global.BackendManager.LoadPlayerData(matchData.Player2.FirebasePlayer.ID, (_,result) =>
        {
            matchData.Player2.FirebasePlayer = new FirebasePlayerData(result.ID, result.Name,
                new PlayerRankingData { Elo = result.PlayerRanking.Elo, Position = result.PlayerRanking.Position },
                result.Icon, result.HistoryMatchIDs, result.FriendIds);
            
            core.RefreshPlayerUI(matchData.Player2.PlayerId, matchData.Player2.PlayerId != OwnerClientId);
        });
    }

    private void TryArrangeFigures(MatchData matchData)
    {
        var board = _gameData.ActiveBoard;
        Debug.Log("TryArrangeFigures" + matchData.Player1.StartArrangement + "" + matchData.Player2.StartArrangement);
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
        ulong playerId, string firestoreId, ArrangementEntry[] startArrangement, float timeControl,
        ulong playerId2, string firestoreId2, ArrangementEntry[] startArrangement2, float timeControl2,
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
                StartArrangement = startArrangement,
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
                StartArrangement = startArrangement2,
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
        _ = _global.BackendManager.RealtimeDatabase.ReConnectRequestsManager.SendReConnectRequest(enemyPlayerId, _gameData.RelayJoinCode);
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
        var matchBootstraps = FindObjectsByType<MatchStarter>((FindObjectsSortMode)FindObjectsInactive.Exclude);
        var connectedPlayerBootstrap =
            matchBootstraps[0].OwnerClientId == connectedPlayerId ? matchBootstraps[0] : matchBootstraps[1];
        if(connectedPlayerBootstrap.OwnerClientId != connectedPlayerId)
            connectedPlayerBootstrap.NetworkObject.ChangeOwnership(connectedPlayerId);
        var serverCore = GetServerCore(matchCores);
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
            whitePlayerId, startTimeControl);
        
        Debug.Log($"whitePlayerId {whitePlayerId} remainingPlayerId {hostPlayerId}");
        
        GetMoves(out var from, out var to);
        connectedPlayerBootstrap.SendReMovesRpc(from.ToArray(), to.ToArray());
        connectedPlayerBootstrap.SetTimeControlRpc(hostTimeToMove, connectedTimeToMove);
        connectedPlayerBootstrap.SetMovingPlayerIdRpc(movingPlayerId);
        
        Debug.Log("Player1Id" + _matchData.Player1.PlayerId + "Player2Id" + _matchData.Player2.PlayerId );
        
    }

    private MatchCore GetServerCore(MatchCore[] matchCores)
    {
        foreach (var core in matchCores)
        {
            if (core.IsServerCore)
                return core;
        }

        return null;
    }

    private MatchStarter GetServerBootstrap()
    {
        var matchBootstraps = FindObjectsByType<MatchStarter>((FindObjectsSortMode)FindObjectsInactive.Exclude);
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


    #region SinglePlayer

    private void StartMatchVsBot()
    {
        Debug.Log("StartMatchVsBot");
        var firestoreId = _global.BackendManager.MyData.ID;
        var myArrangements = _global.MyArrangements;
        var arrangementsArray = myArrangements.ToArray();
        var whitePlayerId = GetWhitePlayerId(0, 1);
        var botArrangement = GetRandomArrangement();
        
        var playerArrangement = new ArrangementEntryArray
        {
            ArrangementEntry = new ArrangementEntry[arrangementsArray.Length]
        };

        for (var index = 0; index < arrangementsArray.Length; index++)
        {
            playerArrangement.ArrangementEntry[index] = arrangementsArray[index];
        }
        
        _matchData = CreateMatchData(
            0, firestoreId, playerArrangement.ArrangementEntry, 1000,
            1, "-1", botArrangement, 1000,
            whitePlayerId, -1);

        var corePlayer = Instantiate(matchCore, transform);
        
        corePlayer.Init(_matchData);
        corePlayer.SetServerCore(corePlayer);
        corePlayer.SetIsLocal(true);
        
        UpdateMatchDataSingleplayer(_matchData, corePlayer);
        
        TryArrangeFigures(_matchData);
        StartBot(_matchData, corePlayer);
    }

    private void UpdateMatchDataSingleplayer(MatchData matchData, MatchCore core)
    {
        UpdateBotData();
        var mydata = _global.BackendManager.GetSavedPlayer(_global.BackendManager.MyData.ID);
        var newPlayerElo = mydata.Ranking.Data.Elo;
        var myIcon = mydata.PlayerData.Data.Icon; 
        _matchData.Player1.FirebasePlayer.Name = mydata.PlayerData.Data.Name;
        _matchData.Player1.FirebasePlayer.PlayerRanking.Elo = newPlayerElo;
        _matchData.Player1.FirebasePlayer.Icon = myIcon;

        UpdateBotData();
        core.RefreshPlayerUI(matchData.Player1.PlayerId, false);
        core.RefreshPlayerUI(matchData.Player2.PlayerId, true);
    }

    private void UpdateBotData()
    {
        var player = _matchData.Player2;
        
        player.FirebasePlayer = new FirebasePlayerData("id");
        player.FirebasePlayer.Icon = _global.BotIcons[_gameData.BotDifficulty];
        player.FirebasePlayer.Name = $"Bot {_gameData.BotDifficulty.ToString()}";
        player.FirebasePlayer.PlayerRanking = new PlayerRankingData();
        player.FirebasePlayer.PlayerRanking.Elo = 625 * (int)_gameData.BotDifficulty;
    }
    private void StartBot(MatchData matchData, MatchCore matchCore)
    {
        var botController = FindAnyObjectByType<BotController>();
        matchCore.SetBotController(botController);
        
        botController.InitBotController(matchData, matchCore);
    }

    private ArrangementEntry[] GetRandomArrangement()
    {
        List<AbstractPiece> pieces = GetRandomPieces(); // очікуємо, що [0] = King
        int kingIdx = pieces.FindIndex(p => p.PieceType == PieceType.King);
        if (kingIdx < 0)
            throw new System.Exception("GetRandomPieces() не повернув короля");
        if (kingIdx != 0)
            (pieces[0], pieces[kingIdx]) = (pieces[kingIdx], pieces[0]);

        var cells = new List<(int row, int col)>(24);
        for (int row = 0; row < 8; row++)
        for (int col = 5; col <= 7; col++)
            cells.Add((row, col));

        for (int i = cells.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (cells[i], cells[j]) = (cells[j], cells[i]);
        }

        var kingCandidates = cells.FindAll(c => c.col == 7);
        var kingCell = kingCandidates[UnityEngine.Random.Range(0, kingCandidates.Count)];
        cells.Remove(kingCell);

        int capacity = 1 + cells.Count;
        if (pieces.Count > capacity)
        {
            pieces = pieces.GetRange(0, capacity);
        }

        var result = new List<ArrangementEntry>(pieces.Count);

        result.Add(new ArrangementEntry {
            row = kingCell.row,
            column = kingCell.col,
            pieceType = pieces[0].PieceType
        });

        for (int i = 1; i < pieces.Count; i++)
        {
            var cell = cells[i - 1];
            result.Add(new ArrangementEntry {
                row = cell.row,
                column = cell.col,
                pieceType = pieces[i].PieceType
            });
        }

        return result.ToArray();
    }

    private List<AbstractPiece> GetRandomPieces()
    {
        var difficulty = _gameData.BotDifficulty;
        var botPiecesCost = 0;
        switch (difficulty)
        {
            case BotDifficulty.Easy:
                botPiecesCost = _global.BackendManager.RemoteConfigManager.GetValue(_global.BackendManager.RemoteConfigManager.PiceCostEasyBotKey);
                break;
            case BotDifficulty.Medium:
                botPiecesCost = _global.BackendManager.RemoteConfigManager.GetValue(_global.BackendManager.RemoteConfigManager.PiceCostNormalBotKey);
                break;
            case BotDifficulty.Hard:
                botPiecesCost = _global.BackendManager.RemoteConfigManager.GetValue(_global.BackendManager.RemoteConfigManager.PiceCostHardBotKey);
                break;
            case BotDifficulty.Expert:
                botPiecesCost = _global.BackendManager.RemoteConfigManager.GetValue(_global.BackendManager.RemoteConfigManager.PiceCostExpertBotKey);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return _global.GetRandomPiecesForCost(botPiecesCost);
    }

    #endregion
}   