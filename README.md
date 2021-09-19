# EchoRenderer

---

EchoRenderer (Echo) is a CPU path traced rendering software that I am writing in C# completely from scratch.
The only library that I am using is CodeHelpers, which is also written by me (https://github.com/GaryHuan9/CodeHelpers).

The renderer currently supports four materials: Diffuse, Glossy, Glass, and Emissive.
All material properties can be controlled by texture maps, which are either loaded from file or dynamically generated.
Texture maps are sampled bilinearly, with future plans of supporting bicubic texture sampling.
A six-sided cubemap can be imported as skybox to provide environmental/ambient light.

Triangles and spheres are the two types of fundamental shapes used by Echo.
Geometries can either be constructed through code, or loaded from Wavefront .obj files.
Along with .obj files, the renderer can also read custom .mat files to load material libraries.
Geometry instancing is also supported to render large amount of identical geometries with different materials.

Echo utilizes the SIMD instructions offered by modern CPUs to improved certain calculations.
A section is also displayed on the console while rendering to monitor the progress and current status.

---

Before rendering begins, EchoRenderer builds bounding volume hierarchies to accelerate the ray-scene intersection speed.
Echo considers multiple surface area heuristics to further improve the intersection performance.
The top-down construction process is parallelized across many threads to improve its build time on very large scenes.

Echo renders in a tile-based fashion, to utilize all CPU cores while limiting memory usage.
Each pixel is rendered with two passes: a fixed sample number is first used to determine the deviation of that pixel.
Then an adaptive sample number is calculated based on the deviation to provide more samples towards areas of higher importance.

After the render concludes, a custom floating point image .fpi file is first stored.
The post processing engine is then created and initialized with the appropriate layers.
Layers such as Bloom and Vignette alter the image after it has been rendered.
Finally, the image is saved as a .png file as the render result.

---

_Low Poly Metallic Stanford Bunny (1920x1080; 2000 spp; 1.4K triangles)_
![Stanford Bunny](https://github.com/MMXXX-VIII/EchoRenderer/blob/main/EchoRenderer/Renders/Path%20Tracing/Old%20Tracer/render%20stanford%20bunny%202k.png?raw=true)

_Cornell Box No Ambient (512x512; 128 spp; 20000 adaptive spp; 38 triangles)_
![Cornell Box](https://github.com/MMXXX-VIII/EchoRenderer/blob/main/EchoRenderer/Renders/Path%20Tracing/New%20Tracer/render%20new%20cornell%20box%2040k%20sp.png?raw=true)

_Blender BMW Multiple Lights No Ambient (1920x1080; 128 spp; 16000 adaptive spp; 1.7M triangles)_
![Lighted Blender BMW](https://github.com/MMXXX-VIII/EchoRenderer/blob/main/EchoRenderer/Renders/Path%20Tracing/Old%20Tracer/render%20bmw%20lights%20transparency%20128%2016000%20samples.png?raw=true)

_Blender Material Ball Transparency (1920x1080; 128 spp; 12000 adaptive spp; 2.4M triangles)_
![Material Ball](https://raw.githubusercontent.com/MMXXX-VIII/EchoRenderer/main/EchoRenderer/Renders/Path%20Tracing/New%20Tracer/render%20material%20ball%20128%2012000%20v1.png?raw=true)

_Instanced Material Balls (960x540; 64 spp; 1600 adaptive spp; 5.8B triangles)_
![Cornell Box](https://github.com/MMXXX-VIII/EchoRenderer/blob/main/EchoRenderer/Renders/Path%20Tracing/New%20Tracer/render%20instancing%206%20billion%20tris.png?raw=true)
