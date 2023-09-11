using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace SampleFramework
{
    public class DirectX12Polygon : IPolygon
    {
        public uint IndiceCount { get; set; }

        public uint VertexStride;


        public unsafe DirectX12Polygon(float[] vertices, uint[] indices, float[] uvs)
        {
        }

        public void Dispose()
        {
        }
    }
}