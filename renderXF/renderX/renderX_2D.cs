using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;

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

       


        int _iClear;
        unsafe int* _iptr;
        unsafe byte* _bptr;
        

        
        unsafe void _2D_Clear(int i)
        {
            for (int o = 0; o < RenderWidth; o++)
                *(_iptr + i * RenderWidth + o) = _iClear;
        }

        #region ScalingFunctions
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

        #endregion

        #region VignetteFunctions

        Shader.VignettePass VPass;
        unsafe float* _vptr;

        unsafe void _2DBuildVignetteBuffer()
        {
            _vptr = (float*)VignetteBuffer;

            if (VPass == null)
                throw new Exception("FATAL ERROR: No Vignette Buffer Method Found. Cannot Build New Vignette Buffer For New Resolution!");

            Parallel.For(0, RenderHeight, i => {
                for (int o = 0; o < RenderWidth; o++)
                {
                    VPass(_vptr + i * RenderWidth + o, o, i);
                }
            });
        }

        public unsafe void VignettePass()
        {
            lock (ThreadLock) {
                if (!usingVignette)
                    throw new Exception("Please Initialize a Vignette Buffer First!");

                _vptr = (float*)VignetteBuffer;
                _bptr = (byte*)DrawingBuffer;

                Parallel.For(0, RenderHeight, vPass);

              //  VIGNETTE_DATA(_bptr, _vptr, RenderWidth);
              //  VIGNETTE_TEST(RenderHeight);
            } 
        }

        unsafe void vPass(int yPos)
        {
            byte* tpr = _bptr + yPos * RenderWidth * 4;
            float* vptr = _vptr + yPos * RenderWidth;

            float v;
            for (int i = 0; i < RenderWidth; ++i, tpr+=4)
            {
                v = vptr[i];
                tpr[0] = (byte)(tpr[0] * v);
                tpr[1] = (byte)(tpr[1] * v);
                tpr[2] = (byte)(tpr[2] * v);
            }
        }

        #endregion

        #region ScreenSpaceShader

        Shader.FragmentPass PSFunction;

        unsafe void PostShaderPass(int i)
        {
            for (int o = 0; o < RenderWidth; o++)
            {
                PSFunction(_bptr + i * RenderWidth * 4 + o * 4, o, i);
            }
        }

        #endregion


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
