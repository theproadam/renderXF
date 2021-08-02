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
    internal unsafe partial class GeometryPath
    {
        [DllImport("kernel32.dll")]
        static extern void RtlZeroMemory(IntPtr dst, int length);



        


        public float* p; //VERTEX_DATA INPUT
        public byte* bptr; //DRAWING_BUFFER
        public int* iptr; //DRAWING_BUFFER as int
        public float* dptr; //DEPTH_BUFFER

        public int* cptr; //CLICK_BUFFER
        public int* aptr; //ANYVALUE_BUFFER
        internal bool WriteClick = false;
        internal int CBuffervalue = 0;

        internal int ReadStride;
        internal int Stride;
        internal int FaceStride;

        renderX SourceGL;

        //Camera Rotations: c = cos, s = sin
        float cX;
        float cY;
        float cZ;

        float sX;
        float sY;
        float sZ;

        //Camera Position
        float coX;
        float coY;
        float coZ;

        internal float nearZ; //Near Z clipping plane
        internal float farZ; //Far Z clipping plane

        float tanHorz;
        float tanVert;

        float rw; //(renderWidth - 1)/2
        float rh; //(renderHeight - 1)/2

        float fw; //XYZtoXY vFov Adjuster
        float fh; //XYZtoXY hFov Adjuster

        float fwi; //-> 1f/fw
        float fhi; //-> 1f/fh

        float ox; //XYZtoXY wSize Adjuster
        float oy; //XYZtoXY hSize Adjuster

        float ow; //clipping Size W
        float oh; //clipping Size H

        bool cmatrix = false;
        float matrixlerpv = -1f; //0 when pers
        float matrixlerpo = -1f; //0 when ortho

        internal int renderWidth;
        internal int renderHeight;

        int sD; //Stride
        int wsD; //Vertical Stride

        internal float zoffset = 0; //Depth Test Offset
        float oValue = 0; //Ortho/Perspective Blend Offset

        //FACE CULLING
        internal bool FACE_CULL = false;
        internal bool CULL_FRONT = false;

        //Selected Shader Config
        internal bool HAS_VERTEX_SHADER = false;
        internal bool CAMERA_BYPASS = false;
        internal bool COPY_ATTRIB_MANUAL = false;
        internal int ATTRIBLVL = 0;
        internal bool attribdata = false;

        //Safe pointers to V/F Shaders
        internal Shader.VertexOperation VS;
        internal Shader.FragmentOperation FS;

        //Rendered triangle counter
        internal bool LOG_T_COUNT = false; //Triangle Log Count Toggle
        internal int T_COUNT = 0; //Triangle Log Count ref

        internal byte* CPptr; //ComparePixels
        internal float* CDptr; //CompareDepth
        internal int* CIptr; //CompareDepthInteger

        internal bool LinkedWFrame = false;

       // internal int lValue; //Late Wireframe Color
        internal byte dB, dR, dG; //Debug Wireframe Colors
        internal int diValue; //Integer Wireframe Color

        //AntiAliasing Data
        internal bool LINE_AA = false;
        internal bool FACE_AA = false;

        //Skybox Drawing
        internal float** sptr;
        internal int* bsptr;
        internal int** txptr;
        internal int rd;
        internal float* sdptr;
        internal int skyboxSize;

        //ThickLine Drawing
        internal bool ThickLine = false;
        internal int UpprThick;
        internal int LwrThick;

        //LineDrawing
        internal bool useLineShader = false;

        public GeometryPath(renderX GLSource, int rW, int rH)
        {
            SourceGL = GLSource;

            coX = 1f;
            coY = 1f;
            coZ = 1f;

            sX = 0;
            sY = 0;
            sZ = 0;

            cX = 0;
            cY = 0;
            cZ = 0;

            nearZ = 0.1f;
            farZ = 100000;

            renderWidth = rW;
            renderHeight = rH;

            float vMod = ((float)renderWidth - 1f) / renderWidth;
            float hMod = ((float)renderHeight - 1f) / renderHeight;

            rw = (renderWidth / 2f) * vMod;//todo swap with (renderWidth - 1f) / 2f
            rh = (renderHeight / 2f) * hMod;

            UpdateRM(90, 10, 0);

            sD = 4;
            wsD = 4 * renderWidth;
        }

        internal void UpdateViewportSize(int rW, int rH)
        {
            renderWidth = rW;
            renderHeight = rH;

            float radsFOV = (float)Math.PI / 2f;

            tanHorz = (float)Math.Tan(radsFOV / 2f) * ((float)renderHeight / (float)renderWidth);

            float vMod = ((float)renderWidth - 1f) / renderWidth;
            float hMod = ((float)renderHeight - 1f) / renderHeight;

            rw = (renderWidth / 2f) * vMod;
            rh = (renderHeight / 2f) * hMod;
            
            fw = rw * (float)Math.Tan((Math.PI / 2) - (radsFOV / 2f));
            fh = rh * ((float)renderWidth / (float)renderHeight) * (float)Math.Tan((Math.PI / 2f) - (radsFOV / 2f));

            sD = 4;
            wsD = 4 * renderWidth;
        }

        internal void UpdateCR(Vector3 SinV, Vector3 CosV)
        {
            sX = SinV.x;
            sY = SinV.y;
            sZ = SinV.z;

            coX = CosV.x;
            coY = CosV.y;
            coZ = CosV.z;
        }

        internal void UpdateCP(Vector3 CameraPosition)
        {
            cX = CameraPosition.x;
            cY = CameraPosition.y;
            cZ = CameraPosition.z;
        }


        internal void UpdateRM(float vFOV, float hFOV, float vSize, float hSize, float iValue)
        {
            float radsFOV = vFOV / 57.2958f;
            float radsFOVh = hFOV / 57.2958f;

            float fovCoefficient = (float)Math.Tan((Math.PI / 2) - (radsFOV / 2f));
            float hFovCoefficient = (float)Math.Tan((Math.PI / 2f) - (radsFOVh / 2f));

            tanVert = (float)Math.Tan((radsFOV / 2f)) * (1f - iValue);
            tanHorz = (float)Math.Tan((radsFOVh) / 2f) * (1f - iValue);

            fw = rw * fovCoefficient;
            fh = rh * hFovCoefficient;

            if (vSize != 0 & hSize != 0)
            {
                ox = rw / vSize;
                oy = (rh / hSize);
            }

            ow = vSize * iValue;
            oh = hSize * iValue;

            fw = rw * fovCoefficient;
            fh = rh * hFovCoefficient;

            fwi = 1f / fw;
            fhi = 1f / fh;

            ox = 1f / ox;
            oy = 1f / oy;

            matrixlerpv = iValue;
            matrixlerpo = 1f - iValue;

            cmatrix = matrixlerpv != 1;

            oValue = ow / ((float)Math.Tan(radsFOV / 2f) * matrixlerpo);
        }

        internal void UpdateRM(float FOV, float Size, float iValue)
        {
            float radsFOV = FOV / 57.2958f;

            float fovCoefficient = (float)Math.Tan((Math.PI / 2) - (radsFOV / 2f));
            float hFovCoefficient = ((float)renderWidth / (float)renderHeight) * (float)Math.Tan((Math.PI / 2f) - (radsFOV / 2f));

            tanVert = (float)Math.Tan((radsFOV / 2f)) * (1f - iValue);
            tanHorz = (float)Math.Tan((radsFOV) / 2f) * ((float)renderHeight / (float)renderWidth) * (1f - iValue);

            fw = rw * fovCoefficient;
            fh = rh * hFovCoefficient;

            if (Size != 0)
            {
                ox = rw / Size;
                oy = (rh / Size) * (float)renderWidth / (float)renderHeight;
            }

            ow = Size * iValue;
            oh = (Size / ((float)renderWidth / (float)renderHeight)) * iValue;

            fw = rw * fovCoefficient;
            fh = rh * hFovCoefficient;

            fwi = 1f / fw;
            fhi = 1f / fh;

            ox = 1f / ox;
            oy = 1f / oy;

            matrixlerpv = iValue;
            matrixlerpo = 1f - iValue;

            cmatrix = matrixlerpv != 1;

            oValue = ow / ((float)Math.Tan(radsFOV / 2f) * matrixlerpo);
        }

        #region Unrelated Stuff

        internal Vector3 GetCP()
        {
            return new Vector3(cX, cY, cZ);
        }

        internal Vector3 GetCR()
        {
            return new Vector3(sX, sY, sZ);
        }

        internal void FastCompare(int index)
        {
            for (int i = 0; i < renderWidth; i++)
            {
                if (-CDptr[index * renderWidth + i] < dptr[index * renderWidth + i])
                {
                    dptr[index * renderWidth + i] = CDptr[index * renderWidth + i];
                    iptr[index * renderWidth + i] = CIptr[index * renderWidth + i];
                }
            }
        }

        internal void FastCopy(int index)
        {
            int ir = index * renderWidth;
            for (int i = 0; i < renderWidth; ++i)
            {
                dptr[ir + i] = CDptr[ir + i];
                iptr[ir + i] = CIptr[ir + i];
            }
        }

        internal void GetCameraSpace(int X, int Y, float Z, out float caX, out float caY)
        {
            caX = ((X - rw) / fw) * Z;
            caY = ((Y - rh) / fh) * Z;
        }

        internal Vector3 TCS(Vector3 I)
        {
            float X = I.x - cX;
            float Y = I.y - cY;
            float Z = I.z - cZ;

            float fiX = (X) * coZ - (Z) * sZ;
            float fiZ = (Z) * coZ + (X) * sZ;
            float ndY = (Y) * coY + (fiZ) * sY;

            float Fx = (fiX) * coX - (ndY) * sX;
            float Fy = (ndY) * coX + (fiX) * sX;
            float Fz = (fiZ) * coY - (Y) * sY;

            return new Vector3(Fx, Fy, Fz);
        }

        internal Vector3 RCS(Vector3 I)
        {
            float X = I.x;
            float Y = I.y;
            float Z = I.z;

            float fiX = (X) * coZ - (Z) * sZ;
            float fiZ = (Z) * coZ + (X) * sZ;
            float ndY = (Y) * coY + (fiZ) * sY;

            float Fx = (fiX) * coX - (ndY) * sX;
            float Fy = (ndY) * coX + (fiX) * sX;
            float Fz = (fiZ) * coY - (Y) * sY;

            return new Vector3(Fx, Fy, Fz);
        }

        #endregion


        void LateWireFrame(float* DATA, int BUFFER_SIZE)
        {
            for (int i = 0; i < BUFFER_SIZE - 1; i++)
                DrawLineDEPTH(DATA + Stride * i, DATA + Stride * (i + 1));

            DrawLineDEPTH(DATA, DATA + Stride * (BUFFER_SIZE - 1));
        }

        unsafe bool ScanLine(int Line, float* TRIS_DATA, int TRIS_SIZE, float* Intersects)
        {
            int IC = 0;
            for (int i = 0; i < TRIS_SIZE; i++)
            {
                if (TRIS_DATA[i * 3 + 1] <= Line)
                {
                    if (i == 0 && TRIS_DATA[(TRIS_SIZE - 1) * 3 + 1] >= Line)
                    {
                        LIP(Intersects, IC, TRIS_DATA, TRIS_SIZE - 1, i, Line);
                        IC++;

                        if (IC >= 2) break;
                    }
                    else if (i > 0 && TRIS_DATA[(i - 1) * 3 + 1] >= Line)
                    {
                        LIP(Intersects, IC, TRIS_DATA, i - 1, i, Line);
                        IC++;

                        if (IC >= 2) break;
                    }
                }
                else if (TRIS_DATA[i * 3 + 1] > Line)
                {
                    if (i == 0 && TRIS_DATA[(TRIS_SIZE - 1) * 3 + 1] <= Line)
                    {
                        LIP(Intersects, IC, TRIS_DATA, TRIS_SIZE - 1, i, Line);
                        IC++;

                        if (IC >= 2) break;
                    }
                    else if (i > 0 && TRIS_DATA[(i - 1) * 3 + 1] <= Line)
                    {
                        LIP(Intersects, IC, TRIS_DATA, i - 1, i, Line);
                        IC++;

                        if (IC >= 2) break;
                    }
                }
            }


            if (IC == 2)
            {
                return true;
            }
            else return false;
            
        }

        unsafe bool ScanLineDATA(int Line, float* TRIS_DATA, int TRIS_SIZE, float* Intersects)
        {
            int IC = 0;
            for (int i = 0; i < TRIS_SIZE; i++)
            {
                if (TRIS_DATA[i * Stride + 1] <= Line)
                {
                    if (i == 0 && TRIS_DATA[(TRIS_SIZE - 1) * Stride + 1] >= Line)
                    {
                        LIPA(Intersects, IC, TRIS_DATA, TRIS_SIZE - 1, i, Line);
                        IC++;

                        if (IC >= 2) break;
                    }
                    else if (i > 0 && TRIS_DATA[(i - 1) * Stride + 1] >= Line)
                    {
                        LIPA(Intersects, IC, TRIS_DATA, i - 1, i, Line);
                        IC++;

                        if (IC >= 2) break;
                    }
                }
                else if (TRIS_DATA[i * Stride + 1] > Line)
                {
                    if (i == 0 && TRIS_DATA[(TRIS_SIZE - 1) * Stride + 1] <= Line)
                    {
                        LIPA(Intersects, IC, TRIS_DATA, TRIS_SIZE - 1, i, Line);
                        IC++;

                        if (IC >= 2) break;
                    }
                    else if (i > 0 && TRIS_DATA[(i - 1) * Stride + 1] <= Line)
                    {
                        LIPA(Intersects, IC, TRIS_DATA, i - 1, i, Line);
                        IC++;

                        if (IC >= 2) break;
                    }

                }
            }


            if (IC == 2)
            {
                return true;
            }
            else return false;
        }

        const float SCANLINE_EPSILON = 1E-3f;
        bool BiggerOrEqual(float Value, float Line)
        {
            if (Value > Line) return true;
            else if (Math.Abs(Value - Line) <= SCANLINE_EPSILON) return true;
            else return false;
        }

        bool SmallerOrEqual(float Value, float Line)
        {
            if (Value < Line) return true;
            else if (Math.Abs(Value - Line) <= SCANLINE_EPSILON) return true;
            else return false;
        }

        bool isApprox(float Value, float Line)
        {
            if (Math.Abs(Value - Line) <= SCANLINE_EPSILON) return true;
            else return false;
        }

        unsafe bool ScanLinePLUS_OLD(int Line, float* TRIS_DATA, int TRIS_SIZE, float* Intersects)
        {
            int IC = 0;
            for (int i = 0; i < TRIS_SIZE; i++)
            {
                if (TRIS_DATA[i * Stride + 1] <= Line)
                {
                    if (i == 0 && TRIS_DATA[(TRIS_SIZE - 1) * Stride + 1] >= Line)
                    {
                        LIPA_PLUS(Intersects, IC, TRIS_DATA, TRIS_SIZE - 1, i, Line);
                        IC++;

                        if (IC >= 2) break;
                    }
                    else if (i > 0 && TRIS_DATA[(i - 1) * Stride + 1] >= Line)
                    {
                        LIPA_PLUS(Intersects, IC, TRIS_DATA, i - 1, i, Line);
                        IC++;

                        if (IC >= 2) break;
                    }
                }
                else if (TRIS_DATA[i * Stride + 1] > Line)
                {
                    if (i == 0 && TRIS_DATA[(TRIS_SIZE - 1) * Stride + 1] <= Line)
                    {
                        LIPA_PLUS(Intersects, IC, TRIS_DATA, TRIS_SIZE - 1, i, Line);
                        IC++;

                        if (IC >= 2) break;
                    }
                    else if (i > 0 && TRIS_DATA[(i - 1) * Stride + 1] <= Line)
                    {
                        LIPA_PLUS(Intersects, IC, TRIS_DATA, i - 1, i, Line);
                        IC++;

                        if (IC >= 2) break;
                    }

                }
            }


            if (IC == 2)
                return true;
            else return false;
        }
      
        unsafe bool ScanLinePLUS(int Line, float* TRIS_DATA, int TRIS_SIZE, float* Intersects)
        {
            int IC = 0;
            for (int i = 0; i < TRIS_SIZE - 1; i++)
            {
                float y1 = TRIS_DATA[i * Stride + 1];
                float y2 = TRIS_DATA[(i + 1) * Stride + 1];

                if (y2 == y1 && Line == y2)
                {
                    LIPA_PLUS(Intersects, 0, TRIS_DATA, i, i + 1, Line);
                    LIPA_PLUS(Intersects, 1, TRIS_DATA, i + 1, i, Line);
                    return true;
                }

                if (y2 < y1)
                {
                    float t = y2;
                    y2 = y1;
                    y1 = t;
                }

                if (Line <= y2 && Line > y1)
                {
                    LIPA_PLUS(Intersects, IC, TRIS_DATA, i, i + 1, Line);
                    IC++;
                }

                if (IC >= 2) return true;
            }

            if (IC < 2)
            {
                float y1 = TRIS_DATA[0 * Stride + 1];
                float y2 = TRIS_DATA[(TRIS_SIZE - 1) * Stride + 1];

                if (y2 == y1 && Line == y2)
                {
                    LIPA_PLUS(Intersects, 0, TRIS_DATA, 0, (TRIS_SIZE - 1), Line);
                    LIPA_PLUS(Intersects, 1, TRIS_DATA, (TRIS_SIZE - 1), 0, Line);
                    return true;
                }

                if (y2 < y1)
                {
                    float t = y2;
                    y2 = y1;
                    y1 = t;
                }

                if (Line <= y2 && Line > y1)
                {
                    LIPA_PLUS(Intersects, IC, TRIS_DATA, 0, TRIS_SIZE - 1, Line);
                    IC++;
                }
            }


            if (IC == 2)
                return true;
            else return false;
        }
      
        #region ScreenSpaceInterpolation
        unsafe void LIP(float* XR, int I, float* V_DATA, int A, int B, int LinePos)
        {
            float X;
            float Z;

            A *= 3;
            B *= 3;

            if (V_DATA[A + 1] == LinePos)
            {
                XR[I * 2] = V_DATA[A];
                XR[I * 2 + 1] = V_DATA[A + 2];
                return;
            }

            if (V_DATA[B + 1] == LinePos)
            {
                XR[I * 2] = V_DATA[B];
                XR[I * 2 + 1] = V_DATA[B + 2];
                return;
            }

            if (V_DATA[A + 1] - V_DATA[B + 1] != 0)
            {
                float slope = (V_DATA[A] - V_DATA[B]) / (V_DATA[A + 1] - V_DATA[B + 1]);
                float b = -slope * V_DATA[A + 1] + V_DATA[A];
                X = slope * LinePos + b;

                float slopeZ = (V_DATA[A + 2] - V_DATA[B + 2]) / (V_DATA[A + 1] - V_DATA[B + 1]);
                float bZ = -slopeZ * V_DATA[A + 1] + V_DATA[A + 2];
                Z = slopeZ * LinePos + bZ;
            }
            else
            {
                throw new Exception("il fix this later");
            }

            XR[I * 2] = X;
            XR[I * 2 + 1] = Z;
        }

        unsafe void LIPA(float* XR, int I, float* V_DATA, int A, int B, int LinePos)
        {
            float X;
            float Z;

            A *= Stride;
            B *= Stride;

            if (V_DATA[A + 1] == LinePos)
            {
                XR[I] = V_DATA[A];
                XR[2 + I] = V_DATA[A + 2];

                for (int a = 3; a < Stride; a++)
                {
                    XR[((a - 3) * 2) + 4 + I] = V_DATA[A + a];
                }
                return;
            }

            if (V_DATA[B + 1] == LinePos)
            {
                XR[I] = V_DATA[B];
                XR[2 + I] = V_DATA[B + 2];

                for (int a = 3; a < Stride; a++)
                {
                    XR[(a - 3) * 2 + 4 + I] = V_DATA[B + a];
                }
                return;
            }

            if (V_DATA[A + 1] - V_DATA[B + 1] != 0)
            {
                float slope = (V_DATA[A] - V_DATA[B]) / (V_DATA[A + 1] - V_DATA[B + 1]);
                float b = -slope * V_DATA[A + 1] + V_DATA[A];
                X = slope * LinePos + b;

                float slopeZ = (V_DATA[A + 2] - V_DATA[B + 2]) / (V_DATA[A + 1] - V_DATA[B + 1]);
                float bZ = -slopeZ * V_DATA[A + 1] + V_DATA[A + 2];
                Z = slopeZ * LinePos + bZ;

            }
            else
            {
                throw new Exception("this shoudnt happen");
            }

            float ZDIFF = (1f / V_DATA[A + 2] - 1f / V_DATA[B + 2]);
            bool usingZ = ZDIFF != 0;

            if (ZDIFF != 0)
                usingZ = ZDIFF * ZDIFF >= 0.00001f;

            if (usingZ & matrixlerpv != 1)
                for (int a = 3; a < Stride; a++)
                {
                    float slopeA = (V_DATA[A + a] - V_DATA[B + a]) / ZDIFF;
                    float bA = -slopeA / V_DATA[A + 2] + V_DATA[A + a];
                    XR[((a - 3) * 2) + 4 + I] = slopeA / Z + bA;
                }
            else if (V_DATA[A + 1] - V_DATA[B + 1] != 0)
                for (int a = 3; a < Stride; a++)
                {
                    float slopeA = (V_DATA[A + a] - V_DATA[B + a]) / (V_DATA[A + 1] - V_DATA[B + 1]);
                    float bA = -slopeA * V_DATA[A + 1] + V_DATA[A + a];
                    XR[((a - 3) * 2) + 4 + I] = (slopeA * (float)LinePos + bA);


                }
            else throw new Exception("this shoudnt happen");

            XR[I] = X;
            XR[2 + I] = Z;
        }

        unsafe void LIPA_PLUS_OLD(float* XR, int I, float* V_DATA, int A, int B, int LinePos)
        {
            float X;
            float Z;

            A *= Stride;
            B *= Stride;

            if (V_DATA[A + 1] == LinePos)
            {
                XR[I * (Stride - 1)] = V_DATA[A];
                XR[I * (Stride - 1) + 1] = V_DATA[A + 2];

                for (int a = 3; a < Stride; a++)
                {
                    XR[I * (Stride - 1) + (a - 1)] = V_DATA[A + a];
                }
                return;
            }

            if (V_DATA[B + 1] == LinePos)
            {
                XR[I * (Stride - 1)] = V_DATA[B];
                XR[I * (Stride - 1) + 1] = V_DATA[B + 2];

                for (int a = 3; a < Stride; a++)
                {
                    XR[I * (Stride - 1) + (a - 1)] = V_DATA[B + a];
                }
                return;
            }

            if (V_DATA[A + 1] - V_DATA[B + 1] != 0)
            {
                float slope = (V_DATA[A] - V_DATA[B]) / (V_DATA[A + 1] - V_DATA[B + 1]);
                float b = -slope * V_DATA[A + 1] + V_DATA[A];
                X = slope * LinePos + b;

                float slopeZ = (V_DATA[A + 2] - V_DATA[B + 2]) / (V_DATA[A + 1] - V_DATA[B + 1]);
                float bZ = -slopeZ * V_DATA[A + 1] + V_DATA[A + 2];
                Z = slopeZ * LinePos + bZ;

            }
            else
            {
                float VALUE1 = V_DATA[A + 1];
                float VALUE2 = V_DATA[B + 1];

                throw new Exception("this shoudnt happen");
            }

            float ZDIFF = (1f / V_DATA[A + 2] - 1f / V_DATA[B + 2]);
            bool usingZ = ZDIFF != 0;

            if (ZDIFF != 0)
                usingZ = ZDIFF * ZDIFF >= 0.00001f;

            if (usingZ & matrixlerpv != 1)
                for (int a = 3; a < Stride; a++)
                {
                    float slopeA = (V_DATA[A + a] - V_DATA[B + a]) / ZDIFF;
                    float bA = -slopeA / V_DATA[A + 2] + V_DATA[A + a];
                    XR[I * (Stride - 1) + (a - 1)] = slopeA / Z + bA;
                }
            else if (V_DATA[A + 1] - V_DATA[B + 1] != 0)
                for (int a = 3; a < Stride; a++)
                {
                    float slopeA = (V_DATA[A + a] - V_DATA[B + a]) / (V_DATA[A + 1] - V_DATA[B + 1]);
                    float bA = -slopeA * V_DATA[A + 1] + V_DATA[A + a];
                    XR[I * (Stride - 1) + (a - 1)] = (slopeA * (float)LinePos + bA);


                }
            else throw new Exception("this shoudnt happen");

            XR[I * (Stride - 1) + 0] = X;
            XR[I * (Stride - 1) + 1] = Z;
        }


        unsafe void LIPA_PLUS(float* XR, int I, float* V_DATA, int A, int B, int LinePos)
        {
            float X;
            float Z;

            A *= Stride;
            B *= Stride;

            if (isApprox(V_DATA[A + 1], LinePos))
            {
                XR[I * (Stride - 1)] = V_DATA[A];
                XR[I * (Stride - 1) + 1] = V_DATA[A + 2];

                for (int a = 3; a < Stride; a++)
                {
                    XR[I * (Stride - 1) + (a - 1)] = V_DATA[A + a];
                }
                return;
            }

            if (isApprox(V_DATA[B + 1], LinePos))
            {
                XR[I * (Stride - 1)] = V_DATA[B];
                XR[I * (Stride - 1) + 1] = V_DATA[B + 2];

                for (int a = 3; a < Stride; a++)
                {
                    XR[I * (Stride - 1) + (a - 1)] = V_DATA[B + a];
                }
                return;
            }

            if (V_DATA[A + 1] - V_DATA[B + 1] != 0)
            {
                float slope = (V_DATA[A] - V_DATA[B]) / (V_DATA[A + 1] - V_DATA[B + 1]);
                float b = -slope * V_DATA[A + 1] + V_DATA[A];
                X = slope * LinePos + b;

                float slopeZ = (V_DATA[A + 2] - V_DATA[B + 2]) / (V_DATA[A + 1] - V_DATA[B + 1]);
                float bZ = -slopeZ * V_DATA[A + 1] + V_DATA[A + 2];
                Z = slopeZ * LinePos + bZ;

            }
            else
            {
                float VALUE1 = V_DATA[A + 1];
                float VALUE2 = V_DATA[B + 1];

                throw new Exception("this shoudnt happen");
            }

            float ZDIFF = (1f / V_DATA[A + 2] - 1f / V_DATA[B + 2]);
            bool usingZ = ZDIFF != 0;

            if (ZDIFF != 0)
                usingZ = ZDIFF * ZDIFF >= 0.00001f;

            if (usingZ & matrixlerpv != 1)
                for (int a = 3; a < Stride; a++)
                {
                    float slopeA = (V_DATA[A + a] - V_DATA[B + a]) / ZDIFF;
                    float bA = -slopeA / V_DATA[A + 2] + V_DATA[A + a];
                    XR[I * (Stride - 1) + (a - 1)] = slopeA / Z + bA;
                }
            else if (V_DATA[A + 1] - V_DATA[B + 1] != 0)
                for (int a = 3; a < Stride; a++)
                {
                    float slopeA = (V_DATA[A + a] - V_DATA[B + a]) / (V_DATA[A + 1] - V_DATA[B + 1]);
                    float bA = -slopeA * V_DATA[A + 1] + V_DATA[A + a];
                    XR[I * (Stride - 1) + (a - 1)] = (slopeA * (float)LinePos + bA);


                }
            else throw new Exception("this shoudnt happen");

            XR[I * (Stride - 1) + 0] = X;
            XR[I * (Stride - 1) + 1] = Z;
        }



        #endregion

        #region FindIntersectPoint
        unsafe void FIP(float* TA, int INDEX, float* VD, int A, int B, float LinePos)
        {
            float X;
            float Y;

            A *= 3;
            B *= 3;

            if (VD[A + 2] - VD[B + 2] != 0)
            {
                float slopeY = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
                float bY = -slopeY * VD[A + 2] + VD[A + 1];
                Y = slopeY * LinePos + bY;

                float slopeX = (VD[A + 0] - VD[B + 0]) / (VD[A + 2] - VD[B + 2]);
                float bX = -slopeX * VD[A + 2] + VD[A + 0];
                X = slopeX * LinePos + bX;
            }
            else
            {
                throw new Exception("Please give a stack trace, if this exception ever occur!");
            }


            TA[INDEX] = X;
            TA[INDEX + 1] = Y;
            TA[INDEX + 2] = LinePos;
         //   Debug.WriteLine("LINEPOS: " + LinePos + ", vsZ: " + nearZ);
        }

        unsafe void FIPA(float* TA, int INDEX, float* VD, int A, int B, float LinePos)
        {
            float X;
            float Y;
            int s = 3;

            A *= Stride;
            B *= Stride;

            if (VD[A + 2] - VD[B + 2] != 0)
            {
                float slopeY = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
                float bY = -slopeY * VD[A + 2] + VD[A + 1];
                Y = slopeY * LinePos + bY;

                float slopeX = (VD[A + 0] - VD[B + 0]) / (VD[A + 2] - VD[B + 2]);
                float bX = -slopeX * VD[A + 2] + VD[A + 0];
                X = slopeX * LinePos + bX;

                for (int i = s; i < Stride; i++)
                {
                    float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 2] - VD[B + 2]);
                    float bA = -slopeA * VD[A + 2] + VD[A + i];
                    TA[INDEX + i] = slopeA * LinePos + bA;
                }

            }
            else
            {
                throw new Exception("Please give a stack trace, if this exception ever occur!");
            }


            TA[INDEX] = X;
            TA[INDEX + 1] = Y;
            TA[INDEX + 2] = LinePos;
        }
        #endregion

        #region SlopeIntersectPoint
        unsafe void SIP(float* TA, int INDEX, float* VD, int A, int B, float TanSlope, bool isLeft = false)
        {
            float X;
            float Y;
            float Z;

            float oW = ow;
            if (isLeft)
                oW = -oW;

            A *= 3;
            B *= 3;

            float s1 = VD[A + 0] - VD[B + 0];
            float s2 = VD[A + 2] - VD[B + 2];
            s1 *= s1;
            s2 *= s2;
            //TODO clean this code up!

            if (s2 > s1)
            {
                float slope = (VD[A] - VD[B]) / (VD[A + 2] - VD[B + 2]);
                float b = -slope * VD[A + 2] + VD[A];

                float V = (b - oW) / (TanSlope - slope);

                X = V * slope + b;
                Z = V;
            }
            else
            {
                float slope = (VD[A + 2] - VD[B + 2]) / (VD[A] - VD[B]);
                float b = -slope * VD[A] + VD[A + 2];

                Z = (slope * oW + b) / (1f - slope * TanSlope);
                X = TanSlope * Z + oW;
            }


            //FLOATING POINT PRECESION ISSUES WITH X - Y != 0 BUT RATHER A VERY VERY SMALL NUMBER
            //SOLUTION INTERPOLATE BASED OF LARGEST NUMBER
            if (s1 > s2)
            {
                float slope = (VD[A + 1] - VD[B + 1]) / (VD[A] - VD[B]);
                float b = -slope * VD[A] + VD[A + 1];

                Y = slope * X + b;
            }
            else
            {
                float slope = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
                float b = -slope * VD[A + 2] + VD[A + 1];

                Y = slope * Z + b;
            }

            TA[INDEX] = X;
            TA[INDEX + 1] = Y;
            TA[INDEX + 2] = Z;
        }

        unsafe void SIPA(float* TA, int INDEX, float* VD, int A, int B, float TanSlope, bool isLeft = false)
        {
            float X;
            float Y;
            float Z;

            int s = 3;

            float oW = ow;
            if (isLeft)
                oW = -oW;

            A *= Stride;
            B *= Stride;

            float s1 = VD[A + 2] - VD[B + 2];
            float s2 = (VD[A] - VD[B]);
            s1 *= s1;
            s2 *= s2;

            if (s1 > s2)
            {
                float slope = (VD[A] - VD[B]) / (VD[A + 2] - VD[B + 2]);
                float b = -slope * VD[A + 2] + VD[A];

                Z = (b - oW) / (TanSlope - slope);
                X = Z * slope + b;

                for (int i = s; i < Stride; i++)
                {
                    float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 2] - VD[B + 2]);
                    float bA = -slopeA * VD[A + 2] + VD[A + i];
                    TA[INDEX + i] = slopeA * Z + bA;
                }
                s = Stride;
            }
            else
            {
                float slope = (VD[A + 2] - VD[B + 2]) / (VD[A] - VD[B]);
                float b = -slope * VD[A] + VD[A + 2];

                Z = (slope * oW + b) / (1f - slope * TanSlope);
                X = TanSlope * Z + oW;

                for (int i = s; i < Stride; i++)
                {
                    float slopeA = (VD[A + i] - VD[B + i]) / (VD[A] - VD[B]);
                    float bA = -slopeA * VD[A] + VD[A + i];
                    TA[INDEX + i] = slopeA * X + bA;
                }
                s = Stride;
            }

            //Floating point error solution:
            if (s1 > s2)
            {
                float slope = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
                float b = -slope * VD[A + 2] + VD[A + 1];

                Y = slope * Z + b;

                //Debug.WriteLine("me was here -> " + s);
                for (int i = s; i < Stride; i++)
                {
                    float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 2] - VD[B + 2]);
                    float bA = -slopeA * VD[A + 2] + VD[A + i];
                    TA[INDEX + i] = slopeA * Z + bA;
                }
                s = Stride;
            }
            else
            {
                float slope = (VD[A + 1] - VD[B + 1]) / (VD[A] - VD[B]);
                float b = -slope * VD[A] + VD[A + 1];

                Y = slope * X + b;

                for (int i = s; i < Stride; i++)
                {
                    float slopeA = (VD[A + i] - VD[B + i]) / (VD[A] - VD[B]);
                    float bA = -slopeA * VD[A] + VD[A + i];
                    TA[INDEX + i] = slopeA * X + bA;
                }
                s = Stride;
            }

            TA[INDEX] = X;
            TA[INDEX + 1] = Y;
            TA[INDEX + 2] = Z;
        }
        #endregion

        #region SlopeIntersectPointHorizontal
        unsafe void SIPH(float* TA, int INDEX, float* VD, int A, int B, float TanSlope, bool isLeft = false)
        {
            float X;
            float Y;
            float Z;

            A *= 3;
            B *= 3;

            float oH = oh;
            if (isLeft)
                oH = -oH;

            float s1 = VD[A + 1] - VD[B + 1];
            float s2 = VD[A + 2] - VD[B + 2];
            s1 *= s1;
            s2 *= s2;

            if (s2 > s1)
            {
                float slope = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
                float b = -slope * VD[A + 2] + VD[A + 1];

                float V = (b - oH) / (TanSlope - slope);

                Y = V * slope + b;
                Z = V;
            }
            else
            {
                float slope = (VD[A + 2] - VD[B + 2]) / (VD[A + 1] - VD[B + 1]);
                float b = -slope * VD[A + 1] + VD[A + 2];

                Z = (slope * oH + b) / (1f - slope * TanSlope);
                Y = TanSlope * Z + oH;
            }

            //Floating point precision errors require this code:
            if (s1 > s2)
            {
                float slope = (VD[A] - VD[B]) / (VD[A + 1] - VD[B + 1]);
                float b = -slope * VD[A + 1] + VD[A];

                X = slope * Y + b;
            }
            else 
            {
                float slope = (VD[A] - VD[B]) / (VD[A + 2] - VD[B + 2]);
                float b = -slope * VD[A + 2] + VD[A];

                X = slope * Z + b;
            }

            TA[INDEX] = X;
            TA[INDEX + 1] = Y;
            TA[INDEX + 2] = Z;
        }

        unsafe void SIPHA(float* TA, int INDEX, float* VD, int A, int B, float TanSlope, bool isLeft = false)
        {
            float X;
            float Y;
            float Z;

            int s = 3;

            A *= Stride;
            B *= Stride;

            float oH = oh;
            if (isLeft)
                oH = -oH;

            //compared to non stride siph, the s1 s2 are flipped; not sure why

            float s1 = VD[A + 2] - VD[B + 2];
            float s2 = VD[A + 1] - VD[B + 1];
            s1 *= s1;
            s2 *= s2;

            if (s2 > s1)
            {
                float slope = (VD[A + 2] - VD[B + 2]) / (VD[A + 1] - VD[B + 1]);
                float b = -slope * VD[A + 1] + VD[A + 2];

                Z = (slope * oH + b) / (1f - slope * TanSlope);
                Y = TanSlope * Z + oH;


                for (int i = s; i < Stride; i++)
                {
                    float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 1] - VD[B + 1]);
                    float bA = -slopeA * VD[A + 1] + VD[A + i];
                    TA[INDEX + i] = slopeA * Y + bA;
                }
                s = Stride;
            }
            else
            {
                float slope = (VD[A + 1] - VD[B + 1]) / (VD[A + 2] - VD[B + 2]);
                float b = -slope * VD[A + 2] + VD[A + 1];

                float V = (b - oH) / (TanSlope - slope);

                Y = V * slope + b;
                Z = V;

                for (int i = s; i < Stride; i++)
                {
                    float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 2] - VD[B + 2]);
                    float bA = -slopeA * VD[A + 2] + VD[A + i];
                    TA[INDEX + i] = slopeA * Z + bA;
                }
                s = Stride;
            }

            if (s1 > s2)
            {
                float slope = (VD[A] - VD[B]) / (VD[A + 2] - VD[B + 2]);
                float b = -slope * VD[A + 2] + VD[A];

                X = slope * Z + b;

                for (int i = s; i < Stride; i++)
                {
                    float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 2] - VD[B + 2]);
                    float bA = -slopeA * VD[A + 2] + VD[A + i];
                    TA[INDEX + i] = slopeA * Z + bA;
                }
                s = Stride;
            }
            else
            {
                float slope = (VD[A] - VD[B]) / (VD[A + 1] - VD[B + 1]);
                float b = -slope * VD[A + 1] + VD[A];

                X = slope * Y + b;

                for (int i = s; i < Stride; i++)
                {
                    float slopeA = (VD[A + i] - VD[B + i]) / (VD[A + 1] - VD[B + 1]);
                    float bA = -slopeA * VD[A + 1] + VD[A + i];
                    TA[INDEX + i] = slopeA * Y + bA;
                }
                s = Stride;
            }

            TA[INDEX] = X;
            TA[INDEX + 1] = Y;
            TA[INDEX + 2] = Z;
        }
        #endregion

        #region FaceCulling

        unsafe float BACKFACECULL3(float* VERTEX_DATA)
        {
            return ((VERTEX_DATA[3]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[7]) - (VERTEX_DATA[1])) - ((VERTEX_DATA[6]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[4]) - (VERTEX_DATA[1]));
        }

        unsafe float BACKFACECULLS(float* VERTEX_DATA)
        {
            return ((VERTEX_DATA[Stride]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[Stride * 2 + 1]) - (VERTEX_DATA[1])) - ((VERTEX_DATA[Stride * 2]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[Stride + 1]) - (VERTEX_DATA[1]));
        }

        #endregion

    }
}
