using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using renderX2;

namespace renderXF
{
    public unsafe partial class Form1 : Form
    {
        #region LightningData
        Vector3 objColor = new Vector3(255f / 255f, 79f / 255f, 79f / 255f);
        Vector3 lightColor = new Vector3(0.8f, 0.8f, 0.8f);

        float ambientStrength = 0.1f;
        float specularStrength = 0.7f;

        Vector3 lightPositionInCameraSpace;
        Vector3 ambient;

        unsafe void PrepareLightningData()
        {
            lightPositionInCameraSpace = GL.RotateToCameraSpace(lightPosition);
            centerInCameraSpace = GL.RotateToCameraSpace(new Vector3(0, 0, 0));
            ambient = ambientStrength * lightColor;
        }


        #endregion

        unsafe void ReflectionShader(byte* BGR, float* DATA, int FaceIndex)
        {
            Vector3 Normal = new Vector3(nbAddr[FaceIndex * 3], nbAddr[FaceIndex * 3 + 1], nbAddr[FaceIndex * 3 + 2]);
            Vector3 Position = new Vector3(DATA[0], DATA[1], DATA[2]);

            Vector3 I = Vector3.Normalize(Position);
            Vector3 R = Vector3.Reflect(I, Normal);
          //  FragColor = vec4(texture(skybox, R).rgb, 1.0);




            BGR[0] = (byte)(I.x * 127.5f + 127.5f);
            BGR[1] = (byte)(I.y * 127.5f + 127.5f);
            BGR[2] = (byte)(I.z * 127.5f + 127.5f);
        }

        unsafe void SSR_Fragment(byte* BGR, float* Normals, int FaceIndex)
        { 
            
        }

        unsafe void SSR_Pass(byte* BGR, int posX, int posY)
        {
            byte r = (byte)((posX / 1024f) * 255f);
            byte g = (byte)((posY / 768f) * 255f);

            BGR[0] = r;
            BGR[1] = g;
            //BGR[2] = 255;
        }

        
        unsafe void VignetteShader(float* Opacity, int posX, int posY)
        {
            float X = (2f * posX / GL.RenderWidth) - 1f;
            float Y = (2f * posY / GL.RenderHeight) - 1f;

            X = 1f - 0.5f * X * X;
            Y = X * (1f - 0.5f * Y * Y);

            *Opacity = Y;
        }

        unsafe void UV_FragmentShader(byte* tA, float* test, int InstanceNumber)
        {
            //  tA[0] = (byte)(255 * test[0]);
            //  tA[1] = (byte)(255 * test[1]);

            int x = (int)(Clamp01(test[0]) * 349f);
            int y = (int)(Clamp01(test[1]) * 399f);

          //  return;
          //  int x = (int)(renderX.Clamp01(test[0]) * (float)(myTexture.Width - 1));
         //   int y = (int)(renderX.Clamp01(test[1]) * (float)(myTexture.Height - 1));

        //    tA[0] = *(textaddr + (y * myTexture.WidthStride + (x * 4) + 0));
        //    tA[1] = *(textaddr + (y * myTexture.WidthStride + (x * 4) + 1));
        //    tA[2] = *(textaddr + (y * myTexture.WidthStride + (x * 4) + 2));
        }

        unsafe void UV_BigShader(float* OUT, float* IN, int FaceIndex)
        {
            OUT[0] = IN[0] * 50;
            OUT[1] = IN[1] * 50;
            OUT[2] = IN[2] * 50;

        }

        unsafe void CubeShader(float* OUT, float* IN, int InstanceID)
        {
            OUT[0] = IN[0] * 50;
            OUT[1] = IN[1] * 50;
            OUT[2] = IN[2] * 50;
            OUT[3] = IN[3];
            OUT[4] = IN[4];
            OUT[5] = IN[5];

        }

        int* TEXTURE_ADDR;
        int textureWidthMinusOne;
        int textureHeightMinusOne;
        int textureHeight;

        unsafe void TextureShader(byte* BGR, float* Attributes, int FaceIndex)
        {
            int U = (int)(Clamp01(Attributes[0]) * textureWidthMinusOne);
            int V = (int)(Clamp01(Attributes[1]) * textureHeightMinusOne);

            int* iptr = (int*)BGR;


            *iptr = TEXTURE_ADDR[U + V * textureHeight];
            
        }

