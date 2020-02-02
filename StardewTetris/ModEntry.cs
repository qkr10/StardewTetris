using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace StardewTetris
{
	public class ModEntry : Mod
	{
		static int keyDownCount = 0;
		static bool[] keyStates = new bool[256];
		bool isTetrisRunning = false;
		//TetrisGame tetris;

		public override void Entry(IModHelper helper)
		{
			Array.Clear(keyStates, 0, 256);
			helper.Events.Input.ButtonPressed += OnButtonPressed;
			helper.Events.Input.ButtonReleased += OnButtonReleased;
			helper.Events.GameLoop.UpdateTicked += Update;
			//tetris.PutEmpty = PutEmpty;
			//tetris.PutBrick = PutBrick;
			//tetris.IsKeyDown = IsKeyDown;
			//tetris.GetKey = GetKey;
		}

		static void PutEmpty(int x, int y)
		{
			var brick = new StardewValley.Object(75, 1, false, -1, 0);
			Game1.getLocationFromName("Farm").dropObject(brick, new Vector2(x, y) * 64f, Game1.viewport, true, (Farmer)null);
		}
		static void PutBrick(int x, int y)
		{
			var brick = new StardewValley.Object(75, 1, false, -1, 0);
			Game1.getLocationFromName("Farm").dropObject(brick, new Vector2(x, y) * 64f, Game1.viewport, true, (Farmer)null);
		}
		static bool IsKeyDown()
		{
			return keyDownCount != 0;
		}
		static int GetKey()
		{
			for (int i = 0; i < 256; i++) {
				if (keyStates[i]) {
					return i;
				}
			}
			return -1;
		}

		private void Update(object sender, UpdateTickedEventArgs e)
		{
			if (!Context.IsWorldReady)
				return;

			if (!isTetrisRunning)
				return;
			//tetris.main();
		}

		private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
		{
			if (!Context.IsWorldReady)
				return;

			e.Button.TryGetKeyboard(out Keys key);
			if (key.ToString() == "OemSemicolon") {
				isTetrisRunning = !isTetrisRunning;
				for (int x = 60; x < 75; x++)
					for (int y = 20; y < 40; y++) {
						var brick = new StardewValley.Object(75, 1, false, -1, 0);
						Game1.getLocationFromName("Farm").dropObject(brick, new Vector2(x, y) * 64f, Game1.viewport, true, (Farmer)null);
					}
			}
			keyStates[(int)key] = true;
			keyDownCount++;
		}

		private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
		{
			if (!Context.IsWorldReady)
				return;

			e.Button.TryGetKeyboard(out Keys key);
			keyStates[(int)key] = false;
			keyDownCount--;
		}
	}
}