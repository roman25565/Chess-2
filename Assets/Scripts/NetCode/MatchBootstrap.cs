using System.Threading.Tasks;
using Setting;
using Unity.Netcode;
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

    [Inject] private Settings _settings;

    private void Awake()
    {
        ProjectContext.Instance.Container.InjectGameObject(gameObject);
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (IsServer) Camera.main.backgroundColor = Color.blue;
#endif
        if (IsOwner && IsLocalPlayer)
        {
            var gameMode = _gameData.Mode;
            var myArrangements = _settings.MyArrangements;

            var arrangementEntryArray = new ArrangementEntryArray
            {
                ArrangementEntry = new ArrangementEntry[myArrangements.Count]
            };

            for (var index = 0; index < myArrangements.ToArray().Length; index++)
            {
                var arrangementEntry = myArrangements.ToArray()[index];
                arrangementEntryArray.ArrangementEntry[index] = arrangementEntry;
            }

            if (gameMode == GameMode.Online || gameMode == GameMode.Offline)
            {
                var id = OwnerClientId;
                Debug.Log(NetworkManager.Singleton.GetInstanceID());
                SomeRpc(id, _settings.FirestoreManager.PlayerData.ID, arrangementEntryArray.ArrangementEntry);
            }

            if (gameMode == GameMode.Test)
            {
                SomeRpc(OwnerClientId, _settings.FirestoreManager.PlayerData.ID,
                    arrangementEntryArray.ArrangementEntry);
                SomeRpc(2, "002",
                    arrangementEntryArray.ArrangementEntry);
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void SomeRpc(ulong playerId, string firestoreId, ArrangementEntry[] arrangement,
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
                whitePlayerId);
            var player2 = CreatePlayerBootstrapData(_player1.ID, firestoreId, _player1.Arrangement.ArrangementEntry,
                whitePlayerId);

            _gameData.ActiveBoard.StartGame(player1, player2);

            _ = StartMatchServer(player1, player2, whitePlayerId);

            var matchBootstraps = FindObjectsByType<MatchBootstrap>((FindObjectsSortMode)FindObjectsInactive.Exclude);

            foreach (var matchBootstrap in matchBootstraps)
                matchBootstrap.SendToClientPlayerBootstrapDataRpc(
                    _player0.ID,
                    _player0.FirestoreId,
                    _player0.Arrangement.ArrangementEntry,
                    _player1.ID,
                    _player1.FirestoreId,
                    _player1.Arrangement.ArrangementEntry,
                    whitePlayerId);
        }
    }

    private Task StartMatchServer(PlayerBootstrapData player1, PlayerBootstrapData player2, ulong whitePlayerId)
    {
        var coreServer = Instantiate(matchCore);

        var corePlayer1 = Instantiate(matchCore, transform);
        corePlayer1.GetComponent<NetworkObject>().SpawnWithOwnership(player1.PlayerId);
        corePlayer1.SetServerCore(coreServer);

        var corePlayer2 = Instantiate(matchCore, transform);
        corePlayer2.GetComponent<NetworkObject>().SpawnWithOwnership(player2.PlayerId);
        corePlayer2.SetServerCore(coreServer);

        coreServer.GetComponent<NetworkObject>().Spawn();

        var matchData = CreateMatchData(player1, player2, whitePlayerId);

        coreServer.Init(matchData);
        coreServer.SetServerCore(coreServer);
        return Task.CompletedTask;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendToClientPlayerBootstrapDataRpc(
        ulong playerId, string firestoreId, ArrangementEntry[] arrangement,
        ulong playerId2, string firestoreId2, ArrangementEntry[] arrangement2,
        ulong whitePlayerId)
    {
        if (!IsLocalPlayer) return;
        var player1 = new PlayerBootstrapData(playerId, firestoreId, arrangement, playerId != whitePlayerId,
            whitePlayerId == playerId);
        var player2 = new PlayerBootstrapData(playerId2, firestoreId2, arrangement2, playerId2 != whitePlayerId,
            whitePlayerId == playerId2);
        _gameData.ActiveBoard.StartGame(player1, player2);

        _ = StartMatchClient(player1, player2, whitePlayerId);
    }

    private Task StartMatchClient(PlayerBootstrapData player1, PlayerBootstrapData player2, ulong whitePlayerId)
    {
        var board = _gameData.ActiveBoard;

        var allMatchCores = FindObjectsByType<MatchCore>((FindObjectsSortMode)FindObjectsInactive.Exclude);

        foreach (var core in allMatchCores)
        {
            if (!core.IsOwner) continue;

            var matchData = CreateMatchData(player1, player2, whitePlayerId);
            _ = _settings.FirestoreManager.GetPlayerData(player1.FirestoreId, result =>
            {
                matchData.Player1.FirebasePlayer = result;

                core.UpdateFirebasePlayerData(matchData.Player1.PlayerId, matchData.Player1.PlayerId != OwnerClientId);
            });
            _ = _settings.FirestoreManager.GetPlayerData(player2.FirestoreId, result =>
            {
                matchData.Player2.FirebasePlayer = result;
                core.UpdateFirebasePlayerData(matchData.Player2.PlayerId, matchData.Player2.PlayerId != OwnerClientId);
            });
            Debug.Log("matchCore.Init(matchData);");
            core.Init(matchData);
        }

        var ownerData = FindOwnerData();


        Debug.Log(
            $"player1: {player1.PlayerId} IsRotate: {player1.IsRotate}, player2: {player2.PlayerId} IsRotate: {player2.IsRotate}");

        if (ownerData.IsRotate) board.RotateBoard();

        return Task.CompletedTask;

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
                PlayerId = player1.PlayerId,
                IsMoving = player1.PlayerId == whitePlayerId,
                IsRotate = player1.IsRotate,
                IsWhite = player1.IsWhite,
                Arrangement = player1.Arrangement,
                TimeToMove = 10f * 60
            },
            Player2 = new PlayerData
            {
                PlayerId = player2.PlayerId,
                IsMoving = player2.PlayerId == whitePlayerId,
                IsRotate = player2.IsRotate,
                IsWhite = player2.IsWhite,
                Arrangement = player2.Arrangement,
                TimeToMove = 10f * 60
            }
        };
    }

    private PlayerBootstrapData CreatePlayerBootstrapData(ulong playerId, string firestoreId,
        ArrangementEntry[] arrangement, ulong whitePlayerId)
    {
        return new PlayerBootstrapData(playerId, firestoreId, arrangement, whitePlayerId != playerId,
            whitePlayerId == playerId);
    }

    private ulong GetWhitePlayerId(ulong playerId, ulong playerId2)
    {
        var randomValue = Random.value;
        var randomIndex = randomValue < 0.5 ? 1 : 2;

        return randomIndex == 1 ? playerId : playerId2;
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