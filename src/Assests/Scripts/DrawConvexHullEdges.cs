using UnityEngine;
using System.Collections.Generic;

public class DrawConvexHullEdges : MonoBehaviour {
    private Material lineMaterial;

    public Color mainColor = new Color(0f, 1f, 0f, 1f);
    public List<List<int>> convexHullEdges;
    public Vector3[] vertices;
    public bool isActive = false;
    public Matrix4x4 trans;

    void CreateLineMaterial()
    {
        if (!lineMaterial)
        {
            // Unity has a built-in shader that is useful for drawing
            // simple colored things.
            var shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            // Turn on alpha blending
            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // Turn backface culling off
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            // Turn off depth writes
            lineMaterial.SetInt("_ZWrite", 0);
        }
    }

    void DrawLines()
    {
        CreateLineMaterial();
        // set the current material
        lineMaterial.SetPass(0);

        GL.PushMatrix();

        GL.Begin(GL.LINES);
        GL.Color(mainColor);
        GL.MultMatrix(trans);
        List<int> indices;
        for (int i = 0; i < convexHullEdges.Count; ++i)
        {
            indices = convexHullEdges[i];
            for (int j = 0; j < indices.Count - 1; ++j)
            {
                GL.Vertex3(vertices[indices[j]].x, vertices[indices[j]].y, vertices[indices[j]].z);
                GL.Vertex3(vertices[indices[j + 1]].x, vertices[indices[j + 1]].y, vertices[indices[j + 1]].z);
            }
            if (indices.Count > 1)
            {
                GL.Vertex3(vertices[indices[indices.Count - 1]].x, vertices[indices[indices.Count - 1]].y, vertices[indices[indices.Count - 1]].z);
                GL.Vertex3(vertices[indices[0]].x, vertices[indices[0]].y, vertices[indices[0]].z);
            }
        }
        GL.End();
        GL.PopMatrix();
    }

    void OnDrawGizmos()
    {
        if(isActive)
            DrawLines();
    }

    void OnPostRender()
    {
        if(isActive)
            DrawLines();
    }
}
