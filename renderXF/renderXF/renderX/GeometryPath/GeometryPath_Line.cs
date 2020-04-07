using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace renderX2
{
    internal unsafe partial class GeometryPath
    {
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

                        FS(bptr + (tY * wsD + (i * sD)), ScratchSpace + (Stride - 3) * 2, Index);
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

                        FS(bptr + (i * wsD + (tY * sD)), ScratchSpace + (Stride - 3) * 2, Index);
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

                        FS(bptr + (i * wsD + (tY * sD)), ScratchSpace + (Stride - 3) * 2, Index);
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
                        else zz = (slopeZ * (float)i + bZ);
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

                        FS(bptr + (tY * wsD + (i * sD)), ScratchSpace + (Stride - 3) * 2, Index);
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

                        FS(bptr + (i * wsD + (tY * sD)), ScratchSpace + (Stride - 3) * 2, Index);
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

                        FS(bptr + (i * wsD + (tY * sD)), ScratchSpace + (Stride - 3) * 2, Index);
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
                        if (cmatrix) s = farZ - (1f / (slopeZ * (float)i + bZ) - oValue);
                        else s = farZ - (slopeZ * (float)i + bZ);

                        if (i < 0 || tY < 0 || tY >= renderHeight || i >= renderWidth) continue;

                        if (dptr[renderWidth * tY + i] > s - zoffset) continue;
                        dptr[renderWidth * tY + i] = s;

                        *(iptr + tY * renderWidth + i) = lValue;
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

                        *(iptr + tY * renderWidth + i) = lValue;
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

                        *(iptr + i * renderWidth + tY) = lValue;
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

                        *(iptr + i * renderWidth + tY) = lValue;
                    }
            }
        }

    }
}
