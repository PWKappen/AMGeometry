 using UnityEngine;
 using System.Collections;

public class GridManager : MonoBehaviour
{

    //public GameObject plane;


    public int gridSizeX;
    public int gridSizeZ;

    public float scale;

    public float startX;
    public float startY;
    public float startZ;

    private Material lineMaterial;

    public Color mainColor = new Color(0f, 1f, 0f, 1f);

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

        GL.Begin(GL.LINES);
        GL.Color(mainColor);

        float xSizeReal = gridSizeX * scale;
        float zSizeReal = gridSizeZ * scale;
        //X axis lines
        for (float i = 0; i <= gridSizeZ; ++i)
        {
            GL.Vertex3(startX, startY, startZ + i *scale);
            GL.Vertex3(startX + xSizeReal, startY, startZ + i*scale);
        }

        //Z axis lines
        for (float i = 0; i <= gridSizeX; ++i)
        {
            GL.Vertex3(startX + i*scale, startY, startZ);
            GL.Vertex3(startX + i*scale, startY, startZ + zSizeReal);
        }


        GL.End();
    }

    void OnDrawGizmos()
    {
        DrawLines();
    }

    void OnPostRender()
    {
        DrawLines();
    }
}
