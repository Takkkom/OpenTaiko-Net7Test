using Silk.NET.Windowing;
using Silk.NET.Direct3D11;
using SkiaSharp;

namespace SampleFramework
{
    public class DirectX11Texture : ITexture
    {

        public DirectX11Texture(SKBitmap bitmap)
        {
            unsafe
            {
                fixed(void* rgbas = bitmap.Pixels)
                {
                }
            }
            
        }

        public void Dispose()
        {
        }
    }
}