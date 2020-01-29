using System.Collections.Generic;
using System;
using System.IO;


// Contains function  CreateLayer(ref List<Vec3> startPlane) to calculate the layers in the voxel grid called grid.
// Grid is a private class member of Voxelizer. Grid is a 3D-Array containing Voxel structs. The Argument to CreateLayer 
// is a list of Vec3 structs. The Vec3 objects contain the indices of the voxels belonging to the start plane which can 
// be used to calculate the layering. 

namespace STLLoader
{
    public struct Voxel
    {
        public bool set;
        public int layer;
        public bool visible;
    }

    public class Voxelizer
    {
        private Voxel[, ,] grid;
        private uint xResolution;
        private uint yResolution;
        private uint zResolution;
        private Vec3 direction;
        private Vec3 planeCoord1;
        private Vec3 planeCoord2;
        private float sizeVoxel;
        private float maxBoundingSize;
        private Vec3 minOffset;

        private Vec3 centerOfMass;

        public Voxelizer(uint xResolution, uint yResolution, uint zResolution)
        {
            grid = new Voxel[xResolution, yResolution, zResolution];
            this.xResolution = xResolution;
            this.yResolution = yResolution;
            this.zResolution = zResolution;
        }

        public Voxelizer(ref bool[,,] bitMap)
        {
            xResolution = (uint)bitMap.GetLength(0);
            yResolution = (uint)bitMap.GetLength(1);
            zResolution = (uint)bitMap.GetLength(2);

            grid = new Voxel[xResolution, yResolution, zResolution];
            Voxel tmpVoxel;
            for(int i = 0; i < xResolution; ++i)
            {
                for(int j = 0; j < yResolution; ++j)
                {
                    for(int k = 0; k < zResolution; ++k)
                    {
                        tmpVoxel.set = bitMap[i, j, k];
                        tmpVoxel.layer = 0;
                        tmpVoxel.visible = true;
                        grid[i, j, k] = tmpVoxel;
                    }
                }
            }
        }

        public float GetSizeVoxel()
        {
            return sizeVoxel;
        }

        public Vec3 GetMinOffset()
        {
            return minOffset;
        }

        public void GetVoxel(int x, int y , int z)
        {

        }

        public Voxel[, ,] MultiyDirectionsVoxelize(Mesh mesh, Vec3[] direction)
        {
            for (int dir = 0; dir < direction.Length; ++dir)
            {
                Voxelize(mesh, direction[dir]);
                for (uint i = 0; i < xResolution; ++i)
                {
                    for (uint j = 0; j < yResolution; ++j)
                    {
                        for (uint k = 0; k < zResolution; ++k)
                        {
                            grid[i, j, k].layer += grid[i, j, k].set ? 1 : -1;
                        }
                    }
                }
            }

            for (uint i = 0; i < xResolution; ++i)
            {
                for (uint j = 0; j < yResolution; ++j)
                {
                    for (uint k = 0; k < zResolution; ++k)
                    {
                        grid[i, j, k].set = grid[i, j, k].layer > 0;
                        grid[i, j, k].layer = 0;
                        grid[i, j, k].visible = true;
                    }
                }
            }

            CalculateCenterOfMass();

            return grid;
        }

        uint FindMaxResolution()
        {
            return xResolution > yResolution ? (xResolution > zResolution ? xResolution : zResolution) : (zResolution > yResolution ? zResolution : yResolution);
        }

        public Voxel[, ,] Voxelize(Mesh mesh, Vec3 direction)
        {
            planeCoord1 = new Vec3(direction.z, direction.x, direction.y);
            planeCoord2 = new Vec3(direction.y, direction.z, direction.x);
            this.direction = direction;
            Vec3 boundingSize = mesh.maxBound - mesh.minBound;
            maxBoundingSize = boundingSize.Max();
            sizeVoxel = (maxBoundingSize + 0.00001f) / (FindMaxResolution() - 1);
            minOffset = mesh.minBound;

            for (uint i = 0; i < xResolution; ++i)
            {
                for (uint j = 0; j < yResolution; ++j)
                {
                    for (uint k = 0; k < zResolution; ++k)
                    {
                        grid[i, j, k].set = false;
                    }
                }
            }
            int size = mesh.vertices.Count;
            Vec3[] face = new Vec3[3];



            for (int i = 0; i < size; i += 3)
            {
                face[0] = mesh.vertices[i];
                face[1] = mesh.vertices[i + 1];
                face[2] = mesh.vertices[i + 2];
                FindOverlappedIndices(face, mesh.normals[i / 3]);
            }

            CalculateInnerVoxels();

            return grid;
        }

