using System.Collections.Generic;
using System.IO;
using System;
using System.Windows.Forms;

namespace STLLoader
{
    public class Mesh
    {
        public List<Vec3> normals;
        public List<Vec3> vertices;
        public Vec3 minBound;
        public Vec3 maxBound;

        public Mesh(List<Vec3> normals, List<Vec3> vertices)
        {
            this.normals = normals;
            this.vertices = vertices;
        }
    }

    public class IndexMesh
    {
        public List<Vec3> vertices;
        public List<Vec3> normals;
        public List<int> indices;
        public Vec3 minBound;
        public Vec3 maxBound;

        public IndexMesh(List<Vec3> normals, List<Vec3> vertices, List<int> indices)
        {
            this.normals = normals;
            this.vertices = vertices;
            this.indices = indices;
        }
    }

    public struct Vec3
    {
        public float x;
        public float y;
        public float z;

        public Vec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vec3 operator +(Vec3 lhs, Vec3 rhs)
        {
            return new Vec3(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
        }

        public static Vec3 operator -(Vec3 lhs, Vec3 rhs)
        {
            return new Vec3(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);
        }

        public static Vec3 operator /(Vec3 lhs, Vec3 rhs)
        {
            return new Vec3(lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z);
        }

        public static Vec3 operator /(Vec3 lhs, float rhs)
        {
            return new Vec3(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs);
        }

        public static Vec3 operator *(Vec3 lhs, float rhs)
        {
            return new Vec3(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs);
        }


        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(Vec3))
                return false;
            Vec3 rhs = (Vec3)obj;

            return x == rhs.x && y == rhs.y && z == rhs.z;
        }

        public static bool operator ==(Vec3 lhs, Vec3 rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
        }

        public static bool operator !=(Vec3 lhs, Vec3 rhs)
        {
            return lhs.x != rhs.x || lhs.y != rhs.y || lhs.z != rhs.z;
        }
       
        public Vec3 outer(Vec3 rhs)
        {
            Vec3 result = new Vec3();
            result.x = x * rhs.x;
            result.y = y * rhs.y;
            result.z = z * rhs.z;
            return result;
        }

        public float dot(Vec3 rhs)
        {
            return x * rhs.x + y * rhs.y + z * rhs.z;
        }

        public bool compareGreater(float value, Vec3 direction)
        {
            return direction.x != 0f ? x > value : (direction.y != 0f) ? y > value : z > value;
        }
        public bool compareSmaller(float value, Vec3 direction)
        {
            return direction.x != 0f ? x < value : (direction.y != 0f) ? y < value : z < value;
        }
        public bool compareEqual(float value, Vec3 direction)
        {
            return direction.x != 0f ? value == x : (direction.y != 0f) ? value == y : value == z;
        }

        public Vec3 Cross(Vec3 rhs)
        {
            Vec3 result = new Vec3();

            result.x = y * rhs.z - z * rhs.y;
            result.y = z * rhs.x - x * rhs.z;
            result.z = x * rhs.y - y * rhs.x;

            return result;
        }

        public void Normalize()
        {
            double abs = AbsDouble();
            x = (float)((double)x / abs);
            y = (float)((double)y / abs);
            z = (float)((double)z / abs);
        }

        public float Abs()
        {
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }

        public double AbsDouble()
        {
            return Math.Sqrt(x * x + y * y + z * z);
        }

