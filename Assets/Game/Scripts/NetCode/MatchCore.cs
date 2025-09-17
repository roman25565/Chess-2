using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Board;
using Board.Piece;
using Game.Scripts.Board;
using Game.Scripts.Matchmaking;
using Setting;
using Statistics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Zenject;
using Zenject.SpaceFighter;

public enum EndGameType
{
    Null,
    Won,
    Lose,
    Draw,
    Canceled
}

public enum WonReason
{
    Surrender,
    Timeouts,
    Null,
}
public class MatchData
{
    public ulong MovingPlayerId;
    public PlayerData Player1;
    public PlayerData Player2;

    public PlayerData GetPlayerData(ulong id)
    {
        if (Player1.PlayerId == id) return Player1;

        if (Player2.PlayerId == id) return Player2;
        throw new Exception("Player id not found" + id);
    }
    
    public PlayerData GetAnotherPlayerData(ulong id)
    {
        if (Player1.PlayerId != id && Player2.PlayerId != id)
            throw new Exception("Player id not found" + id);
        
        return Player1.PlayerId == id ? Player2 : Player1;
    }
}

public class PlayerData
{
    public ArrangementEntry[] StartArrangement;
    public FirebasePlayerData FirebasePlayer;
    public bool IsMoving;
    public bool IsRotate;
    public bool IsWhite;
    public ulong PlayerId;
    public float TimeToMove;
    public float StartTimeToMove;
}

public class MatchCore : NetworkBehaviour
{
    [Inject] private GameData _gameData;
    [Inject] private Global _global;

    private MatchData _matchData;
    private BotController _botController;
    private ulong _enemyId;
    private bool _gameEnded;
    private bool _isInitialize;
    private float _lastUpdateTime;
    private ulong _myId;
    private bool _oneKingDead;
    private bool _isLocal;

    public void SetBotController(BotController botController)
    {
        _botController = botController;
    }
    public void SetIsLocal(bool value)
    {
        _isLocal = value;
    }
    
    private AbstractPiece _lastKilledPiece;

    public bool IsRotated => _matchData.GetPlayerData(_myId).IsRotate;
    private bool IsWhite => _matchData.GetPlayerData(_myId).IsWhite;

    private ulong GetWhitePlayerId =>
        _matchData.Player1.IsWhite ? _matchData.Player1.PlayerId : _matchData.Player2.PlayerId;

    public bool IsServerCore => _serverCore == this;
    public bool IsLocal { get => _isLocal; }


    private void Awake()
    {
        ProjectContext.Instance.Container.InjectGameObject(gameObject);
    }

    private void Update()
    {
        if (_gameEnded) return;
        if (IsOwner && _isInitialize)
        {
            var playerData = _matchData.GetPlayerData(_matchData.MovingPlayerId);
            playerData.TimeToMove -= Time.deltaTime;
            if (playerData.TimeToMove <= 0)
            {
                _gameEnded = true;
                if (IsServer)
                {
                    var anotherPlayer = _matchData.GetAnotherPlayerData(_matchData.MovingPlayerId);
                    var winerId = anotherPlayer.PlayerId;
                    WonPlayerRpc(winerId, WonReason.Timeouts);;
                }
            }
#if !UNITY_SERVER
            var time = playerData.TimeToMove;

            if (Mathf.Abs(playerData.TimeToMove - _lastUpdateTime) > 0.01f)
            {
                _lastUpdateTime = time;
                MatchUIManager.Instance.SetTime(time, playerData.PlayerId != _myId);
            }
#endif
        }
    }

    public bool IsMyId(ulong id)
    {
        return id == _myId;
    }

    public void RefreshPlayerUI(ulong playerId, bool isEnemyPlayer)
    {
#if !UNITY_SERVER
        if (playerId == _matchData.Player1.PlayerId)
        {
            Debug.Log("UpdateFirebasePlayerData");
            MatchUIManager.Instance.SetPlayerUI(_matchData.Player1.FirebasePlayer, isEnemyPlayer);
        }
        else if (playerId == _matchData.Player2.PlayerId)
        {
            MatchUIManager.Instance.SetPlayerUI(_matchData.Player2.FirebasePlayer, isEnemyPlayer);
            Debug.Log("UpdateFirebasePlayerDataFinish");
        }
#endif
    }

