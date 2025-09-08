using System;
using System.Text;
using System.Threading.Tasks;
using Board.Piece;
using Chess.Game;
using Chess.Players;
using Firebase.Extensions;
using Game.Scripts.Board;
using UnityEngine;
using Zenject;
using Move = Chess.Core.Move;

namespace Game.Scripts.Matchmaking
{
public class BotController : MonoBehaviour
{
    [Inject] private GameData _gameData;
    [SerializeField] private MatchCore matchCorePrefab;
    [SerializeField] private Bot bot; 
    private void Start()
    {
        var gameMode = _gameData.Mode;
        if (gameMode == GameMode.SinglePlayVsBot)
        {
            Debug.Log("Starting match BotController");
            var gameObject = Instantiate(new GameObject("GameStarter"));
            var matchStarter = gameObject.AddComponent<MatchStarter>();
            matchStarter.SetMatchCore(matchCorePrefab);
        }
    }
    
    private MatchData _matchData;
    private MatchCore _matchCore;
    private bool _isRotate;

    public void InitBotController(MatchData matchData, MatchCore matchCore)
    {
        _matchData = matchData;
        _matchCore = matchCore;
        _isRotate = matchData.Player2.IsRotate;
        Debug.Log($"IsRotate {_isRotate}");
        var board = _gameData.ActiveBoard;

        bot.loadCustomPosition = true;
        bot.customPosition = Convert(board, !_isRotate);

        bot.whitePlayerType = matchData.Player1.IsWhite ? Bot.PlayerType.Human : Bot.PlayerType.AI;
        bot.blackPlayerType = !matchData.Player1.IsWhite ? Bot.PlayerType.Human : Bot.PlayerType.AI;
        
        bot.onMoveMade += OnMoveMade;
        bot.OnBotDie += RestartBot;
        bot.BotStart();
        bot.NotifyPlayerToMove();
    }

    private void RestartBot()
    {
        Debug.Log("restarting bot");
        var board = _gameData.ActiveBoard;
        var fen = Convert(board, !_isRotate);
        if (_isRotate)
        {
            // RotateFen(ref fen);
            fen = EditToBlackMoveFen(fen);
            // bot.whitePlayerType = Bot.PlayerType.AI;
            // bot.blackPlayerType = Bot.PlayerType.Human;
        }

        bot.customPosition = fen;
        try
        {
            bot.BotStart();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            Debug.Log($"BotStart IsWhiteToMove {bot.board.IsWhiteToMove}");
            bot.NotifyPlayerToMove();
            Debug.Log($"IsWhiteToMove {bot.board.IsWhiteToMove} + is AIPlayer {bot.playerToMove is AIPlayer}");
        }
    }

    private string EditToBlackMoveFen(string fen)
    {
        // Розділяємо FEN на частини
        var parts = fen.Split(' ');
        if (parts.Length < 2) 
            throw new ArgumentException("Invalid FEN format");

        // Змінюємо хід з "w" на "b", залишаючи решту незмінною
        parts[1] = "b";

        // Об'єднуємо частини назад у FEN-рядок
        return string.Join(" ", parts);
    }

    private void RotateFen(ref string fen)
    {
        // Розділити FEN по пробілу (розбиває на частини: положення фігур, черга ходу тощо)
        var parts = fen.Split(' ');
        if (parts.Length < 1) return;

        // Розділити положення фігур по рядках
        var boardRows = parts[0].Split('/');

        // Інвертувати дошку (перевертаємо рядки)
        Array.Reverse(boardRows);

        // Змінити колір фігур
        for (int i = 0; i < boardRows.Length; i++)
        {
            var newRow = new StringBuilder();
            foreach (var c in boardRows[i])
            {
                // Замінити білі фігури на чорні і навпаки
                if (char.IsUpper(c)) // Біла фігура
                    newRow.Append(char.ToLower(c));
                else if (char.IsLower(c)) // Чорна фігура
                    newRow.Append(char.ToUpper(c));
                else
                    newRow.Append(c); // Це цифра або символ розділення
            }
            boardRows[i] = newRow.ToString();
        }

        // Об'єднати перевернуту дошку назад
        parts[0] = string.Join("/", boardRows);

        // Змінити чергу ходу (w -> b, b -> w)
        if (parts.Length > 1)
            parts[1] = parts[1] == "w" ? "b" : "w";

        // Перезаписати решту частин FEN (тикація пішаків, можливі рокіровки тощо) без змін
        fen = string.Join(" ", parts);
    }


