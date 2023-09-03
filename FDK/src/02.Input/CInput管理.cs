using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.Windowing;
using Silk.NET.Input;

namespace FDK
{
	public class CInput管理 : IDisposable
	{
		// 定数

		public static int n通常音量 = 110;


		// プロパティ

		public List<IInputDevice> list入力デバイス
		{
			get;
			private set;
		}
		public IInputDevice Keyboard
		{
			get
			{
				if (this._Keyboard != null)
				{
					return this._Keyboard;
				}
				foreach (IInputDevice device in this.list入力デバイス)
				{
					if (device.e入力デバイス種別 == E入力デバイス種別.Keyboard)
					{
						this._Keyboard = device;
						return device;
					}
				}
				return null;
			}
		}
		public IInputDevice Mouse
		{
			get
			{
				if (this._Mouse != null)
				{
					return this._Mouse;
				}
				foreach (IInputDevice device in this.list入力デバイス)
				{
					if (device.e入力デバイス種別 == E入力デバイス種別.Mouse)
					{
						this._Mouse = device;
						return device;
					}
				}
				return null;
			}
		}


		// コンストラクタ
		public CInput管理(IWindow window, bool bUseMidiIn = true)
		{
			CInput管理初期化(window, bUseMidiIn);
		}

		public void CInput管理初期化(IWindow window, bool bUseMidiIn)
		{
			Context = window.CreateInput();

			this.list入力デバイス = new List<IInputDevice>(10);
			#region [ Enumerate keyboard/mouse: exception is masked if keyboard/mouse is not connected ]
			CInputKeyboard cinputkeyboard = null;
			CInputMouse cinputmouse = null;
			try
			{
				cinputkeyboard = new CInputKeyboard(Context.Keyboards);
				cinputmouse = new CInputMouse(Context.Mice[0]);
			}

			catch
			{
			}
			if (cinputkeyboard != null)
			{
				this.list入力デバイス.Add(cinputkeyboard);
			}
			if (cinputmouse != null)
			{
				this.list入力デバイス.Add(cinputmouse);
			}
			#endregion
			#region [ Enumerate joypad ]
			foreach (var joysticks in Context.Joysticks)
			{
				this.list入力デバイス.Add(new CInputJoystick(joysticks));
			}
			#endregion
		}


		// メソッド

		public IInputDevice Joystick(int ID)
		{
			foreach (IInputDevice device in this.list入力デバイス)
			{
				if ((device.e入力デバイス種別 == E入力デバイス種別.Joystick) && (device.ID == ID))
				{
					return device;
				}
			}
			return null;
		}
		public IInputDevice Joystick(string GUID)
		{
			foreach (IInputDevice device in this.list入力デバイス)
			{
				if ((device.e入力デバイス種別 == E入力デバイス種別.Joystick) && device.GUID.Equals(GUID))
				{
					return device;
				}
			}
			return null;
		}
		public IInputDevice MidiIn(int ID)
		{
			foreach (IInputDevice device in this.list入力デバイス)
			{
				if ((device.e入力デバイス種別 == E入力デバイス種別.MidiIn) && (device.ID == ID))
				{
					return device;
				}
			}
			return null;
		}
		public void tポーリング(bool bバッファ入力を使用する)
		{
			lock (this.objMidiIn排他用)
			{
				//				foreach( IInputDevice device in this.list入力デバイス )
				for (int i = this.list入力デバイス.Count - 1; i >= 0; i--)    // #24016 2011.1.6 yyagi: change not to use "foreach" to avoid InvalidOperation exception by Remove().
				{
					IInputDevice device = this.list入力デバイス[i];
					try
					{
						device.tポーリング(bバッファ入力を使用する);
					}
					catch (Exception e)                                      // #24016 2011.1.6 yyagi: catch exception for unplugging USB joystick, and remove the device object from the polling items.
					{
						this.list入力デバイス.Remove(device);
						device.Dispose();
						Trace.TraceError("tポーリング時に対象deviceが抜かれており例外発生。同deviceをポーリング対象からRemoveしました。");
					}
				}
			}
		}

		#region [ IDisposable＋α ]
		//-----------------
		public void Dispose()
		{
			this.Dispose(true);
		}
		public void Dispose(bool disposeManagedObjects)
		{
			if (!this.bDisposed済み)
			{
				if (disposeManagedObjects)
				{
					foreach (IInputDevice device in this.list入力デバイス)
					{
						CInputMIDI tmidi = device as CInputMIDI;
						if (tmidi != null)
						{
							Trace.TraceInformation("MIDI In: [{0}] を停止しました。", new object[] { tmidi.ID });
						}
					}
					foreach (IInputDevice device2 in this.list入力デバイス)
					{
						device2.Dispose();
					}
					lock (this.objMidiIn排他用)
					{
						this.list入力デバイス.Clear();
					}

					Context.Dispose();
				}
				this.bDisposed済み = true;
			}
		}
		~CInput管理()
		{
			this.Dispose(false);
			GC.KeepAlive(this);
		}
		//-----------------
		#endregion


		// その他

		#region [ private ]
		//-----------------
		private IInputContext Context;
		private IInputDevice _Keyboard;
		private IInputDevice _Mouse;
		private bool bDisposed済み;
		private List<uint> listHMIDIIN = new List<uint>(8);
		private object objMidiIn排他用 = new object();
		//private CTimer timer;

		//-----------------
		#endregion
	}
}
