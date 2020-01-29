using UnityEngine;
using System.Collections.Generic;

public class MeshPrefab : MonoBehaviour {

    private Vector3[] vertices;
    private Vector3[] normals;
    private int[] indices;
    public int layer;
    public Renderer renderer;

    public void AddMesh(STLLoader.Mesh mesh, int layer)
    {
        this.layer = layer;
        int vertexCount;
        vertexCount = mesh.vertices.Count;
        if (mesh.vertices.Count > 64998) // Unity restricts meshes to have no more than 6500 vertices.
            vertexCount = 64998;
        vertices = new Vector3[vertexCount];
        normals = new Vector3[vertexCount];
        indices = new int[vertexCount];
        int normalCounter = 0;
        for (int i = 0; i < vertexCount; ++i)
        {
            indices[i] = i;
            vertices[i] = new Vector3(mesh.vertices[i].x, mesh.vertices[i].y, mesh.vertices[i].z);
            normals[i] = new Vector3(mesh.normals[(i - normalCounter) / 3].x, mesh.normals[(i - normalCounter) / 3].y, mesh.normals[(i - normalCounter) / 3].z);
            normalCounter = (normalCounter + 1) % 3; //normals need to be set facewise.
        }

        Mesh mesh2 = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh2;
        mesh2.vertices = vertices;
        mesh2.triangles = indices;
        mesh2.normals = normals;
    }

    public void SetPickedVoxel(Material mat, int idx, int submeshNumber)
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        int[] submesh;
        int[] mainMesh = mesh.GetTriangles(0);
        if (mesh.subMeshCount > submeshNumber)
        {
            submesh = mesh.GetTriangles(submeshNumber);
            for (int i = 0; i < submesh.Length; i += 3)
            {
                if (submesh[i] == mainMesh[idx] && submesh[i + 1] == mainMesh[idx + 1] && submesh[i + 2] == mainMesh[idx + 2])
                    return;
            }

            System.Array.Resize<int>(ref submesh, submesh.Length + 3);
 
            submesh[submesh.Length - 3] = mainMesh[idx];
            submesh[submesh.Length - 2] = mainMesh[idx+1];
            submesh[submesh.Length - 1] = mainMesh[idx+2];
            mesh.SetTriangles(submesh, submeshNumber);
        }
        else
        {
            mesh.subMeshCount = submeshNumber+1;
            Material[] materials = GetComponent<Renderer>().materials;
            System.Array.Resize<Material>(ref materials, materials.Length + 1);
            materials[materials.Length - 1] = mat;
            GetComponent<Renderer>().materials = materials;
            submesh = new int[3];
            submesh[0] = mainMesh[idx];
            submesh[1] = mainMesh[idx + 1];
            submesh[2] = mainMesh[idx + 2];
            mesh.SetTriangles(submesh, submeshNumber);
        }
    }

    public void AddIndexedMesh(STLLoader.IndexMesh indexMesh, int layer)
    {
        this.layer = layer;
        int vertexCount;
        int indexCount;
        vertexCount = indexMesh.vertices.Count;
        indexCount = indexMesh.indices.Count;

        vertices = new Vector3[vertexCount];
        normals = new Vector3[vertexCount];
        indices = new int[indexCount];

        for (int i = 0; i < vertexCount; ++i)
        {
            vertices[i] = new Vector3(indexMesh.vertices[i].x, indexMesh.vertices[i].y, indexMesh.vertices[i].z);
            normals[i] = new Vector3(indexMesh.normals[i].x, indexMesh.normals[i].y, indexMesh.normals[i].z);
        }
        for(int i = 0; i < indexCount; ++i)
            indices[i] = indexMesh.indices[i];

        Mesh mesh2 = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh2;
        mesh2.vertices = vertices;
        mesh2.triangles = indices;
        mesh2.normals = normals;
    }

    public void AddIndexedMesh(List<STLLoader.Vec3> points, List<int> faces, int layer)
    {
        this.layer = layer;
        int vertexCount;
        int indexCount;
        vertexCount = points.Count;
        indexCount = faces.Count;

        vertices = new Vector3[vertexCount];
        normals = new Vector3[vertexCount];
        indices = new int[indexCount];

        for (int i = 0; i < vertexCount; ++i)
        {
            vertices[i] = new Vector3(points[i].x, points[i].y, points[i].z);
            //normals[i] = new Vector3(indexMesh.normals[i].x, indexMesh.normals[i].y, indexMesh.normals[i].z);
        }
        for (int i = 0; i < indexCount; ++i)
            indices[i] = faces[i];

        Mesh mesh2 = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh2;
        mesh2.vertices = vertices;
        mesh2.triangles = indices;
        mesh2.normals = normals;
    }
}
