using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace SampleFramework
{
    public class OpenGLTexture : ITexture
    {
        internal uint TextureHandle;

        public OpenGLTexture(SKBitmap bitmap)
        {
            TextureHandle = OpenGLDevice.Gl.GenTexture();
            OpenGLDevice.Gl.BindTexture(TextureTarget.Texture2D, TextureHandle);
            
            unsafe
            {
                fixed(void* rgbas = bitmap.Pixels)
                {
                    OpenGLDevice.Gl.TexImage2D(GLEnum.Texture2D, 0, (int)InternalFormat.Rgba32f, (uint)bitmap.Width, (uint)bitmap.Height, 0, GLEnum.Bgra, GLEnum.UnsignedByte, rgbas);
                }
            }
            
            OpenGLDevice.Gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Nearest);
            OpenGLDevice.Gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest);
        }

        public void Dispose()
        {
            OpenGLDevice.Gl.DeleteTexture(TextureHandle);
        }
    }
}