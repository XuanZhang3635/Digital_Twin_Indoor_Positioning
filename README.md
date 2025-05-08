# Digital_Twin_Indoor_Positioning
Thesis of master

## GameObject
- **AnchorManager** : Load four APs and run related scripts
- **roof** : The entire interior space model

## Script
- **MonteCarloRayTracingcs** : Implements Monte Carlo algorithms for simulating realistic light reflecting.
- **IntersectionDetector.cs** : Calculate the intersection point of the rays emitted by the four base stations based on the ray information and collision point information.
- **IntersectionLogger.cs** : Store intersection information for subsequent viewing and processing.
- **LeastSquaresSolution.cs** : Positioning results calculated by the least squares method.
- **RayTracing_MultiFrame.cs** : Ray tracing Estimation with multiple measurements.
- **VoxelIntersectionCounter.cs** : Calculate the positioning error in a voxel-based manner.
- **GlobalDataManager.cs** : Read the json file and convert it into a usable type.
- **Objects/..** : Positioning data and other structures.
