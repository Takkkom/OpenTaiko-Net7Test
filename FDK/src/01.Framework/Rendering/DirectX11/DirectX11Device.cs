using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using SkiaSharp;

namespace SampleFramework
{
    unsafe class DirectX11Device : IGraphicsDevice
    {
        internal static D3D11 D3d11;

        internal static DXGI DxGi;

        internal static D3DCompiler D3dCompiler;

        internal static ComPtr<IDXGIFactory> Factory;

        internal static ComPtr<ID3D11Device> Device;

        internal static ComPtr<ID3D11DeviceContext> ImmediateContext;

        internal static ComPtr<IDXGISwapChain> SwapChain;

        internal static ComPtr<ID3D11RenderTargetView> RenderTargetView;

        internal static ComPtr<ID3D11DepthStencilView> DepthStencilView;

        internal static float* CurrnetClearColor;

        internal static IWindow Window_;







        float[] vertices =
        {
        //X    Y      Z
        0.5f,  0.5f, 0.0f,
        0.5f, -0.5f, 0.0f,
        -0.5f, -0.5f, 0.0f,
        -0.5f,  0.5f, 0.5f
    };

        uint[] indices =
        {
        0, 1, 3,
        1, 2, 3
    };

        uint vertexStride = 5U * sizeof(float);
        uint vertexOffset = 0U;


        private void CreateRenderTargetView()
        {
            var backBuffer = SwapChain.GetBuffer<ID3D11Texture2D>(0);
            SilkMarshal.ThrowHResult
            (
                Device.CreateRenderTargetView(backBuffer, null, ref RenderTargetView)
            );
            backBuffer.Dispose();
        }

        private void CreateDepthStencilView(uint width, uint height)
        {
            Texture2DDesc texture2DDesc = new Texture2DDesc();
            texture2DDesc.Width = width;
            texture2DDesc.Height = height;
            texture2DDesc.MipLevels = 1;
            texture2DDesc.ArraySize = 1;
            texture2DDesc.Format = Format.FormatD24UnormS8Uint;
            texture2DDesc.SampleDesc.Count = 1;
            texture2DDesc.SampleDesc.Quality = 0;
            texture2DDesc.Usage = Usage.Default;
            texture2DDesc.BindFlags = (uint)BindFlag.DepthStencil;
            texture2DDesc.CPUAccessFlags = 0;
            texture2DDesc.MiscFlags = 0;
            ComPtr<ID3D11Texture2D> depthTex = default;
            SilkMarshal.ThrowHResult
            (
                Device.CreateTexture2D(texture2DDesc, null, ref depthTex)
            );



            DepthStencilViewDesc depthStencilDesc = new DepthStencilViewDesc();
            depthStencilDesc.Format = texture2DDesc.Format;
            depthStencilDesc.ViewDimension = DsvDimension.Texture2D;
            depthStencilDesc.Texture2D = new Tex2DDsv(0);
            SilkMarshal.ThrowHResult
            (
                Device.CreateDepthStencilView(depthTex, depthStencilDesc, ref DepthStencilView)
            );
            depthTex.Dispose();
        }

        public DirectX11Device(IWindow window)
        {
            Window_ = window;
            D3d11 = D3D11.GetApi(window, false);
            DxGi = DXGI.GetApi(window, false);
            D3dCompiler = D3DCompiler.GetApi();


            SilkMarshal.ThrowHResult
            (
                DxGi.CreateDXGIFactory(out Factory)
            );


            D3DFeatureLevel[] featureLevels = new D3DFeatureLevel[]
            {
            D3DFeatureLevel.Level111,
            D3DFeatureLevel.Level110,
            };

            uint debugFlag = 0;

#if DEBUG
        debugFlag = (uint)CreateDeviceFlag.Debug;
#endif

            fixed (D3DFeatureLevel* levels = featureLevels)
            {
                SilkMarshal.ThrowHResult
                (
                    D3d11.CreateDevice(
                        default(ComPtr<IDXGIAdapter>),
                        D3DDriverType.Hardware,
                        0,
                        debugFlag,
                        levels,
                        (uint)featureLevels.Length,
                        D3D11.SdkVersion,
                        ref Device,
                        default,
                        ref ImmediateContext)
                );
            }



            SwapChainDesc swapChainDesc = new SwapChainDesc();
            swapChainDesc.BufferDesc.Width = (uint)window.FramebufferSize.X;
            swapChainDesc.BufferDesc.Height = (uint)window.FramebufferSize.Y;
            swapChainDesc.BufferDesc.Format = Format.FormatR8G8B8A8Unorm;
            swapChainDesc.BufferDesc.ScanlineOrdering = ModeScanlineOrder.Unspecified;
            swapChainDesc.BufferDesc.Scaling = ModeScaling.Unspecified;
            swapChainDesc.BufferDesc.RefreshRate.Numerator = 0;
            swapChainDesc.BufferDesc.RefreshRate.Denominator = 1;
            swapChainDesc.SampleDesc.Count = 1;
            swapChainDesc.SampleDesc.Quality = 0;
            swapChainDesc.BufferUsage = DXGI.UsageRenderTargetOutput;
            swapChainDesc.BufferCount = 2;
            swapChainDesc.OutputWindow = window.Native.DXHandle.Value;
            swapChainDesc.Windowed = true;
            swapChainDesc.SwapEffect = SwapEffect.FlipDiscard;

            SilkMarshal.ThrowHResult
            (
                Factory.CreateSwapChain(
                    Device,
                    &swapChainDesc,
                    ref SwapChain
                )
            );

            CreateRenderTargetView();
            CreateDepthStencilView((uint)window.FramebufferSize.X, (uint)window.FramebufferSize.X);













        }