        private void FindOverlappedIndices(Vec3[] face, Vec3 normal)
        {
            Vec3[] newCoord = new Vec3[3];
            uint i;
            Vec3[] projectedCoord = new Vec3[3];
            for (i = 0; i < 3; ++i)
            {
                newCoord[i] = new Vec3();
                newCoord[i].x = (face[i].x - minOffset.x) / sizeVoxel;
                newCoord[i].y = (face[i].y - minOffset.y) / sizeVoxel;
                newCoord[i].z = (face[i].z - minOffset.z) / sizeVoxel;

                projectedCoord[i] = newCoord[i].outer(planeCoord1) + newCoord[i].outer(planeCoord2);
            }
            float normalSign = normal.dot(direction);
            Vec3 min = new Vec3(), max = new Vec3();
            Utility.CalcBoundingBox(projectedCoord, ref min, ref max);

            Vec3 edge1 = newCoord[1] - newCoord[0];
            Vec3 edge2 = newCoord[2] - newCoord[0];
            Vec3 h = direction.Cross(edge2);
            float f = 1f / edge1.dot(h);
            Vec3[] normals = new Vec3[3];
            Vec3[] normals3d = new Vec3[3];

            float t = 0;
            float u = 0;
            float v = 0;

            for (i = 0; i < 3; ++i)
            {
                if (normalSign >= 0)
                    normals[i] = planeCoord1 * (projectedCoord[i].dot(planeCoord2) - projectedCoord[(i + 1) % 3].dot(planeCoord2)) + planeCoord2 * (projectedCoord[(i + 1) % 3].dot(planeCoord1) - projectedCoord[i].dot(planeCoord1));
                else
                    normals[i] = planeCoord1 * (projectedCoord[(i + 1) % 3].dot(planeCoord2) - projectedCoord[i].dot(planeCoord2)) + planeCoord2 * (projectedCoord[i].dot(planeCoord1) - projectedCoord[(i + 1) % 3].dot(planeCoord1));
                normals[i].Normalize();
            }

            min.x = (float)Math.Floor(min.x);
            min.y = (float)Math.Floor(min.y);
            min.z = (float)Math.Floor(min.z);
            max.x = (float)Math.Ceiling(max.x);
            max.y = (float)Math.Ceiling(max.y);
            max.z = (float)Math.Ceiling(max.z);


            Vec3 min1 = new Vec3();
            Vec3 min2 = new Vec3();
            Vec3 max1 = new Vec3();
            Vec3 max2 = new Vec3();


            min1.x = min.x * planeCoord1.x;
            min1.y = min.y * planeCoord1.y;
            min1.z = min.z * planeCoord1.z;
            min2.x = min.x * planeCoord2.x;
            min2.y = min.y * planeCoord2.y;
            min2.z = min.z * planeCoord2.z;

            max1.x = max.x * planeCoord1.x;
            max1.y = max.y * planeCoord1.y;
            max1.z = max.z * planeCoord1.z;
            max2.x = max.x * planeCoord2.x;
            max2.y = max.y * planeCoord2.y;
            max2.z = max.z * planeCoord2.z;
            Vec3 tmp;
            Vec3 pos;
            bool[] leftEdge = new bool[3];
            for (i = 0; i < 3; ++i)
            {
                leftEdge[i] = normals[i].compareGreater(0, planeCoord1);
                leftEdge[i] |= normals[i].compareEqual(0, planeCoord1) && normals[i].compareSmaller(0, planeCoord2);
            }

            float epsilon = 0.00000001f;
            bool edge = true;
            for (Vec3 dir1 = min1; dir1 != max1; dir1 += planeCoord1)
            {
                for (Vec3 dir2 = min2; dir2 != max2; dir2 += planeCoord2)
                {
                    edge = true;
                    tmp = dir1 + dir2 + planeCoord1 * 0.5f + planeCoord2 * 0.5f;
                    if (CheckIntersection(tmp, direction, edge1, edge2, newCoord[0], h, f, ref t, ref u, ref v))
                    {
                        if (u < (0f + epsilon))
                            edge = edge && leftEdge[2];
                        if (v < (0f + epsilon))
                            edge = edge && leftEdge[0];
                        if ((u + v - 1f) < (0f + epsilon) && (u + v - 1f) > (0f - epsilon))
                            edge = edge && leftEdge[1];
                        if (edge)
                        {
                            pos = tmp + direction * t;
                            grid[(int)Math.Floor(pos.x), (int)Math.Floor(pos.y), (int)Math.Floor(pos.z)].set ^= true;
                        }

                    }
                }
            }

        }

        private bool CheckIntersection(Vec3 p, Vec3 direction, Vec3 edge1, Vec3 edge2, Vec3 v0, Vec3 h, float f, ref float t, ref float u, ref float v)
        {
            Vec3 s = p - v0;
            u = f * s.dot(h);
            if (u < 0f || u > 1f)
                return false;

            Vec3 q = s.Cross(edge1);
            v = f * direction.dot(q);
            if (v < 0f || v > 1f)
                return false;
            if (u + v > 1f)
                return false;
            t = f * edge2.dot(q);
            return true;
        }

        private void CalculateInnerVoxels()
        {
            int xStart = direction.x == 1 ? (int)xResolution - 2 : (int)xResolution - 1;
            int yStart = direction.y == 1 ? (int)yResolution - 2 : (int)yResolution - 1;
            int zStart = direction.z == 1 ? (int)zResolution - 2 : (int)zResolution - 1;

            for (int x = xStart; x >= 0; --x)
            {
                for (int y = yStart; y >= 0; --y)
                {
                    for (int z = zStart; z >= 0; --z)
                    {
                        grid[x, y, z].set ^= grid[(int)direction.x + x, (int)direction.y + y, (int)direction.z + z].set;
                    }
                }
            }
        }

