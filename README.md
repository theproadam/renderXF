# renderXF
High performance software rendering in c#<br/>

## Features
- Fully programmable fragment shader
- Partially programmable vertex shader
- Hardcoded performance features
- Screenspace shaders (WIP)
- Direct blit (No bitmaps required)
- GDI+ Interoperability (blit bitmaps onto the drawbuffer)



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


