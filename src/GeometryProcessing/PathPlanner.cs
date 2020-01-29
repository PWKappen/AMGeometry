using System.Collections.Generic;
using System;
using System.Collections;

namespace STLLoader
{

    public class PathPlanner
    {

        public Vec3 result;

        public bool FindLayer(int layer, Voxel[,,] voxelCube)
        {
            for (int i = 0; i < voxelCube.GetLength(0); ++i)
            {
                for (int j = 0; j < voxelCube.GetLength(1); ++j)
                {
                    for (int k = 0; k < voxelCube.GetLength(2); ++k)
                    {
                        if (voxelCube[i, j, k].layer == layer && voxelCube[i, j, k].set)
                        {
                            result = new Vec3(i, j, k);
                            return true;
                        }

                    }
                }
            }
            return false;
        }

        private void FindOptimalDirection(ref List<Vec3> simplePolygon, out int p1, out int p2, out int distPoint)
        {
            List<float> distances = new List<float>(simplePolygon.Count);
            List<int> indices = new List<int>(simplePolygon.Count);

            for (int i = 0; i < simplePolygon.Count; ++i)
            {
                distances[i] = 0;
                indices[i] = -1;
            }

            Vec3 vec1;
            Vec3 vec2;
            float length;

            for (int i = 0; i < simplePolygon.Count; ++i)
            {
                vec1 = simplePolygon[i];
                vec2 = simplePolygon[(i + 1) % simplePolygon.Count] - vec1;

                for (int j = 0; j < simplePolygon.Count; ++j)
                {
                    length = (simplePolygon[i] - vec1).Cross(vec2).Abs() / vec2.Abs();
                    if(length > distances[i])
                    {
                        distances[i] = length;
                        indices[i] = j;
                    }
                }
            }

            int idx = -1;
            length = float.PositiveInfinity;

            for (int i = 0; i < simplePolygon.Count; ++i)
            {
                if (distances[i] < length)
                {
                    idx = i;
                    length = distances[i];
                }
            }

            p1 = idx;
            p2 = idx + 1;
            distPoint = indices[idx];
        }

        public void ExtractLeftChainAndOffset(ref List<Vec3> simplePolygon, out List<Vec3> result, int idx1, float offset, ref Vec3 dir)
        {
            int numPoints = simplePolygon.Count + 1 / 2;
            result = new List<Vec3>(simplePolygon);

            int currentIdx = idx1;

            Vec3 tmpVex;

            for (int i = 0; i < numPoints; ++i)
            {
                tmpVex = (result[currentIdx] - result[(currentIdx + 1) % simplePolygon.Count]);
                tmpVex.Normalize();
                result[currentIdx] += dir * offset;
            }            
        }

