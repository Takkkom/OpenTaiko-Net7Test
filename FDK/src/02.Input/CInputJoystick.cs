using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Silk.NET.Input;

namespace FDK
{
	public class CInputJoystick : IInputDevice, IDisposable
	{
		// コンストラクタ

		public CInputJoystick(IJoystick joystick)
		{
			this.e入力デバイス種別 = E入力デバイス種別.Joystick;
			this.GUID = joystick.Index.ToString();
			this.ID = joystick.Index;

			this.list入力イベント = new List<STInputEvent>(32);

			joystick.ButtonDown += Joystick_ButtonDown;
			joystick.ButtonUp += Joystick_ButtonUp;
			joystick.AxisMoved += Joystick_AxisMoved;
			joystick.HatMoved += Joystick_HatMoved;
		}
		
		
		// メソッド
		
		public void SetID( int nID )
		{
			this.ID = nID;
		}

		#region [ IInputDevice 実装 ]
		//-----------------
		public E入力デバイス種別 e入力デバイス種別
		{ 
			get;
			private set;
		}
		public string GUID
		{
			get;
			private set;
		}
		public int ID
		{ 
			get; 
			private set;
		}
		public List<STInputEvent> list入力イベント 
		{
			get;
			private set;
		}
		public string strDeviceName
		{
			get;
			set;
		}

		public void tポーリング(bool bバッファ入力を使用する)
		{
			list入力イベント.Clear();
			
			for (int i = 0; i < ButtonStates.Length; i++)
			{
				if (ButtonStates[i].Item1)
				{
					if (ButtonStates[i].Item2 >= 1)
					{
						ButtonStates[i].Item2 = 2;
					}
					else
					{
						ButtonStates[i].Item2 = 1;

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
					if (ButtonStates[i].Item2 <= -1)
					{
						ButtonStates[i].Item2 = -2;
					}
					else
					{
						ButtonStates[i].Item2 = -1;

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
			return ButtonStates[nButton].Item2 == 1;
		}
		public bool bキーが押されている(int nButton)
		{
			return ButtonStates[nButton].Item2 >= 1;
		}
		public bool bキーが離された(int nButton)
		{
			return ButtonStates[nButton].Item2 == -1;
		}
		public bool bキーが離されている(int nButton)
		{
			return ButtonStates[nButton].Item2 <= -1;
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
		private (bool, int)[] ButtonStates = new (bool, int)[15];
		private bool bDispose完了済み;

		private void Joystick_ButtonDown(IJoystick joystick, Button button)
		{
			if (button.Name != ButtonName.Unknown)
			{
				ButtonStates[(int)button.Name].Item1 = true;
			}
		}

		private void Joystick_ButtonUp(IJoystick joystick, Button button)
		{
			if (button.Name != ButtonName.Unknown)
			{
				ButtonStates[(int)button.Name].Item1 = false;
			}
		}

		private void Joystick_AxisMoved(IJoystick joystick, Axis axis)
		{

		}

		private void Joystick_HatMoved(IJoystick joystick, Hat hat)
		{

		}
		//-----------------
		#endregion
	}
}