        public Mesh GenerateMesh(uint layerMin, uint layerMax)
        {
            List<Vec3> vertices = new List<Vec3>();
            List<Vec3> normals = new List<Vec3>();

            bool difference = false;
            bool last = false;
            bool inRange = false;
            for (uint i = 0; i < xResolution; ++i)
            {
                for (uint j = 0; j < yResolution; ++j)
                {
                    for (uint k = 0; k < zResolution; ++k)
                    {
                        inRange = (grid[i, j, k].layer <= layerMax && grid[i, j, k].layer >= layerMin) && grid[i, j, k].set;
                        difference = last ^ inRange;
                        last = grid[i, j, k].set && inRange;
                        if (difference)
                        {
                            if (last)
                                AddZUpFace(ref vertices, ref normals, i, j, k);
                            else
                                AddZDownFace(ref vertices, ref normals, i, j, k - 1);
                        }
                        if (inRange)
                        {

                            if ((int)j + 1 >= yResolution || (!grid[i, j + 1, k].set || !(grid[i, j + 1, k].layer <= layerMax && grid[i, j + 1, k].layer >= layerMin)))
                            {
                                AddYDownFace(ref vertices, ref normals, i, j, k);
                            }
                            if ((int)j - 1 < 0 || !grid[i, j - 1, k].set || !(grid[i, j - 1, k].layer <= layerMax && grid[i, j - 1, k].layer >= layerMin))
                            {
                                AddYUpFace(ref vertices, ref normals, i, j, k);
                            }
                            if ((int)i + 1 >= xResolution || (!grid[i + 1, j, k].set || !(grid[i + 1, j, k].layer <= layerMax && grid[i + 1, j, k].layer >= layerMin)))
                            {
                                AddXDownFace(ref vertices, ref normals, i, j, k);
                            }
                            if ((int)i - 1 < 0 || !grid[i - 1, j, k].set || !(grid[i - 1, j, k].layer <= layerMax && grid[i - 1, j, k].layer >= layerMin))
                            {
                                AddXUpFace(ref vertices, ref normals, i, j, k);
                            }
                        }
                    }
                }
            }
            Mesh mesh = new Mesh(normals, vertices);
            Utility.CalcBoundingBox(vertices, ref mesh.minBound, ref mesh.maxBound);
            return mesh;
        }
        public Mesh GenerateMeshVisible()
        {
            List<Vec3> vertices = new List<Vec3>();
            List<Vec3> normals = new List<Vec3>();

            bool difference = false;
            bool last = false;
            bool inRange = false;
            for (uint i = 0; i < xResolution; ++i)
            {
                for (uint j = 0; j < yResolution; ++j)
                {
                    for (uint k = 0; k < zResolution; ++k)
                    {
                        inRange = grid[i,j,k].visible && grid[i, j, k].set;
                        difference = last ^ inRange;
                        last = grid[i, j, k].set && inRange;
                        if (difference)
                        {
                            if (last)
                                AddZUpFace(ref vertices, ref normals, i, j, k);
                            else
                                AddZDownFace(ref vertices, ref normals, i, j, k - 1);
                        }
                        if (inRange)
                        {

                            if ((int)j + 1 >= yResolution || (!grid[i, j + 1, k].set || !grid[i, j + 1, k].visible))
                            {
                                AddYDownFace(ref vertices, ref normals, i, j, k);
                            }
                            if ((int)j - 1 < 0 || !grid[i, j - 1, k].set || !grid[i,j-1,k].visible)
                            {
                                AddYUpFace(ref vertices, ref normals, i, j, k);
                            }
                            if ((int)i + 1 >= xResolution || (!grid[i + 1, j, k].set || !grid[i + 1, j, k].visible))
                            {
                                AddXDownFace(ref vertices, ref normals, i, j, k);
                            }
                            if ((int)i - 1 < 0 || !grid[i - 1, j, k].set || !grid[i-1, j, k].visible)
                            {
                                AddXUpFace(ref vertices, ref normals, i, j, k);
                            }
                        }
                    }
                }
            }
            Mesh mesh = new Mesh(normals, vertices);
            Utility.CalcBoundingBox(vertices, ref mesh.minBound, ref mesh.maxBound);
            return mesh;
        }

        public Mesh[] GenerateMeshLayerWise(int layerMin, int layerMax)
        {
            int numLayer = layerMax - layerMin;
            List<Vec3>[] vertices = new List<Vec3>[numLayer];
            List<Vec3>[] normals = new List<Vec3>[numLayer];

            for (int i = 0; i < numLayer; ++i)
            {
                vertices[i] = new List<Vec3>();
                normals[i] = new List<Vec3>();
            }

            int layerValue;
            bool inRange = false;
            for (uint i = 0; i < xResolution; ++i)
            {
                for (uint j = 0; j < yResolution; ++j)
                {
                    for (uint k = 0; k < zResolution; ++k)
                    {
                        layerValue = grid[i, j, k].layer - (int)layerMin;
                        inRange = (grid[i, j, k].layer < layerMax && grid[i, j, k].layer >= layerMin) && grid[i, j, k].set;
  
                        if (inRange)
                        {
                            if ((int)k + 1 >= zResolution || (!grid[i, j, k + 1].set || !(grid[i, j, k + 1].layer < layerMax && grid[i, j, k + 1].layer >= layerMin)))
                            {
                                AddZDownFace(ref vertices[layerValue], ref normals[layerValue], i, j, k);
                            }
                            if ((int)k - 1 < 0 || (!grid[i, j, k - 1].set || !(grid[i, j, k - 1].layer < layerMax && grid[i, j, k - 1].layer >= layerMin)))
                            {
                                AddZUpFace(ref vertices[layerValue], ref normals[layerValue], i, j, k);
                            }
                            if ((int)j + 1 >=yResolution || (!grid[i, j + 1, k].set || !(grid[i, j + 1, k].layer < layerMax && grid[i, j + 1, k].layer >= layerMin)))
                            {
                                AddYDownFace(ref vertices[layerValue], ref normals[layerValue], i, j, k);
                            }
                            if ((int)j - 1 < 0 || !grid[i, j - 1, k].set || !(grid[i, j - 1, k].layer < layerMax && grid[i, j - 1, k].layer >= layerMin))
                            {
                                AddYUpFace(ref vertices[layerValue], ref normals[layerValue], i, j, k);
                            }
                            if ((int)i + 1 >= xResolution || (!grid[i + 1, j, k].set || !(grid[i + 1, j, k].layer < layerMax && grid[i + 1, j, k].layer >= layerMin)))
                            {
                                AddXDownFace(ref vertices[layerValue], ref normals[layerValue], i, j, k);
                            }
                            if ((int)i - 1 < 0 || !grid[i - 1, j, k].set || !(grid[i - 1, j, k].layer < layerMax && grid[i - 1, j, k].layer >= layerMin))
                            {
                                AddXUpFace(ref vertices[layerValue], ref normals[layerValue], i, j, k);
                            }
                        }
                    }
                }
            }
            Mesh[] result = new Mesh[numLayer];
            for (int i = 0; i < numLayer; ++i)
            {
                if (vertices[i].Count > 0)
                {
                    result[i] = new Mesh(normals[i], vertices[i]);
                    Utility.CalcBoundingBox(vertices[i], ref result[i].minBound, ref result[i].maxBound);
                }
            }
            return result;
        }

