using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Board;
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
        if (Player1.PlayerId == id)
        {
            return Player1;
        }
        else if (Player2.PlayerId == id)
        {
            return Player2;
        }
        Debug.LogError("Player id not found");
        return null;
    }
}

public class PlayerData
{
    public ulong PlayerId;
    public FirebasePlayerData FirebasePlayer;
    public float TimeToMove;
    public bool IsMoving;
    public bool IsRotate;
    public ArrangementEntry[] Arrangement;
    public bool IsWhite;
}
public class MatchCore : NetworkBehaviour
{
    [Inject] private GameData _gameData;
    [Inject] private Settings _settings;
    private ulong _myId;
    private ulong _enemyId;
    private MatchData _matchData;
    private bool _isInitialize;
    private float _lastUpdateTime;
    private bool _gameEnded;

    public bool IsRotated => _matchData.GetPlayerData(_myId).IsRotate;
    public bool IsWhite => _matchData.GetPlayerData(_myId).IsWhite;
    public bool IsMyId(ulong id) => id == _myId;
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
            var time = playerData.TimeToMove;
            
            if (Mathf.Abs(playerData.TimeToMove - _lastUpdateTime) > 0.01f)
            {
                _lastUpdateTime = time;
                UIManager.Instance.SetTime(time, playerData.PlayerId != _myId);
            }
        }
    }

    public void UpdateFirebasePlayerData(ulong playerId, bool isEnemyPlayer)
    {
        if (playerId == _matchData.Player1.PlayerId)
        { 
            Debug.Log("UpdateFirebasePlayerData");
            UIManager.Instance.SetPlayerUI(_matchData.Player1.FirebasePlayer, isEnemyPlayer);
        }else if (playerId == _matchData.Player2.PlayerId)
        {
            UIManager.Instance.SetPlayerUI(_matchData.Player2.FirebasePlayer, isEnemyPlayer);
            Debug.Log("UpdateFirebasePlayerDataFinish");
        }
    }

    public void Init(MatchData matchData)
    { 
        Debug.Log("Application.isMainThread" + Thread.CurrentThread.ManagedThreadId);
        Debug.Log("MatchCore.Init");
        
        _matchData = matchData;
        _gameData.ActiveBoard.SetMatchCore(this);
        _myId = OwnerClientId;
        _enemyId = _myId == matchData.Player2.PlayerId ? matchData.Player1.PlayerId : matchData.Player2.PlayerId;
        
        _isInitialize = true;
        Debug.Log("MatchCore.Inited");
    }

    public void TryMove(Vector2Int from, Vector2Int to)
    {
        if (!_isInitialize)
        {
            Debug.LogError("TryMove called before Initialize");
            return;
        }

        var myData = _matchData.GetPlayerData(_myId);

        if (myData.IsMoving || myData.TimeToMove <= 0)
        {
            TryMoveRpc(from, to);
        }
        else
        {
            Debug.LogError("Error player cont move");
            return;
        }
    }

    #region Server
    private MatchCore _serverCore;
    private MatchCore[] _allMatchCores;
    public void SetServerCore(MatchCore serverCore)
    {
        _serverCore = serverCore;
        if (serverCore == this)
        {
            _allMatchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);
            Debug.Log(_allMatchCores.Length);
        }
        
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
        }

        if (playerData.TimeToMove < 0)
        {
            Debug.LogError("TimeToMove is negative");
        }

        if (!_gameData.ActiveBoard.IsValidMove(from, to))
        {
            Debug.LogError("move is not valid");
        }

        // UseMove(from, to, playerId);
        SendToPlayersMove(from, to, playerId);
    }


    private void DeathRattle(Cell cell)
    {
        Debug.Log("DeathRattle");
        switch (cell.Piece.PieceType)
        {
            case PieceType.Empty:
                break;
            case PieceType.Pawns:
                break;
            case PieceType.Rooks:
                break;
            case PieceType.Knights:
                break;
            case PieceType.Bishops:
                break;
            case PieceType.Queens:
                break;
            case PieceType.Kings:
                if (IsServer || IsHost) LosePlayer(cell.Piece.OwnerId == _matchData.Player1.PlayerId ? _matchData.Player1.PlayerId : _matchData.Player2.PlayerId);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void LosePlayer(ulong loserId)
    {
        Debug.Log("LosePlayer");
        Debug.Log(((IsHost.ToString()) + IsServer));
        foreach (var allMatchCore in _allMatchCores)
        {
            allMatchCore.LosePlayerClientRpc(loserId);
        }
    }

    private void GetScopes(ulong winnerId,ref double player1Score, ref double player2Score)
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
        foreach (var allMatchCore in _allMatchCores)
        {
            allMatchCore.UseMoveCommandRpc(from, to, playerId);
        }
    }
    #endregion

    [Rpc(SendTo.ClientsAndHost)]
    private void LosePlayerClientRpc(ulong winnerId)
    {
        if (!IsOwner) return;

        _gameEnded = true;
        _gameData.ActiveBoard.EndGame();

        CalculateNewEloRatings(winnerId, out var myElo, out var enemyElo);
        UIManager.Instance.EndGame(winnerId == _myId, myElo, enemyElo);
        
        if (!IsWhite) return;
        
        _settings.FirestoreManager.BdSetElo(_matchData.GetPlayerData(_myId).FirebasePlayer.ID, myElo);
        _settings.FirestoreManager.BdSetElo(_matchData.GetPlayerData(_enemyId).FirebasePlayer.ID, enemyElo);

        _ = _settings.FirestoreManager.SaveMatchHistory(
            _matchData.GetPlayerData(winnerId).FirebasePlayer.ID,
            _matchData.Player1.FirebasePlayer.ID,
            _matchData.Player1.Arrangement,
            _matchData.Player2.FirebasePlayer.ID + 1,
            _matchData.Player1.Arrangement,
            _gameData.ActiveBoard.GetHistory()
        );
    }

    private void CalculateNewEloRatings(ulong winnerId, out int myElo, out int enemyElo)
    {
        double scope1 = 0;
        double scope2 = 0;
        GetScopes(winnerId, ref scope1, ref scope2);

        var newElo1 = GlobalTools.CalculateNewRating(_matchData.Player1.FirebasePlayer.Elo,
            _matchData.Player1.FirebasePlayer.Elo, scope1);
        var newElo2 = GlobalTools.CalculateNewRating(_matchData.Player2.FirebasePlayer.Elo,
            _matchData.Player2.FirebasePlayer.Elo, scope2);

        bool isFirstPlayer = _matchData.Player1.PlayerId == _myId;
        myElo = isFirstPlayer ? newElo1 : newElo2;
        enemyElo = isFirstPlayer ? newElo2 : newElo1;
    }

    [Rpc(SendTo.Everyone)]
    public void UseMoveCommandRpc(Vector2Int from, Vector2Int to, ulong playerId)
    {
        if (IsOwner)
        {
            UseMove(from, to, playerId);
        }
    }
    private void UseMove(Vector2Int from, Vector2Int to, ulong playerId)
    {
        var board = _gameData.ActiveBoard;
        var cell = board.GetCell(to.x, to.y);
        if (cell.Piece != null)
        {
            DeathRattle(cell);
        }
        _gameData.ActiveBoard.MovePiece(from, to);
        _matchData.GetPlayerData(playerId).IsMoving = false;
        
        var anotherPlayer = _matchData.Player1.PlayerId == playerId ? _matchData.Player2 : _matchData.Player1;
        
        anotherPlayer.IsMoving = true;
        _matchData.MovingPlayerId = anotherPlayer.PlayerId;
    }
}

