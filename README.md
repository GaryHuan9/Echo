# ForceRenderer

ForceRenderer (name still work in progress) is a CPU path tracing render engine that I am writing in C# completely from scratch.
The only library that I am using in this project is CodeHelpers, which is also written by me (https://github.com/MMXXX-VIII/CodeHelpers).

It currently supports two BSDF: Lambert Diffuse and Phong Specular. A skybox can be imported and used as environmental light.
ForceRenderer also supports Wavefront .obj files loading, althought currently only vertices and triangles are imported (normals and texture coordinates are omitted).

ForceRenderer uses Bounding Volume Hierarchy (BVH) as acceleration structure with Axis Aligned Bounding Boxes (AABB) to significantly improve the ray-scene intersection speed.
Currently, the construction of a very large BVH (> 1 million triangles) is a lengthy process; it can take around several seconds to complete.
This is mainly because all triangles are calculated and used as leaf nodes, and the construction is also performed on only one thread.

ForceRenderer renders in a tile-based fashion, and it will try to utilize all cores in your CPU during a render.
The following is a render of 5 Blender BMW with different levels of specular values.
It was rendered with around 1.7 million triangles and 8.7 billion samples in 4K with 1024 samples per pixel.

![5 Blender BMW](https://github.com/MMXXX-VIII/ForceRenderer/blob/main/ForceRenderer/Renders/Path%20Tracing/render%20bmw%201k%20sample%201.7m%20tri.png?raw=true)

![Smooth Blender BMW](https://github.com/MMXXX-VIII/ForceRenderer/blob/main/ForceRenderer/Renders/Path%20Tracing/render%20smooth%20bmw%201k%20sp.png?raw=true)