        public Mesh[] GenerateMeshLayerWiseVisible(int layerMin, int layerMax)
        {
            int numLayer = layerMax - layerMin;
            List<Vec3>[] vertices = new List<Vec3>[numLayer];
            List<Vec3>[] normals = new List<Vec3>[numLayer];

            for (int i = 0; i < numLayer; ++i)
            {
                vertices[i] = new List<Vec3>();
                normals[i] = new List<Vec3>();
            }

            int layerValue;
            bool inRange = false;
            for (uint i = 0; i < xResolution; ++i)
            {
                for (uint j = 0; j < yResolution; ++j)
                {
                    for (uint k = 0; k < zResolution; ++k)
                    {
                        layerValue = grid[i, j, k].layer - (int)layerMin;
                        inRange = (grid[i, j, k].layer < layerMax && grid[i, j, k].layer >= layerMin) && grid[i, j, k].set && grid[i, j, k].visible;

                        if (inRange)
                        {
                            if ((int)k + 1 >= zResolution || (!grid[i, j, k + 1].set || !(grid[i, j, k + 1].layer < layerMax && grid[i, j, k + 1].layer >= layerMin)) || !grid[i, j, k + 1].visible)
                            {
                                AddZDownFace(ref vertices[layerValue], ref normals[layerValue], i, j, k);
                            }
                            if ((int)k - 1 < 0 || (!grid[i, j, k - 1].set || !(grid[i, j, k - 1].layer < layerMax && grid[i, j, k - 1].layer >= layerMin)) || !grid[i, j, k - 1].visible)
                            {
                                AddZUpFace(ref vertices[layerValue], ref normals[layerValue], i, j, k);
                            }
                            if ((int)j + 1 >= yResolution || (!grid[i, j + 1, k].set || !(grid[i, j + 1, k].layer < layerMax && grid[i, j + 1, k].layer >= layerMin)) || !grid[i, j + 1, k].visible)
                            {
                                AddYDownFace(ref vertices[layerValue], ref normals[layerValue], i, j, k);
                            }
                            if ((int)j - 1 < 0 || !grid[i, j - 1, k].set || !(grid[i, j - 1, k].layer < layerMax && grid[i, j - 1, k].layer >= layerMin) || !grid[i, j - 1, k].visible)
                            {
                                AddYUpFace(ref vertices[layerValue], ref normals[layerValue], i, j, k);
                            }
                            if ((int)i + 1 >= xResolution || (!grid[i + 1, j, k].set || !(grid[i + 1, j, k].layer < layerMax && grid[i + 1, j, k].layer >= layerMin)) || !grid[i + 1, j, k].visible)
                            {
                                AddXDownFace(ref vertices[layerValue], ref normals[layerValue], i, j, k);
                            }
                            if ((int)i - 1 < 0 || !grid[i - 1, j, k].set || !(grid[i - 1, j, k].layer < layerMax && grid[i - 1, j, k].layer >= layerMin) || !grid[i - 1, j, k].visible)
                            {
                                AddXUpFace(ref vertices[layerValue], ref normals[layerValue], i, j, k);
                            }
                        }
                    }
                }
            }

            Mesh[] result = new Mesh[numLayer];
            for (int i = 0; i < numLayer; ++i)
            {
                if (vertices[i].Count > 0)
                {
                    result[i] = new Mesh(normals[i], vertices[i]);
                    Utility.CalcBoundingBox(vertices[i], ref result[i].minBound, ref result[i].maxBound);
                }
            }
            return result;
        }

        public void GenerateMeshPlane(ref List<Vec3> voxelPlane, Vec3 normal, Vec3 p, bool visible)
        {

            int size = voxelPlane.Count;
            int i;
            uint resolution = xResolution;

            Vec3 mainDirection = new Vec3(1, 0, 0);
            if (Math.Abs(normal.x) < Math.Abs(normal.y))
                if (Math.Abs(normal.z) < Math.Abs(normal.y))
                {
                    resolution = yResolution;
                    mainDirection = new Vec3(0, 1, 0);
                }
                else
                {
                    resolution = zResolution;
                    mainDirection = new Vec3(0, 0, 1);
                }
            else if (Math.Abs(normal.x) < Math.Abs(normal.z))
            {
                resolution = zResolution;
                mainDirection = new Vec3(0, 0, 1);
            }

            int sign = mainDirection.dot(centerOfMass - p) > 0 ? -1 : 1;
            Vec3 target;
            Vec3 directionOfchange;
            if (sign > 0)
                directionOfchange = mainDirection;
            else
                directionOfchange = mainDirection * -1f;
            Vec3 index;

            for (i = 0; i < size; ++i)
            {
                target = voxelPlane[i];
                target -= mainDirection * target.dot(mainDirection);
                target += sign > 0 ? mainDirection * resolution : mainDirection * -1f;
                for (index = voxelPlane[i]; index != target; index += directionOfchange)
                {
                    grid[(int)index.x, (int)index.y, (int)index.z].visible = visible;
                }
            }
        }

