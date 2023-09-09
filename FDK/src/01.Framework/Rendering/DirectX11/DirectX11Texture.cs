using Silk.NET.Windowing;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using SkiaSharp;
using Silk.NET.Core.Native;

namespace SampleFramework
{
    public class DirectX11Texture : ITexture
    {
        internal ComPtr<ID3D11Texture2D> Texture;
        internal ComPtr<ID3D11ShaderResourceView> TextureView;
        internal ComPtr<ID3D11SamplerState> SamplerState;

        private Format RgbaTypeToFormat(RgbaType rgbaType)
        {
            switch(rgbaType)
            {
                case RgbaType.Rgba:
                return Format.FormatR32G32B32A32Uint;
                case RgbaType.Bgra:
                return Format.FormatB8G8R8A8Unorm;
                default:
                return Format.FormatR32G32B32A32Uint;
            }
        }

        public unsafe DirectX11Texture(void* data, int width, int height, RgbaType rgbaType)
        {
            Texture2DDesc texture2DDesc = new()
            {
                Width = (uint)width,
                Height = (uint)height,
                CPUAccessFlags = 0,
                BindFlags = (uint)BindFlag.ShaderResource,
                MipLevels = 1,
                Format = RgbaTypeToFormat(rgbaType),
                MiscFlags = 0,
                ArraySize = 1,
                SampleDesc = new()
                {
                    Count = 1,
                    Quality = 0,
                }
            };

            SubresourceData subresourceData = new()
            {
                PSysMem = data,
                SysMemPitch = (uint)(width * 4),
                SysMemSlicePitch = 0
            };
            
            SilkMarshal.ThrowHResult(
                DirectX11Device.Device.CreateTexture2D(texture2DDesc, subresourceData, ref Texture)
            );
            SilkMarshal.ThrowHResult(
                DirectX11Device.Device.CreateShaderResourceView(Texture, null, ref TextureView)
            );

            SamplerDesc samplerDesc = new()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp
            };
            
            SilkMarshal.ThrowHResult(
                DirectX11Device.Device.CreateSamplerState(samplerDesc, ref SamplerState)
            );
        }

        public void Dispose()
        {
            Texture.Dispose();
            TextureView.Dispose();
            SamplerState.Dispose();
        }
    }
}