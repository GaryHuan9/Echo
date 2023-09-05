:construction: this page is currently under construction! :construction:

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a>
    <img src="https://github.com/GaryHuan9/Echo/assets/22217952/7b43fe07-4655-426e-8152-58ddb559971f" alt="Logo">
  </a>

<h3 align="center">Echo</h3>
  <p align="center">
    An awesome ray traced 3D renderer build in C# from scratch!
    <br />
    <a href="docs/1-getting-started.md"><strong>Getting started »</strong></a>
    <br />
    <br />
    <a href="https://github.com/othneildrew/Best-README-Template/issues">Report Bug</a>
    ·
    <a href="https://github.com/othneildrew/Best-README-Template/issues">Request Feature</a>
  </p>
</div>

<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#about-the-project">About the Project</a></li>
    <li><a href="#features">Features</a></li>
    <li><a href="#installation">Installation</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
    <li><a href="#gallery">Gallery</a></li>
  </ol>
</details>

---

## About the Project

Echo is a physically based 3D rendering software package; that is, Echo takes in a 3D scene and captures a 2D picture of it. Scenes are given to Echo as a data collection of geometric shapes, texture and material parameters (which describe physical properties of the shapes), light sources that illuminate the scene, and a camera perspective from which the picture is captured. Since Echo is a [ray tracer](https://en.m.wikipedia.org/wiki/Ray_tracing_(graphics)), it captures and synthesizes a 2D picture by shooting billions of rays in the 3D scene to understand its visual features. 

Echo was built from the ground up using C# without any external libraries for all its core components. All rendering [features](#features) are available to be explored through a standard GUI application (see [Echo.UserInterface](src/Echo.UserInterface)) and/or accessed via an extensive programming library API (see [Echo.Core](src/Echo.Core)). While Echo was only initially an exploratory project, it has now grown to become fully usable as a photorealistic renderer with many advance [features](#features). See the [Gallery](#gallery) for renders produced by Echo, or navigate to the [Getting Started](docs/1-getting-started.md) page to begin using it!

## Features

- Path tracing with multiple importance sampling and next event estimation
- Many cool materials
- QBVH acceleration and SIMD utilization across the application
- Light hierarchy to better importance sample lights
- Multithreading worker pool system supporting C# async (future feature, parallel initialization)
- Customizable compositing stack with many enhancing effects (and Oidn)
- Echo description language (with image support from ImageMagick)

**Academic Papers & Articles Implemented:**
- Physically Based Rendering: From Theory To Implementation [(Link)](https://pbr-book.org/) - [Pharr, Jakob, and Humphreys 2018]
- Optimally Combining Sampling Techniques for Monte Carlo Rendering [(Link)](https://cseweb.ucsd.edu/~viscomp/classes/cse168/sp21/readings/veach.pdf) - [Veach and Guibas 1995]
- Shallow Bounding Volume Hierarchies for Fast SIMD Ray Tracing of Incoherent Rays [(Link)](https://www.uni-ulm.de/fileadmin/website_uni_ulm/iui.inst.100/institut/Papers/QBVH.pdf) - [Dammertz et al. 2008]
- Importance Sampling of Many Lights with Adaptive Tree Splitting [(Link)](https://fpsunflower.github.io/ckulla/data/many-lights-hpg2018.pdf) - [Estevez and Kulla 2018]
- Heuristics for Ray Tracing Using Space Subdivision [(Link)](https://graphicsinterface.org/wp-content/uploads/gi1989-22.pdf) - [MacDonald and Booth 1990]
- Fast and Tight Fitting Bounding Spheres [(Link)](https://ep.liu.se/ecp/034/009/ecp083409.pdf) - [Larsson 2008]
- Higher Density Uniform Floats [(Link)](https://marc-b-reynolds.github.io/distribution/2017/01/17/DenseFloat.html#the-parts-im-not-tell-you) - [Reynolds 2017]
- Average Irregularity Representation of a Rough Surface for Ray Reflection [(Link)](https://pharr.org/matt/blog/images/average-irregularity-representation-of-a-rough-surface-for-ray-reflection.pdf) - [Trowbridge and Reitz 1975]
- A Simpler and Exact Sampling Routine for the GGX Distribution of Visible Normals [(Link)](https://core.ac.uk/download/pdf/84984389.pdf) - [Heitz 2017]
- Generalization of Lambert's Reflectance Model [(Link)](https://www1.cs.columbia.edu/CAVE/publications/pdfs/Oren_SIGGRAPH94.pdf) - [Oren and Nayar 1994]
- A Tiny Improvement of Oren-Nayer Reflectance Model [(Link)](https://mimosa-pudica.net/improved-oren-nayar.html) - [Fujii]
- The Reflection Factor of a Polished Glass Surface for Diffused Light [(Link)](https://scholar.google.com/scholar?hl=en&as_sdt=0%2C23&q=The+Reflection+Factor+of+a+Polished+Glass+Surface+for+Diffused+Light&btnG=) - [Walsh 1926]
- A Quantized-diffusion Model for Rendering Translucent Materials [(Link)](https://naml.us/paper/deon2011_subsurface.pdf) - [D'Eon and Irving 2011]
- Photographic Tone Reproduction for Digital Images [(Link)](https://www-old.cs.utah.edu/docs/techreports/2002/pdf/UUCS-02-001.pdf) - [Reinhard et al. 2002]

## Installation

You need dotnet 6.

## Contributing

All contribution are welcome! Please follow ...

## License

Distributed under the MIT License. See `LICENSE.txt` for more information.

## Acknowledgments

I would like to thank the following people, organizations, and resources for making this project possible:
- The Strnad Project at University School for ...
- The Physically Based Rendering book authors
- 

## Gallery

<p align="center">
  <img src="https://github.com/GaryHuan9/Echo/assets/22217952/495304c1-28d8-4615-ad3d-01d48497cc5a" width="500"><br>
  <i>Canonical Cornell Box</i><br><br>
  <img src="https://github.com/GaryHuan9/Echo/assets/22217952/3e24be36-278d-42a8-9ef0-254044668107" width="500"><br>
  <i>Rough Glass Material Ball</i><br><br>
  <img src="https://github.com/GaryHuan9/Echo/assets/22217952/396fa72b-5028-40cd-95c6-40b377cf9ee5" width="1000"><br>
  <i>Lego 856 Bulldozer on a Table, model by Heinzelnisse (CC-BY-NC) and PolyHaven (CC0)</i><br><br>
  <img src="https://github.com/GaryHuan9/Echo/assets/22217952/315842b1-2972-4700-bd48-81839e932a21" width="1000"><br>
  <i>Two Blue Bugatti Chiron, model by zizian on Sketchfab (CC BY-NC 4.0)</i><br><br>
  <img src="https://github.com/GaryHuan9/Echo/assets/22217952/4db786f8-792b-49c1-867c-3f6ada4a1f6c" width="1000"><br>
  <i>Echo.UserInterface During a Render</i><br><br>
</p>