        unsafe void BasicShader(byte* BGR, float* Attributes, int FaceIndex)
        {
            Vector3 iNormals = new Vector3(nbAddr[FaceIndex * 3], nbAddr[FaceIndex * 3 + 1], nbAddr[FaceIndex * 3 + 2]);
            iNormals = GL.RotateCameraSpace(iNormals);

            byte col = (byte)(127.5f + 127.5f * -iNormals.z);

            BGR[0] = col;
            BGR[1] = col;
            BGR[2] = col;    
        }

        unsafe void NormalFS(byte* BGR, float* attributes, int FaceIndex)
        {
         //   BGR[0] = (byte)(127.5f + 127.5f * nbAddr[FaceIndex * 3]);
         //   BGR[1] = (byte)(127.5f + 127.5f * nbAddr[FaceIndex * 3 + 1]);
          //  BGR[2] = (byte)(127.5f + 127.5f * nbAddr[FaceIndex * 3 + 2]);  


            BGR[0] = (byte)(127.5f + 127.5f * attributes[0]);
            BGR[1] = (byte)(127.5f + 127.5f * attributes[1]);
            BGR[2] = (byte)(127.5f + 127.5f * attributes[2]);   
        }

        unsafe void PhongFS(byte* BGR, float* attributes, int FaceIndex)
        {
            Vector3 fragPos = new Vector3(attributes[3], attributes[4], attributes[5]);
            Vector3 iNormals = new Vector3(attributes[0], attributes[1], attributes[2]);
         //   Vector3 iNormals = new Vector3(nbAddr[FaceIndex * 3], nbAddr[FaceIndex * 3 + 1], nbAddr[FaceIndex * 3 + 2]);
            iNormals = GL.RotateCameraSpace(iNormals);

      //      Vector3 lpos = GL.RotateToCameraSpace(lightPosition);

            Vector3 norm = Vector3.Normalize(iNormals);
            Vector3 lightDir = Vector3.Normalize(lightPositionInCameraSpace - fragPos);
            float diff = Math.Max(Vector3.Dot(norm, lightDir), 0f);
            Vector3 diffuse = diff * lightColor;

            Vector3 viewDir = Vector3.Normalize(-fragPos);
            Vector3 reflectDir = Vector3.Reflect(-lightDir, norm);
            float spec = (float)Math.Pow(Math.Max(Vector3.Dot(viewDir, reflectDir), 0f), 128);
            Vector3 specular = specularStrength * spec * lightColor;

            Vector3 result = (ambient + diffuse + specular) * objColor;

            result.Clamp01();

            BGR[0] = (byte)(result.z * 255f);
            BGR[1] = (byte)(result.y * 255f);
            BGR[2] = (byte)(result.x * 255f);
        }

        unsafe void GouraudShader(float* OUT, float* IN, int FaceIndex)
        {
            Vector3 fragPos = new Vector3(IN[0], IN[1], IN[2]);
           // Vector3 iNormals = new Vector3(IN[3], IN[4], IN[5]);
            Vector3 iNormals = new Vector3(nbAddr[FaceIndex * 3], nbAddr[FaceIndex * 3 + 1], nbAddr[FaceIndex * 3 + 2]);


            Vector3 norm = Vector3.Normalize(iNormals);
            Vector3 lightDir = Vector3.Normalize(lightPosition - fragPos);
            float diff = Math.Max(Vector3.Dot(norm, lightDir), 0f);
            Vector3 diffuse = diff * lightColor;

            Vector3 viewDir = Vector3.Normalize(cameraPosition -fragPos);
            Vector3 reflectDir = Vector3.Reflect(-lightDir, norm);
            float spec = (float)Math.Pow(Math.Max(Vector3.Dot(viewDir, reflectDir), 0f), 8);
            Vector3 specular = specularStrength * spec * lightColor;

            Vector3 result = (ambient + diffuse + specular) * objColor;

            result.Clamp01();

            OUT[0] = IN[0];
            OUT[1] = IN[1];
            OUT[2] = IN[2];

            OUT[3] = (result.z * 255f);
            OUT[4] = (result.y * 255f);
            OUT[5] = (result.x * 255f);
        }


        #region BaseObjects

