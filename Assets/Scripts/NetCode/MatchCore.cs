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
    public float TimeToMove;
    public bool IsMoving;
}
public class MatchCore : NetworkBehaviour
{
    [Inject] private GameData _gameData;
    private ulong _myId;
    private MatchData _matchData;
    private bool _isInitialize;

    public void Initialize(MatchData matchData)
    {
        _matchData = matchData;
        
        _gameData.ActiveBoard.SetMatchCore(this);
        _myId = OwnerClientId;
        _isInitialize = true;
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

        UseMove(from, to, playerId);
        SendToPlayersMove(from, to, playerId);
    }

    private void UseMove(Vector2Int from, Vector2Int to, ulong playerId)
    {
        _gameData.ActiveBoard.MovePiece(from, to);
        _matchData.GetPlayerData(playerId).IsMoving = false;
        
        var anotherPlayer = _matchData.Player1.PlayerId == playerId ? _matchData.Player1 : _matchData.Player2;
        
        anotherPlayer.IsMoving = true;
        _matchData.MovingPlayerId = anotherPlayer.PlayerId;
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
    public void UseMoveCommandRpc(Vector2Int from, Vector2Int to, ulong playerId)
    {
        UseMove(from, to, playerId);
    }
}

