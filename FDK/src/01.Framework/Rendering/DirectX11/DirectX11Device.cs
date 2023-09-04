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
        private D3D11 D3d11;

        private DXGI DxGi;

        private D3DCompiler D3dCompiler;

        private ComPtr<IDXGIFactory> Factory;

        private ComPtr<ID3D11Device> Device;

        private ComPtr<ID3D11DeviceContext> ImmediateContext;

        private ComPtr<IDXGISwapChain> SwapChain;

        private ComPtr<ID3D11RenderTargetView> RenderTargetView;

        private ComPtr<ID3D11DepthStencilView> DepthStencilView;

        private float* CurrnetClearColor;

        private IWindow Window_;





        ComPtr<ID3D11Buffer> vertexBuffer = default;
        ComPtr<ID3D11Buffer> indexBuffer = default;
        ComPtr<ID3D11VertexShader> vertexShader = default;
        ComPtr<ID3D11PixelShader> pixelShader = default;
        ComPtr<ID3D11InputLayout> inputLayout = default;



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

        uint vertexStride = 3U * sizeof(float);
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




















            // Create our vertex buffer.
            var bufferDesc = new BufferDesc
            {
                ByteWidth = (uint)(vertices.Length * sizeof(float)),
                Usage = Usage.Default,
                BindFlags = (uint)BindFlag.VertexBuffer
            };

            fixed (float* vertexData = vertices)
            {
                var subresourceData = new SubresourceData
                {
                    PSysMem = vertexData
                };

                SilkMarshal.ThrowHResult(Device.CreateBuffer(in bufferDesc, in subresourceData, ref vertexBuffer));
            }

            // Create our index buffer.
            bufferDesc = new BufferDesc
            {
                ByteWidth = (uint)(indices.Length * sizeof(uint)),
                Usage = Usage.Default,
                BindFlags = (uint)BindFlag.IndexBuffer
            };

            fixed (uint* indexData = indices)
            {
                var subresourceData = new SubresourceData
                {
                    PSysMem = indexData
                };

                SilkMarshal.ThrowHResult(Device.CreateBuffer(in bufferDesc, in subresourceData, ref indexBuffer));
            }



            DirectXShaderSource directXShaderSource = new DirectXShaderSource(D3dCompiler);

            // Create vertex shader.
            SilkMarshal.ThrowHResult
            (
                Device.CreateVertexShader
                (
                    directXShaderSource.VertexCode.GetBufferPointer(),
                    directXShaderSource.VertexCode.GetBufferSize(),
                    default(ComPtr<ID3D11ClassLinkage>),
                    ref vertexShader
                )
            );

            // Create pixel shader.
            SilkMarshal.ThrowHResult
            (
                Device.CreatePixelShader
                (
                    directXShaderSource.PixelCode.GetBufferPointer(),
                    directXShaderSource.PixelCode.GetBufferSize(),
                    default(ComPtr<ID3D11ClassLinkage>),
                    ref pixelShader
                )
            );

            // Describe the layout of the input data for the shader.
            fixed (byte* name = SilkMarshal.StringToMemory("POS"))
            {
                var inputElement = new InputElementDesc
                {
                    SemanticName = name,
                    SemanticIndex = 0,
                    Format = Format.FormatR32G32B32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 0,
                    InputSlotClass = InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                };

                SilkMarshal.ThrowHResult
                (
                    Device.CreateInputLayout
                    (
                        in inputElement,
                        1,
                        directXShaderSource.VertexCode.GetBufferPointer(),
                        directXShaderSource.VertexCode.GetBufferSize(),
                        ref inputLayout
                    )
                );
            }

            directXShaderSource.Dispose();
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
            ImmediateContext.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
            ImmediateContext.IASetInputLayout(inputLayout);
            ImmediateContext.IASetVertexBuffers(0, 1, vertexBuffer, in vertexStride, in vertexOffset);
            ImmediateContext.IASetIndexBuffer(indexBuffer, Format.FormatR32Uint, 0);

            // Bind our shaders.
            ImmediateContext.VSSetShader(vertexShader, default(ComPtr<ID3D11ClassInstance>), 0);
            ImmediateContext.PSSetShader(pixelShader, default(ComPtr<ID3D11ClassInstance>), 0);

            // Draw the quad.
            ImmediateContext.DrawIndexed(6, 0, 0);



            SilkMarshal.ThrowHResult
            (
                SwapChain.Present(Window_.VSync ? 1u : 0u, 0)
            );
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

        public void DrawPolygon(IPolygon polygon, IShader shader, ITexture texture, BlendType blendType)
        {
        }

        public void Dispose()
        {
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            vertexShader.Dispose();
            pixelShader.Dispose();
            inputLayout.Dispose();



            DepthStencilView.Dispose();
            RenderTargetView.Dispose();
            ImmediateContext.Dispose();
            Device.Dispose();
            Factory.Dispose();
        }
    }
}