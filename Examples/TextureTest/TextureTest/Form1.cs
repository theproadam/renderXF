using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using renderX2;

namespace TextureTest
{
    public unsafe partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        renderX GL;
        GLBuffer vertexBuffer;
        Shader cubeShader;

        Vector3 cameraPosition, cameraRotation;
        Vector3 objectRotation, oSin, oCos;

        GLTexture texture2d;
        int* TEXTURE_ADDR;
        int textureWidthMinusOne;
        int textureHeightMinusOne;
        int textureHeight;

        int rotAngle = 0;

        private void Form1_Load(object sender, EventArgs e)
        {
            this.ClientSize = new System.Drawing.Size(800, 600);

            GL = new renderX(800, 600, this.Handle);
            GL.Clear();

            vertexBuffer = new GLBuffer(renderX.PrimitiveTypes.Cube(), 5, MemoryLocation.Heap);
            cubeShader = new Shader(CubeVS, CubeFS, GLRenderMode.Triangle);

            cameraPosition = new Vector3(0, 0, -100);
            cameraRotation = new Vector3(0, 0, 0);

            GL.SetMatrixData(90, 160, 0);

            GL.ForceCameraPosition(cameraPosition);
            GL.ForceCameraRotation(cameraRotation);

            GL.SetFaceCulling(true, false);


            texture2d = new GLTexture("myTexture.png", MemoryLocation.Heap, DuringLoad.Flip);
            TEXTURE_ADDR = (int*)texture2d.GetAddress();

            textureHeight = texture2d.Height;
            textureWidthMinusOne = texture2d.Width - 1;
            textureHeightMinusOne = texture2d.Height - 1;


            Timer timer = new Timer();
            timer.Interval = 15;
            timer.Tick += timer_Tick;

            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            GL.Clear(0, 0, 0);
            GL.ClearDepth();

            GL.SelectBuffer(vertexBuffer);
            GL.SelectShader(cubeShader);

            GL.Draw();

            GL.Blit();
            objectRotation = new Vector3(0, 0, rotAngle++);

            oSin = GetSin(objectRotation);
            oCos = GetCos(objectRotation);
            cameraPosition.y = 10f * (float)Math.Sin(rotAngle / 25.1f);
            GL.ForceCameraPosition(cameraPosition);
        }

        unsafe void CubeVS(float* OUT, float* IN, int Index)
        {
            Vector3 pos = new Vector3(IN[0], IN[1], IN[2]);
            pos = RotateVector(pos, oCos, oSin);

            OUT[0] = pos.x * 50;
            OUT[1] = pos.y * 50;
            OUT[2] = pos.z * 50;
        }

        unsafe void CubeFS(byte* BGR, float* Attributes, int Index)
        {
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