        private void AddZUpFace(ref List<Vec3> vertices, ref List<Vec3> normals, uint x, uint y, uint z)
        {
            float xP = x;
            float xPP = (x + 1);
            float yP = y;
            float yPP = (y + 1);
            float zP = z;
            vertices.Add(new Vec3(xP, yP, zP));
            vertices.Add(new Vec3(xP, yPP, zP));
            vertices.Add(new Vec3(xPP, yPP, zP));
            vertices.Add(new Vec3(xP, yP, zP));
            vertices.Add(new Vec3(xPP, yPP, zP));
            vertices.Add(new Vec3(xPP, yP, zP));
            normals.Add(new Vec3(0, 0, -1));
            normals.Add(new Vec3(0, 0, -1));
        }

        private void AddZDownFace(ref List<Vec3> vertices, ref List<Vec3> normals, uint x, uint y, uint z)
        {
            float xP = x;
            float xPP = (x + 1);
            float yP = y;
            float yPP = (y + 1);
            float zPP = (z + 1);
            vertices.Add(new Vec3(xP, yP, zPP));
            vertices.Add(new Vec3(xPP, yPP, zPP));
            vertices.Add(new Vec3(xP, yPP, zPP));
            vertices.Add(new Vec3(xP, yP, zPP));
            vertices.Add(new Vec3(xPP, yP, zPP));
            vertices.Add(new Vec3(xPP, yPP, zPP));
            normals.Add(new Vec3(0, 0, 1));
            normals.Add(new Vec3(0, 0, 1));
        }

        private void AddYUpFace(ref List<Vec3> vertices, ref List<Vec3> normals, uint x, uint y, uint z)
        {
            float xP = x;
            float xPP = (x + 1);
            float yP = y;
            float zP = z;
            float zPP = (z + 1);
            vertices.Add(new Vec3(xP, yP, zP));
            vertices.Add(new Vec3(xPP, yP, zP));
            vertices.Add(new Vec3(xPP, yP, zPP));
            vertices.Add(new Vec3(xP, yP, zP));
            vertices.Add(new Vec3(xPP, yP, zPP));
            vertices.Add(new Vec3(xP, yP, zPP));
            normals.Add(new Vec3(0, -1, 0));
            normals.Add(new Vec3(0, -1, 0));
        }

        private void AddYDownFace(ref List<Vec3> vertices, ref List<Vec3> normals, uint x, uint y, uint z)
        {
            float xP = x;
            float xPP = (x + 1);
            float yPP = (y + 1);
            float zP = z;
            float zPP = (z + 1);
            vertices.Add(new Vec3(xP, yPP, zP));
            vertices.Add(new Vec3(xPP, yPP, zPP));
            vertices.Add(new Vec3(xPP, yPP, zP));
            vertices.Add(new Vec3(xP, yPP, zP));
            vertices.Add(new Vec3(xP, yPP, zPP));
            vertices.Add(new Vec3(xPP, yPP, zPP));
            normals.Add(new Vec3(0, 1, 0));
            normals.Add(new Vec3(0, 1, 0));
        }

        private void AddXUpFace(ref List<Vec3> vertices, ref List<Vec3> normals, uint x, uint y, uint z)
        {
            float xP = x;
            float yP = y;
            float yPP = (y + 1);
            float zP = z;
            float zPP = (z + 1);
            vertices.Add(new Vec3(xP, yP, zP));
            vertices.Add(new Vec3(xP, yP, zPP));
            vertices.Add(new Vec3(xP, yPP, zPP));
            vertices.Add(new Vec3(xP, yP, zP));
            vertices.Add(new Vec3(xP, yPP, zPP));
            vertices.Add(new Vec3(xP, yPP, zP));
            normals.Add(new Vec3(-1, 0, 0));
            normals.Add(new Vec3(-1, 0, 0));
        }

        private void AddXDownFace(ref List<Vec3> vertices, ref List<Vec3> normals, uint x, uint y, uint z)
        {
            float xPP = (x + 1);
            float yP = y;
            float yPP = (y + 1);
            float zP = z;
            float zPP = (z + 1);
            vertices.Add(new Vec3(xPP, yP, zP));
            vertices.Add(new Vec3(xPP, yPP, zPP));
            vertices.Add(new Vec3(xPP, yP, zPP));
            vertices.Add(new Vec3(xPP, yP, zP));
            vertices.Add(new Vec3(xPP, yPP, zP));
            vertices.Add(new Vec3(xPP, yPP, zPP));
            normals.Add(new Vec3(1, 0, 0));
            normals.Add(new Vec3(1, 0, 0));
        }

        public float GetVoxelSize()
        {
            return sizeVoxel;
        }

        public void CreateLayerDirection(bool x, bool y, bool z)
        {
            for (uint i = 0; i < xResolution; ++i)
            {
                for (uint j = 0; j < yResolution; ++j)
                {
                    for (uint k = 0; k < zResolution; ++k)
                    {
                        grid[i, j, k].layer = (int)(x ? i : (y ? j : k));
                    }
                }
            }
        }

        public List<Vec3> CalculateLocalCorners()
        {
            List<Vec3> result = new List<Vec3>();

            for(int i = 0; i < xResolution; ++i)
            {
                for(int j = 0; j < yResolution; ++j)
                {
                    for(int k = 0; k < zResolution; ++k)
                    {
                        if(grid[i,j,k].set)
                            if (CheckLocalEdge(i, j, k))
                                result.Add(new Vec3(i, j, k));
                    }
                }
            }

            return result;
        }

