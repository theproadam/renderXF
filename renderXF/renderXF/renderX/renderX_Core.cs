using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace renderX2
{
    public unsafe partial class renderX : IDisposable
    {
        // GeometryPath is Responsible for all Shader Logic
        GeometryPath ops;
        internal object ThreadLock = new object();

        internal Shader SelectedShader;
        GLBuffer SB;
        GLFrameBuffer SFB;

        IntPtr DrawingBuffer;
        IntPtr DepthBuffer;

        BITMAPINFO BINFO;
        IntPtr TargetDC;
        IntPtr LinkedHandle;
 
        
        //IntPtr FaceBuffer;
        private bool disposed = false;
        bool RequestCopyAfterDraw = false;
        GLCachedBuffer CD;

        bool ExceptionOverride = false;
        bool miniGLMode = false;

        IntPtr ScaleBuffer;
        bool scaleBufferInitialized = false;
        bool isScaling = false;
        int scaleWidth, scaleHeight;

        bool ClickBufferEnabled = false;
        IntPtr ClickBuffer;
        IntPtr AnyBuffer;

        IntPtr SkyboxPointerBuffer;
        IntPtr SkyboxFaceCountData;
        IntPtr SkyboxData;
        IntPtr SkyboxTexturePointers;

        [DllImport("AcceleratedFill.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void FillFlatC(int index);

        [DllImport("AcceleratedFill.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void ScanFill(int index);

        [DllImport("AcceleratedFill.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SetFillData(float** sPtr, int* bsPtr, int** txtPtr, int rD, float* sdPtr);




        ~renderX()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                lock (ThreadLock)
                {
                    if (DrawingBuffer != IntPtr.Zero)
                        Marshal.FreeHGlobal(DrawingBuffer);

                    if (DepthBuffer != IntPtr.Zero)
                        Marshal.FreeHGlobal(DepthBuffer);

                    if (ClickBufferEnabled)
                        Marshal.FreeHGlobal(ClickBuffer);

                    if (scaleBufferInitialized)
                        Marshal.FreeHGlobal(ScaleBuffer);

                    ReleaseDC(LinkedHandle, TargetDC);
                }
                disposed = true;
            }
        }

        /// <summary>
        /// Initializes the renderer with a output width, height, and target output
        /// </summary>
        /// <param name="ViewportWidth">The Width of the output in pixels</param>
        /// <param name="ViewportHeight">The Height of the output in pixels</param>
        /// <param name="OutputHandle">The Handle of the control which will display the output</param>
        public unsafe renderX(int ViewportWidth, int ViewportHeight, IntPtr OutputHandle)
	    {
            if (ViewportWidth <= 2 | ViewportHeight <= 2)
                throw new Exception("Invalid Viewport Size.");

            lock (ThreadLock)
            {
                DrawingBuffer = Marshal.AllocHGlobal(ViewportWidth * ViewportHeight * 4);
                DepthBuffer = Marshal.AllocHGlobal(ViewportWidth * ViewportHeight * 4);

                BINFO = new BITMAPINFO();
                BINFO.bmiHeader.biBitCount = 32; //BITS PER PIXEL
                BINFO.bmiHeader.biWidth = ViewportWidth;
                BINFO.bmiHeader.biHeight = ViewportHeight;
                BINFO.bmiHeader.biPlanes = 1;
                unsafe{
                    BINFO.bmiHeader.biSize = (uint)sizeof(BITMAPINFOHEADER);
                }

                LinkedHandle = OutputHandle;
                TargetDC = GetDC(OutputHandle);
                ops = new GeometryPath(this, ViewportWidth, ViewportHeight);
                ops.bptr = (byte*)DrawingBuffer;
            }
	    }

        /// <summary>
        /// Initializes the renderer in MiniGL Mode. No Output Blit is supported. Use This Mode for In-Picture Rendering.
        /// </summary>
        /// <param name="ViewportWidth">The Width of the output in pixels</param>
        /// <param name="ViewportHeight">The Height of the output in pixels</param>
        public unsafe renderX(int ViewportWidth, int ViewportHeight)
        {
            if (ViewportWidth <= 2 | ViewportHeight <= 2)
                throw new Exception("Invalid Viewport Size.");


            lock (ThreadLock)
            {
                DrawingBuffer = Marshal.AllocHGlobal(ViewportWidth * ViewportHeight * 4);
                DepthBuffer = Marshal.AllocHGlobal(ViewportWidth * ViewportHeight * 4);

                BINFO = new BITMAPINFO();
                BINFO.bmiHeader.biBitCount = 32; //BITS PER PIXEL
                BINFO.bmiHeader.biWidth = ViewportWidth;
                BINFO.bmiHeader.biHeight = ViewportHeight;
                BINFO.bmiHeader.biPlanes = 1;
                unsafe
                {
                    BINFO.bmiHeader.biSize = (uint)sizeof(BITMAPINFOHEADER);
                }

                ops = new GeometryPath(this, ViewportWidth, ViewportHeight);
                ops.bptr = (byte*)DrawingBuffer;
            }
        }

        public void SelectBuffer(GLBuffer TargetBuffer)
        {
            if (TargetBuffer == null)
                throw new Exception("Target Buffer cannot be null!");

            if (SFB != null) SFB = null;

            TargetBuffer.BufferLock = ThreadLock;
            SB = TargetBuffer;
            //FaceBuffer = Marshal.AllocHGlobal(31 * SB.FaceCount * 4);
        }

        public void SelectBuffer(GLFrameBuffer TargetBuffer)
        {
            if (TargetBuffer == null)
                throw new Exception("Target Buffer cannot be null!");

            if (SB != null) SB = null;

            TargetBuffer.BufferLock = ThreadLock;
            SFB = TargetBuffer;
        }

        public void SelectShader(Shader Shader)
        {
            if (Shader == null)
                throw new Exception("Shader cannot be null!");

            SelectedShader = Shader;
        }

        public unsafe void Draw()
        {
            if (SelectedShader == null)
                throw new Exception("No Shader Selected, Cannot Draw!");

            if (SelectedShader.isPost)
                throw new Exception("Please use the Pass() function for Post Processing Shaders!");

            if (SB == null)
                throw new Exception("No Buffer Selected, Cannot Draw!");

            if (SelectedShader.manualCamera & SelectedShader.ShaderVertex == null)
                throw new Exception("A Vertex Shader Is Required for Manual Camera!");


            lock(ThreadLock){
                ops.p = SB.ptr;
                ops.bptr = (byte*)DrawingBuffer;
                ops.dptr = (float*)DepthBuffer;
                ops.iptr = (int*)DrawingBuffer;

                //Wireframe Debug is for debugging only. It doesn't care about 90% of the configuration options
                if (SelectedShader.sType != GLRenderMode.WireframeDebug)
                {
                    //Check for illegal parameters before drawing.
                    if (SelectedShader.ShaderFragment == null & SelectedShader.sType != GLRenderMode.TriangleGouraud)
                        throw new Exception("No Fragment Shader Detected!");

                    if (SelectedShader.manualCamera & SelectedShader.ShaderVertex == null)
                        throw new Exception("A Vertex Shader Is Required for Manual Camera!");

                    if (SelectedShader.copyAttributes & SelectedShader.vShdrAttr + 3 > SB.stride)
                        throw new Exception("Cannot have Automatic Copy For Different Amounts of Vertex Attributes!");

                    //ATTRIBUTE DATA CONFIGURATION
                    ops.COPY_ATTRIB_MANUAL = !SelectedShader.copyAttributes;
                    ops.CAMERA_BYPASS = SelectedShader.manualCamera;
                    ops.HAS_VERTEX_SHADER = SelectedShader.ShaderVertex != null;
                    ops.ATTRIBLVL = (int)SelectedShader.sLevel;
                    ops.attribdata = ops.ATTRIBLVL != 0;

                    //SAFE SHADER METHOD POINTERS
                    ops.FS = SelectedShader.ShaderFragment;
                    ops.VS = SelectedShader.ShaderVertex;
                }

                // ------------------------------------------------------------------------------------------------------ //
                // ------------------------------------------------------------------------------------------------------ //
                // ------------------------------------------------------------------------------------------------------ //

                if (SelectedShader.sType == GLRenderMode.Triangle){
                    //Warn about performance benefits of GLRenderMode.TriangleFlat
                    if (SB.stride == 3 && SelectedShader.sLevel == GLExtraAttributeData.None & !ExceptionOverride)
                        throw new WarningException("There is no need to use GLRenderMode.Triangle. Use GLRenderMode.TriangleFlat for better performance.");

                    //INPUT OVERRIDE SETTINGS
                    if (SelectedShader.vShdrAttr >= 0 && SelectedShader.vShdrAttr < 30)
                        ops.Stride = 3 + SelectedShader.vShdrAttr;
                    else ops.Stride = SB.stride;

                    //DATA IN STRIDE
                    ops.ReadStride = SB.stride;
                    ops.FaceStride = 3 * SB.stride;

                    //not sure what this is
                    if (ops.Stride == 3)
                        Parallel.For(0, SB.FaceCount, ops.FillFlat);
                    else
                        Parallel.For(0, SB.FaceCount, ops.FillFull);
                }
                else if (SelectedShader.sType == GLRenderMode.Wireframe)
                {
                    if (SelectedShader.sLevel != GLExtraAttributeData.None)
                        throw new Exception("Wireframe does not yet support extra attribute data. Sorry!");
                    //FIX THIS ^^^^

                    if (SelectedShader.ShaderVertex != null) //Vertex shader not supported
                        throw new Exception("no shader vertex");

                    //INPUT OVERRIDE SETTINGS
                    if (SelectedShader.vShdrAttr >= 0 && SelectedShader.vShdrAttr < 30)
                        ops.Stride = 3 + SelectedShader.vShdrAttr;
                    else ops.Stride = SB.stride;

                    //DATA IN STRIDE
                    ops.ReadStride = SB.stride;
                    ops.FaceStride = 3 * SB.stride;


                    Parallel.For(0, SB.FaceCount, ops.WireFrame);
                }
                else if (SelectedShader.sType == GLRenderMode.TriangleFlat)
                {
                    if (SelectedShader.sLevel != GLExtraAttributeData.None)
                        throw new Exception("GLRenderMode.TriangleFlat does not support extra attirbute data. Attribute pointer will be null.");

                    if (SelectedShader.vShdrAttr > 0 & !ExceptionOverride)
                        throw new WarningException("TriangleFlat does not load attributes");

                    if (SB.stride > 3 & SelectedShader.vShdrAttr != 0)
                        throw new WarningException("TriangleFlat does not load attributes");

                  
                    
                    ops.Stride = 3;
                    ops.ReadStride = SB.stride;
                    ops.FaceStride = 3 * SB.stride;



                 //   for (int i = 0; i < SB.FaceCount; i++) ops.FillTrueFlat(i);
                    Parallel.For(0, SB.FaceCount, ops.FillTrueFlat);
                }
                else if (SelectedShader.sType == GLRenderMode.TriangleFlatCPP)
                {
                    if (SelectedShader.sLevel != GLExtraAttributeData.None)
                        throw new Exception("GLRenderMode.TriangleFlat does not support extra attirbute data. Attribute pointer will be null.");

                    if (SelectedShader.vShdrAttr > 0 & !ExceptionOverride)
                        throw new WarningException("TriangleFlat does not load attributes");

                    if (SB.stride > 3 & SelectedShader.vShdrAttr != 0)
                        throw new WarningException("TriangleFlat does not load attributes");

                    ops.ForceUpdate();

                    ops.Stride = 3;
                    ops.ReadStride = SB.stride;
                    ops.FaceStride = 3 * SB.stride;


                    ops.CPPFill(SB.FaceCount);

                  //  for (int i = 0; i < SB.FaceCount; i++) ops.FillWithCPP(i);
                  //  Parallel.For(0, SB.FaceCount, FillFlatC);

                 //   for (int i = 0; i < SB.FaceCount; i++) FillFlatC(i);
                }
                else if (SelectedShader.sType == GLRenderMode.TriangleGouraud)
                {
                    if (SelectedShader.ShaderFragment != null & !ExceptionOverride)
                        throw new WarningException("Triangle Gouraud Will Ignore Any Fragment Shaders used!");

                    if (SelectedShader.ShaderVertex == null)
                        throw new Exception("Gouraud Shading requires a vertex shader. Please use 3 attributes for the BGR colors");

                //    if (SB.stride != 6 & !ExceptionOverride)
                //        throw new WarningException("It is recommended to only have normals as the attributes.");

                    if (SelectedShader.sLevel != GLExtraAttributeData.None)
                        throw new Exception("Gouraud mode cannot provide Extra Attribute Data");

                    if (SelectedShader.copyAttributes)
                        throw new Exception("Attributes cannot be automatically copied for Gouraud mode");


                    ops.Stride = 6;
                    ops.ReadStride = SB.stride;
                    ops.FaceStride = 3 * SB.stride;



                    Parallel.For(0, SB.FaceCount, ops.FillGouraud);
                }
                else if (SelectedShader.sType == GLRenderMode.Line)
                {
                    ops.Stride = SB.stride;
                    ops.FaceStride = 2 * SB.stride;
                    ops.ReadStride = SB.stride;

                    int b = SB.getifsuspectedline();
                    Parallel.For(0, b, ops.LineMode);
                }
                else if (SelectedShader.sType == GLRenderMode.WireframeDebug)
                {
                    ops.Stride = 3;

                    if (SB.stride != 3) 
                        throw new Exception("Wireframe Debug Does Not Support != 3 Stride");

                    Parallel.For(0, SB.FaceCount, ops.WireFrameDebug);
                }

                if (RequestCopyAfterDraw)
                {
                    CopyQuick();
                    RequestCopyAfterDraw = false;
                }
            }
        }

        public unsafe void Draw(GLBuffer TargetBuffer, Shader TargetShader)
        {
            lock (ThreadLock)
            {
                GLBuffer oldBuffer = SB;
                Shader oldShdr = SelectedShader;

                SelectBuffer(TargetBuffer);
                SelectShader(TargetShader);

                Draw();

                SelectBuffer(oldBuffer);
                SelectShader(oldShdr);

            }
        }

        public unsafe void Pass()
        {
            lock (ThreadLock)
            {
                if (SelectedShader == null)
                    throw new Exception("No Shader Selected, Cannot Draw!");

                if (!SelectedShader.isPost)
                    throw new Exception("This Function takes a post processing shader!");

                if (SelectedShader.ShaderPass == null)
                    throw new Exception("No post processing delegate attached!");

                b = SelectedShader.ShaderPass;
                addr = (byte*)DrawingBuffer;

                Parallel.For(0, RenderHeight, delegatetese);
            } 
        }

        Shader.FragmentPass b;
        byte* addr;

        unsafe void delegatetese(int i)
        {
            for (int o = 0; o < RenderWidth; o++)
            {
                b(addr + i * RenderWidth * 4 + o * 4, o, i);
            }
        }

        public void Blit()
        {
            if (miniGLMode)
                throw new Exception("MiniGL Mode Does Not Support Blit!");

            lock (ThreadLock)
            {

                if (!scaleBufferInitialized) SetDIBitsToDevice(TargetDC, 0, 0, (uint)ops.renderWidth, (uint)ops.renderHeight, 0, 0, 0, (uint)ops.renderHeight, DrawingBuffer, ref BINFO, 0);
                else
                {
                    _2DScaleX = (float)RenderWidth / (float)scaleWidth;
                    _2DScaleY = (float)RenderHeight / (float)scaleHeight;
                    _sptr = (int*)ScaleBuffer;
                    _iptr = (int*)DrawingBuffer;
                    _bptr = (byte*)DrawingBuffer;

                    Parallel.For(0, scaleHeight, _2DScale);

                    SetDIBitsToDevice(TargetDC, 0, 0, (uint)scaleWidth, (uint)scaleHeight, 0, 0, 0, (uint)scaleHeight, ScaleBuffer, ref BINFO, 0);
                }
                    
            }
        }

        public unsafe void Clear(byte R, byte G, byte B)
        {
            lock (ThreadLock)
            {
                _iClear = (((((byte)R) << 8) | (byte)G) << 8) | (byte)B;
                _iptr = (int*)DrawingBuffer;

                Parallel.For(0, RenderHeight, _2D_Clear);
            }
        }

        public void Clear(byte Whiteness)
        {
            lock (ThreadLock)
            {
                MemSet(DrawingBuffer, Whiteness, RenderWidth * RenderHeight * 4);
            }
        }

        public void ClearGradient(byte From, byte To)
        {
            float slope = ((float)From - (float)To) / (0f - (float)RenderHeight);
            float b= -slope * 0 + (float)From;

            Parallel.For(0, RenderHeight, i => {
                MemSet(IntPtr.Add(DrawingBuffer, RenderWidth * 4 * i), (byte)(slope * (float)i + b), RenderWidth * 4);
            });
        }

        public void ClearDepth()
        {
            lock (ThreadLock)
            {
                RtlZeroMemory(DepthBuffer, RenderWidth * RenderHeight * 4);
            }
        }

        public void Clear()
        {
            lock (ThreadLock)
            {
                 RtlZeroMemory(DrawingBuffer, RenderWidth * RenderHeight * 4);
            }
        }

        public void ClearClickBuffer()
        {
            lock (ThreadLock)
            {
                if (!ClickBufferEnabled)
                    throw new Exception("No Click Buffer Exists!");

                RtlZeroMemory(ClickBuffer, RenderWidth * RenderHeight * 4);
            }
        }
 
        public void Clear(GLFrameBuffer TargetBuffer)
        {
            if (TargetBuffer == null)
                throw new Exception("null buffer!");

            lock (ThreadLock)
            {
                RtlZeroMemory(TargetBuffer.GetAddress(), TargetBuffer.size);
            }
        }

        public void Line2D(int x1, int y1, int x2, int y2, byte R, byte G, byte B)
        {
            ops.bptr = (byte*)DrawingBuffer;
            ops.dptr = (float*)DepthBuffer;
            ops.DrawLine(x1, y1, x2, y2, B, G, R);
        }

        public void Line3D(Vector3 From, Vector3 To, byte R, byte G, byte B)
        {
            lock (ThreadLock)
            {
                int sD = ops.Stride;
                ops.bptr = (byte*)DrawingBuffer;
                ops.dptr = (float*)DepthBuffer;

                ops.Stride = 3;
                int oi = ops.lValue;
                float oV = ops.zoffset;

                ops.zoffset = 0;
                ops.lValue = (((((byte)R) << 8) | (byte)G) << 8) | (byte)B;

                ops.DrawLine3D(From, To);
                ops.Stride = sD;

                ops.lValue = oi;
                ops.zoffset = oV;

            }
        }

        public void DrawSkybox(GLCubemap Cubemap)
        {
            lock (ThreadLock)
            {
                if (!Cubemap.isValid())
                    throw new Exception("Invalid Cubemap");

                ops.rd = 0;
                
                ops.bptr = (byte*)DrawingBuffer;
                ops.dptr = (float*)DepthBuffer;
                ops.iptr = (int*)DrawingBuffer;

                SkyboxPointerBuffer = Marshal.AllocHGlobal(RenderHeight * 12 * 4);
                SkyboxData = Marshal.AllocHGlobal(4 * 77 * 12);
                SkyboxFaceCountData = Marshal.AllocHGlobal(RenderHeight * 4);
                SkyboxTexturePointers = Marshal.AllocHGlobal(12 * 4);

                RtlZeroMemory(SkyboxFaceCountData, RenderHeight * 4);
              //  RtlZeroMemory(SkyboxData, 4 * 76 * 12);
              //  RtlZeroMemory(SkyboxPointerBuffer, RenderHeight * 12 * 4);


                ops.sptr = (float**)SkyboxPointerBuffer;
                ops.sdptr = (float*)SkyboxData;
                ops.bsptr = (int*)SkyboxFaceCountData;

                ops.txptr = (int**)SkyboxTexturePointers;

                ops.txptr[2] = (int*)Cubemap.FRONT.ptr;
                ops.txptr[3] = (int*)Cubemap.FRONT.ptr;
                ops.txptr[0] = (int*)Cubemap.BACK.ptr;
                ops.txptr[1] = (int*)Cubemap.BACK.ptr;
                ops.txptr[4] = (int*)Cubemap.LEFT.ptr;
                ops.txptr[5] = (int*)Cubemap.LEFT.ptr;
                ops.txptr[6] = (int*)Cubemap.RIGHT.ptr;
                ops.txptr[7] = (int*)Cubemap.RIGHT.ptr;
                ops.txptr[10] = (int*)Cubemap.TOP.ptr;
                ops.txptr[11] = (int*)Cubemap.TOP.ptr;
                ops.txptr[8] = (int*)Cubemap.BOTTOM.ptr;
                ops.txptr[9] = (int*)Cubemap.BOTTOM.ptr;

                ops.skyboxSize = Cubemap.BACK.Height;

                float[] CUBE_DATA = renderX.PrimitiveTypes.Cube();

                GCHandle ptr = GCHandle.Alloc(CUBE_DATA, GCHandleType.Pinned);

                ops.p = (float*)ptr.AddrOfPinnedObject();
                ops.Stride = 5;
                ops.FaceStride = 5 * 3;
                ops.ReadStride = 5;

              //  for (int i = 0; i < 12; i++) ops.FillSkybox(i);

                Parallel.For(0, 12, ops.FillSkybox);

            //    for (int i = 0; i < RenderHeight; i++) ops.SkyPass(i);


                Parallel.For(0, RenderHeight, ops.SkyPass);

                ptr.Free();

                Marshal.FreeHGlobal(SkyboxFaceCountData);
                Marshal.FreeHGlobal(SkyboxData);
                Marshal.FreeHGlobal(SkyboxPointerBuffer);
            }
        }

        internal void GetHWNDandDC(out IntPtr DC, out IntPtr HWND)
        {
            DC = TargetDC;
            HWND = LinkedHandle;
        }

        /// <summary>
        /// Exports the Drawing and Depth Buffer Pointers.
        /// WARNING: THESE POINTERS RESET WHEN THE RESOLUTION IS CHANGED.
        /// </summary>
        /// <param name="Draw">The Drawing Buffer Address</param>
        /// <param name="Depth">The Depth Buffer Address</param>
        internal void GetBuffers(out IntPtr Draw, out IntPtr Depth)
        {
            Draw = DrawingBuffer;
            Depth = DepthBuffer;
        }
    }

    public enum GLRenderMode
    {
        Triangle,
        TriangleFlat,
        TriangleFlatCPP,
        TriangleGouraud,
        Wireframe,
        WireframeDebug,
        Line
    }

    public class WarningException : Exception
    {
        public WarningException()
        {
        }

        public WarningException(string message): base(message + " - These warnings can be overridden with SetWarningExceptionOverride()")
        {
        }

        public WarningException(string message, Exception inner): base(message, inner)
        {
        }
    }

    public enum GLExtraAttributeData
    { 
        None = 0,
        XYZ_CameraSpace = 3,
        XY_ScreenSpace = 2,
        XYZ_XY_Both = 5,
        Z_Depth = 1
    }

    public enum GLRenderPath
    { 
        Forward,
        Deferred
    }

    public enum MemoryLocation
    { 
        Heap,
        Stack
    }

    public enum DuringLoad
    { 
        Flip,
        ConvertTo32bpp,
        CopyAlpha
    }

    public enum InterpolationMethod
    { 
        NearestNeighbour,
        Bilinear
    }

    public unsafe class GLBuffer : IDisposable
    {
        Thread T;
        internal int stride;
        internal float* ptr;
        internal int size = -1;
        internal int FaceCount;

        internal bool suspectedLinearray = false;

        internal object BufferLock;

        bool STORED_ON_STACK;
        IntPtr HEAP_ptr;

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~GLBuffer()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (STORED_ON_STACK && T != null && size != -1)
                        T.Abort();
                else if (size != -1)
                        Marshal.FreeHGlobal(HEAP_ptr);
                disposed = true;
            }
        }

        public GLBuffer(float[] SourceArray, int Stride, MemoryLocation StackOrHeap)
        {
            if (SourceArray == null || SourceArray.Length == 0)
                throw new Exception("Array Cannot Be Null Or Empty");
            stride = Stride;
            size = SourceArray.Length;
            FaceCount = (SourceArray.Length / Stride) / 3;


            if (SourceArray.Length % Stride != 0)
                throw new Exception("Stride value invalid");

            if (SourceArray.Length % (SourceArray.Length / (float)Stride) / 3f != 0)
                suspectedLinearray = true;

            if (Stride < 3)
                throw new Exception("Invalid Stride Value");

            if (StackOrHeap == MemoryLocation.Heap){
                STORED_ON_STACK = false;
                HEAP_ptr = Marshal.AllocHGlobal(SourceArray.Length * 4);
                ptr = (float*)HEAP_ptr;

                for (int i = 0; i < SourceArray.Length; i++){
                    ptr[i] = SourceArray[i];
                }
            }
            else { 
                STORED_ON_STACK = true;

                T = new Thread(() => ThreadMethod(out ptr, SourceArray.Length), SourceArray.Length * 4 + 1000);
                T.Start();

                while (ptr == null){
                    Thread.Sleep(10);
                }

                for (int i = 0; i < SourceArray.Length; i++){
                    ptr[i] = SourceArray[i];
                }
            }
        }

        public void Release()
        {
            if (size == -1 | T == null)
            {
                throw new Exception("There is nothing to unload!");
            }

            if (!STORED_ON_STACK)
            {
                Marshal.FreeHGlobal(HEAP_ptr);
            }
            else
            {
                try
                { T.Abort(); }
                catch { }
            }
        }

        internal int getifsuspectedline()
        {
            if ((size / 3f) % 2 != 0)
                throw new Exception("Invalid buffer size!");

            int s = ((size / 3) / 2);

            if (((s - 0) * (stride / 3) + 2 * 3) >= size)
                throw new Exception("Warning something is not right!!!");

            return s;
        }

        static unsafe void ThreadMethod(out float* ptr, int FloatSize)
        {
            float* stackFloat = stackalloc float[FloatSize];
            ptr = stackFloat;
            
            Thread.Sleep(Timeout.Infinite);
        }

        public unsafe IntPtr GetAddress()
        {
            return (IntPtr)ptr;
        }
    }

    public unsafe class Shader
    {
        public delegate void FragmentOperation(byte* BGR_Buffer, float* Attributes, int FaceIndex);
        public delegate void VertexOperation(float* XYZandAttributes_OUT, float* XYZandAttributes_IN, int FaceIndex);
        public delegate void FragmentPass(byte* BGR_Buffer, int posX, int posY);

        internal FragmentOperation ShaderFragment;
        internal VertexOperation ShaderVertex;
        internal FragmentPass ShaderPass;

        internal int vShdrAttr = -1;

        internal bool manualCamera = false;

        internal GLRenderMode sType;
        internal GLExtraAttributeData sLevel;
        internal bool copyAttributes = true;
        internal bool isPost = false;

        public Shader(VertexOperation VertexShader, FragmentOperation FragmentShader, GLRenderMode ShaderType, GLExtraAttributeData Data = GLExtraAttributeData.None)
        {
            if (Data != GLExtraAttributeData.None & ShaderType == GLRenderMode.TriangleFlat)
                throw new Exception("Triangle Flat does not support ExtraAttributeData. Attribute pointer will be null.");

            ShaderVertex = VertexShader;
            ShaderFragment = FragmentShader;
            sType = ShaderType;
            sLevel = Data;
        }

        public void SetOverrideAttributeCount(int StridePass)
        {
            if (StridePass > 30 || StridePass <= -2)
                throw new Exception("Stride Pass Must Be A Value Between -1 and 30");

            vShdrAttr = StridePass;
        }

        public void SetOverrideCameraTransform(bool value)
        {
            manualCamera = value;
        }

        public void SetOverrideAttributeCopy(bool value)
        {
            copyAttributes = !value;
        }

        public void SetScratchSpaceSize(int Value)
        {
            if (Value < 0)
                throw new Exception("Scratch space cannot be less than 0");

            if (Value > 20)
                throw new Exception("Scratch space cannot be bigger than 20");

            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates A Post Procesing Shader. Can be used for SSAO, Bloom, Screenspace reflections etc
        /// </summary>
        /// <param name="POST_PROCESS_PASS"></param>
        public Shader(FragmentPass POST_PROCESS_PASS)
        {
            isPost = true;
            ShaderPass = POST_PROCESS_PASS;
        }

        public string GetFragmentAttributePreview(GLBuffer TargetBuffer)
        {
            string str = "";

            if (vShdrAttr == -1)
                for (int i = 0; i < TargetBuffer.stride - 3; i++)
                {
                    str += "Attributes[" + i + "]: Vertex Data\n";
                }
            else if (vShdrAttr != 0)
                for (int i = 0; i < TargetBuffer.stride - 3; i++)
                {
                    str += "Attributes[" + i + "]: Custom Vertex Data\n";
                }
                

            int ATTRIBLVL = (int)sLevel;
            int v = TargetBuffer.stride - 3;


            if (ATTRIBLVL == 3)
            {
                str += "Attributes[" + (v + 0) + "]: Camera Space X Position\n";
                str += "Attributes[" + (v + 1) + "]: Camera Space Y Position\n";
                str += "Attributes[" + (v + 2) + "]: Camera Space Z Position\n";
            }
            else if (ATTRIBLVL == 5)
            {
                str += "Attributes[" + (v + 0) + "]: Camera Space X Position\n";
                str += "Attributes[" + (v + 1) + "]: Camera Space Y Position\n";
                str += "Attributes[" + (v + 2) + "]: Camera Space Z Position\n";
                str += "Attributes[" + (v + 3) + "]: Screen Space X Position\n";
                str += "Attributes[" + (v + 4) + "]: Screen Space Y Position\n";

            }
            else if (ATTRIBLVL == 2)
            {
                str += "Attributes[" + (v + 0) + "]: Screen Space X Position\n";
                str += "Attributes[" + (v + 1) + "]: Screen Space Y Position\n";
            }
            else if (ATTRIBLVL == 1)
            {
                str += "Attributes[" + (v + 0) + "]: Camera Space Z Position\n";
            }


            return str;
        }
    }

    public enum Projection
    { 
        Perspective,
        Orthographic
    }

    /// <summary>
    /// DEPRECATED, DO NOT USE!!!
    /// </summary>
    public struct RenderMatrix
    {
        internal bool isGlobalSize;
        internal bool isOrthographic;

        internal float gFOV;
        internal float hFOV;
        internal float vFOV;

        internal float gSize;
        internal float hSize;
        internal float vSize;

        internal float iValue;

        RenderMatrix(float vH, float vV, bool isOrtho)
        {
            if (isOrtho)
            {
                gSize = vV;
                hSize = vH;
                vSize = vV;
                gFOV = 0;
                hFOV = 0;
                vFOV = 0;
            }
            else
            {
                gFOV = vV;
                hFOV = vH;
                vFOV = vV;
                gSize = 0;
                hSize = 0;
                vSize = 0;
            }
            
            isOrthographic = isOrtho;
            isGlobalSize = false;
           

            if (isOrtho)
                iValue = 1;
            else iValue = 0;
        }

        RenderMatrix(float val, bool isOrtho)
        {
            if (isOrtho)
            {
                gSize = val;
                hSize = val;
                vSize = val;
                gFOV = 0;
                vFOV = 0;
                hFOV = 0;
            }
            else
            {
                gFOV = val;
                vFOV = val;
                hFOV = val;
                gSize = 0;
                vSize = 0;
                hSize = 0;
            }
            

            isOrthographic = isOrtho;
            isGlobalSize = true;
            
           

            if (isOrtho)
                iValue = 1;
            else iValue = 0;
        }

        public override string ToString()
        {
            return "gFOV: " + gFOV + ", vFOV: " + vFOV + ", hFOV: " + hFOV + ", gS: " + gSize + ", hS: " + hSize + ", vS: " + vSize + ", i: " + iValue;
        }

        public bool CheckForIllegalVariables()
        {
            if (!isOrthographic)
            {
                if (hFOV >= 180 | vFOV >= 180)
                    return true;

                if (vFOV < 0 | hFOV < 0)
                    return true;

                if (gFOV >= 180 | gFOV < 0)
                    return true;
            }
            else
            {
                if (hSize <= 0)
                    return true;

                if (vSize <= 0)
                    return true;

                if (gSize <= 0)
                    return true;
            }

            if (iValue > 1 | iValue < 0 & iValue != -1)
                return true;

            return false;
        }

        public static RenderMatrix CreateOrthographicMatrix(float GlobalSize)
        {
            return new RenderMatrix(GlobalSize, true);
        }

        public static RenderMatrix CreateOrthographicMatrix(float SizeWidth, float SizeHeight)
        {
            return new RenderMatrix(SizeWidth, SizeHeight, true);
        }

        public static RenderMatrix CreatePerspectiveMatrix(float GlobalFOVdeg)
        {
            if (GlobalFOVdeg >= 180)
                throw new ArgumentOutOfRangeException("GlobalFOVdeg", "Fov cannot be greater than 180!");

            if (GlobalFOVdeg < 0)
                throw new ArgumentOutOfRangeException("GlobalFOVdeg", "Fov cannot be less than zero!");

            return new RenderMatrix(GlobalFOVdeg, false);
        }

        public static RenderMatrix CreatePerspectiveMatrix(float VerticalFOV, float HorizontalFOV)
        {
            if (VerticalFOV >= 180 | HorizontalFOV >= 180)
                throw new ArgumentOutOfRangeException("Fov cannot be greater than 180!");

            if (VerticalFOV < 0 | HorizontalFOV < 0)
                throw new ArgumentOutOfRangeException("Fov cannot be less than zero!");


            return new RenderMatrix(VerticalFOV, HorizontalFOV, false);
        }

        public static RenderMatrix LerpOrthoPerspMatrix(RenderMatrix From, RenderMatrix To, float Value)
        {
            if (From.isGlobalSize & !To.isGlobalSize)
                throw new Exception("Both Matrices must be the same global/non global value!");

            if (!From.isGlobalSize & To.isGlobalSize)
                throw new Exception("Both Matrices must be the same global/non global value!");

            if (From.isOrthographic & To.isOrthographic)
                throw new Exception("Lerp is Only Allowed For Ortho and Perspective Matrices!");

            if (To.isOrthographic & From.isOrthographic)
                throw new Exception("Lerp is Only Allowed For Ortho and Perspective Matrices!");

            RenderMatrix o = new RenderMatrix();
            RenderMatrix p = new RenderMatrix();

            RenderMatrix m = new RenderMatrix();
            if (!From.isOrthographic){
                p = From;
                o = To;
            } else {
                o = From;
                p = To;
            }

            m.gFOV = p.gFOV;
            m.vFOV = p.vFOV;
            m.hFOV = p.hFOV;

            m.gSize = o.gSize;
            m.vSize = o.vSize;
            m.hSize = o.hSize;
            
            m.isGlobalSize = From.isGlobalSize;
            m.iValue = renderX.Lerp(From.iValue, To.iValue, Value);

            return m;
        }
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
        /// <summary>
        /// Creates a new Vector3
        /// </summary>
        /// <param name="posX">X Value</param>
        /// <param name="posY">Y Value</param>
        /// <param name="posZ">Z Value</param>
        public Vector3(float posX, float posY, float posZ)
        {
            x = posX;
            y = posY;
            z = posZ;
        }

        /// <summary>
        /// Calculates the 3 dimensional distance between point A and Point B
        /// </summary>
        /// <param name="From">Point A</param>
        /// <param name="To">Point B</param>
        /// <returns></returns>
        public static float Distance(Vector3 From, Vector3 To)
        {
            return (float)Math.Sqrt(Math.Pow(From.x - To.x, 2) + Math.Pow(From.y - To.y, 2) + Math.Pow(From.z - To.z, 2));
        }
        /// <summary>
        /// Adds two Vector3 together
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static Vector3 operator +(Vector3 A, Vector3 B)
        {
            return new Vector3(A.x + B.x, A.y + B.y, A.z + B.z);
        }
        /// <summary>
        /// Substacts Vector B from Vector A
        /// </summary>
        /// <param name="A">Vector A</param>
        /// <param name="B">Vector B</param>
        /// <returns></returns>
        public static Vector3 operator -(Vector3 A, Vector3 B)
        {
            return new Vector3(A.x - B.x, A.y - B.y, A.z - B.z);
        }

        public static Vector3 operator -(float A, Vector3 B)
        {
            return new Vector3(A - B.x, A - B.y, A - B.z);
        }

        public static Vector3 operator -(Vector3 A, float B)
        {
            return new Vector3(A.x - B, A.y - B, A.z - B);
        }

        public static bool Compare(Vector3 A, Vector3 B)
        {
            return (A.x == B.x && A.y == B.y && A.z == B.z);
        }

        public static Vector3 operator *(Vector3 A, Vector3 B)
        {
            return new Vector3(A.x * B.x, A.y * B.y, A.z * B.z);
        }

        public static Vector3 operator *(Vector3 A, float B)
        {
            return new Vector3(A.x * B, A.y * B, A.z * B);
        }

        public static Vector3 operator *(float A, Vector3 B)
        {
            return new Vector3(A * B.x, A * B.y, A * B.z);
        }

        public static bool operator >(Vector3 A, float B)
        {
            return A.x > B & A.y > B & A.z > B;
        }

        public static bool operator <(Vector3 A, float B)
        {
            return A.x < B & A.y < B & A.z < B;
        }

        public void Clamp01()
        {
            if (x < 0) x = 0;
            if (x > 1) x = 1;

            if (y < 0) y = 0;
            if (y > 1) y = 1;

            if (z < 0) z = 0;
            if (z > 1) z = 1;
        }

        public Vector3 Abs()
        {
            return new Vector3(Math.Abs(x), Math.Abs(y), Math.Abs(z));
        }

        public static Vector3 LerpAngle(Vector3 a, Vector3 b, float t)
        {
            return new Vector3(Lerp1D(a.x, b.x, t), Lerp1D(a.y, b.y, t), Lerp1D(a.z, b.z, t));
        }

        static float Lerp1D(float a, float b, float t)
        {
            float val = Repeat(b - a, 360);
            if (val > 180f)
                val-=360f;

            return a + val * Clamp01(t);
        }

        static float Repeat(float t, float length)
        {
            return Clamp(t - (float)Math.Floor(t / length) * length, 0f, length);
        }

        public Vector3 Repeat(float length)
        {
            float x1 = Clamp(x - (float)Math.Floor(x / length) * length, 0f, length);
            float y1 = Clamp(y - (float)Math.Floor(y / length) * length, 0f, length);
            float z1 = Clamp(z - (float)Math.Floor(z / length) * length, 0f, length);
            
            if (x1 > 180f) x1 -= 360f;
            if (y1 > 180f) y1 -= 360f;
            if (z1 > 180f) z1 -= 360f;

            return new Vector3(x1,y1,z1);
        }

        static float Clamp(float v, float min, float max)
        {
            if (v > max) return max;
            else if (v < min) return min;
            else return v;
        }

        static float Clamp01(float v)
        {
            if (v < 0) return 0;
            if (v > 1) return 1;
            else return v;
        }

        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            if (t > 1) t = 1;
            else if (t < 0) t = 0;
            return new Vector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }

        public static Vector3 operator -(Vector3 A)
        {
            return new Vector3(-A.x, -A.y, -A.z);
        }

        public static Vector3 operator /(Vector3 a, float d)
        {
            return new Vector3(a.x / d, a.y / d, a.z / d);
        }

        public static float Magnitude(Vector3 vector)
        {
            return (float)Math.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);
        }

        /// <summary>
        /// Returns a string in the format of "Vector3 X: " + X + ", Y: " + Y + ", Z: " + Z
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "X: " + x.ToString() + ", Y: " + y.ToString() + ", Z:" + z.ToString();
        }

        public static Vector3 Reflect(Vector3 inDirection, Vector3 inNormal)
        {
            return -2f * Dot(inNormal, inDirection) * inNormal + inDirection;
        }

        public static float Dot(Vector3 lhs, Vector3 rhs)
        {
            return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
        }

        public static Vector3 Normalize(Vector3 value)
        {
            float num = Magnitude(value);
            if (num > 1E-05f)
            {
                return value / num;
            }
            return new Vector3(0, 0, 0);
        }

        public static Vector3 Max(Vector3 lhs, Vector3 rhs)
        {
            return new Vector3(Math.Max(lhs.x, rhs.x), Math.Max(lhs.y, rhs.y), Math.Max(lhs.z, rhs.z));
        }

        public static Vector3 Sin(Vector3 AngleDegrees)
        {
            return new Vector3((float)Math.Sin(AngleDegrees.x * (Math.PI / 180f)), (float)Math.Sin(AngleDegrees.y * (Math.PI / 180f)), (float)Math.Sin(AngleDegrees.z * (Math.PI / 180f)));
        }

        public static Vector3 Cos(Vector3 AngleDegrees)
        {
            return new Vector3((float)Math.Cos(AngleDegrees.x * (Math.PI / 180f)), (float)Math.Cos(AngleDegrees.y * (Math.PI / 180f)), (float)Math.Cos(AngleDegrees.z * (Math.PI / 180f)));
        }
    }

    public struct Vector2
    {
        public float x;
        public float y;

        public Vector2(float posX, float posY)
        {
            x = posX;
            y = posY;
        }

        public Vector2(Vector2 oldVector2)
        {
            x = oldVector2.x;
            y = oldVector2.y;
        }

        public static float Distance(Vector2 From, Vector2 To)
        {
            return (float)Math.Sqrt(Math.Pow(From.x - To.x, 2) + Math.Pow(From.y - To.y, 2));
        }

        public override string ToString()
        {
            return "Vector2 X: " + x.ToString() + ", Y: " + y.ToString();
        }

    }


}
