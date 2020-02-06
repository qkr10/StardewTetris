using System;
using System.Collections;
using System.Threading;

namespace StardewTetris
{
	public delegate void PutEmptyDel(int x, int y);
	public delegate void PutBrickDel(int x, int y, int color);
	public delegate bool IsKeyDownDel();
	public delegate int GetKeyDel();
	public delegate void GameOverDel();

	class TetrisGame
	{
		const int Left = 200;
		const int Right = 201;
		const int Up = 202;
		const int Down = 203;
		const int Hold = 204;
		const int LeftRot = 205;
		const int RightRot = 206;
		const int Space = 207;

		int[,,] Shape = new int[7, 4, 8]{
			{ {0,0,1,0,2,0,-1,0}, {0,0,0,1,0,-1,0,-2}, {0,0,1,0,2,0,-1,0}, {0,0,0,1,0,-1,0,-2} },
			{ {0,0,1,0,0,1,1,1}, {0,0,1,0,0,1,1,1}, {0,0,1,0,0,1,1,1}, {0,0,1,0,0,1,1,1} },
			{ {0,0,-1,0,0,-1,1,-1}, {0,0,0,1,-1,0,-1,-1}, {0,0,-1,0,0,-1,1,-1}, {0,0,0,1,-1,0,-1,-1} },
			{ {0,0,-1,-1,0,-1,1,0}, {0,0,-1,0,-1,1,0,-1}, {0,0,-1,-1,0,-1,1,0}, {0,0,-1,0,-1,1,0,-1} },
			{ {0,0,-1,0,1,0,-1,-1}, {0,0,0,-1,0,1,-1,1}, {0,0,-1,0,1,0,1,1}, {0,0,0,-1,0,1,1,-1} },
			{ {0,0,1,0,-1,0,1,-1}, {0,0,0,1,0,-1,-1,-1}, {0,0,1,0,-1,0,-1,1}, {0,0,0,-1,0,1,1,1} },
			{ {0,0,-1,0,1,0,0,1}, {0,0,0,-1,0,1,1,0}, {0,0,-1,0,1,0,0,-1}, {0,0,-1,0,0,-1,0,1} },
		};
		//구조체 3차원 배열으로 벽돌모양을 표현한다
		//Shape[벽돌모양, 벽돌의회전, 좌표값]

		enum CellStates { EMPTY, BRICK, WALL };

		int[,] spinCenter = new int[10, 2]{
			{0, 0},{1, 0},{-1, 0},
			{0, 1},{1, 1},{-1, 1},
			{0, 2},{1, 2},{-1, 2},
			{0, -1}
		};

		int[] box = new int[] { 0, 1, 2, 3, 4, 5, 6 };

		int boardX = 50;
		int boardY = 20;
		const int boardW = 10;
		const int boardH = 20;

		int[,] board = new int[boardW + 2, boardH + 2];
		int nx, ny;
		int brick, rot;
		int holdTrig;
		int nbrick = 6;
		int hbrick = 8;
		long DropTime = 0;

		public PutEmptyDel PutEmpty;
		public PutBrickDel PutBrick;
		public IsKeyDownDel IsKeyDown;
		public GetKeyDel GetKey;
		public GameOverDel GameOver;

		Random rand = new Random();

		Thread thread;

		public TetrisGame(int boardX, int boardY, 
			PutEmptyDel putEmpty, PutBrickDel putBrick,
			IsKeyDownDel isKeyDown, GetKeyDel getKey, GameOverDel gameOver)
		{
			this.boardX = boardX;
			this.boardY = boardY;
			PutEmpty = putEmpty;
			PutBrick = putBrick;
			IsKeyDown = isKeyDown;
			GetKey = getKey;
			GameOver = gameOver;
		}

		public void Start()
		{
			thread = new Thread(new ThreadStart(Running));
			thread.Start();
		}

		public void Stop()
		{
			thread.Abort();
		}

		private void Running()
		{
			int x, y;
			for (x = 0; x < boardW + 2; x++)
				for (y = 0; y < boardH + 2; y++)
					board[x, y] = (int)((y == 0 || y == boardH + 1 || x == 0 || x == boardW + 1) ? CellStates.WALL : CellStates.EMPTY);
			
			nbrick = GetNextBrick(nbrick);
			while (true) {//무한히 벽돌을 생성한다
				brick = nbrick;
				nbrick = GetNextBrick(nbrick);
				holdTrig = 1;

				nx = boardW / 2;
				ny = 3;
				rot = 0;
				PrintBri();

				if (GetAround(nx, ny, brick, rot) != (int)CellStates.EMPTY)
					break;//벽돌 생성이 안되면 게임 종료
				DropTime = clock();

				long buttonTime = clock();
				long gravityTime = clock();
				while (true) {//벽돌이 하강
					if (gravityTime + 500 < clock()) {
						gravityTime = clock();
						if (MoveDown())
							break;
					}
					if (buttonTime + 100 < clock()) {
						buttonTime = clock();
						if (ProcessKey())
							break;
					}
				}
			}
		}

		private void PrintBri()
		{
			for (int i = 0; i < 4; i++) {
				int xCor = boardX + Shape[brick, rot, i * 2] + nx;
				int yCor = boardY + Shape[brick, rot, i * 2 + 1] + ny;
				PutBrick(xCor, yCor, brick);
			}
		}
		private void EraseBri()
		{
			for (int i = 0; i < 4; i++) {
				int xCor = boardX + Shape[brick, rot, i * 2] + nx;
				int yCor = boardY + Shape[brick, rot, i * 2 + 1] + ny;
				PutEmpty(xCor, yCor);
			}
		}

