using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Silk.NET.Maths;
using FDK;

using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;
using Color = System.Drawing.Color;

namespace TJAPlayer3
{
	internal class CAct演奏AVI : CActivity
	{
		// コンストラクタ

		public CAct演奏AVI()
		{
			base.b活性化してない = true;
		}


		// メソッド

		public void Start( int nチャンネル番号, CVideoDecoder rVD )
		{
			if ( nチャンネル番号 == 0x54 && TJAPlayer3.ConfigIni.bAVI有効 )
			{
				this.rVD = rVD;
				if (this.rVD != null)
				{
					this.ratio1 = Math.Min((float)SampleFramework.GameWindowSize.Height / ((float)this.rVD.FrameSize.Height), (float)SampleFramework.GameWindowSize.Width / ((float)this.rVD.FrameSize.Height));
					
					this.rVD.Start();
				}
			}
		}
		public void Seek( int ms ) => this.rVD?.Seek(ms);

		public void Stop() => this.rVD?.Stop();
		
		public void tPauseControl() => this.rVD?.PauseControl();

		public unsafe int t進行描画( int x, int y )
		{
			if ( !base.b活性化してない )
			{
				if (this.rVD == null)
					return 0;
					
				this.rVD.GetNowFrame(ref this.tx描画用);

				this.tx描画用.vc拡大縮小倍率.X = this.ratio1;
				this.tx描画用.vc拡大縮小倍率.Y = this.ratio1;

				if (TJAPlayer3.ConfigIni.eClipDispType.HasFlag(EClipDispType.背景のみ))
				{
					this.tx描画用.t2D拡大率考慮描画(CTexture.RefPnt.Center, SampleFramework.GameWindowSize.Width / 2, SampleFramework.GameWindowSize.Height / 2);
				}
			}
			return 0;
		}

		public void t窓表示()
		{
			if( this.rVD == null || this.tx描画用 == null || !TJAPlayer3.ConfigIni.eClipDispType.HasFlag(EClipDispType.ウィンドウのみ))
				return;
				
			float[] fRatio = new float[] { 640.0f - 4.0f, 360.0f - 4.0f }; //中央下表示

			float ratio = Math.Min((float)(fRatio[0] / this.rVD.FrameSize.Width), (float)(fRatio[1] / this.rVD.FrameSize.Height));
			this.tx描画用.vc拡大縮小倍率.X = ratio;
			this.tx描画用.vc拡大縮小倍率.Y = ratio;

			this.tx描画用.t2D拡大率考慮描画(CTexture.RefPnt.Down, SampleFramework.GameWindowSize.Width / 2, SampleFramework.GameWindowSize.Height);
		}

		// CActivity 実装

		public override void On活性化()
		{
			base.On活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if ( !base.b活性化してない )
			{
				base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			if ( !base.b活性化してない )
			{
				if ( this.tx描画用 != null )
				{
					this.tx描画用.Dispose();
					this.tx描画用 = null;
				}
				base.OnManagedリソースの解放();
			}
		}
		public override int On進行描画()
		{
			throw new InvalidOperationException( "t進行描画(int,int)のほうを使用してください。" );
		}


		// その他

		#region [ private ]
		//-----------------
		private float ratio1;

		private CTexture tx描画用;

		public CVideoDecoder rVD;

		//-----------------
		#endregion
	}
}
