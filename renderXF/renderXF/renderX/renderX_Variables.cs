using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace renderX2
{
    public partial class renderX
    {
        List<GLFrameBuffer> LinkedFrameBuffers = new List<GLFrameBuffer>();
        List<GLCachedBuffer> LinkedCacheBuffers = new List<GLCachedBuffer>();

        public int RenderWidth
        {
            get { return ops.renderWidth; }
            set { SetViewportSize(value, ops.renderHeight); }
        }

        public int RenderHeight
        {
            get { return ops.renderHeight; }
            set { SetViewportSize(ops.renderWidth, value); }
        }

        public void ForceCameraRotation(Vector3 value)
        {
            lock (ThreadLock) { ops.UpdateCR(GetSin(value), GetCos(value)); }
        }

        public void ForceCameraPosition(Vector3 value)
        {
            lock (ThreadLock) { ops.UpdateCP(value); }
        }

        public Vector3 CameraPosition
        {
            get { return ops.GetCP(); }
            set { lock (ThreadLock) { ops.UpdateCP(value); } }
        }

        public float NearZ
        {
            get { return ops.nearZ; }
            set { lock (ThreadLock) { ops.nearZ = value; } }
        }

        public float FarZ
        {
            get { return ops.farZ; }
            set { lock (ThreadLock) { ops.farZ = value; } }
        }

        public void SetFaceCulling(bool value, bool FRONTTRUE_BACKFALSE)
        {
            lock (ThreadLock){ ops.FACE_CULL = value; ops.CULL_FRONT = FRONTTRUE_BACKFALSE;} 
        }
      
        public void SetViewport(IntPtr NewHandle)
        {
            if (miniGLMode)
                throw new Exception("MiniGL Mode does not accept Handles or Blit");

            lock (ThreadLock)
            {
                ReleaseDC(LinkedHandle, TargetDC);
                LinkedHandle = NewHandle;
                TargetDC = GetDC(NewHandle);
            }
        }

        public void SetViewportSize(int ViewportWidth, int ViewportHeight)
        {
            lock (ThreadLock)
            {
                DrawingBuffer = Marshal.ReAllocHGlobal(DrawingBuffer, (IntPtr)(ViewportWidth * ViewportHeight * 4));
                DepthBuffer = Marshal.ReAllocHGlobal(DepthBuffer, (IntPtr)(ViewportWidth * ViewportHeight * 4));

                BINFO.bmiHeader.biWidth = ViewportWidth;
                BINFO.bmiHeader.biHeight = ViewportHeight;

                ops.UpdateViewportSize(ViewportWidth, ViewportHeight);

                foreach (GLFrameBuffer f in LinkedFrameBuffers)
                    f.Resize(ViewportWidth, ViewportHeight);

                foreach (GLCachedBuffer f in LinkedCacheBuffers)
                    f.Resize(ViewportWidth, ViewportHeight);

                if (ClickBufferEnabled){
                    ClickBuffer = Marshal.ReAllocHGlobal(ClickBuffer, (IntPtr)(ViewportWidth * ViewportHeight * 4));
                    AnyBuffer = Marshal.ReAllocHGlobal(AnyBuffer, (IntPtr)(ViewportWidth * ViewportHeight * 4));
                    unsafe { 
                        ops.cptr = (int*)ClickBuffer;
                        ops.aptr = (int*)AnyBuffer;
                    }      
                }

                if (scaleBufferInitialized)
                {
                    BINFO.bmiHeader.biWidth = scaleWidth;
                    BINFO.bmiHeader.biHeight = scaleHeight;
                }

                if (usingVignette)
                {
                    VignetteBuffer = Marshal.ReAllocHGlobal(VignetteBuffer, (IntPtr)(ViewportWidth * ViewportHeight * 4));
                    _2DBuildVignetteBuffer();
                }
            }
        }

        public void SetWireFrameOFFSET(float Z_OFFSET)
        {
            lock (ThreadLock)
            {
                ops.zoffset = Z_OFFSET;
            }
        }

        public void ScreenToCameraSpace(int ScreenX, int ScreenY, float CameraZ, out float CameraX, out float CameraY)
        {
            ops.GetCameraSpace(ScreenX, ScreenY, CameraZ, out CameraX, out CameraY);
        }

        private Vector3 GetCos(Vector3 EulerAnglesDEG)
        {
            return new Vector3((float)Math.Cos(EulerAnglesDEG.x / 57.2958f), (float)Math.Cos(EulerAnglesDEG.y / 57.2958f), (float)Math.Cos(EulerAnglesDEG.z / 57.2958f));
        }

        private Vector3 GetSin(Vector3 EulerAnglesDEG)
        {
            return new Vector3((float)Math.Sin(EulerAnglesDEG.x / 57.2958f), (float)Math.Sin(EulerAnglesDEG.y / 57.2958f), (float)Math.Sin(EulerAnglesDEG.z / 57.2958f));
        }

        internal void BindFrameBuffer(GLFrameBuffer FB)
        {
            if (LinkedFrameBuffers.Contains(FB))
            {
                throw new Exception("Frame Buffer Already Exists!");
            }
            else
            {
                LinkedFrameBuffers.Add(FB);
            }
        }

        internal void UnBindFrameBuffer(GLFrameBuffer FB)
        {
            if (LinkedFrameBuffers.Contains(FB))
            {
                LinkedFrameBuffers.Remove(FB);
            }
        }

        internal void BindCachedBuffer(GLCachedBuffer CB)
        {
            if (LinkedCacheBuffers.Contains(CB))
            {
                throw new Exception("Cached Buffer Already Exists!");
            }
            else
            {
                LinkedCacheBuffers.Add(CB);
            }
        }

        internal void UnBindFrameBuffer(GLCachedBuffer CB)
        {
            if (LinkedCacheBuffers.Contains(CB))
            {
                LinkedCacheBuffers.Remove(CB);
            }
        }

        public Vector3 RotateToCameraSpace(Vector3 Input)
        {
            return ops.TCS(Input);
        }

        public Vector3 RotateCameraSpace(Vector3 Input)
        {
            return ops.RCS(Input);
        }

        public void LogTriangleCount(bool value)
        {
            lock (ThreadLock)
            {
                ops.LOG_T_COUNT = value;
                ops.T_COUNT = 0;
            }
        }

        public int GetTriangleCountAndReset()
        {
            lock (ThreadLock)
            {
                int c = ops.T_COUNT;
                ops.T_COUNT = 0;
                return c;
            }
        }

        public void SetLinkedWireframe(bool value, byte R, byte G, byte B)
        {
            lock (ThreadLock)
            {
                ops.LinkedWFrame = value;
                ops.lValue = (((((byte)R) << 8) | (byte)G) << 8) | (byte)B;
            }
        }

        public void CreateCopyOnDraw(GLCachedBuffer TargetBuffer)
        {
            if (TargetBuffer == null)
                throw new Exception("Cannot accept a null buffer!");

            

            lock (ThreadLock)
            {
                RequestCopyAfterDraw = true;
                CD = TargetBuffer;
            }
        }

        public unsafe void CopyFromCache(GLCachedBuffer SourceBuffer, CopyMethod CopyOptions)
        {
            if (SourceBuffer == null)
                throw new Exception("Cannot accept a null buffer!");

            lock (ThreadLock)
            {
                ops.bptr = (byte*)DrawingBuffer;
                ops.dptr = (float*)DepthBuffer;
                ops.iptr = (int*)DrawingBuffer;
                CD = SourceBuffer;

                ops.CDptr = (float*)CD.Z_ptr;
                ops.CPptr = (byte*)CD.RGB_ptr;
                ops.CIptr = (int*)CD.RGB_ptr;

                if (CopyOptions == CopyMethod.Memcpy) CopyQuickReverse();
                else if (CopyOptions == CopyMethod.SplitLoop) CopyQuickSplitLoop();
                else CopyQuickReverseDepthTest();
            }
        }

        public void SetWarningExceptionOverride(bool value)
        {
            lock (ThreadLock)
            {
                ExceptionOverride = value;
            }
        }

        public void SetDebugWireFrameColor(byte r, byte g, byte b)
        {
            lock (ThreadLock)
            {
                ops.dR = r;
                ops.dG = g;
                ops.dB = b;
                ops.diValue = ((((((byte)0 << 8) | (byte)r) << 8) | (byte)g) << 8) | (byte)b; //ARGB Values
            }
        }

        public void BlitInto(renderX Target, int TargetX, int TargetY, Color TransparencyKey)
        {
            lock (ThreadLock)
                lock (Target.ThreadLock)
                {
                    int oX = 0;
                    int oY = 0;

                    int sW = RenderWidth;
                    int sH = RenderHeight;

                    if (sW + TargetX > Target.RenderWidth) sW -= ((sW + TargetX) - Target.RenderWidth);
                    if (sH + TargetY > Target.RenderHeight) sH -= ((sH + TargetY) - Target.RenderHeight);

                    if (TargetX < 0) oX = -TargetX;
                    if (TargetY < 0) oY = -TargetY;


                    Target.zibltfunc(DrawingBuffer, RenderWidth * 4, sW, sH, oX, oY, TargetX, TargetY, TransparencyKey.R, TransparencyKey.G, TransparencyKey.B);
                }
        }

        public void SetClickBufferWrite(bool Value)
        {
            lock (ThreadLock)
            {
                ops.WriteClick = Value;
            }
        }

        public unsafe bool GetClickBufferData(int posX, int posY, out int FaceIndex, out int YourInt)
        {
            lock (ThreadLock)
            {
                if (!ClickBufferEnabled)
                    throw new Exception("Click Buffer Not Enabled!");

                if (posX >= RenderWidth | posY >= RenderHeight | posX < 0 | posY < 0)
                {
                    YourInt = -1;
                    FaceIndex = -1;
                    return false;
                }

                int v1 = ops.cptr[posY * RenderWidth + posX] - 1;
                YourInt =  ops.aptr[posY * RenderWidth + posX];

                FaceIndex = v1;

                if (v1 == -1) return false;
                else return true;
            }
        }

        public unsafe void InitializeClickBuffer()
        {
            lock (ThreadLock)
            {
                if (ClickBufferEnabled)
                    throw new Exception("A Click Buffer Already Exists!");

                ClickBufferEnabled = true;
                ClickBuffer = Marshal.AllocHGlobal(RenderWidth * RenderHeight * 4);
                AnyBuffer = Marshal.AllocHGlobal(RenderWidth * RenderHeight * 4);
                ops.cptr = (int*)ClickBuffer;
                ops.aptr = (int*)AnyBuffer;
            }
        }

        public void SetClickBufferInt(int Value)
        {
            lock (ThreadLock)
            {
                ops.CBuffervalue = Value;
            }
        }

        public unsafe void DeinitializeClickBuffer()
        {
            lock (ThreadLock)
            {
                if (!ClickBufferEnabled)
                    throw new Exception("A Click Buffer Needs To Exist In Order To Be Deinitialized!");

                ClickBufferEnabled = false;
                Marshal.FreeHGlobal(ClickBuffer);
                ops.cptr = null;
                ops.aptr = null;
            }
        }

        public void SetLineAntiAliasing(bool Value)
        {
            lock (ThreadLock)
            {
                ops.LINE_AA = Value;
            }
        }

        /// <summary>
        /// WARNING: Experimental, only for for TriangleFlat, on Y size
        /// </summary>
        /// <param name="Value"></param>
        public void SetFaceAntiAliasing(bool Value)
        {
            lock (ThreadLock)
            {
                ops.FACE_AA = Value;
            }
        }

        /// <summary>
        /// DEBUG FUNCTION; DO NOT USE.
        /// </summary>
        public void SetMatrixData(bool IsOrtho, float hFOV, float vFOV, float hOffset, float vOffset, int iValue = -1)
        { 
           // ops.UpdateCM()

            if (hFOV >= 180 | hFOV < 0)
                throw new Exception("Invalid hFOV");

            if (vFOV >= 180 | vFOV < 0)
                throw new Exception("Invalid vFOV");

            if (hOffset <= 0)
                throw new Exception("Invalid hSize");

            if (vOffset <= 0)
                throw new Exception("Invalid vSize");

        }

        public void SetMatrixData(float FOV, float Size, float iValue = 0)
        {
            if (FOV >= 180 | FOV < 0)
                throw new Exception("Invalid FOV");

            if (Size <= 0)
                throw new Exception("Invalid Size");

            if (iValue < 0 | iValue > 1)
                throw new Exception("Invalid iValue");

            ops.UpdateRM(FOV, Size, iValue);
        }

        public void SetMatrixData(float hFOV, float vFOV, float hSize, float vSize, float iValue = 0)
        {
            if (hFOV >= 180 | hFOV < 0) throw new Exception("Invalid hFOV");
            if (vFOV >= 180 | vFOV < 0) throw new Exception("Invalid vFOV");

            if (hSize <= 0) throw new Exception("Invalid hSize");
            if (vSize <= 0) throw new Exception("Invalid vSize");

            if (iValue < 0 | iValue > 1) throw new Exception("Invalid iValue");

            ops.UpdateRM(vFOV, hFOV, vSize, hSize, iValue);
        }

        public void SetLineThickness(int Size)
        {
            if (Size <= 0 | Size > 100)
                throw new Exception("Invalid Size!");

            lock (ThreadLock)
            {
                if (Size == 1) { 
                    ops.ThickLine = false;
                    return;
                }

                ops.UpprThick = (int)((Size - 1f) / 2f);
                ops.LwrThick = (int)(Size / 2f);
                ops.ThickLine = true;
            }
        }

        public void InitializeScaleBuffer()
        {
            lock (ThreadLock)
            {
                if (scaleBufferInitialized)
                    throw new Exception("A Scale Buffer Already Exists!");
            }
        }

        public unsafe void SetViewportScaling(int width, int height, InterpolationMethod iMethod)
        {
            throw new Exception("feature broken");

            if (width <= 2 | height <= 2)
                throw new Exception("Invalid Scaling Size");

            lock (ThreadLock)
            {
                scaleHeight = height;
                scaleWidth = width;

                scaleBufferInitialized = true;
                ScaleBuffer = Marshal.AllocHGlobal(width * height * 4);

                BINFO = new BITMAPINFO();
                BINFO.bmiHeader.biBitCount = 32; //BITS PER PIXEL
                BINFO.bmiHeader.biWidth = width;
                BINFO.bmiHeader.biHeight = height;
                BINFO.bmiHeader.biPlanes = 1;
                unsafe
                {
                    BINFO.bmiHeader.biSize = (uint)sizeof(BITMAPINFOHEADER);
                }

               // isScaling = !(width == RenderWidth & height == RenderHeight);
            }
        }

        public void InitializeVignetteBuffer(Shader.VignettePass VIGNETTE_PASS)
        {
            lock (ThreadLock)
            {
                if (usingVignette) 
                    throw new Exception("An Existing Vignette Buffer Already Exists!");

                VignetteBuffer = Marshal.AllocHGlobal(RenderWidth * RenderHeight * 4);

                VPass = VIGNETTE_PASS;

                _2DBuildVignetteBuffer();

                usingVignette = true;
            }
        }

        public void DeinitializeVignetteBuffer()
        {
            lock (ThreadLock)
            {
                if (!usingVignette)
                    throw new Exception("No Vignette Buffer Exists!");

                Marshal.FreeHGlobal(VignetteBuffer);

                VPass = null;
                usingVignette = false;
            }
        }

        public void RebuildVignetteBuffer()
        {
            lock (ThreadLock)
            {
                if (!usingVignette)
                    throw new Exception("No Vignette Buffer Exists!");

                _2DBuildVignetteBuffer();
            }
        }

        public void SwapVignetteMethod(Shader.VignettePass NEW_VIGNETTE_PASS)
        {
            lock (ThreadLock)
            {
                if (NEW_VIGNETTE_PASS == null)
                    throw new Exception("Null Method!");

                if (!usingVignette)
                    throw new Exception("Please Initialize a Buffer First!");

                VPass = NEW_VIGNETTE_PASS;
            }
        }
    }

    public unsafe class GLTexture
    {
        Thread T;
        internal int stride;
        internal byte* ptr;
        internal int size = -1;

        public int Width;
        public int Height;

        public int WidthStride;

        //internal object BufferLock;
        private bool disposed = false;

        bool STORED_ON_STACK;
        IntPtr HEAP_ptr;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~GLTexture()
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

        public GLTexture(string FilePath, MemoryLocation StackOrHeap, params DuringLoad[] LoadingOptions)
        {
            if (!System.IO.File.Exists(FilePath))
                throw new System.IO.FileNotFoundException();

            byte[] SourceArray = GetBytesFromFile(FilePath, out stride, out Width, out Height);
            size = SourceArray.Length;
            WidthStride = stride * Width;

            int readstride = stride;

            if (SourceArray.Length != Width * Height * stride)
                throw new Exception("uhhh this exception should not trigger");

            if (SourceArray.Length % stride != 0)
                throw new Exception("uhhh this exception should not trigger");

            int allocSize = SourceArray.Length;
            bool copyAlpha = LoadingOptions.Contains(DuringLoad.CopyAlpha);

            if (copyAlpha & stride == 3)
                throw new Exception("Cannot copy alpha for a 24bpp image");

            if (stride != 3 & stride != 4)
                throw new Exception("Only 24bpp or 32bpp images are supported!");

            if (LoadingOptions.Contains(DuringLoad.ConvertTo32bpp))
            { 
                allocSize = Width * Height * 4;
                stride = 4;
                WidthStride = Width * stride;
            }

            if (StackOrHeap == MemoryLocation.Heap)
            {
                STORED_ON_STACK = false;
                HEAP_ptr = Marshal.AllocHGlobal(allocSize);
                ptr = (byte*)HEAP_ptr;
            }
            else
            {
                STORED_ON_STACK = true;

                T = new Thread(() => ThreadMethod(out ptr, allocSize), allocSize + 1000);
                T.Start();

                while (ptr == null) Thread.Sleep(10);
            }

            
            if (LoadingOptions.Contains(DuringLoad.Flip))
                for (int w = 0; w < Width; w++)
                    for (int h = 0; h < Height; h++)
                    {
                        ptr[h * WidthStride + w * stride] = SourceArray[((Height - 1) - h) * WidthStride + w * stride];
                        ptr[h * WidthStride + w * stride + 1] = SourceArray[((Height - 1) - h) * WidthStride + w * stride + 1];
                        ptr[h * WidthStride + w * stride + 2] = SourceArray[((Height - 1) - h) * WidthStride + w * stride + 2];

                        if (copyAlpha) 
                            ptr[h * WidthStride + w * stride + 3] = SourceArray[((Height - 1) - h) * WidthStride + w * stride + 3];
                    }    
            else
                for (int i = 0; i < Width * Height; i++)
                {
                    ptr[i * stride] = SourceArray[i * readstride];
                    ptr[i * stride + 1] = SourceArray[i * readstride + 1];
                    ptr[i * stride + 2] = SourceArray[i * readstride + 2];

                    if (copyAlpha) ptr[i * stride + 3] = SourceArray[i * readstride + 3];
                }
            
        }

        public GLTexture(Bitmap Image, MemoryLocation StackOrHeap, params DuringLoad[] LoadingOptions)
        {
            if (Image == null)
                throw new ArgumentNullException();

            byte[] SourceArray = GetBytesFromBitmap(Image, out stride, out Width, out Height);
            size = SourceArray.Length;
            WidthStride = stride * Width;

            int readstride = stride;

            if (SourceArray.Length != Width * Height * stride)
                throw new Exception("uhhh this exception should not trigger");

            if (SourceArray.Length % stride != 0)
                throw new Exception("uhhh this exception should not trigger");

            int allocSize = SourceArray.Length;
            bool copyAlpha = LoadingOptions.Contains(DuringLoad.CopyAlpha);

            if (copyAlpha & stride == 3)
                throw new Exception("Cannot copy alpha for a 24bpp image");

            if (stride != 3 & stride != 4)
                throw new Exception("Only 24bpp or 32bpp images are supported!");

            if (LoadingOptions.Contains(DuringLoad.ConvertTo32bpp))
            {
                allocSize = Width * Height * 4;
                stride = 4;
                WidthStride = Width * stride;
            }

            if (StackOrHeap == MemoryLocation.Heap)
            {
                STORED_ON_STACK = false;
                HEAP_ptr = Marshal.AllocHGlobal(allocSize);
                ptr = (byte*)HEAP_ptr;
            }
            else
            {
                STORED_ON_STACK = true;

                T = new Thread(() => ThreadMethod(out ptr, allocSize), allocSize + 1000);
                T.Start();

                while (ptr == null) Thread.Sleep(10);
            }


            if (LoadingOptions.Contains(DuringLoad.Flip))
                for (int w = 0; w < Width; w++)
                    for (int h = 0; h < Height; h++)
                    {
                        ptr[h * WidthStride + w * stride] = SourceArray[((Height - 1) - h) * WidthStride + w * stride];
                        ptr[h * WidthStride + w * stride + 1] = SourceArray[((Height - 1) - h) * WidthStride + w * stride + 1];
                        ptr[h * WidthStride + w * stride + 2] = SourceArray[((Height - 1) - h) * WidthStride + w * stride + 2];

                        if (copyAlpha)
                            ptr[h * WidthStride + w * stride + 3] = SourceArray[((Height - 1) - h) * WidthStride + w * stride + 3];
                    }
            else
                for (int i = 0; i < Width * Height; i++)
                {
                    ptr[i * stride] = SourceArray[i * readstride];
                    ptr[i * stride + 1] = SourceArray[i * readstride + 1];
                    ptr[i * stride + 2] = SourceArray[i * readstride + 2];

                    if (copyAlpha) ptr[i * stride + 3] = SourceArray[i * readstride + 3];
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

        static unsafe void ThreadMethod(out byte* ptr, int ByteSize)
        {
            byte* stackByte = stackalloc byte[ByteSize];
            ptr = stackByte;

            Thread.Sleep(Timeout.Infinite);
        }

        public unsafe IntPtr GetAddress()
        {
            return (IntPtr)ptr;
        }

        byte[] GetBytesFromBitmap(Bitmap srcBitmap, out int BytesPerPixel, out int Width, out int Height)
        {
            Bitmap bmp = new Bitmap(srcBitmap);
            ushort bpp = (ushort)Image.GetPixelFormatSize(bmp.PixelFormat);

            int width = bmp.Width;
            int height = bmp.Height;

            Width = width;
            Height = height;
            BytesPerPixel = bpp / 8;

            System.Drawing.Imaging.BitmapData resultData = bmp.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);
            byte[] byteArray = new byte[Width * Height * BytesPerPixel];

            Marshal.Copy(resultData.Scan0, byteArray, 0, byteArray.Length);

            bmp.UnlockBits(resultData);
            bmp.Dispose();

            return byteArray;
        }

        byte[] GetBytesFromFile(string FilePath, out int BytesPerPixel, out int Width, out int Height)
        {
            Bitmap bmp = new Bitmap(FilePath);
            ushort bpp = (ushort)Image.GetPixelFormatSize(bmp.PixelFormat);

            int width = bmp.Width;
            int height = bmp.Height;

            Width = width;
            Height = height;
            BytesPerPixel = bpp / 8;

            System.Drawing.Imaging.BitmapData resultData = bmp.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.WriteOnly, bmp.PixelFormat);
            byte[] byteArray = new byte[Width * Height * BytesPerPixel];

            Marshal.Copy(resultData.Scan0, byteArray, 0, byteArray.Length);

            bmp.UnlockBits(resultData);
            bmp.Dispose();

            return byteArray;
        }
    }

    public unsafe class GLFrameBuffer
    {
        internal int stride;
        internal int size = -1;

        public int Width;
        public int Height;

        public int WidthStride;

        internal float renderScale;
        internal object BufferLock;
        private bool disposed = false;

        IntPtr HEAP_ptr;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~GLFrameBuffer()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                Marshal.FreeHGlobal(HEAP_ptr);
                disposed = true;
            }
        }

        public GLFrameBuffer(renderX SourceGL, int ByteStrideSize, float RenderScale = 1)
        {
            if (SourceGL == null)
                throw new Exception("null error");

            if (RenderScale == 0)
                throw new Exception("Invalid Renderscale");

            if (renderScale != 1)
                throw new Exception("Renderscale currentely disabled!");

            BufferLock = SourceGL.ThreadLock;

            lock (SourceGL.ThreadLock)
            {
                Width = (int)(SourceGL.RenderWidth * RenderScale);
                Height = (int)(SourceGL.RenderHeight * RenderScale);
                renderScale = RenderScale;

                size = Width * Height * ByteStrideSize;
                stride = ByteStrideSize;
                WidthStride = stride * Width;
                HEAP_ptr = Marshal.AllocHGlobal(size);

                SourceGL.BindFrameBuffer(this);
            }
        }

        public void Release()
        {
            if (size == -1)
            {
                throw new Exception("There is nothing to unload!");
            }

            Marshal.FreeHGlobal(HEAP_ptr);
        }

        public unsafe IntPtr GetAddress()
        {
            return HEAP_ptr;
        }

        /// <summary>
        /// This Is called automatically when the buffer is binded
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        internal void Resize(int w, int h)
        {
            HEAP_ptr = Marshal.ReAllocHGlobal(HEAP_ptr, (IntPtr)(w * h * stride));
            size = w * h * stride;
            Width = w;
            Height = h;
        }
    }

    public unsafe class GLCachedBuffer
    {
        internal object BufferLock;
        bool init = false;

        internal IntPtr RGB_ptr;
        internal IntPtr Z_ptr;

        ~GLCachedBuffer()
        {
            if (!init) return;

            Marshal.FreeHGlobal(RGB_ptr);
            Marshal.FreeHGlobal(Z_ptr);
        }

        public GLCachedBuffer(renderX SourceGL)
        {
            if (SourceGL == null)
                throw new Exception("null error");

            BufferLock = SourceGL.ThreadLock;

            lock (SourceGL.ThreadLock)
            {
                RGB_ptr = Marshal.AllocHGlobal(SourceGL.RenderWidth * SourceGL.RenderHeight * 4);
                Z_ptr = Marshal.AllocHGlobal(SourceGL.RenderWidth * SourceGL.RenderHeight * 4);

                SourceGL.BindCachedBuffer(this);
            }
        }

        public void Release()
        {
            if (!init)
            {
                throw new Exception("There is nothing to unload!");
            }

            Marshal.FreeHGlobal(RGB_ptr);
            Marshal.FreeHGlobal(Z_ptr);
            init = false;
        }

        public unsafe IntPtr GetAddress()
        {
            return RGB_ptr;
        }

        /// <summary>
        /// This Is called automatically when the buffer is binded
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        internal void Resize(int w, int h)
        {
            RGB_ptr = Marshal.ReAllocHGlobal(RGB_ptr, (IntPtr)(w * h * 4));
            Z_ptr = Marshal.ReAllocHGlobal(Z_ptr, (IntPtr)(w * h * 4));
        }
    }

    public unsafe class GLCubemap
    {
        public GLTexture FRONT;
        public GLTexture BACK;
        public GLTexture LEFT;
        public GLTexture RIGHT;
        public GLTexture TOP;
        public GLTexture BOTTOM;

        public GLCubemap(GLTexture Front, GLTexture Back, GLTexture Left, GLTexture Right, GLTexture Top, GLTexture Bottom)
        {
            throw new Exception("Not Yet Supported!");

            FRONT = Front;
            BACK =  Back;
            LEFT = Left;
            RIGHT = Right;
            TOP = Top;
            BOTTOM = Bottom;
        }

        public GLCubemap(string folderpath)
        { 
            if (!File.Exists(folderpath + @"\FRONT.jpg"))
                throw new Exception("Error!");

            if (!File.Exists(folderpath + @"\BACK.jpg"))
                throw new Exception("Error!");

            if (!File.Exists(folderpath + @"\TOP.jpg"))
                throw new Exception("Error!");

            if (!File.Exists(folderpath + @"\BOTTOM.jpg"))
                throw new Exception("Error!");

            if (!File.Exists(folderpath + @"\LEFT.jpg"))
                throw new Exception("Error!");

            if (!File.Exists(folderpath + @"\RIGHT.jpg"))
                throw new Exception("Error!");


            FRONT = new GLTexture(folderpath + @"\FRONT.jpg", MemoryLocation.Heap, DuringLoad.ConvertTo32bpp);
            BACK = new GLTexture(folderpath + @"\BACK.jpg", MemoryLocation.Heap, DuringLoad.ConvertTo32bpp);
            TOP = new GLTexture(folderpath + @"\TOP.jpg", MemoryLocation.Heap, DuringLoad.ConvertTo32bpp);
            BOTTOM = new GLTexture(folderpath + @"\BOTTOM.jpg", MemoryLocation.Heap, DuringLoad.ConvertTo32bpp);
            LEFT = new GLTexture(folderpath + @"\LEFT.jpg", MemoryLocation.Heap, DuringLoad.ConvertTo32bpp);
            RIGHT = new GLTexture(folderpath + @"\RIGHT.jpg", MemoryLocation.Heap, DuringLoad.ConvertTo32bpp);

        }

        public void Sample(Vector3 Direction)
        { 
            
        }

        internal bool CheckValid()
        {
            return (FRONT != null) & (BACK != null) & (LEFT != null) & (RIGHT != null) & (TOP != null) & (BOTTOM != null);
        }

        internal bool isValid()
        {
            bool h = (FRONT.Height == BACK.Height & BACK.Height == LEFT.Height & LEFT.Height == RIGHT.Height & RIGHT.Height == TOP.Height & TOP.Height == BOTTOM.Height);
            bool w = (FRONT.Width == BACK.Width & BACK.Width == LEFT.Width & LEFT.Width == RIGHT.Width & RIGHT.Width == TOP.Width & TOP.Width == BOTTOM.Width);
            bool s = (FRONT.stride == BACK.stride & BACK.stride == LEFT.stride & LEFT.stride == RIGHT.stride & RIGHT.stride == TOP.stride & TOP.stride == BOTTOM.stride);
            bool sxsy = FRONT.Height == FRONT.Width;

            return s & w & h & sxsy;
        }
    }
}
