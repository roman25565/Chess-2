using System;
using System.Collections.Generic;
using System.IO;
using Board.Piece;
using Game.Scripts.Board.Piece;
using Newtonsoft.Json;
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
    private readonly string _arrangementFile = Application.persistentDataPath + "/game_pieces.json";
    public CellStates CellStates;
    public BackendManager BackendManager;
    public Dictionary<PieceType, PieceData> Pieces;
    public Sound Sound;

    public List<ArrangementEntry> SelectedArrangement;
    public Dictionary<int,List<ArrangementEntry>> SavedArrangements;
    public UnityEvent<List<ArrangementEntry>> OnArrangementChanged = new UnityEvent<List<ArrangementEntry>>();

    public bool IsSignIn;
    public EndGameData EndGameData;
    
    public Dictionary<BotDifficulty, Sprite> BotIcons = new();

    public void Init(Dictionary<int,List<ArrangementEntry>> arrangements, PieceData[] pieces, CellStates cellStates, BackendManager backendManager)
    {
        SavedArrangements = arrangements;
        SelectedArrangement = arrangements[0];
        CellStates = cellStates;
#if !UNITY_SERVER
        BackendManager = backendManager;
#endif
        Pieces = new Dictionary<PieceType, PieceData>();
        foreach (var piece in pieces)
        {
            Debug.Log(piece);
            Pieces.Add(piece.pieceType, piece);
        };
    }

    public void SetSelectedArrangement(int index)
    {
        if (SavedArrangements[index] == null)
        {
            Debug.LogError("SavedArrangements[index] == null");
            return;
        }
        SelectedArrangement = SavedArrangements[index];
        OnArrangementChanged?.Invoke(SelectedArrangement);
    }
    
    public void SaveToJson()
    {
        var json = JsonConvert.SerializeObject(SavedArrangements, Formatting.Indented);

        File.WriteAllText(_arrangementFile, json);
    }

    public void SetArrangement(int index, List<ArrangementEntry> arr)
    {
        SavedArrangements[index] = arr;
        SaveToJson();
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