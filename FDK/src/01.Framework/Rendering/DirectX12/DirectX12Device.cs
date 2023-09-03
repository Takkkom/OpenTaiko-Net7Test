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
    unsafe class DirectX12Device : IGraphicsDevice
    {
        private D3D12 D3d12;

        private DXGI DxGi;

        private D3DCompiler D3dCompiler;

        private ComPtr<ID3D12Debug> DebugController;

        private ComPtr<IDXGIFactory4> Factory;

        private ComPtr<IDXGIAdapter1> HardwareAdapters;

        private ComPtr<ID3D12Device> Device;

        private ComPtr<ID3D12CommandQueue> CommandQueue;

        private ComPtr<IDXGISwapChain3> SwapChain;

        private ComPtr<ID3D12DescriptorHeap> RtvHeap;

        private ComPtr<ID3D12Resource>[] RenderTargets = new ComPtr<ID3D12Resource>[2];

        private ComPtr<ID3D12CommandAllocator> CommandAllocator;

        private ComPtr<ID3D12RootSignature> RootSignature;

        private ComPtr<ID3D12PipelineState> PipelineState;

        private ComPtr<ID3D12GraphicsCommandList> CommandList;

        private ComPtr<ID3D12Fence> Fence;

        private uint FenceValue;

        private IntPtr FenceEvent;

        private uint RtvDescriptorSize;

        private float* CurrnetClearColor;

        private IWindow Window_;

        private uint FrameBufferIndex;

        private uint FrameCount = 2;



        private ComPtr<ID3D12Resource> VertexBuffer;

        private VertexBufferView VertexBufferView_;

        private ComPtr<ID3D12Resource> IndexBuffer;

        private IndexBufferView IndexBufferView_;



        private bool SupportsRequiredDirect3DVersion(IDXGIAdapter1* adapter1)
        {
            var iid = ID3D12Device.Guid;
            return HResult.IndicatesSuccess(D3d12.CreateDevice((IUnknown*)adapter1, D3DFeatureLevel.Level110, &iid, null));
        }

        private ID3D12Device* GetDevice()
        {
            ComPtr<IDXGIAdapter1> adapter = default;
            ID3D12Device* device = default;

            for (uint i = 0; Factory.EnumAdapters(i, ref adapter) != 0x887A0002; i++)
            {
                AdapterDesc1 desc = default;
                adapter.GetDesc1(ref desc);

                if ((desc.Flags & (uint)AdapterFlag.Software) != 0)
                {
                    continue;
                }

                if (SupportsRequiredDirect3DVersion(adapter)) break;
            }

            var device_iid = ID3D12Device.Guid;
            IDXGIAdapter1* hardwareAdapters = adapter.Detach();
            HardwareAdapters = hardwareAdapters;
            SilkMarshal.ThrowHResult
            (
                D3d12.CreateDevice((IUnknown*)hardwareAdapters, D3DFeatureLevel.Level110, &device_iid, (void**)&device)
            );

            return device;
        }

        private void CreateCommandQueue()
        {
            CommandQueueDesc commandQueueDesc = new CommandQueueDesc();
            commandQueueDesc.Flags = CommandQueueFlags.None;
            commandQueueDesc.Type = CommandListType.Direct;
            void* commandQueue = null;
            var iid = ID3D12CommandQueue.Guid;
            SilkMarshal.ThrowHResult
            (
                Device.CreateCommandQueue(&commandQueueDesc, ref iid, &commandQueue)
            );
            CommandQueue = (ID3D12CommandQueue*)commandQueue;
        }

        private void CreateSwapChain()
        {
            SwapChainDesc swapChainDesc = new SwapChainDesc();
            swapChainDesc.BufferDesc.Width = (uint)Window_.FramebufferSize.X;
            swapChainDesc.BufferDesc.Height = (uint)Window_.FramebufferSize.Y;
            swapChainDesc.BufferDesc.Format = Format.FormatR8G8B8A8Unorm;
            swapChainDesc.BufferDesc.ScanlineOrdering = ModeScanlineOrder.Unspecified;
            swapChainDesc.BufferDesc.Scaling = ModeScaling.Unspecified;
            swapChainDesc.BufferDesc.RefreshRate.Numerator = 0;
            swapChainDesc.BufferDesc.RefreshRate.Denominator = 1;
            swapChainDesc.SampleDesc.Count = 1;
            swapChainDesc.SampleDesc.Quality = 0;
            swapChainDesc.BufferUsage = DXGI.UsageRenderTargetOutput;
            swapChainDesc.BufferCount = FrameCount;
            swapChainDesc.OutputWindow = Window_.Native.DXHandle.Value;
            swapChainDesc.Windowed = true;
            swapChainDesc.SwapEffect = SwapEffect.FlipDiscard;

            void* device = CommandQueue;
            void** swapChain = (void**)SwapChain.GetAddressOf();
            SilkMarshal.ThrowHResult
            (
                Factory.CreateSwapChain(
                    (IUnknown*)device,
                    &swapChainDesc,
                    (IDXGISwapChain**)swapChain
                )
            );

            FrameBufferIndex = SwapChain.GetCurrentBackBufferIndex();
        }

        private void CreateRTVHeap()
        {
            DescriptorHeapDesc rtvHeapDesc = new DescriptorHeapDesc();
            rtvHeapDesc.NumDescriptors = FrameCount;
            rtvHeapDesc.Type = DescriptorHeapType.Rtv;
            rtvHeapDesc.Flags = DescriptorHeapFlags.None;

            void* rtvHeap = null;
            var iid = ID3D12DescriptorHeap.Guid;
            Device.CreateDescriptorHeap(&rtvHeapDesc, ref iid, &rtvHeap);
            RtvHeap = (ID3D12DescriptorHeap*)rtvHeap;

            RtvDescriptorSize = Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv);
        }

        private void CreateRenderTargetViews()
        {
            CpuDescriptorHandle rtvHandle = new CpuDescriptorHandle();
            rtvHandle.Ptr = RtvHeap.GetCPUDescriptorHandleForHeapStart().Ptr;
            var iid = ID3D12Resource.Guid;

            for (uint i = 0; i < FrameCount; i++)
            {
                void* renderTarget;
                SilkMarshal.ThrowHResult
                (
                    SwapChain.GetBuffer(i, ref iid, &renderTarget)
                );
                RenderTargets[i] = (ID3D12Resource*)renderTarget;
                Device.CreateRenderTargetView(RenderTargets[i], null, rtvHandle);
                rtvHandle.Ptr = (uint)rtvHandle.Ptr + RtvDescriptorSize;
            }
        }

        private void CreateCommandAllocator()
        {
            var iid = ID3D12CommandAllocator.Guid;
            void* commandAllocator;
            SilkMarshal.ThrowHResult
            (
                Device.CreateCommandAllocator(CommandListType.Direct, &iid, &commandAllocator)
            );

            CommandAllocator = (ID3D12CommandAllocator*)commandAllocator;
        }

        private void CreateRootSignature()
        {
            RootSignatureDesc rootSignatureDesc = new RootSignatureDesc();
            rootSignatureDesc.NumParameters = 0;
            rootSignatureDesc.PParameters = null;
            rootSignatureDesc.NumStaticSamplers = 0;
            rootSignatureDesc.PStaticSamplers = null;
            rootSignatureDesc.Flags = RootSignatureFlags.AllowInputAssemblerInputLayout;

            ComPtr<ID3D10Blob> signature = null;
            ComPtr<ID3D10Blob> error = null;

            SilkMarshal.ThrowHResult
            (
                D3d12.SerializeRootSignature(rootSignatureDesc, D3DRootSignatureVersion.Version1, signature.GetAddressOf(), error.GetAddressOf())
            );

            var iid = ID3D12RootSignature.Guid;
            void* rootSignature;

            SilkMarshal.ThrowHResult
            (
                Device.CreateRootSignature(0, signature.Get().GetBufferPointer(), signature.Get().GetBufferSize(), &iid, &rootSignature)
            );

            RootSignature = (ID3D12RootSignature*)rootSignature;

            signature.Dispose();
            error.Dispose();
        }

        private void CreateFence()
        {
            var iid = ID3D12Fence.Guid;
            void* fence;
            Device.CreateFence(0, FenceFlags.None, &iid, &fence);
            Fence = (ID3D12Fence*)fence;
        }

        private void CreateFenceEvent()
        {
            var fenceEvent = SilkMarshal.CreateWindowsEvent(null, false, false, null);

            if (fenceEvent == IntPtr.Zero)
            {
                var hr = Marshal.GetHRForLastWin32Error();
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        private void CreatePipelineState()
        {
            GraphicsPipelineStateDesc graphicsPipelineStateDesc = new GraphicsPipelineStateDesc();

            InputElementDesc[] inputElementDescs = new InputElementDesc[1];

            fixed (byte* name = SilkMarshal.StringToMemory("POS"))
            {
                inputElementDescs[0] = new InputElementDesc()
                {
                    SemanticName = name,
                    SemanticIndex = 0,
                    Format = Format.FormatR32G32B32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 0,
                    InputSlotClass = InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                };
            }

            DirectXShaderSource directXShaderSource = new DirectXShaderSource(D3dCompiler);

            fixed (InputElementDesc* elements = inputElementDescs)
            {
                graphicsPipelineStateDesc.InputLayout = new InputLayoutDesc()
                {
                    PInputElementDescs = elements,
                    NumElements = (uint)inputElementDescs.Length,
                };
            }

            graphicsPipelineStateDesc.PRootSignature = RootSignature;
            graphicsPipelineStateDesc.VS = new ShaderBytecode(directXShaderSource.VertexCode.GetBufferPointer(), directXShaderSource.VertexCode.GetBufferSize());
            graphicsPipelineStateDesc.PS = new ShaderBytecode(directXShaderSource.PixelCode.GetBufferPointer(), directXShaderSource.PixelCode.GetBufferSize());

            RasterizerDesc rasterizerDesc = new RasterizerDesc()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
                FrontCounterClockwise = 0,
                DepthBias = D3D12.DefaultDepthBias,
                DepthBiasClamp = 0,
                SlopeScaledDepthBias = 0,
                DepthClipEnable = 1,
                MultisampleEnable = 0,
                AntialiasedLineEnable = 0,
                ForcedSampleCount = 0,
                ConservativeRaster = ConservativeRasterizationMode.Off
            };
            graphicsPipelineStateDesc.RasterizerState = rasterizerDesc;


            var defaultRenderTargetBlend = new RenderTargetBlendDesc()
            {
                BlendEnable = 0,
                LogicOpEnable = 0,
                SrcBlend = Blend.One,
                DestBlend = Blend.Zero,
                BlendOp = BlendOp.Add,
                SrcBlendAlpha = Blend.One,
                DestBlendAlpha = Blend.Zero,
                BlendOpAlpha = BlendOp.Add,
                LogicOp = LogicOp.Noop,
                RenderTargetWriteMask = (byte)ColorWriteEnable.All
            };
            BlendDesc blendDesc = new BlendDesc()
            {
                AlphaToCoverageEnable = 0,
                IndependentBlendEnable = 0,
                RenderTarget = new BlendDesc.RenderTargetBuffer()
                {
                    [0] = defaultRenderTargetBlend,
                    [1] = defaultRenderTargetBlend,
                    [2] = defaultRenderTargetBlend,
                    [3] = defaultRenderTargetBlend,
                    [4] = defaultRenderTargetBlend,
                    [5] = defaultRenderTargetBlend,
                    [6] = defaultRenderTargetBlend,
                    [7] = defaultRenderTargetBlend
                }
            };
            graphicsPipelineStateDesc.BlendState = blendDesc;


            graphicsPipelineStateDesc.DepthStencilState.DepthEnable = 0;
            graphicsPipelineStateDesc.DepthStencilState.StencilEnable = 0;
            graphicsPipelineStateDesc.SampleMask = uint.MaxValue;
            graphicsPipelineStateDesc.PrimitiveTopologyType = PrimitiveTopologyType.Triangle;
            graphicsPipelineStateDesc.NumRenderTargets = 1;
            graphicsPipelineStateDesc.RTVFormats[0] = Format.FormatR8G8B8A8Unorm;
            graphicsPipelineStateDesc.SampleDesc.Count = 1;


            void* pipelineState;
            var iid = ID3D12PipelineState.Guid;
            SilkMarshal.ThrowHResult
            (
                Device.CreateGraphicsPipelineState(graphicsPipelineStateDesc, &iid, &pipelineState)
            );

            directXShaderSource.Dispose();

            PipelineState = (ID3D12PipelineState*)pipelineState;
        }

        private void CreateCommandList()
        {
            void* commandList;
            var iid = ID3D12GraphicsCommandList.Guid;

            SilkMarshal.ThrowHResult
            (
                Device.CreateCommandList(0, CommandListType.Direct, CommandAllocator, PipelineState, &iid, &commandList)
            );

            CommandList = (ID3D12GraphicsCommandList*)commandList;

            CommandList.Close();
        }

        private void WaitForPreviousFrame()
        {
            uint fence = FenceValue;
            CommandQueue.Signal(Fence, FenceValue);
            FenceValue++;

            if (Fence.GetCompletedValue() < fence)
            {
                Fence.SetEventOnCompletion(fence, FenceEvent.ToPointer());
                _ = SilkMarshal.WaitWindowsObjects(FenceEvent);
            }
        }

        float[] vertices =
        {
        //X    Y      Z
        0.0f, 0.5f, 0.0f,
        0.5f, -0.5f, 0.0f,
        -0.5f, -0.5f, 0.0f,
        //-0.5f,  0.5f, 0.5f
    };

        uint[] indices =
        {
        0, 1, 3,
        1, 2, 3
    };

        private void CreateAssets()
        {
            uint vertexBufferSize = (uint)(sizeof(float) * vertices.Length);

            HeapProperties heapProperties = new HeapProperties()
            {
                Type = HeapType.Upload,
                CPUPageProperty = CpuPageProperty.Unknown,
                MemoryPoolPreference = MemoryPool.Unknown,
                CreationNodeMask = 1,
                VisibleNodeMask = 1
            };

            ResourceDesc resourceDesc = new ResourceDesc()
            {
                Dimension = ResourceDimension.Buffer,
                Alignment = 0,
                Width = vertexBufferSize,
                Height = 1,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Format = Format.FormatUnknown,
                SampleDesc = new SampleDesc()
                {
                    Count = 1,
                    Quality = 0,
                },
                Layout = TextureLayout.LayoutRowMajor,
                Flags = ResourceFlags.None
            };

            void* vertexBuffer;
            var iid = ID3D12Resource.Guid;
            SilkMarshal.ThrowHResult
            (
                Device.CreateCommittedResource(heapProperties, HeapFlags.None, resourceDesc, ResourceStates.GenericRead, null, &iid, &vertexBuffer)
            );
            VertexBuffer = (ID3D12Resource*)vertexBuffer;

            void* vertexDataBegin;
            Silk.NET.Direct3D12.Range range = new Silk.NET.Direct3D12.Range(0, 0);
            SilkMarshal.ThrowHResult(VertexBuffer.Map(0, &range, &vertexDataBegin));
            fixed (void* data = vertices)
            {
                Unsafe.CopyBlock(vertexDataBegin, data, vertexBufferSize);
            }

            VertexBufferView_ = new VertexBufferView()
            {
                BufferLocation = VertexBuffer.GetGPUVirtualAddress(),
                StrideInBytes = sizeof(float),
                SizeInBytes = vertexBufferSize,
            };
        }

        public DirectX12Device(IWindow window)
        {
            Window_ = window;
            D3d12 = D3D12.GetApi();
            DxGi = DXGI.GetApi(window, false);
            D3dCompiler = D3DCompiler.GetApi();

            uint dxgiFactoryFlags = 0;

#if DEBUG

        SilkMarshal.ThrowHResult
        (
            D3d12.GetDebugInterface(out DebugController)
        );
        DebugController.EnableDebugLayer();
        dxgiFactoryFlags |= 0x01;

#endif

            SilkMarshal.ThrowHResult
            (
                DxGi.CreateDXGIFactory2(dxgiFactoryFlags, out Factory)
            );

            Device = GetDevice();

            CreateCommandQueue();

            CreateSwapChain();

            CreateRTVHeap();

            CreateRenderTargetViews();

            CreateCommandAllocator();

            CreateRootSignature();

            CreatePipelineState();

            CreateFence();

            CreateFenceEvent();

            CreateCommandList();

            SetViewPort(0, 0, (uint)window.FramebufferSize.X, (uint)window.FramebufferSize.Y);

            WaitForPreviousFrame();

            CreateAssets();
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
            if (CommandList.AsVtblPtr() == null) return;

            Viewport viewport = new Viewport(0, 0, width, height, 0.0f, 1.0f);
            CommandList.RSSetViewports(1, viewport);

            Box2D<int> rect = new Box2D<int>(x, y, new Vector2D<int>((int)width, (int)height));
            CommandList.RSSetScissorRects(1, rect);
        }

        public void SetFrameBuffer(uint width, uint height)
        {
            if (SwapChain.AsVtblPtr() != null)
            {
                SwapChain.ResizeBuffers(0, width, height, Format.FormatR8G8B8A8Unorm, 0);
            }
        }

        private void SetResourceBarrier(ResourceStates stateBefore, ResourceStates stateAfter)
        {
            ResourceBarrier resourceBarrier = new ResourceBarrier();
            resourceBarrier.Type = ResourceBarrierType.Transition;
            resourceBarrier.Flags = ResourceBarrierFlags.None;
            resourceBarrier.Transition = new ResourceTransitionBarrier(RenderTargets[FrameBufferIndex], D3D12.ResourceBarrierAllSubresources, stateBefore, stateAfter);
            CommandList.ResourceBarrier(1, resourceBarrier);
        }

        public void ClearBuffer()
        {
            SilkMarshal.ThrowHResult
            (
                CommandAllocator.Reset()
            );

            SilkMarshal.ThrowHResult
            (
                CommandList.Reset(CommandAllocator, PipelineState)
            );

            CommandList.SetGraphicsRootSignature(RootSignature);


            SetResourceBarrier(ResourceStates.Present, ResourceStates.RenderTarget);


            CpuDescriptorHandle rtvHandle = new CpuDescriptorHandle();
            rtvHandle.Ptr = (uint)RtvHeap.GetCPUDescriptorHandleForHeapStart().Ptr + FrameBufferIndex * RtvDescriptorSize;
            CommandList.OMSetRenderTargets(1, rtvHandle, false, null);

            Box2D<int> rect = default;
            CommandList.ClearRenderTargetView(rtvHandle, CurrnetClearColor, 0, rect);

















            CommandList.IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
            CommandList.IASetVertexBuffers(0, 1, VertexBufferView_);
            //CommandList.IASetIndexBuffer(IndexBufferView_);
            //CommandList.DrawIndexedInstanced(,);
            CommandList.DrawInstanced(3, 1, 0, 0);
        }

        public void SwapBuffer()
        {
            SetResourceBarrier(ResourceStates.RenderTarget, ResourceStates.Present);

            SilkMarshal.ThrowHResult
            (
                CommandList.Close()
            );

            const int CommandListsCount = 1;
            void* commandList = CommandList;
            var ppCommandLists = stackalloc ID3D12CommandList*[CommandListsCount]
            {
                (ID3D12CommandList*)commandList,
            };
            CommandQueue.ExecuteCommandLists(CommandListsCount, ppCommandLists);

            SilkMarshal.ThrowHResult
            (
                SwapChain.Present(Window_.VSync ? 1u : 0u, 0)
            );

            FrameBufferIndex = SwapChain.GetCurrentBackBufferIndex();

            WaitForPreviousFrame();
        }

        public void Dispose()
        {
            VertexBuffer.Dispose();
            IndexBuffer.Dispose();


            Fence.Dispose();
            CommandList.Dispose();
            PipelineState.Dispose();
            RootSignature.Dispose();
            CommandAllocator.Dispose();
            for (int i = 0; i < RenderTargets.Length; i++)
            {
                RenderTargets[i].Dispose();
            }
            RtvHeap.Dispose();
            SwapChain.Dispose();
            CommandQueue.Dispose();
            HardwareAdapters.Dispose();
            Device.Dispose();
            Factory.Dispose();

#if DEBUG

        DebugController.Dispose();

#endif
        }
    }
}