        private bool CheckLocalEdge(int x, int y, int z)
        {
            bool result = true;

            int x2, y2, z2;
            int i;

            for (i = 1; i < 8; ++i)
            {
                x2 = i % 2;
                y2 = (i / 2) % 2;
                z2 = i > 3 ? 1 : 0;
                
                if(CheckBounds(x + x2, y + y2, z + z2) && CheckBounds(x - x2, y - y2, z - z2))
                    result &= !(grid[x + x2, y + y2, z + z2].set && grid[x - x2, y - y2, z - z2].set);
                if (!result)
                    return false;
            }

            for (i = 0; i < 6; ++i)
            {
                x2 = i < 2 ? -1 : i < 4 ?  i % 2 :  1;
                y2 = i < 2 ?  1 : i < 4 ? -1 : i %  2;
                z2 = i < 2 ?  i % 2 : i <  4 ? 1 : -1;
                if (CheckBounds(x + x2, y + y2, z + z2) && CheckBounds(x - x2, y - y2, z - z2))
                    result &= !(grid[x + x2, y + y2, z + z2].set && grid[x - x2, y - y2, z - z2].set);
                if (!result)
                    return false;
            }

                return result;
        }

        public void RemoveVoxelPlane(ref List<Vec3> voxelPlane, Vec3 p1, Vec3 p2, Vec3 p3)
        {
            int size = voxelPlane.Count;
            int i;
            List<int> tmpList = new List<int>();

            for (i = 0; i < size; ++i)
            {
                tmpList.Add(grid[(int)voxelPlane[i].x, (int)voxelPlane[i].y, (int)voxelPlane[i].z].layer);
                grid[(int)voxelPlane[i].x, (int)voxelPlane[i].y, (int)voxelPlane[i].z].layer = -2;
            }


            Vec3 normal = (p2 - p1).Cross((p3 - p1));
            uint resolution = xResolution;

            Vec3 mainDirection = new Vec3(1, 0, 0);
            if (Math.Abs(normal.x) < Math.Abs(normal.y))
                if (Math.Abs(normal.z) < Math.Abs(normal.y))
                {
                    resolution = yResolution;
                    mainDirection = new Vec3(0, 1, 0);
                }
                else
                {
                    resolution = zResolution;
                    mainDirection = new Vec3(0, 0, 1);
                }
            else if (Math.Abs(normal.x) < Math.Abs(normal.z))
            {
                resolution = zResolution;
                mainDirection = new Vec3(0, 0, 1);
            }


            int sign = mainDirection.dot(centerOfMass - p1) > 0? -1 : 1;
            Vec3 target;
            Vec3 directionOfchange;
            if (sign > 0)
                directionOfchange = mainDirection;
            else
                directionOfchange = mainDirection * -1f;
            Vec3 index;

            for(i = 0; i < size; ++i)
            {
                target = voxelPlane[i];
                target -= mainDirection * target.dot(mainDirection);
                target += sign > 0 ? mainDirection * resolution : mainDirection * -1f;
                for(index = voxelPlane[i]; index != target; index += directionOfchange)
                {
                    Voxel voxel = grid[(int)index.x, (int)index.y, (int)index.z];
                    if(voxel.set && voxel.layer != -2)
                    {
                        voxel.set = false;
                        voxel.layer = 0;
                        grid[(int)index.x, (int)index.y, (int)index.z] = voxel;
                    }
                }
            }

            for (i = 0; i < size; ++i)
                grid[(int)voxelPlane[i].x, (int)voxelPlane[i].y, (int)voxelPlane[i].z].layer = tmpList[i];
            
        }

        public Vec3 PickVoxel(Vec3 pos, Vec3 direction)
        {
            Vec3 voxelPos = (pos - minOffset) / sizeVoxel;
            Vec3 voxelDirection = direction;

            uint resolution = xResolution;
            Vec3 mainDirection = new Vec3(voxelDirection.x, 0, 0);
            if (Math.Abs(voxelDirection.x) < Math.Abs(voxelDirection.y))
                if (Math.Abs(voxelDirection.z) < Math.Abs(voxelDirection.y))
                {
                    resolution = yResolution;
                    mainDirection = new Vec3(0, voxelDirection.y, 0);
                }
                else
                {
                    resolution = zResolution;
                    mainDirection = new Vec3(0, 0, voxelDirection.z);
                }
            else if (Math.Abs(voxelDirection.x) < Math.Abs(voxelDirection.z))
            {
                resolution = zResolution;
                mainDirection = new Vec3(0, 0, voxelDirection.z);
            }

            mainDirection.Normalize();

            Vec3 v0 = mainDirection.dot(new Vec3(1, 1, 1)) >= 0 ? new Vec3(0, 0, 0) : mainDirection * -resolution;
            float r;
            Vec3 point2;
            Vec3 point1;
            for (int i = 0; i <= resolution; ++i)
            {
                r = mainDirection.dot(v0 - voxelPos) / mainDirection.dot(voxelDirection);
                v0 += mainDirection;
                point2 = voxelPos + voxelDirection * r;
                point2.x = (float)Math.Floor(point2.x);
                point2.y = (float)Math.Floor(point2.y);
                point2.z = (float)Math.Floor(point2.z);

                point1 = point2 - mainDirection;
                if (CheckBounds(point1))
                {
                    if (grid[Convert.ToInt32(point1.x), Convert.ToInt32(point1.y), Convert.ToInt32(point1.z)].set)
                        return point1;
                }
                if (CheckBounds(point2))
                {
                    if (grid[Convert.ToInt32(point2.x), Convert.ToInt32(point2.y), Convert.ToInt32(point2.z)].set)
                        return point2;
                }

            }
            return new Vec3(-1, 0, 0);
        }

