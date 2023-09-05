# Core Systems

---

:construction: this page is currently under construction! :construction:

The main `Echo.Core` is composed of seven major systems, each contributing a different purpose towards to the goal of synthesizing photorealistic images. These systems occupy various namespaces under `Echo.Core` and are catalogued below in order of relative importance.

<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#evaluation">Evaluation</a></li>
    <li><a href="#aggregation">Aggregation</a></li>
    <li><a href="#processes">Processes</a></li>
    <li><a href="#scenic">Scenic</a></li>
    <li><a href="#textures">Textures</a></li>
    <li><a href="#common">Common</a></li>
    <li><a href="#in-out">In Out</a></li>
  </ol>
</details>

---

## Evaluation
The `Echo.Core.Evaluation` namespace contains systems related to the evaluation of a `Scene` (many evaluations are composited together to create a render). Besides [aggregation](#Aggregation), this namespace occupies the majority of the runtime during rendering. Its systems are listed here:
- An `Evaluator` defines how a `Scene` is evaluated. It takes in a `PreparedScene` and a `Ray`, then returns a corresponding `Float4` which represents the evaluated data. The `PathTracedEvaluator` does exactly as its name suggests; it evaluates the `Scene` using path tracing with multiple importance sampling and next-event estimation. A simpler evaluator is the `BruteForcedEvaluator`, which implements the naive recursive path tracing algorithm; it is sometimes used as a reference evaluator. Besides evaluators, there is also `EvaluatorStatistics` that features a very lightweight statistics capture system and is mainly used by the `PathTracedEvaluator`.
- A `Material` defines the physical properties of a geometric surface when it interacts with light. In the current version of Echo, each `Material` can produce one or more `BxDF`s (described below) upon a `Ray` interaction. All materials are presented with more detail in [Materials](3-materials.md).
- To support sampling techniques such as multiple importance sampling, Echo implements various distributions which can easily sampled from. Normally, a `Sample1D` is first drawn from a `ContinuousDistribution` (a `Sample1D` is just a number `>=` 0 and `<` 1), and then used to sample a `DiscreteDistribution1D`, or select from some other source. Note that Echo evaluates the `Scene` in epochs, and the size of each epoch is precisely the value of `Extend` in the `ContinuousDistribution`. This is also why `EvaluationProfile` uses `MinEpoch` and `MaxEpoch`, and why number of samples per pixel is `Extend` * `Epoch`. 
- All light scattering behavior are contained in various `BxDF` implementations. `BxDF` stands for bidirectional X distribution function, where the X can be either reflective or transmissive. A set of `BxDF` is created and stored in a `BSDF` when a ray interacts with a surface, and that `BSDF` wraps the functionality of the containing `BxDF`. Note that all `BxDF` calculation are done in local space of the surface, where the surface normal points in the positive Z direction. Additionally, an `outgoing` direction points from the point of interaction towards the camera, while an `incoming` direction points towards the light.

## Aggregation
The `Echo.Core.Aggregation` namespace manages spatial calculations done on the `Scene`. This mainly includes the `Ray`-`Scene` intersection testing and light picking.
- An `Accelerator` improves the speed of 
- Light selection
- Preparation

## Processes
The `Echo.Core.Processes` namespace oversees the entire rendering process using `Operation`s; it handles the full lifecycle of a render, containing the following three stages.
- Preparation
- Evaluation
- Composition

## Scenic
The `Echo.Core.Scenic` namespace has the necessary components to building a `Scene`.
- Geometric objects
- Lights
- Instancing
- Preparation

## Textures
The `Echo.Core.Textures` namespace defines various implementations of `Texture`s, which are 2D planes that can be samples at arbitrary coordinates to get colors.
- Colors
- Textures
- Directional

## Common
The `Echo.Core.Common` namespace
- Compute
- Mathematics
- Packed

## In Out
The `Echo.Core.InOut` namespace
- Echo Description
- Images
- Models
