using System.Collections.Generic;

namespace STLLoader
{
    public static class Utility
    {
        public static void CalcBoundingBox(Vec3[] vertices, ref Vec3 min, ref Vec3 max)
        {
            min = vertices[0];
            max = vertices[0];

            for (int i = 1; i < vertices.Length; ++i)
            {
                min.x = (vertices[i].x < min.x) ? vertices[i].x : min.x;
                min.y = (vertices[i].y < min.y) ? vertices[i].y : min.y;
                min.z = (vertices[i].z < min.z) ? vertices[i].z : min.z;

                max.x = (vertices[i].x > max.x) ? vertices[i].x : max.x;
                max.y = (vertices[i].y > max.y) ? vertices[i].y : max.y;
                max.z = (vertices[i].z > max.z) ? vertices[i].z : max.z;
            }
        }

        public static void CalcBoundingBox(List<Vec3> vertices, ref Vec3 min, ref Vec3 max)
        {
            min = vertices[0];
            max = vertices[0];

            for (int i = 1; i < vertices.Count; ++i)
            {
                min.x = (vertices[i].x < min.x) ? vertices[i].x : min.x;
                min.y = (vertices[i].y < min.y) ? vertices[i].y : min.y;
                min.z = (vertices[i].z < min.z) ? vertices[i].z : min.z;

                max.x = (vertices[i].x > max.x) ? vertices[i].x : max.x;
                max.y = (vertices[i].y > max.y) ? vertices[i].y : max.y;
                max.z = (vertices[i].z > max.z) ? vertices[i].z : max.z;
            }
        }
    }
}
