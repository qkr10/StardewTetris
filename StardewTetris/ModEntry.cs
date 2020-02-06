using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace StardewTetris
{
	using StardewValleyObject = StardewValley.Object;

	public class ModEntry : Mod
	{
		static int keyDownCount = 0;
		static public bool[] keyStates = new bool[256];
		static StardewValleyObject brick = null;
		static StardewValleyObject empty = null;
		static TetrisGame tetris;
		static GameLocation farm;
		static int boardX = 50, boardY = 20;
		bool isTetrisRunning = false;

		Dictionary<string, int> keyValues = new Dictionary<string, int> {
			{ "Left", 200 },
			{ "Right", 201 },
			{ "Up", 202 },
			{ "Down", 203 },
			{ "C", 204 },
			{ "Z", 205 },
			{ "X", 206 },
			{ "Space", 207 }
		};

		public override void Entry(IModHelper helper)
		{
			Array.Clear(keyStates, 0, 256);
			helper.Events.GameLoop.SaveLoaded += SaveLoaded;
			helper.Events.Input.ButtonPressed += OnButtonPressed;
			helper.Events.Input.ButtonReleased += OnButtonReleased;
			helper.Events.GameLoop.UpdateTicked += Update;
			helper.Events.World.ObjectListChanged += OnObjectListChanged;
			tetris = new TetrisGame(boardX, boardY, PutEmpty, PutBrick, IsKeyDown, GetKey, GameOver);
		}

		static void PutEmpty(int x, int y)
		{
			if (empty == null) {
				Game1.getLocationFromName("farm").removeEverythingFromThisTile(x, y);
				empty = Game1.getLocationFromName("farm").getObjectAt(x, y);
				return;
			}
			if (empty == Game1.getLocationFromName("farm").getObjectAt(x, y)) {
				return;
			}
			Game1.getLocationFromName("farm").setObjectAt(x, y, empty);
		}

		static void PutBrick(int x, int y, int color)
		{
			if (brick == Game1.getLocationFromName("farm").getObjectAt(x, y))
				return;
			Game1.getLocationFromName("Farm").setObjectAt(x, y, brick);
		}

		static bool IsKeyDown()
		{
			return keyDownCount != 0;
		}

		static int GetKey()
		{
			for (int i = 0; i < 256; i++)
				if (keyStates[i])
					return i;
			return -1;
		}

		static void GameOver()
		{
			ResetBoard();
			tetris.Stop();
			return;
		}

		static private void ResetBoard()
		{
			for (int y = boardY; y < boardY + 22; y++) {
				for (int x = boardX; x < boardX + 12; x++) {
					PutEmpty(x, y);
					Game1.getLocationFromName("farm").removeTile(x, y, "Front");
				}
			}
			return;
		}

		private void InitBoard()
		{
			if (brick == null) {
				brick = new StardewValleyObject(75, 1, false, -1, 0);
			}

			for (int y = boardY; y < boardY + 22; y++) {
				for (int x = boardX; x < boardX + 12; x++) {
					PutEmpty(x, y);
				}
				//Monitor.Log($"y좌표 : {y} 타일 인덱스 합 : {sum}", LogLevel.Debug);
			}
			Game1.getLocationFromName("farm").setMapTileIndex(boardX, boardY + 21, 0, "Front");
			Game1.getLocationFromName("farm").setMapTileIndex(boardX, boardY, 1, "Front");
			Game1.getLocationFromName("farm").setMapTileIndex(boardX + 11, boardY, 2, "Front");
			Game1.getLocationFromName("farm").setMapTileIndex(boardX + 11, boardY + 21, 3, "Front");
			for (int x = boardX + 1; x < boardX + 11; x++)
				Game1.getLocationFromName("farm").setMapTileIndex(x, boardY, 5, "Front");
			for (int x = boardX + 1; x < boardX + 11; x++)
				Game1.getLocationFromName("farm").setMapTileIndex(x, boardY + 21, 5, "Front");
			for (int y = boardY + 1; y < boardY + 21; y++)
				Game1.getLocationFromName("farm").setMapTileIndex(boardX, y, 6, "Front");
			for (int y = boardY + 1; y < boardY + 21; y++)
				Game1.getLocationFromName("farm").setMapTileIndex(boardX + 11, y, 6, "Front");
		}

		private void Update(object sender, UpdateTickedEventArgs e)
		{
			if (!Context.IsWorldReady)
				return;
		}

		private void SaveLoaded(object sender, SaveLoadedEventArgs e)
		{
			if (!Context.IsWorldReady)
				return;
		}

		private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			if (!Context.IsWorldReady)
				return;

			e.Button.TryGetKeyboard(out Keys key);
			string keyString = key.ToString();
			if (keyString == "OemSemicolon") {
				isTetrisRunning = !isTetrisRunning;
				if (isTetrisRunning) {
					InitBoard();
					tetris.Start();
				}
				else {
					ResetBoard();
					tetris.Stop();
				}
			}
			
			if (!keyValues.ContainsKey(keyString))
				return;
			keyStates[keyValues[keyString]] = true;
			keyDownCount++;
		}

		private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
		{
			if (!Context.IsWorldReady)
				return;

			e.Button.TryGetKeyboard(out Keys key);
			string keyString = key.ToString();

			if (!keyValues.ContainsKey(keyString))
				return;
			keyStates[keyValues[keyString]] = false;
			keyDownCount--;
		}

		static readonly int[,] Tshape = new int[2, 3] { { 1, 1, 1 }, { 0, 1, 0 } };
		static readonly int[][] Tindex = new int[4][] {
			new int[2] {0, 0},
			new int[2] {0, 1},
			new int[2] {0, 2},
			new int[2] {1, 1} };
		static readonly int[] stoneItemCodes = { 2, 4, 75, 76, 77, 290, 343, 390, 411, 449, 450, 751, 760, 762 };

		//private void OnObjectListChanged(object sender, ObjectListChangedEventArgs e)
		//{
		//	if (Game1.currentLocation.name != "farm")
		//		return;

		//	//추가된 오브젝트가 돌일때, T모양으로 설치되어 있으면 T의 가운데 돌 기준으로 테트리스 시작
		//	foreach (var pair in e.Added) {
		//		Vector2 cor = pair.Key;
		//		StardewValleyObject obj = pair.Value;
		//		if (Array.Exists(stoneItemCodes, O => O == obj.parentSheetIndex)) {
		//			foreach (int[] ind in Tindex)
		//				for (int x = 0; x < 3; x++)
		//					for (int y = 0; y < 2; y++) {
		//						Game1.getLocationFromName("farm").
		//					}
		//		}
		//	}
		//}
	}
}