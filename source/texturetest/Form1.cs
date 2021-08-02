using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using renderX2;

namespace TextureTest
{
    public unsafe partial class Form1 : Form
    {
        private const int RENDER_HEIGHT = 512;
        private const int RENDER_WIDTH = RENDER_HEIGHT;

        public Form1()
        {
            InitializeComponent();

            this.BackColor = Color.Black;
        }

        renderX GL;

        GLTexture texture2d;
        int* TEXTURE_ADDR;
        int textureWidthMinusOne;
        int textureHeightMinusOne;
        int textureHeight;

        Int32[] bits;
        GCHandle bitsHandle;
        Bitmap bitmap;

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ClientSize = new System.Drawing.Size(RENDER_WIDTH * 2, RENDER_HEIGHT * 2);
            this.bits = new int[RENDER_WIDTH * RENDER_HEIGHT];
            this.bitsHandle = GCHandle.Alloc(this.bits, GCHandleType.Pinned);
            this.bitmap = new Bitmap(RENDER_WIDTH, RENDER_HEIGHT, RENDER_WIDTH * 4, PixelFormat.Format32bppPArgb, this.bitsHandle.AddrOfPinnedObject());

            GL = new renderX(RENDER_WIDTH, RENDER_HEIGHT);
            //GL.Clear();

            var cameraPosition = new Vector3(128, 128, 4000);
            var cameraRotation = new Vector3(0, 180, 0);

            GL.SetMatrixData(90, 128, 1);

            GL.ForceCameraPosition(cameraPosition);
            GL.ForceCameraRotation(cameraRotation);

            GL.SetFaceCulling(false, true);


            var textureScaled = new Bitmap(RENDER_WIDTH, RENDER_HEIGHT);
            using (var gr = Graphics.FromImage(textureScaled))
            {
                gr.DrawImage(Image.FromFile("container2.png"), 0, 0, textureScaled.Width, textureScaled.Height);
            }
            texture2d = new GLTexture(textureScaled, MemoryLocation.Heap, DuringLoad.Flip);
            TEXTURE_ADDR = (int*)texture2d.GetAddress();

            textureHeight = texture2d.Height;
            textureWidthMinusOne = texture2d.Width - 1;
            textureHeightMinusOne = texture2d.Height - 1;

            Timer timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += timer_Tick;

            timer.Start();

            this.Paint += new System.Windows.Forms.PaintEventHandler(this.DoPaint);
        }

