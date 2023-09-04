using Silk.NET.Maths;

namespace SampleFramework
{
    public interface IShader : IDisposable
    {
        void SetMVP(Matrix4X4<float> mvp);
        void SetOpacity(float opacity);
        void SetTextureRect(Vector4D<float> rect);
    }
}