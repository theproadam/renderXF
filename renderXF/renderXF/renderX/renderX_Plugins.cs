using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace renderX2
{
    public class STLImporter
    {
        //WARNING: This STL Importer has issues importing ASCII Files on certain computers running Windows 10.
        public string STLHeader { get; private set; }
        public STLFormat STLType { get; private set; }
        public uint TriangleCount { get; private set; }
        public Triangle[] AllTriangles { get; private set; }

        public STLImporter(string TargetFile)
        {
            // Verify That The File Exists
            if (!File.Exists(TargetFile))
                throw new System.IO.FileNotFoundException("Target File Does Not Exist!", "Error!");

            // Load The File Into The Memory as ASCII
            string[] allLinesASCII = File.ReadAllLines(TargetFile);

            // Detect if STL File is ASCII or Binary
            bool ASCII = isAscii(allLinesASCII);

            // Insert Comment Here
            if (ASCII)
            {
                STLType = STLFormat.ASCII;
                AllTriangles = ASCIISTLOpen(allLinesASCII);
            }
            else
            {
                STLType = STLFormat.Binary;
                AllTriangles = BinarySTLOpen(TargetFile);
            }

        }

        Triangle[] BinarySTLOpen(string TargetFile)
        {
            List<Triangle> Triangles = new List<Triangle>();

            byte[] fileBytes = File.ReadAllBytes(TargetFile);
            byte[] header = new byte[80];

            for (int b = 0; b < 80; b++)
                header[b] = fileBytes[b];

            STLHeader = System.Text.Encoding.UTF8.GetString(header);

            uint NumberOfTriangles = System.BitConverter.ToUInt32(fileBytes, 80);
            TriangleCount = NumberOfTriangles;

            for (int i = 0; i < NumberOfTriangles; i++)
            {
                // Read The Normal Vector
                float normalI = System.BitConverter.ToSingle(fileBytes, 84 + i * 50);
                float normalJ = System.BitConverter.ToSingle(fileBytes, (1 * 4) + 84 + i * 50);
                float normalK = System.BitConverter.ToSingle(fileBytes, (2 * 4) + 84 + i * 50);

                // Read The XYZ Positions of The First Vertex
                float vertex1x = System.BitConverter.ToSingle(fileBytes, 3 * 4 + 84 + i * 50);
                float vertex1y = System.BitConverter.ToSingle(fileBytes, 4 * 4 + 84 + i * 50);
                float vertex1z = System.BitConverter.ToSingle(fileBytes, 5 * 4 + 84 + i * 50);

                // Read The XYZ Positions of The Second Vertex
                float vertex2x = System.BitConverter.ToSingle(fileBytes, 6 * 4 + 84 + i * 50);
                float vertex2y = System.BitConverter.ToSingle(fileBytes, 7 * 4 + 84 + i * 50);
                float vertex2z = System.BitConverter.ToSingle(fileBytes, 8 * 4 + 84 + i * 50);

                // Read The XYZ Positions of The Third Vertex
                float vertex3x = System.BitConverter.ToSingle(fileBytes, 9 * 4 + 84 + i * 50);
                float vertex3y = System.BitConverter.ToSingle(fileBytes, 10 * 4 + 84 + i * 50);
                float vertex3z = System.BitConverter.ToSingle(fileBytes, 11 * 4 + 84 + i * 50);

                // Read The Attribute Byte Count
                int Attribs = System.BitConverter.ToInt16(fileBytes, 12 * 4 + 84 + i * 50);

                // Create a Triangle
                Triangle T = new Triangle();

                // Save all the Data Into Said Triangle
                T.normals = new Vector3(normalI, normalK, normalJ);
                T.vertex1 = new Vector3(vertex1x, vertex1z, vertex1y);
                T.vertex2 = new Vector3(vertex2x, vertex2z, vertex2y);//Possible Error?
                T.vertex3 = new Vector3(vertex3x, vertex3z, vertex3y);

                // Add The Triangle
                Triangles.Add(T);
            }

            return Triangles.ToArray();
        }

        Triangle[] ASCIISTLOpen(string[] ASCIILines)
        {
            STLHeader = ASCIILines[0].Replace("solid ", "");

            uint tCount = 0;
            List<Triangle> Triangles = new List<Triangle>();

            foreach (string s in ASCIILines)
                if (s.Contains("facet normal"))
                    tCount++;

            TriangleCount = tCount;

            for (int i = 0; i < tCount * 7; i += 7)
            {
                string n = ASCIILines[i + 1].Trim().Replace("facet normal", "").Replace("  ", " ");

                // Read The Normal Vector
                float normalI = float.Parse(n.Split(' ')[1]);
                float normalJ = float.Parse(n.Split(' ')[2]);
                float normalK = float.Parse(n.Split(' ')[3]);

                string v1 = ASCIILines[i + 3].Split('x')[1].Replace("  ", " ");


                // Read The XYZ Positions of The First Vertex
                float vertex1x = float.Parse(v1.Split(' ')[1]);
                float vertex1y = float.Parse(v1.Split(' ')[2]);
                float vertex1z = float.Parse(v1.Split(' ')[3]);

                string v2 = ASCIILines[i + 4].Split('x')[1].Replace("  ", " ");

                // Read The XYZ Positions of The Second Vertex
                float vertex2x = float.Parse(v2.Split(' ')[1]);
                float vertex2y = float.Parse(v2.Split(' ')[2]);
                float vertex2z = float.Parse(v2.Split(' ')[3]);

                string v3 = ASCIILines[i + 5].Split('x')[1].Replace("  ", " ");

                // Read The XYZ Positions of The Third Vertex
                float vertex3x = float.Parse(v3.Split(' ')[1]);
                float vertex3y = float.Parse(v3.Split(' ')[2]);
                float vertex3z = float.Parse(v3.Split(' ')[3]);

                // Create a Triangle
                Triangle T = new Triangle();

                // Save all the Data Into Said Triangle
                T.normals = new Vector3(normalI, normalK, normalJ);
                T.vertex1 = new Vector3(vertex1x, vertex1z, vertex1y);
                T.vertex2 = new Vector3(vertex2x, vertex2z, vertex2y);
                T.vertex3 = new Vector3(vertex3x, vertex3z, vertex3y);

                // Add The Triangle
                Triangles.Add(T);
            }

            return Triangles.ToArray();
        }

        bool isAscii(string[] Lines)
        {
            string[] Keywords = new string[] { "facet", "solid", "outer", "loop", "vertex", "endloop", "endfacet" };
            int Det = 0;

            foreach (string s in Lines)
            {
                foreach (string ss in Keywords)
                {
                    if (s.Contains(ss))
                    {
                        Det++;
                    }
                }
            }

            if (Det > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public enum STLFormat
        {
            ASCII,
            Binary
        }

        public static float[] AverageUpFaceNormalsAndOutputVertexBuffer(Triangle[] Input, float CutoffAngle)
        {
            Vector3[] VERTEX_DATA = new Vector3[Input.Length * 3];
            Vector3[] VERTEX_NORMALS = new Vector3[Input.Length * 3];
            int[] N_COUNT = new int[Input.Length * 3];

            for (int i = 0; i < Input.Length; i++)
            {
                VERTEX_DATA[i * 3] = Input[i].vertex1;
                VERTEX_DATA[i * 3 + 1] = Input[i].vertex2;
                VERTEX_DATA[i * 3 + 2] = Input[i].vertex3;
            }

            CutoffAngle *= (float)(Math.PI / 180f);
            CutoffAngle = (float)Math.Cos(CutoffAngle);

            for (int i = 0; i < VERTEX_DATA.Length; i++)
            {
                for (int j = 0; j < VERTEX_DATA.Length; j++)
                {
                    if (Vector3.Compare(VERTEX_DATA[j], VERTEX_DATA[i]) && Vector3.Dot(Input[i / 3].normals, Input[j / 3].normals) > CutoffAngle)
                    {
                        VERTEX_NORMALS[i] += Input[j / 3].normals;
                        N_COUNT[i]++;
                    }
                }
            }

            for (int i = 0; i < N_COUNT.Length; i++)
            {
                if (N_COUNT[i] != 0)
                VERTEX_NORMALS[i] /= N_COUNT[i];
            }

            float[] Output = new float[VERTEX_DATA.Length * 6];

            for (int i = 0; i < VERTEX_DATA.Length; i++)
            {
                Output[i * 6 + 0] = VERTEX_DATA[i].x;
                Output[i * 6 + 1] = VERTEX_DATA[i].y;
                Output[i * 6 + 2] = VERTEX_DATA[i].z;
                Output[i * 6 + 3] = VERTEX_NORMALS[i].x;
                Output[i * 6 + 4] = VERTEX_NORMALS[i].y;
                Output[i * 6 + 5] = VERTEX_NORMALS[i].z;

            }

            return Output;
        }
    }

    public class Triangle
    {
        public Vector3 normals;
        public Vector3 vertex1;
        public Vector3 vertex2;
        public Vector3 vertex3;

    }

    public class RenderThread
    {
        Thread T;
        bool DontStop = true;
        double TickRate;
        double NextTimeToFire = 0;
        bool finished = false;

        public bool isStopped
        {
            get { return finished; }
        }

        public bool isAlive
        {
            get { return T.IsAlive; }
        }

        public RenderThread(float TargetFrameTime)
        {
            TickRate = TargetFrameTime;
        }

        public RenderThread(int TargetFrameRate)
        {
            TickRate = 1000f / (float)TargetFrameRate;
        }

        public delegate void TimerFire();
        public event TimerFire RenderFrame;

        public void SetTickRate(float TickRateInMs)
        {
            TickRate = TickRateInMs;
            NextTimeToFire = 0;
        }

        public void Start()
        {
            DontStop = true;
            T = new Thread(RenderCode);
            T.Start();
        }

        public void Abort()
        {
            T.Abort();
        }

        public void Stop()
        {
            DontStop = false;
        }

        void RenderCode()
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();
            while (DontStop)
            {
                if (sw.Elapsed.TotalMilliseconds >= NextTimeToFire)
                {
                    NextTimeToFire = sw.Elapsed.TotalMilliseconds + TickRate;
                    RenderFrame();
                }
            }

            finished = true;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public BitmapCompressionMode biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RGBQUAD
    {
        public byte rgbBlue;
        public byte rgbGreen;
        public byte rgbRed;
        public byte rgbReserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public RGBQUAD bmiColors;
    }

    public enum BitmapCompressionMode : uint
    {
        BI_RGB = 0,
        BI_RLE8 = 1,
        BI_RLE4 = 2,
        BI_BITFIELDS = 3,
        BI_JPEG = 4,
        BI_PNG = 5
    }

    public partial class renderX
    {
        #region PINVOKE

        [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        static extern int SetDIBitsToDevice(IntPtr hdc, int XDest, int YDest, uint
           dwWidth, uint dwHeight, int XSrc, int YSrc, uint uStartScan, uint cScanLines,
           IntPtr lpvBits, [In] ref BITMAPINFO lpbmi, uint fuColorUse);

        [DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        static extern IntPtr MemSet(IntPtr dest, int c, int byteCount);

        [DllImport("kernel32.dll")]
        static extern void RtlZeroMemory(IntPtr dst, int length);
        #endregion

        public static Vector3 Pan3D(Vector3 Input, Vector3 Rotation, float deltaX, float deltaY, float deltaZ = 0)
        {
            Vector3 I = Input;
            Vector3 RADS = new Vector3(0f, Rotation.y / 57.2958f, Rotation.z / 57.2958f);

            float sinX = (float)Math.Sin(RADS.z); //0
            float sinY = (float)Math.Sin(RADS.y); //0


            float cosX = (float)Math.Cos(RADS.z); //0
            float cosY = (float)Math.Cos(RADS.y); //0

            float XAccel = (cosX * -deltaX + (sinY * deltaY) * sinX) + (sinX * -deltaZ) * cosY;
            float YAccel = (cosY * deltaY) + (sinY * deltaZ);
            float ZAccel = (sinX * deltaX + (sinY * deltaY) * cosX) + (cosX * -deltaZ) * cosY;

            I = I + new Vector3(XAccel, YAccel, ZAccel);

            return I;
        }

        public static float Lerp(float A, float B, float t)
        {
            t = Math.Min(Math.Max(t, 0f), 1f);
            return A + (B - A) * t;
        }

        public static float Clamp01(float value)
        {
            if (value < 0) return 0f;
            else if (value > 1) return 1f;
            else return value;
        }

        public static byte Clamp0255(byte value)
        {
            if (value < 0) return 0;
            else if (value > 255) return 255;
            else return value;
        }

        public static class PrimitiveTypes
        {
            /// <summary>
            /// UV Mapped Cube. Half-thanks to learnopengl.com
            /// </summary>
            /// <returns></returns>
            public static float[] Cube()
            {
                return new float[] {   
                    //Back
         -0.5f, -0.5f, -0.5f, 1.0f,  1.0f,
         0.5f, -0.5f, -0.5f,  0.0f,  1.0f,
         0.5f,  0.5f, -0.5f,  0.0f,  0.0f,
         0.5f,  0.5f, -0.5f,  0.0f,  0.0f,
        -0.5f,  0.5f, -0.5f,  1.0f,  0.0f,
        -0.5f, -0.5f, -0.5f,  1.0f,  1.0f,

        //front
         0.5f, -0.5f,  0.5f,    1.0f,  1.0f,
        -0.5f, -0.5f,  0.5f,    0.0f,  1.0f,
         0.5f,  0.5f,  0.5f,    1.0f,  0.0f,
         0.5f,  0.5f,  0.5f,    1.0f,  0.0f,
        -0.5f, -0.5f,  0.5f,    0.0f,  1.0f,
        -0.5f,  0.5f,  0.5f,    0.0f,  0.0f,

        //left
        -0.5f,  0.5f,  0.5f,  1.0f,  0.0f,
        -0.5f, -0.5f, -0.5f,  0.0f,  1.0f,
        -0.5f,  0.5f, -0.5f,  0.0f,  0.0f,
        -0.5f, -0.5f, -0.5f,  0.0f,  1.0f,  
        -0.5f,  0.5f,  0.5f,  1.0f,  0.0f,
        -0.5f, -0.5f,  0.5f,  1.0f,  1.0f,

        //right
         0.5f,  0.5f,  0.5f,   0.0f,  0.0f,
         0.5f,  0.5f, -0.5f,  1.0f, 0.0f,
         0.5f, -0.5f, -0.5f,   1.0f,  1.0f,
         0.5f, -0.5f, -0.5f,  1.0f,  1.0f,
         0.5f, -0.5f,  0.5f,   0.0f,  1.0f,
         0.5f,  0.5f,  0.5f,   0.0f,  0.0f,

         //bottom
        -0.5f, -0.5f, -0.5f,   0.0f,  1.0f,
         0.5f, -0.5f,  0.5f,   1.0f,  0.0f,
         0.5f, -0.5f, -0.5f,   1.0f,  1.0f,
         0.5f, -0.5f,  0.5f,    1.0f,  0.0f,
        -0.5f, -0.5f, -0.5f,    0.0f,  1.0f,
        -0.5f, -0.5f,  0.5f,    0.0f,  0.0f,

        //top
         -0.5f,  0.5f, -0.5f,  0.0f,  0.0f,
         0.5f,  0.5f, -0.5f,   1.0f,  0.0f,
         0.5f,  0.5f,  0.5f,   1.0f,  1.0f,
         0.5f,  0.5f,  0.5f,   1.0f,  1.0f,
        -0.5f,  0.5f,  0.5f,   0.0f,  1.0f,
        -0.5f,  0.5f, -0.5f,   0.0f,  0.0f
                };
            }

            public static float[] CubeNormals()
            {
                return new float[] { 
                    -0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,
         0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,
         0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,
         0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,
        -0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,
        -0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,

        -0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,
         0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,
         0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,
         0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,
        -0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,
        -0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,

        -0.5f,  0.5f,  0.5f, -1.0f,  0.0f,  0.0f,
        -0.5f,  0.5f, -0.5f, -1.0f,  0.0f,  0.0f,
        -0.5f, -0.5f, -0.5f, -1.0f,  0.0f,  0.0f,
        -0.5f, -0.5f, -0.5f, -1.0f,  0.0f,  0.0f,
        -0.5f, -0.5f,  0.5f, -1.0f,  0.0f,  0.0f,
        -0.5f,  0.5f,  0.5f, -1.0f,  0.0f,  0.0f,

         0.5f,  0.5f,  0.5f,  1.0f,  0.0f,  0.0f,
         0.5f,  0.5f, -0.5f,  1.0f,  0.0f,  0.0f,
         0.5f, -0.5f, -0.5f,  1.0f,  0.0f,  0.0f,
         0.5f, -0.5f, -0.5f,  1.0f,  0.0f,  0.0f,
         0.5f, -0.5f,  0.5f,  1.0f,  0.0f,  0.0f,
         0.5f,  0.5f,  0.5f,  1.0f,  0.0f,  0.0f,

        -0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,
         0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,
         0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,
         0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,
        -0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,
        -0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,

        -0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f,
         0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f,
         0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,
         0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,
        -0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,
        -0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f
                };
            }

            public static float[] PlaneXZ()
            {
                return new float[] { 
                  //X, Y, Z, U, V
                    -0.5f, 0, -0.5f, 0, 0,
                    0.5f, 0, -0.5f, 1, 0,
                    0.5f, 0, 0.5f, 1, 1,
                    -0.5f, 0, -0.5f, 0, 0,
                    0.5f, 0, 0.5f, 1, 1,
                    -0.5f, 0, 0.5f, 0, 1
                };
            }

            public static float[] PlaneXY()
            {
                return new float[] { 
                  //X, Y, Z, U, V
                    -0.5f, -0.5f, 0, 0, 0,
                    0.5f, -0.5f, 0, 1, 0,
                    0.5f, 0.5f, 0, 1, 1,
                    -0.5f, -0.5f, 0, 0, 0,
                    0.5f, 0.5f, 0, 1, 1,
                    -0.5f, 0.5f, 0, 0, 1
                };
            }

            public static float[] TriangleXZ()
            {
                return new float[] { 
                  //X, Y, Z, U, V
                    -0.5f, 0, -0.5f, 0, 0,
                    0.5f, 0, -0.5f, 1, 0,
                    0.5f, 0, 0.5f, 1, 1,
                };
            }

            public static float[] TriangleXZ2()
            {
                return new float[] { 
                  //X, Y, Z, U, V
                    -0.5f, 0, -0.5f, 0, 0,
                    0.5f, 0, 0.5f, 1, 1,
                    0.5f, 0, -0.5f, 1, 0
                };
            }

            public static float[] PlaneT()
            {
                return new float[] { 
                  //X, Y, Z, U, V
                    -0.5f, -1, -0.5f, 0, 0, 0,
                    0.5f, 0, -0.5f, 1, 0, 0,
                    0.5f, 0, 0.5f, 1, 1, 0, 
                    -0.5f, -1, -0.5f, 0, 0, 0, 
                    0.5f, 0, 0.5f, 1, 1, 0,
                    -0.5f, -1, 0.5f, 0, 1, 0
                };
            }

            public static float[] CameraIndicator()
            {
                return new float[] { 50, -50, -150, 
-50, 50, -150, 
-50, -50, -150, 
-50, 50, -150, 
50, -50, -150, 
50, 50, -150, 
50, -50, 0, 
25, -25, 0, 
-50, -50, 0, 
25, -25, 0, 
50, -50, 0, 
50, 50, 0, 
25, -25, 0, 
50, 50, 0, 
25, 25, 0, 
25, 25, 0, 
50, 50, 0, 
-25, 25, 0, 
-50, -50, 0, 
-25, -25, 0, 
-50, 50, 0, 
-25, -25, 0, 
-50, -50, 0, 
25, -25, 0, 
-50, 50, 0, 
-25, -25, 0, 
-25, 25, 0, 
-50, 50, 0, 
-25, 25, 0, 
50, 50, 0, 
50, 50, -150, 
-25, 25, 0, 
-50, 50, -150, 
-25, 25, 0, 
50, 50, -150, 
25, 25, 0, 
-25, -25, 0, 
50, -50, -150, 
-50, -50, -150, 
50, -50, -150, 
-25, -25, 0, 
25, -25, 0, 
25, -25, 0, 
50, 50, -150, 
50, -50, -150, 
50, 50, -150, 
25, -25, 0, 
25, 25, 0, 
-25, 25, 0, 
-50, -50, -150, 
-50, 50, -150, 
-50, -50, -150, 
-25, 25, 0, 
-25, -25, 0
};
            }

            public static float[] CameraIndicatorNormals()
            {
                return new float[] { 0, -0, -1, 
0, -0, -1, 
0, 0, -1, 
0, 0, -1, 
0, 0, -1, 
0, 0, -1, 
0, 0, -1, 
0, 0, -1, 
0, 0, -1, 
0, 0, -1, 
-0, 0.9863939f, 0.164399f, 
-0, 0.9863939f, 0.164399f, 
0, -0.9863939f, 0.164399f, 
0, -0.9863939f, 0.164399f, 
0.9863939f, 0, 0.164399f, 
0.9863939f, 0, 0.164399f, 
-0.9863939f, 0, 0.164399f, 
-0.9863939f, 0, 0.164399f
};
            }

            public static float[] CMI()
            {
                return new float[] { 
                    50f, -50f, -50f, 12f, -25f, -50f, -50f, -50f, -50f, 
12f, -25f, -50f, 50f, -50f, -50f, 25f, -12f, -50f, 
25f, -12f, -50f, 50f, -50f, -50f, 50f, 50f, -50f, 
25f, -12f, -50f, 50f, 50f, -50f, 25f, 12f, -50f, 
25f, 12f, -50f, 50f, 50f, -50f, 12f, 25f, -50f, 
12f, 25f, -50f, 50f, 50f, -50f, -12f, 25f, -50f, 
-50f, -50f, -50f, -25f, -12f, -50f, -50f, 50f, -50f, 
-25f, -12f, -50f, -50f, -50f, -50f, -12f, -25f, -50f, 
-12f, -25f, -50f, -50f, -50f, -50f, 12f, -25f, -50f, 
-50f, 50f, -50f, -25f, -12f, -50f, -25f, 12f, -50f, 
-50f, 50f, -50f, -25f, 12f, -50f, -12f, 25f, -50f, 
-50f, 50f, -50f, -12f, 25f, -50f, 50f, 50f, -50f, 
-50f, -50f, 50f, -12f, -25f, 50f, 50f, -50f, 50f, 
-12f, -25f, 50f, -50f, -50f, 50f, -25f, -12f, 50f, 
-25f, -12f, 50f, -50f, -50f, 50f, -50f, 50f, 50f, 
-25f, -12f, 50f, -50f, 50f, 50f, -25f, 12f, 50f, 
-25f, 12f, 50f, -50f, 50f, 50f, -12f, 25f, 50f, 
-12f, 25f, 50f, -50f, 50f, 50f, 12f, 25f, 50f, 
50f, -50f, 50f, 25f, -12f, 50f, 50f, 50f, 50f, 
25f, -12f, 50f, 50f, -50f, 50f, 12f, -25f, 50f, 
12f, -25f, 50f, 50f, -50f, 50f, -12f, -25f, 50f, 
50f, 50f, 50f, 25f, -12f, 50f, 25f, 12f, 50f, 
50f, 50f, 50f, 25f, 12f, 50f, 12f, 25f, 50f, 
50f, 50f, 50f, 12f, 25f, 50f, -50f, 50f, 50f, 
50f, -50f, -50f, 50f, -12f, -25f, 50f, 50f, -50f, 
50f, -12f, -25f, 50f, -50f, -50f, 50f, -25f, -12f, 
50f, -25f, -12f, 50f, -50f, -50f, 50f, -50f, 50f, 
50f, -25f, -12f, 50f, -50f, 50f, 50f, -25f, 12f, 
50f, -25f, 12f, 50f, -50f, 50f, 50f, -12f, 25f, 
50f, -12f, 25f, 50f, -50f, 50f, 50f, 12f, 25f, 
50f, 50f, -50f, 50f, 25f, -12f, 50f, 50f, 50f, 
50f, 25f, -12f, 50f, 50f, -50f, 50f, 12f, -25f, 
50f, 12f, -25f, 50f, 50f, -50f, 50f, -12f, -25f, 
50f, 50f, 50f, 50f, 25f, -12f, 50f, 25f, 12f, 
50f, 50f, 50f, 50f, 25f, 12f, 50f, 12f, 25f, 
50f, 50f, 50f, 50f, 12f, 25f, 50f, -50f, 50f, 
-50f, 50f, -50f, -12f, 50f, -25f, -25f, 50f, -12f, 
-12f, 50f, -25f, -50f, 50f, -50f, 50f, 50f, -50f, 
-12f, 50f, -25f, 50f, 50f, -50f, 12f, 50f, -25f, 
12f, 50f, -25f, 50f, 50f, -50f, 25f, 50f, -12f, 
25f, 50f, -12f, 50f, 50f, -50f, 25f, 50f, 12f, 
-50f, 50f, 50f, -12f, 50f, 25f, 50f, 50f, 50f, 
-12f, 50f, 25f, -50f, 50f, 50f, -25f, 50f, 12f, 
-25f, 50f, 12f, -50f, 50f, 50f, -50f, 50f, -50f, 
-25f, 50f, 12f, -50f, 50f, -50f, -25f, 50f, -12f, 
50f, 50f, 50f, -12f, 50f, 25f, 12f, 50f, 25f, 
50f, 50f, 50f, 12f, 50f, 25f, 25f, 50f, 12f, 
50f, 50f, 50f, 25f, 50f, 12f, 50f, 50f, -50f, 
-50f, -50f, 50f, -25f, -50f, 12f, -50f, -50f, -50f, 
-25f, -50f, 12f, -50f, -50f, 50f, -12f, -50f, 25f, 
-12f, -50f, 25f, -50f, -50f, 50f, 50f, -50f, 50f, 
-12f, -50f, 25f, 50f, -50f, 50f, 12f, -50f, 25f, 
12f, -50f, 25f, 50f, -50f, 50f, 25f, -50f, 12f, 
25f, -50f, 12f, 50f, -50f, 50f, 25f, -50f, -12f, 
25f, -50f, -12f, 50f, -50f, 50f, 50f, -50f, -50f, 
-50f, -50f, -50f, -12f, -50f, -25f, 50f, -50f, -50f, 
-12f, -50f, -25f, -50f, -50f, -50f, -25f, -50f, -12f, 
-25f, -50f, -12f, -50f, -50f, -50f, -25f, -50f, 12f, 
50f, -50f, -50f, -12f, -50f, -25f, 12f, -50f, -25f, 
50f, -50f, -50f, 12f, -50f, -25f, 25f, -50f, -12f, 
-50f, 50f, -50f, -50f, 25f, -12f, -50f, 12f, -25f, 
-50f, 25f, -12f, -50f, 50f, -50f, -50f, 50f, 50f, 
-50f, 25f, -12f, -50f, 50f, 50f, -50f, 25f, 12f, 
-50f, 25f, 12f, -50f, 50f, 50f, -50f, 12f, 25f, 
-50f, 12f, 25f, -50f, 50f, 50f, -50f, -12f, 25f, 
-50f, -50f, -50f, -50f, -25f, -12f, -50f, -50f, 50f, 
-50f, -25f, -12f, -50f, -50f, -50f, -50f, -12f, -25f, 
-50f, -12f, -25f, -50f, -50f, -50f, -50f, 50f, -50f, 
-50f, -12f, -25f, -50f, 50f, -50f, -50f, 12f, -25f, 
-50f, -50f, 50f, -50f, -25f, -12f, -50f, -25f, 12f, 
-50f, -50f, 50f, -50f, -25f, 12f, -50f, -12f, 25f, 
-50f, -50f, 50f, -50f, -12f, 25f, -50f, 50f, 50f
                };
            }

            public static float[] CMINormals()
            {
                return new float[] {-7.219114E-16f, 7.219114E-16f, -1f, 
-7.219114E-16f, 7.219114E-16f, -1f, 
-7.219114E-16f, 7.219114E-16f, -1f, 
-7.219114E-16f, 7.219114E-16f, -1f, 
-7.219114E-16f, 7.219114E-16f, -1f, 
-7.219114E-16f, 7.219114E-16f, -1f, 
-7.219114E-16f, 7.219114E-16f, -1f, 
-7.219114E-16f, 7.219114E-16f, -1f, 
-7.219114E-16f, 7.219114E-16f, -1f, 
-7.219114E-16f, 7.219114E-16f, -1f, 
-7.219114E-16f, 7.219114E-16f, -1f, 
-7.219114E-16f, 7.219114E-16f, -1f, 
-9.023893E-17f, 3.609557E-16f, 1f, 
-9.023893E-17f, 3.609557E-16f, 1f, 
-9.023893E-17f, 3.609557E-16f, 1f, 
-9.023893E-17f, 3.609557E-16f, 1f, 
-9.023893E-17f, 3.609557E-16f, 1f, 
-9.023893E-17f, 3.609557E-16f, 1f, 
-9.023893E-17f, 3.609557E-16f, 1f, 
-9.023893E-17f, 3.609557E-16f, 1f, 
-9.023893E-17f, 3.609557E-16f, 1f, 
-9.023893E-17f, 3.609557E-16f, 1f, 
-9.023893E-17f, 3.609557E-16f, 1f, 
-9.023893E-17f, 3.609557E-16f, 1f, 
1f, 3.158362E-16f, -4.060752E-16f, 
1f, 3.158362E-16f, -4.060752E-16f, 
1f, 3.158362E-16f, -4.060752E-16f, 
1f, 3.158362E-16f, -4.060752E-16f, 
1f, 3.158362E-16f, -4.060752E-16f, 
1f, 3.158362E-16f, -4.060752E-16f, 
1f, 3.158362E-16f, -4.060752E-16f, 
1f, 3.158362E-16f, -4.060752E-16f, 
1f, 3.158362E-16f, -4.060752E-16f, 
1f, 3.158362E-16f, -4.060752E-16f, 
1f, 3.158362E-16f, -4.060752E-16f, 
1f, 3.158362E-16f, -4.060752E-16f, 
1.01886E-32f, 1f, 9.023893E-17f, 
1.01886E-32f, 1f, 9.023893E-17f, 
1.01886E-32f, 1f, 9.023893E-17f, 
1.01886E-32f, 1f, 9.023893E-17f, 
1.01886E-32f, 1f, 9.023893E-17f, 
1.01886E-32f, 1f, 9.023893E-17f, 
1.01886E-32f, 1f, 9.023893E-17f, 
1.01886E-32f, 1f, 9.023893E-17f, 
1.01886E-32f, 1f, 9.023893E-17f, 
1.01886E-32f, 1f, 9.023893E-17f, 
1.01886E-32f, 1f, 9.023893E-17f, 
1.01886E-32f, 1f, 9.023893E-17f, 
-9.023893E-17f, -1f, -9.023893E-17f, 
-9.023893E-17f, -1f, -9.023893E-17f, 
-9.023893E-17f, -1f, -9.023893E-17f, 
-9.023893E-17f, -1f, -9.023893E-17f, 
-9.023893E-17f, -1f, -9.023893E-17f, 
-9.023893E-17f, -1f, -9.023893E-17f, 
-9.023893E-17f, -1f, -9.023893E-17f, 
-9.023893E-17f, -1f, -9.023893E-17f, 
-9.023893E-17f, -1f, -9.023893E-17f, 
-9.023893E-17f, -1f, -9.023893E-17f, 
-9.023893E-17f, -1f, -9.023893E-17f, 
-9.023893E-17f, -1f, -9.023893E-17f, 
-1f, 4.511946E-17f, -3.158362E-16f, 
-1f, 4.511946E-17f, -3.158362E-16f, 
-1f, 4.511946E-17f, -3.158362E-16f, 
-1f, 4.511946E-17f, -3.158362E-16f, 
-1f, 4.511946E-17f, -3.158362E-16f, 
-1f, 4.511946E-17f, -3.158362E-16f, 
-1f, 4.511946E-17f, -3.158362E-16f, 
-1f, 4.511946E-17f, -3.158362E-16f, 
-1f, 4.511946E-17f, -3.158362E-16f, 
-1f, 4.511946E-17f, -3.158362E-16f, 
-1f, 4.511946E-17f, -3.158362E-16f, 
-1f, 4.511946E-17f, -3.158362E-16f};
            }

            public static float[] CMS()
            {
                return new float[] { 
                    -12f, 25f, -50f, -25f, 12f, -187f, -12f, 25f, -187f, 
-25f, 12f, -187f, -12f, 25f, -50f, -25f, 12f, -50f, 
12f, 25f, -187f, -12f, 12f, -200f, 12f, 12f, -200f, 
-12f, 12f, -200f, 12f, 25f, -187f, -12f, 25f, -187f, 
12f, 25f, -187f, -12f, 25f, -50f, -12f, 25f, -187f, 
-12f, 25f, -50f, 12f, 25f, -187f, 12f, 25f, -50f, 
-25f, 12f, -50f, -25f, -12f, -187f, -25f, 12f, -187f, 
-25f, -12f, -187f, -25f, 12f, -50f, -25f, -12f, -50f, 
25f, -12f, -50f, 25f, 12f, -187f, 25f, -12f, -187f, 
25f, 12f, -187f, 25f, -12f, -50f, 25f, 12f, -50f, 
12f, -12f, -200f, -12f, -25f, -187f, 12f, -25f, -187f, 
-12f, -25f, -187f, 12f, -12f, -200f, -12f, -12f, -200f, 
-25f, -12f, -50f, -12f, -25f, -187f, -25f, -12f, -187f, 
-12f, -25f, -187f, -25f, -12f, -50f, -12f, -25f, -50f, 
12f, 12f, -200f, -12f, -12f, -200f, 12f, -12f, -200f, 
-12f, -12f, -200f, 12f, 12f, -200f, -12f, 12f, -200f, 
-12f, -25f, -50f, 12f, -25f, -187f, -12f, -25f, -187f, 
12f, -25f, -187f, -12f, -25f, -50f, 12f, -25f, -50f, 
25f, 12f, -187f, 12f, -12f, -200f, 25f, -12f, -187f, 
12f, -12f, -200f, 25f, 12f, -187f, 12f, 12f, -200f, 
-25f, -12f, -187f, -12f, -25f, -187f, -12f, -12f, -200f, 
12f, -12f, -200f, 12f, -25f, -187f, 25f, -12f, -187f, 
12f, -25f, -50f, 25f, -12f, -187f, 12f, -25f, -187f, 
25f, -12f, -187f, 12f, -25f, -50f, 25f, -12f, -50f, 
25f, 12f, -187f, 12f, 25f, -50f, 12f, 25f, -187f, 
12f, 25f, -50f, 25f, 12f, -187f, 25f, 12f, -50f, 
12f, 25f, -187f, 12f, 12f, -200f, 25f, 12f, -187f, 
-12f, 25f, -187f, -25f, 12f, -187f, -12f, 12f, -200f, 
-12f, 12f, -200f, -25f, -12f, -187f, -12f, -12f, -200f, 
-25f, -12f, -187f, -12f, 12f, -200f, -25f, 12f, -187f
                };
            }

            public static float[] CMSNormals()
            {
                return new float[] { 
                    -0.7071068f, 0.7071068f, 1.727037E-16f, 
-0.7071068f, 0.7071068f, 1.727037E-16f, 
1.373832E-31f, 0.7071068f, -0.7071068f, 
1.373832E-31f, 0.7071068f, -0.7071068f, 
3.6793E-32f, 1f, 9.880175E-17f, 
3.6793E-32f, 1f, 9.880175E-17f, 
-1f, 1.879978E-16f, 9.880175E-17f, 
-1f, 1.879978E-16f, 9.880175E-17f, 
1f, 1.69495E-31f, -9.880175E-17f, 
1f, 1.69495E-31f, -9.880175E-17f, 
-1.111443E-15f, -0.7071068f, -0.7071068f, 
-1.111443E-15f, -0.7071068f, -0.7071068f, 
-0.7071068f, -0.7071068f, 6.908146E-18f, 
-0.7071068f, -0.7071068f, 6.908146E-18f, 
0f, 0f, -1f, 
0f, 0f, -1f, 
-1.188872E-31f, -1f, -6.586783E-17f, 
-1.188872E-31f, -1f, -6.586783E-17f, 
0.7071068f, 1.055168E-15f, -0.7071068f, 
0.7071068f, 1.055168E-15f, -0.7071068f, 
-0.5773503f, -0.5773503f, -0.5773503f, 
0.5773503f, -0.5773503f, -0.5773503f, 
0.7071068f, -0.7071068f, -1.692496E-16f, 
0.7071068f, -0.7071068f, -1.692496E-16f, 
0.7071068f, 0.7071068f, -1.381629E-17f, 
0.7071068f, 0.7071068f, -1.381629E-17f, 
0.5773503f, 0.5773503f, -0.5773503f, 
-0.5773503f, 0.5773503f, -0.5773503f, 
-0.7071068f, 7.03445E-17f, -0.7071068f, 
-0.7071068f, 7.03445E-17f, -0.7071068f
                };
            }
        }

        /// <summary>
        /// Rotates A Vector around (0,0,0), calculating Sin and Cos takens cpu time, thereby its a waste to calculate it multiple times
        /// If you want to rotate around a specitic vector, just subtract it before the function, and re-add it afterwards
        /// </summary>
        /// <param name="inputX"></param>
        /// <param name="inputY"></param>
        /// <param name="inputZ"></param>
        /// <param name="sinedAngle">The sin(x) of the Rotation Angle</param>
        /// <param name="cosedAngle">The cos(x) of the Rotation Angle</param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="Z"></param>
        public static void Rotate(float inputX, float inputY, float inputZ, Vector3 sinedAngle, Vector3 cosedAngle, out float X, out float Y, out float Z)
        {
            float fiX = (inputX) * cosedAngle.z - (inputZ) * sinedAngle.z;
            float fiZ = (inputZ) * cosedAngle.z + (inputX) * sinedAngle.z;
            float ndY = (inputY) * cosedAngle.y + (fiZ) * sinedAngle.y;

            //Returns the newly rotated Vector
            X = (fiX) * cosedAngle.x - (ndY) * sinedAngle.x;
            Y = (ndY) * cosedAngle.x + (fiX) * sinedAngle.x;
            Z = (fiZ) * cosedAngle.y - (inputY) * sinedAngle.y;
        }

        public static Vector3 Rotate(Vector3 input, Vector3 sinedAngle, Vector3 cosedAngle)
        {
            float fiX = (input.x) * cosedAngle.z - (input.z) * sinedAngle.z;
            float fiZ = (input.z) * cosedAngle.z + (input.x) * sinedAngle.z;
            float ndY = (input.y) * cosedAngle.y + (fiZ) * sinedAngle.y;

            //Returns the newly rotated Vector
            return new Vector3(
            (fiX) * cosedAngle.x - (ndY) * sinedAngle.x,
            (ndY) * cosedAngle.x + (fiX) * sinedAngle.x,
            (fiZ) * cosedAngle.y - (input.y) * sinedAngle.y);
        }
    }

    public unsafe class BitmapUtility : IDisposable
    {
        public static bool PixelFormatOverride = false;
        [DllImport("kernel32.dll")]
        static extern void RtlZeroMemory(IntPtr dst, int length);

        BitmapData bmpData;
        Bitmap srcBitmap;

        int width;
        int height;
        int sD;

        public BitmapUtility(Bitmap SourceBitmap)
        {
            if (SourceBitmap == null)
                throw new Exception("SourceBitmap cannot be null!");

            width = SourceBitmap.Width;
            height = SourceBitmap.Height;

            ushort bpp = (ushort)Image.GetPixelFormatSize(SourceBitmap.PixelFormat);
            sD = bpp / 8;

            if (sD != 4 & sD != 3)
                throw new Exception("Unsupported Pixel Format.");

            if (sD != 4 & !PixelFormatOverride)
                throw new Exception("24bpp Pixel Format is much slower than 32bpp format! You can override this warning by setting BitmapUtility.PixelFormatOverride to true!");
            
            srcBitmap = SourceBitmap;
            bmpData = SourceBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, SourceBitmap.PixelFormat);
        }

        public void BlitInto(renderX Target, int TargetX, int TargetY, int srcWidth, int srcHeight, Color TransparencyKey)
        {
            lock (Target.ThreadLock)
            {
                int oX = 0;
                int oY = 0;

                int sW = srcWidth;
                int sH = srcHeight;

                if (srcWidth > width | srcWidth < 0) sW = width;
                if (srcHeight > height | srcHeight < 0) sH = height;

                if (sW + TargetX > Target.RenderWidth) sW -= ((sW + TargetX) - Target.RenderWidth);
                if (sH + TargetY > Target.RenderHeight) sH -= ((sH + TargetY) - Target.RenderHeight);

                if (TargetX < 0) oX = -TargetX;
                if (TargetY < 0) oY = -TargetY;

                Target.gdiplusinteropcopy(bmpData.Scan0, width * sD, sW, sH, oX, oY, TargetX, TargetY, sH, TransparencyKey.B,TransparencyKey.G,TransparencyKey.R );
            }
        }

        public void CopyFrom(renderX Source, int TargetX, int TargetY, int srcWidth, int srcHeight)
        {
            lock (Source.ThreadLock)
            {
                int oX = 0;
                int oY = 0;

                int sW = srcWidth;
                int sH = srcHeight;

                if (srcWidth > width | srcWidth < 0) sW = width;
                if (srcHeight > height | srcHeight < 0) sH = height;

                if (sW + TargetX > Source.RenderWidth) sW -= ((sW + TargetX) - Source.RenderWidth);
                if (sH + TargetY > Source.RenderHeight) sH -= ((sH + TargetY) - Source.RenderHeight);

                if (TargetX < 0) oX = -TargetX;
                if (TargetY < 0) oY = -TargetY;

                Source.gdipluscopy(bmpData.Scan0, width * sD, sW, sH, oX, oY, TargetX, TargetY, sH);
            }
        }

        public void Clear()
        {
            RtlZeroMemory(bmpData.Scan0, width * height * sD);
        }

        public void Clear(byte A, byte R, byte G, byte B)
        { 
            
        }

        void clearstrd(int index)
        { 
            
        }

        public void Dispose()
        {
            srcBitmap.UnlockBits(bmpData);
        }
    }

    /// <summary>
    /// DO NOT USE
    /// </summary>
    public class TextRenderer : IDisposable
    {
        [DllImport("user32.dll")]
        static extern int DrawText(IntPtr hDC, string lpString, int nCount, ref RECT lpRect, uint uFormat);

        [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
        static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr hgdiobj);

        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("gdi32.dll")]
        static extern uint SetTextColor(IntPtr hdc, int crColor);

        [DllImport("gdi32.dll")]
        static extern int SetBkMode(IntPtr hdc, int iBkMode);

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        static extern bool DeleteObject([In] IntPtr hObject);

        RECT rc;
        IntPtr oldFont;
        int oldBkMode;
        int oldCol;
        IntPtr DC;
        IntPtr HFont;
        TextAlignment TAlign = TextAlignment.Center;
        IntPtr HWND;
        Font FNT;


        public TextRenderer(renderX SourceGL)
        {
            SourceGL.GetHWNDandDC(out DC, out HWND);
            if (!GetClientRect(HWND, out rc))
                throw new Exception("FATAL ERROR, FAILED TO GET CLIENT RECTANGLE!");

            oldCol = (int)SetTextColor(DC, ColorTranslator.ToWin32(Color.FromArgb(255, 0, 0, 0)));
            FNT = new Font("Microsoft Sans Serif", 11);
            HFont = FNT.ToHfont();
            oldFont = SelectObject(DC, HFont);
            oldBkMode = SetBkMode(DC, 1);
        }

        public void Dispose()
        {
            SetBkMode(DC, oldBkMode);
            SetTextColor(DC, oldCol);
            SelectObject(DC, oldFont);
            DeleteObject(HFont);
            FNT.Dispose();
        }

        public void SelectFont(Font font)
        {
            FNT = font;
            HFont = font.ToHfont();
            SelectObject(DC, HFont);
        }

        public void SetBounds(int Left, int Right, int Top, int Bottom)
        {
            rc = new RECT(Left, Top, Right, Bottom);
        }

        public void SetBoundsClientSize()
        {
            if (!GetClientRect(HWND, out rc))
                throw new Exception("Failed To Get Client Rectangle!");
        }

        public void SetTextAlignment(TextAlignment Aligment)
        {
            TAlign = Aligment;
        }

        public void SelectColor(Color Color)
        {
            SetTextColor(DC, ColorTranslator.ToWin32(Color));
        }

        public void SetTransparency(bool value)
        {
            if (value)
                SetBkMode(DC, 1);
            else SetBkMode(DC, 0);
        }

        public void DrawText(string mystring)
        {
            if (TAlign == TextAlignment.BottomRight)
                DrawText(DC, mystring, -1, ref rc, 0x00000002 | 0x00000008 | 0x00000020);
            else
                DrawText(DC, mystring, -1, ref rc, (uint)TAlign | 0x00000020);
        }
    }

    public enum TextAlignment
    {
        TopLeft = 0x000000000,
        TopCenter = 0x000000001,
        TopRight = 0x000000002,
        CenterLeft = 0x000000004,
        Center = 0x000000005,
        CenterRight = 0x000000006,
        BottomLeft = 0x000000008,
        BottomCenter = 0x000000009,
        BottomRight = 0x000000010,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public RECT(System.Drawing.Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

        public int X
        {
            get { return Left; }
            set { Right -= (Left - value); Left = value; }
        }

        public int Y
        {
            get { return Top; }
            set { Bottom -= (Top - value); Top = value; }
        }

        public int Height
        {
            get { return Bottom - Top; }
            set { Bottom = value + Top; }
        }

        public int Width
        {
            get { return Right - Left; }
            set { Right = value + Left; }
        }

        public System.Drawing.Point Location
        {
            get { return new System.Drawing.Point(Left, Top); }
            set { X = value.X; Y = value.Y; }
        }

        public System.Drawing.Size Size
        {
            get { return new System.Drawing.Size(Width, Height); }
            set { Width = value.Width; Height = value.Height; }
        }

        public static implicit operator System.Drawing.Rectangle(RECT r)
        {
            return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
        }

        public static implicit operator RECT(System.Drawing.Rectangle r)
        {
            return new RECT(r);
        }

        public static bool operator ==(RECT r1, RECT r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator !=(RECT r1, RECT r2)
        {
            return !r1.Equals(r2);
        }

        public bool Equals(RECT r)
        {
            return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
        }

        public override bool Equals(object obj)
        {
            if (obj is RECT)
                return Equals((RECT)obj);
            else if (obj is System.Drawing.Rectangle)
                return Equals(new RECT((System.Drawing.Rectangle)obj));
            return false;
        }

        public override int GetHashCode()
        {
            return ((System.Drawing.Rectangle)this).GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
        }
    }
}
