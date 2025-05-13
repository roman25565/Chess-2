using System;
using System.Collections.Generic;
using System.Linq;
using Board.Piece;
using Setting;
using Unity.Netcode;
using UnityEngine;
using Zenject;

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
    public ArrangementEntry[] Arrangement;
    public FirebasePlayerData FirebasePlayer;
    public bool IsMoving;
    public bool IsRotate;
    public bool IsWhite;
    public ulong PlayerId;
    public float TimeToMove;
}

public class MatchCore : NetworkBehaviour
{
    [Inject] private GameData _gameData;
    [Inject] private Global _global;
    
    private MatchData _matchData;
    private ulong _enemyId;
    private bool _gameEnded;
    private bool _isInitialize;
    private float _lastUpdateTime;
    private ulong _myId;
    private bool _oneKingDead;

    public bool IsRotated => _matchData.GetPlayerData(_myId).IsRotate;
    public bool IsWhite => _matchData.GetPlayerData(_myId).IsWhite;
    private ulong GetWhitePlayerId => _matchData.Player1.IsWhite ? _matchData.Player1.PlayerId : _matchData.Player2.PlayerId;
    
    public bool IsServerCore => OwnerClientId == 0;

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
                    LosePlayer(winerId);
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
        Debug.Log("playerId" + playerId + "isEnemyPlayer" + isEnemyPlayer);
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
        Debug.Log(_matchData);
        _gameData.ActiveBoard.SetMatchCore(this);
        _myId = OwnerClientId;
        _enemyId = _myId == matchData.Player2.PlayerId ? matchData.Player1.PlayerId : matchData.Player2.PlayerId;
        Debug.Log("_myId" + _myId + "_enemyId" + _enemyId);
        _isInitialize = true;

