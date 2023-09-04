using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using SkiaSharp;

namespace SampleFramework
{
    class VulkanDevice : IGraphicsDevice
    {
        private Vk VK;

        public VulkanDevice(IWindow window)
        {
            VK = Vk.GetApi();
        }

        public void SetClearColor(float r, float g, float b, float a)
        {
        }

        public void SetViewPort(int x, int y, uint width, uint height)
        {
        }

        public void SetFrameBuffer(uint width, uint height)
        {
        }

        public void ClearBuffer()
        {
        }

        public void SwapBuffer()
        {
        }


        public IPolygon GenPolygon(float[] vertices, uint[] indices, float[] uvs)
        {
            return null;
        }

        public IShader GenShader()
        {
            return null;
        }

        public ITexture GenTexture(SKBitmap bitmap)
        {
            return null;
        }

        public void DrawPolygon(IPolygon polygon, IShader shader, ITexture texture)
        {
        }

        public void Dispose()
        {
        }
    }
}