using UnityEngine;
using System.Collections.Generic;

public class DrawPath : MonoBehaviour {

    private Material lineMaterial;

    public Color mainColor = new Color(0f, 1f, 0f, 1f);
    public List<STLLoader.Vec3> path;
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
        STLLoader.Vec3 p1;
        STLLoader.Vec3 p2;
        for (int i = 0; i < path.Count-1; ++i)
        {
            p1 = path[i];
            p2 = path[i+1];
            GL.Vertex3(p1.x, p1.y, p1.z);
            GL.Vertex3(p2.x, p2.y, p2.z);
        }
        GL.End();
        GL.PopMatrix();
    }

    void OnDrawGizmos()
    {
        if (isActive)
            DrawLines();
    }

    void OnPostRender()
    {
        if (isActive)
            DrawLines();
    }
}