        private Tuple<float[], int> makePlane(Vector3 origin, Vector2 size, Vector2 tessellation)
        {
            System.Diagnostics.Debug.Assert((int)tessellation.x > 0);
            System.Diagnostics.Debug.Assert((int)tessellation.y > 0);

            const int terrainVertexStride = 6; // XYZ,UVZ - the first three are eaten by RenderXF, the latter 3 are passed through to the frag shader.

            var quadCountColumns = (int)tessellation.x;
            var quadCountRows = (int)tessellation.y;
            var terrainVertexPoints = new float[2 * 3 * quadCountColumns * quadCountRows * terrainVertexStride]; // Two triangles per quad, 3 vertices per tri.

            float textureULeft, textureURight;
            float textureVTop, textureVBottom;
            int quadCol, quadRow;
            var cornerBottomLeft = new renderX2.Vector3();
            var cornerBottomRight = new renderX2.Vector3();
            var cornerTopLeft = new renderX2.Vector3();
            var cornerTopRight = new renderX2.Vector3();
            for (quadCol = 0; quadCol < quadCountColumns; ++quadCol)
            {
                cornerBottomLeft.x = cornerTopLeft.x = origin.x + size.x * quadCol / quadCountColumns; // 0 - 1, for interpolation.
                textureULeft = (float)quadCol / quadCountColumns;
                cornerBottomRight.x = cornerTopRight.x = origin.x + size.x * (quadCol + 1) / quadCountColumns; // 0 - 1, for interpolation.
                textureURight = (float)(quadCol + 1) / quadCountColumns;

                for (quadRow = 0; quadRow < quadCountRows; ++quadRow)
                {
                    cornerBottomLeft.y = cornerBottomRight.y = origin.y + size.y * quadRow / quadCountRows;
                    textureVBottom = (float)quadRow / quadCountRows; // 0 - 1, for interpolation.
                    cornerTopLeft.y = cornerTopRight.y = origin.y + size.y * (quadRow + 1) / quadCountRows;
                    textureVTop = (float)(quadRow + 1) / quadCountRows; // 0 - 1, for interpolation.

                    cornerBottomLeft.z = cornerBottomRight.z = cornerTopLeft.z = cornerTopRight.z = origin.z;

                    if (double.IsInfinity(cornerBottomLeft.z) || double.IsNaN(cornerBottomLeft.z))
                    {
                        cornerBottomLeft.z = 0f;
                    }
                    if (double.IsInfinity(cornerBottomRight.z) || double.IsNaN(cornerBottomRight.z))
                    {
                        cornerBottomRight.z = 0f;
                    }
                    if (double.IsInfinity(cornerTopLeft.z) || double.IsNaN(cornerTopLeft.z))
                    {
                        cornerTopLeft.z = 0f;
                    }
                    if (double.IsInfinity(cornerTopRight.z) || double.IsNaN(cornerTopRight.z))
                    {
                        cornerTopRight.z = 0f;
                    }

                    var baseIndex = (quadCol + quadRow * quadCountColumns) * terrainVertexStride * 6; // 6 verts per quad, due to decomposing to 2 tris per quad first.

                    // Triangle 1: Right, Top
                    terrainVertexPoints[baseIndex + 0] = cornerTopRight.x; // X
                    terrainVertexPoints[baseIndex + 1] = cornerTopRight.y; // Y
                    terrainVertexPoints[baseIndex + 2] = cornerTopRight.z; // Z
                    terrainVertexPoints[baseIndex + 3] = textureURight; // U
                    terrainVertexPoints[baseIndex + 4] = textureVTop; // V
                    terrainVertexPoints[baseIndex + 5] = cornerTopRight.z; // Z

                    // Triangle 1: Right, Bottom
                    terrainVertexPoints[baseIndex + 6] = cornerBottomRight.x; // X
                    terrainVertexPoints[baseIndex + 7] = cornerBottomRight.y; // Y
                    terrainVertexPoints[baseIndex + 8] = cornerBottomRight.z; // Z
                    terrainVertexPoints[baseIndex + 9] = textureURight; // U
                    terrainVertexPoints[baseIndex + 10] = textureVBottom; // V
                    terrainVertexPoints[baseIndex + 11] = cornerBottomRight.z; // Z

                    // Triangle 1: Left, Bottom
                    terrainVertexPoints[baseIndex + 12] = cornerBottomLeft.x; // X
                    terrainVertexPoints[baseIndex + 13] = cornerBottomLeft.y; // Y
                    terrainVertexPoints[baseIndex + 14] = cornerBottomLeft.z; // Z
                    terrainVertexPoints[baseIndex + 15] = textureULeft; // U
                    terrainVertexPoints[baseIndex + 16] = textureVBottom; // V
                    terrainVertexPoints[baseIndex + 17] = cornerBottomLeft.z; // Z

                    // Triangle 2: Left, Top
                    terrainVertexPoints[baseIndex + 18] = cornerTopLeft.x; // X
                    terrainVertexPoints[baseIndex + 19] = cornerTopLeft.y; // Y
                    terrainVertexPoints[baseIndex + 20] = cornerTopLeft.z; // Z
                    terrainVertexPoints[baseIndex + 21] = textureULeft; // U
                    terrainVertexPoints[baseIndex + 22] = textureVTop; // V
                    terrainVertexPoints[baseIndex + 23] = cornerTopLeft.z; // Z

                    // Triangle 2: Right, Top
                    terrainVertexPoints[baseIndex + 24] = cornerTopRight.x; // X
                    terrainVertexPoints[baseIndex + 25] = cornerTopRight.y; // Y
                    terrainVertexPoints[baseIndex + 26] = cornerTopRight.z; // Z
                    terrainVertexPoints[baseIndex + 27] = textureURight; // U
                    terrainVertexPoints[baseIndex + 28] = textureVTop; // V
                    terrainVertexPoints[baseIndex + 29] = cornerTopRight.z; // Z

                    // Triangle 2: Left, Bottom
                    terrainVertexPoints[baseIndex + 30] = cornerBottomLeft.x; // X
                    terrainVertexPoints[baseIndex + 31] = cornerBottomLeft.y; // Y
                    terrainVertexPoints[baseIndex + 32] = cornerBottomLeft.z; // Z
                    terrainVertexPoints[baseIndex + 33] = textureULeft; // U
                    terrainVertexPoints[baseIndex + 34] = textureVBottom; // V
                    terrainVertexPoints[baseIndex + 35] = cornerBottomLeft.z; // Z
                }
            }
            return Tuple.Create(terrainVertexPoints, terrainVertexStride);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            // Force a re-render:
            this.Invalidate();
        }

