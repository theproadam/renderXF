using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace renderX2
{
    internal unsafe partial class GeometryPath
    {
        //Ctrl + M + O to collapse all regions

        public void WireFrameDebug(int index)
        {
            float* VERTEX_DATA = stackalloc float[9 + 3];
            int BUFFER_SIZE = 3;
            
            for (int b = 0; b < 3; b++)
            {
                float X = *(p + (index * 9 + b * 3)) - cX;
                float Y = *(p + (index * 9 + b * 3 + 1)) - cY;
                float Z = *(p + (index * 9 + b * 3 + 2)) - cZ;

                float fiX = X * coZ - Z * sZ;
                float fiZ = Z * coZ + X * sZ;
                float ndY = Y * coY + fiZ * sY;

                //Returns the newly rotated Vector
                *(VERTEX_DATA + b * 3 + 0) = fiX * coX - ndY * sX;
                *(VERTEX_DATA + b * 3 + 1) = ndY * coX + fiX * sX;
                *(VERTEX_DATA + b * 3 + 2) = fiZ * coY - Y * sY;
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

        public void WireFrameTHICK(int index)
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

            #region XYZtoXY
            if (matrixlerpv == 0)
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * 3 + 0] = rw + (VERTEX_DATA[im * 3 + 0] / VERTEX_DATA[im * 3 + 2]) * fw;
                    VERTEX_DATA[im * 3 + 1] = rh + (VERTEX_DATA[im * 3 + 1] / VERTEX_DATA[im * 3 + 2]) * fh;
                    VERTEX_DATA[im * 3 + 2] = 1f / VERTEX_DATA[im * 3 + 2];
                }
            else if (matrixlerpv == 1)
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * 3 + 0] = rw + (VERTEX_DATA[im * 3 + 0] / ox);
                    VERTEX_DATA[im * 3 + 1] = rh + (VERTEX_DATA[im * 3 + 1] / oy);
                }
            else
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * 3 + 0] = rw + VERTEX_DATA[im * 3 + 0] / ((VERTEX_DATA[im * 3 + 2] * fwi - ox) * (1f - matrixlerpv) + ox);
                    VERTEX_DATA[im * 3 + 1] = rh + VERTEX_DATA[im * 3 + 1] / ((VERTEX_DATA[im * 3 + 2] * fhi - oy) * (1f - matrixlerpv) + oy);

                    VERTEX_DATA[im * 3 + 2] = 1f / (VERTEX_DATA[im * 3 + 2] + oValue);
                }
            #endregion

            for (int i = 0; i < BUFFER_SIZE - 1; i++)
            {
                DrawLineTHICK(VERTEX_DATA + i * 3, VERTEX_DATA + (i + 1) * 3);
            }

            DrawLineTHICK(VERTEX_DATA, VERTEX_DATA + (BUFFER_SIZE - 1) * 3);
        }

        public void WireFrame(int index)
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
                            VS((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                    else for (int b = 0; b < 3; b++)
                        {
                            VS((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
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
                        VS((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                else for (int b = 0; b < 3; b++)
                    {
                        VS((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
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
                    VERTEX_DATA[im * Stride + 0] = roundf(rw + (VERTEX_DATA[im * Stride + 0] / ox));
                    VERTEX_DATA[im * Stride + 1] = roundf(rh + (VERTEX_DATA[im * Stride + 1] / oy));
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
                DrawLineDATA(VERTEX_DATA + im * Stride, VERTEX_DATA + (im - 1) * Stride, sSpace, index);
            
            DrawLineDATA(VERTEX_DATA, VERTEX_DATA + (BUFFER_SIZE - 1) * Stride, sSpace, index);
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
                        VS((VERTEX_DATA + b * 3 + 0), (p + (index * FaceStride + b * ReadStride)), index);
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
                    VS((VERTEX_DATA + b * 3 + 0), (p + (index * FaceStride + b * ReadStride)), index);
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

            float slopeZ;
            float bZ;
            float s;
            float z;

            float* FROM;
            float* TO;

            int FromX;
            int ToX;

            for (int i = yMin; i <= yMax; i++)
            {
                if (ScanLine(i, VERTEX_DATA, BUFFER_SIZE, Intersects))
                {
                    if (Intersects[0] > Intersects[2])
                    {
                        TO = Intersects;
                        FROM = Intersects + 2;
                    }
                    else
                    {
                        FROM = Intersects;
                        TO = Intersects + 2;
                    }

                    FromX = (int)FROM[0];
                    ToX = (int)TO[0];

                    #region Z_Interpolation
                    slopeZ = (FROM[1] - TO[1]) / (FROM[0] - TO[0]);
                    bZ = -slopeZ * FROM[0] + FROM[1];
                    #endregion

                    #region BufferOverflowProtection
                    if (ToX >= renderWidth) ToX = renderWidth - 1;
                    if (FromX < 0) FromX = 0;
                    #endregion
                    //  bool test = Intersects[2] - Intersects[3] != 0;

                    if (attribdata)
                        for (int o = FromX + 1; o <= ToX; o++)
                        {
                            if (cmatrix) z = 1f / ((slopeZ * (float)o + bZ) - oValue);
                            else z = (slopeZ * (float)o + bZ);

                            s = farZ - z;

                            if (dptr[renderWidth * i + o] > s) continue;
                            dptr[renderWidth * i + o] = s;

                            if (ATTRIBLVL == 3)
                            {
                                Intersects[4] = ((z * fwi - ox) * matrixlerpo + ox) * (o - rw);
                                Intersects[5] = ((z * fhi - oy) * matrixlerpo + oy) * (i - rh);
                                Intersects[6] = z;
                            }
                            else if (ATTRIBLVL == 5)
                            {
                                Intersects[4] = ((z * fwi - ox) * matrixlerpo + ox) * (o - rw);
                                Intersects[5] = ((z * fhi - oy) * matrixlerpo + oy) * (i - rh);
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


                            FS((bptr + (i * wsD + (o * sD) + 0)), Intersects + 4, index);
                        }
                    else
                        for (int o = FromX + 1; o <= ToX; o++)
                        {
                            if (cmatrix) s = farZ - 1f / ((slopeZ * (float)o + bZ) - oValue);
                            else s = farZ - (slopeZ * (float)o + bZ);

                            if (dptr[renderWidth * i + o] > s) continue;

                            dptr[renderWidth * i + o] = s;
                            FS((bptr + (i * wsD + (o * sD) + 0)), null, index);
                        }
                }
            }//);

            if (LinkedWFrame) LateWireFrame(VERTEX_DATA, BUFFER_SIZE);
        }

        public void FillTrueFlat(int index)
        {
            float* VERTEX_DATA = stackalloc float[9 + 3];
            int BUFFER_SIZE = 3;

            #region VertexShader
            if (!CAMERA_BYPASS)
            {
                if (HAS_VERTEX_SHADER)
                    for (int b = 0; b < 3; b++)
                    {
                        VS((VERTEX_DATA + b * 3 + 0), (p + (index * FaceStride + b * ReadStride)), index);
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
                    VS((VERTEX_DATA + b * 3 + 0), (p + (index * FaceStride + b * ReadStride)), index);
            }

            #endregion
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
            FS(bBGR, null, index);

            if (LOG_T_COUNT) Interlocked.Increment(ref T_COUNT);

            float slopeZ;
            float bZ;
            float s;

            if (yMax >= renderHeight) yMax = renderHeight - 1;
            if (yMin < 0) yMin = 0;


            float* Intersects = stackalloc float[4];

            int LX1 = 0, LX2 = 0, RX1 = 0, RX2 = 0;

            bool AX = false;

            float* FROM;
            float* TO;

            int FromX;
            int ToX;

            int* RGB_iptr;
            float* Z_fptr;

            float zBegin;

            for (int i = yMin; i <= yMax; ++i)
            {
                if (ScanLine(i, VERTEX_DATA, BUFFER_SIZE, Intersects))
                {
                    if (Intersects[0] > Intersects[2])
                    {
                        TO = Intersects;
                        FROM = Intersects + 2;
                    }
                    else
                    {
                        FROM = Intersects;
                        TO = Intersects + 2;
                    }

                    FromX = (int)FROM[0];
                    ToX = (int)TO[0];

                    #region Z_Interpolation
                    slopeZ = (FROM[1] - TO[1]) / (FROM[0] - TO[0]);
                    bZ = -slopeZ * FROM[0] + FROM[1];
                    #endregion

                    #region BufferOverflowProtection
                    if (ToX >= renderWidth) ToX = renderWidth - 1;
                    if (FromX < 0) FromX = 0;
                    #endregion

                    RGB_iptr = iptr + i * renderWidth;
                    Z_fptr = dptr + i * renderWidth;

                    zBegin = slopeZ * (float)(FromX + 1) + bZ;

                    for (int o = FromX + 1; o <= ToX; ++o)
                    {
                        if (cmatrix) s = farZ - (1f / zBegin - oValue);
                        else s = farZ - zBegin;
                        zBegin += slopeZ;

                        if (Z_fptr[o] > s) continue;
                        Z_fptr[o] = s;
                        
                        RGB_iptr[o] = BGR;

                        if (WriteClick){
                            cptr[i * renderWidth + o] = index + 1;
                            aptr[i * renderWidth + o] = CBuffervalue;
                        }
                    }


                    #region FaceAA
                    if (FACE_AA & false)
                    {
                        LX2 = (int)Intersects[0] + 1;
                        RX2 = (int)Intersects[1];

                        if (!AX)
                        {
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

                          //  DrawLine(LX1, i, LX2, i, 255, 0, 255);

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
                    #endregion
                }
            }

            if (LinkedWFrame) LateWireFrame(VERTEX_DATA, BUFFER_SIZE);
        }

        int FastInt(float R, float G, float B)
        {
	        return (byte)B + 256 * (byte)G + (byte)R * 65536;
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
                            VS((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                    else for (int b = 0; b < 3; b++)
                        {
                            VS((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
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
                        VS((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                else for (int b = 0; b < 3; b++)
                    {
                        VS((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
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


            float yMaxValue = 0;
            float yMinValue = renderHeight;

            int yMax = 0;
            int yMin = renderHeight;

            float iox = 1f / ox;
            float ioy = 1f / oy;


            #region CameraSpaceToScreenSpace
            if (matrixlerpv == 0)
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + (VERTEX_DATA[im * Stride + 0] / VERTEX_DATA[im * Stride + 2]) * fw;
                    VERTEX_DATA[im * Stride + 1] = rh + (VERTEX_DATA[im * Stride + 1] / VERTEX_DATA[im * Stride + 2]) * fh;
                    VERTEX_DATA[im * Stride + 2] = 1f / (VERTEX_DATA[im * Stride + 2]);

                    if (VERTEX_DATA[im * Stride + 1] > yMaxValue) yMaxValue = VERTEX_DATA[im * Stride + 1];
                    if (VERTEX_DATA[im * Stride + 1] < yMinValue) yMinValue = VERTEX_DATA[im * Stride + 1];
                }
            else if (matrixlerpv == 1)
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = roundf(rw + VERTEX_DATA[im * Stride + 0] * iox);
                    VERTEX_DATA[im * Stride + 1] = roundf(rh + VERTEX_DATA[im * Stride + 1] * ioy);

                    if (VERTEX_DATA[im * Stride + 1] > yMaxValue) yMaxValue = VERTEX_DATA[im * Stride + 1];
                    if (VERTEX_DATA[im * Stride + 1] < yMinValue) yMinValue = VERTEX_DATA[im * Stride + 1];
                }
            else
                for (int im = 0; im < BUFFER_SIZE; im++)
                {
                    VERTEX_DATA[im * Stride + 0] = rw + VERTEX_DATA[im * Stride + 0] / ((VERTEX_DATA[im * Stride + 2] * fwi - ox) * (1f - matrixlerpv) + ox);
                    VERTEX_DATA[im * Stride + 1] = rh + VERTEX_DATA[im * Stride + 1] / ((VERTEX_DATA[im * Stride + 2] * fhi - oy) * (1f - matrixlerpv) + oy);
                    VERTEX_DATA[im * Stride + 2] = 1f / (VERTEX_DATA[im * Stride + 2] + oValue);


                    if (VERTEX_DATA[im * Stride + 1] > yMaxValue) yMaxValue = VERTEX_DATA[im * Stride + 1];
                    if (VERTEX_DATA[im * Stride + 1] < yMinValue) yMinValue = VERTEX_DATA[im * Stride + 1];
                }
            #endregion

            #region FaceCulling
            if (FACE_CULL)
            {
                float A = BACKFACECULLS(VERTEX_DATA);
                if (CULL_FRONT && A >= 0) return;
                else if (!CULL_FRONT && A <= 0) return;
            }
            #endregion

            int RW = renderWidth;

            if (LOG_T_COUNT) Interlocked.Increment(ref T_COUNT);


            yMin = (int)yMinValue;
           // yMax = yMaxValue >= (renderHeight - 1.5f) ? renderHeight - 1 : (int)yMaxValue;
            yMax = (int)yMaxValue;
         //   yMax = (int)roundf(yMaxValue);


            if (yMin < 0) yMin = 0;
            if (yMax >= renderHeight) yMax = renderHeight - 1;

            float* Intersects = stackalloc float[4 + (Stride - 3) * 5 + ATTRIBLVL];
            float* az = Intersects + 4 + (Stride - 3) * 2;
            float* slopeAstack = az + (Stride - 3) + ATTRIBLVL;
            float* bAstack = slopeAstack + (Stride - 3);
            float* RTNA = az + ATTRIBLVL;

            for (int i = yMin; i < yMax + 1; i++)
            // Parallel.For(yMin, yMax + 1, i =>
            {
               

                float* FROM;
                float* TO;

                int FromX;
                int ToX;

                float slopeZ;
                float bZ;

                float sA;
                float sB;

                if (ScanLinePLUS(i, VERTEX_DATA, BUFFER_SIZE, Intersects))
                {
                    if (Intersects[0] > Intersects[Stride - 1])
                    {
                        TO = Intersects;
                        FROM = Intersects + (Stride - 1);
                    }
                    else
                    {
                        FROM = Intersects;
                        TO = Intersects + (Stride - 1);
                    }

                    FROM[0] = roundf(FROM[0]);
                    TO[0] = roundf(TO[0]);

                    FromX = (int)FROM[0] == 0 ? 0 : (int)FROM[0] + 1;
                    ToX = (int)TO[0];

                    slopeZ = (FROM[1] - TO[1]) / (FROM[0] - TO[0]);
                    bZ = -slopeZ * FROM[0] + FROM[1];

                    if (ToX >= renderWidth) ToX = renderWidth - 1;
                    if (FromX < 0) FromX = 0;

                    float ZDIFF = 1f / FROM[1] - 1f / TO[1];
                    bool usingZ = ZDIFF != 0;
                    if (ZDIFF != 0) usingZ = ZDIFF * ZDIFF >= 0.00001f;

                    if (usingZ & matrixlerpv != 1)
                        for (int b = 0; b < Stride - 3; b++)
                        {
                            sA = (FROM[2 + b] - TO[2 + b]) / ZDIFF;
                            sB = -sA / FROM[1] + FROM[2 + b];

                            slopeAstack[b] = sA;
                            bAstack[b] = sB;
                        }
                    else
                        for (int b = 0; b < Stride - 3; b++)
                        {
                            sA = (FROM[2 + b] - TO[2 + b]) / (FROM[0] - TO[0]);
                            sB = -sA * FROM[0] + FROM[2 + b];

                            slopeAstack[b] = sA;
                            bAstack[b] = sB;
                        }


                    //IF ATTRIBS ARE PLACE ON A XZ FLAT PLANE (IN SCREEN SPACE) THE INTERPOLATION BREAKS!!!
                    //FOR SOME REASON USING REGULAR LINEAR INTERPOLATION WORKS PERFECTELY OK.
                    //THE Z VALUES FROM X1 to X2 INTERSECTS SEEMS TO BE EXACTLY THE SAME
                    if (attribdata)
                        for (int o = FromX; o <= ToX; o++)
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

                            if (usingZ & matrixlerpv != 1) for (int z = 0; z < Stride - 3; z++) az[z] = (slopeAstack[z] / (slopeZ * (float)o + bZ) + bAstack[z]);
                            else for (int z = 0; z < Stride - 3; z++) az[z] = (slopeAstack[z] * (float)o + bAstack[z]);


                            //   SF((bptr + (i * wsD + (o * sD) + 0)), index);
                            FS((bptr + (i * wsD + (o * sD) + 0)), az, index);
                        }
                    else
                        for (int o = FromX; o <= ToX; o++)
                        {
                            float s;
                            //   float s = farZ - (1f / ((slopeZ * (float)o + bZ)));
                            if (cmatrix) s = farZ - (1f / (slopeZ * (float)o + bZ) - oValue);
                            else s = farZ - (slopeZ * (float)o + bZ);
                            //  float d = slopeZ * (float)o + bZ;


                            if (dptr[renderWidth * i + o] > s) continue;
                            dptr[renderWidth * i + o] = s;

                            if (usingZ & matrixlerpv != 1) for (int z = 0; z < Stride - 3; z++) az[z] = (slopeAstack[z] / (slopeZ * (float)o + bZ) + bAstack[z]);
                            else for (int z = 0; z < Stride - 3; z++) az[z] = (slopeAstack[z] * (float)o + bAstack[z]);

                            //   SF((bptr + (i * wsD + (o * sD) + 0)), index);
                            FS((bptr + (i * wsD + (o * sD) + 0)), az, index);
                        }
                }
            }//);

            if (LinkedWFrame) LateWireFrame(VERTEX_DATA, BUFFER_SIZE);
        }

        float roundf(float value)
        {
            int trunc = (int)value;

            if (value - (float)trunc >= 0.5f) 
                return trunc + 1;
            else return trunc;
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
                        VS((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                else for (int b = 0; b < 3; b++)
                    {
                        VS((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
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
                        VS((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                else for (int b = 0; b < 3; b++)
                    {
                        VS((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
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

            float* Intersects = stackalloc float[10];
            float s;

            float* FROM;
            float* TO;

            int FromX;
            int ToX;

            float slopeZ;
            float bZ;

            float slopeB;
            float slopeG;
            float slopeR;

            float bB;
            float bG;
            float bR;

            int* RGB_iptr;
            float* Z_fptr;

            float zBegin;
            float zValue;

            int col;
            byte* r = (byte*)&col;

            for (int i = yMin + 1; i <= yMax; i++)
            {
                if (ScanLinePLUS(i, VERTEX_DATA, BUFFER_SIZE, Intersects))
                {
                    if (Intersects[0] > Intersects[Stride - 1])
                    {
                        TO = Intersects;
                        FROM = Intersects + (Stride - 1);
                    }
                    else
                    {
                        FROM = Intersects;
                        TO = Intersects + (Stride - 1);
                    }

                    slopeZ = (FROM[1] - TO[1]) / (FROM[0] - TO[0]);
                    bZ = -slopeZ * FROM[0] + FROM[1];

                    FromX = (int)FROM[0];
                    ToX = (int)TO[0];

                    if (ToX >= renderWidth) TO[0] = renderWidth - 1;
                    if (FromX < 0) FROM[0] = 0;

                    float ZDIFF = 1f / FROM[1] - 1f / TO[1];
                    bool usingZ = ZDIFF != 0;
                    if (ZDIFF != 0) usingZ = ZDIFF * ZDIFF >= 0.00001f;

                    if (usingZ & matrixlerpv != 1)
                    {
                        slopeB = (FROM[2] - TO[2]) / ZDIFF;
                        bB = -slopeB / FROM[1] + FROM[2];

                        slopeG = (FROM[3] - TO[3]) / ZDIFF;
                        bG = -slopeG / FROM[1] + FROM[3];

                        slopeR = (FROM[4] - TO[4]) / ZDIFF;
                        bR = -slopeR / FROM[1] + FROM[4];
                    }
                    else
                    {
                        float subd = FROM[0] - TO[0];

                        slopeB = (FROM[2] - TO[2]) / subd;
                        bB = -slopeB * FROM[0] + FROM[2];

                        slopeG = (FROM[3] - TO[3]) / subd;
                        bG = -slopeG * FROM[0] + FROM[3];

                        slopeR = (FROM[4] - TO[4]) / subd;
                        bR = -slopeR * FROM[0] + FROM[4];
                    }

                    RGB_iptr = iptr + i * renderWidth;
                    Z_fptr = dptr + i * renderWidth;

                    zBegin = slopeZ * (float)(FromX + 1) + bZ;

                    for (int o = FromX + 1; o <= ToX; o++)
                    {
                        if (cmatrix)
                        {
                            zValue = 1f / zBegin;
                            s = farZ - zValue + oValue;
                        }
                        else {
                            zValue = zBegin;
                            s = farZ - zValue;
                        }
                              
                        zBegin += slopeZ;

                        if (Z_fptr[o] > s) continue;
                        Z_fptr[o] = s;

                        if (usingZ & matrixlerpv != 1)
                        {
                            r[0] = (byte)(slopeB * zValue + bB);
                            r[1] = (byte)(slopeG * zValue + bG);
                            r[2] = (byte)(slopeR * zValue + bR);
                        }
                        else
                        {
                            r[0] = (byte)(slopeB * (float)o + bB);
                            r[1] = (byte)(slopeG * (float)o + bG);
                            r[2] = (byte)(slopeR * (float)o + bR);
                        }

                        RGB_iptr[o] = col;

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

        public void FillSkybox(int index)
        {
            float* VERTEX_DATA = stackalloc float[Stride * 3];
            int BUFFER_SIZE = 3;

            if (oValue != 0)
                return;

            #region Vertex Input and Processing
            for (int b = 0; b < 3; b++)
            {
                float X = *(p + (index * FaceStride + b * ReadStride)) - 0;
                float Y = *(p + (index * FaceStride + b * ReadStride + 1)) - 0;
                float Z = *(p + (index * FaceStride + b * ReadStride + 2)) - 0; 

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
            #endregion

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

            for (int im = 0; im < BUFFER_SIZE; im++)
            {
                VERTEX_DATA[im * Stride + 0] = rw + (VERTEX_DATA[im * Stride + 0] / VERTEX_DATA[im * Stride + 2]) * fw;
                VERTEX_DATA[im * Stride + 1] = rh + (VERTEX_DATA[im * Stride + 1] / VERTEX_DATA[im * Stride + 2]) * fh;
                VERTEX_DATA[im * Stride + 2] = 1f / (VERTEX_DATA[im * Stride + 2]);

                if (VERTEX_DATA[im * Stride + 1] > yMax) yMax = (int)VERTEX_DATA[im * Stride + 1];
                if (VERTEX_DATA[im * Stride + 1] < yMin) yMin = (int)VERTEX_DATA[im * Stride + 1];
            }

            int t = Interlocked.Increment(ref rd) - 1;

            if (yMax >= renderHeight | yMin < 0)
                throw new Exception("FATAL ERROR");

            for (int i = yMin; i <= yMax; i++)
            {
                int lt = Interlocked.Increment(ref *(bsptr + i)) - 1;
                sptr[i * 12 + lt] = sdptr + t * 77;
            }

            sdptr[t * 77] = BUFFER_SIZE;
            sdptr[t * 77 + 1] = index;

            for (int i = 0; i < BUFFER_SIZE * Stride; i++)
            {
                sdptr[t * 77 + (i + 2)] = VERTEX_DATA[i];
            }

            float* vd = sptr[yMin * 12 + 0];
        }

        public void SkyPass(int i)
        {
            float* Intersects = stackalloc float[4 + (Stride - 3) * 5];
            float* az = Intersects + 4 + (Stride - 3) * 2;
            float* slopeAstack = az + (Stride - 3) + ATTRIBLVL;
            float* bAstack = slopeAstack + (Stride - 3);

            int FACE_COUNT = bsptr[i];

            float sA;
            float sB;

            float slopeZ;
            float bZ;

            int X;
            int Y;

            float* FROM;
            float* TO;

            int FromX;
            int ToX;

            int Addr;
            int sizemone = skyboxSize - 1;
            int maxaddr = (skyboxSize * skyboxSize) - 1;

            float slopeU;
            float slopeV;

            float bU;
            float bV;

            for (int t = 0; t < FACE_COUNT; t++)
            {
                int BUFFER_SIZE = (int)*(sptr + 12 * i + t)[0];
                int* smpl = txptr[(int)sptr[12 * i + t][1]];
               
                if (ScanLinePLUS(i, *(sptr + 12 * i + t) + 2, BUFFER_SIZE, Intersects))
                {
                    if (Intersects[0] > Intersects[Stride - 1])
                    {
                        TO = Intersects;
                        FROM = Intersects + (Stride - 1);
                    }
                    else
                    {
                        FROM = Intersects;
                        TO = Intersects + (Stride - 1);
                    }

                    slopeZ = (FROM[1] - TO[1]) / (FROM[0] - TO[0]);
                    bZ = -slopeZ * FROM[0] + FROM[1];

                    FromX = (int)FROM[0];
                    ToX = (int)TO[0];

                    if (ToX >= renderWidth) TO[0] = renderWidth - 1;
                    if (FromX < 0) FROM[0] = 0;

                    float ZDIFF = 1f / FROM[1] - 1f / TO[1];
                    bool usingZ = ZDIFF != 0;
                    if (ZDIFF != 0) usingZ = ZDIFF * ZDIFF >= 0.00001f;


                    if (usingZ)
                    {
                        sA = (FROM[2] - TO[2]) / ZDIFF;
                        sB = -sA / FROM[1] + FROM[2];

                        slopeU = sA * sizemone;
                        bU = sB * sizemone;

                        sA = (FROM[3] - TO[3]) / ZDIFF;
                        sB = -sA / FROM[1] + FROM[3];

                        slopeV = sA * sizemone;
                        bV = sB * sizemone;
                    }
                    else
                    {
                        sA = (FROM[2] - TO[2]) / (FROM[0] - TO[0]);
                        sB = -sA * FROM[0] + FROM[2];

                        slopeU = sA * sizemone;
                        bU = sB * sizemone;

                        sA = (FROM[3] - TO[3]) / (FROM[0] - TO[0]);
                        sB = -sA * FROM[0] + FROM[3];

                        slopeV = sA * sizemone;
                        bV = sB * sizemone;
                    }
                    //Leftover code for debugging:
                    //   byte* addr = bptr + (i * wsD + ((FromX + 1) * sD) + 0);
                    //   int* addr = iptr + (i * renderWidth + (FromX + 1) + 0);
                    //   addr[0] = (byte)(az[0] * 255f);
                    //   addr[1] = (byte)(az[1] * 255f);
                    //   addr++;

                    int* addr = iptr + (i * renderWidth);

                    float begin = slopeZ * (float)(FromX + 1) + bZ;
                    float begin1 = slopeU * (float)(FromX + 1) + bU;
                    float begin2 = slopeV * (float)(FromX + 1) + bV;

                    
                    if (usingZ)
                        for (int o = FromX + 1; o <= ToX; ++o)
                        {
                            X = (int)(slopeU / begin + bU);
                            Y = (int)(slopeV / begin + bV);
                            begin += slopeZ;

                            Addr = Y * skyboxSize + X;

                            if (Addr > maxaddr | Addr < 0)
                                continue;

                            addr[o] = smpl[Addr];
                        }
                    else
                        for (int o = FromX + 1; o <= ToX; ++o)
                        {
                            X = (int)(begin1);
                            Y = (int)(begin2);
                            begin1 += slopeU;
                            begin2 += slopeV;

                            Addr = Y * skyboxSize + X;

                            if (Addr > maxaddr | Addr < 0)
                                continue;

                            addr[o] = smpl[Addr];
                        }
                
                }
            } 
        }

        public void LineMode(int index)
        {
            float* VERTEX_DATA = stackalloc float[2 * Stride];

            if (!CAMERA_BYPASS)
            {
                if (COPY_ATTRIB_MANUAL)
                    for (int b = 0; b < 2; b++)
                        VS((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                else for (int b = 0; b < 2; b++)
                    {
                        VS((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
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
                        VS((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
                else for (int b = 0; b < 2; b++)
                    {
                        VS((VERTEX_DATA + b * Stride + 0), (p + (index * FaceStride + b * ReadStride)), index);
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

            DrawLineDATA(VERTEX_DATA, VERTEX_DATA + Stride, Sspace, index);
          //  if (attribdata) DrawLineDATA(VERTEX_DATA, VERTEX_DATA + Stride, Sspace, index);
          //  else
            //DrawLineFull(VERTEX_DATA, VERTEX_DATA + Stride, Sspace, index);
        }
    }
}