        public void CalculateInterriorZigZag(ref List<Vec3> simplePolygon, out List<Vec3> points, int idx1, int idx2, int endIdx, float offset)
        {
            points = new List<Vec3>();

            Vec3 direction = simplePolygon[idx2] - simplePolygon[idx1];
            direction.Normalize();

            int idx2Adder = idx2;
            int idx1Adder = idx1;

            int size = (int)simplePolygon.Count;

            Vec3 edge2;

            Vec3 edge2Norm;

            Vec3 parallel2;
            Vec3 normal2;

            Vec3 parallelNorm2;
            Vec3 normalNorm2;

            Vec3 newPoint2;
;

            List<Vec3> points1 = new List<Vec3>();
            List<Vec3> points2 = new List<Vec3>();
            newPoint2 = simplePolygon[(idx2Adder) % size];

            float fraction = offset;

            while (idx2Adder != endIdx)
            {
                edge2 = simplePolygon[(idx2Adder + 1) % size] - simplePolygon[idx2Adder];

                edge2Norm = edge2;

                edge2Norm.Normalize();

                parallel2 = direction * edge2.dot(direction);
                normal2 = direction - parallel2;
                parallelNorm2 = parallel2;
                normalNorm2 = normal2;
                parallelNorm2.Normalize();
                normalNorm2.Normalize();


                if (parallelNorm2.dot(direction) > 0)
                {
                    parallelNorm2 *= -1f;
                }

                float normal2Abs = normal2.Abs();

                float numPoints = (normal2Abs - fraction) / offset;

                int fullPoints = (int)Math.Floor(numPoints);
                float remainingFraction = numPoints - (float)fullPoints;

                if(idx2Adder == idx2)
                    newPoint2 = newPoint2 + normalNorm2 * fraction + parallel2 * fraction + parallelNorm2 * offset;
                else
                    newPoint2 = newPoint2 + normalNorm2 * fraction + parallel2 * fraction;

                points2.Add(newPoint2);

                for(int i = 0; i < fullPoints; ++i)
                {
                    newPoint2 = newPoint2 + normalNorm2 * offset + parallel2 * offset;
                    points2.Add(newPoint2);
                }

                newPoint2 = newPoint2 + normalNorm2 * remainingFraction + parallel2 * remainingFraction;
                fraction = offset - remainingFraction;

                idx2Adder = (idx2Adder + 1) % size;
            }

            newPoint2 = simplePolygon[(idx1Adder) % size];

            fraction = offset;

            while (idx1Adder != endIdx)
            {
                edge2 = simplePolygon[(idx1Adder - 1) % size] - simplePolygon[idx1Adder];

                edge2Norm = edge2;

                edge2Norm.Normalize();


                parallel2 = direction * edge2.dot(direction);
                normal2 = direction - parallel2;
                parallelNorm2 = parallel2;
                normalNorm2 = normal2;
                parallelNorm2.Normalize();
                normalNorm2.Normalize();


                if (parallelNorm2.dot(direction) < 0)
                {
                   parallelNorm2 *= -1f;
                }

                float normal2Abs = normal2.Abs();

                float numPoints = (normal2Abs - fraction) / offset;

                int fullPoints = (int)Math.Floor(numPoints);
                float remainingFraction = numPoints - (float)fullPoints;

                if (idx1Adder == idx1)
                    newPoint2 = newPoint2 + normalNorm2 * fraction + parallel2 * fraction + parallelNorm2 * offset;
                else
                    newPoint2 = newPoint2 + normalNorm2 * fraction + parallel2 * fraction;

                points1.Add(newPoint2);

                for (int i = 0; i < fullPoints; ++i)
                {
                    newPoint2 = newPoint2 + normalNorm2 * offset + parallel2 * offset;
                    points1.Add(newPoint2);
                }

                newPoint2 = newPoint2 + normalNorm2 * remainingFraction + parallel2 * remainingFraction;
                fraction = offset - remainingFraction;

                idx1Adder = (idx1Adder - 1) % size;
            }

            size = points1.Count > points2.Count ? points1.Count : points2.Count;


            for(int i = 0; i < size; ++i)
            {
                if (i % 2 == 0)
                {
                    points.Add(points2[i]);
                    points.Add(points1[i]);
                }
                else
                {
                    points.Add(points1[i]);
                    points.Add(points2[i]);
                }
            }
        }

        public void CreateZigZagHybridPath(ref List<Vec3> simplePolygon, out List<Vec3> result, float distanceBetweenPaths)
        {
            int idx1, idx2, distPoint;
            FindOptimalDirection(ref simplePolygon, out idx1, out idx2, out distPoint);
            List<Vec3> transformed;
            Vec3 transformDir = (simplePolygon[idx2] - simplePolygon[idx1]);
            transformDir.Normalize();
            ExtractLeftChainAndOffset(ref simplePolygon, out transformed, idx1, distanceBetweenPaths, ref transformDir);
            CalculateInterriorZigZag(ref simplePolygon, out result, idx1, idx2, distPoint, distanceBetweenPaths);
        }
        
