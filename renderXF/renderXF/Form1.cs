using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using renderX2;

namespace renderXF
{
    public unsafe partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        renderX GL;
        renderX MiniGL;

        RenderThread RT;

        GLBuffer VertexBuffer;
        GLBuffer NormalBuffer;

        GLBuffer CICVertex, CICNormals, CIPVertex, CIPNormals;
        GLBuffer CubeVBO;

        Shader LineShader;
        GLBuffer LineBuffer;

        Shader CameraIndicator;
        Shader StandardShader;
        Shader cubeShader;

        Shader SSRShader;
        Shader SSRShaderPost;

        Shader VignetteShader;

        GLCachedBuffer cachedBuffer;
        Stopwatch sw = new Stopwatch();

        Vector3 ModelCenter;
        float DistanceCenter;

        Vector3 TargetPosition;
        Vector3 TargetRotation;

        #region Inputs
        bool CursorHook = false;
        bool mmbdown = false;
        bool rdown = false;
        bool ldown = false;
        bool udown = false;
        bool bdown = false;
        Vector2 KeyDelta = new Vector2(0, 0);
        int MMBDeltaX, MMBDeltaY;
        #endregion

        bool fcull = false;
        float* nbAddr;


        #region FOV/Matrix Lerp
        float TargetFOV = 90f;
        float CurrentFOV = 90f;

        float TMatrix = 0;
        float CMatrix = 0;
        #endregion

        bool FBCaching = false;