        public void SetClearColor(float r, float g, float b, float a)
        {
            fixed (float* color = new float[] { r, g, b, a })
            {
                CurrnetClearColor = color;
            }
        }

        public void SetViewPort(int x, int y, uint width, uint height)
        {
            Viewport viewport = new Viewport(0, 0, width, height, 0.0f, 1.0f);
            ImmediateContext.RSSetViewports(1, in viewport);
        }

        public void SetFrameBuffer(uint width, uint height)
        {
            RenderTargetView.Dispose();
            DepthStencilView.Dispose();

            SilkMarshal.ThrowHResult
            (
                SwapChain.ResizeBuffers(0, width, height, Format.FormatR8G8B8A8Unorm, 0)
            );

            CreateRenderTargetView();
            CreateDepthStencilView(width, height);
        }

        public void ClearBuffer()
        {
            ImmediateContext.OMSetRenderTargets(1, ref RenderTargetView, DepthStencilView);
            ImmediateContext.ClearRenderTargetView(RenderTargetView, ref CurrnetClearColor[0]);
            ImmediateContext.ClearDepthStencilView(DepthStencilView, (uint)ClearFlag.Depth | (uint)ClearFlag.Depth, 1.0f, 0);
        }

        public void SwapBuffer()
        {

            SilkMarshal.ThrowHResult
            (
                SwapChain.Present(Window_.VSync ? 1u : 0u, 0)
            );
        }


        public IPolygon GenPolygon(float[] vertices, uint[] indices, float[] uvs)
        {
            return new DirectX11Polygon(vertices, indices, uvs);
        }

        public IShader GenShader()
        {
            return new DirectX11Shader(
                @"
                struct vs_in {
                    float3 position_local : POS;
                    float2 uvposition_local : UVPOS;
                };

                struct vs_out {
                    float4 position_clip : SV_POSITION;
                    float2 uvposition_clip : UVPOS;
                };
                
                cbuffer ConstantBuffer
                {
                    float4x4 mvp;
                    float4 color;
                    float4 textureRect;
                }

                vs_out vs_main(vs_in input) {
                    vs_out output = (vs_out)0;

                    float4 position = float4(input.position_local, 1.0);
                    output.position_clip = position;
                    output.uvposition_clip = input.uvposition_local;
                    return output;
                }

                float4 ps_main(vs_out input) : SV_TARGET {
                    float2 uv = input.uvposition_clip;
                    return float4( uv.x, uv.y, 0.0, 1.0 );
                }
                ");
        }


        public unsafe ITexture GenTexture(void* data, int width, int height, RgbaType rgbaType)
        {
            return null;
        }

        public unsafe void DrawPolygon(IPolygon polygon, IShader shader, ITexture texture, BlendType blendType)
        {
            DirectX11Polygon dx11polygon = (DirectX11Polygon)polygon;
            DirectX11Shader dx11shader = (DirectX11Shader)shader;
            ImmediateContext.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
            ImmediateContext.IASetInputLayout(dx11shader.InputLayout);
            ImmediateContext.IASetVertexBuffers(0, 1, dx11polygon.VertexBuffer, in vertexStride, in vertexOffset);
            ImmediateContext.IASetIndexBuffer(dx11polygon.IndexBuffer, Format.FormatR32Uint, 0);

            ImmediateContext.UpdateSubresource(dx11shader.ConstantBuffer, 0, null, dx11shader.ConstantBufferStruct_, 0, 0);
            ImmediateContext.VSSetConstantBuffers(0, 1, dx11shader.ConstantBuffer);

            // Bind our shaders.
            ImmediateContext.VSSetShader(dx11shader.VertexShader, default(ComPtr<ID3D11ClassInstance>), 0);
            ImmediateContext.PSSetShader(dx11shader.PixelShader, default(ComPtr<ID3D11ClassInstance>), 0);

            // Draw the quad.
            ImmediateContext.DrawIndexed(polygon.IndiceCount, 0, 0);
        }

        public unsafe SKBitmap GetScreenPixels()
        {  
            return null;
        }

        public void Dispose()
        {
            DepthStencilView.Dispose();
            RenderTargetView.Dispose();
            ImmediateContext.Dispose();
            Device.Dispose();
            Factory.Dispose();
        }
    }
}