		bool ProcessKey()
		{
			int ch, trot;
			int xx = 0, yy = 0;
			bool ret = false;

			if (IsKeyDown()) {
				ch = GetKey();
				switch (ch) {
					case Left:
						if (GetAround(nx - 1, ny, brick, rot) == (int)CellStates.EMPTY) {
							EraseBri();
							nx--;
							PrintBri();
							DropTime = clock();
						}
						break;
					case Right:
						if (GetAround(nx + 1, ny, brick, rot) == (int)CellStates.EMPTY) {
							EraseBri();
							nx++;
							PrintBri();
							DropTime = clock();
						}
						break;
					case Down:
						ret = MoveDown();
						break;
					case Hold:          //c입력시 블럭 홀드
						if (holdTrig == 0)
							break;
						holdTrig = 0;
						HoldBrick();
						PrintBri();
						break;
					case Up:
					case LeftRot:
						trot = (rot == 3 ? 0 : rot + 1);
						if (GetAroundSpin(nx, ny, brick, trot, ref xx, ref yy) == (int)CellStates.EMPTY) {
							EraseBri();
							rot = trot;
							nx = xx;
							ny = yy;
							PrintBri();
							DropTime = clock();
						}
						break;
					case RightRot:
						trot = (rot == 0 ? 3 : rot - 1);
						if (GetAroundSpin(nx, ny, brick, trot, ref xx, ref yy) == (int)CellStates.EMPTY) {
							EraseBri();
							rot = trot;
							nx = xx;
							ny = yy;
							PrintBri();
							DropTime = clock();
						}
						break;
					case Space:
						while (MoveDown() == false) {; }
						return true;
				}
			}
			return ret;
		}

		int GetAround(int x, int y, int b, int r)   //벽돌 주면에 무엇이 있는지 검사하여 벽돌의 이동 및 회전가능성 조사
		{                                       //이동중인 벽돌의 주변을 조사하는 것이 아니므로 인수로 전달된 위치의 벽돌모양을 참조한다
			int k = (int)CellStates.EMPTY;
			for (int i = 0; i < 4; i++) {
				int xx = x + Shape[b, r, i * 2];
				int yy = y + Shape[b, r, i * 2 + 1];
				if (xx > boardW + 1 || xx < 0 || yy > boardY + 1 || yy < 0)
					continue;
				k = Math.Max(k, board[xx, yy]);
			}
			return k;
		}

		int GetAroundSpin(int x, int y, int b, int r, ref int retx, ref int rety)
		{
			int i;
			for (int j = 0; j < 10; j++) {
				int k = (int)CellStates.EMPTY;
				int xx = spinCenter[j,0] + x;
				int yy = spinCenter[j,1] + y;
				for (i = 0; i < 4; i++) {
					int xxx = xx + Shape[b, r, i * 2];
					int yyy = yy + Shape[b, r, i * 2 + 1];
					if (xxx > boardW + 1 || xxx < 0 || yyy > boardY + 1 || yyy < 0)
						continue;
					k = Math.Max(k, board[xxx, yyy]);
				}
				if (k == (int)CellStates.EMPTY) {
					retx = xx;
					rety = yy;
					return (int)CellStates.EMPTY;
				}
			}
			return 1;
		}

		bool MoveDown()   //벽돌을 한칸 아래로 이동시킨다.
		{
			if (GetAround(nx, ny + 1, brick, rot) != (int)CellStates.EMPTY) {
				if (DropTime + 500 >= clock()) {
					return false;
				}
				TestFull();
				return true;//바닥에 닿았다면 TestFull() 한 후 TRUE를 리턴한다.
			}
			EraseBri();
			ny++;
			DropTime = clock();
			PrintBri();
			return false;
		}

		private long clock()
		{
			return DateTime.Now.Ticks / 10000;
		}

		void TestFull()              //수평으로 다 채워진 줄을 찾아 삭제한다
		{
			int i, x, y, ty;
			int count = 0;
			int[] arScoreInc = { 0, 1, 3, 8, 20 };

			for (i = 0; i < 4; i++) {
				board[nx + Shape[brick,rot,i*2],ny + Shape[brick,rot,i*2+1]] = (int)CellStates.BRICK;
			}

			for (y = 1; y < boardH + 1; y++) {
				for (x = 1; x < boardW + 1; x++) {
					if (board[x,y] != (int)CellStates.BRICK) break;
				}
				if (x == boardW + 1) {
					count++;
					for (ty = y; ty > 1; ty--) {
						for (x = 1; x < boardW + 1; x++) {
							board[x,ty] = board[x,ty - 1];
						}
					}
				}
			}
		}

		void HoldBrick()
		{ //블럭을 홀드한다
			EraseBri();
			if (hbrick == 8) {
				hbrick = brick;
				brick = nbrick;
				nbrick = GetNextBrick(nbrick);
			}
			else {
				swap(ref brick, ref hbrick);
			}
			nx = boardW / 2;
			ny = 3;
			rot = 0;
			for (int i = 0; i < 4; i++) {
				int xCor = boardX + (Shape[brick, rot, i * 2] + nx) * 2;
				int yCor = boardY + Shape[brick, rot, i * 2 + 1] + ny;
				PrintBri();
			}
		}

		private int Random(int v)
		{
			return rand.Next(v);
		}

		private void swap(ref int v1, ref int v2)
		{
			int temp = v1;
			v1 = v2;
			v2 = temp;
		}

		int GetNextBrick(int previousBrick)
		{
			if (previousBrick == box[6]) {
				Shuffle();
				return box[0];
			}
			int i = 0;
			while (box[i++] != previousBrick);
			return box[i];
		}

		void Shuffle()
		{
			int[] start = box;
			for (int i = 0; i < 6; i++)
				swap(ref start[i], ref start[i + Random(6 - i) + 1]);
		}
	}
}