        Vector3 GridScale = new Vector3(20, 0, 20);
        Vector3 centerInCameraSpace; // (0, 0, 0) in camera space
        float cdist = 40 * 2; //<- Grid related


        //Camera Indicator (top right thing) Data
        Vector3 CI_RAngle = new Vector3(0, 0, 0);
        Vector3 CI_Color = new Vector3(1, 1, 1);

        bool SMPLT = false;
        float* CIC_Normals;
        float* CIP_Normals;


        unsafe void GridShaderVS(float* OUT, float* IN, int FaceIndex)
        {
            OUT[0] = (IN[0] - 4.5f) * GridScale.x;
            OUT[1] = (IN[1] - 4.5f) * GridScale.y;
            OUT[2] = (IN[2] - 4.5f) * GridScale.z;
        }
        unsafe void GridShaderFS(byte* BGR, float* attributes, int FaceIndex)
        {
            Vector3 a = new Vector3(attributes[0], attributes[1], attributes[2]);

            float d = Vector3.Distance(centerInCameraSpace, a);

            BGR[0] = (byte)(255f - 255f * Clamp01(d / cdist));
            BGR[1] = (byte)(255f - 255f * Clamp01(d / cdist));
            BGR[2] = (byte)(255f - 255f * Clamp01(d / cdist));
        }

        unsafe void CIVS(float* OUT, float* IN, int FaceIndex)
        {
            renderX.Rotate(IN[0], IN[1], IN[2], Vector3.Sin(CI_RAngle), Vector3.Cos(CI_RAngle), out OUT[0], out OUT[1], out OUT[2]);
            renderX.Rotate(OUT[0], OUT[1], OUT[2], Vector3.Sin(cameraRotation), Vector3.Cos(cameraRotation), out OUT[0], out OUT[1], out OUT[2]);

            if (SMPLT)
            {
                Vector3 iNormals = new Vector3(CIC_Normals[FaceIndex * 3], CIC_Normals[FaceIndex * 3 + 1], CIC_Normals[FaceIndex * 3 + 2]);
                
                iNormals = GL.RotateCameraSpace(iNormals);

                byte col = (byte)(CI_Color.x * (127.5f + 127.5f * -iNormals.z));

                OUT[3] = col;
                OUT[4] = col;
                OUT[5] = col;
            }
            else
            {
                Vector3 iNormals = new Vector3(CIP_Normals[FaceIndex * 3], CIP_Normals[FaceIndex * 3 + 1], CIP_Normals[FaceIndex * 3 + 2]);
                iNormals = renderX.Rotate(iNormals, Vector3.Sin(CI_RAngle), Vector3.Cos(CI_RAngle));
                iNormals = GL.RotateCameraSpace(iNormals);

                float col = (byte)(127.5f + 127.5f * -iNormals.z);

                OUT[3] = (byte)(col * CI_Color.x);
                OUT[4] = (byte)(col * CI_Color.y);
                OUT[5] = (byte)(col * CI_Color.z);
            }

            OUT[2] = OUT[2] + 400;    
        }
        unsafe void CIFS(byte* BGR, int FaceIndex)
        {
            Vector3 iNormals = new Vector3(CIC_Normals[FaceIndex * 3], CIC_Normals[FaceIndex * 3 + 1], CIC_Normals[FaceIndex * 3 + 2]);
            iNormals = MiniGL.RotateCameraSpace(iNormals);

            byte col = (byte)(127.5f + 127.5f * -iNormals.z);

            BGR[0] = col;
            BGR[1] = col;
            BGR[2] = col;   
        }

        unsafe void CubeFS(byte* BGR, float* attributes, int FadeIndex)
        {
            BGR[0] = 255;
            BGR[1] = 255;
            BGR[2] = 255;
        }
        unsafe void CubeVS(float* OUT, float* IN, int FaceIndex)
        {
            OUT[0] = IN[0] * 50f + lightPosition.x;
            OUT[1] = IN[1] * 50f + lightPosition.y;
            OUT[2] = IN[2] * 50f + lightPosition.z;

        }

        #endregion

        public byte Clamp0255(int val)
        {
            if (val < 0) return 0;
            else if (val > 255) return 255;
            else return (byte)val;
        }

        public float Clamp01(float value)
        {
            if (value < 0) return 0f;
            else if (value > 1) return 1f;
            else return value;
        }
    }
}
