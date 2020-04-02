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
     //   Vector3 objColor = new Vector3(79f / 255f, 255f / 255f, 79f / 255f);
        Vector3 objColor = new Vector3(255f / 255f, 79f / 255f, 79f / 255f);


        Vector3 scale = new Vector3(20, 0, 20);

        Vector3 lightColor = new Vector3(0.8f, 0.8f, 0.8f);
        float ambientStrength = 0.05f;
        float specularStrength = 0.7f;

        float cdist = 40 * 2;
        Vector3 lPos;

        Vector3 rotLpos;
        Vector3 ambient;
        Vector3 lightDir;
        Vector3 cLoc;

        Vector3 CI_RAngle = new Vector3(0, 0, 0);

        bool SMPLT = false;
        float* CIC_Normals;
        float* CIP_Normals;

        Vector3 CI_Color = new Vector3(1, 1, 1);

        unsafe void SSR_Fragment(byte* BGR, float* Normals, int FaceIndex)
        { 
            
        }

        unsafe void SSR_Pass(byte* BGR, int posX, int posY)
        { 
            
        }


        unsafe void PrepareLightningData()
        {
          //  lPos = GL.RotateToCameraSpace(lightPosition);
            rotLpos = GL.RotateToCameraSpace(lightPosition);
            lPos = lightPosition;
            ambient = ambientStrength * lightColor;
            lightDir = Vector3.Normalize(lPos);
            cLoc = GL.RotateToCameraSpace(new Vector3(0, 0, 0));
        }

        unsafe void StandardShader_Shader(byte* tA, float* test, int InstanceNumber)
        {
            //  tA[0] = (byte)(127.5f + 127.5f * nbAddr[InstanceNumber * 3]);
            //  tA[1] = (byte)(127.5f + 127.5f * nbAddr[InstanceNumber * 3 + 1]);
            //  tA[2] = (byte)(127.5f + 127.5f * nbAddr[InstanceNumber * 3 + 2]);    

             // tA[2] = (byte)(255 * test[0]);
             // tA[1] = (byte)(255 * test[1]);

            //  tA[0] = (byte)(127.5f + 127.5f * test[0]);
            //  tA[1] = (byte)(127.5f + 127.5f * test[1]);
            //  tA[2] = (byte)(127.5f + 127.5f * test[2]);    

            //   tA[2] = 155;

            //  tA[0] = (byte)((255f / 1024f) * test[0]);
            //  tA[1] = (byte)((255f / 768f) * test[1]);

          //  tA[0] = (byte)((255f / 1000f) * test[0] + 127.5f);
          //  tA[1] = (byte)((255f / 1000f) * test[1] + 127.5f);
         //   tA[2] = (byte)((255f / 1000f) * test[2] + 127.5f);

            //   tA[0] = (byte)test[0];
            //   tA[1] = (byte)test[1];
            //   tA[2] = (byte)test[2];


          //  return;
            int x = (int)(renderX.Clamp01(test[0]) * (float)(myTexture.Width - 1));
            int y = (int)(renderX.Clamp01(test[1]) * (float)(myTexture.Height - 1));

            tA[0] = *(textaddr + (y * myTexture.WidthStride + (x * 4) + 0));
            tA[1] = *(textaddr + (y * myTexture.WidthStride + (x * 4) + 1));
            tA[2] = *(textaddr + (y * myTexture.WidthStride + (x * 4) + 2));
        }

        unsafe void ScaleIT_VS(float* OUT, float* IN, int Index)
        {
            OUT[0] = IN[0] * 10f;
            OUT[1] = IN[1] * 10f;
            OUT[2] = IN[2] * 10f;

        }

        unsafe void BasicShader(byte* BGR, int FaceIndex)
        {
           // Vector3 iNormals = new Vector3(attributes[0], attributes[1], attributes[2]);
            Vector3 iNormals = new Vector3(nbAddr[FaceIndex * 3], nbAddr[FaceIndex * 3 + 1], nbAddr[FaceIndex * 3 + 2]);
            iNormals = GL.RotateCameraSpace(iNormals);

            byte col = (byte)(127.5f + 127.5f * -iNormals.z);

            BGR[0] = col;
            BGR[1] = col;
            BGR[2] = col;    
        }

        unsafe void AltShader(byte* BGR, float* attributes, int FaceIndex)
        {
         //   BGR[0] = (byte)(127.5f + 127.5f * nbAddr[FaceIndex * 3]);
         //   BGR[1] = (byte)(127.5f + 127.5f * nbAddr[FaceIndex * 3 + 1]);
          //  BGR[2] = (byte)(127.5f + 127.5f * nbAddr[FaceIndex * 3 + 2]);  


            BGR[0] = (byte)(127.5f + 127.5f * attributes[0]);
            BGR[1] = (byte)(127.5f + 127.5f * attributes[1]);
            BGR[2] = (byte)(127.5f + 127.5f * attributes[2]);   
        }

        unsafe void FuncShader(byte* BGR, float* attributes, int FaceIndex)
        {
            Vector3 fragPos = new Vector3(attributes[3], attributes[4], attributes[5]);
            Vector3 iNormals = new Vector3(attributes[0], attributes[1], attributes[2]);
         //   Vector3 iNormals = new Vector3(nbAddr[FaceIndex * 3], nbAddr[FaceIndex * 3 + 1], nbAddr[FaceIndex * 3 + 2]);
            iNormals = GL.RotateCameraSpace(iNormals);

      //      Vector3 lpos = GL.RotateToCameraSpace(lightPosition);

            Vector3 norm = Vector3.Normalize(iNormals);
            Vector3 lightDir = Vector3.Normalize(rotLpos - fragPos);
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
            Vector3 iNormals = new Vector3(IN[3], IN[4], IN[5]);
           // Vector3 iNormals = new Vector3(nbAddr[FaceIndex * 3], nbAddr[FaceIndex * 3 + 1], nbAddr[FaceIndex * 3 + 2]);
            iNormals = GL.RotateCameraSpace(iNormals);

         //   Vector3 lpos = GL.RotateToCameraSpace(lightPosition);

            Vector3 norm = Vector3.Normalize(iNormals);
            Vector3 lightDir = Vector3.Normalize(lPos - fragPos);
            float diff = Math.Max(Vector3.Dot(norm, lightDir), 0f);
            Vector3 diffuse = diff * lightColor;

            Vector3 viewDir = Vector3.Normalize(cameraPosition -fragPos);
            Vector3 reflectDir = Vector3.Reflect(-lightDir, norm);
            float spec = (float)Math.Pow(Math.Max(Vector3.Dot(viewDir, reflectDir), 0f), 128);
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

        unsafe void GridShaderVS(float* OUT, float* IN, int FaceIndex)
        {
            OUT[0] = (IN[0] - 4.5f) * scale.x;
            OUT[1] = (IN[1] - 4.5f) * scale.y;
            OUT[2] = (IN[2] - 4.5f) * scale.z;
        }

        unsafe void GridShaderFS(byte* BGR, float* attributes, int FaceIndex)
        {
          //  BGR[0] = (byte)((BGR[0] + 255) / 2);
          //  BGR[1] = (byte)((BGR[1] + 255) / 2);
          //  BGR[2] = (byte)((BGR[2] + 255) / 2);
            Vector3 a = new Vector3(attributes[0], attributes[1], attributes[2]);


            float d = Vector3.Distance(cLoc, a);

            BGR[0] = (byte)(255f - 255f * Clamp01(d / cdist));
            BGR[1] = (byte)(255f - 255f * Clamp01(d / cdist));
            BGR[2] = (byte)(255f - 255f * Clamp01(d / cdist));
            
          //  BGR[0] = (byte)((255f / 1920f) * attributes[0]);
          //  BGR[1] = (byte)((255f / 1080f) * attributes[1]);
        //    BGR[2] = 0;

        }

        unsafe void WFrameShader(byte* BGR, float* attributes, int FaceIndex)
        {
            BGR[0] = 255;
            BGR[1] = 255;
            BGR[2] = 255;
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

        unsafe void ObjectVS(float* OUT, float* IN, int FaceIndex)
        {
            OUT[0] = IN[0];
            OUT[1] = IN[1];
            OUT[2] = IN[2] - 10;
        }

        unsafe void RotateVS(float* OUT, float* IN, int FaceIndex)
        {
            //renderX.Rotate(IN[0], IN[1], IN[2], Vector3.Sin(angle), Vector3.Cos(angle), out OUT[0], out OUT[1], out OUT[2]);
        }

        unsafe void StandardShader_Flat(byte* BGR, int FaceIndex)
        {
            BGR[0] = (byte)(127.5f + 127.5f * nbAddr[FaceIndex * 3]);
            BGR[1] = (byte)(127.5f + 127.5f * nbAddr[FaceIndex * 3 + 1]);
            BGR[2] = (byte)(127.5f + 127.5f * nbAddr[FaceIndex * 3 + 2]);
        }

        unsafe void CubeFS(byte* BGR, int FadeIndex)
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
