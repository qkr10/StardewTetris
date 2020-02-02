using System;
using System.Collections;
using System.Threading;

namespace StardewTetris
{
	public delegate void PutEmptyDel(int x, int y);
	public delegate void PutBrickDel(int x, int y);
	public delegate bool IsKeyDownDel();
	public delegate int GetKeyDel();
	public delegate int MoveCursorDel();

	class TetrisGame
	{
		const int LEFT = 75;
		const int RIGHT = 77;
		const int UP = 72;
		const int DOWN = 80;
		const int ESC = 27;
		const int BX = 5;
		const int BY = 1;
		const int BW = 10;
		const int BH = 20;
		const int PGUP = 73;
		const int PGDN = 81;
		const int HOLD = 104;
		const int DO_SUFFLE = -1;

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
		
		enum CellStates{ EMPTY, BRICK, WALL };

		int[,] spinCenter = new int[10, 2]{
			{0, 0},{1, 0},{-1, 0},
			{0, 1},{1, 1},{-1, 1},
			{0, 2},{1, 2},{-1, 2},
			{0, -1}
		};

		int[] box = new int[]{ 0, 1, 2, 3, 4, 5, 6 };

		int[,] board = new int[BW + 2,BH + 2];
		int nx, ny;
		int brick, rot;
		int nbrick;
		int bricknum;
		int hbrick = 8;
		int HoldTrig = 1;
		int DropTime = 1000000000;

		static void PutEmptyVir(int x, int y)
		{
			throw new NotImplementedException();
		}
		static void PutBrickVir(int x, int y)
		{
			throw new NotImplementedException();
		}
		static bool IsKeyDownVir()
		{
			throw new NotImplementedException();
		}
		static int GetKeyVir()
		{
			throw new NotImplementedException();
		}

		public PutEmptyDel PutEmpty = PutEmptyVir;
		public PutBrickDel PutBrick = PutBrickVir;
		public IsKeyDownDel IsKeyDown = IsKeyDownVir;
		public GetKeyDel GetKey = GetKeyVir;

		Random rand = new Random();

		Thread thread;

		public void Start()
		{
			thread = new Thread(new ThreadStart(Running));
			thread.Start();
		}

		private void Running()
		{
			int nFrame, nStay;
			int x, y;
			while (true) {
				for (x = 0; x < BW + 2; x++)
					for (y = 0; y < BH + 2; y++)
						board[x, y] = (int)((y == 0 || y == BH + 1 || x == 0 || x == BW + 1) ? CellStates.WALL : CellStates.EMPTY);
				//board 배열에 WALL이나 EMPTY를 넣음
				//BW 와 BH는 벽돌이 실제 움직이는 공간이므로 +2씩 한다
				nFrame = 20;
				bricknum = 0;

				Shuffle();
				nbrick = GetNextBrick(nbrick);
				while (true) {
					bricknum++;
					brick = nbrick;
					nbrick = GetNextBrick(nbrick);

					nx = BW / 2;      //nx,ny는 떨어지고있는 벽돌의 좌표값
					ny = 3;
					rot = 0;
					PrintBri();

					if (GetAround(nx, ny, brick, rot) != (int)CellStates.EMPTY) break;
					nStay = nFrame;
					while (true) {
						if (--nStay == 0) {
							nStay = nFrame;
							if (MoveDown()) break;
						}
						if (ProcessKey()) break;
						delay(1000 / 20);
					}
					if (bricknum % 10 == 0 && nFrame > 5) {
						nFrame--;
					}
				}
			}
		}

		private void delay(int v)
		{
			int old = clock();
			while (old + v < clock()) ;
		}

		private void PrintBri()
		{
			for (int i = 0; i < 4; i++) {
				int xCor = BX + (Shape[brick, rot, i * 2] + nx) * 2;
				int yCor = BY + Shape[brick, rot, i * 2 + 1] + ny;
				PutBrick(xCor, yCor);
			}
		}
		private void EraseBri()
		{
			for (int i = 0; i < 4; i++) {
				int xCor = BX + (Shape[brick, rot, i * 2] + nx) * 2;
				int yCor = BY + Shape[brick, rot, i * 2 + 1] + ny;
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
					case LEFT:
						if (GetAround(nx - 1, ny, brick, rot) == (int)CellStates.EMPTY) {
							EraseBri();
							nx--;
							PrintBri();
							DropTime = clock();
						}
						break;
					case RIGHT:
						if (GetAround(nx + 1, ny, brick, rot) == (int)CellStates.EMPTY) {
							EraseBri();
							nx++;
							PrintBri();
							DropTime = clock();
						}
						break;
					case UP:
						trot = (rot == 3 ? 0 : rot + 1);
						if (GetAround(nx, ny, brick, trot) == (int)CellStates.EMPTY) {
							EraseBri();
							rot = trot;
							PrintBri();
							DropTime = clock();
						}
						break;
					case DOWN:
						ret = MoveDown();
						break;
					case HOLD:
						HoldBrick();
						break;
					case 'c':          //c입력시 블럭 홀드
						if (HoldTrig == 0)
							break;
						HoldBrick();
						PrintBri();
						break;
					case 'z':
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
					case 'x':
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
					case ' ':
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
				k = Math.Max(k, board[x + Shape[b,r,i*2], y + Shape[b,r,i*2+1]]);
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
				for (i = 0; i < 4; i++)
					k = Math.Max(k, board[xx + Shape[b,r,i*2],yy + Shape[b,r,i*2+1]]);
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
				if (DropTime + 500 >= clock())
					return false;
				HoldTrig = 1;
				TestFull();
				return true;//바닥에 닿았다면 TestFull() 한 후 TRUE를 리턴한다.
			}
			EraseBri();
			ny++;
			DropTime = clock();
			PrintBri();
			return false;
		}

		private int clock()
		{
			return (int)(DateTime.Now.Ticks / 10000);
		}

		void TestFull()              //수평으로 다 채워진 줄을 찾아 삭제한다
		{
			int i, x, y, ty;
			int count = 0;
			int[] arScoreInc = { 0, 1, 3, 8, 20 };

			for (i = 0; i < 4; i++) {
				board[nx + Shape[brick,rot,i*2],ny + Shape[brick,rot,i*2+1]] = (int)CellStates.BRICK;
			}

			for (y = 1; y < BH + 1; y++) {
				for (x = 1; x < BW + 1; x++) {
					if (board[x,y] != (int)CellStates.BRICK) break;
				}
				if (x == BW + 1) {
					count++;
					for (ty = y; ty > 1; ty--) {
						for (x = 1; x < BW + 1; x++) {
							board[x,ty] = board[x,ty - 1];
						}
					}
					delay(200);
				}
			}
		}

		void HoldBrick()
		{ //블럭을 홀드한다
			HoldTrig = 0;
			if (hbrick == 8) {
				hbrick = brick;
				brick = nbrick;
				nbrick = GetNextBrick(nbrick);
			}
			else {
				swap(ref brick, ref hbrick);
			}
			nx = BW / 2;
			ny = 3;
			for (int i = 0; i < 4; i++) {
				int xCor = BX + (Shape[brick, rot, i * 2] + nx) * 2;
				int yCor = BY + Shape[brick, rot, i * 2 + 1] + ny;
				PrintBri();
			}
		}

		void Shuffle()
		{
			int[] start = box;
			for (int i = 0; i < 7; i++) {
				swap(ref start[i], ref start[i + Random(7 - i)]);
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
			v2 = v1;
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
	}
}