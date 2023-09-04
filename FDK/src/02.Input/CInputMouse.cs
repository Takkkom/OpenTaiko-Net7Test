using System;
using System.Numerics;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Silk.NET.Input;

namespace FDK
{
	public class CInputMouse : IInputDevice, IDisposable
	{
		// 定数

		public const int nマウスの最大ボタン数 = 8;


		// コンストラクタ

		public CInputMouse(IMouse mouse)
		{
			this.e入力デバイス種別 = E入力デバイス種別.Mouse;
			this.GUID = "";
			this.ID = 0;
			try
			{
				Trace.TraceInformation(mouse.Name + " を生成しました。");  // なぜか0x00のゴミが出るので削除
				this.strDeviceName = mouse.Name;
			}
			catch
			{
				Trace.TraceWarning("Mouse デバイスの生成に失敗しました。");
				throw;
			}

			mouse.Click += Mouse_Click;
			mouse.DoubleClick += Mouse_DoubleClick;
			mouse.MouseDown += Mouse_MouseDown;
			mouse.MouseUp += Mouse_MouseUp;
			mouse.MouseMove += Mouse_MouseMove;

			this.list入力イベント = new List<STInputEvent>(32);
		}


		// メソッド

		#region [ IInputDevice 実装 ]
		//-----------------
		public E入力デバイス種別 e入力デバイス種別 { get; private set; }
		public string GUID { get; private set; }
		public int ID { get; private set; }
		public List<STInputEvent> list入力イベント { get; private set; }
		public string strDeviceName { get; set; }

		public void tポーリング(bool bバッファ入力を使用する)
		{
			list入力イベント.Clear();
			
			for (int i = 0; i < MouseStates.Length; i++)
			{
				if (MouseStates[i].Item1)
				{
					if (MouseStates[i].Item2 >= 1)
					{
						MouseStates[i].Item2 = 2;
					}
					else
					{
						MouseStates[i].Item2 = 1;
						list入力イベント.Add(
							new STInputEvent()
							{
								nKey = i,
								b押された = true,
								b離された = false,
								nTimeStamp = SampleFramework.Game.TimeMs,
								nVelocity = 0,
							}
						);
					}
				}
				else
				{
					if (MouseStates[i].Item2 <= -1)
					{
						MouseStates[i].Item2 = -2;
					}
					else
					{
						MouseStates[i].Item2 = -1;
						list入力イベント.Add(
							new STInputEvent()
							{
								nKey = i,
								b押された = false,
								b離された = true,
								nTimeStamp = SampleFramework.Game.TimeMs,
								nVelocity = 0,
							}
						);
					}
				}
			}
		}
		public bool bキーが押された(int nButton)
		{
			return MouseStates[nButton].Item2 == 1;
		}
		public bool bキーが押されている(int nButton)
		{
			return MouseStates[nButton].Item2 >= 1;
		}
		public bool bキーが離された(int nButton)
		{
			return MouseStates[nButton].Item2 == -1;
		}
		public bool bキーが離されている(int nButton)
		{
			return MouseStates[nButton].Item2 <= -1;
		}
		//-----------------
		#endregion

		#region [ IDisposable 実装 ]
		//-----------------
		public void Dispose()
		{
			if(!this.bDispose完了済み)
			{
				if (this.list入力イベント != null)
				{
					this.list入力イベント = null;
				}
				this.bDispose完了済み = true;
			}
		}
		//-----------------
		#endregion


		// その他

		#region [ private ]
		//-----------------
		private (bool, int)[] MouseStates = new (bool, int)[12];
		private bool bDispose完了済み;

		private void Mouse_Click(IMouse mouse, MouseButton mouseButton, Vector2 vector2)
		{

		}

		private void Mouse_DoubleClick(IMouse mouse, MouseButton mouseButton, Vector2 vector2)
		{

		}

		private void Mouse_MouseDown(IMouse mouse, MouseButton mouseButton)
		{
			if (mouseButton != MouseButton.Unknown)
			{
				MouseStates[(int)mouseButton].Item1 = true;
			}
		}

		private void Mouse_MouseUp(IMouse mouse, MouseButton mouseButton)
		{
			if (mouseButton != MouseButton.Unknown)
			{
				MouseStates[(int)mouseButton].Item1 = false;
			}
		}

		private void Mouse_MouseMove(IMouse mouse, Vector2 vector2)
		{

		}
		//-----------------
		#endregion
	}
}