        private void DoPaint(object sender, PaintEventArgs e)
        {
            // Yes, creating the vertex data every frame is a bad idea - when you are rendering many frames.
            // However in Anaximander I don't have a timer, I render once and blit to a Bitmap. After that it's all destroyed.
            var planeData = makePlane(new Vector3(0, 0, 0), new Vector2(256, 256), new Vector2(256, 256));
            var vertices = planeData.Item1;
            var vertexStride = planeData.Item2;

            GL.Clear(255, 0, 0);
            GL.ClearDepth();

            using (var vertexBuffer = new GLBuffer(vertices, vertexStride, MemoryLocation.Heap))
            {
                GL.SelectBuffer(vertexBuffer);

                var cubeShader = new Shader(CubeVS, CubeFS, GLRenderMode.Triangle);
                cubeShader.SetOverrideAttributeCount(vertexStride - 3); // UVZ
                GL.SelectShader(cubeShader);

                GL.Draw();
            }

            GL.BlitIntoBitmap(this.bitmap, new Point(), new Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height));

            // Draw centered on form.
            var drawArea = new RectangleF(0, 0, this.ClientSize.Width, this.ClientSize.Height);
            e.Graphics.DrawImageUnscaled(this.bitmap, ((int)drawArea.Width - this.bitmap.Width) / 2, ((int)drawArea.Height - this.bitmap.Height) / 2);
        }

        unsafe void CubeVS(float* OUT, float* IN, int Index)
        {
            //Vector3 pos = new Vector3(IN[0], IN[1], IN[2]);
            //pos = RotateVector(pos, oCos, oSin);

            OUT[0] = IN[0];
            OUT[1] = IN[1];
            OUT[2] = IN[2];
        }

        unsafe void CubeFS(byte* BGR, float* Attributes, int Index)
        {
            // Uncomment block to force rendering of single color, excepting where the index < 50.
            // Useful for debugging issue #5.
            //BGR[0] = 255;
            //BGR[1] = 255;
            //BGR[2] = 255;
            //if (Index < 50)
            //{
            //    BGR[0] = 0;
            //    BGR[1] = 255;
            //    BGR[2] = 0;
            //}
            //return;

            int U = (int)(Clamp01(Attributes[0]) * textureWidthMinusOne);
            int V = (int)(Clamp01(Attributes[1]) * textureHeightMinusOne);

            *((int*)BGR) = TEXTURE_ADDR[U + V * textureHeight];
        }

        public float Clamp01(float value)
        {
            if (value < 0) return 0f;
            else if (value > 1) return 1f;
            else return value;
        }

        Vector3 RotateVector(Vector3 input, Vector3 co, Vector3 si)
        {
            float fiX = input.x * co.z - input.z * si.z;
            float fiZ = input.z * co.z + input.x * si.z;
            float ndY = input.y * co.y + fiZ * si.y;

            return new Vector3(fiX * co.x - ndY * si.x, ndY * co.x + fiX * si.x, fiZ * co.y - input.y * si.y);
        }

        Vector3 GetCos(Vector3 EulerAnglesDEG)
        {
            return new Vector3((float)Math.Cos(EulerAnglesDEG.x / 57.2958f), (float)Math.Cos(EulerAnglesDEG.y / 57.2958f), (float)Math.Cos(EulerAnglesDEG.z / 57.2958f));
        }

        Vector3 GetSin(Vector3 EulerAnglesDEG)
        {
            return new Vector3((float)Math.Sin(EulerAnglesDEG.x / 57.2958f), (float)Math.Sin(EulerAnglesDEG.y / 57.2958f), (float)Math.Sin(EulerAnglesDEG.z / 57.2958f));
        }

    }
}
