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
        public ComPtr<ID3D12PipelineState> PipelineState;

        internal unsafe DirectX12Shader(DirectX12Device device, string shaderSource)
        {
            GraphicsPipelineStateDesc graphicsPipelineStateDesc = new GraphicsPipelineStateDesc();

            DirectXShaderSource directXShaderSource = new DirectXShaderSource(device.D3dCompiler);

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

            graphicsPipelineStateDesc.PRootSignature = device.RootSignature;
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
                device.Device.CreateGraphicsPipelineState(graphicsPipelineStateDesc, &iid, &pipelineState)
            );

            directXShaderSource.Dispose();

            PipelineState = (ID3D12PipelineState*)pipelineState;
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
            PipelineState.Dispose();
        }
    }
}