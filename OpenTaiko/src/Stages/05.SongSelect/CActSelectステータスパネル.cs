using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using FDK;

namespace TJAPlayer3
{
	internal class CActSelectステータスパネル : CActivity
	{
		// メソッド

		public CActSelectステータスパネル()
		{
			base.IsDeActivated = true;
		}
		public void t選択曲が変更された()
		{

		}


		// CActivity 実装

		public override void Activate()
		{

			base.Activate();
		}
		public override void DeActivate()
		{

			base.DeActivate();
		}
		public override void CreateManagedResource()
		{
			if( !base.IsDeActivated )
			{

				base.CreateManagedResource();
			}
		}
		public override void ReleaseManagedResource()
		{
			if( !base.IsDeActivated )
			{

				base.ReleaseManagedResource();
			}
		}
		public override int Draw()
		{
			if( !base.IsDeActivated )
			{

			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		//-----------------
		#endregion
	}
}