        if (OwnerClientId == 0)
        {
            _myId = matchData.Player1.PlayerId;
            _enemyId = matchData.Player2.PlayerId;
        }
#if !UNITY_SERVER
        MatchUIManager.Instance.Init(_matchData.GetPlayerData(_enemyId), _matchData.GetPlayerData(_myId), this);
        
#endif
        Debug.Log("MatchCore.Inited");
    }

    public bool CanMove()
    {
        if (!_isInitialize)
        {
            Debug.LogError("TryMove called before Initialize");
            return false;
        }

        var myData = _matchData.GetPlayerData(_myId);

        if (myData.IsMoving || myData.TimeToMove <= 0)
            return true;
        else
        {
            Debug.LogError("Error player cont move");
            return false;
        }
    }

    public void TryMove(Vector2Int from, Vector2Int to)
    {
        CanMove();
        TryMoveRpc(from, to);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void DrawClientRpc()
    {
#if !UNITY_SERVER
        if (!IsOwner) return;
        
        _gameEnded = true;
        _gameData.ActiveBoard.EndGame();
        
        MatchUIManager.Instance.EndGame(_global.EndGameType);
        
        if (!IsWhite) return;
        
        var player1 = _matchData.GetPlayerData(_myId); //player1 always White
        var player2 = _matchData.GetPlayerData(_enemyId);
        
        var player1Elo = _matchData.Player1.FirebasePlayer.Elo;
        var player2Elo = _matchData.Player2.FirebasePlayer.Elo;
        _global.FirestoreManager.SaveMatchHistory(
            "-1",
            player1.FirebasePlayer.ID, player1Elo, player1.Arrangement,
            player2.FirebasePlayer.ID, player2Elo, player2.Arrangement,
            _gameData.ActiveBoard.GetHistory()
        );
#endif
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void LosePlayerClientRpc(ulong winnerId)
    {
        
#if !UNITY_SERVER
        if (!IsOwner) return;

        Debug.Log("LosePlayerClientRpc");
        _gameEnded = true;
        _gameData.ActiveBoard.EndGame();

        CalculateNewEloRatings(winnerId, out var player1Elo, out var player2Elo);
        var isFirstPlayer = _matchData.Player1.PlayerId == _myId;
        var isWon = winnerId == _myId;
        _global.EndGameType = isWon ? EndGameType.Won : EndGameType.Lose;
        MatchUIManager.Instance.EndGame(_global.EndGameType, isFirstPlayer ? player1Elo : player2Elo,
            isFirstPlayer ? player2Elo : player1Elo);

        if (!IsWhite) return;

        _global.FirestoreManager.BdSetElo(_matchData.Player1.FirebasePlayer.ID, player1Elo);
        _global.FirestoreManager.BdSetElo(_matchData.Player2.FirebasePlayer.ID, player2Elo);

        var player1 = _matchData.GetPlayerData(_myId); //player1 always White
        var player2 = _matchData.GetPlayerData(_enemyId);
        _global.FirestoreManager.SaveMatchHistory(
            _matchData.GetPlayerData(winnerId).FirebasePlayer.ID,
            player1.FirebasePlayer.ID, player1Elo, player1.Arrangement,
            player2.FirebasePlayer.ID, player2Elo, player2.Arrangement,
            _gameData.ActiveBoard.GetHistory()
        );
#endif
    }
#if !UNITY_SERVER
    private void CalculateNewEloRatings(ulong winnerId, out int player1Elo, out int player2Elo)
    {
        double scope1 = 0;
        double scope2 = 0;
        GetScopes(winnerId, ref scope1, ref scope2);

        player1Elo = GlobalTools.CalculateNewRating(_matchData.Player1.FirebasePlayer.Elo,
            _matchData.Player1.FirebasePlayer.Elo, scope1);
        player2Elo = GlobalTools.CalculateNewRating(_matchData.Player2.FirebasePlayer.Elo,
            _matchData.Player2.FirebasePlayer.Elo, scope2);
    }
#endif
    [Rpc(SendTo.Everyone)]
    public void UseMoveCommandRpc(Vector2Int from, Vector2Int to, ulong playerId)
    {
        if (IsOwner) UseMove(from, to, playerId);
    }

    private void UseMove(Vector2Int from, Vector2Int to, ulong playerId)
    {
        var board = _gameData.ActiveBoard;
        var cell = board.GetCell(to.x, to.y);
        var killedPiece = cell.Piece;

        _gameData.ActiveBoard.MovePiece(from, to);
        

        _matchData.GetPlayerData(playerId).IsMoving = false;
        var anotherPlayer = _matchData.GetAnotherPlayerData(playerId);
        anotherPlayer.IsMoving = true;
        _matchData.MovingPlayerId = anotherPlayer.PlayerId;
        
        if ((IsServer || IsHost) && _oneKingDead)
        {
            if (killedPiece.PieceType == PieceType.Kings)
            {
                Draw();
            }
            else
            {
                LosePlayer(_matchData.MovingPlayerId);
            }
            return;
        }
        
        if (killedPiece != null)
            DeathRattle(killedPiece);
    }

    #region Server

    private MatchCore _serverCore;
    private List<MatchCore> _allMatchCores;

    public void SetServerCore(MatchCore serverCore)
    {
        _serverCore = serverCore;
#if UNITY_SERVER || UNITY_EDITOR
        if (serverCore == this)
        {
            _allMatchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude).ToList();
#if UNITY_SERVER
            var server = FindFirstObjectByType<Server>();
            server.SetMatchCore(this);
#endif
#if UNITY_EDITOR
            // var serverObject = new GameObject("Server");
            // var server = serverObject.AddComponent<Server>();
            // server.SetMatchCore(this);
#endif
            Debug.Log("Cores Count" + _allMatchCores.Count);
        }
#endif
    }

    public void AddCore(MatchCore core)
    {
        _allMatchCores.Add(core);
    }

    [Rpc(SendTo.Server)]
    private void TryMoveRpc(Vector2Int from, Vector2Int to, RpcParams rpcParams = default)
    {
        _serverCore.TryMoveServer(from, to, rpcParams);
    }

    public void TryMoveServer(Vector2Int from, Vector2Int to, RpcParams rpcParams)
    {
        Debug.Log("TryMove");
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

        if (!_gameData.ActiveBoard.IsValidMove(from, to))
        {
            Debug.LogError("move is not valid");
            return;
        }

        // UseMove(from, to, playerId);
        SendToPlayersMove(from, to, playerId);
    }


    private void DeathRattle(AbstractPiece piece)
    {
        Debug.Log("DeathRattle");
        switch (piece.PieceType)
        {
            case PieceType.Empty:
            case PieceType.Pawns:
            case PieceType.Rooks:
            case PieceType.Knights:
            case PieceType.Bishops:
            case PieceType.Queens:
                break;
            case PieceType.Kings:
                _oneKingDead = true;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void LosePlayer(ulong winnerId)
    {
        Debug.Log("LosePlayer");
        Debug.Log(IsHost.ToString() + IsServer);
        foreach (var allMatchCore in _allMatchCores) allMatchCore.LosePlayerClientRpc(winnerId);
    }

    private void Draw()
    {
        foreach (var allMatchCore in _allMatchCores) allMatchCore.DrawClientRpc();
    }

    private void GetScopes(ulong winnerId, ref double player1Score, ref double player2Score)
    {
        if (winnerId == _matchData.Player1.PlayerId)
        {
            player1Score = 1.0;
            player2Score = 0.0;
        }
        else if (winnerId == _matchData.Player2.PlayerId)
        {
            player1Score = 0.0;
            player2Score = 1.0;
        }
        else
        {
            player1Score = 0.5;
            player2Score = 0.5;
        }
    }

    private void SendToPlayersMove(Vector2Int from, Vector2Int to, ulong playerId)
    {
        foreach (var allMatchCore in _allMatchCores) allMatchCore.UseMoveCommandRpc(from, to, playerId);
    }

    [Rpc(SendTo.Server)]
    public void TrySurrenderRpc(ulong winnerId)
    {
        LosePlayer(winnerId);
    }

    public void OnClientDisconnect(ulong clientId)
    {
        
        MatchCore disconnectedCore = null;
        foreach (var core in _allMatchCores)
        {
            if (core.OwnerClientId != clientId) continue;
            disconnectedCore = core;
            break;
        }
        _allMatchCores.Remove(disconnectedCore);
        
        if (_gameEnded)
            return;
        
        MatchCore lastPlayerCore = null;
        foreach (var core in _allMatchCores)
        {
            if (core == this) continue;
            lastPlayerCore = core;
            break;
        }
        lastPlayerCore?.OnClientDisconnectRpc(clientId);
        
    }
    #endregion

    [Rpc(SendTo.ClientsAndHost)]
    public void OnClientDisconnectRpc(ulong clientId)
    {
        if (_gameEnded)
            return;
        var enemyPlayerId = _matchData.GetPlayerData(clientId).FirebasePlayer.ID;
        var ip = _gameData.IP;
        var port = _gameData.Port;
#if !UNITY_SERVER
        _ = _global.FirestoreManager.RealtimeDatabase.ReConnectRequestsManager.SendReConnectRequest(enemyPlayerId, ip, port);
#endif
        
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    public void OnClientReConnectRpc(ulong oldId, ulong clientId, string firestoreId)
    {
        OnClientReConnect(oldId, clientId);
    }

    public void OnClientReConnect(ulong oldId, ulong clientId)
    {
        if (_gameEnded || !IsOwner) return;
        
        ChangePlayerDataIP(oldId, clientId);
        ChangeAllPieceIP(oldId, clientId);
        
        return;
        
        void ChangePlayerDataIP(ulong oldId, ulong clientId)
        {
            if(_matchData.MovingPlayerId == oldId)
                _matchData.MovingPlayerId = clientId;
            var oldPlayer = _matchData.Player1.PlayerId == oldId ? _matchData.Player1 : _matchData.Player2;
            oldPlayer.PlayerId = clientId;
        }
        void ChangeAllPieceIP(ulong oldId, ulong clientId)
        {
            _gameData.ActiveBoard.UpdateClientId(oldId, clientId);
        }
    }

    public void GetMatchData(ulong connectedPlayerId,ulong remainingPlayerId,out float connectedTimeToMove,out float remainingTimeToMove,out ulong movingPlayerId,out ulong whitePlayerId)
    {
        connectedTimeToMove = _matchData.GetPlayerData(connectedPlayerId).TimeToMove;
        remainingTimeToMove = _matchData.GetPlayerData(remainingPlayerId).TimeToMove;
        
        movingPlayerId = _matchData.MovingPlayerId;
        whitePlayerId = GetWhitePlayerId;
    }

    public string GetFirestoreId(ulong remainingPlayerId)
    {
        Debug.Log("GetFirestoreId");
        Debug.Log(remainingPlayerId);
        Debug.Log(_matchData.Player1.FirebasePlayer.ID);
        Debug.Log(_matchData.Player2.FirebasePlayer.ID);
        return _matchData.Player1.PlayerId == remainingPlayerId ? _matchData.Player1.FirebasePlayer.ID : _matchData.Player2.FirebasePlayer.ID;
    }

    public void SetEnemyStartArrangement(ArrangementEntry[] arrangement)
    {
        _matchData.GetPlayerData(_enemyId).Arrangement = arrangement;
    }
}
