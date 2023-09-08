using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using FDK;
using System.Drawing;

namespace TJAPlayer3
{
	internal class CAct演奏Drums判定文字列 : CActivity
	{
		// コンストラクタ

		public CAct演奏Drums判定文字列()
		{
			base.IsDeActivated = true;
		}

        public override void Activate()
        {
			JudgeAnimes = new JudgeAnime[5, 512];
			for (int i = 0; i < 512; i++)
			{
				JudgeAnimes[0, i] = new JudgeAnime();
				JudgeAnimes[1, i] = new JudgeAnime();
				JudgeAnimes[2, i] = new JudgeAnime();
				JudgeAnimes[3, i] = new JudgeAnime();
				JudgeAnimes[4, i] = new JudgeAnime();
			}
            base.Activate();
        }

        public override void DeActivate()
        {
			for (int i = 0; i < 512; i++)
            {
				JudgeAnimes[0, i] = null;
				JudgeAnimes[1, i] = null;
				JudgeAnimes[2, i] = null;
				JudgeAnimes[3, i] = null;
				JudgeAnimes[4, i] = null;
			}
            base.DeActivate();
        }

        // CActivity 実装（共通クラスからの差分のみ）
        public override int Draw()
		{
			if (!base.IsDeActivated)
			{
				for (int i = 0; i < 512; i++)
				{
					for(int j = 0; j < 5; j++)
					{
						if (JudgeAnimes[j, i].counter.IsStoped) continue;
						JudgeAnimes[j, i].counter.Tick();

						if (TJAPlayer3.Tx.Judge != null)
						{
							float moveValue = CubicEaseOut(JudgeAnimes[j, i].counter.CurrentValue / 410.0f) - 1.0f;

							float x = 0;
							float y = 0;

							if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
							{
								x = TJAPlayer3.Skin.Game_Judge_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * j);
								y = TJAPlayer3.Skin.Game_Judge_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * j);
							}
							else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
							{
								x = TJAPlayer3.Skin.Game_Judge_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * j);
								y = TJAPlayer3.Skin.Game_Judge_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * j);
							}
							else
							{
								x = TJAPlayer3.Skin.Game_Judge_X[j];
								y = TJAPlayer3.Skin.Game_Judge_Y[j];
							}
							x += (moveValue * TJAPlayer3.Skin.Game_Judge_Move[0]) + TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLX(j);
							y += (moveValue * TJAPlayer3.Skin.Game_Judge_Move[1]) + TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLY(j);

							TJAPlayer3.Tx.Judge.Opacity = (int)(255f - (JudgeAnimes[j, i].counter.CurrentValue >= 360 ? ((JudgeAnimes[j, i].counter.CurrentValue - 360) / 50.0f) * 255f : 0f));
							TJAPlayer3.Tx.Judge.t2D描画(x, y, JudgeAnimes[j, i].rc);
                        }
						
					}
				}
			}
            return 0;
		}

		public void Start(int player, E判定 judge)
		{
			JudgeAnimes[player, JudgeAnime.Index].counter.Start(0, 410, 1, TJAPlayer3.Timer);
			JudgeAnimes[player, JudgeAnime.Index].Judge = judge;

			//int njudge = judge == E判定.Perfect ? 0 : judge == E判定.Good ? 1 : judge == E判定.ADLIB ? 3 : judge == E判定.Auto ? 0 : 2;

			int njudge = 2;
			if (JudgesDict.ContainsKey(judge))
            {
				njudge = JudgesDict[judge];
            }

			int height = TJAPlayer3.Tx.Judge.szテクスチャサイズ.Height / 5;
			JudgeAnimes[player, JudgeAnime.Index].rc = new Rectangle(0, (int)njudge * height, TJAPlayer3.Tx.Judge.szテクスチャサイズ.Width, height);
			JudgeAnime.Index++;
			if (JudgeAnime.Index >= 511) JudgeAnime.Index = 0;
		}

		// その他

		#region [ private ]
		//-----------------

		private static Dictionary<E判定, int> JudgesDict = new Dictionary<E判定, int>
		{
			[E判定.Perfect] = 0,
			[E判定.Auto] = 0,
			[E判定.Good] = 1,
			[E判定.Bad] = 2,
			[E判定.Miss] = 2,
			[E判定.ADLIB] = 3,
			[E判定.Mine] = 4,
		};

		private JudgeAnime[,] JudgeAnimes;
		private class JudgeAnime
        {
			public static int Index;
			public E判定 Judge;
			public Rectangle rc;
			public CCounter counter = new CCounter();
		}

		private float CubicEaseOut(float p)
		{
			float f = (p - 1);
			return f * f * f + 1;
		}
		//-----------------
		#endregion
	}
}
