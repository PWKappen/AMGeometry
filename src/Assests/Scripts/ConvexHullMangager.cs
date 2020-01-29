using UnityEngine;
using System.Collections.Generic;

public class ConvexHullMangager : MonoBehaviour {

    public DrawConvexHullEdges convexHullEdges;

    public void ConfigureConvexHull(ref List<STLLoader.Vec3> localCorners, ref List<int> faces, float scale, Vector3 minOffset, ref List<List<int>> convexHullOutline)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[localCorners.Count];
        Vector3[] normals = new Vector3[localCorners.Count];
        int[] indices = new int[faces.Count];
        Vector3[] vertices2 = new Vector3[localCorners.Count];
        Vector3[] normals2 = new Vector3[localCorners.Count];
        int[] indices2 = new int[faces.Count];

        for (int i = 0; i < faces.Count; ++i)
        {
            indices[i] = faces[i];
            indices2[i] = faces[i];
        }
        for (int i = 0; i < localCorners.Count; ++i)
        {
            vertices[i] = new Vector3(localCorners[i].x, localCorners[i].y, localCorners[i].z);
            vertices2[i] = new Vector3(localCorners[i].x, localCorners[i].y, localCorners[i].z);
        }
        for (int i = 0; i < normals.Length; ++i)
            normals[i] = new Vector3(0,0,0);
        for (int i = 0; i < faces.Count; i+=3)
        {
            Vector3 normal = Vector3.Cross(vertices[faces[i + 1]] - vertices[faces[i]], vertices[faces[i + 2]] - vertices[faces[i]]).normalized;
            normals[faces[i]] += normal;
            normals[faces[i+1]] += normal;
            normals[faces[i+2]] += normal;
        }
        for (int i = 0; i < normals.Length; ++i)
        {
            normals[i] = normals[i].normalized;
            normals2[i] = normals[i];
        }
       
        
        for (int i = 0; i < vertices.Length; ++i)
            vertices[i] += normals[i] * 2 *( scale > 1 ? scale : 1);

        Mesh mesh2 = new Mesh();
        mesh2.vertices = vertices2;
        mesh2.triangles = indices2;
        mesh2.normals = normals2;

        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.normals = normals;

        MeshFilter filter = GetComponent<MeshFilter>();
        filter.mesh = mesh;
        MeshCollider collider = GetComponent<MeshCollider>();
        collider.sharedMesh = mesh2;

        transform.position = minOffset;
        transform.localScale = new Vector3(scale, scale, scale);

        convexHullEdges.convexHullEdges = convexHullOutline;
        convexHullEdges.trans = transform.localToWorldMatrix;
        convexHullEdges.vertices = vertices;
    }

    public bool FindFaceOnConvexHull(ref Vector3 p1, ref Vector3 p2, ref Vector3 p3)
    {
        RaycastHit hit;
        if(!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
            return false;

        MeshCollider meshCollider = hit.collider as MeshCollider;
        if (meshCollider == null || meshCollider.sharedMesh == null)
            return false;

        Mesh mesh = meshCollider.sharedMesh;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        p1 = vertices[triangles[hit.triangleIndex * 3 + 0]];
        p2 = vertices[triangles[hit.triangleIndex * 3 + 1]];
        p3 = vertices[triangles[hit.triangleIndex * 3 + 2]];
        return true;
    }
}
