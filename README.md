# ForceRenderer

ForceRenderer (name still work in progress) is a CPU path traced render engine that I am writing in C# completely from scratch.
The only library that I am using in this project is CodeHelpers, which is also written by me (https://github.com/MMXXX-VIII/CodeHelpers).

It currently supports three BSDF (bidirectional scattering distribution functions): Lambert Diffuse, Phong Specular, and Fresnel Transparency. A skybox can be imported and used as environmental light.
ForceRenderer also supports Wavefront .obj files loading; reading vertices, normals, triangles, texture coordinates, and material properties including diffuse, specular, emission, dissolve, and various maps.

ForceRenderer uses bounding volume hierarchy (BVH) as an acceleration structure with axis aligned bounding boxes (AABB) to significantly improve the ray-scene intersection speed.
Currently, the construction of a very large BVH (> 1 million triangles) is a lengthy process; it can take around several seconds to complete.
This is mainly because all triangles are calculated and used as leaf nodes, and the construction is also performed on only one thread.
BVH also considers multiple surface area heuristics (SAH) to further improve the intersection performance at the cost of longer construction time.

ForceRenderer renders in a tile-based fashion, and it will try to utilize all cores in your CPU during a render.
Settings can be configured to use adaptive sampling to provide more samples towards areas of higher importance.

![5 Blender BMW](https://github.com/MMXXX-VIII/ForceRenderer/blob/main/ForceRenderer/Renders/Path%20Tracing/render%20bmw%201k%20sample%201.7m%20tri.png?raw=true)

![Noisy Cornell Box](https://github.com/MMXXX-VIII/ForceRenderer/blob/main/ForceRenderer/Renders/Path%20Tracing/Noisy/render%20noisy%2040k%20cornell%203h.png?raw=true)

![Lighted Blender BMW](https://github.com/MMXXX-VIII/ForceRenderer/blob/main/ForceRenderer/Renders/Path%20Tracing/render%20bmw%20lights%20transparency%20128%2016000%20samples.png?raw=true)