using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace FDK
{
	internal interface ISoundDevice : IDisposable
	{
		ESoundDeviceType SoundDeviceType { get; }
		int nMasterVolume { get; set; }
		long n実出力遅延ms { get; }
		long BufferSize { get; }
		long n経過時間ms { get; }
		long n経過時間を更新したシステム時刻ms { get; }
		CTimer tmシステムタイマ { get; }

		CSound tCreateSound( string strファイル名, ESoundGroup soundGroup );
		void tCreateSound( string strファイル名, CSound sound );
	}
}