        public List<Vec3> FindPath(Voxel[,,] voxelCube, Vec3 start, int layer)
        {
            List<Vec3> result = new List<Vec3>();
            result.Add(start);
            Vec3 current = start;

            int oldX = 0;
            int oldY = 0;
            int oldZ = 0;

            int currentX = (int)current.x;
            int currentY = (int)current.y;
            int currentZ = (int)current.z;

            int tmpX = 0;
            int tmpY = 0;
            int tmpZ = 0;

            int newX = 0;
            int newY = -1;
            int newZ = 0;

            bool done = false;

            bool[,,] used = new bool[voxelCube.GetLength(0), voxelCube.GetLength(1), voxelCube.GetLength(2)];
            for (int i = 0; i < voxelCube.GetLength(0); ++i)
            {
                for (int j = 0; j < voxelCube.GetLength(1); ++j)
                {
                    for (int k = 0; k < voxelCube.GetLength(2); ++k)
                        used[i, j, k] = !voxelCube[i, j, k].set || voxelCube[i, j, k].layer != layer;
                }
            }

            used[(int)start.x, (int)start.y, (int)start.z] = true;

            if (FindDirection(voxelCube, used, current, out oldX, out oldY, out oldZ))
            {

                int trys = 0;
                do
                {
                    oldX = newZ;
                    oldY = -newX;
                    oldZ = newY;
                    trys = 0;
                    while (trys < 6)
                    {

                        tmpX = currentX + newX;
                        tmpY = currentY + newY;
                        tmpZ = currentZ + newZ;

                        if (CheckBounds(voxelCube.GetLength(0), voxelCube.GetLength(1), voxelCube.GetLength(2), tmpX, tmpY, tmpZ) && !used[tmpX, tmpY, tmpZ])
                        {
                            oldX = newX;
                            oldY = newY;
                            oldZ = newZ;
                            break;
                        }
                        oldX = newZ;
                        oldY = -newX;
                        oldZ = newY;

                        newX = oldX;
                        newY = oldY;
                        newZ = oldZ;
                        ++trys;
                    }

                    if (trys < 6)
                    {
                        current = new Vec3(tmpX, tmpY, tmpZ);
                        if (current != start)
                            result.Add(current);
                        currentX = (int)current.x;
                        currentY = (int)current.y;
                        currentZ = (int)current.z;
                        used[currentX, currentY, currentZ] = true;
                    }
                    else
                    {
                        Vec3 tmpPoint = new Vec3();
                        if (FindUnused(used, current, out tmpPoint))
                        {
                            current = new Vec3(tmpPoint.x, tmpPoint.y, tmpPoint.z);
                            if (current != start)
                                result.Add(current);
                            currentX = (int)current.x;
                            currentY = (int)current.y;
                            currentZ = (int)current.z;
                            used[currentX, currentY, currentZ] = true;
                            FindDirection(voxelCube, used, current, out oldX, out oldY, out oldZ);
                        }
                        else
                        {
                            done = true;
                        }
                    }
                } while (!done);
            }
            return result;
        }
        