        public List<Vec3> GetAllVoxelOnPlane(Vec3 p1, Vec3 p2, Vec3 p3, bool getAll)
        {
            List<Vec3> result = new List<Vec3>();

            Vec3 normal = (p2 - p1).Cross((p3 - p1));
            float d = -p1.dot(normal);

            uint resolution = xResolution;

            Vec3 mainDirection = new Vec3(1, 0, 0);
            if (Math.Abs(normal.x) < Math.Abs(normal.y))
                if (Math.Abs(normal.z) < Math.Abs(normal.y))
                {
                    resolution = yResolution;
                    mainDirection = new Vec3(0, 1, 0);
                }
                else
                {
                    resolution = zResolution;
                    mainDirection = new Vec3(0, 0, 1);
                }
            else if (Math.Abs(normal.x) < Math.Abs(normal.z))
            {
                resolution = zResolution;
                mainDirection = new Vec3(0, 0, 1);
            }
            Vec3 planeCoord1 = new Vec3(mainDirection.z, mainDirection.x, mainDirection.y);
            Vec3 planeCoord2 = new Vec3(mainDirection.y, mainDirection.z, mainDirection.x);
            float min = -d / mainDirection.dot(normal);
            float max = min;
            float tmp = -(d + (planeCoord1 * resolution).dot(normal)) / mainDirection.dot(normal);
            min = tmp < min ? tmp : min;
            max = tmp > max ? tmp : max;
            tmp = -(d + (planeCoord2 * resolution).dot(normal)) / mainDirection.dot(normal);
            min = tmp < min ? tmp : min;
            max = tmp > max ? tmp : max;
            tmp = -(d + (planeCoord1 * resolution + planeCoord2 * resolution).dot(normal)) / mainDirection.dot(normal);
            min = tmp < min ? tmp : min;
            max = tmp > max ? tmp : max;

            min = (float)Math.Floor(min);
            max = (float)Math.Ceiling(max);
            min = min < 0 ? 0 : min;
            max = max > resolution ? resolution : max;
            Vec3 maxHeightVector = mainDirection * max;
            if (max - min > 1)
            {
                Vec3 point1 = new Vec3();
                Vec3 point2 = new Vec3();

                Vec3 maxVector1 = planeCoord1 * resolution;
                Vec3 minVector1 = new Vec3(0, 0, 0);

                Vec3 minVector2 = new Vec3();
                Vec3 maxVector2 = new Vec3();

                Vec3 lineNormal = normal.Cross(mainDirection);
                lineNormal.Normalize();

                float t;
                float dotTmp;
                Vec3 tmpVec;
                Vec3 tmpResult = new Vec3();

                if (lineNormal.dot(planeCoord1) == 0)
                {
                    tmpVec = planeCoord1;
                    planeCoord1 = planeCoord2;
                    planeCoord2 = tmpVec;

                    maxVector1 = planeCoord1 * resolution;
                    minVector1 = new Vec3(0, 0, 0);
                }

                for (Vec3 mainVec = mainDirection * min; mainVec != maxHeightVector; mainVec = mainVec + mainDirection)
                {
                    if (normal.dot(planeCoord1) != 0)
                    {
                        point1 = planeCoord1 * -(mainVec.dot(normal) + d) / (normal.dot(planeCoord1)) + mainVec;
                        point2 = planeCoord1 * -((mainVec + mainDirection).dot(normal) + d) / (normal.dot(planeCoord1)) + (mainVec + mainDirection);
                    }
                    else
                    {
                        point1 = planeCoord2 * -(mainVec.dot(normal) + d) / (normal.dot(planeCoord2)) + mainVec;
                        point2 = planeCoord2 * -((mainVec + mainDirection).dot(normal) + d) / (normal.dot(planeCoord2)) + (mainVec + mainDirection);
                    }
                    for (Vec3 direction1 = minVector1; direction1 != maxVector1; direction1 += planeCoord1)
                    {
                        dotTmp = lineNormal.dot(planeCoord1);
                        t = (direction1 + mainVec - point1).dot(planeCoord1) / dotTmp;
                        minVector2 = point1 + lineNormal * t;
                        t = (direction1 + mainVec + mainDirection - point2).dot(planeCoord1) / dotTmp;
                        maxVector2 = point2 + lineNormal * t;
                        dotTmp = maxVector2.dot(planeCoord2);
                        if (dotTmp < minVector2.dot(planeCoord2))
                        {
                            tmpVec = minVector2;
                            minVector2 = maxVector2;
                            maxVector2 = tmpVec;
                            dotTmp = maxVector2.dot(planeCoord2);
                        }
                        if (dotTmp < 0)
                            continue;
                        if ((dotTmp+1) > resolution)
                            maxVector2 = planeCoord1 * maxVector2.dot(planeCoord1) + planeCoord2 * resolution + mainVec;
                        else
                            maxVector2 = planeCoord1 * maxVector2.dot(planeCoord1) + planeCoord2 * (dotTmp+1) + mainVec;
                        dotTmp = minVector2.dot(planeCoord2);
                        if (dotTmp > resolution)
                            continue;
                        if (dotTmp < 0)
                            minVector2 = planeCoord1 * (minVector2.dot(planeCoord1)) + mainVec;
                        minVector2 = direction1 + mainVec + planeCoord2 * (float)Math.Floor(planeCoord2.dot(minVector2));
                        maxVector2 = direction1 + mainVec + planeCoord2 * (float)Math.Ceiling(planeCoord2.dot(maxVector2));
                        for (Vec3 direction2 = minVector2; direction2 != maxVector2; direction2 += planeCoord2)
                        {
                            tmpResult.x = (float)Math.Floor(direction2.x);
                            tmpResult.y = (float)Math.Floor(direction2.y);
                            tmpResult.z = (float)Math.Floor(direction2.z);
                            if (grid[(int)tmpResult.x, (int)tmpResult.y, (int)tmpResult.z].set || getAll )
                                result.Add(tmpResult);
                        }
                    }
                }
            }
            else
            {
                Vec3 pos;
                for (int i = 0; i < resolution; ++i)
                {
                    for (int j = 0; j < resolution; ++j)
                    {

                        pos = planeCoord1 * i + planeCoord2 * j + maxHeightVector;
                        if (grid[(int)pos.x, (int)pos.y, (int)pos.z].set || getAll)
                            result.Add(pos);
                    }
                }
            }
            return result;
        }

