using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace renderX2
{
    internal unsafe partial class GeometryPath
    {
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

            //Statements
            if (useLineShader)
                if (LINE_AA)
                    if (ThickLine)
                    {

                    }
                    else
                    {

                    }
                else
                    if (ThickLine)
                    {

                    }
                    else DrawLineDATA(null, null, null, 0);
            else
                if (ThickLine) DrawLineTHICK(VERTEX_DATA, VERTEX_DATA + Stride);
                else {
                    if (LINE_AA) DrawLineAA(0, 0, 0, 0);
                    else DrawLineDEPTH(VERTEX_DATA, VERTEX_DATA + Stride);
                }




            DrawLineTHICK(VERTEX_DATA, VERTEX_DATA + Stride);

       //     DrawLineLATE(VERTEX_DATA, VERTEX_DATA + Stride);
        }

        internal unsafe void DrawLineAA(float fromX, float fromY, float toX, float toY)
        {
            if (fromX == toX & fromY == toY)
                return;

            // Buffer OverFlow Protection will still be needed regardless how polished the code is...
            float aa = (fromX - toX);
            float ba = (fromY - toY);
            float ty = 0;
            int Floor;


            if (aa * aa > ba * ba)
            {
                float slope = (fromY - toY) / (fromX - toX);
                float b = -slope * fromX + fromY;

                if (fromX < toX)
                    for (int i = (int)fromX; i <= toX; i++)
                    {
                        ty = ((float)i * slope + b);
                        Floor = (int)ty;

                        if (i < 0 || Floor < 0 || ty >= renderHeight || i >= renderWidth) continue;

                        if (LINE_AA) *(iptr + renderWidth * Floor + i) = diValue;
                        else
                        {
                            float MB = (float)Floor + 1f - ty;
                            float MT = ty - (float)Floor;

                            byte* lptr = bptr + (Floor * wsD + (i * sD));

                            *(lptr + 0) = (byte)((*(lptr + 0) * (1f - MB)) + MB * dB);
                            *(lptr + 1) = (byte)((*(lptr + 1) * (1f - MB)) + MB * dG);
                            *(lptr + 2) = (byte)((*(lptr + 2) * (1f - MB)) + MB * dR);

                            if (Floor + 1 >= renderHeight) continue;
                            lptr = bptr + ((Floor + 1) * wsD + (i * sD));

                            *(lptr + 0) = (byte)(*(lptr + 0) * (1f - MT) + MT * dB);
                            *(lptr + 1) = (byte)(*(lptr + 1) * (1f - MT) + MT * dG);
                            *(lptr + 2) = (byte)(*(lptr + 2) * (1f - MT) + MT * dR);
                        }
                    }
                else
                    for (int i = (int)toX; i <= fromX; i++)
                    {
                        ty = ((float)i * slope + b);
                        Floor = (int)ty;

                        if (i < 0 || Floor < 0 || ty >= renderHeight || i >= renderWidth) continue;

                        if (LINE_AA) *(iptr + renderWidth * Floor + i) = diValue;
                        else
                        {
                            float MB = (float)Floor + 1f - ty;
                            float MT = ty - (float)Floor;

                            byte* lptr = bptr + (Floor * wsD + (i * sD));

                            *(lptr + 0) = (byte)((*(lptr + 0) * (1f - MB)) + MB * dB);
                            *(lptr + 1) = (byte)((*(lptr + 1) * (1f - MB)) + MB * dG);
                            *(lptr + 2) = (byte)((*(lptr + 2) * (1f - MB)) + MB * dR);

                            if (Floor + 1 >= renderHeight) continue;
                            lptr = bptr + ((Floor + 1) * wsD + (i * sD));

                            *(lptr + 0) = (byte)(*(lptr + 0) * (1f - MT) + MT * dB);
                            *(lptr + 1) = (byte)(*(lptr + 1) * (1f - MT) + MT * dG);
                            *(lptr + 2) = (byte)(*(lptr + 2) * (1f - MT) + MT * dR);
                        }
                    }
            }
            else
            {
                float slope = (fromX - toX) / (fromY - toY);
                float b = -slope * fromY + fromX;

                if (fromY < toY)
                    for (int i = (int)fromY; i <= toY; i++)
                    {
                        ty = ((float)i * slope + b);
                        Floor = (int)ty;

                        if (i < 0 || Floor < 0 || Floor >= renderWidth || i >= renderHeight) continue;

                        if (LINE_AA) *(iptr + renderWidth * i + Floor) = diValue;
                        else
                        {
                            float MB = (float)Floor + 1f - ty;
                            float MT = ty - (float)Floor;

                            byte* lptr = bptr + (i * wsD + (Floor * sD));

                            *(lptr + 0) = (byte)(*(lptr + 0) * (1f - MB) + MB * dB);
                            *(lptr + 1) = (byte)(*(lptr + 1) * (1f - MB) + MB * dG);
                            *(lptr + 2) = (byte)(*(lptr + 2) * (1f - MB) + MB * dR);


                            if (Floor + 1 >= renderWidth) continue;

                            lptr = bptr + (i * wsD + ((Floor + 1) * sD));
                            *(lptr + 0) = (byte)(*(lptr + 0) * (1f - MT) + MT * dB);
                            *(lptr + 1) = (byte)(*(lptr + 1) * (1f - MT) + MT * dG);
                            *(lptr + 2) = (byte)(*(lptr + 2) * (1f - MT) + MT * dR);
                        }
                    }
                else
                    for (int i = (int)toY; i <= fromY; i++)
                    {
                        ty = ((float)i * slope + b);
                        Floor = (int)ty;

                        if (i < 0 || Floor < 0 || Floor >= renderWidth || i >= renderHeight) continue;

                        if (LINE_AA) *(iptr + renderWidth * i + Floor) = diValue;
                        else
                        {
                            float MB = (float)Floor + 1f - ty;
                            float MT = ty - (float)Floor;

                            byte* lptr = bptr + (i * wsD + (Floor * sD));

                            *(lptr + 0) = (byte)(*(lptr + 0) * (1f - MB) + MB * dB);
                            *(lptr + 1) = (byte)(*(lptr + 1) * (1f - MB) + MB * dG);
                            *(lptr + 2) = (byte)(*(lptr + 2) * (1f - MB) + MB * dR);

                            if (Floor + 1 >= renderWidth) continue;

                            lptr = bptr + (i * wsD + ((Floor + 1) * sD));
                            *(lptr + 0) = (byte)(*(lptr + 0) * (1f - MT) + MT * dB);
                            *(lptr + 1) = (byte)(*(lptr + 1) * (1f - MT) + MT * dG);
                            *(lptr + 2) = (byte)(*(lptr + 2) * (1f - MT) + MT * dR);
                        }
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

        internal unsafe void DrawLinePLUS(float* FromDATA, float* ToDATA)
        {
            if (FromDATA[0] == ToDATA[0] & FromDATA[1] == ToDATA[1])
                return;

            float aa = (FromDATA[0] - ToDATA[0]);
            float ba = (FromDATA[1] - ToDATA[1]);

            if (aa * aa > ba * ba)
            {
                float slope = ba / aa;
                float b = -slope * FromDATA[0] + FromDATA[1];

                if (FromDATA[0] < ToDATA[0])
                    for (int i = (int)FromDATA[0]; i <= ToDATA[0]; i++)
                    {
                        int tY = (int)(i * slope + b);
                        if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue;

                        *(iptr + renderWidth * tY + i) = diValue;
                    }
                else
                    for (int i = (int)ToDATA[0]; i <= FromDATA[0]; i++)
                    {
                        int tY = (int)(i * slope + b);
                        if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue;

                        *(iptr + renderWidth * tY + i) = diValue;
                    }
            }
            else
            {
                float slope = aa / ba;
                float b = -slope * FromDATA[1] + FromDATA[0];

                if (FromDATA[1] < ToDATA[1])
                    for (int i = (int)FromDATA[1]; i <= ToDATA[1]; i++)
                    {
                        int tY = (int)(i * slope + b);
                        if (i < 0 || tY < 0 || tY >= renderWidth || i >= renderHeight) continue;

                        *(iptr + renderWidth * i + tY) = diValue;
                    }
                else
                    for (int i = (int)ToDATA[1]; i <= FromDATA[1]; i++)
                    {
                        int tY = (int)(i * slope + b);
                        if (i < 0 || tY < 0 || tY >= renderWidth || i >= renderHeight) continue;

                        *(iptr + renderWidth * i + tY) = diValue;
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

                if (FromDATA[0] > ToDATA[0])
                {
                    float* temp = ToDATA;
                    ToDATA = FromDATA;
                    FromDATA = temp;
                }

                for (int i = (int)FromDATA[0]; i <= ToDATA[0]; i++)
                {
                    int tY = (int)(i * slope + b);
                    if (cmatrix) zz = (1f / (slopeZ * (float)i + bZ) - oValue);
                    else zz = (slopeZ * (float)i + bZ);
                    //  float zz = (1f / ((slopeZ * (float)i + bZ)));

                    float s = farZ - zz;
                    if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue;

                    if (dptr[renderWidth * tY + i] > s - zoffset) continue;
                    dptr[renderWidth * tY + i] = s;

                    if (attribdata)
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

                    FS(bptr + (tY * wsD + (i * sD)), ScratchSpace + (Stride - 3) * 2, Index);
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

                if (FromDATA[1] > ToDATA[1])
                {
                    float* temp = ToDATA;
                    ToDATA = FromDATA;
                    FromDATA = temp;
                }

                for (int i = (int)FromDATA[1]; i <= ToDATA[1]; i++)
                {
                    int tY = (int)(i * slope + b);

                    if (cmatrix) zz = (1f / (slopeZ * (float)i + bZ) - oValue);
                    else zz = (slopeZ * (float)i + bZ);

                    float s = farZ - zz;
                    if (i < 0 || tY < 0 || tY >= renderWidth || i >= renderHeight) continue;

                    if (dptr[renderWidth * i + tY] > s - zoffset) continue;
                    dptr[renderWidth * i + tY] = s;

                    if (attribdata)
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

                    FS(bptr + (i * wsD + (tY * sD)), ScratchSpace + (Stride - 3) * 2, Index);
                }
            }
        }

        internal unsafe void DrawLineDEPTH(float* FromDATA, float* ToDATA)
        {
            if (FromDATA[0] == ToDATA[0] & FromDATA[1] == ToDATA[1])
                return;

            float aa = (FromDATA[0] - ToDATA[0]);
            float ba = (FromDATA[1] - ToDATA[1]);

            int addr;

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
                        if (cmatrix) s = farZ - (1f / (slopeZ * (float)i + bZ) - oValue);
                        else s = farZ - (slopeZ * (float)i + bZ);

                        if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue;

                        addr = renderWidth * tY + i;

                        if (dptr[addr] > s - zoffset) continue;
                        dptr[addr] = s;

                        *(iptr + addr) = diValue;
                    }
                else
                    for (int i = (int)ToDATA[0]; i <= (int)FromDATA[0]; i++)
                    {
                        int tY = (int)(i * slope + b);
                        float s;
                        if (cmatrix) s = farZ - (1f / (slopeZ * (float)i + bZ) - oValue);
                        else s = farZ - (slopeZ * (float)i + bZ);

                        if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue;

                        addr = renderWidth * tY + i;

                        if (dptr[addr] > s - zoffset) continue;
                        dptr[addr] = s;

                        *(iptr + addr) = diValue;
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

                        addr = i * renderWidth + tY;

                        if (dptr[addr] > s - zoffset) continue;
                        dptr[addr] = s;

                        *(iptr + addr) = diValue;
                    }
                else
                    for (int i = (int)ToDATA[1]; i <= (int)FromDATA[1]; i++)
                    {
                        int tY = (int)(i * slope + b);
                        float s;
                        if (cmatrix) s = farZ - (1f / (slopeZ * (float)i + bZ) - oValue);
                        else s = farZ - (slopeZ * (float)i + bZ);

                        if (i < 0 || tY < 0 || tY >= renderWidth || i >= renderHeight) continue;

                        addr = i * renderWidth + tY;

                        if (dptr[addr] > s - zoffset) continue;
                        dptr[addr] = s;

                        *(iptr + addr) = diValue;
                    }
            }
        }

        internal unsafe void DrawLineTHICK(float* FROM, float* TO)
        {
            if (FROM[0] == TO[0] & FROM[1] == TO[1])
                return;

            float aa = (FROM[0] - TO[0]);
            float ba = (FROM[1] - TO[1]);
            int Floor;
            float ty;

            float s;
            int addr;

            if (aa * aa >= ba * ba)
            {
                float slope = (FROM[1] - TO[1]) / (FROM[0] - TO[0]);
                float b = -slope * FROM[0] + FROM[1];

                float slopeZ = (FROM[2] - TO[2]) / (FROM[0] - TO[0]);
                float bZ = -slopeZ * FROM[0] + FROM[2];

                if (FROM[0] > TO[0])
                {
                    float* temp = FROM;
                    FROM = TO;
                    TO = temp;
                }

                if (LINE_AA)
                {
                    for (int i = (int)FROM[0]; i <= TO[0]; i++)
                    {
                        ty = ((float)i * slope + b) - LwrThick - 1f;
                        Floor = (int)ty;

                        if (cmatrix) s = farZ - (1f / (slopeZ * (float)i + bZ) - oValue);
                        else s = farZ - (slopeZ * (float)i + bZ);

                        if (i < 0 || Floor < 0 || ty >= renderHeight || i >= renderWidth) continue;

                        addr = renderWidth * Floor + i;

                        if (dptr[addr] > s - zoffset) continue;
                        dptr[addr] = s;

                        float MB = (float)Floor + 1f - ty;

                        byte* lptr = bptr + (Floor * wsD + (i * sD));

                            *(lptr + 0) = (byte)((*(lptr + 0) * (1f - MB)) + MB * dB);
                            *(lptr + 1) = (byte)((*(lptr + 1) * (1f - MB)) + MB * dG);
                            *(lptr + 2) = (byte)((*(lptr + 2) * (1f - MB)) + MB * dR);
                    }

                    for (int i = (int)FROM[0]; i <= TO[0]; i++)
                    {
                        ty = ((float)i * slope + b) + UpprThick + 1f;
                        Floor = (int)ty;

                        if (cmatrix) s = farZ - (1f / (slopeZ * (float)i + bZ) - oValue);
                        else s = farZ - (slopeZ * (float)i + bZ);

                        if (i < 0 || Floor < 0 || Floor >= renderHeight || i >= renderWidth) continue;

                        addr = renderWidth * Floor + i;

                        if (dptr[addr] > s - zoffset) continue;
                        dptr[addr] = s;

                        float MB = ty - (float)Floor;
                        byte* lptr = bptr + (Floor * wsD + (i * sD));

                            *(lptr + 0) = (byte)((*(lptr + 0) * (1f - MB)) + MB * dB);
                            *(lptr + 1) = (byte)((*(lptr + 1) * (1f - MB)) + MB * dG);
                            *(lptr + 2) = (byte)((*(lptr + 2) * (1f - MB)) + MB * dR);
                    }
                }

                for (int j = -LwrThick; j <= UpprThick; j++)
                    for (int i = (int)FROM[0]; i <= TO[0]; i++)
                    {
                        int tY = (int)(i * slope + b) + j;

                        if (cmatrix) s = farZ - (1f / (slopeZ * (float)i + bZ) - oValue);
                        else s = farZ - (slopeZ * (float)i + bZ);

                        if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue;

                        addr = renderWidth * tY + i;

                        if (dptr[addr] > s - zoffset) continue;
                        dptr[addr] = s;

                        *(iptr + addr) = diValue;
                    }
                
            }
            else
            {
                float slope = (FROM[0] - TO[0]) / (FROM[1] - TO[1]);
                float b = -slope * FROM[1] + FROM[0];

                float slopeZ = (FROM[2] - TO[2]) / (FROM[1] - TO[1]);
                float bZ = -slopeZ * FROM[1] + FROM[2];


                if (FROM[1] > TO[1])
                {
                    float* temp = FROM;
                    FROM = TO;
                    TO = temp;
                }


                if (LINE_AA)
                {
                    for (int i = (int)FROM[1]; i <= TO[1]; i++)
                    {
                        ty = ((float)i * slope + b) - LwrThick - 1f;
                        Floor = (int)ty;

                        if (i < 0 || Floor < 0 || Floor >= renderWidth || i >= renderHeight) continue;

                        if (cmatrix) s = farZ - (1f / (slopeZ * (float)i + bZ) - oValue);
                        else s = farZ - (slopeZ * (float)i + bZ);

                        addr = i * renderWidth + Floor;

                        if (dptr[addr] > s - zoffset) continue;
                        dptr[addr] = s;

                        float MB = (float)Floor + 1f - ty;
                        float MT = ty - (float)Floor;

                        byte* lptr = bptr + (i * wsD + (Floor * sD));

                        *(lptr + 0) = (byte)(*(lptr + 0) * (1f - MB) + MB * dB);
                        *(lptr + 1) = (byte)(*(lptr + 1) * (1f - MB) + MB * dG);
                        *(lptr + 2) = (byte)(*(lptr + 2) * (1f - MB) + MB * dR);
                    }

                    for (int i = (int)FROM[1]; i <= TO[1]; i++)
                    {
                        ty = ((float)i * slope + b) + UpprThick + 1f;
                        Floor = (int)ty;

                        if (i < 0 || Floor < 0 || Floor >= renderWidth || i >= renderHeight) continue;

                        if (cmatrix) s = farZ - (1f / (slopeZ * (float)i + bZ) - oValue);
                        else s = farZ - (slopeZ * (float)i + bZ);

                        addr = i * renderWidth + Floor;

                        if (dptr[addr] > s - zoffset) continue;
                        dptr[addr] = s;
                        
                        float MB = ty - (float)Floor;

                        byte* lptr = bptr + (i * wsD + (Floor * sD));

                           *(lptr + 0) = (byte)(*(lptr + 0) * (1f - MB) + MB * dB);
                           *(lptr + 1) = (byte)(*(lptr + 1) * (1f - MB) + MB * dG);
                           *(lptr + 2) = (byte)(*(lptr + 2) * (1f - MB) + MB * dR);
                    }
                }

                for (int j = -LwrThick; j <= UpprThick; j++)
                    for (int i = (int)FROM[1]; i <= TO[1]; i++)
                    {
                        int tY = (int)(i * slope + b) + j;
                       
                        if (cmatrix) s = farZ - (1f / (slopeZ * (float)i + bZ) - oValue);
                        else s = farZ - (slopeZ * (float)i + bZ);

                        if (i < 0 || tY < 0 || tY >= renderWidth || i >= renderHeight) continue;

                        addr = i * renderWidth + tY;

                        if (dptr[addr] > s - zoffset) continue;
                        dptr[addr] = s;

                        *(iptr + addr) = diValue;
                    }

            }
        }
    }
}