        bool CheckBounds(int length1, int length2, int length3, int px, int py, int pz)
        {
            return px >= 0 && py >= 0 && pz >= 0 && px < length1 && py < length2 && pz < length3;
        }
        
        
        bool FindUnused(bool[,,] used, Vec3 start, out Vec3 found)
        {
            Queue open = new Queue();
            open.Enqueue(start);
            bool[,,] visited = new bool[used.GetLength(0), used.GetLength(1), used.GetLength(2)];
            visited[(int)start.x, (int)start.y, (int)start.z] = true;

            Vec3 current;
            int tmpX = 0;
            int tmpY = 0;
            int tmpZ = 0;
            while (open.Count > 0)
            {
                current = (Vec3)open.Dequeue();
                if (!used[(int)current.x, (int)current.y, (int)current.z])
                {
                    found = current;
                    return true;
                }

                tmpX = (int)current.x + 1;
                tmpY = (int)current.y;
                tmpZ = (int)current.z;

                if (CheckBounds(used.GetLength(0), used.GetLength(1), used.GetLength(2), tmpX, tmpY, tmpZ) && !visited[tmpX, tmpY, tmpZ])
                {
                    open.Enqueue(new Vec3(tmpX, tmpY, tmpZ));
                    visited[tmpX, tmpY, tmpZ] = true;
                }

                tmpX = (int)current.x;
                tmpY = (int)current.y + 1;
                tmpZ = (int)current.z;
                if (CheckBounds(used.GetLength(0), used.GetLength(1), used.GetLength(2), tmpX, tmpY, tmpZ) && !visited[tmpX, tmpY, tmpZ])
                {
                    open.Enqueue(new Vec3(tmpX, tmpY, tmpZ));
                    visited[tmpX, tmpY, tmpZ] = true;
                }

                tmpX = (int)current.x;
                tmpY = (int)current.y;
                tmpZ = (int)current.z + 1;
                if (CheckBounds(used.GetLength(0), used.GetLength(1), used.GetLength(2), tmpX, tmpY, tmpZ) && !visited[tmpX, tmpY, tmpZ])
                {
                    open.Enqueue(new Vec3(tmpX, tmpY, tmpZ));
                    visited[tmpX, tmpY, tmpZ] = true;
                }

                tmpX = (int)current.x - 1;
                tmpY = (int)current.y;
                tmpZ = (int)current.z;

                if (CheckBounds(used.GetLength(0), used.GetLength(1), used.GetLength(2), tmpX, tmpY, tmpZ) && !visited[tmpX, tmpY, tmpZ])
                {
                    open.Enqueue(new Vec3(tmpX, tmpY, tmpZ));
                    visited[tmpX, tmpY, tmpZ] = true;
                }

                tmpX = (int)current.x;
                tmpY = (int)current.y - 1;
                tmpZ = (int)current.z;
                if (CheckBounds(used.GetLength(0), used.GetLength(1), used.GetLength(2), tmpX, tmpY, tmpZ) && !visited[tmpX, tmpY, tmpZ])
                {
                    open.Enqueue(new Vec3(tmpX, tmpY, tmpZ));
                    visited[tmpX, tmpY, tmpZ] = true;
                }

                tmpX = (int)current.x;
                tmpY = (int)current.y;
                tmpZ = (int)current.z - 1;
                if (CheckBounds(used.GetLength(0), used.GetLength(1), used.GetLength(2), tmpX, tmpY, tmpZ) && !visited[tmpX, tmpY, tmpZ])
                {
                    open.Enqueue(new Vec3(tmpX, tmpY, tmpZ));
                    visited[tmpX, tmpY, tmpZ] = true;
                }
            }

            found = new Vec3(-1f, -1f, -1f);
            return false;
        }
        
        bool FindDirection(Voxel[,,] voxelCube, bool[,,] used, Vec3 current, out int oldX, out int oldY, out int oldZ)
        {
            int trys = 0;
            oldX = 0;
            oldY = 0;
            oldZ = 0;

            int currentX = (int) current.x;
            int currentY = (int) current.y;
            int currentZ = (int) current.z;

            int tmpX = 0;
            int tmpY = 0;
            int tmpZ = 0;

            int newX = 0;
            int newY = -1;
            int newZ = 0;

            bool foundNotSet = false;

            while (!foundNotSet && trys < 6)
            {
                trys++;
                oldX = newZ;
                oldY = -newX;
                oldZ = newY;

                tmpX = currentX + oldX;
                tmpY = currentY + oldY;
                tmpZ = currentZ + oldZ;

                if (!CheckBounds(voxelCube.GetLength(0), voxelCube.GetLength(1), voxelCube.GetLength(2), tmpX, tmpY, tmpZ) || used[tmpX, tmpY, tmpZ])
                {
                    foundNotSet = true;
                }

                newX = oldX;
                newY = oldY;
                newZ = oldZ;
            }


            if (!foundNotSet)
            {
                Console.WriteLine("ERROR: POINT lies not on Edge");
                return false;
            }

            trys = 0;
            while (foundNotSet && trys < 5)
            {
                trys++;
                oldX = newZ;
                oldY = -newX;
                oldZ = newY;

                tmpX = currentX + oldX;
                tmpY = currentY + oldY;
                tmpZ = currentZ + oldZ;

                if (CheckBounds(voxelCube.GetLength(0), voxelCube.GetLength(1), voxelCube.GetLength(2), tmpX, tmpY, tmpZ) && !used[tmpX, tmpY, tmpZ])
                {
                    foundNotSet = false;
                }

                newX = oldX;
                newY = oldY;
                newZ = oldZ;
            }
            return true;
        }
    }
}

    
