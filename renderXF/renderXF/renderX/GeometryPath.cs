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
    internal unsafe class GeometryPath
    {
        [DllImport("kernel32.dll")]
        static extern void RtlZeroMemory(IntPtr dst, int length);

        public float* p; //VERTEX_DATA
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

        float cX;
        float cY;
        float cZ;

        float sX;
        float sY;
        float sZ;

        float coX;
        float coY;
        float coZ;

        internal float nearZ;
        internal float farZ;

        float tanHorz;
        float tanVert;

        float tanHorzInv;
        float tanVertInv;

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

        float radsFOV;
        int drawBufferLimit;

        internal bool FACE_CULL = false;
        internal bool CULL_FRONT = false;

        internal bool HAS_VERTEX_SHADER = false;
        internal bool CAMERA_BYPASS = false;
        internal bool COPY_ATTRIB_MANUAL = false;
        internal int ATTRIBLVL = 0;
        internal bool attribdata = false;

        internal Shader.ShaderFillB SBsc;
        internal Shader.ShaderFillA SAdv;

        internal bool LOG_T_COUNT = false; //Triangle Log Count Toggle
        internal int T_COUNT = 0; //Triangle Log Count ref

        internal Shader.VertexOperation VShdr;
        internal bool LinkedWFrame = false;

        internal byte* CPptr; //ComparePixels
        internal float* CDptr; //CompareDepth
        internal int* CIptr; //CompareDepthInteger

        internal byte lB, lR, lG; //Late Wireframe Colors
        internal byte dB, dR, dG; //Debug Wireframe Colors
        internal int diValue; //Integer Debug Wireframe Color

        internal bool LINE_AA = false;
        internal bool FACE_AA = false;

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
            farZ = 5000f;

            renderWidth = rW;
            renderHeight = rH;

            tanVert = (float)Math.Tan(Math.PI / 4f);
            tanHorz = (float)Math.Tan(Math.PI / 4f) * ((float)renderHeight / (float)renderWidth);

            tanHorzInv = 1f / tanHorz;
            tanVertInv = 1f / tanVert;

            float vMod = ((float)renderWidth - 1f) / renderWidth;
            float hMod = ((float)renderHeight - 1f) / renderHeight;

            rw = (renderWidth / 2f) * vMod;//todo swap with (renderWidth - 1f) / 2f
            rh = (renderHeight / 2f) * hMod;

            fw = rw * (float)Math.Tan((Math.PI / 2) - (Math.PI / 4f));
            fh = rh * ((float)renderWidth / (float)renderHeight) * (float)Math.Tan((Math.PI / 2f) - (Math.PI / 4f));


            sD = 4;
            wsD = 4 * renderWidth;
            drawBufferLimit = renderWidth * renderHeight;

          //  myobj = new object[rW * rH];
          //  for (int i = 0; i < myobj.Length; i++) myobj[i] = new object();
        }

        internal void UpdateViewportSize(int rW, int rH)
        {
            renderWidth = rW;
            renderHeight = rH;

            tanHorz = (float)Math.Tan(radsFOV / 2f) * ((float)renderHeight / (float)renderWidth);
            tanHorzInv = 1f / tanHorz;

            float vMod = ((float)renderWidth - 1f) / renderWidth;
            float hMod = ((float)renderHeight - 1f) / renderHeight;

            rw = (renderWidth / 2f) * vMod;
            rh = (renderHeight / 2f) * hMod;
            
            fw = rw * (float)Math.Tan((Math.PI / 2) - (radsFOV / 2f));
            fh = rh * ((float)renderWidth / (float)renderHeight) * (float)Math.Tan((Math.PI / 2f) - (radsFOV / 2f));

            sD = 4;
            wsD = 4 * renderWidth;
            drawBufferLimit = renderWidth * renderHeight;

          //  myobj = new object[rW * rH];
           // for (int i = 0; i < myobj.Length; i++) myobj[i] = new object();
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

        internal void UpdateCM(float FOV)
        {
            if (FOV >= 180 | FOV < 0)
                throw new Exception("Invalid FOV Submitted!");

           // renderOrthographic = false; 
            radsFOV = FOV / 57.2958f;

            float fovCoefficient = (float)Math.Tan((Math.PI / 2) - (radsFOV / 2f));
            float hFovCoefficient = ((float)renderWidth / (float)renderHeight) * (float)Math.Tan((Math.PI / 2f) - (radsFOV / 2f));

            tanVert = (float)Math.Tan(radsFOV / 2f);
            tanHorz = (float)Math.Tan((radsFOV) / 2f) * ((float)renderHeight / (float)renderWidth);

            tanHorzInv = 1f / tanHorz;
       //     tanVertInv = 1f / tanVert;

            tanVertInv = tanVert;

            fw = rw * fovCoefficient;
            fh = rh * hFovCoefficient;
        }

        internal void UpdateCM_O(float Size)
        {
            ox = rw / Size;
            oy = (rh / Size) * (float)renderWidth / (float)renderHeight;
        }

        internal void UpdateRM_O(float vSize, float hSize)
        {
            //renderOrthographic = true;

            ox = rw / vSize;
            oy = rh / hSize;
        }

        internal void UpdateRM_P(float vFOV, float hFOV)
        {
          //  renderOrthographic = false;

            radsFOV = vFOV / 57.2958f;
            float radsFOVh = hFOV / 57.2958f;
           // float aspct = (float)Math.Tan(vFOV / 2f) / (float)Math.Tan(hFOV / 2f);

            float fovCoefficient = (float)Math.Tan((Math.PI / 2) - (radsFOV / 2f));
            float hFovCoefficient = (float)Math.Tan((Math.PI / 2f) - (radsFOVh / 2f));

            tanVert = (float)Math.Tan(radsFOV / 2f);
            tanHorz = (float)Math.Tan(radsFOVh / 2f);

            tanHorzInv = 1f / tanHorz;
            tanVertInv = 1f / tanVert;

            fw = rw * fovCoefficient;
            fh = rh * hFovCoefficient;
        }

        internal void UpdateRM(float vFOV, float hFOV, float vSize, float hSize, float iValue)
        { 
            
        }

        internal void UpdateRM(float FOV, float Size, float iValue)
        {
            radsFOV = FOV / 57.2958f;

            float fovCoefficient = (float)Math.Tan((Math.PI / 2) - (radsFOV / 2f));
            float hFovCoefficient = ((float)renderWidth / (float)renderHeight) * (float)Math.Tan((Math.PI / 2f) - (radsFOV / 2f));

            tanVert = (float)Math.Tan((radsFOV / 2f)) * (1f - iValue);
            tanHorz = (float)Math.Tan((radsFOV) / 2f) * ((float)renderHeight / (float)renderWidth) * (1f - iValue);

            tanHorzInv = 1f / tanHorz;
            tanVertInv = 1f / tanVert;

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
        internal void znearzfar(float znear, float zfar)
        {
            if (znear <= 0)
                throw new Exception("znear must be bigger than zero!");

            nearZ = znear;
            farZ = zfar;
        }

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
                  //  bptr[index * wsD + i * 4 + 0] = CPptr[index * wsD + i * 4 + 0];
                  //  bptr[index * wsD + i * 4 + 1] = CPptr[index * wsD + i * 4 + 1];
                  //  bptr[index * wsD + i * 4 + 2] = CPptr[index * wsD + i * 4 + 2];
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
              //  bptr[index * wsD + i * 4 + 0] = CPptr[index * wsD + i * 4 + 0];
              //  bptr[index * wsD + i * 4 + 1] = CPptr[index * wsD + i * 4 + 1];
             //   bptr[index * wsD + i * 4 + 2] = CPptr[index * wsD + i * 4 + 2];
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

        public void WireFrameDebug(int index)
        {
            float* VERTEX_DATA = stackalloc float[9 + 3];
            int BUFFER_SIZE = 3;

            for (int b = 0; b < 3; b++)
            {
                float X = *(p + (index * 9 + b * 3)) - cX;
                float Y = *(p + (index * 9 + b * 3 + 1)) - cY;
                float Z = *(p + (index * 9 + b * 3 + 2)) - cZ;

                float fiX = (X) * coZ - (Z) * sZ;
                float fiZ = (Z) * coZ + (X) * sZ;
                float ndY = (Y) * coY + (fiZ) * sY;

                //Returns the newly rotated Vector
                *(VERTEX_DATA + b * 3 + 0) = (fiX) * coX - (ndY) * sX;
                *(VERTEX_DATA + b * 3 + 1) = (ndY) * coX + (fiX) * sX;
                *(VERTEX_DATA + b * 3 + 2) = (fiZ) * coY - (Y) * sY;
            }
            //TODO: Replace RTL_ZERO_MEMORY with a simple loop, it should be much faster
            
            bool* AP = stackalloc bool[BUFFER_SIZE + 12];

            #region NearPlaneCFG
            int v = 0;


            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] < nearZ)
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region NearPlane
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];

                int API = 0;

                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }

                BUFFER_SIZE = API / 3;
                VERTEX_DATA = strFLT;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }

            #endregion

            #region FarPlaneCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] > farZ)
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region FarPlane
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, farZ);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, i - 1, i, farZ);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, farZ);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, i - 1, i, farZ);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region RightFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] * tanVert + ow < VERTEX_DATA[i * 3])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;
            #endregion

            #region RightFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, i - 1, i, tanVert);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, i - 1, i, tanVert);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region LeftFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] * -tanVert - ow > VERTEX_DATA[i * 3])
                {
                    AP[i] = true;
                    v++;
                }

            }

            if (v == BUFFER_SIZE)
                return;
            #endregion

            #region LeftFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, true);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, true);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, true);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, true);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region TopFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] * tanHorz + oh < VERTEX_DATA[i * 3 + 1])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region TopFOV

            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);


            }

            #endregion

            #region BottomFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] * -tanHorz - oh > VERTEX_DATA[i * 3 + 1])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region BottomFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];

                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, true);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, true);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, true);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, true);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
            }
            #endregion

            if (LOG_T_COUNT) Interlocked.Increment(ref T_COUNT);

            if (matrixlerpv == 1)
            {
                float lX = rw + (VERTEX_DATA[0]) / ox;
                float lY = rh + (VERTEX_DATA[1]) / oy;

                float x = lX;
                float y = lY;

                for (int im = 1; im < BUFFER_SIZE; im++)
                {
                    float posX = rw + (VERTEX_DATA[im * 3 + 0]) / ox;
                    float posY = rh + (VERTEX_DATA[im * 3 + 1]) / oy;

                    if (LINE_AA) DrawLineAA(posX, posY, lX, lY);
                    else DrawLine(posX, posY, lX, lY);

                    lX = posX;
                    lY = posY;
                }

                if (LINE_AA) DrawLineAA(x, y, lX, lY);
                else DrawLine(x, y, lX, lY);
            }
            else if (matrixlerpv == 0)
            {
                float lX = rw + (VERTEX_DATA[0] / VERTEX_DATA[2]) * fw;
                float lY = rh + (VERTEX_DATA[1] / VERTEX_DATA[2]) * fh;

                float x = lX;
                float y = lY;

                for (int im = 1; im < BUFFER_SIZE; im++)
                {
                    float posX = rw + (VERTEX_DATA[im * 3 + 0] / VERTEX_DATA[im * 3 + 2]) * fw;
                    float posY = rh + (VERTEX_DATA[im * 3 + 1] / VERTEX_DATA[im * 3 + 2]) * fh;

                    if (LINE_AA) DrawLineAA(posX, posY, lX, lY);
                    else DrawLine(posX, posY, lX, lY);

                    lX = posX;
                    lY = posY;
                }

                if (LINE_AA) DrawLineAA(x, y, lX, lY);
                else DrawLine(x, y, lX, lY);
            }
            else
            {
               // float lX = rw + ((VERTEX_DATA[0] / VERTEX_DATA[2]) * fw * (1f - matrixlerpv) + VERTEX_DATA[0] * matrixlerpv * ox);
               // float lY = rh + ((VERTEX_DATA[1] / VERTEX_DATA[2]) * fh * (1f - matrixlerpv) + VERTEX_DATA[1] * matrixlerpv * oy); 

                float lX = rw + VERTEX_DATA[0] / ((VERTEX_DATA[2] * fwi - ox) * (1f - matrixlerpv) + ox);
                float lY = rh + VERTEX_DATA[1] / ((VERTEX_DATA[2] * fhi - oy) * (1f - matrixlerpv) + oy);

                float x = lX;
                float y = lY;

                for (int im = 1; im < BUFFER_SIZE; im++)
                {
                    float posX = rw + VERTEX_DATA[im * 3 + 0] / ((VERTEX_DATA[im * 3 + 2] * fwi - ox) * (1f - matrixlerpv) + ox);
                    float posY = rh + VERTEX_DATA[im * 3 + 1] / ((VERTEX_DATA[im * 3 + 2] * fhi - oy) * (1f - matrixlerpv) + oy);

                    if (LINE_AA) DrawLineAA(posX, posY, lX, lY);
                    else DrawLine(posX, posY, lX, lY);

                    lX = posX;
                    lY = posY;
                }

                if (LINE_AA) DrawLineAA(x, y, lX, lY);
                else DrawLine(x, y, lX, lY);
            }

           // Debug.WriteLine("fw: " + fw + ", fh: " + fh + ", ox: " + ox + ", oy: " + oy);
        }

        public void FillFlat(int index)
        {
            float* VERTEX_DATA = stackalloc float[9 + 3];
            int BUFFER_SIZE = 3;

            if (!CAMERA_BYPASS)
            {
                if (HAS_VERTEX_SHADER)
                    for (int b = 0; b < 3; b++)
                    {
                        VShdr((VERTEX_DATA + b * 3 + 0), (p + (index * FaceStride + b * ReadStride)), index);
                        float X = *(VERTEX_DATA + b * 3 + 0) - cX;
                        float Y = *(VERTEX_DATA + b * 3 + 1) - cY;
                        float Z = *(VERTEX_DATA + b * 3 + 2) - cZ;

                        float fiX = (X) * coZ - (Z) * sZ;
                        float fiZ = (Z) * coZ + (X) * sZ;
                        float ndY = (Y) * coY + (fiZ) * sY;

                        //Returns the newly rotated Vector
                        *(VERTEX_DATA + b * 3 + 0) = (fiX) * coX - (ndY) * sX;
                        *(VERTEX_DATA + b * 3 + 1) = (ndY) * coX + (fiX) * sX;
                        *(VERTEX_DATA + b * 3 + 2) = (fiZ) * coY - (Y) * sY;
                    }
                else
                    for (int b = 0; b < 3; b++)
                    {
                        float X = *(p + (index * FaceStride + b * ReadStride)) - cX;
                        float Y = *(p + (index * FaceStride + b * ReadStride + 1)) - cY;
                        float Z = *(p + (index * FaceStride + b * ReadStride + 2)) - cZ;

                        float fiX = (X) * coZ - (Z) * sZ;
                        float fiZ = (Z) * coZ + (X) * sZ;
                        float ndY = (Y) * coY + (fiZ) * sY;

                        //Returns the newly rotated Vector
                        *(VERTEX_DATA + b * 3 + 0) = (fiX) * coX - (ndY) * sX;
                        *(VERTEX_DATA + b * 3 + 1) = (ndY) * coX + (fiX) * sX;
                        *(VERTEX_DATA + b * 3 + 2) = (fiZ) * coY - (Y) * sY;
                    }
            }
            else
            {
                for (int b = 0; b < 3; b++)
                    VShdr((VERTEX_DATA + b * 3 + 0), (p + (index * FaceStride + b * ReadStride)), index);
            }


            //TODO: Replace RTL_ZERO_MEMORY with a simple loop, it should be much faster
            //WARNING: Max faces is actually 12, with 4 intersectspoints coming from one vertex MAX
            //UPDATE: max 5 intersect points rippp
            //Solution Increase AP Size to + 12 grrrrr
            //INFO: Perhaps create a GLBuffer which would allow to save the amount of stackallocs called, and just keep the data
            //SUGGESTION: Use separate arrays for min maxs, index, and size buffer?

            bool* AP = stackalloc bool[BUFFER_SIZE + 12];

            #region NearPlaneCFG
            int v = 0;


            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] < nearZ)
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region NearPlane
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];

                int API = 0;

                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }

                BUFFER_SIZE = API / 3;
                VERTEX_DATA = strFLT;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }

            #endregion

            #region RightFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] * tanVert + ow < VERTEX_DATA[i * 3])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;
            #endregion

            #region RightFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, i - 1, i, tanVert);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, i - 1, i, tanVert);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region LeftFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] * -tanVert - ow > VERTEX_DATA[i * 3])
                {
                    AP[i] = true;
                    v++;
                }

            }

            if (v == BUFFER_SIZE)
                return;
            #endregion

            #region LeftFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, true);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, true);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, true);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, true);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region TopFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] * tanHorz + oh < VERTEX_DATA[i * 3 + 1])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region TopFOV

            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);


            }

            #endregion

            #region BottomFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] * -tanHorz - oh > VERTEX_DATA[i * 3 + 1])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region BottomFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];

                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, true);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, true);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, true);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, true);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
            }
            #endregion

            int yMax = 0;
            int yMin = renderHeight;

            #region CameraSpaceToScreenSpace
            if (matrixlerpv == 0)
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * 3 + 0] = rw + (VERTEX_DATA[im * 3 + 0] / VERTEX_DATA[im * 3 + 2]) * fw;
                    VERTEX_DATA[im * 3 + 1] = rh + (VERTEX_DATA[im * 3 + 1] / VERTEX_DATA[im * 3 + 2]) * fh;
                    VERTEX_DATA[im * 3 + 2] = 1f / VERTEX_DATA[im * 3 + 2];

                    if (VERTEX_DATA[im * 3 + 1] > yMax) yMax = (int)VERTEX_DATA[im * 3 + 1];
                    if (VERTEX_DATA[im * 3 + 1] < yMin) yMin = (int)VERTEX_DATA[im * 3 + 1];
                }
            else if (matrixlerpv == 1)
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * 3 + 0] = rw + (VERTEX_DATA[im * 3 + 0] / ox);
                    VERTEX_DATA[im * 3 + 1] = rh + (VERTEX_DATA[im * 3 + 1] / oy);

                    if (VERTEX_DATA[im * 3 + 1] > yMax) yMax = (int)VERTEX_DATA[im * 3 + 1];
                    if (VERTEX_DATA[im * 3 + 1] < yMin) yMin = (int)VERTEX_DATA[im * 3 + 1];
                }
            else
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * 3 + 0] = rw + VERTEX_DATA[im * 3 + 0] / ((VERTEX_DATA[im * 3 + 2] * fwi - ox) * (1f - matrixlerpv) + ox);
                    VERTEX_DATA[im * 3 + 1] = rh + VERTEX_DATA[im * 3 + 1] / ((VERTEX_DATA[im * 3 + 2] * fhi - oy) * (1f - matrixlerpv) + oy);

                    VERTEX_DATA[im * 3 + 2] = 1f / (VERTEX_DATA[im * 3 + 2] + oValue);

                    if (VERTEX_DATA[im * 3 + 1] > yMax) yMax = (int)VERTEX_DATA[im * 3 + 1];
                    if (VERTEX_DATA[im * 3 + 1] < yMin) yMin = (int)VERTEX_DATA[im * 3 + 1];
                }

            #endregion

            #region FaceCulling
            if (FACE_CULL)
            {
                float A = BACKFACECULL3(VERTEX_DATA);
                if (CULL_FRONT && A > 0) return;
                else if (!CULL_FRONT && A < 0) return;
            }
            #endregion

            if (LOG_T_COUNT) Interlocked.Increment(ref T_COUNT);

           // Parallel.For(yMin, yMax + 1, i =>{
            float* Intersects = stackalloc float[4 + ATTRIBLVL];
            float s;
            float z;

            for (int i = yMin; i <= yMax; i++){
                if (ScanLine(i, VERTEX_DATA, BUFFER_SIZE, Intersects))
                {
                    if (Intersects[0] > Intersects[1])
                    {
                        float a = Intersects[1];
                        Intersects[1] = Intersects[0];
                        Intersects[0] = a;

                        a = Intersects[3];
                        Intersects[3] = Intersects[2];
                        Intersects[2] = a;
                    }

                    float slopeZ = (Intersects[2] - Intersects[3]) / (Intersects[0] - Intersects[1]);
                    float bZ = -slopeZ * (int)Intersects[0] + Intersects[2];

                    if ((int)Intersects[1] >= renderWidth) Intersects[1] = renderWidth - 1;
                    if ((int)Intersects[0] < 0) Intersects[0] = 0;

                  //  bool test = Intersects[2] - Intersects[3] != 0;

                    if (attribdata)
                        for (int o = (int)Intersects[0]; o <= (int)Intersects[1]; o++)
                        {
                            if (cmatrix) z = 1f / ((slopeZ * (float)o + bZ) - oValue);
                            else z = (slopeZ * (float)o + bZ);

                            s = farZ - z;

                            if (dptr[renderWidth * i + o] > s) continue;
                            dptr[renderWidth * i + o] = s;

                            if (ATTRIBLVL == 3)
                            {
                                Intersects[4] = ((o - rw) / fw) * z;
                                Intersects[5] = ((i - rh) / fh) * z;
                                Intersects[6] = z;
                            }
                            else if (ATTRIBLVL == 5)
                            {
                                Intersects[4] = ((o - rw) / fw) * z;
                                Intersects[5] = ((i - rh) / fh) * z;
                                Intersects[6] = z;
                                Intersects[7] = o;
                                Intersects[8] = i;
                            }
                            else if (ATTRIBLVL == 2)
                            {
                                Intersects[4] = o;
                                Intersects[5] = i;
                            }
                            else if (ATTRIBLVL == 1)
                            {
                                Intersects[4] = z;
                            }


                            SAdv((bptr + (i * wsD + (o * sD) + 0)), Intersects + 4, index);
                        }
                    else
                        for (int o = (int)Intersects[0]; o <= (int)Intersects[1]; o++)
                        {
                            if (cmatrix) s = farZ - 1f / ((slopeZ * (float)o + bZ) - oValue);
                            else s = farZ - (slopeZ * (float)o + bZ);

                            if (dptr[renderWidth * i + o] > s) continue;
                            //really no point in using this code here

                        //    lock(myobj[i * renderWidth + o]){
                            //    if (dptr[renderWidth * i + o] > s) continue;
                                dptr[renderWidth * i + o] = s;
                                SBsc((bptr + (i * wsD + (o * sD) + 0)), index);
                          //  }
                            
                        }
                }
            }//);

            if (LinkedWFrame) LateWireFrame(VERTEX_DATA, BUFFER_SIZE);
        }

        public void FillTrueFlat(int index)
        {
            float* VERTEX_DATA = stackalloc float[9 + 3];
            int BUFFER_SIZE = 3;

            if (!CAMERA_BYPASS)
            {
                if (HAS_VERTEX_SHADER)
                    for (int b = 0; b < 3; b++)
                    {
                        VShdr((VERTEX_DATA + b * 3 + 0), (p + (index * FaceStride + b * ReadStride)), index);
                        float X = *(VERTEX_DATA + b * 3 + 0) - cX;
                        float Y = *(VERTEX_DATA + b * 3 + 1) - cY;
                        float Z = *(VERTEX_DATA + b * 3 + 2) - cZ;

                        float fiX = (X) * coZ - (Z) * sZ;
                        float fiZ = (Z) * coZ + (X) * sZ;
                        float ndY = (Y) * coY + (fiZ) * sY;

                        //Returns the newly rotated Vector
                        *(VERTEX_DATA + b * 3 + 0) = (fiX) * coX - (ndY) * sX;
                        *(VERTEX_DATA + b * 3 + 1) = (ndY) * coX + (fiX) * sX;
                        *(VERTEX_DATA + b * 3 + 2) = (fiZ) * coY - (Y) * sY;
                    }
                else
                    for (int b = 0; b < 3; b++)
                    {
                        float X = *(p + (index * FaceStride + b * ReadStride)) - cX;
                        float Y = *(p + (index * FaceStride + b * ReadStride + 1)) - cY;
                        float Z = *(p + (index * FaceStride + b * ReadStride + 2)) - cZ;

                        float fiX = (X) * coZ - (Z) * sZ;
                        float fiZ = (Z) * coZ + (X) * sZ;
                        float ndY = (Y) * coY + (fiZ) * sY;

                        //Returns the newly rotated Vector
                        *(VERTEX_DATA + b * 3 + 0) = (fiX) * coX - (ndY) * sX;
                        *(VERTEX_DATA + b * 3 + 1) = (ndY) * coX + (fiX) * sX;
                        *(VERTEX_DATA + b * 3 + 2) = (fiZ) * coY - (Y) * sY;
                    }
            }
            else
            {
                for (int b = 0; b < 3; b++)
                    VShdr((VERTEX_DATA + b * 3 + 0), (p + (index * FaceStride + b * ReadStride)), index);
            }


            //TODO: Replace RTL_ZERO_MEMORY with a simple loop, it should be much faster
            //WARNING: Max faces is actually 12, with 4 intersectspoints coming from one vertex MAX
            //UPDATE: max 5 intersect points rippp
            //Solution Increase AP Size to + 12 grrrrr
            //INFO: Perhaps create a GLBuffer which would allow to save the amount of stackallocs called, and just keep the data
            //SUGGESTION: Use separate arrays for min maxs, index, and size buffer?

            bool* AP = stackalloc bool[BUFFER_SIZE + 12];

            #region NearPlaneCFG
            int v = 0;


            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] < nearZ)
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region NearPlane
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];

                int API = 0;

                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            FIP(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }

                BUFFER_SIZE = API / 3;
                VERTEX_DATA = strFLT;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }

            #endregion

            #region RightFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] * tanVert + ow < VERTEX_DATA[i * 3])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;
            #endregion

            #region RightFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, i - 1, i, tanVert);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, i - 1, i, tanVert);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region LeftFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] * -tanVert - ow > VERTEX_DATA[i * 3])
                {
                    AP[i] = true;
                    v++;
                }

            }

            if (v == BUFFER_SIZE)
                return;
            #endregion

            #region LeftFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, true);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, true);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, true);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIP(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, true);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region TopFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] * tanHorz + oh < VERTEX_DATA[i * 3 + 1])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region TopFOV

            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);


            }

            #endregion

            #region BottomFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * 3 + 2] * -tanHorz - oh > VERTEX_DATA[i * 3 + 1])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region BottomFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * 3 + 3];

                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, true);
                            API += 3;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, true);
                            API += 3;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, true);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPH(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, true);
                            strFLT[API + 3] = VERTEX_DATA[i * 3];
                            strFLT[API + 4] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 5] = VERTEX_DATA[i * 3 + 2];
                            API += 6;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * 3];
                            strFLT[API + 1] = VERTEX_DATA[i * 3 + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * 3 + 2];
                            API += 3;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / 3;
            }
            #endregion


            int yMax = 0;
            int yMin = renderHeight;

            #region CameraSpaceToScreenSpace
            if (matrixlerpv == 0)
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * 3 + 0] = rw + (VERTEX_DATA[im * 3 + 0] / VERTEX_DATA[im * 3 + 2]) * fw;
                    VERTEX_DATA[im * 3 + 1] = rh + (VERTEX_DATA[im * 3 + 1] / VERTEX_DATA[im * 3 + 2]) * fh;
                    VERTEX_DATA[im * 3 + 2] = 1f / VERTEX_DATA[im * 3 + 2];

                    if (VERTEX_DATA[im * 3 + 1] > yMax) yMax = (int)VERTEX_DATA[im * 3 + 1];
                    if (VERTEX_DATA[im * 3 + 1] < yMin) yMin = (int)VERTEX_DATA[im * 3 + 1];
                }
            else if (matrixlerpv == 1)
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * 3 + 0] = rw + (VERTEX_DATA[im * 3 + 0] / ox);
                    VERTEX_DATA[im * 3 + 1] = rh + (VERTEX_DATA[im * 3 + 1] / oy);

                    if (VERTEX_DATA[im * 3 + 1] > yMax) yMax = (int)VERTEX_DATA[im * 3 + 1];
                    if (VERTEX_DATA[im * 3 + 1] < yMin) yMin = (int)VERTEX_DATA[im * 3 + 1];
                }
            else
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * 3 + 0] = rw + VERTEX_DATA[im * 3 + 0] / ((VERTEX_DATA[im * 3 + 2] * fwi - ox) * (1f - matrixlerpv) + ox);
                    VERTEX_DATA[im * 3 + 1] = rh + VERTEX_DATA[im * 3 + 1] / ((VERTEX_DATA[im * 3 + 2] * fhi - oy) * (1f - matrixlerpv) + oy);

                    VERTEX_DATA[im * 3 + 2] = 1f / (VERTEX_DATA[im * 3 + 2] + oValue);

                    if (VERTEX_DATA[im * 3 + 1] > yMax) yMax = (int)VERTEX_DATA[im * 3 + 1];
                    if (VERTEX_DATA[im * 3 + 1] < yMin) yMin = (int)VERTEX_DATA[im * 3 + 1];
                }
            #endregion

            #region FaceCulling
            if (FACE_CULL)
            {
                float A = BACKFACECULL3(VERTEX_DATA);
                if (CULL_FRONT && A > 0) return;
                else if (!CULL_FRONT && A < 0) return;
            }
            #endregion

            int BGR = 0;
            byte* bBGR = (byte*)&BGR;
            SBsc(bBGR, index);

            if (LOG_T_COUNT) Interlocked.Increment(ref T_COUNT);

            float slopeZ;
            float bZ;
            float s;

            if (yMax >= renderHeight) yMax = renderHeight - 1;
            if (yMin < 0) yMin = 0;

            // Parallel.For(yMin, yMax + 1, i =>{
            float* Intersects = stackalloc float[4];

            int LX1 = 0, LX2 = 0, RX1 = 0, RX2 = 0;
            
            bool AX = false;

            for (int i = yMin; i <= yMax; i++)
            {
                if (ScanLine(i, VERTEX_DATA, BUFFER_SIZE, Intersects))
                {
                    #region IntersectSortLeftToRight
                    if (Intersects[0] > Intersects[1])
                    {
                        float a = Intersects[1];
                        Intersects[1] = Intersects[0];
                        Intersects[0] = a;

                        a = Intersects[3];
                        Intersects[3] = Intersects[2];
                        Intersects[2] = a;
                    }
                    #endregion

                    #region Z_Interpolation
                    slopeZ = (Intersects[2] - Intersects[3]) / (Intersects[0] - Intersects[1]);
                    bZ = -slopeZ * (int)Intersects[0] + Intersects[2];
                    #endregion

                    #region BufferOverflowProtection
                    if ((int)Intersects[1] >= renderWidth) Intersects[1] = renderWidth - 1;
                    if ((int)Intersects[0] < 0) Intersects[0] = 0;
                    #endregion

                   
                    for (int o = (int)Intersects[0] + 1; o <= (int)Intersects[1]; ++o)
                    {
                        if (cmatrix) s = farZ - (1f / (slopeZ * (float)o + bZ) - oValue);
                        else s = farZ - (slopeZ * (float)o + bZ);

                        if (dptr[renderWidth * i + o] > s) continue;
                        dptr[renderWidth * i + o] = s;

                        *(iptr + i * renderWidth + o) = BGR;

                        if (WriteClick)
                        {
                            cptr[i * renderWidth + o] = index + 1;
                            aptr[i * renderWidth + o] = CBuffervalue;
                        }
                    }

                    
                    if (FACE_AA & false)
                    {
                        LX2 = (int)Intersects[0] + 1;
                        RX2 = (int)Intersects[1];

                        if (!AX){
                            LX1 = LX2;
                            RX1 = RX2;
                            AX = true;
                        }



                        if (LX2 - LX1 > 1f | LX2 - LX1 < -1f)
                        {
                            float slopeO = -1f / (float)(LX1 - LX2);
                            float bO = -slopeO * LX2;

                         //   DrawLine(LX1, i, LX2, i, 255, 0, 255);

                            if (LX2 > LX1 & false)
                                for (int o = LX1; o <= LX2; o++)
                                {
                                    if (cmatrix) s = farZ - (1f / (slopeZ * (float)o + bZ) - oValue);
                                    else s = farZ - (slopeZ * (float)o + bZ);

                                    float MB = slopeO * o + bO;

                                    if (dptr[renderWidth * i + o] > s) continue;
                                    dptr[renderWidth * i + o] = s;

                                    byte* lptr = bptr + i * wsD + (sD * o);

                                    *(iptr + i * renderWidth + o) = (((
                                        (byte)((*(lptr + 2) * (1f - MB)) + MB * bBGR[2]) << 8) |
                                        (byte)((*(lptr + 1) * (1f - MB)) + MB * bBGR[1])) << 8) |
                                        (byte)((*(lptr + 0) * (1f - MB)) + MB * bBGR[0]);

                                   // iptr[i * renderWidth + o] = 255;
                                }
                            else
                                for (int o = LX2; o <= LX1; o++)
                                {
                                    if (cmatrix) s = farZ - (1f / (slopeZ * (float)o + bZ) - oValue);
                                    else s = farZ - (slopeZ * (float)o + bZ);

                                    float MB = slopeO * o + bO;

                                    if (dptr[renderWidth * i + o] > s) continue;
                                    dptr[renderWidth * i + o] = s;

                                    byte* lptr = bptr + i * wsD + (sD * o);

                                    *(iptr + i * renderWidth + o) = (((
                                        (byte)((*(lptr + 2) * (1f - MB)) + MB * bBGR[2]) << 8) |
                                        (byte)((*(lptr + 1) * (1f - MB)) + MB * bBGR[1])) << 8) |
                                        (byte)((*(lptr + 0) * (1f - MB)) + MB * bBGR[0]);
                                }     
                        }
                        else
                        {
                            int o = (int)Intersects[0];
                            float MB = (o + 1f) - Intersects[0];

                            if (cmatrix) s = farZ - 1f / (slopeZ * (float)o + bZ);
                            else s = farZ - (slopeZ * (float)o + bZ);

                            if (dptr[renderWidth * i + o] <= s)
                            {
                                byte* lptr = bptr + i * wsD + (sD * o);

                                *(iptr + i * renderWidth + o) = (((
                                 (byte)((*(lptr + 2) * (1f - MB)) + MB * bBGR[2]) << 8) |
                                 (byte)((*(lptr + 1) * (1f - MB)) + MB * bBGR[1])) << 8) |
                                 (byte)((*(lptr + 0) * (1f - MB)) + MB * bBGR[0]);
                            }
                        }

                        if (RX2 - RX1 > 1f | RX2 - RX1 < -1f)
                        {
                            float slopeO = -1f / (RX1 - RX2);
                            float bO = -slopeO * RX2;

                            DrawLine(LX1, i, LX2, i, 255, 0, 255);

                            if (RX2 > RX1)
                                for (int o = RX1; o <= RX2; o++)
                                {
                                    if (cmatrix) s = farZ - (1f / (slopeZ * (float)o + bZ) - oValue);
                                    else s = farZ - (slopeZ * (float)o + bZ);

                                    float MB = slopeO * o + bO;

                                    if (dptr[renderWidth * i + o] > s) continue;
                                    dptr[renderWidth * i + o] = s;

                                    byte* lptr = bptr + i * wsD + (sD * o);

                                    *(iptr + i * renderWidth + o) = (((
                                        (byte)((*(lptr + 2) * (1f - MB)) + MB * bBGR[2]) << 8) |
                                        (byte)((*(lptr + 1) * (1f - MB)) + MB * bBGR[1])) << 8) |
                                        (byte)((*(lptr + 0) * (1f - MB)) + MB * bBGR[0]);
                                }
                            else
                                for (int o = RX2; o <= RX1; o++)
                                {
                                    if (cmatrix) s = farZ - (1f / (slopeZ * (float)o + bZ) - oValue);
                                    else s = farZ - (slopeZ * (float)o + bZ);

                                    float MB = slopeO * o + bO;

                                    if (dptr[renderWidth * i + o] > s) continue;
                                    dptr[renderWidth * i + o] = s;

                                    byte* lptr = bptr + i * wsD + (sD * o);

                                    *(iptr + i * renderWidth + o) = (((
                                        (byte)((*(lptr + 2) * (1f - MB)) + MB * bBGR[2]) << 8) |
                                        (byte)((*(lptr + 1) * (1f - MB)) + MB * bBGR[1])) << 8) |
                                        (byte)((*(lptr + 0) * (1f - MB)) + MB * bBGR[0]);
                                }
                        }
                        else
                        {
                            int o = (int)Intersects[1];
                            float MB = ((Intersects[1]) - (float)o);
                            o++;

                            if (cmatrix) s = farZ - 1f / (slopeZ * (float)o + bZ);
                            else s = farZ - (slopeZ * (float)o + bZ);

                            if (dptr[renderWidth * i + o] <= s)
                            {
                                byte* lptr = bptr + i * wsD + (sD * o);

                                *(iptr + i * renderWidth + o) = (((
                                 (byte)((*(lptr + 2) * (1f - MB)) + MB * bBGR[2]) << 8) |
                                 (byte)((*(lptr + 1) * (1f - MB)) + MB * bBGR[1])) << 8) |
                                 (byte)((*(lptr + 0) * (1f - MB)) + MB * bBGR[0]);
                            }
                        }

                        LX1 = LX2;
                        RX1 = RX2;
                    }

                   // if (false)


                    
                }
            }

            if (LinkedWFrame) LateWireFrame(VERTEX_DATA, BUFFER_SIZE);
        }

        public void FillFull(int index) 
        {
            float* VERTEX_DATA = stackalloc float[Stride * 3];
            int BUFFER_SIZE = 3;

            #region Vertex Input and Processing
            if (!CAMERA_BYPASS)
            {
                if (HAS_VERTEX_SHADER)
                {
                    if (COPY_ATTRIB_MANUAL) 
                        for (int b = 0; b < 3; b++)
                            VShdr((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                    else for (int b = 0; b < 3; b++)
                        {
                            VShdr((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                            for (int a = 3; a < Stride; a++)
                                VERTEX_DATA[b * Stride + a] = *(p + (index * FaceStride) + b * ReadStride + a);
                        }

                    for (int b = 0; b < 3; b++)
                    {
                        float X = *(VERTEX_DATA + b * Stride + 0) - cX;
                        float Y = *(VERTEX_DATA + b * Stride + 1) - cY;
                        float Z = *(VERTEX_DATA + b * Stride + 2) - cZ;

                        float fiX = (X) * coZ - (Z) * sZ;
                        float fiZ = (Z) * coZ + (X) * sZ;
                        float ndY = (Y) * coY + (fiZ) * sY;

                        //Returns the newly rotated Vector
                        *(VERTEX_DATA + b * Stride + 0) = (fiX) * coX - (ndY) * sX;
                        *(VERTEX_DATA + b * Stride + 1) = (ndY) * coX + (fiX) * sX;
                        *(VERTEX_DATA + b * Stride + 2) = (fiZ) * coY - (Y) * sY;
                    }
                }
                else
                    for (int b = 0; b < 3; b++)
                    {
                        float X = *(p + (index * FaceStride + b * ReadStride)) - cX;
                        float Y = *(p + (index * FaceStride + b * ReadStride + 1)) - cY;
                        float Z = *(p + (index * FaceStride + b * ReadStride + 2)) - cZ;

                        float fiX = (X) * coZ - (Z) * sZ;
                        float fiZ = (Z) * coZ + (X) * sZ;
                        float ndY = (Y) * coY + (fiZ) * sY;

                        //Returns the newly rotated Vector
                        *(VERTEX_DATA + b * Stride + 0) = (fiX) * coX - (ndY) * sX;
                        *(VERTEX_DATA + b * Stride + 1) = (ndY) * coX + (fiX) * sX;
                        *(VERTEX_DATA + b * Stride + 2) = (fiZ) * coY - (Y) * sY;

                        for (int a = 3; a < Stride; a++)
                            VERTEX_DATA[b * Stride + a] = *(p + (index * FaceStride) + b * ReadStride + a);
                    }
            }
            else
            {
                if (COPY_ATTRIB_MANUAL)
                    for (int b = 0; b < 3; b++)
                        VShdr((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                else for (int b = 0; b < 3; b++)
                    {
                        VShdr((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                        for (int a = 3; a < Stride; a++)
                            VERTEX_DATA[b * Stride + a] = *(p + (index * FaceStride) + b * ReadStride + a);
                    }
            }
            #endregion
            //TODO: Replace RTL_ZERO_MEMORY with a simple loop, it should be much faster
            //WARNING: Max faces is actually 12, with 4 intersectspoints coming from one vertex MAX
            //UPDATE: max 5 intersect points rippp
            //Solution Increase AP Size to + 12 grrrrr
            //INFO: Perhaps create a GLBuffer which would allow to save the amount of stackallocs called, and just keep the data
            //SUGGESTION: Use separate arrays for min maxs, index, and size buffer?

            bool* AP = stackalloc bool[BUFFER_SIZE + 12];

            #region NearPlaneCFG
            int v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] < nearZ)
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region NearPlane
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;

                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }

                BUFFER_SIZE = API / Stride;
                VERTEX_DATA = strFLT;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }

            #endregion

            #region FarPlaneCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] > farZ)
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region FarPlane
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, farZ);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, i - 1, i, farZ);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, farZ);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, i - 1, i, farZ);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / Stride;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region RightFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] * tanVert + ow < VERTEX_DATA[i * Stride])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;
            #endregion

            #region RightFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, i - 1, i, tanVert);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, i - 1, i, tanVert);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / Stride;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region LeftFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] * -tanVert - ow > VERTEX_DATA[i * Stride])
                {
                    AP[i] = true;
                    v++;
                }

            }

            if (v == BUFFER_SIZE)
                return;
            #endregion

            #region LeftFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, true);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, true);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, true);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, true);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / Stride;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region TopFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] * tanHorz + oh < VERTEX_DATA[i * Stride + 1])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region TopFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / Stride;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }

            #endregion

            #region BottomFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] * -tanHorz - oh > VERTEX_DATA[i * Stride + 1])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region BottomFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, true);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, true);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, true);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];


                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, true);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];


                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / Stride;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion


            int yMax = 0;
            int yMin = renderHeight;

            #region CameraSpaceToScreenSpace
            if (matrixlerpv == 0)
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + (VERTEX_DATA[im * Stride + 0] / VERTEX_DATA[im * Stride + 2]) * fw;
                    VERTEX_DATA[im * Stride + 1] = rh + (VERTEX_DATA[im * Stride + 1] / VERTEX_DATA[im * Stride + 2]) * fh;
                    VERTEX_DATA[im * Stride + 2] = 1f / (VERTEX_DATA[im * Stride + 2]);

                    if (VERTEX_DATA[im * Stride + 1] > yMax) yMax = (int)VERTEX_DATA[im * Stride + 1];
                    if (VERTEX_DATA[im * Stride + 1] < yMin) yMin = (int)VERTEX_DATA[im * Stride + 1];
                }
            else if (matrixlerpv == 1)
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + VERTEX_DATA[im * Stride + 0] / ox;
                    VERTEX_DATA[im * Stride + 1] = rh + VERTEX_DATA[im * Stride + 1] / oy;

                    if (VERTEX_DATA[im * Stride + 1] > yMax) yMax = (int)VERTEX_DATA[im * Stride + 1];
                    if (VERTEX_DATA[im * Stride + 1] < yMin) yMin = (int)VERTEX_DATA[im * Stride + 1];
                }
            else
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + VERTEX_DATA[im * Stride + 0] / ((VERTEX_DATA[im * Stride + 2] * fwi - ox) * (1f - matrixlerpv) + ox);
                    VERTEX_DATA[im * Stride + 1] = rh + VERTEX_DATA[im * Stride + 1] / ((VERTEX_DATA[im * Stride + 2] * fhi - oy) * (1f - matrixlerpv) + oy);
                    VERTEX_DATA[im * Stride + 2] = 1f / (VERTEX_DATA[im * Stride + 2] + oValue);


                    if (VERTEX_DATA[im * Stride + 1] > yMax) yMax = (int)VERTEX_DATA[im * Stride + 1];
                    if (VERTEX_DATA[im * Stride + 1] < yMin) yMin = (int)VERTEX_DATA[im * Stride + 1];
                }
            #endregion

            #region FaceCulling
            if (FACE_CULL)
            {
                float A = BACKFACECULLS(VERTEX_DATA);
                if (CULL_FRONT && A > 0) return;
                else if (!CULL_FRONT && A < 0) return;
            }
            #endregion

            if (LOG_T_COUNT) Interlocked.Increment(ref T_COUNT);

            if (yMin < 0) yMin = 0;
            if (yMax >= renderHeight) yMax = renderHeight - 1;

            Parallel.For(yMin, yMax + 1, i =>
            {
                float* Intersects = stackalloc float[4 + (Stride - 3) * 5 + ATTRIBLVL];
                float* az = Intersects + 4 + (Stride - 3) * 2;
                float* slopeAstack = az + (Stride - 3) + ATTRIBLVL;
                float* bAstack = slopeAstack + (Stride - 3);
                float* RTNA = az + ATTRIBLVL;
                //  for (int i = yMin; i <= yMax; i++){


                if (ScanLineTEST(i, VERTEX_DATA, BUFFER_SIZE, Intersects))
                {
                    if (Intersects[0] > Intersects[1])
                    {
                        float a = Intersects[1];
                        Intersects[1] = Intersects[0];
                        Intersects[0] = a;

                        a = Intersects[3];
                        Intersects[3] = Intersects[2];
                        Intersects[2] = a;

                        for (int b = 3; b < Stride; b++)
                        {
                            float bn = Intersects[(b - 3) * 2 + 4 + 1];
                            Intersects[(b - 3) * 2 + 4 + 1] = Intersects[(b - 3) * 2 + 4 + 0];
                            Intersects[(b - 3) * 2 + 4 + 0] = bn;
                        }
                    }

                    float slopeZ = (Intersects[2] - Intersects[3]) / (Intersects[0] - Intersects[1]);
                    float bZ = -slopeZ * Intersects[0] + Intersects[2];

                    if ((int)Intersects[1] >= renderWidth) Intersects[1] = renderWidth - 1;
                    if ((int)Intersects[0] < 0) Intersects[0] = 0;

                    float ZDIFF = 1f / Intersects[2] - 1f / Intersects[3];
                    bool usingZ = ZDIFF != 0;
                    if (ZDIFF != 0) usingZ = ZDIFF * ZDIFF >= 0.00001f;
               
                    if (usingZ & matrixlerpv != 1)
                        for (int b = 0; b < Stride - 3; b++)
                        {
                            float slopeA = (Intersects[(b) * 2 + 4 + 0] - Intersects[(b) * 2 + 4 + 1]) / ((1f / Intersects[2]) - (1f / Intersects[3]));
                            float bA = -slopeA / Intersects[2] + Intersects[(b) * 2 + 4 + 0];

                            slopeAstack[b] = slopeA;
                            bAstack[b] = bA;
                    }
                    else
                        for (int b = 0; b < Stride - 3; b++)
                        {
                            float slopeA = (Intersects[(b) * 2 + 4 + 0] - Intersects[(b) * 2 + 4 + 1]) / (Intersects[0] - Intersects[1]);
                            float bA = -slopeA * Intersects[0] + Intersects[(b) * 2 + 4 + 0];

                            slopeAstack[b] = slopeA;
                            bAstack[b] = bA;
                        }
                    
                    
                    /*/
                    for (int b = 0; b < Stride - 3; b++)
                    {
                        float slopeA = ((Intersects[b * 2 + 4 + 0]) -(Intersects[b * 2 + 4 + 1])) / (Intersects[0] - Intersects[1]);
                        float bA = -slopeA * Intersects[0] + (Intersects[(b) * 2 + 4 + 0]);

                        slopeAstack[b] = slopeA;
                        bAstack[b] = bA;
                    }
                    /*/
                     
                    //IF ATTRIBS ARE PLACE ON A XZ FLAT PLANE (IN WORLD SPACE) THE INTERPOLATION BREAKS!!!
                    //FOR SOME REASON USING REGULAR LINEAR INTERPOLATION WORKS PERFECTELY OK.
                    //THE Z VALUES FROM X1 to X2 INTERSECTS SEEMS TO BE EXACTLY THE SAME
                    if (attribdata)
                        for (int o = (int)Intersects[0]; o <= (int)Intersects[1]; o++)
                        {
                            float zz;// = (1f / (slopeZ * (float)o + bZ) - oValue);
                           // float zz = 1f / (slopeZ * (float)o + bZ);
                           
                            if (cmatrix) zz = (1f / (slopeZ * (float)o + bZ) - oValue);
                            else zz = (slopeZ * (float)o + bZ);

                            float s = farZ - zz;


                            if (dptr[renderWidth * i + o] > s) continue;
                            dptr[renderWidth * i + o] = s;

                            if (ATTRIBLVL == 3)
                            {
                                RTNA[0] = ((zz * fwi - ox) * matrixlerpo + ox) * (o - rw);
                                RTNA[1] = ((zz * fhi - oy) * matrixlerpo + oy) * (i - rh);
                                RTNA[2] = zz;
                            }
                            else if (ATTRIBLVL == 5)
                            {
                                RTNA[0] = ((zz * fwi - ox) * matrixlerpo + ox) * (o - rw);
                                RTNA[1] = ((zz * fhi - oy) * matrixlerpo + oy) * (i - rh);
                                RTNA[2] = zz;
                                RTNA[3] = o;
                                RTNA[4] = i;
                            }
                            else if (ATTRIBLVL == 2)
                            {
                                RTNA[0] = o;
                                RTNA[1] = i;
                            }
                            else if (ATTRIBLVL == 1)
                            {
                                RTNA[0] = zz;
                            }

                            if (usingZ) for (int z = 0; z < Stride - 3; z++) az[z] = (slopeAstack[z] / (slopeZ * (float)o + bZ) + bAstack[z]);
                            else for (int z = 0; z < Stride - 3; z++) az[z] = (slopeAstack[z] * (float)o + bAstack[z]);
                            
                            //   SF((bptr + (i * wsD + (o * sD) + 0)), index);
                            SAdv((bptr + (i * wsD + (o * sD) + 0)), az, index);
                        }
                    else
                        for (int o = (int)Intersects[0]; o <= (int)Intersects[1]; o++)
                        {
                            float s;
                         //   float s = farZ - (1f / ((slopeZ * (float)o + bZ)));
                            if (cmatrix) s = farZ - (1f / (slopeZ * (float)o + bZ) - oValue);
                            else s = farZ - (slopeZ * (float)o + bZ);
                          //  float d = slopeZ * (float)o + bZ;


                            if (dptr[renderWidth * i + o] > s) continue;
                            dptr[renderWidth * i + o] = s;

                            if (usingZ & matrixlerpv != 1) 
                                for (int z = 0; z < Stride - 3; z++) az[z] = (slopeAstack[z] / (slopeZ * (float)o + bZ) + bAstack[z]);
                            else for (int z = 0; z < Stride - 3; z++)
                                az[z] = (slopeAstack[z] * (float)o + bAstack[z]);

                            //   SF((bptr + (i * wsD + (o * sD) + 0)), index);
                            SAdv((bptr + (i * wsD + (o * sD) + 0)), az, index);
                        }
                }
            });

            if (LinkedWFrame) LateWireFrame(VERTEX_DATA, BUFFER_SIZE);
        }

        public void WireFrame(int index)
        {
            float* VERTEX_DATA = stackalloc float[Stride * 3];
            int BUFFER_SIZE = 3;

            for (int b = 0; b < 3; b++)
            {
                float X = *(p + (index * FaceStride + b * Stride)) - cX;
                float Y = *(p + (index * FaceStride + b * Stride + 1)) - cY;
                float Z = *(p + (index * FaceStride + b * Stride + 2)) - cZ;

                float fiX = (X) * coZ - (Z) * sZ;
                float fiZ = (Z) * coZ + (X) * sZ;
                float ndY = (Y) * coY + (fiZ) * sY;

                //Returns the newly rotated Vector
                *(VERTEX_DATA + b * Stride + 0) = (fiX) * coX - (ndY) * sX;
                *(VERTEX_DATA + b * Stride + 1) = (ndY) * coX + (fiX) * sX;
                *(VERTEX_DATA + b * Stride + 2) = (fiZ) * coY - (Y) * sY;

                for (int a = 3; a < Stride; a++)
                    VERTEX_DATA[b * Stride + a] = *(p + (index * FaceStride) + b * Stride + a);
            }
            //TODO: Replace RTL_ZERO_MEMORY with a simple loop, it should be much faster
            //WARNING: Max faces is actually 12, with 4 intersectspoints coming from one vertex MAX
            //UPDATE: max 5 intersect points rippp
            //Solution Increase AP Size to + 12 grrrrr
            //INFO: Perhaps create a GLBuffer which would allow to save the amount of stackallocs called, and just keep the data
            //SUGGESTION: Use separate arrays for min maxs, index, and size buffer?

            bool* AP = stackalloc bool[BUFFER_SIZE + 12];

            #region NearPlaneCFG
            int v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] < nearZ)
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region NearPlane
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;

                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }

                BUFFER_SIZE = API / Stride;
                VERTEX_DATA = strFLT;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }

            #endregion

            #region FarPlaneCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] > farZ)
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region FarPlane
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, farZ);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, i - 1, i, farZ);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, farZ);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, i - 1, i, farZ);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / Stride;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region RightFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] * tanVert + ow < VERTEX_DATA[i * Stride])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;
            #endregion

            #region RightFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, i - 1, i, tanVert);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, i - 1, i, tanVert);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / Stride;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region LeftFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] * -tanVert - ow > VERTEX_DATA[i * Stride])
                {
                    AP[i] = true;
                    v++;
                }

            }

            if (v == BUFFER_SIZE)
                return;
            #endregion

            #region LeftFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, true);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, true);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, true);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, true);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / Stride;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region TopFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] * tanHorz + oh < VERTEX_DATA[i * Stride + 1])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region TopFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / Stride;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }

            #endregion

            #region BottomFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] * -tanHorz - oh > VERTEX_DATA[i * Stride + 1])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region BottomFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, true);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, true);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, true);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];


                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, true);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];


                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / Stride;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            
            if (LOG_T_COUNT) Interlocked.Increment(ref T_COUNT);


            #region CameraSpaceToScreenSpace
            if (matrixlerpv == 0)
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + (VERTEX_DATA[im * Stride + 0] / VERTEX_DATA[im * Stride + 2]) * fw;
                    VERTEX_DATA[im * Stride + 1] = rh + (VERTEX_DATA[im * Stride + 1] / VERTEX_DATA[im * Stride + 2]) * fh;
                    VERTEX_DATA[im * Stride + 2] = 1f / VERTEX_DATA[im * Stride + 2];
                }
            else if (matrixlerpv == 1)
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + (VERTEX_DATA[im * Stride + 0] / ox);
                    VERTEX_DATA[im * Stride + 1] = rh + (VERTEX_DATA[im * Stride + 1] / oy);
                }
            else
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + VERTEX_DATA[im * Stride + 0] / ((VERTEX_DATA[im * Stride + 2] * fwi - ox) * (1f - matrixlerpv) + ox);
                    VERTEX_DATA[im * Stride + 1] = rh + VERTEX_DATA[im * Stride + 1] / ((VERTEX_DATA[im * Stride + 2] * fhi - oy) * (1f - matrixlerpv) + oy);

                    VERTEX_DATA[im * Stride + 2] = 1f / (VERTEX_DATA[im * Stride + 2] + oValue);
                }
            #endregion

            #region FaceCulling
            if (FACE_CULL)
            {
                float A = BACKFACECULLS(VERTEX_DATA);
                if (CULL_FRONT && A > 0) return;
                else if (!CULL_FRONT && A < 0) return;
            }
            #endregion

            float* sSpace = stackalloc float[(Stride - 3) * 3];

            for (int im = 1; im < BUFFER_SIZE; im++)
            {
                DrawLineFull(VERTEX_DATA + im * Stride, VERTEX_DATA + (im - 1) * Stride, sSpace, index);
            }

            DrawLineFull(VERTEX_DATA, VERTEX_DATA + (BUFFER_SIZE - 1) * Stride, sSpace, index);
        }

        public void FillGouraud(int index)
        {
            float* VERTEX_DATA = stackalloc float[Stride * 3];
            int BUFFER_SIZE = 3;

            #region Vertex Input and Processing
            if (!CAMERA_BYPASS)
            {
                if (COPY_ATTRIB_MANUAL)
                    for (int b = 0; b < 3; b++)
                        VShdr((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                else for (int b = 0; b < 3; b++)
                    {
                        VShdr((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                        for (int a = 3; a < Stride; a++)
                            VERTEX_DATA[b * Stride + a] = *(p + (index * FaceStride) + b * ReadStride + a);
                    }

                for (int b = 0; b < 3; b++)
                {
                    float X = *(VERTEX_DATA + b * Stride + 0) - cX;
                    float Y = *(VERTEX_DATA + b * Stride + 1) - cY;
                    float Z = *(VERTEX_DATA + b * Stride + 2) - cZ;

                    float fiX = (X) * coZ - (Z) * sZ;
                    float fiZ = (Z) * coZ + (X) * sZ;
                    float ndY = (Y) * coY + (fiZ) * sY;

                    //Returns the newly rotated Vector
                    *(VERTEX_DATA + b * Stride + 0) = (fiX) * coX - (ndY) * sX;
                    *(VERTEX_DATA + b * Stride + 1) = (ndY) * coX + (fiX) * sX;
                    *(VERTEX_DATA + b * Stride + 2) = (fiZ) * coY - (Y) * sY;
                }                
            }
            else
            {
                if (COPY_ATTRIB_MANUAL)
                    for (int b = 0; b < 3; b++)
                        VShdr((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                else for (int b = 0; b < 3; b++)
                    {
                        VShdr((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                        for (int a = 3; a < Stride; a++)
                            VERTEX_DATA[b * Stride + a] = *(p + (index * FaceStride) + b * ReadStride + a);
                    }
            }
            #endregion
            //TODO: Replace RTL_ZERO_MEMORY with a simple loop, it should be much faster
            //WARNING: Max faces is actually 12, with 4 intersectspoints coming from one vertex MAX
            //UPDATE: max 5 intersect points rippp
            //Solution Increase AP Size to + 12 grrrrr
            //INFO: Perhaps create a GLBuffer which would allow to save the amount of stackallocs called, and just keep the data
            //SUGGESTION: Use separate arrays for min maxs, index, and size buffer?

            bool* AP = stackalloc bool[BUFFER_SIZE + 12];

            #region NearPlaneCFG
            int v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] < nearZ)
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region NearPlane
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;

                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, nearZ);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, i - 1, i, nearZ);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }

                BUFFER_SIZE = API / Stride;
                VERTEX_DATA = strFLT;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }

            #endregion

            #region FarPlaneCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] > farZ)
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region FarPlane
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, farZ);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, i - 1, i, farZ);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, farZ);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            FIPA(strFLT, API, VERTEX_DATA, i - 1, i, farZ);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / Stride;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region RightFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] * tanVert + ow < VERTEX_DATA[i * Stride])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;
            #endregion

            #region RightFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, i - 1, i, tanVert);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanVert);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, i - 1, i, tanVert);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / Stride;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region LeftFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] * -tanVert - ow > VERTEX_DATA[i * Stride])
                {
                    AP[i] = true;
                    v++;
                }

            }

            if (v == BUFFER_SIZE)
                return;
            #endregion

            #region LeftFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, true);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, true);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanVert, true);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPA(strFLT, API, VERTEX_DATA, i - 1, i, -tanVert, true);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / Stride;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            #region TopFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] * tanHorz + oh < VERTEX_DATA[i * Stride + 1])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region TopFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, tanHorz);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, tanHorz);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / Stride;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }

            #endregion

            #region BottomFOVCFG
            v = 0;

            for (int i = 0; i < BUFFER_SIZE; i++)
            {
                if (VERTEX_DATA[i * Stride + 2] * -tanHorz - oh > VERTEX_DATA[i * Stride + 1])
                {
                    AP[i] = true;
                    v++;
                }
            }

            if (v == BUFFER_SIZE)
                return;

            #endregion

            #region BottomFOV
            if (v != 0)
            {
                float* strFLT = stackalloc float[BUFFER_SIZE * Stride + Stride];
                int API = 0;
                for (int i = 0; i < BUFFER_SIZE; i++)
                {
                    if (AP[i])
                    {
                        if (i == 0 && !AP[BUFFER_SIZE - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, true);
                            API += Stride;
                        }
                        else if (i > 0 && !AP[i - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, true);
                            API += Stride;
                        }
                    }
                    else
                    {
                        if (i == 0 && AP[BUFFER_SIZE - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, BUFFER_SIZE - 1, i, -tanHorz, true);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];


                            API += Stride;
                        }
                        else if (i > 0 && AP[i - 1])
                        {
                            SIPHA(strFLT, API, VERTEX_DATA, i - 1, i, -tanHorz, true);
                            API += Stride;

                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];


                            API += Stride;
                        }
                        else
                        {
                            strFLT[API + 0] = VERTEX_DATA[i * Stride];
                            strFLT[API + 1] = VERTEX_DATA[i * Stride + 1];
                            strFLT[API + 2] = VERTEX_DATA[i * Stride + 2];

                            for (int a = 3; a < Stride; a++)
                                strFLT[API + a] = VERTEX_DATA[i * Stride + a];

                            API += Stride;
                        }
                    }
                }
                VERTEX_DATA = strFLT;
                BUFFER_SIZE = API / Stride;
                RtlZeroMemory((IntPtr)AP, BUFFER_SIZE);
            }
            #endregion

            int yMax = 0;
            int yMin = renderHeight;

            #region CameraSpaceToScreenSpace
            if (matrixlerpv == 0)
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + (VERTEX_DATA[im * Stride + 0] / VERTEX_DATA[im * Stride + 2]) * fw;
                    VERTEX_DATA[im * Stride + 1] = rh + (VERTEX_DATA[im * Stride + 1] / VERTEX_DATA[im * Stride + 2]) * fh;
                    VERTEX_DATA[im * Stride + 2] = 1f / (VERTEX_DATA[im * Stride + 2]);

                    if (VERTEX_DATA[im * Stride + 1] > yMax) yMax = (int)VERTEX_DATA[im * Stride + 1];
                    if (VERTEX_DATA[im * Stride + 1] < yMin) yMin = (int)VERTEX_DATA[im * Stride + 1];
                }
            else if (matrixlerpv == 1)
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + VERTEX_DATA[im * Stride + 0] / ox;
                    VERTEX_DATA[im * Stride + 1] = rh + VERTEX_DATA[im * Stride + 1] / oy;

                    if (VERTEX_DATA[im * Stride + 1] > yMax) yMax = (int)VERTEX_DATA[im * Stride + 1];
                    if (VERTEX_DATA[im * Stride + 1] < yMin) yMin = (int)VERTEX_DATA[im * Stride + 1];
                }
            else
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + VERTEX_DATA[im * Stride + 0] / ((VERTEX_DATA[im * Stride + 2] * fwi - ox) * (1f - matrixlerpv) + ox);
                    VERTEX_DATA[im * Stride + 1] = rh + VERTEX_DATA[im * Stride + 1] / ((VERTEX_DATA[im * Stride + 2] * fhi - oy) * (1f - matrixlerpv) + oy);
                    VERTEX_DATA[im * Stride + 2] = 1f / (VERTEX_DATA[im * Stride + 2] + oValue);


                    if (VERTEX_DATA[im * Stride + 1] > yMax) yMax = (int)VERTEX_DATA[im * Stride + 1];
                    if (VERTEX_DATA[im * Stride + 1] < yMin) yMin = (int)VERTEX_DATA[im * Stride + 1];
                }
            #endregion

            #region FaceCulling
            if (FACE_CULL)
            {
                float A = BACKFACECULLS(VERTEX_DATA);
                if (CULL_FRONT && A > 0) return;
                else if (!CULL_FRONT && A < 0) return;
            }
            #endregion

            if (LOG_T_COUNT) Interlocked.Increment(ref T_COUNT);

            if (yMin < 0) yMin = 0;
            if (yMax >= renderHeight) yMax = renderHeight - 1;

            float* Intersects = stackalloc float[4 + (Stride - 3) * 5];
            float* az = Intersects + 4 + (Stride - 3) * 2;
            float* slopeAstack = az + (Stride - 3);
            float* bAstack = slopeAstack + (Stride - 3);
            float s;

            for (int i = yMin; i <= yMax; i++){
                if (ScanLineTEST(i, VERTEX_DATA, BUFFER_SIZE, Intersects))
                {
                    if (Intersects[0] > Intersects[1])
                    {
                        float a = Intersects[1];
                        Intersects[1] = Intersects[0];
                        Intersects[0] = a;

                        a = Intersects[3];
                        Intersects[3] = Intersects[2];
                        Intersects[2] = a;

                        for (int b = 3; b < Stride; b++)
                        {
                            float bn = Intersects[(b - 3) * 2 + 4 + 1];
                            Intersects[(b - 3) * 2 + 4 + 1] = Intersects[(b - 3) * 2 + 4 + 0];
                            Intersects[(b - 3) * 2 + 4 + 0] = bn;
                        }
                    }

                    float slopeZ = (Intersects[2] - Intersects[3]) / (Intersects[0] - Intersects[1]);
                    float bZ = -slopeZ * Intersects[0] + Intersects[2];

                    if ((int)Intersects[1] >= renderWidth) Intersects[1] = renderWidth - 1;
                    if ((int)Intersects[0] < 0) Intersects[0] = 0;

                    float ZDIFF = 1f / Intersects[2] - 1f / Intersects[3];
                    bool usingZ = ZDIFF != 0;
                    if (ZDIFF != 0) usingZ = ZDIFF * ZDIFF >= 0.00001f;

                    if (usingZ & matrixlerpv != 1)
                        for (int b = 0; b < Stride - 3; b++)
                        {
                            float slopeA = (Intersects[(b) * 2 + 4 + 0] - Intersects[(b) * 2 + 4 + 1]) / (1f / Intersects[2] - 1f / Intersects[3]);
                            float bA = -slopeA / Intersects[2] + Intersects[(b) * 2 + 4 + 0];

                            slopeAstack[b] = slopeA;
                            bAstack[b] = bA;
                        }
                    else
                        for (int b = 0; b < Stride - 3; b++)
                        {
                            float slopeA = (Intersects[(b) * 2 + 4 + 0] - Intersects[(b) * 2 + 4 + 1]) / (Intersects[0] - Intersects[1]);
                            float bA = -slopeA * Intersects[0] + Intersects[(b) * 2 + 4 + 0];

                            slopeAstack[b] = slopeA;
                            bAstack[b] = bA;
                        }

                    //IF ATTRIBS ARE PLACE ON A XZ FLAT PLANE (IN WORLD SPACE) THE INTERPOLATION BREAKS!!!
                    //FOR SOME REASON USING REGULAR LINEAR INTERPOLATION WORKS PERFECTELY OK.
                    //THE Z VALUES FROM X1 to X2 INTERSECTS SEEMS TO BE EXACTLY THE SAME
                    for (int o = (int)Intersects[0] + 1; o <= (int)Intersects[1]; o++)
                    {
                        if (cmatrix) s = farZ - (1f / (slopeZ * (float)o + bZ) - oValue);
                        else s = farZ - (slopeZ * (float)o + bZ);

                        if (dptr[renderWidth * i + o] > s) continue;
                        dptr[renderWidth * i + o] = s;

                        if (usingZ & matrixlerpv != 1) for (int z = 0; z < Stride - 3; z++) az[z] = (slopeAstack[z] / (slopeZ * (float)o + bZ) + bAstack[z]);
                        else for (int z = 0; z < Stride - 3; z++) az[z] = (slopeAstack[z] * (float)o + bAstack[z]);
                        //int* big performance boost possibility!

                        *(bptr + (i * wsD + (o * sD) + 0)) = (byte)az[0];
                        *(bptr + (i * wsD + (o * sD) + 1)) = (byte)az[1];
                        *(bptr + (i * wsD + (o * sD) + 2)) = (byte)az[2];

                        if (WriteClick)
                        {
                            cptr[i * renderWidth + o] = index + 1;
                            aptr[i * renderWidth + o] = CBuffervalue;
                        }
                    }
                        
                }
            }

            if (LinkedWFrame) LateWireFrame(VERTEX_DATA, BUFFER_SIZE);
        }

        public void LineMode(int index)
        {
            float* VERTEX_DATA = stackalloc float[2 * Stride];

            if (!CAMERA_BYPASS)
            {
                if (COPY_ATTRIB_MANUAL)
                    for (int b = 0; b < 2; b++)
                        VShdr((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                else for (int b = 0; b < 2; b++)
                    {
                        VShdr((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                        for (int a = 3; a < Stride; a++)
                            VERTEX_DATA[b * Stride + a] = *(p + (index * FaceStride) + b * ReadStride + a);
                    }

                for (int b = 0; b < 2; b++)
                {
                    float X = *(VERTEX_DATA + b * Stride + 0) - cX;
                    float Y = *(VERTEX_DATA + b * Stride + 1) - cY;
                    float Z = *(VERTEX_DATA + b * Stride + 2) - cZ;

                    float fiX = (X) * coZ - (Z) * sZ;
                    float fiZ = (Z) * coZ + (X) * sZ;
                    float ndY = (Y) * coY + (fiZ) * sY;

                    //Returns the newly rotated Vector
                    *(VERTEX_DATA + b * Stride + 0) = (fiX) * coX - (ndY) * sX;
                    *(VERTEX_DATA + b * Stride + 1) = (ndY) * coX + (fiX) * sX;
                    *(VERTEX_DATA + b * Stride + 2) = (fiZ) * coY - (Y) * sY;
                }
            }
            else
            {
                if (COPY_ATTRIB_MANUAL)
                    for (int b = 0; b < 2; b++)
                        VShdr((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                else for (int b = 0; b < 2; b++)
                    {
                        VShdr((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                        for (int a = 3; a < Stride; a++)
                            VERTEX_DATA[b * Stride + a] = *(p + (index * FaceStride) + b * ReadStride + a);
                    }
            }

            bool A = false;
            bool B = false;
            
            //NearZ Clipping
            A = VERTEX_DATA[2] < nearZ;
            B = VERTEX_DATA[2 + Stride] < nearZ;

            if (A && B) return;

            if (A) FIPA(VERTEX_DATA, 0, VERTEX_DATA, 0, 1, nearZ);
            if (B) FIPA(VERTEX_DATA, Stride, VERTEX_DATA, 0, 1, nearZ);

            //FarZ Clipping
            A = VERTEX_DATA[2] > farZ;
            B = VERTEX_DATA[2 + Stride] > farZ;

            if (A && B) return;

            if (A) FIPA(VERTEX_DATA, 0, VERTEX_DATA, 0, 1, farZ);
            if (B) FIPA(VERTEX_DATA, Stride, VERTEX_DATA, 0, 1, farZ);

            //RightFOV
            A = VERTEX_DATA[2] * tanVert + ow < VERTEX_DATA[0];
            B = VERTEX_DATA[Stride + 2] * tanVert + ow < VERTEX_DATA[Stride];

            if (A && B) return;

            if (A) SIPA(VERTEX_DATA, 0, VERTEX_DATA, 0, 1, tanVert);
            if (B) SIPA(VERTEX_DATA, Stride, VERTEX_DATA, 0, 1, tanVert);

            //LeftFOV
            A = VERTEX_DATA[2] * -tanVert - ow > VERTEX_DATA[0];
            B = VERTEX_DATA[Stride + 2] * -tanVert - ow > VERTEX_DATA[Stride];

            if (A && B) return;

            if (A) SIPA(VERTEX_DATA, 0, VERTEX_DATA, 0, 1, -tanVert, true);
            if (B) SIPA(VERTEX_DATA, Stride, VERTEX_DATA, 0, 1, -tanVert, true);

            //TopFOV
            A = VERTEX_DATA[2] * tanHorz + oh < VERTEX_DATA[1];
            B = VERTEX_DATA[2 + Stride] * tanHorz + oh < VERTEX_DATA[1 + Stride];

            if (A && B) return;

            if (A) SIPHA(VERTEX_DATA, 0, VERTEX_DATA, 0, 1, tanHorz);
            if (B) SIPHA(VERTEX_DATA, Stride, VERTEX_DATA, 0, 1, tanHorz);

            //BottomFOV
            A = VERTEX_DATA[2] * -tanHorz - oh > VERTEX_DATA[1];
            B = VERTEX_DATA[2 + Stride] * -tanHorz - oh > VERTEX_DATA[1 + Stride];

            if (A && B) return;

            if (A) SIPHA(VERTEX_DATA, 0, VERTEX_DATA, 0, 1, -tanHorz, true);
            if (B) SIPHA(VERTEX_DATA, Stride, VERTEX_DATA, 0, 1, -tanHorz, true);

            //XYZtoXY
            float* Sspace = stackalloc float[(Stride - 3) * 3 + ATTRIBLVL];

            #region CameraSpaceToScreenSpace
            if (matrixlerpv == 0)
                for (int im = 0; im < 2; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + (VERTEX_DATA[im * Stride + 0] / VERTEX_DATA[im * Stride + 2]) * fw;
                    VERTEX_DATA[im * Stride + 1] = rh + (VERTEX_DATA[im * Stride + 1] / VERTEX_DATA[im * Stride + 2]) * fh;
                    VERTEX_DATA[im * Stride + 2] = 1f / (VERTEX_DATA[im * Stride + 2]);
                }
            else if (matrixlerpv == 1)
                for (int im = 0; im < 2; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + VERTEX_DATA[im * Stride + 0] / ox;
                    VERTEX_DATA[im * Stride + 1] = rh + VERTEX_DATA[im * Stride + 1] / oy;
                }
            else
                for (int im = 0; im < 2; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + VERTEX_DATA[im * Stride + 0] / ((VERTEX_DATA[im * Stride + 2] * fwi - ox) * (1f - matrixlerpv) + ox);
                    VERTEX_DATA[im * Stride + 1] = rh + VERTEX_DATA[im * Stride + 1] / ((VERTEX_DATA[im * Stride + 2] * fhi - oy) * (1f - matrixlerpv) + oy);
                    VERTEX_DATA[im * Stride + 2] = 1f / (VERTEX_DATA[im * Stride + 2] + oValue);
                }
            #endregion

           // DrawLineLATE(VERTEX_DATA, VERTEX_DATA + Stride);
            if (attribdata) DrawLineDATA(VERTEX_DATA, VERTEX_DATA + Stride, Sspace, index);
            else DrawLineFull(VERTEX_DATA, VERTEX_DATA + Stride, Sspace, index);
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

        unsafe bool ScanLineTEST(int Line, float* TRIS_DATA, int TRIS_SIZE, float* Intersects)
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

      
        #region ScreenSpaceInterpolation
        unsafe void LIP(float* XR, int I, float* V_DATA, int A, int B, int LinePos)
        {
            float X;
            float Z;

            A *= 3;
            B *= 3;

            if (V_DATA[A + 1] == LinePos)
            {
                XR[I] = V_DATA[A];
                XR[2 + I] = V_DATA[A + 2];
                return;
            }

            if (V_DATA[B + 1] == LinePos)
            {
                XR[I] = V_DATA[B];
                XR[2 + I] = V_DATA[B + 2];
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

            XR[I] = X;
            XR[2 + I] = Z;
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

        #endregion

        #region FindIntersectPoint
        unsafe void FIP_te(float* TA, int INDEX, float* VD, int A, int B, float LinePos)
        {
            float X;
            float Y;

            A *= 3;
            B *= 3;

            if (VD[A + 1] - VD[B + 1] != 0)
            {
                float slope = (VD[A + 2] - VD[B + 2]) / (VD[A + 1] - VD[B + 1]);
                float b = -slope * VD[A + 1] + VD[A + 2];
                Y = (LinePos - b) / slope;
            }
            else
            {
                Y = VD[A + 1];
            }


            if (VD[A + 0] - VD[B + 0] != 0)
            {
                float slope = (VD[A + 2] - VD[B + 2]) / (VD[A + 0] - VD[B + 0]);
                float b = -slope * VD[A + 0] + VD[A + 2];
                X = (LinePos - b) / slope;
            }
            else
            {
                X = VD[A + 0];
            }

            TA[INDEX] = X;
            TA[INDEX + 1] = Y;
            TA[INDEX + 2] = LinePos;
        }

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

        unsafe float BACKFACECULL3(float* VERTEX_DATA)
        {
            //return ((VERTEX_DATA[3] - rw) - (VERTEX_DATA[0] - rw)) * ((VERTEX_DATA[7] - rh) - (VERTEX_DATA[1] - rh)) - ((VERTEX_DATA[6] - rw) - (VERTEX_DATA[0] - rw)) * ((VERTEX_DATA[4] - rh) - (VERTEX_DATA[1] - rh));
            return ((VERTEX_DATA[3]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[7]) - (VERTEX_DATA[1])) - ((VERTEX_DATA[6]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[4]) - (VERTEX_DATA[1]));
        }

        unsafe float BACKFACECULLS(float* VERTEX_DATA)
        {
            return ((VERTEX_DATA[Stride]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[Stride * 2 + 1]) - (VERTEX_DATA[1])) - ((VERTEX_DATA[Stride * 2]) - (VERTEX_DATA[0])) * ((VERTEX_DATA[Stride + 1]) - (VERTEX_DATA[1]));
        }

        #region WireFrameDrawing
        void LateWireFrame(float* DATA, int BUFFER_SIZE)
        {
            for (int i = 0; i < BUFFER_SIZE - 1; i++)
                DrawLineLATE(DATA + Stride * i, DATA + Stride * (i + 1));

            DrawLineLATE(DATA, DATA + Stride * (BUFFER_SIZE - 1));
        }

        internal void DrawLine3D(Vector3 T, Vector3 F)
        {
            float* VERTEX_DATA = stackalloc float[6];
            VERTEX_DATA[0] = T.x;
            VERTEX_DATA[1] = T.y;
            VERTEX_DATA[2] = T.z;
            VERTEX_DATA[3] = F.x;
            VERTEX_DATA[4] = F.y;
            VERTEX_DATA[5] = F.z;

            for (int b = 0; b < 2; b++)
            {
                float X = VERTEX_DATA[Stride * b] - cX;
                float Y = VERTEX_DATA[Stride * b + 1] - cY;
                float Z = VERTEX_DATA[Stride * b + 2] - cZ;

                float fiX = (X) * coZ - (Z) * sZ;
                float fiZ = (Z) * coZ + (X) * sZ;
                float ndY = (Y) * coY + (fiZ) * sY;

                //Returns the newly rotated Vector
                *(VERTEX_DATA + b * 3 + 0) = (fiX) * coX - (ndY) * sX;
                *(VERTEX_DATA + b * 3 + 1) = (ndY) * coX + (fiX) * sX;
                *(VERTEX_DATA + b * 3 + 2) = (fiZ) * coY - (Y) * sY;
            }

            bool A = false;
            bool B = false;

            A = VERTEX_DATA[2] < nearZ;
            B = VERTEX_DATA[2 + Stride] < nearZ;

            if (A && B) return;

            if (A) FIPA(VERTEX_DATA, 0, VERTEX_DATA, 0, 1, nearZ);
            if (B) FIPA(VERTEX_DATA, Stride, VERTEX_DATA, 0, 1, nearZ);


            A = VERTEX_DATA[2] >= farZ;
            B = VERTEX_DATA[2 + Stride] >= farZ;

            if (A && B) return;

            if (A) FIPA(VERTEX_DATA, 0, VERTEX_DATA, 0, 1, farZ);
            if (B) FIPA(VERTEX_DATA, Stride, VERTEX_DATA, 0, 1, farZ);


            //RightFOV
            A = VERTEX_DATA[2] * tanVert + ow < VERTEX_DATA[0];
            B = VERTEX_DATA[Stride + 2] * tanVert + ow < VERTEX_DATA[Stride];

            if (A && B) return;

            if (A) SIPA(VERTEX_DATA, 0, VERTEX_DATA, 0, 1, tanVert);
            if (B) SIPA(VERTEX_DATA, Stride, VERTEX_DATA, 0, 1, tanVert);

            //LeftFOV
            A = VERTEX_DATA[2] * -tanVert - ow > VERTEX_DATA[0];
            B = VERTEX_DATA[Stride + 2] * -tanVert - ow > VERTEX_DATA[Stride];

            if (A && B) return;

            if (A) SIPA(VERTEX_DATA, 0, VERTEX_DATA, 0, 1, -tanVert, true);
            if (B) SIPA(VERTEX_DATA, Stride, VERTEX_DATA, 0, 1, -tanVert, true);

            //TopFOV
            A = VERTEX_DATA[2] * tanHorz + oh < VERTEX_DATA[1];
            B = VERTEX_DATA[2 + Stride] * tanHorz + oh < VERTEX_DATA[1 + Stride];

            if (A && B) return;

            if (A) SIPHA(VERTEX_DATA, 0, VERTEX_DATA, 0, 1, tanHorz);
            if (B) SIPHA(VERTEX_DATA, Stride, VERTEX_DATA, 0, 1, tanHorz);

            //BottomFOV
            A = VERTEX_DATA[2] * -tanHorz - oh > VERTEX_DATA[1];
            B = VERTEX_DATA[2 + Stride] * -tanHorz - oh > VERTEX_DATA[1 + Stride];

            if (A && B) return;

            if (A) SIPHA(VERTEX_DATA, 0, VERTEX_DATA, 0, 1, -tanHorz, true);
            if (B) SIPHA(VERTEX_DATA, Stride, VERTEX_DATA, 0, 1, -tanHorz, true);

            #region CameraSpaceToScreenSpace
            if (matrixlerpv == 0)
                for (int im = 0; im < 2; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + (VERTEX_DATA[im * Stride + 0] / VERTEX_DATA[im * Stride + 2]) * fw;
                    VERTEX_DATA[im * Stride + 1] = rh + (VERTEX_DATA[im * Stride + 1] / VERTEX_DATA[im * Stride + 2]) * fh;
                    VERTEX_DATA[im * Stride + 2] = 1f / (VERTEX_DATA[im * Stride + 2]);
                }
            else if (matrixlerpv == 1)
                for (int im = 0; im < 2; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + VERTEX_DATA[im * Stride + 0] / ox;
                    VERTEX_DATA[im * Stride + 1] = rh + VERTEX_DATA[im * Stride + 1] / oy;
                }
            else
                for (int im = 0; im < 2; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + VERTEX_DATA[im * Stride + 0] / ((VERTEX_DATA[im * Stride + 2] * fwi - ox) * (1f - matrixlerpv) + ox);
                    VERTEX_DATA[im * Stride + 1] = rh + VERTEX_DATA[im * Stride + 1] / ((VERTEX_DATA[im * Stride + 2] * fhi - oy) * (1f - matrixlerpv) + oy);
                    VERTEX_DATA[im * Stride + 2] = 1f / (VERTEX_DATA[im * Stride + 2] + oValue);
                }
            #endregion

            DrawLineLATE(VERTEX_DATA, VERTEX_DATA + Stride);
        }

        internal unsafe void DrawLineAA(float fromX, float fromY, float toX, float toY)
        {
            if (fromX == toX & fromY == toY)
                return;

            // Buffer OverFlow Protection will still be needed regardless how polished the code is...
            float aa = (fromX - toX);
            float ba = (fromY - toY);

            if (aa * aa > ba * ba)
            {
                float slope = (fromY - toY) / (fromX - toX);
                float b = -slope * fromX + fromY;

                if (fromX < toX)
                    for (int i = (int)fromX; i <= toX; i++)
                    {
                        float ty = ((float)i * slope + b);
                        int Floor = (int)ty;
                        if (i < 0 || Floor < 0 || ty >= renderHeight || i >= renderWidth) continue;

                        float MB = (float)Floor + 1f - ty;
                        float MT = ty - (float)Floor;

                        byte* lptr = bptr + (Floor * wsD + (i * sD));

                     //   *(lptr + 0) = (byte)((*(lptr + 0) * (1f - MB)) + MB * dB);
                     //   *(lptr + 1) = (byte)((*(lptr + 1) * (1f - MB)) + MB * dG);
                    //    *(lptr + 2) = (byte)((*(lptr + 2) * (1f - MB)) + MB * dR);

                        *(iptr + renderWidth * Floor + i) = (((
                            (byte)((*(lptr + 2) * (1f - MB)) + MB * dR) << 8) | 
                            (byte)((*(lptr + 1) * (1f - MB)) + MB * dG)) << 8) | 
                            (byte)((*(lptr + 0) * (1f - MB)) + MB * dB);

                        if (Floor + 1 >= renderHeight) continue;
                        lptr = bptr + ((Floor + 1) * wsD + (i * sD));

                     //   *(lptr + 0) = (byte)(*(lptr + 0) * (1f - MT) + MT * dB);
                     //   *(lptr + 1) = (byte)(*(lptr + 1) * (1f - MT) + MT * dG);
                     //   *(lptr + 2) = (byte)(*(lptr + 2) * (1f - MT) + MT * dR);

                        *(iptr + renderWidth * (Floor + 1) + i) = (((
                            (byte)((*(lptr + 2) * (1f - MT)) + MT * dR) << 8) |
                            (byte)((*(lptr + 1) * (1f - MT)) + MT * dG)) << 8) |
                            (byte)((*(lptr + 0) * (1f - MT)) + MT * dB);

                    }
                else
                    for (int i = (int)toX; i <= fromX; i++)
                    {
                        float ty = ((float)i * slope + b);
                        int Floor = (int)ty;
                        if (i < 0 || Floor < 0 || ty >= renderHeight || i >= renderWidth) continue;

                        float MB = (float)Floor + 1f - ty;
                        float MT = ty - (float)Floor;

                        byte* lptr = bptr + (Floor * wsD + (i * sD));

                        *(iptr + renderWidth * Floor + i) = (((
                            (byte)((*(lptr + 2) * (1f - MB)) + MB * dR) << 8) |
                            (byte)((*(lptr + 1) * (1f - MB)) + MB * dG)) << 8) |
                            (byte)((*(lptr + 0) * (1f - MB)) + MB * dB);


                       // *(lptr + 0) = (byte)((*(lptr + 0) * (1f - MB)) + MB * dB);
                       // *(lptr + 1) = (byte)((*(lptr + 1) * (1f - MB)) + MB * dG);
                      //  *(lptr + 2) = (byte)((*(lptr + 2) * (1f - MB)) + MB * dR);

                        if (Floor + 1 >= renderHeight) continue;
                        lptr = bptr + ((Floor + 1) * wsD + (i * sD));

                      //  *(lptr + 0) = (byte)(*(lptr + 0) * (1f - MT) + MT * dB);
                      //  *(lptr + 1) = (byte)(*(lptr + 1) * (1f - MT) + MT * dG);
                      //  *(lptr + 2) = (byte)(*(lptr + 2) * (1f - MT) + MT * dR);

                        *(iptr + renderWidth * (Floor + 1) + i) = (((
                           (byte)((*(lptr + 2) * (1f - MT)) + MT * dR) << 8) |
                           (byte)((*(lptr + 1) * (1f - MT)) + MT * dG)) << 8) |
                           (byte)((*(lptr + 0) * (1f - MT)) + MT * dB);
                    }
            }
            else
            {
                float slope = (fromX - toX) / (fromY - toY);
                float b = -slope * fromY + fromX;

                if (fromY < toY)
                    for (int i = (int)fromY; i <= toY; i++)
                    {
                        float ty = ((float)i * slope + b);
                        int Floor = (int)ty;
                        if (i < 0 || Floor < 0 || Floor >= renderWidth || i >= renderHeight) continue;

                        float MB = (float)Floor + 1f - ty;
                        float MT = ty - (float)Floor;

                        byte* lptr = bptr + (i * wsD + (Floor * sD));
                        
                     //   *(lptr + 0) = (byte)(*(lptr + 0) * (1f - MB) + MB * dB);
                     //   *(lptr + 1) = (byte)(*(lptr + 1) * (1f - MB) + MB * dG);
                     //   *(lptr + 2) = (byte)(*(lptr + 2) * (1f - MB) + MB * dR);

                        *(iptr + renderWidth * i + Floor) = (((
                            (byte)((*(lptr + 2) * (1f - MB)) + MB * dR) << 8) |
                            (byte)((*(lptr + 1) * (1f - MB)) + MB * dG)) << 8) |
                            (byte)((*(lptr + 0) * (1f - MB)) + MB * dB);

                        if (Floor + 1 >= renderWidth) continue;

                        lptr = bptr + (i * wsD + ((Floor + 1) * sD));
                     //   *(lptr + 0) = (byte)(*(lptr + 0) * (1f - MT) + MT * dB);
                    //    *(lptr + 1) = (byte)(*(lptr + 1) * (1f - MT) + MT * dG);
                    //    *(lptr + 2) = (byte)(*(lptr + 2) * (1f - MT) + MT * dR);

                        *(iptr + renderWidth * i + Floor + 1) = (((
                           (byte)((*(lptr + 2) * (1f - MT)) + MT * dR) << 8) |
                           (byte)((*(lptr + 1) * (1f - MT)) + MT * dG)) << 8) |
                           (byte)((*(lptr + 0) * (1f - MT)) + MT * dB);
                    }
                else
                    for (int i = (int)toY; i <= fromY; i++)
                    {
                        float ty = ((float)i * slope + b);
                        int Floor = (int)ty;
                        if (i < 0 || Floor < 0 || Floor >= renderWidth || i >= renderHeight) continue;

                        float MB = (float)Floor + 1f - ty;
                        float MT = ty - (float)Floor;

                        byte* lptr = bptr + (i * wsD + (Floor * sD));

                      //  *(lptr + 0) = (byte)(*(lptr + 0) * (1f - MB) + MB * dB);
                      //  *(lptr + 1) = (byte)(*(lptr + 1) * (1f - MB) + MB * dG);
                      //  *(lptr + 2) = (byte)(*(lptr + 2) * (1f - MB) + MB * dR);

                        *(iptr + renderWidth * i + Floor) = (((
                            (byte)((*(lptr + 2) * (1f - MB)) + MB * dR) << 8) |
                            (byte)((*(lptr + 1) * (1f - MB)) + MB * dG)) << 8) |
                            (byte)((*(lptr + 0) * (1f - MB)) + MB * dB);

                        if (Floor + 1 >= renderWidth) continue;

                        lptr = bptr + (i * wsD + ((Floor + 1) * sD));
                       // *(lptr + 0) = (byte)(*(lptr + 0) * (1f - MT) + MT * dB);
                       // *(lptr + 1) = (byte)(*(lptr + 1) * (1f - MT) + MT * dG);
                       // *(lptr + 2) = (byte)(*(lptr + 2) * (1f - MT) + MT * dR);

                        *(iptr + renderWidth * i + Floor + 1) = (((
                           (byte)((*(lptr + 2) * (1f - MT)) + MT * dR) << 8) |
                           (byte)((*(lptr + 1) * (1f - MT)) + MT * dG)) << 8) |
                           (byte)((*(lptr + 0) * (1f - MT)) + MT * dB);
                    }
            }
        }

        internal unsafe void DrawLine(float fromX, float fromY, float toX, float toY)
        {
            if (fromX == toX & fromY == toY)
                return;

            // Buffer OverFlow Protection will still be needed regardless how polished the code is...
            float aa = (fromX - toX);
            float ba = (fromY - toY);

            if (aa * aa > ba * ba)
            {
                float slope = (fromY - toY) / (fromX - toX);
                float b = -slope * fromX + fromY;

                if (fromX < toX)
                    for (int i = (int)fromX; i <= toX; i++)
                    {
                        int tY = (int)(i * slope + b);
                        if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue;

                        *(iptr + renderWidth * tY + i) = diValue;
                    }
                else
                    for (int i = (int)toX; i <= fromX; i++)
                    {
                        int tY = (int)(i * slope + b);
                        if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue;

                        *(iptr + renderWidth * tY + i) = diValue;
                    }
            }
            else
            {
                float slope = (fromX - toX) / (fromY - toY);
                float b = -slope * fromY + fromX;

                if (fromY < toY)
                    for (int i = (int)fromY; i <= toY; i++)
                    {
                        int tY = (int)(i * slope + b);
                        if (i < 0 || tY < 0 || tY >= renderWidth || i >= renderHeight) continue;

                        *(iptr + renderWidth * i + tY) = diValue;
                    }
                else


                    for (int i = (int)toY; i <= fromY; i++)
                    {
                        int tY = (int)(i * slope + b);
                        if (i < 0 || tY < 0 || tY >= renderWidth || i >= renderHeight) continue;

                        *(iptr + renderWidth * i + tY) = diValue;
                    }

            }
        }

        internal unsafe void DrawLine(float fromX, float fromY, float toX, float toY, byte B, byte G, byte R)
        {
            if (fromX == toX & fromY == toY)
                return;

            // Buffer OverFlow Protection will still be needed regardless how polished the code is...
            float aa = (fromX - toX);
            float ba = (fromY - toY);

            if (aa * aa > ba * ba)
            {
                float slope = (fromY - toY) / (fromX - toX);
                float b = -slope * fromX + fromY;

                if (fromX < toX)
                    for (int i = (int)fromX; i <= toX; i++)
                    {
                        int tY = (int)(i * slope + b);

                        if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue;

                        *(bptr + (tY * wsD + (i * sD) + 0)) = B;
                        *(bptr + (tY * wsD + (i * sD) + 1)) = G;
                        *(bptr + (tY * wsD + (i * sD) + 2)) = R;

                    }
                else
                    for (int i = (int)toX; i <= fromX; i++)
                    {
                        int tY = (int)(i * slope + b);

                        if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue;

                        *(bptr + (tY * wsD + (i * sD) + 0)) = B;
                        *(bptr + (tY * wsD + (i * sD) + 1)) = G;
                        *(bptr + (tY * wsD + (i * sD) + 2)) = R;

                    }
            }
            else
            {
                float slope = (fromX - toX) / (fromY - toY);
                float b = -slope * fromY + fromX;

                if (fromY < toY)
                    for (int i = (int)fromY; i <= toY; i++)
                    {
                        int tY = (int)(i * slope + b);
                        if (i < 0 || tY < 0 || tY >= renderWidth || i >= renderHeight) continue;
                        
                        *(bptr + (i * wsD + (tY * sD) + 0)) = B;
                        *(bptr + (i * wsD + (tY * sD) + 1)) = G;
                        *(bptr + (i * wsD + (tY * sD) + 2)) = R;
                    }
                else


                    for (int i = (int)toY; i <= fromY; i++)
                    {
                        int tY = (int)(i * slope + b);

                        if (i < 0 || tY < 0 || tY >= renderWidth || i >= renderHeight) continue;

                        *(bptr + (i * wsD + (tY * sD) + 0)) = B;
                        *(bptr + (i * wsD + (tY * sD) + 1)) = G;
                        *(bptr + (i * wsD + (tY * sD) + 2)) = R;
                    }
            }
        }

        internal unsafe void DrawLineFull(float* FromDATA, float* ToDATA, float* ScratchSpace, int Index)
        {
            if (FromDATA[0] == ToDATA[0] & FromDATA[1] == ToDATA[1])
                return;

            float aa = (FromDATA[0] - ToDATA[0]);
            float ba = (FromDATA[1] - ToDATA[1]);

           // throw new Exception("BREAK POINT");
            if (aa * aa > ba * ba)
            {
                float slope = (FromDATA[1] - ToDATA[1]) / (FromDATA[0] - ToDATA[0]);
                float b = -slope * FromDATA[0] + FromDATA[1];

                float slopeZ = (FromDATA[2] - ToDATA[2]) / (FromDATA[0] - ToDATA[0]);
                float bZ = -slopeZ * FromDATA[0] + FromDATA[2];

                for (int s = 3; s < Stride; s++)
                {
                    ScratchSpace[(s - 3) * 2] = (FromDATA[s] - ToDATA[s]) / (1f / FromDATA[2] - 1f / ToDATA[3]);
                    ScratchSpace[(s - 3) * 2 + 1] = -ScratchSpace[(s - 3) * 2] / FromDATA[2] + ToDATA[s];
                }

                if (FromDATA[0] < ToDATA[0])
                    for (int i = (int)FromDATA[0]; i <= ToDATA[0]; i++)
                    {
                        int tY = (int)(i * slope + b);
                        float s = farZ - (1f / ((slopeZ * (float)i + bZ)));
                        if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue;

                        if (dptr[renderWidth * tY + i] > s - zoffset) continue;
                        dptr[renderWidth * tY + i] = s;

                        for (int z = 0; z < Stride - 3; z++)
                            ScratchSpace[(Stride - 3) * 2 + z] = (ScratchSpace[z * 2] / (slopeZ * (float)i + bZ) + ScratchSpace[z * 2 + 1]);

                        SAdv(bptr + (tY * wsD + (i * sD)), ScratchSpace + (Stride - 3) * 2, Index);
                    }
                else
                    for (int i = (int)ToDATA[0]; i <= FromDATA[0]; i++)
                    {
                        int tY = (int)(i * slope + b);
                        float s = farZ - (1f / ((slopeZ * (float)i + bZ)));
                        if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue;

                        if (dptr[renderWidth * tY + i] > s - zoffset) continue;
                        dptr[renderWidth * tY + i] = s;

                        for (int z = 0; z < Stride - 3; z++)
                            ScratchSpace[(Stride - 3) * 2 + z] = (ScratchSpace[z * 2] / (slopeZ * (float)i + bZ) + ScratchSpace[z * 2 + 1]);

                        SAdv(bptr + (tY * wsD + (i * sD)), ScratchSpace + (Stride - 3) * 2, Index);
                    }
            }
            else
            {
                float slope = (FromDATA[0] - ToDATA[0]) / (FromDATA[1] - ToDATA[1]);
                float b = -slope * FromDATA[1] + FromDATA[0];

                float slopeZ = (FromDATA[2] - ToDATA[2]) / (FromDATA[1] - ToDATA[1]);
                float bZ = -slopeZ * FromDATA[1] + FromDATA[2];

                for (int s = 3; s < Stride; s++)
                {
                    ScratchSpace[(s - 3) * 2] = (FromDATA[s] - ToDATA[s]) / (1f / FromDATA[2] - 1f / ToDATA[3]);
                    ScratchSpace[(s - 3) * 2 + 1] = -ScratchSpace[(s - 3) * 2] / FromDATA[2] + ToDATA[s];
                }

                if (FromDATA[1] < ToDATA[1])
                    for (int i = (int)FromDATA[1]; i <= ToDATA[1]; i++)
                    {
                        int tY = (int)(i * slope + b);
                        float s = farZ - (1f / ((slopeZ * (float)i + bZ)));
                        if (i < 0 || tY < 0 || tY >= renderWidth || i >= renderHeight) continue;

                        if (dptr[renderWidth * i + tY] > s - zoffset) continue;
                        dptr[renderWidth * i + tY] = s;

                        for (int z = 0; z < Stride - 3; z++)
                            ScratchSpace[(Stride - 3) * 2 + z] = (ScratchSpace[z * 2] / (slopeZ * (float)i + bZ) + ScratchSpace[z * 2 + 1]);

                        SAdv(bptr + (i * wsD + (tY * sD)), ScratchSpace + (Stride - 3) * 2, Index);
                    }
                else
                    for (int i = (int)ToDATA[1]; i <= FromDATA[1]; i++)
                    {
                        int tY = (int)(i * slope + b);
                        float s = farZ - (1f / ((slopeZ * (float)i + bZ)));
                        if (i < 0 || tY < 0 || tY >= renderWidth || i >= renderHeight) continue;

                        if (dptr[renderWidth * i + tY] > s - zoffset) continue;
                        dptr[renderWidth * i + tY] = s;

                        for (int z = 0; z < Stride - 3; z++)
                            ScratchSpace[(Stride - 3) * 2 + z] = (ScratchSpace[z * 2] / (slopeZ * (float)i + bZ) + ScratchSpace[z * 2 + 1]);

                        SAdv(bptr + (i * wsD + (tY * sD)), ScratchSpace + (Stride - 3) * 2, Index);
                    }
            }
        }

        internal unsafe void DrawLineDATA(float* FromDATA, float* ToDATA, float* ScratchSpace, int Index)
        {
            if (FromDATA[0] == ToDATA[0] & FromDATA[1] == ToDATA[1])
                return;

            float* ATB = ScratchSpace + (Stride - 3) * 3;
            float aa = (FromDATA[0] - ToDATA[0]);
            float ba = (FromDATA[1] - ToDATA[1]);
            float zz;

            // throw new Exception("BREAK POINT");
            if (aa * aa > ba * ba)
            {
                float slope = (FromDATA[1] - ToDATA[1]) / (FromDATA[0] - ToDATA[0]);
                float b = -slope * FromDATA[0] + FromDATA[1];

                float slopeZ = (FromDATA[2] - ToDATA[2]) / (FromDATA[0] - ToDATA[0]);
                float bZ = -slopeZ * FromDATA[0] + FromDATA[2];

                for (int s = 3; s < Stride; s++)
                {
                    ScratchSpace[(s - 3) * 2] = (FromDATA[s] - ToDATA[s]) / (1f / FromDATA[2] - 1f / ToDATA[3]);
                    ScratchSpace[(s - 3) * 2 + 1] = -ScratchSpace[(s - 3) * 2] / FromDATA[2] + ToDATA[s];
                }

                if (FromDATA[0] < ToDATA[0])
                    for (int i = (int)FromDATA[0]; i <= ToDATA[0]; i++)
                    {
                        int tY = (int)(i * slope + b);
                        if (cmatrix) zz = (1f / (slopeZ * (float)i + bZ) - oValue);
                        else zz =  (slopeZ * (float)i + bZ);
                      //  float zz = (1f / ((slopeZ * (float)i + bZ)));

                        float s = farZ - zz;
                        if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue;

                        if (dptr[renderWidth * tY + i] > s - zoffset) continue;
                        dptr[renderWidth * tY + i] = s;

                        if (ATTRIBLVL == 3)
                        {
                            ATB[0] = ((zz * fwi - ox) * matrixlerpo + ox) * (i - rw);
                            ATB[1] = ((zz * fhi - oy) * matrixlerpo + oy) * (tY - rh);
                            ATB[2] = zz;
                        }
                        else if (ATTRIBLVL == 5)
                        {
                            ATB[0] = ((zz * fwi - ox) * matrixlerpo + ox) * (i - rw);
                            ATB[1] = ((zz * fhi - oy) * matrixlerpo + oy) * (tY - rh);
                            ATB[2] = zz;
                            ATB[3] = i;
                            ATB[4] = tY;
                        }
                        else if (ATTRIBLVL == 2)
                        {
                            ATB[0] = i;
                            ATB[1] = tY;
                        }
                        else if (ATTRIBLVL == 1)
                        {
                            ATB[0] = zz;
                        }

                        for (int z = 0; z < Stride - 3; z++)
                            ScratchSpace[(Stride - 3) * 2 + z] = (ScratchSpace[z * 2] / (slopeZ * (float)i + bZ) + ScratchSpace[z * 2 + 1]);

                        SAdv(bptr + (tY * wsD + (i * sD)), ScratchSpace + (Stride - 3) * 2, Index);
                    }
                else
                    for (int i = (int)ToDATA[0]; i <= FromDATA[0]; i++)
                    {
                        int tY = (int)(i * slope + b);
                        if (cmatrix) zz = (1f / (slopeZ * (float)i + bZ) - oValue);
                        else zz = (slopeZ * (float)i + bZ);

                        float s = farZ - zz;

                        if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue;

                        if (dptr[renderWidth * tY + i] > s - zoffset) continue;
                        dptr[renderWidth * tY + i] = s;

                        if (ATTRIBLVL == 3)
                        {
                            ATB[0] = ((zz * fwi - ox) * matrixlerpo + ox) * (i - rw);
                            ATB[1] = ((zz * fhi - oy) * matrixlerpo + oy) * (tY - rh);
                            ATB[2] = zz;
                        }
                        else if (ATTRIBLVL == 5)
                        {
                            ATB[0] = ((i - rw) / fw) * zz;
                            ATB[1] = ((tY - rh) / fh) * zz;
                            ATB[2] = zz;
                            ATB[3] = i;
                            ATB[4] = tY;
                        }
                        else if (ATTRIBLVL == 2)
                        {
                            ATB[0] = i;
                            ATB[1] = tY;
                        }
                        else if (ATTRIBLVL == 1)
                        {
                            ATB[0] = zz;
                        }

                        for (int z = 0; z < Stride - 3; z++)
                            ScratchSpace[(Stride - 3) * 2 + z] = (ScratchSpace[z * 2] / (slopeZ * (float)i + bZ) + ScratchSpace[z * 2 + 1]);

                        SAdv(bptr + (tY * wsD + (i * sD)), ScratchSpace + (Stride - 3) * 2, Index);
                    }
            }
            else
            {
                float slope = (FromDATA[0] - ToDATA[0]) / (FromDATA[1] - ToDATA[1]);
                float b = -slope * FromDATA[1] + FromDATA[0];

                float slopeZ = (FromDATA[2] - ToDATA[2]) / (FromDATA[1] - ToDATA[1]);
                float bZ = -slopeZ * FromDATA[1] + FromDATA[2];

                for (int s = 3; s < Stride; s++)
                {
                    ScratchSpace[(s - 3) * 2] = (FromDATA[s] - ToDATA[s]) / (1f / FromDATA[2] - 1f / ToDATA[3]);
                    ScratchSpace[(s - 3) * 2 + 1] = -ScratchSpace[(s - 3) * 2] / FromDATA[2] + ToDATA[s];
                }

                if (FromDATA[1] < ToDATA[1])
                    for (int i = (int)FromDATA[1]; i <= ToDATA[1]; i++)
                    {
                        int tY = (int)(i * slope + b);

                        if (cmatrix) zz = (1f / (slopeZ * (float)i + bZ) - oValue);
                        else zz = (slopeZ * (float)i + bZ);

                        float s = farZ - zz;
                        if (i < 0 || tY < 0 || tY >= renderWidth || i >= renderHeight) continue;

                        if (dptr[renderWidth * i + tY] > s - zoffset) continue;
                        dptr[renderWidth * i + tY] = s;

                        if (ATTRIBLVL == 3)
                        {
                            ATB[0] = ((zz * fwi - ox) * matrixlerpo + ox) * (tY - rw);
                            ATB[1] = ((zz * fhi - oy) * matrixlerpo + oy) * (i - rh);
                            ATB[2] = zz;
                        }
                        else if (ATTRIBLVL == 5)
                        {
                            ATB[0] = ((zz * fwi - ox) * matrixlerpo + ox) * (tY - rw);
                            ATB[1] = ((zz * fhi - oy) * matrixlerpo + oy) * (i - rh);
                            ATB[2] = zz;
                            ATB[3] = i;
                            ATB[4] = tY;
                        }
                        else if (ATTRIBLVL == 2)
                        {
                            ATB[0] = tY;
                            ATB[1] = i;
                        }
                        else if (ATTRIBLVL == 1)
                        {
                            ATB[0] = zz;
                        }

                        for (int z = 0; z < Stride - 3; z++)
                            ScratchSpace[(Stride - 3) * 2 + z] = (ScratchSpace[z * 2] / (slopeZ * (float)i + bZ) + ScratchSpace[z * 2 + 1]);

                        SAdv(bptr + (i * wsD + (tY * sD)), ScratchSpace + (Stride - 3) * 2, Index);
                    }
                else
                    for (int i = (int)ToDATA[1]; i <= FromDATA[1]; i++)
                    {
                        int tY = (int)(i * slope + b);
                        if (cmatrix) zz = (1f / (slopeZ * (float)i + bZ) - oValue);
                        else zz = (slopeZ * (float)i + bZ);

                        float s = farZ - zz;
                        if (i < 0 || tY < 0 || tY >= renderWidth || i >= renderHeight) continue;



                        if (dptr[renderWidth * i + tY] > s - zoffset) continue;
                        dptr[renderWidth * i + tY] = s;

                        if (ATTRIBLVL == 3)
                        {
                            ATB[0] = ((zz * fwi - ox) * matrixlerpo + ox) * (tY - rw);
                            ATB[1] = ((zz * fhi - oy) * matrixlerpo + oy) * (i - rh);
                            ATB[2] = zz;
                        }
                        else if (ATTRIBLVL == 5)
                        {
                            ATB[0] = ((zz * fwi - ox) * matrixlerpo + ox) * (tY - rw);
                            ATB[1] = ((zz * fhi - oy) * matrixlerpo + oy) * (i - rh);
                            ATB[2] = zz;
                            ATB[3] = i;
                            ATB[4] = tY;
                        }
                        else if (ATTRIBLVL == 2)
                        {
                            ATB[0] = tY;
                            ATB[1] = i;
                        }
                        else if (ATTRIBLVL == 1)
                        {
                            ATB[0] = zz;
                        }

                        for (int z = 0; z < Stride - 3; z++)
                            ScratchSpace[(Stride - 3) * 2 + z] = (ScratchSpace[z * 2] / (slopeZ * (float)i + bZ) + ScratchSpace[z * 2 + 1]);

                        SAdv(bptr + (i * wsD + (tY * sD)), ScratchSpace + (Stride - 3) * 2, Index);
                    }
            }
        }

        internal unsafe void DrawLineLATE(float* FromDATA, float* ToDATA)
        {
            if (FromDATA[0] == ToDATA[0] & FromDATA[1] == ToDATA[1])
                return;

            float aa = (FromDATA[0] - ToDATA[0]);
            float ba = (FromDATA[1] - ToDATA[1]);

             //throw new Exception("BREAK POINT");
            if (aa * aa > ba * ba)
            {
                float slope = (FromDATA[1] - ToDATA[1]) / (float)((int)FromDATA[0] - (int)ToDATA[0]);
                float b = -slope * FromDATA[0] + FromDATA[1];

                float slopeZ = (FromDATA[2] - ToDATA[2]) / (float)((int)FromDATA[0] - (int)ToDATA[0]);
                float bZ = -slopeZ * FromDATA[0] + FromDATA[2];

                if (FromDATA[0] < ToDATA[0])
                    for (int i = (int)FromDATA[0]; i <= (int)ToDATA[0]; i++)
                    {
                        int tY = (int)(i * slope + b);

                        float s;
                        if (cmatrix) s = farZ -  (1f / (slopeZ * (float)i + bZ) - oValue);
                        else s = farZ - (slopeZ * (float)i + bZ);

                        if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue; 

                        if (dptr[renderWidth * tY + i] > s - zoffset) continue;
                        dptr[renderWidth * tY + i] = s;

                        *(bptr + (tY * wsD + (i * sD)) + 0) = lB;
                        *(bptr + (tY * wsD + (i * sD)) + 1) = lG;
                        *(bptr + (tY * wsD + (i * sD)) + 2) = lR;
                    }
                else
                    for (int i = (int)ToDATA[0]; i <= (int)FromDATA[0]; i++)
                    {
                        int tY = (int)(i * slope + b);
                        float s;
                        if (cmatrix) s = farZ - (1f / (slopeZ * (float)i + bZ) - oValue);
                        else s = farZ - (slopeZ * (float)i + bZ);

                        if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue; 

                        if (dptr[renderWidth * tY + i] > s - zoffset) continue;
                        dptr[renderWidth * tY + i] = s;

                        *(bptr + (tY * wsD + (i * sD)) + 0) = lB;
                        *(bptr + (tY * wsD + (i * sD)) + 1) = lG;
                        *(bptr + (tY * wsD + (i * sD)) + 2) = lR;
                    }
            }
            else
            {
                float slope = (FromDATA[0] - ToDATA[0]) / (float)((int)FromDATA[1] - (int)ToDATA[1]);
                float b = -slope * FromDATA[1] + FromDATA[0];

                float slopeZ = (FromDATA[2] - ToDATA[2]) / (float)((int)FromDATA[1] - (int)ToDATA[1]);
                float bZ = -slopeZ * FromDATA[1] + FromDATA[2];


                if (FromDATA[1] < ToDATA[1])
                    for (int i = (int)FromDATA[1]; i <= (int)ToDATA[1]; i++)
                    {
                        int tY = (int)(i * slope + b);
                        float s;
                        if (cmatrix) s = farZ - (1f / (slopeZ * (float)i + bZ) - oValue);
                        else s = farZ - (slopeZ * (float)i + bZ);

                        if (i < 0 || tY < 0 || tY >= renderWidth || i >= renderHeight) continue;

                        if (dptr[renderWidth * i + tY] > s - zoffset) continue;
                        dptr[renderWidth * i + tY] = s;

                        *(bptr + (i * wsD + (tY * sD))) = lB;
                        *(bptr + (i * wsD + (tY * sD)) + 1) = lG;
                        *(bptr + (i * wsD + (tY * sD)) + 2) = lR;
                    }
                else
                    for (int i = (int)ToDATA[1]; i <= (int)FromDATA[1]; i++)
                    {
                        int tY = (int)(i * slope + b);
                        float s;
                        if (cmatrix) s = farZ - (1f / (slopeZ * (float)i + bZ) - oValue);
                        else s = farZ - (slopeZ * (float)i + bZ);

                        if (i < 0 || tY < 0 || tY >= renderWidth || i >= renderHeight) continue; 

                        if (dptr[renderWidth * i + tY] > s - zoffset) continue;
                        dptr[renderWidth * i + tY] = s;

                        *(bptr + (i * wsD + (tY * sD))) = lB;
                        *(bptr + (i * wsD + (tY * sD)) + 1) = lG;
                        *(bptr + (i * wsD + (tY * sD)) + 2) = lR;
                    }
            }
        }

        #endregion
    }
}
