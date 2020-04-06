# renderXF
renderXF is a realtime, high performance, software renderer written in c#. It uses .net 4.5. Parallelization and memory pointers are used throughout to heavily improve performance. This software renderer is primarily focused on CAD thanks to its buffer caching capability. The current demo uses STL files. Unfortunately it is currently pixel fill rate limited. <br/>

## Features
- Fully programmable fragment shader
- Partially programmable vertex shader
- Built in camera transforms
- Hardcoded performance features
- Screenspace shaders (WIP)
- Direct blit (No bitmaps required)
- GDI+ Interoperability (blit bitmaps onto the drawbuffer)
- Draw and Depth Buffer caching
- Simple Shader Code


## Shader Code Example
First a shader has be declared with its type, and attribute settings.
```c#
Shader myShader = new Shader(VertexShader, FragmentShader, GLRenderMode.Line, GLExtraAttributeData.None);

```
For performance reasons, renderXF has its own built in camera position and rotation transformation system. It can be disabled via the SetOverrideCameraTransform() method. However the XYZ to XY transform systems are not programmable.

```c#
unsafe void VertexShader(float* OUT, float* IN, int FaceIndex)
{
    OUT[0] = IN[0] * 50f + lightPosition.x;
    OUT[1] = IN[1] * 50f + lightPosition.y;
    OUT[2] = IN[2] * 50f + lightPosition.z;
}
```
###### renderXF doesn't actually require a vertex shader unless manual camera is selected. This is in contrast with gouraud mode which requires only a vertex shader.

The fragment shader gives the user a byte pointer to the RGB pixel it will be setting. and an attribute pointer that will first give the interpolated vertex data, and then the extra attributes.
```c#
unsafe void FragmentShader(byte* BGR, float* Attributes, int FaceIndex)
{
    BGR[0] = (byte)(127.5f + 127.5f * Attributes[0]); //Blue Color
    BGR[1] = (byte)(127.5f + 127.5f * Attributes[1]); //Green Color
    BGR[2] = (byte)(127.5f + 127.5f * Attributes[2]); //Red Color
}
```

## Screenshots
#### One Sample Per Triangle Shader: 97773 Triangles, 1920x1017, ~6.9ms
![Flat Shading Example](https://i.imgur.com/XeEbYci.png)

#### Phong Shader: 558 Triangles, 1920x1017, 16ms
![Phong Example](https://i.imgur.com/QPBYM0s.png)

#### Phong Shader: 7980 Triangles, 1920x1017, 23ms
![Phong Example](https://i.imgur.com/4YiKSkv.png)

#### Hardcoded Gouraud Shader: 12938 Triangles, 1920x1017, ~9.1ms
![Hardcoded Gouraud Shading Example](https://i.imgur.com/8g3ieII.png) 

#### Hardcoded Gouraud Shader: 7980 Triangles, 1920x1017, ~9.3ms
![Hardcoded Gouraud Shading Example](https://i.imgur.com/2nbCUOs.png)

#### Wireframe Shader: 2208 Triangles, 1920x1017, ~0.47ms
![Wireframe Mode](https://i.imgur.com/QB98IEo.png) 

#### Both perpsective and orthographic modes are supported
![Perspective and Orthographic interpolation](https://i.imgur.com/4SR1Qtx.gif)

#### Late Wireframe supported with depth offset
![Late Wireframe Example](https://i.imgur.com/5t9iNZn.png)

#### Backface and Frontface culling
![Culling Example](https://i.imgur.com/I6QNBsm.png)

### Screen space shaders
![Post-Processing](https://i.imgur.com/cNpguJJ.png)

First create the shader code:
```c#
unsafe void VignettePass(byte* BGR, int posX, int posY)
{
    float X = (2f * posX / renderWidth) - 1f;
    float Y = (2f * posY / renderHeight) - 1f;

    X = 1f - 0.5f * X * X;
    Y = X * (1f - 0.5f * Y * Y);

    BGR[0] = (byte)(BGR[0] * Y);
    BGR[1] = (byte)(BGR[1] * Y);
    BGR[2] = (byte)(BGR[2] * Y);
}
```
Next create the shader:
```c#
Shader VignetteShader = new Shader(VignettePass);

//Then during each frame, select and execute the shader
GL.SelectShader(VignetteShader);
GL.Pass();
```

### Buffer caching
The drawing and depth buffer can both be saved. This can hugely improve performance by just rendering what is moving.
```c#
//Initialize renderX and the Cached Buffer
renderX GL = new renderX(1920, 1080, this.Handle);
GLCachedBuffer cachedBuffer = new GLCachedBuffer(GL);

//Tell renderX to create a copy of the drawing and depth buffer upon draw
GL.CreateCopyOnDraw(cachedBuffer);
//Draw the object
GL.Draw();

//During the next frame, just recopy the old buffers:

//Copy with depth test:
GL.CopyFromCache(cachedBuffer, CopyMethod.SplitLoopDepthTest)
//Copy without depth test:
GL.CopyFromCache(cachedBuffer, CopyMethod.SplitLoop)
```

![Caching Example](https://i.imgur.com/2y0COTs.png)
