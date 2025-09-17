using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Board;
using Board.Piece;
using Firebase.Extensions;
using Firebase.Firestore;
using Game.Scripts.Board;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.Firebase.Firestore
{
public class HistoryManager
{

    private FirebaseFirestore _db;
    private readonly BackendManager _backendManager;
    public HistoryManager(FirebaseFirestore db ,BackendManager backendManager)
    {
        _db = db;
        _backendManager = backendManager;
    }
    public async void LoadHistory(string historyID, UnityAction<HistoryMatchData> callback)
    {
        try
        {
            var docRef = _db.Collection(MatchesDataCollectionName).Document(historyID);
            var snapshot = await docRef.GetSnapshotAsync();
            
            if (snapshot.Exists)
            {
                await Task.Yield(); //Optimization
                // Парсинг даних із документа
                var winnerID = snapshot.GetValue<string>(WinnerID);
                var date = snapshot.GetValue<DateTime>(Date);
                var player1ID = snapshot.GetValue<string>(Player1ID);
                var player1Elo = snapshot.GetValue<int>(Player1Elo);
                var arrangementList1 =
                    ParseArrangement(snapshot.GetValue<Dictionary<string, object>>(ArrangementList1));
                var player2ID = snapshot.GetValue<string>(Player2ID);
                var player2Elo = snapshot.GetValue<int>(Player2Elo);
                var arrangementList2 =
                    ParseArrangement(snapshot.GetValue<Dictionary<string, object>>(ArrangementList2));

                var moveHistory =
                    ParseMoveList(snapshot.GetValue<List<Dictionary<string, object>>>(MoveHistory));
                var player1Name = await _backendManager.GetPlayerName(player1ID);
                var player2Name = await _backendManager.GetPlayerName(player2ID);
                var historyMatchData = new HistoryMatchData
                (
                    snapshot.Id,
                    winnerID,
                    date,
                    player1ID,
                    player1Elo,
                    player1Name,
                    arrangementList1,
                    player2ID,
                    player2Elo,
                    player2Name,
                    arrangementList2,
                    moveHistory
                );
                callback?.Invoke(historyMatchData);
            }
            else
            {
                Debug.LogError($"Документ із ID {historyID} не знайдено.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in GetHistory for ID {historyID}: {ex.Message}");
            callback?.Invoke(null);
        }
    }

    private const string MatchesDataCollectionName = "Matches";

    private const string WinnerID = "WinnerID";
    private const string Date = "Date";
    private const string Player1ID = "Player1ID";
    private const string Player1Elo = "Player1Elo";
    private const string Player2ID = "Player2ID";
    private const string Player2Elo = "Player2Elo";
    private const string ArrangementList1 = "ArrangementList1";
    private const string ArrangementList2 = "ArrangementList2";
    private const string MoveHistory = "MoveHistory";

    private const string Row = "Row";
    private const string Column = "Column";
    private const string PieceType = "PieceType";

    public void SaveMatchHistory(string winnerID,
        string player1ID, int player1Elo, ArrangementEntry[] arrangement1,
        string player2ID, int player2Elo, ArrangementEntry[] arrangement2,
        List<Move> moveHistory, UnityAction<string> historyDocId)
    {
        var collectionRef = _db.Collection(MatchesDataCollectionName);

        var arrangementList1 = ConvertArrangement(arrangement1);
        var arrangementList2 = ConvertArrangement(arrangement2);
        var history = ConvertMoveList(moveHistory);
        var matchData = new Dictionary<string, object>
        {
            { WinnerID, winnerID },
            { Date, DateTime.UtcNow },
            { Player1ID, player1ID },
            { Player1Elo, player1Elo },
            { ArrangementList1, arrangementList1 },
            { Player2ID, player2ID },
            { Player2Elo, player2Elo },
            { ArrangementList2, arrangementList2 },
            { MoveHistory, history }
        };
        Debug.Log(matchData);
        collectionRef.AddAsync(matchData).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                var docRef = task.Result;
                historyDocId.Invoke(docRef.Id);
                Debug.Log("matchData Added successfully. " + docRef.Id );
            }
            else if (task.IsFaulted)
            {
                Debug.LogError("Error updating document: " + task.Exception);
            }
        });
        Debug.Log("Match saved");
    }
    

    private List<Dictionary<string, object>> ConvertMoveList(List<Move> moveHistory)
    {
        var result = new List<Dictionary<string, object>>();

        foreach (var move in moveHistory)
        {
            var dict = new Dictionary<string, object>
            {
                {
                    "From", new Dictionary<string, object>
                    {
                        { Row, move.From.Row },
                        { Column, move.From.Column }
                    }
                },
                {
                    "To", new Dictionary<string, object>
                    {
                        { Row, move.To.Row },
                        { Column, move.To.Column }
                    }
                }
            };
            result.Add(dict);
        }

        return result;
    }

    private Dictionary<string, object> ConvertArrangement(ArrangementEntry[] arrangement)
    {
        var result = new Dictionary<string, object>();

        for (var i = 0; i < arrangement.Length; i++)
        {
            var entry = arrangement[i];
            var dict = new Dictionary<string, object>
            {
                { Row, entry.row },
                { Column, entry.column },
                { PieceType, entry.pieceType.ToString() }
            };

            // Add the entry to the result dictionary with a key like "0", "1", "2", etc.
            result[i.ToString()] = dict;
        }

        return result;
    }

    
    private ArrangementEntry[] ParseArrangement(Dictionary<string, object> arrangementData)
    {
        var result = new List<ArrangementEntry>();

        foreach (var item in arrangementData)
        {
            var entryData = item.Value as Dictionary<string, object>;

            if (entryData != null)
            {
                var arrangementEntry = new ArrangementEntry
                {
                    row = Convert.ToInt32(entryData[Row]),
                    column = Convert.ToInt32(entryData[Column]),
                    pieceType = Enum.Parse<PieceType>(entryData[PieceType].ToString())
                };

                result.Add(arrangementEntry);
            }
            else
            {
                Debug.LogError($"Помилка парсингу розстановки: невірний формат даних для ключа {item.Key}");
            }
        }

        return result.ToArray();
    }

    private List<int4> ParseMoveList(List<Dictionary<string, object>> moveData)
    {
        var result = new List<int4>();

        foreach (var moveDict in moveData)
        {
            var fromData = moveDict["From"] as Dictionary<string, object>;
            var toData = moveDict["To"] as Dictionary<string, object>;

            if (fromData != null && toData != null)
            {
                var move = new int4
                {
                    x = Convert.ToInt32(fromData[Row]),
                    y = Convert.ToInt32(fromData[Column]),
                    z = Convert.ToInt32(toData[Row]),
                    w = Convert.ToInt32(toData[Column])
                };

                result.Add(move);
            }
            else
            {
                Debug.LogError("Помилка розбору руху: формат даних неправильний.");
            }
        }

        return result;
    }
}
}