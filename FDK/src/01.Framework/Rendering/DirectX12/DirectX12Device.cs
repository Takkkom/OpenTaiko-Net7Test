using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using SkiaSharp;


namespace SampleFramework
{
    unsafe class DirectX12Device : IGraphicsDevice
    {
        private const uint FrameCount = 2;

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

        private ComPtr<ID3D12DescriptorHeap> DSVHeap;

        private ID3D12Resource*[] RenderTargets = new ID3D12Resource*[FrameCount];

        private ComPtr<ID3D12Resource> DepthStencil;

        private ComPtr<ID3D12CommandAllocator>[] CommandAllocator = new ComPtr<ID3D12CommandAllocator>[FrameCount];

        private ComPtr<ID3D12RootSignature> RootSignature;

        private ComPtr<ID3D12PipelineState> PipelineState;

        private ComPtr<ID3D12GraphicsCommandList>[] CommandList = new ComPtr<ID3D12GraphicsCommandList>[FrameCount];

        private ComPtr<ID3D12Fence> Fence;

        private uint[] FenceValue = new uint[FrameCount];

        private IntPtr FenceEvent;

        private uint RtvDescriptorSize;

        private float[] CurrnetClearColor;

        private IWindow Window_;

        private uint FrameBufferIndex;

        private bool IsActivate;



        private ComPtr<ID3D12Resource> VertexBuffer;

        private VertexBufferView VertexBufferView_;

        private ComPtr<ID3D12Resource> IndexBuffer;

        private IndexBufferView IndexBufferView_;

        private Viewport viewport;
        private Box2D<int> rect;



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
            SwapChainDesc1 swapChainDesc = new(){
                Width = (uint)Window_.FramebufferSize.X,
                Height = (uint)Window_.FramebufferSize.Y,
                Format = Format.FormatR8G8B8A8Unorm,
                SampleDesc = new SampleDesc(1, 0),
                BufferUsage = DXGI.UsageRenderTargetOutput,
                BufferCount = FrameCount,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipDiscard,
                //Flags = (uint)SwapChainFlag.AllowModeSwitch,
                AlphaMode = AlphaMode.Ignore
            };

            SwapChainFullscreenDesc swapChainFullscreenDesc = new()
            {
                RefreshRate = new Rational(0, 1),
                ScanlineOrdering = ModeScanlineOrder.Unspecified,
                Scaling = ModeScaling.Unspecified,
                Windowed = true
            };

            void* device = CommandQueue;
            IDXGISwapChain1* swapChain;

            /*
            SilkMarshal.ThrowHResult
            (
                Factory.CreateSwapChainForHwnd((IUnknown*)device, Window_.Native.DXHandle.Value, swapChainDesc, swapChainFullscreenDesc, (IDXGIOutput*)0, &swapChain)
            );
            */
            SilkMarshal.ThrowHResult
            (
                Window_.CreateDxgiSwapchain((IDXGIFactory2*)Factory.AsVtblPtr(), (IUnknown*)device, &swapChainDesc, &swapChainFullscreenDesc, (IDXGIOutput*)0, &swapChain)
            );

            SwapChain = (IDXGISwapChain3*)swapChain;

            FrameBufferIndex = SwapChain.GetCurrentBackBufferIndex();
        }

