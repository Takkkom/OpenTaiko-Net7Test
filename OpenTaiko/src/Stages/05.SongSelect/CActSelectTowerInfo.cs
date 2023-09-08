using FDK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Minimalist menu class to use for custom menus
namespace TJAPlayer3
{
    class CActSelectTowerInfo : CStage
    {
        public CActSelectTowerInfo()
        {
            base.b活性化してない = true;
        }

        public override void On活性化()
        {
            // On activation

            if (base.b活性化してる)
                return;



            base.On活性化();
        }

        public override void On非活性化()
        {
            // On de-activation

            base.On非活性化();
        }

        public override void OnManagedリソースの作成()
        {
            if (base.b活性化してない)
                return;

            // Ressource allocation
            SongSelect_Floor_Number = TJAPlayer3.tテクスチャの生成(CSkin.Path($@"{TextureLoader.BASE}{TextureLoader.SONGSELECT}Floor_Number.png"));

            base.OnManagedリソースの作成();
        }

        public override void OnManagedリソースの解放()
        {
            if (base.b活性化してない)
                return;

            // Ressource freeing
            TJAPlayer3.t安全にDisposeする(ref SongSelect_Floor_Number);

            base.OnManagedリソースの解放();
        }

        public override int On進行描画()
        {
            tFloorNumberDraw(TJAPlayer3.Skin.SongSelect_FloorNum_X, TJAPlayer3.Skin.SongSelect_FloorNum_Y, TJAPlayer3.stage選曲.r現在選択中の曲.nTotalFloor);

            return 0;
        }

        #region [Private]

        private CTexture SongSelect_Floor_Number;

        private void tFloorNumberDraw(float originx, float originy, int num)
        {
            int[] nums = CConversion.SeparateDigits(num);

            for (int j = 0; j < nums.Length; j++)
            {
                if (TJAPlayer3.Skin.SongSelect_FloorNum_Show && SongSelect_Floor_Number != null)
                {
                    float offset = j;
                    float x = originx - (TJAPlayer3.Skin.SongSelect_FloorNum_Interval[0] * offset);
                    float y = originy - (TJAPlayer3.Skin.SongSelect_FloorNum_Interval[1] * offset);

                    float width = SongSelect_Floor_Number.sz画像サイズ.Width / 10.0f;
                    float height = SongSelect_Floor_Number.sz画像サイズ.Height;

                    SongSelect_Floor_Number.t2D描画(x, y, new RectangleF(width * nums[j], 0, width, height));
                }
            }
        }

        #endregion
    }
}
