using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace renderX2
{
    public partial class renderX
    {
        /// <summary>
        /// INTERNAL FUNCTION, DO NOT USE
        /// </summary>
        internal unsafe void zibltfunc(IntPtr RGB, int wSD, int sW, int sH, int oX, int oY, int wX, int wY, byte R, byte G, byte B)
        {
            byte* tptr = (byte*)DrawingBuffer;
            byte* sptr = (byte*)RGB;

            //     for (int h = oY; h < sH; h++)
            Parallel.For(oY, sH, h =>
            {
                for (int w = oX; w < sW; w++)
                {
                    if (sptr[h * wSD + w * 4 + 0] == B && sptr[h * wSD + w * 4 + 1] == G && sptr[h * wSD + w * 4 + 2] == R)
                        continue;

                    tptr[(h + wY) * RenderWidth * 4 + (w + wX) * 4 + 0] = sptr[h * wSD + w * 4 + 0];
                    tptr[(h + wY) * RenderWidth * 4 + (w + wX) * 4 + 1] = sptr[h * wSD + w * 4 + 1];
                    tptr[(h + wY) * RenderWidth * 4 + (w + wX) * 4 + 2] = sptr[h * wSD + w * 4 + 2];
                }
            });
        }

        /// <summary>
        /// Internal Function DO NOT USE
        /// </summary>
        /// <param name="RGB"></param>
        /// <param name="wSD"></param>
        /// <param name="sW"></param>
        /// <param name="sH"></param>
        /// <param name="oX"></param>
        /// <param name="oY"></param>
        /// <param name="wX"></param>
        /// <param name="wY"></param>
        /// <param name="hr"></param>
        /// <param name="B"></param>
        /// <param name="G"></param>
        /// <param name="R"></param>
        internal unsafe void gdiplusinteropcopy(IntPtr RGB, int wSD, int sW, int sH, int oX, int oY, int wX, int wY, int hr, byte B, byte G, byte R)
        {
            byte* tptr = (byte*)DrawingBuffer;
            byte* sptr = (byte*)RGB;

            Parallel.For(oY, sH, h =>
            {
                for (int w = oX; w < sW; w++)
                {
                    if (sptr[(hr - h) * wSD + w * 4 + 0] == B && sptr[(hr - h) * wSD + w * 4 + 1] == G && sptr[(hr - h) * wSD + w * 4 + 2] == R)
                        continue;

                    tptr[(h + wY) * RenderWidth * 4 + (w + wX) * 4 + 0] = sptr[(hr - h) * wSD + w * 4 + 0];
                    tptr[(h + wY) * RenderWidth * 4 + (w + wX) * 4 + 1] = sptr[(hr - h) * wSD + w * 4 + 1];
                    tptr[(h + wY) * RenderWidth * 4 + (w + wX) * 4 + 2] = sptr[(hr - h) * wSD + w * 4 + 2];
                }
            });
        }

        internal unsafe void gdipluscopy(IntPtr RGB, int wSD, int sW, int sH, int oX, int oY, int wX, int wY, int hr)
        {
            byte* tptr = (byte*)DrawingBuffer;
            byte* sptr = (byte*)RGB;

            Parallel.For(oY, sH, h =>
            {
                for (int w = oX; w < sW; w++)
                {
                    sptr[(hr - h) * wSD + w * 4 + 0] = tptr[(h + wY) * RenderWidth * 4 + (w + wX) * 4 + 0];
                    sptr[(hr - h) * wSD + w * 4 + 1] = tptr[(h + wY) * RenderWidth * 4 + (w + wX) * 4 + 1];
                    sptr[(hr - h) * wSD + w * 4 + 2] = tptr[(h + wY) * RenderWidth * 4 + (w + wX) * 4 + 2];
                }
            });
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

        public void BlitInto(Bitmap TargetBitmap, Rectangle SourceRectangle)
        {
            if (TargetBitmap == null)
                throw new Exception("TargetBitmap cannot be null!");

            if (Image.GetPixelFormatSize(TargetBitmap.PixelFormat) != 32)
                throw new Exception("For performance reasons, renderX only supports 32 bits per pixel bitmaps!");

            lock (ThreadLock)
            {
                
                
            }
        }

        public void BlitFrom(Bitmap SourceBitmap, Rectangle SourceRectangle, int TargetX, int TargetY)
        {
            if (SourceBitmap == null)
                throw new Exception("TargetBitmap cannot be null!");

            if (Image.GetPixelFormatSize(SourceBitmap.PixelFormat) != 32)
                throw new Exception("For performance reasons, renderX only supports 32 bits per pixel bitmaps!");

            lock (ThreadLock)
            {


            }
        }

        public unsafe void VignettePass()
        {
            lock (ThreadLock)
            {
                srcptr = (byte*)DrawingBuffer;
                pptr = (float*)DepthBuffer;

                hM = RenderHeight / 2f;
                vM = RenderWidth / 2f;
                mp = 1f / vM;


                Parallel.For(0, RenderHeight, vPass);
            }
            
        }

        unsafe byte* srcptr;
        unsafe float* pptr;
        float hM;
        float vM;
        float mp;

        unsafe void vPass(int i)
        {
            float X;
            float Y = (i / hM) - 1f;
            Y = (1f - 0.5f * Y * Y);

            byte* BGR = srcptr + RenderWidth * 4 * i;

            float m;

            for (int o = 0; o < RenderWidth; o++, BGR+=4)
            {
                X = (o * mp) - 1f;
                m = Y * (1f - 0.5f * X * X);

                BGR[0] = (byte)(BGR[0] * m);
                BGR[1] = (byte)(BGR[1] * m);
                BGR[2] = (byte)(BGR[2] * m);
            }
        }

        unsafe void testPass(int i)
        {
            byte* BGR = srcptr + RenderWidth * 4 * i;
            float* SRC = pptr + RenderWidth * i;
            for (int o = 0; o < RenderWidth; o++, BGR += 4, SRC++)
            {
                BGR[0] = (byte)(BGR[0] * pptr[0]);
                BGR[1] = (byte)(BGR[1] * pptr[0]);
                BGR[2] = (byte)(BGR[2] * pptr[0]);
            }
        }

        int _iClear;
        unsafe int* _iptr;
        unsafe byte* _bptr;


        unsafe void _2D_Clear(int i)
        {
            for (int o = 0; o < RenderWidth; o++)
                *(_iptr + i * RenderWidth + o) = _iClear;
        }

        unsafe int* _sptr;
        float _2DScaleX;
        float _2DScaleY;

        unsafe void _2DScale(int index)
        {
            int* tptr = _sptr + scaleWidth * index;
            int* sptr = _iptr + (int)(index * _2DScaleY * RenderWidth);

            for (int i = 0; i < scaleWidth; ++i)
            {
                tptr[i] = sptr[(int)(i * _2DScaleX)];
            }
        }

        unsafe void _2DScale2x2(int h)
        {
            int* optr = _sptr + h * scaleWidth;
          //  byte* rptr = _bptr + h * 4 * RenderWidth;

            byte* smplLWR;

            float Y = h * _2DScaleY;
            int Ys = (int)Y;
            float Yu = Y - Ys;
            float Yl = 1f - Yu;

            float R;
            float G;
            float B;

            for (int i = 0; i < scaleWidth; ++i)
            {
                float X = i * _2DScaleX;
                int Xs = (int)X;
                float Xu = X - Xs;
                float Xl = 1f - Xu;

                smplLWR = _bptr + Ys * RenderWidth + 4 * Xs;

              //  R = smplLWR[0] * Xl + smplLWR[4]

       
            }
        }
    }

    public enum BlitMethod
    { 
        TransparencyKey,
        Alpha,
        AlphaBlend,
        Overwrite
    }

    public enum CopyMethod
    {
        Memcpy,
        SplitLoop,
        SplitLoopDepthTest
    }
}
