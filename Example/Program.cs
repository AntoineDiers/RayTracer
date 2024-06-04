using System;
using System.Diagnostics;
using System.Windows.Media.Media3D;
using OpenCvSharp;
using RayTracerLib;

Scene scene = new();
RayTracerLib.Timer timer = new(0);

// -----------------------------------------------------------------
//                          MATERIALS
// -----------------------------------------------------------------

timer.Restart("Generating Materials...");

RayTracerLib.Material glassMaterial = new();
glassMaterial.color = new(100, 100, 100);
glassMaterial.specularCoeff = 0.1;
glassMaterial.diffuseCoeff = 0.1;
glassMaterial.transparencyCoeff = 0.98;
glassMaterial.transparencyIndex = 1.5;
int glassMaterialIndex = scene.materials.AddMaterial(glassMaterial);

RayTracerLib.Material whiteWallMaterial = new();
int whiteWallMaterialIndex = scene.materials.AddMaterial(whiteWallMaterial);

RayTracerLib.Material greenWallMaterial = new();
greenWallMaterial.color = new(9, 106, 9);
int greenWallMaterialIndex = scene.materials.AddMaterial(greenWallMaterial);

RayTracerLib.Material redWallMaterial = new();
redWallMaterial.color = new(170, 52, 39);
int redWallMaterialIndex = scene.materials.AddMaterial(redWallMaterial);

RayTracerLib.Material blueWallMaterial = new();
blueWallMaterial.color = new(39, 145, 171);
int blueWallMaterialIndex = scene.materials.AddMaterial(blueWallMaterial);

RayTracerLib.Material mirrorMaterial = new();
mirrorMaterial.color = new(255, 255, 255);
mirrorMaterial.reflectiveCoeff = 0.95;
int mirrorMaterialIndex = scene.materials.AddMaterial(mirrorMaterial);

timer.Print("");

// -----------------------------------------------------------------
//                          TEXTURES
// -----------------------------------------------------------------

timer.Restart("Generating Textures...");

int catTextureIndex = scene.textures.LoadTexture("../../../assets/cat/cat_texture.jpg");

timer.Print("");

// -----------------------------------------------------------------
//                          MESHES
// -----------------------------------------------------------------

timer.Restart("Generating Meshes...");

SmartMesh whiteWallMesh = MeshesFactory.CreateCube(whiteWallMaterialIndex);
SmartMesh redWallMesh = MeshesFactory.CreateCube(redWallMaterialIndex);
SmartMesh blueWallMesh = MeshesFactory.CreateCube(blueWallMaterialIndex);
SmartMesh catMesh = ObjParser.Parse("../../../assets/cat/cat.obj", whiteWallMaterialIndex, catTextureIndex);
SmartMesh glassSphereMesh = MeshesFactory.CreateSphere(100, glassMaterialIndex);
SmartMesh mirrorMesh = MeshesFactory.CreateCube(mirrorMaterialIndex);

timer.Print("");

// -----------------------------------------------------------------
//                          ASSETS
// -----------------------------------------------------------------

timer.Restart("Generating Assets...");

Asset frontWall = new(blueWallMesh);
frontWall.tf.SetScaling(new Vector3D(1, 100, 50));
frontWall.tf.SetTranslation(new(50,0,25));

Asset backWall = new(whiteWallMesh);
backWall.tf.SetScaling(new Vector3D(1, 100, 50));
backWall.tf.SetTranslation(new(-50, 0, 25));

Asset LeftWall = new(whiteWallMesh);
LeftWall.tf.SetScaling(new Vector3D(100, 1, 50));
LeftWall.tf.SetTranslation(new(0, 50, 25));

Asset RightWall = new(whiteWallMesh);
RightWall.tf.SetScaling(new Vector3D(100, 1, 50));
RightWall.tf.SetTranslation(new(0, -50, 25));

Asset floor = new(whiteWallMesh);
floor.tf.SetScaling(new Vector3D(100, 100, 1));
floor.tf.SetTranslation(new(0, 0, 0));

Asset ceiling = new(whiteWallMesh);
ceiling.tf.SetScaling(new Vector3D(100, 100, 1));
ceiling.tf.SetTranslation(new(0, 0, 50));

Asset redCube = new(redWallMesh);
redCube.tf.SetScaling(new Vector3D(20, 20, 20));
redCube.tf.SetTranslation(new(0, 0, 10));

Asset bluePillar = new(blueWallMesh);
bluePillar.tf.SetScaling(new Vector3D(5, 5, 25));
bluePillar.tf.SetTranslation(new(-30, 20, 10));
bluePillar.tf.SetRotation(new Vector3D(1, 0, 1), - Math.PI / 4);

Asset cat = new(catMesh);
cat.tf.SetScaling(new Vector3D(20, 20, 20));
cat.tf.SetTranslation(new(30, -20, 6.5));
cat.tf.SetRotation(new(0, 0, 1), -Math.PI / 2.0);
cat.tf.SetRotation(new Vector3D(0, -1 , 0), new Vector3D(1,0,0));

Asset glassSphere = new(glassSphereMesh);
glassSphere.tf.SetScaling(new Vector3D(10, 10, 10));
glassSphere.tf.SetTranslation(new(0, 0, 26));

Asset mirror = new(mirrorMesh);
mirror.tf.SetScaling(new Vector3D(30, 3, 30));
mirror.tf.SetTranslation(new(0, 50, 25));

scene.assets.Add(frontWall);
scene.assets.Add(backWall);
scene.assets.Add(LeftWall);
scene.assets.Add(RightWall);
scene.assets.Add(floor);
scene.assets.Add(ceiling);
scene.assets.Add(redCube);
scene.assets.Add(bluePillar);
scene.assets.Add(cat);
scene.assets.Add(glassSphere);
scene.assets.Add(mirror);

timer.Print("");

// -----------------------------------------------------------------
//                          LIGHT SOURCES
// -----------------------------------------------------------------

timer.Restart("Generating Light Sources...");

LightSource blueLight = new();
blueLight.position = new(-45, 45, 45);
blueLight.color = new(50, 50, 250);
blueLight.ambiantIntensity = 0.1;
blueLight.radius = 3;

LightSource redLight = new();
redLight.position = new(45, -45, 45);
redLight.color = new(250, 50, 50);
redLight.ambiantIntensity = 0.1;
redLight.radius = 3;

LightSource whiteLight = new();
redLight.position = new(0, 20, 45);
redLight.color = new(255, 255, 255);
redLight.ambiantIntensity = 0.1;
redLight.radius = 3;

scene.lightSources.Add(blueLight);
scene.lightSources.Add(redLight);
scene.lightSources.Add(whiteLight);

timer.Print("");

// -----------------------------------------------------------------
//                          Camera
// -----------------------------------------------------------------

timer.Restart("Setting up camera...");

scene.camera.tf.SetTranslation(new(-35, -40, 40));
scene.camera.tf.SetRotation(new Vector3D(1,1,-0.6), new Vector3D(-1, 1, 0));

timer.Print("");

// -----------------------------------------------------------------
//                          Running Raytracer
// -----------------------------------------------------------------

timer.Restart("Running Raytracer...");

RayTracer rayTracer = new();
OpenCvSharp.Mat img = rayTracer.Run(scene);

timer.Print("");

// -----------------------------------------------------------------
//                          Showing results
// -----------------------------------------------------------------

Cv2.ImShow("Raytracer", img);
Cv2.WaitKey(0);