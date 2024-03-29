# Echo

---

Echo is a CPU photorealistic rendering software written in pure C# completely from scratch<sup>1</sup>.

Echo currently support four materials: Diffuse, Glossy, Glass, and Emissive.
All material properties can be controlled by texture maps, which are either loaded from file or dynamically generated.
Texture maps are sampled bilinearly, with future plans of supporting bicubic texture sampling.
A six-sided cubemap can be imported as skybox to provide environmental/ambient light.

Triangles and spheres are the two types of fundamental shapes used by Echo.
Geometries can either be constructed through code, or loaded from Wavefront .obj files.
Along with .obj files, the renderer can also read custom .mat files to load material libraries.
Instancing is also supported to render large amount of identical geometries with different materials.

Echo utilizes the SIMD instructions offered by modern CPUs to improved certain calculations.
A section is also displayed on the console while rendering to monitor the progress and current status.

<sup>1</sup>All core code are by me in C#. Cross platform user interface backend supported by Dear ImGui & SDL2. Optional machine learning denoiser Oidn from Intel.

---

Before rendering begins, Echo builds acceleration structure(s) to improve the ray-scene intersection speed.
Echo considers surface area heuristics across the longest axis to further increase the hierarchy build quality.
The top-down construction process is parallelized across many threads to improve its build time on very large scenes.
Currently, both binary bounding volume hierarchies (BVH) and quad BVH are featured. Especially on large scenes, 
quad BVH better exploits SIMD instructions by computing intersections with four children simultaneously.

Echo renders in a tile-based fashion, to utilize all CPU threads while limiting memory usage.
Each pixel is rendered with two passes: a fixed sample number is first used to determine the variance of that pixel.
Then an adaptive sample number is calculated based on the variance to provide more samples towards areas of higher importance.

After the render concludes, a custom floating point image .fpi file is first stored.
The post processing engine is then created and initialized with the appropriate layers.
Layers such as Bloom and Vignette alter the image after it has been rendered.
Finally, the image is saved as a .png file as the render result.

---

_Cornell Box No Ambient (1024x1024; 38 triangles)_
![Cornell Box](https://github.com/GaryHuan9/Echo/blob/main/Renders/cornell-on-new-system.png?raw=true)

_Blender BMW (1920x1080; 1.7M triangles)_
![Blender BMW](https://github.com/GaryHuan9/Echo/blob/main/Renders/bmw-lights-transparency.png?raw=true)

_Instanced Material Balls (1920x1080; 5.8B triangles)_
![Material Balls](https://github.com/GaryHuan9/Echo/blob/main/Renders/instancing-six-billion-tris.png?raw=true)
