using System;
using Silk.NET.Windowing;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using SkiaSharp;

namespace SampleFramework
{
    public class DirectX12Shader : IShader
    {
        public unsafe DirectX12Shader(string shaderSource)
        {
        }

        public void SetMVP(Matrix4X4<float> mvp)
        {
        }

        public void SetColor(Vector4D<float> color)
        {
        }

        public void SetTextureRect(Vector4D<float> rect)
        {
        }

        public void Dispose()
        {
        }
    }
}