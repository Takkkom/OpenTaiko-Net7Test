using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Silk.NET.Input;

namespace FDK
{
	public class CInputKeyboard : IInputDevice, IDisposable
	{
		// コンストラクタ

		public CInputKeyboard(IReadOnlyList<IKeyboard> keyboards)
		{
			this.e入力デバイス種別 = E入力デバイス種別.Keyboard;
			this.GUID = "";
			this.ID = 0;

			foreach (var keyboard in keyboards)
			{
				keyboard.KeyDown += KeyDown;
				keyboard.KeyUp += KeyUp;
				keyboard.KeyChar += KeyChar;
			}

			//this.timer = new CTimer( CTimer.E種別.MultiMedia );
			this.list入力イベント = new List<STInputEvent>(32);
			// this.ct = new CTimer( CTimer.E種別.PerformanceCounter );
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
			for (int i = 0; i < KeyStates.Length; i++)
			{
				if (KeyStates[i].Item1)
				{
					if (KeyStates[i].Item2 >= 1)
					{
						KeyStates[i].Item2 = 2;
					}
					else
					{
						KeyStates[i].Item2 = 1;
					}
				}
				else
				{
					if (KeyStates[i].Item2 <= -1)
					{
						KeyStates[i].Item2 = -2;
					}
					else
					{
						KeyStates[i].Item2 = -1;
					}
				}
			}
		}
		/// <param name="nKey">
		///		調べる SlimDX.DirectInput.Key を int にキャストした値。（SharpDX.DirectInput.Key ではないので注意。）
		/// </param>
		public bool bキーが押された(int nKey)
		{
			return KeyStates[nKey].Item2 == 1;
		}
		/// <param name="nKey">
		///		調べる SlimDX.DirectInput.Key を int にキャストした値。（SharpDX.DirectInput.Key ではないので注意。）
		/// </param>
		public bool bキーが押されている(int nKey)
		{
			return KeyStates[nKey].Item2 >= 1;
		}
		/// <param name="nKey">
		///		調べる SlimDX.DirectInput.Key を int にキャストした値。（SharpDX.DirectInput.Key ではないので注意。）
		/// </param>
		public bool bキーが離された(int nKey)
		{
			return KeyStates[nKey].Item2 == -1;
		}
		/// <param name="nKey">
		///		調べる SlimDX.DirectInput.Key を int にキャストした値。（SharpDX.DirectInput.Key ではないので注意。）
		/// </param>
		public bool bキーが離されている(int nKey)
		{
			return KeyStates[nKey].Item2 <= -1;
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
		private (bool, int)[] KeyStates = new (bool, int)[144];
		private bool bDispose完了済み;
		//private CTimer timer;
		//private CTimer ct;


		private void KeyDown(IKeyboard keyboard, Key key, int keyCode)
		{
			if (key != Key.Unknown)
			{
				var keyNum = DeviceConstantConverter.DIKtoKey(key);
				KeyStates[(int)keyNum].Item1 = true;
			}
		}

		private void KeyUp(IKeyboard keyboard, Key key, int keyCode)
		{
			if (key != Key.Unknown)
			{
				var keyNum = DeviceConstantConverter.DIKtoKey(key);
				KeyStates[(int)keyNum].Item1 = false;
			}
		}

		private void KeyChar(IKeyboard keyboard, char ch)
		{

		}
		//-----------------
		#endregion
	}
}
