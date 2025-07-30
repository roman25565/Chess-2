using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Mathematics;

public class HistoryMatchData
{
    [NotNull] public readonly DateTime Date;
    [NotNull] public readonly string MatchId;
    [NotNull] public readonly List<int4> MoveHistory;
    [NotNull] public readonly ArrangementEntry[] Player1Arrangement;
    [NotNull] public readonly int Player1Elo;

    [NotNull] public readonly string FirestorePlayer1Id;
    [NotNull] public readonly string Player1Name;
    [NotNull] public readonly ArrangementEntry[] Player2Arrangement;
    [NotNull] public readonly int Player2Elo;

    [NotNull] public readonly string FirestorePlayer2Id;
    [NotNull] public readonly string Player2Name;
    [NotNull] public readonly string WinnerID;


    public HistoryMatchData(string matchId, string winnerID, DateTime date,
        string firestorePlayer1Id, int player1Elo, string player1Name, ArrangementEntry[] player1Arrangement,
        string firestorePlayer2Id, int player2Elo, string player2Name, ArrangementEntry[] player2Arrangement,
        List<int4> moveHistory)
    {
        if (matchId == null)
            throw new ArgumentNullException(nameof(matchId));
        if (winnerID == null)
            throw new ArgumentNullException(nameof(winnerID));
        if (firestorePlayer1Id == null)
            throw new ArgumentNullException(nameof(firestorePlayer1Id));
        if (player1Name == null)
            throw new ArgumentNullException(nameof(player1Name));
        if (player1Arrangement == null)
            throw new ArgumentNullException(nameof(player1Arrangement));
        if (firestorePlayer2Id == null)
            throw new ArgumentNullException(nameof(firestorePlayer2Id));
        if (player2Name == null)
            throw new ArgumentNullException(nameof(player2Name));
        if (player2Arrangement == null)
            throw new ArgumentNullException(nameof(player2Arrangement));
        if (moveHistory == null)
            throw new ArgumentNullException(nameof(moveHistory));


        MatchId = matchId;
        FirestorePlayer1Id = firestorePlayer1Id;
        Player1Elo = player1Elo;
        Player1Name = player1Name;
        Player1Arrangement = player1Arrangement;

        FirestorePlayer2Id = firestorePlayer2Id;
        Player2Elo = player2Elo;
        Player2Name = player2Name;
        Player2Arrangement = player2Arrangement;

        WinnerID = winnerID;
        Date = date;
        MoveHistory = moveHistory;
    }
}