    private bool _lastMovePlayer;
    public void OnMoveChosen(Vector2Int from, Vector2Int to) // коли гравець походив
    {
        _lastMovePlayer = true;
        var move = new Move(PosToSquare(from), PosToSquare(to));
        Debug.Log($"OnMoveChosen Unity=({from.x},{from.y}-> {to.x},{to.y}) → Engine={move.StartSquare}->{move.TargetSquare}, {move.Value} , {move.ToString()}");
        // if (IsSelfKilling(from, to)) 
        OnMoveMade(move);
    }

    private bool IsSelfKilling(Vector2Int from, Vector2Int to)
    {
        var board = _gameData.ActiveBoard;
        var piece1 = board.GetCell(to.x, to.y).Piece;
        var piece2 = board.GetHistory()[board.GetHistory().Count - 1].KilledPiece;
        return piece1 != null && piece2 != null && piece1.OwnerId == piece2.OwnerId;
    }

    int PosToSquare(Vector2Int pos)
    {
        int file = pos.x;         
        int rank = 7 - pos.y;     

        return rank * 8 + file;
    }

    private Vector2Int SquareToPos(int squareIndex)
    {
        int file = squareIndex % 8;
        int rank = squareIndex / 8;

        return new Vector2Int(file, 7 - rank);
    }

    public void OnMoveMade(Move move) // коли походив бот або гравець  
    {
        Task.Run(() => { }).ContinueWithOnMainThread(task =>
        {
            (Vector2Int from, Vector2Int to) = Convert(move);
            Debug.Log($"OnMoveMade Engine={move.StartSquare}->{move.TargetSquare} → Unity=({from.x},{from.y})->({to.x},{to.y})");
            ulong moverId = _lastMovePlayer ? (ulong)0 : (ulong)1;
            _matchCore.UseMove(from, to, moverId);
            if(_lastMovePlayer) RestartBot();
            _lastMovePlayer = false;
        });
    }

    private (Vector2Int from, Vector2Int to) Convert(Move move)
    {
        Vector2Int from = SquareToPos(move.StartSquare);
        Vector2Int to = SquareToPos(move.TargetSquare);

        return (from, to);
    }

    private string Convert(AbstractBoard board, bool isWhitePerspective)
    {
        var sb = new StringBuilder();

        for (int rank = 7; rank >= 0; rank--) // FEN ranks 8 -> 1
        {
            int empty = 0;

            for (int file = 0; file < 8; file++) // files a -> h
            {
                int x = file;
                int y = 7 - rank;

                var cell = board.GetCell(x, y);
                var piece = cell.Piece;

                if (piece == null)
                {
                    empty++;
                    continue;
                }

                if (empty > 0)
                {
                    sb.Append(empty);
                    empty = 0;
                }

                sb.Append(GetFenChar(piece.PieceType, piece.Color));
            }

            if (empty > 0) sb.Append(empty);
            if (rank > 0) sb.Append('/');
        }

        sb.Append(" w - - 0 1");

        // Debug view у Unity
        string line = "";
        for (int y = 0; y < 8; y++)
        {
            line += $"\n{y}:";
            for (int x = 0; x < 8; x++)
            {
                var cell = board.GetCell(x, y);
                if (cell.Piece != null)
                    line += GetFenChar(cell.Piece.PieceType, cell.Piece.Color);
                else
                    line += ".";
            }
        }
        Debug.Log($"Unity board view: {line}");

        return sb.ToString();
    }

    private char GetFenChar(PieceType type, PieceColor color)
    {
        char c = type switch
        {
            PieceType.Pawn => 'p',
            PieceType.Knight => 'n',
            PieceType.Bishop => 'b',
            PieceType.Rook => 'r',
            PieceType.Queen => 'q',
            PieceType.King => 'k',
            _ => '?'
        };
        return color == PieceColor.White ? char.ToUpper(c) : c;
    }
}
}