        Bitmap infoBitmap = new Bitmap(200, 200, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

        #region DeltaTimes
        float deltaTime;
        float deltaTimeAdjusted;
        Stopwatch deltaStopwatch = new Stopwatch();
        #endregion

        private void Form1_Load(object sender, EventArgs e)
        {
            StartupForm StartForm = new StartupForm(Environment.GetCommandLineArgs());
            StartForm.ShowDialog();
           
            #region StartupWindow
            if (StartupForm.ApplicationTerminated)
            {
                this.Close();
                Application.Exit();
                return;
            }
            #endregion

            #region WindowSettings
            this.Size = new Size(StartupForm.W, StartupForm.H);
            int WindowWidth = StartupForm.W - ClientSize.Width;
            int WindowHeight = StartupForm.H - ClientSize.Height;
            this.Size = new Size(StartupForm.W + WindowWidth, StartupForm.H + WindowHeight);
            this.SetStyle(ControlStyles.StandardDoubleClick, false);
            FBCaching = StartupForm.NormalsInterpolated;
            this.MouseWheel += Form1_MouseWheel;
            #endregion

            #region FullscreenSettings
            if (StartupForm.FullscreenSelected)
            {
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
                StartupForm.W = this.Width;
                StartupForm.H = this.Height;
            }
            #endregion

            STLImporter Importer = new STLImporter(StartupForm.FilePath);
            #region VertexBufferInit
            float[] vertexpoints = new float[Importer.AllTriangles.Length * 3 * 3];
            float[] normalBuffer = new float[Importer.AllTriangles.Length * 3];
            for (int i = 0; i < Importer.AllTriangles.Length; i++)
            {
                vertexpoints[i * 9] = Importer.AllTriangles[i].vertex1.x;
                vertexpoints[i * 9 + 1] = Importer.AllTriangles[i].vertex1.y;
                vertexpoints[i * 9 + 2] = Importer.AllTriangles[i].vertex1.z;
                vertexpoints[i * 9 + 3] = Importer.AllTriangles[i].vertex2.x;
                vertexpoints[i * 9 + 4] = Importer.AllTriangles[i].vertex2.y;
                vertexpoints[i * 9 + 5] = Importer.AllTriangles[i].vertex2.z;
                vertexpoints[i * 9 + 6] = Importer.AllTriangles[i].vertex3.x;
                vertexpoints[i * 9 + 7] = Importer.AllTriangles[i].vertex3.y;
                vertexpoints[i * 9 + 8] = Importer.AllTriangles[i].vertex3.z;
                normalBuffer[i * 3] = Importer.AllTriangles[i].normals.x;
                normalBuffer[i * 3 + 1] = Importer.AllTriangles[i].normals.y;
                normalBuffer[i * 3 + 2] = Importer.AllTriangles[i].normals.z;
            }
            #endregion

            ModelCenter = CalculateCenterOfModel(ref vertexpoints, out DistanceCenter);

           // vertexpoints = STLImporter.AverageUpFaceNormalsAndOutputVertexBuffer(Importer.AllTriangles, 45);
            NormalBuffer = new GLBuffer(normalBuffer, 3, MemoryLocation.Heap);
            VertexBuffer = new GLBuffer(vertexpoints, 3, MemoryLocation.Heap);
            nbAddr = (float*)NormalBuffer.GetAddress();
            vertexpoints = null;

            #region CubeObject
            CubeVBO = new GLBuffer(renderX.PrimitiveTypes.Cube(), 5, MemoryLocation.Heap);
            cubeShader = new Shader(CubeVS, CubeFS, GLRenderMode.TriangleFlat);
            cubeShader.SetOverrideAttributeCount(0);
            #endregion

            SSRShader = new Shader(null, SSR_Fragment, GLRenderMode.Triangle, GLExtraAttributeData.XYZ_XY_Both);

            VignetteShader = new Shader(VignettePass);
            SSRShaderPost = new Shader(SSR_Pass);


            #region ScreenGrid
            float[] vpoints = new float[]{
                0,0,0, 9,0,0,
                0,0,1, 9,0,1,
                0,0,2, 9,0,2,
                0,0,3, 9,0,3,
                0,0,4, 9,0,4,
                0,0,5, 9,0,5,
                0,0,6, 9,0,6,
                0,0,7, 9,0,7,
                0,0,8, 9,0,8,
                0,0,9, 9,0,9,

                0,0,0, 0,0,9,
                1,0,0, 1,0,9,
                2,0,0, 2,0,9,
                3,0,0, 3,0,9,
                4,0,0, 4,0,9,
                5,0,0, 5,0,9,
                6,0,0, 6,0,9,
                7,0,0, 7,0,9,
                8,0,0, 8,0,9,
                9,0,0, 9,0,9
            };


            #endregion
            LineBuffer = new GLBuffer(vpoints, 3, MemoryLocation.Heap);
            LineShader = new Shader(GridShaderVS, GridShaderFS, GLRenderMode.Line, GLExtraAttributeData.XYZ_CameraSpace);
            
            
            #region CameraIndicator
            CameraIndicator = new Shader(CIVS, null, GLRenderMode.TriangleGouraud);
            CameraIndicator.SetOverrideCameraTransform(true);
            CameraIndicator.SetOverrideAttributeCopy(true);

            //Inner Part
            CICVertex = new GLBuffer(renderX.PrimitiveTypes.CMI(), 3, MemoryLocation.Heap);
            CICNormals = new GLBuffer(renderX.PrimitiveTypes.CMINormals(), 3, MemoryLocation.Heap);

            //Outer Part
            CIPVertex = new GLBuffer(renderX.PrimitiveTypes.CMS(), 3, MemoryLocation.Heap);
            CIPNormals = new GLBuffer(renderX.PrimitiveTypes.CMSNormals(), 3, MemoryLocation.Heap);

            CIC_Normals = (float*)CICNormals.GetAddress();
            CIP_Normals = (float*)CIPNormals.GetAddress();
            #endregion

            GL = new renderX(StartupForm.W, StartupForm.H, this.Handle);
            GL.SelectBuffer(VertexBuffer);

            MiniGL = new renderX(130, 128);
            #region MiniGL
            MiniGL.SetMatrixData(90, 10);
            MiniGL.SelectShader(CameraIndicator);
            MiniGL.SetFaceCulling(true, false);
            #endregion

            StandardShader = new Shader(null, BasicShader, GLRenderMode.TriangleFlat);

           // Debug.WriteLine(StandardShader.GetFragmentAttributePreview(CubeVBO));

            RT = new RenderThread(144);
            RT.RenderFrame += RT_RenderFrame;
           
            GL.SelectShader(StandardShader);
            GL.SetMatrixData(90, 10);

            MiniGL.InitializeClickBuffer();
            MiniGL.SetClickBufferWrite(true);

            cachedBuffer = new GLCachedBuffer(GL);

       

            GL.SetWireFrameOFFSET(-0.1f);
            GL.SetFaceCulling(true, false);
            RT.Start();
        }

        Vector3 lightPosition = new Vector3(0, 0, 0);

        Vector3 cameraRotation = new Vector3(0, 0, 0);
        Vector3 cameraPosition = new Vector3(0, 0, -50);

        Vector3 lcP = new Vector3(0, 0, 0);
        Vector3 lcR = new Vector3(0, 0, 0);
        
        float tick = 0;
        
        bool requestHome = false;
        bool readyCache = false;
        bool requestClick = false;
        int rcX, rcY;

        void RT_RenderFrame()
        {
            CalculateDeltaTime();
            lightPosition = new Vector3(1000f * (float)Math.Cos(tick), 50, 1000f * (float)Math.Sin(tick));

            tick += 0.01f * deltaTimeAdjusted;
            int MouseX = 0;
            int MouseY = 0;
            
            #region CursorPosition
            if (CursorHook)
            {
                int cursorX = Cursor.Position.X;
                int cursorY = Cursor.Position.Y;

                int sourceX = 0;
                int sourceY = 0;

                this.Invoke((Action)delegate()
                {
                    sourceX = PointToScreen(Point.Empty).X + this.ClientSize.Width / 2;
                    sourceY = PointToScreen(Point.Empty).Y + this.ClientSize.Height / 2;
                });

                MouseX = cursorX - sourceX;
                MouseY = cursorY - sourceY;

                Cursor.Position = new Point(sourceX, sourceY);
                if (!requestHome)
                    cameraRotation += new Vector3(0, MouseY / 8f, MouseX / 8f);
            }
            else if (mmbdown & !requestHome)
            {
                int cursorX = Cursor.Position.X;
                int cursorY = Cursor.Position.Y;

                MouseX = cursorX - MMBDeltaX;
                MouseY = cursorY - MMBDeltaY;
                MMBDeltaX = cursorX; MMBDeltaY = cursorY;

                cameraPosition = renderX.Pan3D(cameraPosition, cameraRotation, MouseX / 8f, MouseY / 8f);
            }
            #endregion

            #region KeyboardDeltas
            if (rdown | ldown)
            {
                if (rdown)
                {
                    if (KeyDelta.x > 0)
                    {
                        KeyDelta.x = 0;
                    }
                    KeyDelta.x--;
                }
                else if (ldown)
                {
                    if (KeyDelta.x < 0)
                    {
                        KeyDelta.x = 0;
                    }
                    KeyDelta.x++;
                }
            }
            else
            {
                KeyDelta.x = 0;
            }

            if (udown | bdown)
            {
                if (udown)
                {
                    if (KeyDelta.y > 0)
                    {
                        KeyDelta.y = 0;
                    }
                    KeyDelta.y--;
                }
                else if (bdown)
                {
                    if (KeyDelta.y < 0)
                    {
                        KeyDelta.y = 0;
                    }
                    KeyDelta.y++;
                }
            }
            else
            {
                KeyDelta.y = 0;
            }
            #endregion

            #region CameraLerping
            if (!requestHome)
                cameraPosition = renderX.Pan3D(cameraPosition, cameraRotation, (KeyDelta.x / 32f) * deltaTimeAdjusted, 0, (KeyDelta.y / 32f) * deltaTimeAdjusted);
            else
            {
                cameraPosition = Vector3.Lerp(cameraPosition, TargetPosition, 0.1f * deltaTimeAdjusted);
                cameraRotation = Vector3.LerpAngle(cameraRotation, TargetRotation, 0.1f * deltaTimeAdjusted);

                if ((cameraPosition - TargetPosition).Abs() < 0.01f && (cameraRotation - TargetRotation).Abs().Repeat(360) < 0.01f) requestHome = false;
            }
            #endregion
            
            GL.ForceCameraRotation(cameraRotation);
            GL.ForceCameraPosition(cameraPosition);
            
            if (Math.Abs(CMatrix - TMatrix) > 0.001f) CMatrix = renderX.Lerp(CMatrix, TMatrix, 0.1f * deltaTimeAdjusted); else CMatrix = TMatrix;           
           // GL.SetMatrixData(90, 20 * 2 + 40, CMatrix);

            GL.SetMatrixData(90, 20, CMatrix);

            MiniGL.SetMatrixData(90, 350, CMatrix);

            PrepareLightningData();

            GL.ClearDepth();

            bool y = cameraPosition.Equals(lcP) && cameraRotation.Equals(lcR) && Math.Abs(CurrentFOV - TargetFOV) < 0.01f && CMatrix == TMatrix;
            if (!y) readyCache = false;

            ProcessCameraIndicator();


            GL.Clear(51, 153, 255);
            GL.SelectShader(StandardShader);
            GL.SelectBuffer(VertexBuffer);

            if (y & !readyCache & FBCaching)
            {
                GL.CreateCopyOnDraw(cachedBuffer);
                GL.Draw();
                readyCache = true;
                y = false;
            }


            sw.Start();
            if (readyCache) GL.CopyFromCache(cachedBuffer, CopyMethod.SplitLoop);  else GL.Draw();
            sw.Stop();
            

            GL.Draw(LineBuffer, LineShader);
            GL.Draw(CubeVBO, cubeShader);

            

         //   GL.SelectShader(SSRShaderPost);
         //   GL.Pass();



            GL.Line3D(new Vector3(0, 0, 0), new Vector3(1000000, 0, 0), 255, 0, 0);
            GL.Line3D(new Vector3(0, 0, 0), new Vector3(0, 1000000, 0), 0, 255, 0);
            GL.Line3D(new Vector3(0, 0, 0), new Vector3(0, 0, 1000000), 0, 0, 255);

            GL.SelectShader(VignetteShader);


          //  sw.Start();
          //  if (fcull) GL.Pass();
         //   else GL.VignettePass();

         //   sw.Stop();

            MiniGL.BlitInto(GL, GL.RenderWidth - 130, GL.RenderHeight - 128, Color.FromArgb(255, 0, 0, 0));

         //   GL.BlitInto(infoBitmap, new Rectangle(0, 0, 200, 200)); 

         //   using (Graphics g = Graphics.FromImage(infoBitmap))
        //        g.DrawString("My String", this.Font, Brushes.Black, 0, 0);

         //   GL.BlitFrom(infoBitmap, new Rectangle(0, 0, 40, 40), 0, 0);

            GL.Blit();

          //  this.Invoke((Action)delegate() { this.Text = (sw.Elapsed.TotalMilliseconds) + " ms"; });
            this.Invoke((Action)delegate() { this.Text = (1000f / deltaTime) + " FPS, DrawTime: " + sw.Elapsed.TotalMilliseconds + "ms"; });

            lcR = cameraRotation;
            lcP = cameraPosition;

            sw.Reset();
        }

        void CalculateDeltaTime()
        {
            deltaStopwatch.Stop();
            deltaTime = (float)deltaStopwatch.Elapsed.TotalMilliseconds;
            deltaTimeAdjusted = deltaTime * 0.144f;
            deltaStopwatch.Restart(); 
        }

        void ProcessCameraIndicator()
        {
            int FI;
            int YI;

            bool BS = false;
            bool GS = false;
            bool RS = false;
            bool RS1 = false;
            bool GS1 = false;
            bool BS1 = false;
            bool OS = false;

            if (requestClick && MiniGL.GetClickBufferData(130 - (GL.RenderWidth - rcX), 128 - (GL.RenderHeight - (this.ClientSize.Height - rcY)), out FI, out YI))
            {
                if (YI == 1) //BLUE
                {
                    TargetPosition = new Vector3(0, 0, DistanceCenter) + ModelCenter;
                    TargetRotation = new Vector3(0, 0, 180);
                }
                else if (YI == 2) //RED
                {
                    TargetPosition = new Vector3(DistanceCenter, 0, 0) + ModelCenter;
                    TargetRotation = new Vector3(0, 0, -90);
                }
                else if (YI == 3) //GREEN
                {
                    TargetPosition = new Vector3(0, DistanceCenter, 0) + ModelCenter;
                    TargetRotation = new Vector3(0, 90, 0);
                }
                else if (YI == 4) //WHITE
                {
                    if (TMatrix == 1f)
                        TMatrix = 0f;
                    else
                        TMatrix = 1f;
                }
                else if (YI == 5) //BLUE1
                {
                    TargetPosition = new Vector3(0, 0, -DistanceCenter) + ModelCenter;
                    TargetRotation = new Vector3(0, 0, 0);
                }
                else if (YI == 6) //GREEN1
                {
                    TargetPosition = new Vector3(0, -DistanceCenter, 0) + ModelCenter;
                    TargetRotation = new Vector3(0, -90, 0);
                }
                else if (YI == 7) //RED1
                {
                    TargetPosition = new Vector3(-DistanceCenter, 0, 0) + ModelCenter;
                    TargetRotation = new Vector3(0, 0, 90);
                }

                if (YI != 4)
                    requestHome = true;
                requestClick = false;
            }

            int sourceX = 0;
            int sourceY = 0;

            this.Invoke((Action)delegate(){
                sourceX = this.PointToClient(Cursor.Position).X;
                sourceY = this.PointToClient(Cursor.Position).Y;
            });

            if (MiniGL.GetClickBufferData(130 - (GL.RenderWidth - sourceX), 128 - (GL.RenderHeight - (this.ClientSize.Height - sourceY)), out FI, out YI))
            {
                BS = YI == 1;
                RS = YI == 2;
                GS = YI == 3;
                OS = YI == 4;
                BS1 = YI == 5;
                GS1 = YI == 6;
                RS1 = YI == 7;
            }


            MiniGL.Clear();
            MiniGL.ClearClickBuffer();
            MiniGL.ClearDepth();

            //DRAW CENTER AXIS INDICATOR PIECE
            MiniGL.SetClickBufferInt(4);
            MiniGL.SelectBuffer(CICVertex);
            CI_RAngle = new Vector3(0, 0, 0);
            CI_Color = new Vector3(1f, 1f, 1f);
            if (OS) CI_Color = CI_Color * 0.5f;
            SMPLT = true; MiniGL.Draw(); SMPLT = false;  

            MiniGL.SelectBuffer(CIPVertex);

            //DRAW BLUE AXIS INDICATOR
            MiniGL.SetClickBufferInt(1);
            CI_Color = new Vector3(1, 0.69f, 0.14f);
            if (BS) CI_Color = CI_Color * 0.5f;
            CI_RAngle = new Vector3(0, 0, 180);
            MiniGL.Draw();

            //DRAW RED AXIS INDICATOR
            MiniGL.SetClickBufferInt(2);
            CI_Color = new Vector3(0, 0, 1);
            if (RS) CI_Color = CI_Color * 0.5f;
            CI_RAngle = new Vector3(0, 0, 90);
            MiniGL.Draw();

            MiniGL.SetClickBufferInt(7);
            CI_Color = new Vector3(1, 1, 1);
            if (RS1) CI_Color = CI_Color * 0.5f;
            CI_RAngle = new Vector3(0, 0, 270);
            MiniGL.Draw();


            MiniGL.SetClickBufferInt(5);
            CI_Color = new Vector3(1, 1, 1);
            if (BS1) CI_Color = CI_Color * 0.5f;
            CI_RAngle = new Vector3(0, 0, 0);
            MiniGL.Draw();


            MiniGL.SetClickBufferInt(3);
            CI_Color = new Vector3(0, 1, 0);
            if (GS) CI_Color = CI_Color * 0.5f;
            CI_RAngle = new Vector3(0, -90, 0);
            MiniGL.Draw();

            MiniGL.SetClickBufferInt(6);
            CI_Color = new Vector3(1, 1, 1);
            if (GS1) CI_Color = CI_Color * 0.5f;
            CI_RAngle = new Vector3(0, 90, 0);
            MiniGL.Draw();
        }

        #region FormEvents

        Vector3 CalculateCenterOfModel(ref float[] Input, out float BiggestDelta)
        {
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

            for (int i = 0; i < Input.Length / 3; i++)
            {
                if (Input[i * 3] > max.x) max.x = Input[i * 3];
                if (Input[i * 3] < min.x) min.x = Input[i * 3];

                if (Input[i * 3 + 1] > max.y) max.y = Input[i * 3 + 1];
                if (Input[i * 3 + 1] < min.y) min.y = Input[i * 3 + 1];

                if (Input[i * 3 + 2] > max.z) max.z = Input[i * 3 + 2];
                if (Input[i * 3 + 2] < min.z) min.z = Input[i * 3 + 2];
            }

            float BG = (max.x - min.x);
            if ((max.y - min.y) > BG) BG = (max.y - min.y);
            if ((max.z - min.z) > BG) BG = (max.z - min.z);

            BiggestDelta = BG * 2;
            return new Vector3((max.x - min.x) / 2f + min.x, (max.y - min.y) / 2f + min.y, (max.z - min.z) / 2f + min.z);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                lock (GL.ThreadLock)
                {
                    if (RT != null){
                        RT.Abort();
                    }

                    if (VertexBuffer != null)
                        VertexBuffer.Dispose();

                    if (NormalBuffer != null)
                        NormalBuffer.Dispose();

                  //  if (myTexture != null)
                   //     myTexture.Dispose();

                }
               
            }
            catch { 
            
            }
        }
  
        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                Cursor.Position = new Point(PointToScreen(Point.Empty).X + this.ClientSize.Width / 2, PointToScreen(Point.Empty).Y + this.ClientSize.Height / 2);
                Cursor.Hide();
                CursorHook = true;
                this.Text = "CursorHook: " + CursorHook;
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                rcX = e.X;
                rcY = e.Y;
                requestClick = true;
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D)
            {
                rdown = true;
            }

            if (e.KeyCode == Keys.A)
            {
                ldown = true;
            }

            if (e.KeyCode == Keys.W)
            {
                udown = true;
            }

            if (e.KeyCode == Keys.S)
            {
                bdown = true;
            }

            if (e.KeyCode == Keys.Escape)
            {
                Cursor.Show();
                CursorHook = false;
                this.Text = "CursorHook: " + CursorHook;
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.D)
            {
                rdown = false;
            }

            if (e.KeyCode == Keys.A)
            {
                ldown = false;
            }

            if (e.KeyCode == Keys.W)
            {
                udown = false;
            }

            if (e.KeyCode == Keys.S)
            {
                bdown = false;
            }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (GL != null)
            {
                GL.SetViewportSize(this.ClientSize.Width, this.ClientSize.Height);
                readyCache = false; 
            }
        }

        void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
        //    TargetFOV = Math.Min(Math.Max(TargetFOV + e.Delta / 60, 1), 179);
           // TargetFOV = Math.Min(Math.Max(TargetFOV + e.Delta / 60, 0), 90);


            TMatrix = Math.Min((float)Math.Max(TMatrix + ((e.Delta / 120f) / 20f), 0f), 1f);

            this.Text = "FOV: " + TMatrix;
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' '){
                GL.SetFaceCulling(true, fcull);
              
            //    GL.SetLinkedWireframe(fcull, 255, 255, 255);
                fcull = !fcull;
                readyCache = false;
            }

            if (e.KeyChar == 'c')
            {
                MessageBox.Show("POS: " + cameraPosition + Environment.NewLine + "ROT: " + cameraRotation);
            }

            if (e.KeyChar == 'r')
            {
              //  cameraPosition = new Vector3(0, 0, 0);
              //  cameraRotation = new Vector3(0, 0, 0);
                TargetRotation = new Vector3(0, 0, 0);
                TargetPosition = new Vector3(0, 0, 0);

                TargetFOV = 90;
                requestHome = true;
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            MMBDeltaX = Cursor.Position.X;
            MMBDeltaY = Cursor.Position.Y;
            mmbdown = e.Button == System.Windows.Forms.MouseButtons.Middle;
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Middle) mmbdown = false;
        }

        #endregion
    }
}