        private bool CheckBounds(Vec3 vec)
        {
            return vec.x >= 0 && vec.x < xResolution && vec.y >= 0 && vec.y < yResolution && vec.z >= 0 && vec.z < zResolution;
        }
        private bool CheckBounds(int x, int y, int z)
        {
            return x >= 0 && x < xResolution && y >= 0 && y < yResolution && z >= 0 && z < zResolution;
        }

        public void SafeVoxelization(string fileName)
        {
            using(BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
            {
                writer.Write(xResolution);
                writer.Write(yResolution);
                writer.Write(zResolution);
                for(int i = 0; i < xResolution; ++i)
                {
                    for(int j = 0; j < yResolution; ++j)
                    {
                        for(int k = 0; k < zResolution; ++k)
                        {
                            writer.Write(grid[i, j, k].layer);
                            writer.Write(grid[i, j, k].set);
                        }
                    }
                }
            }
        }

        public static void LoadVoxelization(string fileName, ref Voxelizer voxelizer)
        {
            using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open)))
            {
                uint xResolution = reader.ReadUInt32();
                uint yResolution = reader.ReadUInt32();
                uint zResolution = reader.ReadUInt32();

                voxelizer = new Voxelizer(xResolution, yResolution, zResolution);
                Voxel[,,] grid = voxelizer.grid;
                for(int i = 0; i < xResolution; ++i)
                { 
                    for(int j = 0; j < yResolution; ++j)
                    {
                        for(int k = 0; k < zResolution; ++k)
                        {
                            grid[i, j, k].layer = reader.ReadInt32();
                            grid[i, j, k].set = reader.ReadBoolean();
                        }
                    }
                }
            }
        }

        public Voxel[,,] GetGrid()
        {
            return grid;
        }

        public void SafeLayer(string fileName, int layer)
        {
            List<Vec3> layerPoints = new List<Vec3>();

            FindLayer(ref layerPoints, layer);
            if(layerPoints.Count == 0)
            {
                return;
            }

            string header = "\n Das File hat folgendes Format: als erstes ein int der angibt wie viele byte der header lang ist.\n" +
            "Dann kommt der Header als char symbole. Dann kommen 3 unsigned ints die die Dimension des grids in x,y,z richtung angeben.\n"
            + "Dann kommt die ein float Wert der die Groese des einer Zelle im Grid angibt, dann drei floats, die einen offset des Grid nullpunkts (unten links im grid) von dem nullpunkt im wolrd space angibt. \n"
            + "nun kommt noch ein int Wert der die Layernummer angibt und ein int für die anzahl N der nun folgenden Punkte. \n"
            + "Jeder Punkt besteht nun aus drei floats x,y und z. Nach N punkten ist die Datei zuende. \n";

            using(BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
            {
                writer.Write(header.Length);
                writer.Write(header.ToCharArray());
                writer.Write(xResolution);
                writer.Write(yResolution);
                writer.Write(zResolution);
                writer.Write(sizeVoxel);
                writer.Write(minOffset.x);
                writer.Write(minOffset.y);
                writer.Write(minOffset.z);
                writer.Write(layer);
                writer.Write(layerPoints.Count);
                for(int i = 0; i < layerPoints.Count; ++i)
                {
                    writer.Write(layerPoints[i].x);
                    writer.Write(layerPoints[i].y);
                    writer.Write(layerPoints[i].z);
                }
            }
        }

        public void CalculateCenterOfMass()
        {
            centerOfMass = new Vec3(0, 0, 0);
            int numElements = 0;
            for (int i = 0; i < xResolution; ++i)
            {
                for (int j = 0; j < yResolution; ++j)
                {
                    for (int k = 0; k < zResolution; ++k)
                    {
                        if (grid[i, j, k].set)
                        {
                            ++numElements;
                            centerOfMass += new Vec3(i, j, k);
                        }
                    }
                }
            }
            centerOfMass /= numElements;
        }

        public Vec3 GetCenterOfMass()
        {
            return centerOfMass;
        }

        private void FindLayer(ref List<Vec3> points, int layer)
        {
            Voxel tmp;
            for(int i = 0; i < xResolution; ++i)
            {
                for(int j = 0; j < yResolution; ++j)
                {
                    for(int k = 0; k < zResolution; ++k)
                    {
                        tmp = grid[i, j, k];
                        if(tmp.set && tmp.layer == layer)
                        {
                            points.Add(new Vec3(i, j, k));
                        }
                    }
                }
            }
        }

        public void CreateLayer(ref List<Vec3> startPlane)
        {
            //Gets the positions(indices) of all the Voxel contained in the start plane.
            //The grid, member of voxelizer class, consists of voxel objects, each object has a integer called layer which must be set to the value of the corresponding layer. 
            CreateLayerDirection(false, true, false);
        }
    }
}
