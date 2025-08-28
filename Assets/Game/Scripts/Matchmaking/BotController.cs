using System.Text;
using System.Threading.Tasks;
using Board.Piece;
using Chess.Game;
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
            var gameObject = Instantiate(new GameObject("GameStarter"));
            var matchStarter = gameObject.AddComponent<MatchStarter>();
            matchStarter.SetMatchCore(matchCorePrefab);
        }
    }
    
    // private Bot _bot;
    private ulong _botPlayerId;
    private MatchData _matchData;
    private MatchCore _matchCore;
    private bool _isRotate;

    public void InitBotController(MatchData matchData, MatchCore matchCore)
    {
        _matchData = matchData;
        _matchCore = matchCore;
        _botPlayerId = matchData.Player2.PlayerId;
        _isRotate = matchData.Player2.IsRotate;
        Debug.Log($"IsRotate {_isRotate}");
        var board = _gameData.ActiveBoard;

        bot.loadCustomPosition = true;
        bot.customPosition = Convert(board, !_isRotate);

        bot.whitePlayerType = matchData.Player1.IsWhite ? Bot.PlayerType.Human : Bot.PlayerType.AI;
        bot.blackPlayerType = !matchData.Player1.IsWhite ? Bot.PlayerType.Human : Bot.PlayerType.AI;
        
        bot.onMoveMade += OnMoveMade;
        bot.BotStart();
    }


    void OnApplicationQuit()
    {
        // if (_bot.IsThinking)
        // {
        //     _bot.StopThinking();
        // }
        // _bot.Quit();
    }

    private bool _lastMovePlayer;
    public void OnMoveChosen(Vector2Int from, Vector2Int to) // коли гравець походив
    {
        _lastMovePlayer = true;
        var move = new Move(PosToSquare(from), PosToSquare(to));
        Debug.Log($"OnMoveChosen Unity=({from.x},{from.y}-> {to.x},{to.y}) → Engine={move.StartSquare}->{move.TargetSquare}, {move.Value} , {move.ToString()}");
        bot.OnMoveChosen(move);
    }

    int PosToSquare(Vector2Int pos)
    {
        // Unity (0,0) зверху → Engine (a1 = 0 знизу)
        int file = pos.x;         
        int rank = 7 - pos.y;     

        // if (_isRotate)
        // {
        //     file = 7 - file;
        //     rank = 7 - rank;
        // }

        return rank * 8 + file;
    }

    private Vector2Int SquareToPos(int squareIndex)
    {
        int file = squareIndex % 8;
        int rank = squareIndex / 8;

        // if (_isRotate)
        // {
        //     file = 7 - file;
        //     rank = 7 - rank;
        // }

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