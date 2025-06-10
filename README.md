# Digital_Twin_Indoor_Positioning
Thesis of master

## GameObject
- **AnchorManager** : Load four APs and run related scripts
- **roof** : The entire interior space model

## Script
Some scripts are reserved for debugging purposes. The ones currently used are described in detail below.

- **Gau_RayTracing_Multi.cs** : Multi-base station ray-launching system based on Gaussian distribution.
- **Gau_MH_RayTracing_Multi.cs** : Multi-base station ray-launching system based on Gaussian distribution + Metropolis-Hastings (MH) sampling.
- **VonM_RayTracing_Multi.cs.cs** : Multi-base station ray-launching system based on Von Mises distribution.
- **IntersectionDetector.cs** : Calculate the intersection point of the rays emitted by the four base stations based on the ray information and collision point information.
- **IntersectionLogger.cs** : Store intersection information for subsequent viewing and processing.
- **LeastSquaresSolution.cs** : Positioning results calculated by the least squares method.
- **VoxelIntersectionCounter.cs** : Calculate the positioning error in a voxel-based manner.
- **GlobalDataManager.cs** : Read the json file and convert it into a usable type.
- **Objects/..** : Positioning data and other structures.

## Estimating location based on voxel-based crossing counts

[Start] 
   ↓  
[Input AoA measurements (y), AoA distributions p(θ|y), 3D digital twin]
   ↓  
[For each base station (n BS), sample l angles based on p(θ|y_i)]
   ↓  
[Generate n × l rays from all BS using sampled directions]
   ↓  
[For each voxel (X_k) in the scene:]
     ├─ Collect all rays intersecting X_k
     └─ Count valid n-ray combinations (each from different BS)
   ↓  
[Compute β_k = number of valid combinations]
   ↓  
[Find voxel with maximum β_k → Estimated Position]
   ↓  
[Output: Most likely voxel X_k]
