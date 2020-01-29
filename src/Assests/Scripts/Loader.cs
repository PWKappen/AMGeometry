using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Loader : MonoBehaviour {

    public Vector3 min;
    public Vector3 max;
    public CameraScript camera;
    public bool mergeVertices = true;
    public GameObject meshPrefab;
    public GameObject testCube;
    public uint xResolution = 128;
    public uint yResolution = 128;
    public uint zResolution = 128;
    public uint resolution = 128;
    public Vector3 direction = new Vector3(0, 1, 0);
    public uint minLayer = 0;
    public uint maxLayer = 100000;
    public bool xLayering = true, yLayering = false, zLayering = false;

    public List<STLLoader.Vec3> startPlane;
    public Color startColor;
    public Color endColor;
    public int periode;
    public Material templateMaterial;

    public string fileName;

    public Material pickedMaterial;
    public ConvexHullMangager convexHull;
    public PlaneController planeController;

    private STLLoader.Voxelizer voxelizer;
    private STLLoader.Mesh[] mesh;
    private STLLoader.Mesh[] voxelMesh;
    private List<STLLoader.IndexMesh>[] indexedVoxelMesh;

    private bool objectHasChanged;
    private bool voxelMeshHasChanged;
    private int numPicks;
    private STLLoader.Vec3[] pickedVoxel;

    private int changeUpLayer;
    private int changeDownLayer;
    private bool layersSet;
    private bool chooseConvexHullObject;
    private bool showPlane;
    private bool choosePlane;

    private List<STLLoader.Vec3> voxelPlane;
    private List<List<int>> convexHullOutline;
    private Material[] materials;

    public DrawConvexHullEdges convexHullEdges;
    public DrawPath drawPath;
    public void Start()
    {
        showPlane = false;
        objectHasChanged = true;
        voxelMeshHasChanged = true;
        if (minLayer < 0)
            minLayer = 0;
        if (maxLayer > resolution)
            maxLayer = resolution;
        pickedVoxel = new STLLoader.Vec3[3];
        numPicks = 0;
        indexedVoxelMesh = new List<STLLoader.IndexMesh>[resolution + 2];
        mesh = new STLLoader.Mesh[1];
        voxelPlane = new List<STLLoader.Vec3>();
        materials = new Material[periode];

        for (int i = 0; i < periode; ++i)
        {
            materials[i] = new Material(templateMaterial);
            materials[i].color = Color.Lerp(startColor, endColor, (float)i / (float)(periode - 1));
        }

        convexHullEdges.isActive = false;
        convexHullOutline = new List<List<int>>();
    }

    public void LoadSTLMesh()
    {
        STLLoader.Mesh tmpMesh;
        tmpMesh = STLLoader.STLLoader.LoadMeshFileBrowser();
        if (tmpMesh == null)
            return;

        mesh[0] = tmpMesh;

        transform.localScale = new Vector3(1, 1, 1);
        transform.position = new Vector3(0, 0, 0);

        Transform[] ts = gameObject.GetComponentsInChildren<Transform>();
        for (int i = 1; i < ts.Length; ++i)
            Destroy(ts[i].gameObject);

        if (!mergeVertices)
            CreateMesh(ref mesh[0]); //Sets indices, normals etc. without merging faces.
        else
            CreateIndexedMesh(ref mesh, (int)resolution + 1);

        objectHasChanged = true;
        voxelMeshHasChanged = false;

        min = new Vector3(mesh[0].minBound.x, mesh[0].minBound.y, mesh[0].minBound.z);
        max = new Vector3(mesh[0].maxBound.x, mesh[0].maxBound.y, mesh[0].maxBound.z);
        AdjustCamera(); //Adjustst the rotation of the Mesh, Camera position and view frustum size.
    }

    public void RecreateMesh()
    {
        if (voxelizer != null)
        {
            if (layersSet)
            {
                if (changeUpLayer != 0)
                {
                    if (changeUpLayer > 0)
                    {
                        voxelMesh = voxelizer.GenerateMeshLayerWise((int)maxLayer, (int)maxLayer + changeUpLayer);
                        Transform[] ts = gameObject.GetComponentsInChildren<Transform>();

                        for (int i = 0; i < ts.Length; ++i)
                        {
                            if (ts[i].name == maxLayer + "_" || ts[i].name == (maxLayer + changeUpLayer) + "_")
                                Destroy(ts[i].gameObject);
                        }

                        CreateIndexedMesh(ref voxelMesh, (int)maxLayer);
                    }
                    else
                    {
                        voxelMesh = voxelizer.GenerateMeshLayerWise((int)maxLayer + changeUpLayer - 2, (int)maxLayer + changeUpLayer);
                        Transform[] ts = gameObject.GetComponentsInChildren<Transform>();

                        for (int i = 0; i < ts.Length; ++i)
                        {
                            if (ts[i].name == (maxLayer + changeUpLayer) + "_" || ts[i].name == (maxLayer + changeUpLayer - 1) + "_")
                                Destroy(ts[i].gameObject);

                        }
                        CreateIndexedMesh(ref voxelMesh[1], (int)maxLayer + changeUpLayer - 1);

                    }
                    maxLayer = (uint)((int)maxLayer + changeUpLayer);
                }
            }
        }
    }

    public void VoxelizeMesh()
    {
        if (mesh[0] == null)
            return;
        if (!objectHasChanged)
            return;

        Transform[] ts = gameObject.GetComponentsInChildren<Transform>();
        for (int i = 1; i < ts.Length; ++i)
            Destroy(ts[i].gameObject);
        STLLoader.Vec3[] direction = new STLLoader.Vec3[3];
        direction[0] = new STLLoader.Vec3(1, 0, 0);
        direction[1] = new STLLoader.Vec3(0, 1, 0);
        direction[2] = new STLLoader.Vec3(0, 0, 1);


        voxelizer = new STLLoader.Voxelizer(xResolution, yResolution, zResolution);
        voxelizer.MultiyDirectionsVoxelize(mesh[0], direction);
        List<STLLoader.Vec3> localCorners = voxelizer.CalculateLocalCorners();

        STLLoader.Quickhull quickhull = new STLLoader.Quickhull();
        List<int> convexFaces = new List<int>();
        quickhull.CalculateQuickhull(localCorners, ref convexFaces);
        convexHullOutline.Clear();
        quickhull.CalculatePolygonEdges(ref convexHullOutline);
        STLLoader.Vec3 centerOfMass = voxelizer.GetCenterOfMass();

        //voxelizer.SafeLayer(fileName, 0);
        voxelMesh = new STLLoader.Mesh[1];
 
        voxelMesh[0] = voxelizer.GenerateMesh(0, resolution);

        float voxelSize = voxelizer.GetVoxelSize();
        STLLoader.Vec3 offset = voxelizer.GetMinOffset();

        convexHull.ConfigureConvexHull(ref localCorners, ref convexFaces, voxelSize, new Vector3(offset.x, offset.y, offset.z), ref convexHullOutline);


        CreateIndexedMesh(ref voxelMesh, (int)resolution);

        transform.localScale = new Vector3(voxelSize, voxelSize, voxelSize);
        transform.position = new Vector3(offset.x, offset.y, offset.z);


        objectHasChanged = false;
        voxelMeshHasChanged = false;
    }

    private float FindLongest(Vector3 max, Vector3 min) // Finds the side which has the longest absolut value in order to positioning the camera (else parts of the mesh will be culled).
    {
        return max.magnitude > min.magnitude ? max.magnitude : min.magnitude;
    }

    private void CreateMesh(ref STLLoader.Mesh mesh)
    {
        GameObject obj = (GameObject)Instantiate(meshPrefab, Vector3.zero, Quaternion.identity);
        obj.GetComponent<MeshPrefab>().AddMesh(mesh, (int)resolution + 1);
        obj.transform.parent = gameObject.transform;
        obj.transform.position = Vector3.zero;
        obj.transform.localScale = Vector3.one;
    }

    private void CreateIndexedMesh(ref STLLoader.Mesh[] mesh, int startPos)
    {
        for (int i = 0; i < mesh.Length; ++i)
        {
            if (mesh[i] == null)
                continue;
            indexedVoxelMesh[i + startPos] = STLLoader.STLLoader.CreateIndexedMesh(mesh[i], 65000);
            for (int j = 0; j < indexedVoxelMesh[i + startPos].Count; ++j)
            {
                GameObject obj = (GameObject)Instantiate(meshPrefab, Vector3.zero, Quaternion.identity);
                obj.GetComponent<MeshPrefab>().AddIndexedMesh(indexedVoxelMesh[i + startPos][j], i + startPos);
                obj.transform.parent = gameObject.transform;
                obj.transform.localScale = Vector3.one;
                obj.transform.localPosition = Vector3.zero;
                obj.name = i + startPos + "_";
            }
        }
    }

    private void CreateIndexedMesh(ref STLLoader.Mesh[] mesh, int startPos, string addString)
    {
        for (int i = 0; i < mesh.Length; ++i)
        {
            if (mesh[i] == null)
                continue;
            indexedVoxelMesh[i + startPos] = STLLoader.STLLoader.CreateIndexedMesh(mesh[i], 65000);

            for (int j = 0; j < indexedVoxelMesh[i + startPos].Count; ++j)
            {
                GameObject obj = (GameObject)Instantiate(meshPrefab, Vector3.zero, Quaternion.identity);
                obj.GetComponent<MeshPrefab>().AddIndexedMesh(indexedVoxelMesh[i + startPos][j], i + startPos);
                obj.transform.parent = gameObject.transform;
                obj.transform.localScale = Vector3.one;
                obj.transform.localPosition = Vector3.zero;
                obj.name = i + startPos + "_" + addString;
            }
        }
    }

    private void CreateIndexedMesh(ref STLLoader.Mesh mesh, int startPos)
    {
        if (mesh == null)
            return;
        indexedVoxelMesh[startPos] = STLLoader.STLLoader.CreateIndexedMesh(mesh, 65000);

        for (int j = 0; j < indexedVoxelMesh[startPos].Count; ++j)
        {
            GameObject obj = (GameObject)Instantiate(meshPrefab, Vector3.zero, Quaternion.identity);
            obj.GetComponent<MeshPrefab>().AddIndexedMesh(indexedVoxelMesh[startPos][j], startPos);
            obj.transform.parent = gameObject.transform;
            obj.transform.localScale = Vector3.one;
            obj.transform.localPosition = Vector3.zero;
            obj.name = startPos + "_";
        }
    }


    private void AdjustCamera()
    {
        int longestSide = 0;
        for (int i = 0; i < 3; ++i)
            longestSide = ((max[i] - min[i]) > (max[longestSide] - min[longestSide])) ? i : longestSide;

        int secondLongest = (longestSide + 1) % 3;
        for (int i = 0; i < 3; ++i)
            if (i != longestSide)
                secondLongest = ((max[i] - min[i]) > (max[secondLongest] - min[secondLongest])) ? i : secondLongest;

        if (longestSide != 1 && secondLongest != 1)
        {
            //transform.rotation = Quaternion.AngleAxis(-90, Vector3.right);
            //max = Quaternion.AngleAxis(-90, Vector3.right) * max;
            //min = Quaternion.AngleAxis(-90, Vector3.right) * min;
        }
        if (longestSide != 0 && secondLongest != 0)
        {
            //transform.rotation = Quaternion.AngleAxis(90, Vector3.up);
            //max = Quaternion.AngleAxis(90, Vector3.up) * max;
            //min = Quaternion.AngleAxis(90, Vector3.up) * min;
        }


        camera.lookAt = (max + min) / 2f;
        camera.eye = (max + min) / 2f - Vector3.forward * 5 * (FindLongest(max, min));
        float size = ((max.x - min.x) > (max.y - min.y)) ? (max.x - min.x) : (max.y - min.y);
        camera.AddNear(Vector3.forward);
        camera.NewEye(size / 2f);
    }

    public void SetLayerDirection()
    {
        if (voxelizer != null)
        {
            if(startPlane == null)
                startPlane = new List<STLLoader.Vec3>();
            voxelizer.CreateLayer(ref startPlane);
            Transform[] ts = gameObject.GetComponentsInChildren<Transform>();
            for (int i = 1; i < ts.Length; ++i)
                Destroy(ts[i].gameObject);
            
            voxelMesh = voxelizer.GenerateMeshLayerWise(0, (int)resolution);

            CreateIndexedMesh(ref voxelMesh, 0);
            layersSet = true;


            MeshPrefab[] prefab = gameObject.GetComponentsInChildren<MeshPrefab>();
            int layer;
            for (int i = 0; i < prefab.Length; ++i)
            {
                layer = prefab[i].layer;
                if (layer < resolution)
                {
                    prefab[i].renderer.material = materials[layer % periode];
                }
            }

            /*STLLoader.PathPlanner planner = new STLLoader.PathPlanner();
            planner.FindLayer(3, voxelizer.GetGrid());
            List<STLLoader.Vec3> path = planner.FindPath(voxelizer.GetGrid(), planner.result, 3);
            drawPath.path = path;
            drawPath.isActive = true;
            drawPath.trans = transform.localToWorldMatrix;*/
        }
    }

    void Update()
    {
        if (!showPlane)
        {
            changeUpLayer = 0;
            changeDownLayer = 0;
            if (Input.GetKeyDown("w"))
            {
                changeUpLayer = maxLayer <= resolution ? ++changeUpLayer : changeUpLayer;
                voxelMeshHasChanged = true;
            }

            if (Input.GetKeyDown("escape"))
                Application.Quit();

            if (Input.GetKeyDown("e"))
            {
                changeDownLayer = minLayer < maxLayer ? ++changeDownLayer : changeDownLayer;
                voxelMeshHasChanged = true;
            }
            if (Input.GetKeyDown("s"))
            {
                changeUpLayer = maxLayer > minLayer ? --changeUpLayer : changeUpLayer;
                voxelMeshHasChanged = true;
            }
            if (Input.GetKeyDown("d"))
            {
                changeDownLayer = minLayer > 0 ? --changeDownLayer : changeDownLayer;
                voxelMeshHasChanged = true;
            }

            if (voxelMeshHasChanged)
            {
                RecreateMesh();
                voxelMeshHasChanged = false;
            }
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                chooseConvexHullObject = true;
                convexHull.gameObject.SetActive(true);
                convexHullEdges.isActive = true;
            }
            if (Input.GetKeyUp(KeyCode.LeftControl))
            {
                chooseConvexHullObject = false;
                convexHull.gameObject.SetActive(false);
                convexHullEdges.isActive = false;
            }


            if (Input.GetMouseButtonDown(0))
            {
                if (chooseConvexHullObject)
                {
                    Vector3 p1 = new Vector3();
                    Vector3 p2 = new Vector3();
                    Vector3 p3 = new Vector3();
                    if (convexHull.FindFaceOnConvexHull(ref p1, ref p2, ref p3))
                    {
                        float scale = voxelizer.GetSizeVoxel();
                        STLLoader.Vec3 offset = voxelizer.GetMinOffset();


                        STLLoader.Vec3 voxelP1 = new STLLoader.Vec3(p1.x, p1.y, p1.z);
                        STLLoader.Vec3 voxelP2 = new STLLoader.Vec3(p2.x, p2.y, p2.z);
                        STLLoader.Vec3 voxelP3 = new STLLoader.Vec3(p3.x, p3.y, p3.z);

                        List<STLLoader.Vec3> voxelPlane = voxelizer.GetAllVoxelOnPlane(voxelP1, voxelP2, voxelP3, false);
                        for (int i = 0; i < voxelPlane.Count; ++i)
                        {
                            GameObject ins1 = Instantiate<GameObject>(testCube);
                            STLLoader.Vec3 minOffset1 = voxelizer.GetMinOffset();
                            float sizeVoxel1 = voxelizer.GetSizeVoxel();
                            ins1.transform.position = new Vector3((voxelPlane[i].x + 0.5f) * sizeVoxel1 + minOffset1.x, (voxelPlane[i].y + 0.5f) * sizeVoxel1 + minOffset1.y, (voxelPlane[i].z + 0.5f) * sizeVoxel1 + minOffset1.z);
                            ins1.transform.localScale = new Vector3(sizeVoxel1 + 0.2f * sizeVoxel1, sizeVoxel1 + 0.1f * sizeVoxel1, sizeVoxel1 + 0.1f * sizeVoxel1);
                            ins1.transform.parent = transform;
                        }
                        startPlane = voxelPlane;
                    }
                }
                else if (Input.GetKey(KeyCode.LeftAlt))
                    PickVoxel();
            }
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(1))
                RemoveUnderPlane();
        }
    }

    public void ChoosePlane()
    {
        if (showPlane == false && !choosePlane)
        {
            planeController.gameObject.SetActive(true);
            showPlane = true;
            choosePlane = true;
            STLLoader.Vec3 offset = voxelizer.GetCenterOfMass() * voxelizer.GetSizeVoxel() + voxelizer.GetMinOffset();
            float voxelScale = voxelizer.GetSizeVoxel() * 2f * resolution;
            planeController.transform.position = new Vector3(offset.x, offset.y, offset.z);
            planeController.transform.localScale = new Vector3(voxelScale, voxelScale / 2f / (int)resolution, voxelScale);
        }
        else
        {
            planeController.gameObject.SetActive(false);
            showPlane = false;
            choosePlane = false;
        }
       
    }

    public void ShowLayer(string layer)
    {
        int layerInt;
        if (int.TryParse(layer, out layerInt))
            ShowLayer(layerInt); 
    }

    private void ShowLayer(int layer)
    {
        if (voxelizer != null)
        {
            if (layer > 0 || layer >= resolution)
            {
                voxelMesh = voxelizer.GenerateMeshLayerWise(layer, layer + 1);
                Transform[] ts = gameObject.GetComponentsInChildren<Transform>();

                for (int i = 1; i < ts.Length; ++i)
                {
                    ts[i].gameObject.SetActive(false);
                }

                CreateIndexedMesh(ref voxelMesh, 0,"tmp");
            }
        }
    }

    public void ShowComplete()
    {
        if (voxelizer != null)
        {
            Transform[] ts = gameObject.GetComponentsInChildren<Transform>(true);
            for (int i = 1; i < ts.Length; ++i)
            {
                ts[i].gameObject.SetActive(true);
                if (ts[i].name.Contains("tmp"))
                    Destroy(ts[i].gameObject);
            }

            if (layersSet)
            {
                voxelMesh = voxelizer.GenerateMeshLayerWise(0, (int)resolution);

                CreateIndexedMesh(ref voxelMesh, 0);
                layersSet = true;


                MeshPrefab[] prefab = gameObject.GetComponentsInChildren<MeshPrefab>();
                int layer;
                for (int i = 0; i < prefab.Length; ++i)
                {
                    layer = prefab[i].layer;
                    if (layer < resolution)
                    {
                        prefab[i].renderer.material = materials[layer % periode];
                    }
                }
            }
            else
            {
                voxelMesh[0] = voxelizer.GenerateMesh(0, resolution);
                CreateIndexedMesh(ref voxelMesh, (int)resolution);
                for (int i = 1; i < ts.Length; ++i)
                {
                    if (ts[i].name.Contains(resolution + ""));
                        Destroy(ts[i].gameObject);
                }
            }
        }
    }

    public void ShowNewPlane(Vector3 normal, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        planeController.gameObject.SetActive(false);
        showPlane = false;
        choosePlane = false;
        if (voxelizer != null)
        {
            planeController.gameObject.SetActive(false);
            showPlane = false;
            float voxelScale = voxelizer.GetSizeVoxel();
            STLLoader.Vec3 offset = voxelizer.GetMinOffset();

            STLLoader.Vec3 newP1 = new STLLoader.Vec3(p1.x, p1.y, p1.z);
            STLLoader.Vec3 newP2 = new STLLoader.Vec3(p2.x, p2.y, p2.z);
            STLLoader.Vec3 newP3 = new STLLoader.Vec3(p3.x, p3.y, p3.z);

            newP1 = (newP1 - offset) / voxelScale;
            newP2 = (newP2 - offset) / voxelScale;
            newP3 = (newP3 - offset) / voxelScale;

            List<STLLoader.Vec3> voxelPlane = voxelizer.GetAllVoxelOnPlane(newP1, newP2, newP3, true);
            if (voxelPlane.Count != 0)
            {
                Transform[] ts = gameObject.GetComponentsInChildren<Transform>();
                for (int i = 1; i < ts.Length; ++i)
                    Destroy(ts[i].gameObject);

                voxelizer.GenerateMeshPlane(ref voxelPlane, new STLLoader.Vec3(normal.x, normal.y, normal.z), newP1, false);
                STLLoader.Mesh[] mesh;
                if (layersSet)
                {
                    mesh = voxelizer.GenerateMeshLayerWiseVisible(0, (int)resolution);
                    CreateIndexedMesh(ref mesh, 0);
                    MeshPrefab[] prefab = gameObject.GetComponentsInChildren<MeshPrefab>();
                    int layer;
                    for (int i = 0; i < prefab.Length; ++i)
                    {
                        layer = prefab[i].layer;
                        if (layer < resolution)
                        {
                            prefab[i].renderer.material = materials[layer % periode];
                        }
                    }
                }
                else
                {
                    mesh = new STLLoader.Mesh[1];
                    mesh[0] = voxelizer.GenerateMeshVisible();
                    CreateIndexedMesh(ref mesh, (int)resolution);
                }
                voxelizer.GenerateMeshPlane(ref voxelPlane, new STLLoader.Vec3(normal.x, normal.y, normal.z), newP1, true);
            }
        }
    }

    public void SetPickedVoxel(STLLoader.Vec3 point)
    {
        float scale = voxelizer.GetSizeVoxel();
        STLLoader.Vec3 offset = voxelizer.GetMinOffset();
        float distance = 3f/4f;
        STLLoader.Vec3 voxel;
        STLLoader.Vec3 tmpVec;
        Transform[] ts = gameObject.GetComponentsInChildren<Transform>();
        for (int i = 0; i < indexedVoxelMesh[resolution].Count; ++i)
        {
            for (int j = 0; j < indexedVoxelMesh[resolution][i].indices.Count; j += 3)
            {
                voxel = indexedVoxelMesh[resolution][i].vertices[indexedVoxelMesh[resolution][i].indices[j]];
                tmpVec = voxel - point;
                if (tmpVec.x * tmpVec.x + tmpVec.y * tmpVec.y + tmpVec.z * tmpVec.z > distance)
                    continue;
                voxel = indexedVoxelMesh[resolution][i].vertices[indexedVoxelMesh[resolution][i].indices[j+1]];
                tmpVec = voxel - point;
                if (tmpVec.x * tmpVec.x + tmpVec.y * tmpVec.y + tmpVec.z * tmpVec.z > distance)
                    continue;
                voxel = indexedVoxelMesh[resolution][i].vertices[indexedVoxelMesh[resolution][i].indices[j+2]];
                tmpVec = voxel - point;
                if (tmpVec.x * tmpVec.x + tmpVec.y * tmpVec.y + tmpVec.z * tmpVec.z > distance)
                    continue;
                (ts[i + 1].GetComponent<MeshPrefab>()).SetPickedVoxel( pickedMaterial, j, 1);
            }
        }
    }

    public void PickVoxel()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        STLLoader.Vec3 res = voxelizer.PickVoxel(new STLLoader.Vec3(ray.origin.x, ray.origin.y, ray.origin.z), new STLLoader.Vec3(ray.direction.x, ray.direction.y, ray.direction.z));
        if (res.x < 0)
            return;
        numPicks = (numPicks + 1) % 3;
        pickedVoxel[numPicks] = res + new STLLoader.Vec3(0.5f, 0.5f, 0.5f);
        SetPickedVoxel((res + new STLLoader.Vec3(0.5f, 0.5f, 0.5f)));
        if (numPicks == 0)
        {
            voxelPlane = voxelizer.GetAllVoxelOnPlane(pickedVoxel[0], pickedVoxel[1], pickedVoxel[2], false);
            for(int i = 0; i < voxelPlane.Count; ++i)
            {
               GameObject ins1 = Instantiate<GameObject>(testCube);
               STLLoader.Vec3 minOffset1 = voxelizer.GetMinOffset();
               float sizeVoxel1 = voxelizer.GetSizeVoxel();
               ins1.transform.position = new Vector3((voxelPlane[i].x + 0.5f) * sizeVoxel1 + minOffset1.x, (voxelPlane[i].y + 0.5f) * sizeVoxel1 + minOffset1.y, (voxelPlane[i].z + 0.5f) * sizeVoxel1 + minOffset1.z);
               ins1.transform.localScale = new Vector3(sizeVoxel1 + 0.2f * sizeVoxel1, sizeVoxel1 + 0.1f * sizeVoxel1, sizeVoxel1 + 0.1f * sizeVoxel1);
               ins1.transform.parent = transform;
            }
        }
    }

    private void RemoveUnderPlane()
    {
        if (numPicks == 0)
        {
            voxelPlane = voxelizer.GetAllVoxelOnPlane(pickedVoxel[0], pickedVoxel[1], pickedVoxel[2], true);
            startPlane = voxelPlane;
            voxelizer.RemoveVoxelPlane(ref voxelPlane, pickedVoxel[0], pickedVoxel[1], pickedVoxel[2]);

            List<STLLoader.Vec3> localCorners = voxelizer.CalculateLocalCorners();
            voxelizer.CalculateCenterOfMass();

            Transform[] ts = gameObject.GetComponentsInChildren<Transform>();
            for (int i = 1; i < ts.Length; ++i)
                Destroy(ts[i].gameObject);

            STLLoader.Quickhull quickhull = new STLLoader.Quickhull();
            List<int> convexFaces = new List<int>();
            quickhull.CalculateQuickhull(localCorners, ref convexFaces);
            convexHullOutline.Clear();
            quickhull.CalculatePolygonEdges(ref convexHullOutline);

            voxelMesh = new STLLoader.Mesh[1];
            voxelMesh[0] = voxelizer.GenerateMesh(0, resolution);
            float voxelSize = voxelizer.GetVoxelSize();
            STLLoader.Vec3 offset = voxelizer.GetMinOffset();

            convexHull.ConfigureConvexHull(ref localCorners, ref convexFaces, voxelSize, new Vector3(offset.x, offset.y, offset.z), ref convexHullOutline);

            CreateIndexedMesh(ref voxelMesh, (int)resolution);
        }
    }
}
