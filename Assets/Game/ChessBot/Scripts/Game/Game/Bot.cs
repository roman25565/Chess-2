using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Chess.Core;
using Chess.UI;
using Chess.Players;
using UnityEngine.InputSystem;

namespace Chess.Game
{
	public class Bot : MonoBehaviour
	{
		public event System.Action onPositionLoaded;
		public event System.Action<Move> onMoveMade;

		public enum PlayerType { Human, AI }

		[Header("Start Position")]
		public bool loadCustomPosition;
		public string customPosition = "1rbq1r1k/2pp2pp/p1n3p1/2b1p3/R3P3/1BP2N2/1P3PPP/1NBQ1RK1 w - - 0 1";

		[Header("Players")]
		public PlayerType whitePlayerType;
		public PlayerType blackPlayerType;

		[Header("References")]
		public AISettings aiSettings;
		public TMPro.TMP_Text resultUI;

		[Header("Debug")]
		public string currentFen;
		public ulong zobristDebug;

		// Internal stuff
		GameResult.Result gameResult;

		Player whitePlayer;
		Player blackPlayer;
		Player playerToMove;
		BoardUI boardUI;

		public Core.Board board { get; private set; }
		Core.Board searchBoard; // Duplicate version of board used for ai search

		public void BotStart()
		{
			boardUI = FindObjectOfType<BoardUI>();
			board = new Core.Board();
			searchBoard = new Core.Board();
			aiSettings.diagnostics = new Searcher.SearchDiagnostics();

			NewGame(whitePlayerType, blackPlayerType);
		}

		void Update()
		{
			UpdateGame();
			//UpdateDebugInfo();
		}

		void UpdateGame()
		{
			if (gameResult == GameResult.Result.Playing)
			{
				playerToMove.Update();
			}

		}

		void UpdateDebugInfo()
		{
			zobristDebug = board.currentGameState.zobristKey;
			ulong generatedKey = Zobrist.CalculateZobristKey(board);
			if (generatedKey != zobristDebug)
			{
				Debug.Log("Key Error: incremental: " + zobristDebug + "  gen: " + generatedKey);
			}

		}

		public void OnMoveChosen(Move move)
		{
			bool animateMove = playerToMove is AIPlayer;
			try
			{

				Debug.Log($"move: {move.Value}");
				board.MakeMove(move);
				searchBoard.MakeMove(move);

				currentFen = FenUtility.CurrentFen(board);
			}
			catch (Exception e)
			{
				Debug.LogWarning(e);
				Console.WriteLine(e);
				throw;
			}
			finally
			{
				onMoveMade?.Invoke(move);
			}
			boardUI.UpdatePosition(board, move, animateMove);


			NotifyPlayerToMove();
		}

		public void NewGame(bool humanPlaysWhite)
		{
			boardUI.SetPerspective(humanPlaysWhite);
			NewGame((humanPlaysWhite) ? PlayerType.Human : PlayerType.AI, (humanPlaysWhite) ? PlayerType.AI : PlayerType.Human);
		}

		public void NewComputerVersusComputerGame()
		{
			boardUI.SetPerspective(true);
			NewGame(PlayerType.AI, PlayerType.AI);
		}

		void NewGame(PlayerType whitePlayerType, PlayerType blackPlayerType)
		{
			if (loadCustomPosition)
			{
				currentFen = customPosition;
				board.LoadPosition(customPosition);
				searchBoard.LoadPosition(customPosition);
			}
			else
			{
				currentFen = FenUtility.StartPositionFEN;
				board.LoadStartPosition();
				searchBoard.LoadStartPosition();
			}
			onPositionLoaded?.Invoke();
			boardUI.UpdatePosition(board);
			boardUI.ResetSquareColours();

			CreatePlayer(ref whitePlayer, whitePlayerType);
			CreatePlayer(ref blackPlayer, blackPlayerType);



			gameResult = GameResult.Result.Playing;

			NotifyPlayerToMove();
		}

		void NotifyPlayerToMove()
		{
			Debug.Log("Notify Player To Move");
			gameResult = GameResult.GetGameState(board);

			if (gameResult == GameResult.Result.Playing)
			{
				playerToMove = (board.IsWhiteToMove) ? whitePlayer : blackPlayer;

				playerToMove.NotifyTurnToMove();

			}
			else
			{
				GameOver();
			}
		}

		void GameOver()
		{
			Debug.Log("Game Over " + gameResult);
			PrintGameResult(gameResult);
		}

		void PrintGameResult(GameResult.Result result)
		{
			if (result == GameResult.Result.Playing)
			{
				resultUI.text = "";
			}
			else
			{
				string subtitleSettings = $"<color=#787878> <size=75%>";
				resultUI.text = "Game Over\n" + subtitleSettings;

				if (result is GameResult.Result.WhiteIsMated or GameResult.Result.BlackIsMated)
				{
					string winner = result == GameResult.Result.WhiteIsMated ? "Black" : "White";
					resultUI.text += $"{winner} wins by checkmate";
				}
				else if (result is GameResult.Result.WhiteTimeout or GameResult.Result.BlackTimeout)
				{
					string winner = result == GameResult.Result.WhiteTimeout ? "Black" : "White";
					resultUI.text += $"{winner} wins on time";
				}
				else if (result == GameResult.Result.FiftyMoveRule)
				{
					resultUI.text += "Draw by 50 move rule";
				}
				else if (result == GameResult.Result.Repetition)
				{
					resultUI.text += "Draw by 3-fold repetition";
				}
				else if (result == GameResult.Result.Stalemate)
				{
					resultUI.text += "Draw by stalemate";
				}
				else if (result == GameResult.Result.InsufficientMaterial)
				{
					resultUI.text += "Draw due to insufficient material";
				}
			}
		}

		void CreatePlayer(ref Player player, PlayerType playerType)
		{
			if (player != null)
			{
				player.onMoveChosen -= OnMoveChosen;
			}

			if (playerType == PlayerType.Human)
			{
				player = new HumanPlayer(board);
			}
			else
			{
				player = new AIPlayer(searchBoard, aiSettings);
			}
			player.onMoveChosen += OnMoveChosen;
		}
	}
}