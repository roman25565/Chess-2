using System;
using System.Collections.Generic;
using System.Linq;
using Setting;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Cache = UnityEngine.Cache;
using Random = UnityEngine.Random;

public class MatchBootstrap : NetworkBehaviour
{
    
    [Inject]
    private Settings _settings;
    [Inject]
    private GameData _gameData;
    
    [SerializeField] private MatchCore matchCore;

    private void Awake()
    {
        ProjectContext.Instance.Container.InjectGameObject(gameObject);
    }

    private void Start()
    {
        #if UNITY_EDITOR
        if (IsServer)
        {
            Camera.main.backgroundColor = Color.blue;
        }
        #endif
        if (IsOwner && IsLocalPlayer)
        {
            var gameMode = _gameData.Mode;
            var myArrangements = _settings.MyArrangements;

            var arrangementEntryArray = new ArrangementEntryArray();
            arrangementEntryArray.ArrangementEntry = new ArrangementEntry[myArrangements.Count];

            for (var index = 0; index < myArrangements.ToArray().Length; index++)
            {
                var arrangementEntry = myArrangements.ToArray()[index];
                arrangementEntryArray.ArrangementEntry[index] = arrangementEntry;
            }
            if (gameMode == GameMode.Online || gameMode == GameMode.Offline)
            {
                var id = OwnerClientId;
                Debug.Log(NetworkManager.Singleton.GetInstanceID());
                SomeRpc(id, arrangementEntryArray.ArrangementEntry);
            }

            if (gameMode == GameMode.Test)
            {
                SomeRpc(OwnerClientId, arrangementEntryArray.ArrangementEntry);
                SomeRpc(OwnerClientId, arrangementEntryArray.ArrangementEntry);
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void SomeRpc(ulong playerId, ArrangementEntry[] arrangement, RpcParams rpcParams = default)
    {
        if (_gameData.Player0.Arrangement == null)
        {
            _gameData.Player0.ID = playerId;
            _gameData.Player0.Arrangement = new ArrangementEntryArray{ ArrangementEntry = arrangement };
        }
        else if (_gameData.Player1.Arrangement == null)
        {
            _gameData.Player1.ID = playerId;
            _gameData.Player1.Arrangement = new ArrangementEntryArray{ ArrangementEntry = arrangement };
        }
        
        if (_gameData.Player1.Arrangement != null && _gameData.Player0.Arrangement != null)
        {
            var whitePlayerId = GetWhitePlayerId(_gameData.Player0.ID, _gameData.Player1.ID);
            
            var player1 = CreatePlayerBootstrapData(_gameData.Player0.ID, _gameData.Player0.Arrangement.ArrangementEntry, whitePlayerId);
            var player2 = CreatePlayerBootstrapData(_gameData.Player1.ID, _gameData.Player1.Arrangement.ArrangementEntry, whitePlayerId);

            ArrangeFigures(player1);
            ArrangeFigures(player2);
            
            foreach (var boardLinesKey in _gameData.ActiveBoard._boardLines.Keys)
            {
                foreach (var cel in _gameData.ActiveBoard._boardLines[boardLinesKey])
                {
                    if (cel.Piece == null) continue;
                    Debug.Log($"cell2Id: {cel.Piece.OwnerId}");
                }
            }
            Debug.Log($"player1: {player1.PlayerId} player2: {player2.PlayerId}");
            
            
            StartMatchServer(player1, player2, whitePlayerId);
            
            MatchBootstrap[] matchBootstraps = FindObjectsByType<MatchBootstrap>((FindObjectsSortMode)FindObjectsInactive.Exclude);

            foreach (var matchBootstrap in matchBootstraps)
            {
                matchBootstrap.ArrangeFiguresOnClientsRpc(_gameData.Player0.ID,
                    _gameData.Player0.Arrangement.ArrangementEntry, _gameData.Player1.ID,
                    _gameData.Player1.Arrangement.ArrangementEntry, whitePlayerId);
            }
        }
    }

    private void StartMatchServer(PlayerBootstrapData player1, PlayerBootstrapData player2, ulong whitePlayerId)
    {
        MatchCore coreServer = Instantiate(matchCore);
        coreServer.GetComponent<NetworkObject>().Spawn();
        var matchData = CreateMatchData(player1, player2, whitePlayerId);
        ProjectContext.Instance.Container.InjectGameObject(coreServer.gameObject);
        coreServer.Initialize(matchData);
        coreServer.SetServerCore(coreServer);
        
        MatchCore corePlayer1 = Instantiate(matchCore, this.transform);
        corePlayer1.GetComponent<NetworkObject>().SpawnWithOwnership(player1.PlayerId);
        corePlayer1.SetServerCore(coreServer);
        
        MatchCore corePlayer2 = Instantiate(matchCore, this.transform);
        corePlayer2.GetComponent<NetworkObject>().SpawnWithOwnership(player2.PlayerId);
        corePlayer2.SetServerCore(coreServer);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void ArrangeFiguresOnClientsRpc(ulong playerId, ArrangementEntry[] arrangement, 
        ulong playerId2, ArrangementEntry[] arrangement2, ulong whitePlayerId)
    {
        if (!IsLocalPlayer)
        {
            return;
        }
        Debug.Log("ArrangeFiguresOnClientsRpc");
        var player1 = new PlayerBootstrapData(playerId, arrangement,playerId != whitePlayerId, whitePlayerId == playerId);
        var player2 = new PlayerBootstrapData(playerId2, arrangement2,playerId2 != whitePlayerId, whitePlayerId == playerId2);
        Debug.Log($"player1: {player1.PlayerId} player2: {player2.PlayerId}");
        ArrangeFigures(player1);
        ArrangeFigures(player2);

        
        StartMatchClient(player1, player2, whitePlayerId);
    }

    private void StartMatchClient(PlayerBootstrapData player1, PlayerBootstrapData player2, ulong whitePlayerId)
    {
        var matchData = CreateMatchData(player1, player2, whitePlayerId);
        var board = _gameData.ActiveBoard;
        
        MatchCore[] allMatchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);
        
        foreach (MatchCore matchCore in allMatchCores)
        {
            if (matchCore.IsOwner)
            {
                ProjectContext.Instance.Container.InjectGameObject(matchCore.gameObject);
                matchCore.Initialize(matchData);
            }
        }

        var ownerData = FindOwnerData();

        
        Debug.Log($"player1: {player1.PlayerId} IsRotate: {player1.IsRotate}, player2: {player2.PlayerId} IsRotate: {player2.IsRotate}");
        
        if (ownerData.IsRotate)
        {
            board.RotateBoard();
        }

        return;

        PlayerBootstrapData FindOwnerData()
        {
            if (OwnerClientId == player1.PlayerId)
            {
                return player1;
            }
            else if (OwnerClientId == player2.PlayerId)
            {
                return player2;
            }

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
                IsMoving = player1.PlayerId == whitePlayerId,
                PlayerId = player1.PlayerId,
                TimeToMove = 10f
            },
            Player2 = new PlayerData
            {
                IsMoving = player2.PlayerId == whitePlayerId,
                PlayerId = player2.PlayerId,
                TimeToMove = 10f
            }
        };
    }


    private class PlayerBootstrapData
    {
        
        public PlayerBootstrapData(ulong playerId, ArrangementEntry[] arrangement, bool isRotate, bool isWhite)
        {
            PlayerId = playerId;
            Arrangement = arrangement;
            IsRotate = isRotate;
            IsWhite = isWhite;
        }

        public ulong PlayerId;
        public ArrangementEntry[] Arrangement;
        public bool IsRotate;
        public bool IsWhite;
    }

    private PlayerBootstrapData CreatePlayerBootstrapData(ulong playerId,  ArrangementEntry[] arrangement, ulong whitePlayerId)
    {
        return new PlayerBootstrapData(playerId, arrangement,whitePlayerId != playerId,whitePlayerId == playerId);
    }

    private void ArrangeFigures(PlayerBootstrapData playerBootstrapData)
    {
        Debug.Log($"ArrangeFigures playerId: {playerBootstrapData.PlayerId}");
        var board = _gameData.ActiveBoard;
        foreach (var arrangementArrangement in playerBootstrapData.Arrangement)
        {
            var row = arrangementArrangement.column;
            var column = arrangementArrangement.row;
            var type = arrangementArrangement.pieceType;
            var piece = _settings.CreatePiece(type);
            piece.OwnerId = playerBootstrapData.PlayerId;
            piece.IsRotated = playerBootstrapData.IsRotate;
            piece.Color = playerBootstrapData.IsWhite ? PieceColor.White : PieceColor.Black;
            if (playerBootstrapData.IsRotate)
            {
                column = 7 - column;
                row = 7 - row;
            }
            board.GetCell(column,row).SetPiece(piece);
            
        }
    }

    private ulong GetWhitePlayerId(ulong playerId, ulong playerId2)
    {
        float randomValue = Random.value;
        var randomIndex = randomValue < 0.5 ? 1 : 2;
        
        return randomIndex == 1 ? playerId : playerId2;
    }
}
