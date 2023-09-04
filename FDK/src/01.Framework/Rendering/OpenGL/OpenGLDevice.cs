using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SkiaSharp;

namespace SampleFramework
{
    class OpenGLDevice : IGraphicsDevice
    {
        public static GL Gl;

        public OpenGLDevice(IWindow window)
        {
            Gl = window.CreateOpenGL();
            Gl.Enable(GLEnum.Blend);
            Gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
        }

        public void SetClearColor(float r, float g, float b, float a)
        {
            Gl.ClearColor(r, g, b, a);
        }

        public void SetViewPort(int x, int y, uint width, uint height)
        {
            Gl.Viewport(x, y, width, height);
        }

        public void SetFrameBuffer(uint width, uint height)
        {
        }

        public void ClearBuffer()
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        public void SwapBuffer()
        {
        }

        public IPolygon GenPolygon(float[] vertices, uint[] indices, float[] uvs)
        {
            return new OpenGLPolygon(vertices, indices, uvs);
        }

        public IShader GenShader()
        {
            return new OpenGLShader(
                
                @"
                #version 330 core

                layout (location = 0) in vec3 aPosition;
                layout (location = 1) in vec2 aTexCoord;

                uniform mat4 mvp;

                out vec2 texcoord;

                void main()
                {
                    vec4 position = vec4(aPosition, 1.0);
                    position = mvp * position;
                    
                    gl_Position = position;
                    texcoord = aTexCoord;
                }
                "
                ,
                @"
                #version 330 core

                in vec2 texcoord;
                out vec4 out_color;
                uniform sampler2D texture1;
                uniform float opacity;
                uniform vec4 textureRect;

                void main()
                {
                    vec2 texcoord2 = vec2(textureRect.x, textureRect.y);
                    texcoord2.x += texcoord.x * textureRect.z;
                    texcoord2.y += texcoord.y * textureRect.w;

                    vec4 color = texture(texture1, texcoord2);
                    color.a *= opacity;
                    out_color = color;
                }
                "
            );
        }

        public ITexture GenTexture(SKBitmap bitmap)
        {
            return new OpenGLTexture(bitmap);
        }

        public void DrawPolygon(IPolygon polygon, IShader shader, ITexture texture)
        {
            OpenGLPolygon glPolygon = (OpenGLPolygon)polygon;
            OpenGLShader glShader = (OpenGLShader)shader;
            OpenGLTexture glTexture = (OpenGLTexture)texture;

            if (glTexture == null) return;

            Gl.BindTexture(TextureTarget.Texture2D, glTexture.TextureHandle);
            Gl.BindVertexArray(glPolygon.VAO);
            unsafe
            {
                Gl.UseProgram(glShader.ShaderProgram);
                Gl.DrawElements(PrimitiveType.Triangles, glPolygon.IndiceCount, DrawElementsType.UnsignedInt, (void*)0);
            }
        }

        public void Dispose()
        {
        }
    }
}