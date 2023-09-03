using System;
using Silk.NET.Windowing;
using Silk.NET.Maths;

namespace SampleFramework
{
    interface IGraphicsDevice : IDisposable
    {
        void SetClearColor(float r, float g, float b, float a);

        void SetViewPort(int x, int y, uint width, uint height);

        void SetFrameBuffer(uint width, uint height);

        void ClearBuffer();

        void SwapBuffer();
    }
}