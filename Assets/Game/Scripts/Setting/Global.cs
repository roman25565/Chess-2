using System;
using System.Collections.Generic;
using Board.Piece;
using Game.Scripts.Board.Piece;
using TMPro;
#if !UNITY_SERVER
using Firebase.RealtimeDatabase;
#endif
using UnityEngine;
using UnityEngine.Events;

namespace Setting
{
public class EndGameData
{
    public EndGameType Type;
    public WonReason WonReason;
    public PlayerData MyPlayerData;
    public PlayerData EnemyPlayerData;
    public int MyNewElo;
    public int EnemyNewElo;
    public string MatchId;
    public bool IsLocal;

    public EndGameData(EndGameType type, WonReason wonReason, PlayerData myPlayerData, PlayerData enemyPlayerData, int myNewElo, int enemyNewElo, string matchId, bool isLocal = false)
    {
        Type = type;
        WonReason = wonReason;
        MyPlayerData = myPlayerData;
        EnemyPlayerData = enemyPlayerData;
        MyNewElo = myNewElo;
        EnemyNewElo = enemyNewElo;
        MatchId = matchId;
        IsLocal = isLocal;
    }
}
public class Global : IDisposable
{
    public static readonly string ArrangementFile = Application.persistentDataPath + "/game_pieces.json";
    public CellStates CellStates;
    public FirestoreManager FirestoreManager;
    public List<ArrangementEntry> MyArrangements;
    public Dictionary<PieceType, PieceData> Pieces;
    public Sound Sound;

    public bool IsSignIn;
    public EndGameData EndGameData;
    
    public Dictionary<BotDifficulty, Sprite> BotIcons = new();

    public void Init(List<ArrangementEntry> arrangement, PieceData[] pieces, CellStates cellStates, FirestoreManager firestoreManager)
    {
        MyArrangements = RepackingArrangement(arrangement);
        CellStates = cellStates;
#if !UNITY_SERVER
        FirestoreManager = firestoreManager;
#endif
        Pieces = new Dictionary<PieceType, PieceData>();
        foreach (var piece in pieces)
        {
            Debug.Log(piece);
            Pieces.Add(piece.pieceType, piece);
        };
    }
    
    public AbstractPiece CreatePiece(PieceType pieceType)
    {
        AbstractPiece result = pieceType switch
        {
            PieceType.Pawn => new Pawn(Pieces[pieceType]),
            PieceType.Rook => new Rook(Pieces[pieceType]),
            PieceType.Knight => new Knight(Pieces[pieceType]),
            PieceType.Bishop => new Bishop(Pieces[pieceType]),
            PieceType.Queen => new Queen(Pieces[pieceType]),
            PieceType.King => new King(Pieces[pieceType]),
            _ => throw new ArgumentOutOfRangeException(nameof(pieceType), pieceType, null)
        };
        return result;
    }

    private List<ArrangementEntry> RepackingArrangement(List<ArrangementEntry> arrangement)
    {
        List<ArrangementEntry> result = new();
        foreach (var arrangementArrangement in arrangement)
            result.Add(new ArrangementEntry
            {
                column = arrangementArrangement.column,
                row = arrangementArrangement.row,
                pieceType = arrangementArrangement.pieceType
            });
        return result;
    }

    public void Dispose()
    {
        GlobalTools.Dispose();
    }

    public List<AbstractPiece> GetRandomPiecesForCost(int cost) //King is [0]
    {
        var result = new List<AbstractPiece>();

        var king = CreatePiece(PieceType.King);
        result.Add(king);

        var currentCost = Pieces[PieceType.King].arrangementCost;

        List<PieceType> availableTypes = new List<PieceType>();
        foreach (var kv in Pieces)
        {
            if (kv.Key != PieceType.King && kv.Key != PieceType.Empty)
            {
                availableTypes.Add(kv.Key);
            }
        }

        for (var i = 0; i < 100; i++)
        {
            if (currentCost == cost)
                break;

            PieceType chosenType = WeightedRandom(availableTypes, Pieces);

            var pieceCost = Pieces[chosenType].arrangementCost;

            if (currentCost + pieceCost <= cost)
            {
                var piece = CreatePiece(chosenType);
                result.Add(piece);
                currentCost += pieceCost;
            }
        }

        return result;
    }
    PieceType WeightedRandom(List<PieceType> pool, Dictionary<PieceType, PieceData> Pieces)
    {
        float totalWeight = 0;
        foreach (var t in pool)
            totalWeight += 1f / Pieces[t].arrangementCost; // чим дешевше, тим більше вага

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0;

        foreach (var t in pool)
        {
            cumulative += 1f / Pieces[t].arrangementCost;
            if (roll <= cumulative)
                return t;
        }

        return pool[pool.Count - 1];
    }
}
}