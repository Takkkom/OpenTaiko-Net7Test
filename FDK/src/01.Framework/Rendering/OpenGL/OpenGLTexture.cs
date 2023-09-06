using Silk.NET.Windowing;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace SampleFramework
{
    public class OpenGLTexture : ITexture
    {
        internal uint TextureHandle;

        public unsafe OpenGLTexture(void* data, int width, int height)
        {
            TextureHandle = OpenGLDevice.Gl.GenTexture();
            OpenGLDevice.Gl.BindTexture(TextureTarget.Texture2D, TextureHandle);
            
            OpenGLDevice.Gl.TexImage2D(GLEnum.Texture2D, 0, (int)InternalFormat.Rgba32f, (uint)width, (uint)height, 0, GLEnum.Bgra, GLEnum.UnsignedByte, data);
            
            OpenGLDevice.Gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.Nearest);
            OpenGLDevice.Gl.TexParameterI(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.Nearest);
        }

        public void Dispose()
        {
            OpenGLDevice.Gl.DeleteTexture(TextureHandle);
        }
    }
}