        private void CreateRTVHeap()
        {
            DescriptorHeapDesc rtvHeapDesc = new DescriptorHeapDesc()
            {
                NumDescriptors = FrameCount + 1,
                Type = DescriptorHeapType.Rtv,
                Flags = DescriptorHeapFlags.None
            };
            
            void* rtvHeap = null;
            var iid = ID3D12DescriptorHeap.Guid;
            SilkMarshal.ThrowHResult
            (
                Device.CreateDescriptorHeap(&rtvHeapDesc, ref iid, &rtvHeap)
            );
            RtvHeap = (ID3D12DescriptorHeap*)rtvHeap;

            RtvDescriptorSize = Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv);
        }

        protected virtual void CreateDSVHeap()
        {
            var dsvHeapDesc = new DescriptorHeapDesc
            {
                NumDescriptors = 1,
                Type = DescriptorHeapType.Dsv,
            };

            ID3D12DescriptorHeap* dsvHeap;

            var iid = ID3D12DescriptorHeap.Guid;
            SilkMarshal.ThrowHResult(Device.CreateDescriptorHeap(&dsvHeapDesc, &iid, (void**) &dsvHeap));

            DSVHeap = dsvHeap;
        }

        private void CreateRenderTargetViews()
        {
            CpuDescriptorHandle rtvHandle = new CpuDescriptorHandle();
            rtvHandle.Ptr = RtvHeap.GetCPUDescriptorHandleForHeapStart().Ptr;
            var iid = ID3D12Resource.Guid;

            for (uint i = 0; i < FrameCount; i++)
            {
                ID3D12Resource* renderTarget;
                SilkMarshal.ThrowHResult
                (
                    SwapChain.GetBuffer(i, ref iid, (void**)&renderTarget)
                );
                RenderTargets[i] = renderTarget;

                
                Device.CreateRenderTargetView(renderTarget, (RenderTargetViewDesc*)0, rtvHandle);
                rtvHandle.Ptr += RtvDescriptorSize;
            }
        }

        private void CreateDepthStencil()
        {
            
            ID3D12Resource* depthStencil;

            var heapProperties = new HeapProperties(HeapType.Default);

            var resourceDesc = new ResourceDesc
            (
                ResourceDimension.Texture2D,
                0ul,
                (ulong) Window_.FramebufferSize.X,
                (uint) Window_.FramebufferSize.Y,
                1,
                1,
                Format.FormatD32Float,
                new SampleDesc() {Count = 1, Quality = 0},
                TextureLayout.LayoutUnknown,
                ResourceFlags.AllowDepthStencil
            );

            var clearValue = new ClearValue(Format.FormatD32Float, depthStencil: new DepthStencilValue(1.0f, 0));

            var iid = ID3D12Resource.Guid;
            SilkMarshal.ThrowHResult
            (
                Device.CreateCommittedResource
                (
                    &heapProperties, HeapFlags.None, &resourceDesc, ResourceStates.DepthWrite,
                    &clearValue, &iid, (void**) &depthStencil
                )
            );

            var dsvDesc = new DepthStencilViewDesc
            {
                Format = Format.FormatD32Float,
                ViewDimension = DsvDimension.Texture2D
            };
            Device.CreateDepthStencilView(depthStencil, &dsvDesc, DSVHeap.GetCPUDescriptorHandleForHeapStart());

            DepthStencil = depthStencil;
        }

        private void CreateCommandAllocator()
        {
            var iid = ID3D12CommandAllocator.Guid;

            for(int i = 0; i < FrameCount; i++)
            {
                void* commandAllocator;
                SilkMarshal.ThrowHResult
                (
                    Device.CreateCommandAllocator(CommandListType.Direct, &iid, &commandAllocator)
                );

                CommandAllocator[i] = (ID3D12CommandAllocator*)commandAllocator;
            }
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
            FenceValue[0] = 1;
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

            DirectXShaderSource directXShaderSource = new DirectXShaderSource(D3dCompiler);

            const int ElementsLength = 1;

            var inputElementDescs = stackalloc InputElementDesc[ElementsLength]
            {
                new InputElementDesc()
                {
                    SemanticName = (byte*)SilkMarshal.StringToMemory("POS"),
                    SemanticIndex = 0,
                    Format = Format.FormatR32G32B32Float,
                    InputSlot = 0,
                    AlignedByteOffset = 0,
                    InputSlotClass = InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                }
            };

            /*
            fixed (InputElementDesc* elements = inputElementDescs)
            {
                graphicsPipelineStateDesc.InputLayout = new InputLayoutDesc()
                {
                    PInputElementDescs = elements,
                    NumElements = (uint)inputElementDescs.Length,
                };
            }
            */

            graphicsPipelineStateDesc.InputLayout = new InputLayoutDesc()
            {
                PInputElementDescs = inputElementDescs,
                NumElements = (uint)ElementsLength,
            };

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

            var defaultStencilOp = new DepthStencilopDesc
            {
                StencilFailOp = StencilOp.Keep,
                StencilDepthFailOp = StencilOp.Keep,
                StencilPassOp = StencilOp.Keep,
                StencilFunc = ComparisonFunc.Always
            };

            graphicsPipelineStateDesc.BlendState = blendDesc;


            graphicsPipelineStateDesc.DepthStencilState = new ()
            {
                DepthEnable = 1,
                DepthWriteMask = DepthWriteMask.All,
                DepthFunc = ComparisonFunc.Less,
                StencilEnable = 0,
                StencilReadMask = D3D12.DefaultStencilReadMask,
                StencilWriteMask = D3D12.DefaultStencilWriteMask,
                FrontFace = defaultStencilOp,
                BackFace = defaultStencilOp
            };
            graphicsPipelineStateDesc.SampleMask = uint.MaxValue;
            graphicsPipelineStateDesc.PrimitiveTopologyType = PrimitiveTopologyType.Triangle;
            graphicsPipelineStateDesc.NumRenderTargets = 1;
            graphicsPipelineStateDesc.RTVFormats[0] = Format.FormatR8G8B8A8Unorm;
            graphicsPipelineStateDesc.SampleDesc.Count = 1;
            graphicsPipelineStateDesc.DepthStencilState.DepthEnable = 0;


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

            for(int i = 0; i < FrameCount; i++)
            {
                SilkMarshal.ThrowHResult
                (
                    Device.CreateCommandList(0, CommandListType.Direct, CommandAllocator[i], (ID3D12PipelineState*)0, &iid, &commandList)
                );

                CommandList[i] = (ID3D12GraphicsCommandList*)commandList;

                CommandList[i].Close();
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
        0, 1, 2
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

            Silk.NET.Direct3D12.Range range = new Silk.NET.Direct3D12.Range();

            void* vertexDataBegin;
            SilkMarshal.ThrowHResult(VertexBuffer.Map(0, &range, &vertexDataBegin));

            var vertic = (float*)SilkMarshal.Allocate(sizeof(float) * vertices.Length);
            for(int i = 0; i < vertices.Length; i++)
            {
                vertic[i] = vertices[i];
            }
            
            Unsafe.CopyBlock(vertexDataBegin, vertic, vertexBufferSize);
            VertexBuffer.Unmap(0, (Silk.NET.Direct3D12.Range*)0);

            VertexBufferView_ = new VertexBufferView()
            {
                BufferLocation = VertexBuffer.GetGPUVirtualAddress(),
                StrideInBytes = sizeof(float) * 3,
                SizeInBytes = vertexBufferSize,
            };
        }

        private void CreateAssets2()
        {
            uint bufferSize = (uint)(sizeof(uint) * indices.Length);

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
                Width = bufferSize,
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

            void* indexBuffer;
            var iid = ID3D12Resource.Guid;
            SilkMarshal.ThrowHResult
            (
                Device.CreateCommittedResource(heapProperties, HeapFlags.None, resourceDesc, ResourceStates.GenericRead, null, &iid, &indexBuffer)
            );
            IndexBuffer = (ID3D12Resource*)indexBuffer;

            Silk.NET.Direct3D12.Range range = new Silk.NET.Direct3D12.Range();

            void* vertexDataBegin;
            SilkMarshal.ThrowHResult(IndexBuffer.Map(0, &range, &vertexDataBegin));

            var data = (uint*)SilkMarshal.Allocate(sizeof(uint) * indices.Length);
            for(int i = 0; i < indices.Length; i++)
            {
                data[i] = indices[i];
            }
            
            Unsafe.CopyBlock(vertexDataBegin, data, bufferSize);
            IndexBuffer.Unmap(0, (Silk.NET.Direct3D12.Range*)0);

            VertexBufferView_ = new VertexBufferView()
            {
                BufferLocation = IndexBuffer.GetGPUVirtualAddress(),
                SizeInBytes = bufferSize,
            };
        }

        private void WaitForGpu(bool moveToNextFrame, bool resetFrameBufferIndex = false)
        {
            SilkMarshal.ThrowHResult
            (
                CommandQueue.Signal(Fence, FenceValue[FrameBufferIndex])
            );

            if (moveToNextFrame)
            {
                FrameBufferIndex = SwapChain.GetCurrentBackBufferIndex();
            }

            if (!moveToNextFrame || (Fence.GetCompletedValue() < FenceValue[FrameBufferIndex]))
            {
                SilkMarshal.ThrowHResult
                (
                    Fence.SetEventOnCompletion(FenceValue[FrameBufferIndex], FenceEvent.ToPointer())
                );
                _ = SilkMarshal.WaitWindowsObjects(FenceEvent);
            }

            FenceValue[FrameBufferIndex]++;

            if (resetFrameBufferIndex) FrameBufferIndex = 0;
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

            CreateDSVHeap();

            CreateRenderTargetViews();

            CreateDepthStencil();

            CreateCommandAllocator();

            CreateRootSignature();

            CreatePipelineState();

            CreateFence();

            CreateFenceEvent();

            CreateCommandList();

            CreateAssets();

            CreateAssets2();

            WaitForGpu(false);

            SetViewPort(0, 0, (uint)window.FramebufferSize.X, (uint)window.FramebufferSize.Y);
        }

        public void SetClearColor(float r, float g, float b, float a)
        {
            CurrnetClearColor = new float[] { r, g, b, a };
        }

        public void SetViewPort(int x, int y, uint width, uint height)
        {
            if (CommandList[FrameBufferIndex].AsVtblPtr() == null) return;

            viewport = new Viewport(0, 0, width, height, 0.0f, 1.0f);
            rect = new Box2D<int>(x, y, new Vector2D<int>((int)width, (int)height));
        }

        public void SetFrameBuffer(uint width, uint height)
        {
            if (width <= 0 || height <= 0) return;
            
            WaitForGpu(false, true);

            for (uint i = 0; i < FrameCount; i++)
            {
                RenderTargets[i]->Release();
                FenceValue[i] = FenceValue[FrameBufferIndex];
            }

            SilkMarshal.ThrowHResult
            (
                SwapChain.ResizeBuffers(FrameCount, width, height, Format.FormatR8G8B8A8Unorm, 0)
            );

            CreateRenderTargetViews();

            DepthStencil.Release();
            CreateDepthStencil();
        }

        private void SetResourceBarrier(ResourceStates stateBefore, ResourceStates stateAfter)
        {
            ResourceBarrier resourceBarrier = new ResourceBarrier();
            resourceBarrier.Type = ResourceBarrierType.Transition;
            resourceBarrier.Flags = ResourceBarrierFlags.None;
            resourceBarrier.Transition = new ResourceTransitionBarrier(RenderTargets[FrameBufferIndex], D3D12.ResourceBarrierAllSubresources, stateBefore, stateAfter);
            CommandList[FrameBufferIndex].ResourceBarrier(1, resourceBarrier);
        }

        public void ClearBuffer()
        {
            WaitForGpu(false);

            SilkMarshal.ThrowHResult
            (
                CommandAllocator[FrameBufferIndex].Reset()
            );

            SilkMarshal.ThrowHResult
            (
                CommandList[FrameBufferIndex].Reset(CommandAllocator[FrameBufferIndex], (ID3D12PipelineState*)0)
            );
            
            CommandList[FrameBufferIndex].SetGraphicsRootSignature(RootSignature);
            CommandList[FrameBufferIndex].RSSetViewports(1, viewport);
            CommandList[FrameBufferIndex].RSSetScissorRects(1, rect);


            SetResourceBarrier(ResourceStates.Present, ResourceStates.RenderTarget);


            CpuDescriptorHandle rtvHandle = new CpuDescriptorHandle();
            rtvHandle.Ptr = RtvHeap.GetCPUDescriptorHandleForHeapStart().Ptr + FrameBufferIndex * RtvDescriptorSize;
            CommandList[FrameBufferIndex].OMSetRenderTargets(1, rtvHandle, false, null);

            fixed (float* color = CurrnetClearColor)
            {
                CommandList[FrameBufferIndex].ClearRenderTargetView(rtvHandle, color, 0, (Box2D<int>*)0);
            }

            var dsvHandle = DSVHeap.GetCPUDescriptorHandleForHeapStart();
            CommandList[FrameBufferIndex].ClearDepthStencilView(dsvHandle, ClearFlags.Depth, 1, 0, 0, (Box2D<int>*)0);

















            CommandList[FrameBufferIndex].SetPipelineState(PipelineState);
            CommandList[FrameBufferIndex].IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
            CommandList[FrameBufferIndex].IASetVertexBuffers(0, 1, VertexBufferView_);
            CommandList[FrameBufferIndex].IASetIndexBuffer(IndexBufferView_);
            CommandList[FrameBufferIndex].DrawIndexedInstanced(3, 1, 0, 0, 0);
        }

        public void SwapBuffer()
        {
            SetResourceBarrier(ResourceStates.RenderTarget, ResourceStates.Present);

            SilkMarshal.ThrowHResult
            (
                CommandList[FrameBufferIndex].Close()
            );
            
            const int CommandListsCount = 1;
            void* commandList = CommandList[FrameBufferIndex];
            var ppCommandLists = stackalloc ID3D12CommandList*[CommandListsCount]
            {
                (ID3D12CommandList*)commandList,
            };
            CommandQueue.ExecuteCommandLists(CommandListsCount, ppCommandLists);

            SilkMarshal.ThrowHResult
            (
                Device.GetDeviceRemovedReason()
            );

            SilkMarshal.ThrowHResult
            (
                SwapChain.Present(Window_.VSync ? 1u : 0u, 0)
            );

            WaitForGpu(false);

            FrameBufferIndex = SwapChain.GetCurrentBackBufferIndex();

            IsActivate = true;
        }

        public IPolygon GenPolygon(float[] vertices, uint[] indices, float[] uvs)
        {
            return new DirectX12Polygon(vertices, indices, uvs);
        }

        public IShader GenShader()
        {
            return new DirectX12Shader(
                @"

                Texture2D g_texture : register(t0);
                SamplerState g_sampler : register(s0);

                struct vs_in {
                    float3 position_local : POS;
                    float2 uvposition_local : UVPOS;
                };

                struct vs_out {
                    float4 position_clip : SV_POSITION;
                    float2 uvposition_clip : TEXCOORD0;
                };
                
                cbuffer ConstantBufferStruct
                {
                    float4x4 Projection;
                    float4 Color;
                    float4 TextureRect;
                }

                vs_out vs_main(vs_in input) {
                    vs_out output = (vs_out)0;

                    float4 position = float4(input.position_local, 1.0);
                    position = mul(Projection, position);

                    output.position_clip = position;

                    float2 texcoord = float2(TextureRect.x, TextureRect.y);
                    texcoord.x += input.uvposition_local.x * TextureRect.z;
                    texcoord.y += input.uvposition_local.y * TextureRect.w;

                    output.uvposition_clip = texcoord;

                    return output;
                }

                float4 ps_main(vs_out input) : SV_TARGET {
                    float4 totalcolor = float4(1.0, 1.0, 1.0, 1.0);

                    totalcolor = g_texture.Sample(g_sampler, input.uvposition_clip);

                    totalcolor.rgba *= Color.rgba;

                    return totalcolor;
                }
                ");
        }

        public unsafe ITexture GenTexture(void* data, int width, int height, RgbaType rgbaType)
        {
            return new DirectX12Texture(data, width, height, rgbaType);
        }

        public void DrawPolygon(IPolygon polygon, IShader shader, ITexture texture, BlendType blendType)
        {
        }

        public unsafe SKBitmap GetScreenPixels()
        {  
            return null;
        }

        public void Dispose()
        {
            WaitForGpu(false);

            VertexBuffer.Dispose();
            IndexBuffer.Dispose();


            Fence.Dispose();
            for (int i = 0; i < CommandList.Length; i++)
            {
                CommandList[i].Dispose();
            }
            PipelineState.Dispose();
            RootSignature.Dispose();
            for (int i = 0; i < CommandAllocator.Length; i++)
            {
                CommandAllocator[i].Dispose();
            }
            for (int i = 0; i < RenderTargets.Length; i++)
            {
                RenderTargets[i]->Release();
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