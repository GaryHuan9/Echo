# ForceRenderer

ForceRenderer (name still work in progress) is a CPU path traced render engine that I am writing in C# completely from scratch.
The only library that I am using in this project is CodeHelpers, which is also written by me (https://github.com/MMXXX-VIII/CodeHelpers).

It currently supports three BSDF (bidirectional scattering distribution functions): Lambert Diffuse, Phong Specular, and Fresnel Transparency.
A skybox can be imported and used as environmental/ambient light. Material properties can also contain imported texture maps that are bilinearly sampled based on UV coordinates.

ForceRenderer supports Wavefront .obj files loading; reading vertices, normals, triangles, texture coordinates, and material properties including diffuse, specular, emission, dissolve, and various maps.

ForceRenderer uses bounding volume hierarchy (BVH) as an acceleration structure with axis aligned bounding boxes (AABB) to significantly improve the ray-scene intersection speed.
Currently, the construction of a very large BVH (> 1 million triangles) is a lengthy process; it can take around several seconds to complete.
This is mainly because all triangles are calculated and used as leaf nodes, and the construction is also performed on only one thread.
BVH also considers multiple surface area heuristics (SAH) to further improve the intersection performance at the cost of longer construction time.

ForceRenderer renders in a tile-based fashion, and it will try to utilize all cores in your CPU during a render.
Settings can be configured to use adaptive sampling to provide more samples towards areas of higher importance.

_Low Poly Metallic Stanford Bunny (1920x1080; 2000 spp; 1440 triangles)_
![Stanford Bunny](https://github.com/MMXXX-VIII/ForceRenderer/blob/main/ForceRenderer/Renders/Path%20Tracing/Old%20Tracer/render%20stanford%20bunny%202k.png?raw=true)

_Textured Apex Legends Kunai Knife (3840x2160; 4096 spp; 5386 triangles)_
![Apex Kunai](https://github.com/MMXXX-VIII/ForceRenderer/blob/main/ForceRenderer/Renders/Path%20Tracing/Old%20Tracer/render%20textures%204k%20sp.png?raw=true)

_Standard Cornell Box No Ambient (1000x1000; 40000 spp; 38 triangles)_
![Noisy Cornell Box](https://github.com/MMXXX-VIII/ForceRenderer/blob/main/ForceRenderer/Renders/Path%20Tracing/Old%20Tracer/Noisy/render%20noisy%2040k%20cornell%203h.png?raw=true)

_Blender BMW Multiple Lights No Ambient (1920x1080; 128 spp; 16000 adaptive spp; 1689404 triangles)_
![Lighted Blender BMW](https://github.com/MMXXX-VIII/ForceRenderer/blob/main/ForceRenderer/Renders/Path%20Tracing/Old%20Tracer/render%20bmw%20lights%20transparency%20128%2016000%20samples.png?raw=true)

_Blender Material Ball Transparency (1920x1080l; 128 spp; 12000 adaptive spp; 2369792 triangles)_
![Material Ball](https://github.com/MMXXX-VIII/ForceRenderer/blob/main/ForceRenderer/Renders/Path%20Tracing/New%20Tracer/render%material%20ball%20128%2012000%20v1.png?raw=true)
