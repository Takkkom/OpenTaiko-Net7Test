using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace SampleFramework
{
    class OpenGLDevice : IGraphicsDevice
    {
        private GL Gl;

        public OpenGLDevice(IWindow window)
        {
            Gl = window.CreateOpenGL();
        }

        public void SetClearColor(float r, float g, float b, float a)
        {
            Gl.ClearColor(r, g, b, a);
        }

        public void SetViewPort(int x, int y, uint width, uint height)
        {
            Gl.Viewport(x, y, width, height);
        }

        public void SetFrameBuffer(uint width, uint height)
        {
        }

        public void ClearBuffer()
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        public void SwapBuffer()
        {
        }

        public void Dispose()
        {
        }
    }
}