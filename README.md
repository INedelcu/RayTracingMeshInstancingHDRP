# RayTracingMeshInstancingSimple
Unity sample project using instancing and per-instance shader properties in HDRP Path Tracing.

## Description
The project uses [RayTracingAccelerationStructure.AddInstances](https://docs.unity3d.com/2023.1/Documentation/ScriptReference/Rendering.RayTracingAccelerationStructure.AddInstances.html) function to add many ray tracing instances of a Mesh to an acceleration structure. HDRP Path Tracing is used to generate a high-quality image.

## Prerequisites

* Windows 10 version 1809 and above.
* GPU supporting Ray Tracing ([SystemInfo.supportsRayTracing](https://docs.unity3d.com/2023.1/Documentation/ScriptReference/SystemInfo-supportsRayTracing.html) must be true).
* Unity 2023.1.0a17+.

## Resources
* [DirectX Raytracing (DXR) specs](https://microsoft.github.io/DirectX-Specs/d3d/Raytracing.html)
* [Unity Forum](https://forum.unity.com)
