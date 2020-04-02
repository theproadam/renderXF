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

        bool ClickBufferEnabled = false;
        IntPtr ClickBuffer;
        IntPtr AnyBuffer;

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

            if (Shader.ShaderFragmentBasic != null)
            {
                ops.SBsc = Shader.ShaderFragmentBasic;
            }
            else if (Shader.ShaderFragmentAdvanced != null)
            {
                ops.SAdv = Shader.ShaderFragmentAdvanced;
            }
            else if (Shader.ShaderPass != null)
            { 
                
            }
            else if (Shader.sType == GLRenderMode.TriangleGouraud)
            { 
            
            }
            else
            {
                throw new Exception("There is no fragment shader attached to this shader!");
            }

            if (Shader.ShaderFragmentAdvanced != null & Shader.ShaderFragmentBasic != null)
                throw new Exception("Two Shaders Submitted!");
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

          //  if (SelectedShader.ShaderFragmentBasic == null & SelectedShader.sLevel == GLExtraAttributeData.None & !ExceptionOverride)
         //       throw new WarningException("No Attribute Or Screen Data, Please Use Basic Fragment Shader.");

            if (SelectedShader.manualCamera & SelectedShader.ShaderVertex == null)
                throw new Exception("A Vertex Shader Is Required for Manual Camera!");


            lock(ThreadLock){
                ops.p = SB.ptr;
                ops.bptr = (byte*)DrawingBuffer;
                ops.dptr = (float*)DepthBuffer;
                ops.iptr = (int*)DrawingBuffer;

                if (SelectedShader.sType == GLRenderMode.Triangle){
                    lock (ThreadLock)
                    {
                        if (SelectedShader.ShaderFragmentBasic != null & SelectedShader.ShaderFragmentAdvanced != null)
                            throw new Exception("Two separate Fragment Shaders submitted!");

                        if (SelectedShader.ShaderFragmentBasic == null & SelectedShader.ShaderFragmentAdvanced == null)
                            throw new Exception("No Fragment Shader detected!");

                        if (SB.stride != 3 & SelectedShader.ShaderFragmentBasic != null & SelectedShader.vShdrAttr == -1)
                            throw new Exception("The Basic Fragment Shader does not support Extra Attribute Data!");

                        if (SB.stride == 3 && SelectedShader.ShaderFragmentBasic == null && SelectedShader.sLevel == GLExtraAttributeData.None)
                            throw new WarningException("No Attribute Or Screen Data, Please Use Basic Fragment Shader");

                        if (SB.stride != 3 & SelectedShader.ShaderFragmentAdvanced == null & SelectedShader.sLevel == GLExtraAttributeData.None & SelectedShader.vShdrAttr == -1)
                            throw new Exception("No Attribute Or Screen Data, Please Use Basic Fragment Shader"); //FLAT LIGHTNING WARNING!

                        if (SelectedShader.manualCamera & SelectedShader.ShaderVertex == null)
                            throw new Exception("A Vertex Shader Is Required for Manual Camera!");

                        if (SelectedShader.copyAttributes & SelectedShader.vShdrAttr + 3 > SB.stride)
                            throw new Exception("Cannot have Automatic Copy For Different Amounts of Vertex Attributes!");

                        if (SelectedShader.vShdrAttr > 0 & SelectedShader.ShaderFragmentAdvanced == null)
                            throw new Exception("The Advanced Fragment Shader is Required for Attributes!");
                
                        if (SelectedShader.vShdrAttr >= 0 && SelectedShader.vShdrAttr < 30)
                            ops.Stride = 3 + SelectedShader.vShdrAttr;
                        else ops.Stride = SB.stride;
                    
                        ops.ReadStride = SB.stride;
                        ops.FaceStride = 3 * SB.stride;

                        ops.COPY_ATTRIB_MANUAL = !SelectedShader.copyAttributes;
                        ops.CAMERA_BYPASS = SelectedShader.manualCamera;
                        ops.HAS_VERTEX_SHADER = SelectedShader.ShaderVertex != null;
                        ops.VShdr = SelectedShader.ShaderVertex;
                    
                        ops.ATTRIBLVL = (int)SelectedShader.sLevel;
                        ops.attribdata = ops.ATTRIBLVL != 0;

                        if (ops.Stride == 3)
                            Parallel.For(0, SB.FaceCount, ops.FillFlat);
                        else
                            Parallel.For(0, SB.FaceCount, ops.FillFull);

                        if (RequestCopyAfterDraw)
                        {
                            CopyQuick(); RequestCopyAfterDraw = false;
                        }
                    }
                }
                else if (SelectedShader.sType == GLRenderMode.Wireframe)
                {
                    if (SelectedShader.sType == GLRenderMode.Wireframe && SelectedShader.sLevel != GLExtraAttributeData.None)
                        throw new Exception("Wireframe does not yet support extra attribute data. Sorry!");

                    if (SelectedShader.ShaderFragmentBasic != null & SelectedShader.ShaderFragmentAdvanced != null)
                        throw new Exception("Two separate Fragment Shaders submitted!");

                    if (SelectedShader.manualCamera & SelectedShader.ShaderVertex == null)
                        throw new Exception("A Vertex Shader Is Required for Manual Camera!");

                    if (SelectedShader.ShaderFragmentBasic == null & SelectedShader.ShaderFragmentAdvanced == null)
                        throw new Exception("No Fragment Shader detected!");

                    if ( SelectedShader.ShaderFragmentAdvanced == null)
                        throw new Exception("Wireframe currently only supports Advanced Shader! Sorry!");


                    //problem -> BasicAdvanced Shader

                    if (SelectedShader.vShdrAttr >= 0 && SelectedShader.vShdrAttr < 30)
                        ops.Stride = 3 + SelectedShader.vShdrAttr;
                    else ops.Stride = SB.stride;

                    ops.ReadStride = SB.stride;
                    ops.FaceStride = 3 * SB.stride;

                    ops.COPY_ATTRIB_MANUAL = !SelectedShader.copyAttributes;
                    ops.CAMERA_BYPASS = SelectedShader.manualCamera;
                    ops.HAS_VERTEX_SHADER = SelectedShader.ShaderVertex != null;
                    ops.VShdr = SelectedShader.ShaderVertex;

                    ops.ATTRIBLVL = (int)SelectedShader.sLevel;
                    ops.attribdata = ops.ATTRIBLVL != 0;

                    Parallel.For(0, SB.FaceCount, ops.WireFrame);

               //     for (int o = 0; o < SB.FaceCount; o++) ops.WireFrame(o);

                    if (RequestCopyAfterDraw)
                    {
                        CopyQuick(); RequestCopyAfterDraw = false;
                    }
                }
                else if (SelectedShader.sType == GLRenderMode.TriangleFlat)
                {
                    if (SelectedShader.ShaderFragmentBasic != null & SelectedShader.ShaderFragmentAdvanced != null)
                        throw new Exception("Two separate Fragment Shaders submitted!");

                    if (SelectedShader.ShaderFragmentBasic == null & SelectedShader.ShaderFragmentAdvanced == null)
                        throw new Exception("No Fragment Shader detected!");

                    if (SelectedShader.ShaderFragmentBasic == null)
                        throw new Exception("TriangleFlat only supports the basic fragment shader");

                    if (SelectedShader.vShdrAttr > 0 & !ExceptionOverride)
                        throw new WarningException("TriangleFlat does not does not load attributes");

                    if (SB.stride > 3 & SelectedShader.vShdrAttr != 0)
                        throw new WarningException("TriangleFlat does not does not load attributes");


                    ops.Stride = 3;
                    ops.ReadStride = SB.stride;
                    ops.FaceStride = 3 * SB.stride;

                    ops.COPY_ATTRIB_MANUAL = !SelectedShader.copyAttributes;
                    ops.CAMERA_BYPASS = SelectedShader.manualCamera;
                    ops.HAS_VERTEX_SHADER = SelectedShader.ShaderVertex != null;
                    ops.VShdr = SelectedShader.ShaderVertex;

                    ops.ATTRIBLVL = (int)SelectedShader.sLevel;
                    ops.attribdata = ops.ATTRIBLVL != 0;


                    Parallel.For(0, SB.FaceCount, ops.FillTrueFlat);
                    if (RequestCopyAfterDraw)
                    {
                        CopyQuick(); RequestCopyAfterDraw = false;
                    }
                }
                else if (SelectedShader.sType == GLRenderMode.TriangleGouraud)
                {
                    if (SelectedShader.ShaderFragmentBasic != null | SelectedShader.ShaderFragmentAdvanced != null & !ExceptionOverride)
                        throw new WarningException("Triangle Gouraud Will Ignore Any Fragment Shaders used!");

                    if (SelectedShader.ShaderVertex == null)
                        throw new Exception("Gouraud Shading requires a vertex shader. Please use 3 attributes for the BGR colors");

                //    if (SB.stride != 6 & !ExceptionOverride)
                //        throw new WarningException("It is recommended to only have normals as the attributes.");

                    if (SelectedShader.sLevel != GLExtraAttributeData.None & !ExceptionOverride)
                        throw new WarningException("Gouraud mode cannot provide Extra Attribute Data");

                    if (SelectedShader.copyAttributes)
                        throw new Exception("Attributes cannot be automatically copied for Gouraud mode");


                    ops.Stride = 6;
                    ops.ReadStride = SB.stride;
                    ops.FaceStride = 3 * SB.stride;

                    ops.COPY_ATTRIB_MANUAL = !SelectedShader.copyAttributes;
                    ops.CAMERA_BYPASS = SelectedShader.manualCamera;
                    ops.HAS_VERTEX_SHADER = SelectedShader.ShaderVertex != null;
                    ops.VShdr = SelectedShader.ShaderVertex;

                    ops.ATTRIBLVL = (int)SelectedShader.sLevel;
                    ops.attribdata = ops.ATTRIBLVL != 0;

                    Parallel.For(0, SB.FaceCount, ops.FillGouraud);
                    if (RequestCopyAfterDraw)
                    {
                        CopyQuick(); RequestCopyAfterDraw = false;
                    }
                }
                else if (SelectedShader.sType == GLRenderMode.Line)
                {
                    if (SelectedShader.ShaderFragmentBasic != null & SelectedShader.ShaderFragmentAdvanced != null)
                        throw new Exception("Two separate Fragment Shaders submitted!");

                    if (SelectedShader.ShaderFragmentBasic == null & SelectedShader.ShaderFragmentAdvanced == null)
                        throw new Exception("No Fragment Shader detected!");

                    if (SelectedShader.ShaderFragmentAdvanced == null)
                        throw new Exception("Line mode requires advanced shader, sorry!");


                    ops.Stride = SB.stride;
                    ops.FaceStride = 2 * SB.stride;
                    ops.p = SB.ptr;
                    ops.bptr = (byte*)DrawingBuffer;
                    ops.ReadStride = SB.stride;

                    ops.COPY_ATTRIB_MANUAL = !SelectedShader.copyAttributes;
                    ops.CAMERA_BYPASS = SelectedShader.manualCamera;
                    ops.HAS_VERTEX_SHADER = SelectedShader.ShaderVertex != null;
                    ops.VShdr = SelectedShader.ShaderVertex;

                    ops.ATTRIBLVL = (int)SelectedShader.sLevel;
                    ops.attribdata = ops.ATTRIBLVL != 0;

                    int b = SB.getifsuspectedline();
                    Parallel.For(0, b, ops.LineMode);

                    if (RequestCopyAfterDraw)
                    {
                        CopyQuick();
                        RequestCopyAfterDraw = false;
                    }
                }
                else if (SelectedShader.sType == GLRenderMode.WireframeDebug)
                {
                    ops.Stride = SB.stride;
                    ops.p = SB.ptr;
                    ops.bptr = (byte*)DrawingBuffer;

                    if (SB.stride != 3)
                        throw new Exception("Wireframe Debug Does Not Support != 3 Stride");

                    Parallel.For(0, SB.FaceCount, ops.WireFrameDebug);

                    if (RequestCopyAfterDraw)
                    {
                        CopyQuick();
                        RequestCopyAfterDraw = false;
                    }
                }
            }
        }

        public unsafe void Pass()
        {
            throw new Exception("Unfortunately Post-Processing Effects Have Been Temporarily Disabled Due The A Memory Corruption Issue!");

            if (SelectedShader == null)
                throw new Exception("No Shader Selected, Cannot Draw!");

            if (!SelectedShader.isPost)
                throw new Exception("This Function takes a post processing shader!");

            if (SelectedShader.ShaderPass == null)
                throw new Exception("No post processing delegate attached!");

            Shader.FragmentPass b = SelectedShader.ShaderPass;
            byte* addr = (byte*)DrawingBuffer;

            int W = SFB.Width;
            Parallel.For(0, RenderWidth * RenderHeight, i => {
                int X = i % W;
                int Y = i / W;

                b(addr + i * 4, X, Y);
            });
        }

        public void Blit()
        {
            if (miniGLMode)
                throw new Exception("MiniGL Mode Does Not Support Blit!");

            lock (ThreadLock)
            {
                SetDIBitsToDevice(TargetDC, 0, 0, (uint)ops.renderWidth, (uint)ops.renderHeight, 0, 0, 0, (uint)ops.renderHeight, DrawingBuffer, ref BINFO, 0);
            }
        }

        public unsafe void Clear(byte R, byte G, byte B)
        {
            byte _B = B;
            byte _G = G;
            byte _R = R;

            lock (ThreadLock)
            {
                byte* p = (byte*)DrawingBuffer;
                for (int i = (RenderWidth * RenderHeight) - 1; i >= 0; i--)
                {
                    *(p + i * 4) = B;
                    *(p + i * 4 + 1) = G;
                    *(p + i * 4 + 2) = R;
                }
            }
        }

        public unsafe void ClearFaster(byte R, byte G, byte B)
        {
             _B = B;
             _G = G;
             _R = R;

            rws = RenderWidth * 4;
            int pv = RenderHeight;
          
            lock (ThreadLock)
            {
                p = (byte*)DrawingBuffer;
                Parallel.For(0, pv, ClearLambda);
            }
        }

        byte _B, _G, _R;
        int rws;
        byte* p;
        void ClearLambda(int i)
        {
            for (int h = rws - 4; h >= 0; h -= 4)
            {
                int a = i * rws + h;
                *(p + a) = _B;
                *(p + a + 1) = _G;
                *(p + a + 2) = _R;
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
                RtlZeroMemory(TargetBuffer.GET_ADDR(), TargetBuffer.size);
            }
        }

        public void Line2D(int x1, int y1, int x2, int y2, byte R, byte G, byte B)
        {
            ops.DrawLine(x1, y1, x2, y2, R, G, B);
        }

        public void Line3D(Vector3 From, Vector3 To, byte R, byte G, byte B)
        {
            lock (ThreadLock)
            {
                int sD = ops.Stride;
                ops.bptr = (byte*)DrawingBuffer;
                ops.dptr = (float*)DepthBuffer;

                ops.Stride = 3;
                byte oB, oG, oR;

                oB = ops.lB; oG = ops.lG; oR = ops.lR;
                ops.lB = B; ops.lG = G; ops.lR = R; 

                ops.DrawLine3D(From, To);
                ops.Stride = sD;

                ops.lB = oB; ops.lG = oG; ops.lR = oR; 


            }
        }

        void CopyQuick()
        {
            memcpy(CD.RGB_ptr, DrawingBuffer, (UIntPtr)(RenderHeight * RenderWidth * 4));
            memcpy(CD.Z_ptr, DepthBuffer, (UIntPtr)(RenderHeight * RenderWidth * 4));
        }

        void CopyQuickReverse()
        {
            memcpy(DrawingBuffer, CD.RGB_ptr, (UIntPtr)(RenderHeight * RenderWidth * 4));
            memcpy(DepthBuffer, CD.Z_ptr, (UIntPtr)(RenderHeight * RenderWidth * 4));
        }

        void CopyQuickReverseDepthTest()
        {
            Parallel.For(0, RenderHeight, ops.FastCompare);
        }

        void CopyQuickSplitLoop()
        {
            Parallel.For(0, RenderHeight, ops.FastCopy);
        }

        internal void GetHWNDandDC(out IntPtr DC, out IntPtr HWND)
        {
            DC = TargetDC;
            HWND = LinkedHandle;
        }
    }

    public enum GLRenderMode
    {
        Triangle,
        TriangleFlat,
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

        public unsafe IntPtr GET_ADDR()
        {
            return (IntPtr)ptr;
        }
    }

    public unsafe class Shader
    {
        public delegate void ShaderFillB(byte* BGR_Buffer, int FaceIndex);
        public delegate void ShaderFillA(byte* BGR_Buffer, float* Attributes, int FaceIndex);
        public delegate void VertexOperation(float* XYZandAttributes_OUT, float* XYZandAttributes_IN, int FaceIndex);
        public delegate void FragmentPass(byte* BGR_Buffer, int posX, int posY);

        internal ShaderFillB ShaderFragmentBasic;
        internal ShaderFillA ShaderFragmentAdvanced;
        internal FragmentPass ShaderPass;

        internal VertexOperation ShaderVertex;

        internal int vShdrAttr = -1;

        internal bool manualCamera = false;

        internal GLRenderMode sType;
        internal GLExtraAttributeData sLevel;
        internal bool copyAttributes = true;
        internal bool isPost = false;

        public Shader(VertexOperation VertexShader, ShaderFillA FragmentShader, GLRenderMode ShaderType, GLExtraAttributeData Data = GLExtraAttributeData.None)
        {
            ShaderVertex = VertexShader;
            ShaderFragmentAdvanced = FragmentShader;
            sType = ShaderType;
            sLevel = Data;
        }

        public Shader(VertexOperation VertexShader, ShaderFillB FragmentShader, GLRenderMode ShaderType)
        {
            ShaderVertex = VertexShader;
            ShaderFragmentBasic = FragmentShader;
            sType = ShaderType;
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

        /// <summary>
        /// Creates A Post Procesing Shader. Can be used for SSAO, Bloom, Screenspace reflections etc
        /// </summary>
        /// <param name="POST_PROCESS_PASS"></param>
        public Shader(FragmentPass POST_PROCESS_PASS)
        {
            isPost = true;
            ShaderPass = POST_PROCESS_PASS;
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