        public float Max()
        {
            float result = x;
            result = y > result ? y : result;
            return z > result ? z : result;
        }
    }

   
    public static class STLLoader
    {
        public static Mesh LoadMeshFileBrowser()
        {
            OpenFileDialog dLog = new OpenFileDialog();
            dLog.Filter = "All Files (*.stl)|*.stl";
            dLog.FilterIndex = 1;
            dLog.Multiselect = false;
            dLog.Title = "Load STL File";
            dLog.InitialDirectory = "C:\\";

            string fileName;
            if(dLog.ShowDialog() == DialogResult.OK)
            {
                fileName = dLog.FileName;
                return LoadSTLMesh(fileName);
            }
            else
            {
                return null;
            }
        }

        public static Mesh LoadSTLMesh(string fileName)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
            {
                char[] header = new char[80];
                reader.Read(header, 0, 80);
                string headerS = new string(header);

                if (!headerS.Contains("solid")) // Check if the file is a binary file.
                {
                    return CreateMeshFromBinaryFile(reader);
                }

            }

            using (TextReader reader = File.OpenText(fileName)) // The file is no binary file.
            {
                return CreateMeshFromASCIIFile(reader);
            }
        }

        public static List<IndexMesh> CreateIndexedMesh(Mesh mesh, uint bufferSize)
        {
            Vec3 min = mesh.minBound;
            Vec3 max = mesh.maxBound;
            List<IndexMesh> result = new List<IndexMesh>();

            uint resolution = (uint)Math.Ceiling(Math.Pow(150*bufferSize, 1f/3f)); 
            Vec3 spaceSize = (max - min) / (float)(resolution - 1);
            if (spaceSize.x == 0)
                spaceSize.x = 1;
            if (spaceSize.y == 0)
                spaceSize.y = 1;
            if (spaceSize.z == 0)
                spaceSize.z = 1;

            List<int>[, ,] indexSpace = new List<int>[resolution, resolution, resolution];

            List<Vec3> vertices = new List<Vec3>();
            List<Vec3> normals = new List<Vec3>();

            int size = mesh.vertices.Count;
            List<int> indices = new List<int>();

            Vec3 tmpVertex;
            Vec3 tmpNormal;
            Vec3 transVertex;
            bool vertexExists;
            int normalCounter = 0;
            List<int> foundList;
            int idx;
            uint threashold = bufferSize - 3;

            for (int i = 0; i < size; ++i)
            {
                vertexExists = false;
                tmpVertex = mesh.vertices[i];
                tmpNormal = mesh.normals[(i - normalCounter) / 3];
                transVertex = (tmpVertex - min) / spaceSize;
                foundList = indexSpace[(int)Math.Floor(transVertex.x), (int)Math.Floor(transVertex.y), (int)Math.Floor(transVertex.z)];
                if (foundList == null)
                {
                    foundList = new List<int>();
                    foundList.Add(vertices.Count);
                    indexSpace[(int)Math.Floor(transVertex.x), (int)Math.Floor(transVertex.y), (int)Math.Floor(transVertex.z)] = foundList;
                    indices.Add(vertices.Count);
                    vertices.Add(tmpVertex);
                    normals.Add(tmpNormal);
                }
                else
                {
                    for (int j = 0; j < foundList.Count; ++j)
                    {
                        idx = foundList[j];
                        if (vertices[idx] == tmpVertex)
                        {
                            indices.Add(idx);
                            normals[idx] += tmpNormal;
                            vertexExists = true;
                            break;
                        }
                    }
                    if (!vertexExists)
                    {
                        foundList.Add(vertices.Count);
                        indices.Add(vertices.Count);
                        vertices.Add(tmpVertex);
                        normals.Add(tmpNormal);
                    }
                }
                normalCounter = (normalCounter + 1) % 3;
                if(vertices.Count >= threashold && normalCounter == 0)
                {
                    for (int j = 0; j < normals.Count; ++j)
                        normals[j].Normalize();

                    result.Add(new IndexMesh(normals, vertices, indices));
                    indexSpace = new List<int>[resolution, resolution, resolution];

                    vertices = new List<Vec3>();
                    normals = new List<Vec3>();
                    indices = new List<int>();
                }
            }

            for (int j = 0; j < normals.Count; ++j)
                normals[j].Normalize();

            result.Add(new IndexMesh(normals, vertices, indices));
            indexSpace = new List<int>[resolution, resolution, resolution];

            vertices = new List<Vec3>();
            normals = new List<Vec3>();
            indices = new List<int>();
            return result;
        }

        private static Mesh CreateMeshFromASCIIFile(TextReader reader)
        {
            List<Vec3> normals = new List<Vec3>();
            List<Vec3> vertices = new List<Vec3>();
            Vec3 tmpVec = new Vec3();

            string text;
            string[] parts;
            int idx;
            while (true)
            {
                reader.ReadLine();
                text = reader.ReadLine();
                if (text.Contains("endsolid"))
                    break;
                idx = text.IndexOf("normal");
                text = text.Substring(idx);
                parts = text.Split(' ');
                tmpVec.x = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                tmpVec.y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                tmpVec.z = float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
                normals.Add(tmpVec);
                reader.ReadLine();

                text = reader.ReadLine();
                idx = text.IndexOf("vertex");
                text = text.Substring(idx);
                parts = text.Split(' ');
                tmpVec.x = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                tmpVec.y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                tmpVec.z = float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
                vertices.Add(tmpVec);

                text = reader.ReadLine();
                idx = text.IndexOf("vertex");
                text = text.Substring(idx);
                parts = text.Split(' ');
                tmpVec.x = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                tmpVec.y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                tmpVec.z = float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
                vertices.Add(tmpVec);

                text = reader.ReadLine();
                idx = text.IndexOf("vertex");
                text = text.Substring(idx);
                parts = text.Split(' ');
                tmpVec.x = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                tmpVec.y = float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                tmpVec.z = float.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture);
                vertices.Add(tmpVec);
                reader.ReadLine();
            }
            Mesh result = new Mesh(normals, vertices);
            Utility.CalcBoundingBox(result.vertices, ref result.minBound, ref result.maxBound);
            return result;
        }

        private static Mesh CreateMeshFromBinaryFile(BinaryReader reader)
        {
            uint numberFaces = reader.ReadUInt32();
            List<Vec3> normals = new List<Vec3>();
            List<Vec3> vertices = new List<Vec3>();
            Vec3 tmpVec = new Vec3();
            for (int i = 0; i < numberFaces; ++i)
            {
                tmpVec.x = reader.ReadSingle();
                tmpVec.y = reader.ReadSingle();
                tmpVec.z = reader.ReadSingle();
                normals.Add(tmpVec);

                tmpVec.x = reader.ReadSingle();
                tmpVec.y = reader.ReadSingle();
                tmpVec.z = reader.ReadSingle();
                vertices.Add(tmpVec);

                tmpVec.x = reader.ReadSingle();
                tmpVec.y = reader.ReadSingle();
                tmpVec.z = reader.ReadSingle();
                vertices.Add(tmpVec);

                tmpVec.x = reader.ReadSingle();
                tmpVec.y = reader.ReadSingle();
                tmpVec.z = reader.ReadSingle();
                vertices.Add(tmpVec);

                reader.ReadInt16();
            }
            Mesh result = new Mesh(normals, vertices);
            Utility.CalcBoundingBox(result.vertices, ref result.minBound, ref result.maxBound);
            return result;
        }

    }
}

     