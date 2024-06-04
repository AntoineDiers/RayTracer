# RayTracer

A basic ray tracing engine that runs on Cuda-compatible GPUs

## Algorithm Overview

1. Photon Mapping
   
Photon mapping consists in casting many photons from the light sources and registering where those photons end up after being reflected or refracted by transparent / reflective surfaces.
The generated photons are then stored in a KD-tree (to improve K-neighbor search performances) and later used to render caustics
The implemented approach is very naive but works well enough for me.


2. Raytracing

During raytracing, many "rays" are shot from the camera's position (one per output image pixel). 
The color of each of these rays is computed using Phong's model at the collision point between the ray and assets.
If the collided material is transparent or reflective, additional rays may be shot following Fresnel Coefficients

## References

Photon Mapping : https://web.cs.wpi.edu/~emmanuel/courses/cs563/write_ups/zackw/photon_mapping/PhotonMapping.html

Phong's Model : https://en.wikipedia.org/wiki/Phong_reflection_model

Fresnel Coefficients : https://fr.wikipedia.org/wiki/Coefficients_de_Fresnel