    public void Init(MatchData matchData)
    {
        _matchData = matchData;
        _gameData.ActiveBoard.SetMatchCore(this);
        _myId = OwnerClientId;
        _enemyId = _myId == matchData.Player2.PlayerId ? matchData.Player1.PlayerId : matchData.Player2.PlayerId;
        Debug.Log("_myId" + _myId + "_enemyId" + _enemyId + "1" + matchData.Player1.PlayerId + "2" + matchData.Player2.PlayerId);
        _isInitialize = true;
        MatchUIManager.Instance.Init(_matchData.GetPlayerData(_enemyId), _matchData.GetPlayerData(_myId), this);
    }

    private MatchCore GetServerCore()
    {
        var matchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);
        foreach (var core in matchCores)
        {
            if (core.OwnerClientId == 0)
                return core;
        }

        Debug.LogError("ServerCore not found");
        return null;
    }

    private bool _isConfirm;
    public bool CanMove(Cell from, Cell to)
    {
        if (!_isInitialize)
        {
            Debug.LogError("TryMove called before Initialize");
            return false;
        }

        var myData = _matchData.GetPlayerData(_myId); 
        var board = _gameData.ActiveBoard;
        
        if (!myData.IsMoving || myData.TimeToMove < 0)
            return false;

        Debug.Log("to.Piece != null && to.Piece.OwnerId == OwnerClientId" +(to.Piece != null && to.Piece.OwnerId == OwnerClientId));
        if (to.Piece != null && to.Piece.OwnerId == OwnerClientId && !_isConfirm)
        {
            _isConfirm = true;
            MatchUIManager.Instance.ConfirmSelfCapture((fr,t) =>
                {
                    board.BoardTryMove(fr, t);
                    _isConfirm = false;
                }
                , () => { _isConfirm = false; }, from, to);
            return false;
        }

        return true;
    }

    public void TryMove(Vector2Int from, Vector2Int to)
    {
        if (!IsSpawned && !_isLocal)
        {
            Debug.LogError("NetworkObject not spawned yet!");
            return;
        }

        if (_isLocal)
        {
            _botController.OnMoveChosen(from, to);
        }
        
        
        
        TryMoveRpc(from, to);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(!IsOwner) return; 
        Debug.Log($"MatchCore spawned - IsServer: {IsServer}, IsClient: {IsClient}");
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void DrawClientRpc() //TODO Delete this()  
    {
        Debug.Log("Draw ClientRpc");
        HandleEndGameLogic(-1, EndGameType.Draw, WonReason.Null);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void WonPlayerClientRpc(ulong winnerId, WonReason wonReason)
    {
        var endGameType = winnerId == _myId ? EndGameType.Won : EndGameType.Lose;
        HandleEndGameLogic(Convert.ToInt32(winnerId.ToString()), endGameType, wonReason);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void GameEndedRpc(string historyId)
    {
        OnGameEnded?.Invoke(historyId);
    }
    private UnityEvent<string> OnGameEnded = new ();
    private void HandleEndGameLogic(int winnerId, EndGameType endGameType, WonReason wonReason) // winnerId -1 if is Draw
    {
        if (!IsOwner) return;

        Debug.Log($"HandleEndGameLogic winnerId {winnerId} endGameType {endGameType} wonReason {wonReason}");

        var isFirstPlayer = _matchData.Player1.PlayerId == _myId;
        var myId = _global.BackendManager.MyData.ID;
        var myPlayer = _matchData.GetPlayerData(_myId);
        var enemyPlayer = _matchData.GetPlayerData(_enemyId);
        var board = _gameData.ActiveBoard;
        var history = board.GetHistory();

        CalculateNewEloRatings(winnerId, out var player1Elo, out var player2Elo);
        if (endGameType == EndGameType.Lose || endGameType == EndGameType.Won)
        {
            var myNewElo = isFirstPlayer ? player1Elo : player2Elo;
            _global.BackendManager.PlayerRankingManager.UpdateMyPlayerRanking(myId, new PlayerRankingData{Elo = myNewElo, Position = -1});
            _global.BackendManager.MyData.GetPlayerRanking((ranking) =>
            {
                ranking.Elo = myNewElo;
            });
        }
        
        _gameEnded = true;
        board.EndGame();
        
        _global.BackendManager.StatisticManager.UpdatePlayerStatistics(myId, myPlayer, history, endGameType, wonReason);
        OnGameEnded.AddListener((matchId) =>
        {
            
            _global.EndGameData = new EndGameData(endGameType, wonReason, myPlayer, enemyPlayer,isFirstPlayer ? player1Elo : player2Elo,
                isFirstPlayer ? player2Elo : player1Elo, matchId);
            NetworkManager.Singleton.Shutdown();
            Destroy(global::DontDestroyOnLoad.Instance.gameObject);
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
        });
        if (!IsServerCore) return;

        var firebaseWinnerId = winnerId >= 0 ? _matchData.GetPlayerData((ulong)winnerId).FirebasePlayer.ID : winnerId.ToString();
        _global.BackendManager.SaveMatchHistory(
            firebaseWinnerId,
            myPlayer.FirebasePlayer.ID, player1Elo, myPlayer.StartArrangement,
            enemyPlayer.FirebasePlayer.ID, player2Elo, enemyPlayer.StartArrangement,
            _gameData.ActiveBoard.GetHistory(),
            SendGameEndedRpc
        );
    }
    
    public async Task HandleEndGameLogicLocal(int id)
    {
        _gameEnded = true;
        _gameData.ActiveBoard.EndGame();
        
        await Task.Delay(TimeSpan.FromSeconds(3));
        try
        {
            Debug.Log("HandleEndGameLogicLocal");
            _global.EndGameData = CreateEndGameDataLocal(id);
            Destroy(global::DontDestroyOnLoad.Instance.gameObject);
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Console.WriteLine(e);
            throw;
        }
    }

    private EndGameData CreateEndGameDataLocal(int id)
    {
        var endGameType = EndGameType.Lose;
        var  newPlayerElo = _matchData.Player1.FirebasePlayer.PlayerRanking.Elo;
        var myId = _global.BackendManager.MyData.ID;
        
        if (id == 1)
        {
            endGameType = EndGameType.Won;
            newPlayerElo += 2 * (int)_gameData.BotDifficulty;
        }
        _global.BackendManager.PlayerRankingManager.UpdateMyPlayerRanking(myId, new PlayerRankingData{Elo = newPlayerElo, Position = -1});
        _global.BackendManager.MyData.GetPlayerRanking((ranking) =>
        {
            ranking.Elo = newPlayerElo;
        });
        return new EndGameData(endGameType, WonReason.Null, _matchData.Player1, _matchData.Player2,newPlayerElo,
            _matchData.Player2.FirebasePlayer.PlayerRanking.Elo, null,true);
    }

    [Rpc(SendTo.Server)]
    private void SendGameEndedRpc(string historyId)
    {
        foreach (var allMatchCore in _allMatchCores) allMatchCore.GameEndedRpc(historyId);
    }
#if !UNITY_SERVER
    private void CalculateNewEloRatings(int winnerId, out int player1Elo, out int player2Elo)
    {
        double scope1 = 0;
        double scope2 = 0;
        GetScopes(winnerId, ref scope1, ref scope2);

        var player1RankingElo = _matchData.Player1.FirebasePlayer.PlayerRanking.Elo;
        var player2RankingElo = _matchData.Player2.FirebasePlayer.PlayerRanking.Elo;
        
        player1Elo = GlobalTools.CalculateNewRating(player1RankingElo,
            player1RankingElo, scope1);
        player2Elo = GlobalTools.CalculateNewRating(player2RankingElo,
            player2RankingElo, scope2);

        Debug.Log($"new Elo P1 {player1Elo} P2 {player2Elo}");
    }
#endif
    [Rpc(SendTo.ClientsAndHost)]
    private void UseMoveCommandRpc(Vector2Int from, Vector2Int to, ulong playerId)
    {
        if (IsOwner && IsClient) UseMove(from, to, playerId);
    }

    public void UseMove(Vector2Int from, Vector2Int to, ulong playerId)
    {
        var board = _gameData.ActiveBoard;
        var isRotate = _matchData.GetPlayerData(_matchData.MovingPlayerId).IsRotate;
        var anotherPlayer = _matchData.GetAnotherPlayerData(playerId);
        var killedPiece = board.GetCell(to.x, to.y).Piece;
        
        
        _matchData.GetPlayerData(playerId).IsMoving = false;
        anotherPlayer.IsMoving = true;
        _matchData.MovingPlayerId = anotherPlayer.PlayerId;
        
        _gameData.ActiveBoard.MovePiece(from, to);
        
        Debug.Log("UseMove" + playerId + " " + _myId + "E" + (playerId == _myId));
        if (playerId == _myId) return;

        var movedPiece = board.GetCell(to.x, to.y)?.Piece;
        if ((to.y == 0 || to.y == 7) && movedPiece != null && movedPiece.PieceType == PieceType.Pawn) //Queen Update
        {
            var queen = _global.CreatePiece(PieceType.Queen);
            board.GetCell(to.x, to.y).SetPiece(queen);
        }

        if (_killedKings > 0)
        {
            _serverCore = GetServerCore();
            Debug.Log("[TTT]" + (killedPiece != null && killedPiece.PieceType == PieceType.King));
            if (killedPiece != null && killedPiece.PieceType == PieceType.King)
            {
                _serverCore.DrawRpc();
            }
            else
            {
                Debug.Log("WonPlayerClientRpc _serverCore" + (_serverCore == null));
                _serverCore.WonPlayerRpc(_lastKilledKingPlayerID, WonReason.Null); 
            }
        }

        if (killedPiece != null)
        {
            
            DeathRattle(killedPiece);
        }

        return;

        void DeathRattle(AbstractPiece piece)
        {
            Debug.Log("DeathRattle" + piece.PieceType + "local" + _isLocal);
            switch (piece.PieceType)
            {
                case PieceType.Empty:
                case PieceType.Pawn:
                case PieceType.Rook:
                case PieceType.Knight:
                case PieceType.Bishop:
                case PieceType.Queen:
                    break;
                case PieceType.King:
                    if (_isLocal)
                    {
                        HandleEndGameLogicLocal((int)piece.OwnerId);
                    }
                    OnKingDeath(_enemyId);
                
                    Debug.Log("DeathRattle Kings");
                    //TODO перевірити чи у ворога є фігури якими він може походити якщо ні перемогти завершити матч
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        // if (!IsServer && !IsHost) return;
        // if (_killedKings == 0) return;
        //
        // if (_killedKings == 1)
        // {
        //     var winerId = _matchData.GetAnotherPlayerData(_lastKilledKingPlayerID).PlayerId;
        //     WonPlayer(winerId, WonReason.Null);
        // }
        // else if(_killedKings == 2)
        // {
        //     Draw();
        // }

    }

    #region Server

    private MatchCore _serverCore;
    private List<MatchCore> _allMatchCores;

    public void SetServerMatchData(MatchData matchData)
    {
        _matchData = matchData;
    }

    public void UpdateServerData()
    {
       // GetMigratedMatchData();
       var advancedMatchmaking = FindAnyObjectByType<AdvancedMatchmaking>();
       _matchData = advancedMatchmaking.GetMigretedMatchData();
    }

    public void SetServerCore(MatchCore serverCore)
    {
        _serverCore = serverCore;

        if (IsServerCore)
        {
            _allMatchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude).ToList();
        }
    }

    public bool AddCore(MatchCore core)
    {
        if (core == null)
        {
            Debug.LogError("Null core passed to AddCore");
            return false;
        }

        _allMatchCores.Add(core);
        return true;
    }

    [Rpc(SendTo.Server)]
    private void TryMoveRpc(Vector2Int from, Vector2Int to, RpcParams rpcParams = default)
    {
        Debug.Log("TryMoveRpc");
        _serverCore.TryMoveServer(from, to, rpcParams);
    }

    private void TryMoveServer(Vector2Int from, Vector2Int to, RpcParams rpcParams)
    {
        if (IsClient && !IsHost)
        {
            Debug.LogError("Client in [Rpc(SendTo.Server)]public void TryMoveRps()");
            return;
        }

        var playerId = rpcParams.Receive.SenderClientId;
        var playerData = _matchData.GetPlayerData(playerId);
        if (!playerData.IsMoving)
        {
            Debug.LogError("isNotValidMoving");
            return;
        }

        if (playerData.TimeToMove <= 0)
        {
            Debug.LogError("TimeToMove is negative");
            return;
        }

        if (OwnerClientId != playerId)
        {
            if (!_gameData.ActiveBoard.IsValidMove(from, to))
            {
                Debug.LogError("move is not valid");
                return;
            }
        }
        
        // UseMove(from, to, playerId);
        SendToPlayersMove(from, to, playerId);
    }


    private void OnKingDeath(ulong id)
    {
        _serverCore = GetServerCore();
        _serverCore.OnKingDeathRPC(id);
    }
    
    [Rpc(SendTo.Server)]
    private void OnKingDeathRPC(ulong id)
    {
        foreach (var core in _allMatchCores) core.OnKingDeathClientRpc(id);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OnKingDeathClientRpc(ulong id)
    {
        if (!IsOwner) return;

        _killedKings += 1;
        _lastKilledKingPlayerID = id;
    }

    private int _killedKings;
    private ulong _lastKilledKingPlayerID;

    [Rpc(SendTo.Server)]
    private void WonPlayerRpc(ulong winnerId, WonReason wonReason)
    {
        foreach (var allMatchCore in _allMatchCores) allMatchCore.WonPlayerClientRpc(winnerId, wonReason);
    }
    [Rpc(SendTo.Server)]
    private void DrawRpc()
    {
        foreach (var allMatchCore in _allMatchCores) allMatchCore.DrawClientRpc();
    }

    private void GetScopes(int winnerId, ref double player1Score, ref double player2Score)
    {
        if (winnerId < 0)
        {
            player1Score = 0.5;
            player2Score = 0.5;
        }
        else
        {
            var id = (ulong)winnerId;

            if (id == _matchData.Player1.PlayerId)
            {
                player1Score = 1.0;
                player2Score = 0.0;
            }
            else if (id == _matchData.Player2.PlayerId)
            {
                player1Score = 0.0;
                player2Score = 1.0;
            }
            else
            {
                Debug.LogError("GetScopes Player Id Not Valid " + id + "expected value is" + _matchData.Player1.PlayerId + " or " + _matchData.Player2.PlayerId);;
            }
        }
    }

    private void SendToPlayersMove(Vector2Int from, Vector2Int to, ulong playerId)
    {
        Debug.Log($"_allMatchCores count {_allMatchCores.Count}, {_allMatchCores[0].IsServerCore}");
        foreach (var allMatchCore in _allMatchCores) allMatchCore.UseMoveCommandRpc(from, to, playerId);
    }

    [Rpc(SendTo.Server)]
    public void TrySurrenderRpc(RpcParams rpcParams = default)
    {
        var senderId = rpcParams.Receive.SenderClientId;
        var winer = _matchData.GetAnotherPlayerData(senderId);
        _serverCore = GetServerCore();
        _serverCore.WonPlayerRpc(winer.PlayerId, WonReason.Surrender);
    }

    [Rpc(SendTo.Server)]
    public void TryCancelMatchRpc()
    {
        var senderId = OwnerClientId;
        var anotherPlayerId = _matchData.GetAnotherPlayerData(senderId).PlayerId;

        //TODO
    }

    #endregion


    [Rpc(SendTo.ClientsAndHost)]
    public void ChangeDataIPRpc(ulong oldId, ulong clientId, string firestoreId)
    {
        ChangeDataIP(oldId, clientId);
    }

    private void ChangeDataIP(ulong oldId, ulong newClientId)
    {
        if (_gameEnded || !IsOwner) return;
        if(oldId == newClientId) return;
        if (oldId == newClientId) return;
        Debug.Log($"ChangeDataIP, OldId {oldId}, NewId {newClientId}");

        if (oldId != _matchData.Player1.PlayerId && oldId != _matchData.Player2.PlayerId)
        {
            Debug.LogError($"PlayerID Not Found, IsServer{IsServer}, IsServerCore {IsServerCore}, IsHost{IsHost}, IsClient{IsClient} Pl1 {_matchData.Player1.PlayerId},  Pl2 {_matchData.Player2.PlayerId} OldId {oldId}, NewId {newClientId}, OwnerId {OwnerClientId}");
            return;
        }

        ChangePlayerDataIP(oldId, newClientId);
        ChangeAllPieceIP(oldId, newClientId);

        return;

        void ChangePlayerDataIP(ulong oldId, ulong newId)
        {
            if (_matchData.MovingPlayerId == oldId)
                _matchData.MovingPlayerId = newId;
            var oldPlayer = _matchData.Player1.PlayerId == oldId ? _matchData.Player1 : _matchData.Player2;
            oldPlayer.PlayerId = newId;
        }

        void ChangeAllPieceIP(ulong oldId, ulong newId)
        {
            _gameData.ActiveBoard.UpdatePiecesId(oldId, newId);
        }
    }


    public void GetReconnectData(ulong connectedPlayerId, ulong remainingPlayerId, out float connectedTimeToMove,
        out float remainingTimeToMove,out float startTimeControl, out ulong movingPlayerId, out ulong whitePlayerId,
        out ArrangementEntry[] connectedArrangement,
        out ArrangementEntry[] hostArrangement)
    {
        Debug.Log($"_matchData is null {_matchData == null}, connectedPlayerId  {connectedPlayerId}, remainingPlayerId {remainingPlayerId}");
        Debug.Log($"Pl1 {_matchData.Player1.PlayerId}, Pl2 {_matchData.Player2.PlayerId}");
        
        var connectedPlayer = _matchData.GetPlayerData(connectedPlayerId);
        var hostPlayer = _matchData.GetPlayerData(remainingPlayerId);
        connectedTimeToMove = connectedPlayer.TimeToMove;
        remainingTimeToMove = hostPlayer.TimeToMove;
        startTimeControl = hostPlayer.StartTimeToMove;
        movingPlayerId = _matchData.MovingPlayerId;
        whitePlayerId = GetWhitePlayerId;

        connectedArrangement = connectedPlayer.StartArrangement;
        hostArrangement = hostPlayer.StartArrangement;
    }

    public MatchData GetMatchData()
    {
        return _matchData;
    }

    public string GetFirestoreId(ulong playerId)
    {
        Debug.Log("GetFirestoreId");
        Debug.Log(playerId);
        return _matchData.Player1.PlayerId == playerId
            ? _matchData.Player1.FirebasePlayer.ID
            : _matchData.Player2.FirebasePlayer.ID;
    }

    public void SetEnemyStartArrangement(ArrangementEntry[] arrangement)
    {
        _matchData.GetPlayerData(_enemyId).StartArrangement = arrangement;
    }

    #region Draw

    [Rpc(SendTo.Server)]
    public void TryOfferDrawRpc(RpcParams rpcParams = default)
    {
        var senderId = rpcParams.Receive.SenderClientId;
        var anotherPlayerId = _matchData.GetAnotherPlayerData(senderId).PlayerId;
        
        MatchCore targetPlayerCore = null;
        foreach (var core in _allMatchCores)
        {
            if (core.OwnerClientId == anotherPlayerId)
            {
                targetPlayerCore = core;
                break;
            }
        }

        if (targetPlayerCore == null)
        {
            throw new Exception("targetPlayerCore is null player Id: " + anotherPlayerId);
        }

        targetPlayerCore.OnAnotherPlayerWantsDrawRpc();
    }
    
    private void OnAnotherPlayerWantsDrawRpc()
    {
        MatchUIManager.Instance.OnAnotherPlayerWantsDrawRpc();
    }

    public void AcceptAnotherPlayerWantsDrawRpc()
    {
        _serverCore = GetServerCore();
        _serverCore.DrawRpc();
    }

    #endregion

    public ulong GetMovingPlayerId()
    {
        return _matchData.MovingPlayerId;
    }

    public void DestroyDisconnectedCore()
    {
        Debug.Log(
            $"core id 1 {_allMatchCores[0].OwnerClientId}, 2 {_allMatchCores[1].OwnerClientId} , {_allMatchCores[0].IsServerCore}, {_allMatchCores[1].IsServerCore}");
        var disconnectedCore = _allMatchCores[0].IsServerCore ? _allMatchCores[1] : _allMatchCores[0];

        _allMatchCores.Remove(disconnectedCore);
        NetworkObject networkObject = disconnectedCore.GetComponent<NetworkObject>();
        networkObject.Despawn();
    }

    public void SetMovingPlayerId(ulong movingPlayerId)
    {
        _matchData.MovingPlayerId = movingPlayerId;

        _matchData.Player1.IsMoving = false;
        _matchData.Player2.IsMoving = false;

        _matchData.GetPlayerData(movingPlayerId).IsMoving = true;
    }

    public void SetTimeControl(float hostTimeToMove, float myTimeToMove)
    {
        Debug.Log($"SetTimeControl host {hostTimeToMove}, my {myTimeToMove}");
        _matchData.GetAnotherPlayerData(_myId).TimeToMove = hostTimeToMove;
        _matchData.GetPlayerData(_myId).TimeToMove = myTimeToMove;

        MatchUIManager.Instance.SetTime(hostTimeToMove, true);
        MatchUIManager.Instance.SetTime(myTimeToMove, false);

    }

    [Rpc(SendTo.ClientsAndHost)]
    public void OnHostMigratedRpc(ulong oldId, ulong newId)
    {
        var matchData = GetMigratedMatchData(oldId, newId);
        Init(matchData);
        Debug.Log($"[Host Migrated] Player Data:\n" +
                  $"P1: NetworkID={matchData.Player1.PlayerId} | FirebaseID={matchData.Player1.FirebasePlayer?.ID ?? "null"} | IsMoving={matchData.Player1.IsMoving} | Color={(matchData.Player1.IsWhite ? "White" : "Black")}\n" +
                  $"P2: NetworkID={matchData.Player2.PlayerId} | FirebaseID={matchData.Player2.FirebasePlayer?.ID ?? "null"} | IsMoving={matchData.Player2.IsMoving} | Color={(matchData.Player2.IsWhite ? "White" : "Black")}\n" +
                  $"Current Moving Player: {(matchData.MovingPlayerId == matchData.Player1.PlayerId ? "P1" : "P2")}\n" +
                  $"Time Remaining: P1={matchData.Player1.TimeToMove:F1}s | P2={matchData.Player2.TimeToMove:F1}s");
        LogOnHostMigratedRpc();
    }

    [Rpc(SendTo.Server)]
    private void LogOnHostMigratedRpc()
    {
        Debug.Log($"[Host Migrated Server] Player Data:\n" +
                  $"P1: NetworkID={_matchData.Player1.PlayerId} | FirebaseID={_matchData.Player1.FirebasePlayer?.ID ?? "null"} | IsMoving={_matchData.Player1.IsMoving} | Color={(_matchData.Player1.IsWhite ? "White" : "Black")}\n" +
                  $"P2: NetworkID={_matchData.Player2.PlayerId} | FirebaseID={_matchData.Player2.FirebasePlayer?.ID ?? "null"} | IsMoving={_matchData.Player2.IsMoving} | Color={(_matchData.Player2.IsWhite ? "White" : "Black")}\n" +
                  $"Current Moving Player: {(_matchData.MovingPlayerId == _matchData.Player1.PlayerId ? "P1" : "P2")}\n" +
                  $"Time Remaining: P1={_matchData.Player1.TimeToMove:F1}s | P2={_matchData.Player2.TimeToMove:F1}s");
    }

    private MatchData GetMigratedMatchData(ulong oldId, ulong newId)
    {
        var advancedMatchmaking = FindAnyObjectByType<AdvancedMatchmaking>();

        var matchData = advancedMatchmaking.GetMigretedMatchData();
        Debug.Log($"Core OnHostMigratedRpc Pl1 {matchData.Player1.PlayerId}, Pl2 {matchData.Player2.PlayerId}");
        _matchData = matchData;
        
        ChangeDataIP(0, 404);   //404 random big temporary value at the time of migration
        ChangeDataIP(oldId, 0); //0 new Host Id
        ChangeDataIP(404, 1);   //1 new client Id
        _myId = OwnerClientId;
        _enemyId = matchData.GetAnotherPlayerData(_myId).PlayerId;
        Debug.Log("Server _myId" + _myId + "_enemyId" + _enemyId + "1" + matchData.Player1.PlayerId + "2" + matchData.Player2.PlayerId);
        
        return matchData;
    }
}