# AMGeometry
Geometry processing for additive manufacturing.  
This software was created to perform geometry processing for additive manufacturing. It consists of two parts; a managed DLL written in C# and a Unity3D application. The DLL contains various geometry processing algorithms. The Unity3D application visualizes the results and provides a basic user interface to perform those operations.  
Algorithms for the following geometry processing problems are included in the library:  
*	STL file loading
*	3D Triangle mesh voxelization
*	Mesh extraction from a voxel representation
*	Computation of 3D convex hulls
*	Computation of the median axis
*	Computation of the euclidean skeleton
*	Simple layering of the voxel model
*	Simple Path planning operations

The Unity 3D application provides an interface for the following operations:

*	Loading of a 3D mesh from disk
*	Voxelization of the loaded 3D mesh
*	Visualizing the computed convex hull
*	Layering
*	Visualization of single layers
*	Cutting of the 3D model

## Example
Following are a few example images of the application.  
Figure 1 shows a voxelization of the Stanford bunny with a resolution of 512, in every dimension, in the application.  
![](images/voxlized_bunny.png)  
Figure 2 shows the computed convex hull of the bunny, in the application.  
![](images/voxlized_bunny_convex_hull.png)  
Figure 3 shows a cut through the bunny as well as a layering of it, in the application.  
![](images/cut_bunny_layering.png)  
An STL file of the Stanford bunny is available under the following link:  
https://www.thingiverse.com/thing:11622  

## Control
The Application contains the following GUI elements:  
* Load File: Opens a file selection window that can be used to load a STL model into the application.  
* Voxelize: Voxelizes the loaded model (requires a model to be loaded). This is a requirement for all following steps.  
* Create Layer: Creates a simple layering of the model.  
* Choose Plane: Creates a plane which can be used to cut the voxelized model.   
* Show Layer: Visualizes only a specific layer of the voxelized model.  
* Show Complete: Visualizes the complete voxelized model after cutting it or visualizing selected layers.  

Following keyboard inputs are available.  

* Holding the left mouse button allows rotation of the camera around the center point while holding the right mouse button allows the translation of the camera.  
* When holding the control button the convex hull is visualized and a left click on a face of the hull will select all voxels of it.  
* The plane which visualizes the cut through the voxelized model can be rotated with the q,e,z and x button. The cut will be executed by pressing space.  
* The application can be teminated by pressing escape.  
The library contains more functionality which is currently not completely integrated into the application.  

## Installation
In order to execute the application download the bin directory and execute the BuildExe.exe contained in it.  
The library can be used by including the compiled .dll file.  
## Building
To build the DLL add all the files from the src/GeometryProcessing directory to a Visual Studio managed DLL project. System.Windows.Forms must be added to the references of the project (It is needed for the DLL loader).  After those steps the DLL can be build.
To add the Unity3D parts to an application copy everything in the src/Assets directory into the respective Unity3D directory of the project or replace it completely. The used scene can be found in the Scene directory in Assets and is called TestScene. Opening it in Unity allows the modification of the Example application as well as the execution of it in Unity3D. All application scripts can be found in src/Assets/Scripts.
If a changed DLL should be used it is only necessary to replace the existing STLLoader.dll file in the Unity3D Asset/Plugin folder. The name must be kept to do it without requiring additional changes.


