using System;
using System.Collections.Generic;

namespace STLLoader
{
    struct HalfEdge
    {
        public int vertex;
        public int face;
        public int opposite;
    }

    struct Face
    {
        public int halfEdge;
        public Vec3 normal;
        public int pointList;
    }

    public class Quickhull
    {
        struct PointList
        {
            public int Face;
            public List<int> points;
            public float greatestDistance;
            public int idxDistance;
        }

        private List<Vec3> points;
        private List<Face> faces;
        private List<HalfEdge> edges;
        public float epsilon = 0.00001f;
        public float epsilon2 = 0.00001f;

        public void CalculateQuickhull(List<Vec3> points, ref List<int> resultFaces)
        {
            this.points = points;
            faces = new List<Face>();
            edges = new List<HalfEdge>();

            CreateStart();

            List<PointList> pointList = new List<PointList>();
            CreateStartFaceList(ref pointList);

            PointList currentPointList;
            Vec3 distantPoint;

            List<int> foundFaces = new List<int>();
            List<int> horizonEdges = new List<int>();
            List<int> freeFaces = new List<int>();
            List<int> freePointLists = new List<int>();

            bool found;
            Face currentFace;

            while (pointList.Count > 0 && freePointLists.Count < pointList.Count)
            {

                found = false;
                currentFace = faces[0];
                for (int i = 0; i < faces.Count; ++i)
                {
                    currentFace = faces[i];
                    if (currentFace.pointList > -1)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    break;
                currentPointList = pointList[currentFace.pointList];
                distantPoint = points[currentPointList.idxDistance];
                foundFaces.Clear();
                horizonEdges.Clear();

                FindHorizon(ref foundFaces, ref horizonEdges, currentPointList.Face, faces[currentPointList.Face].halfEdge, ref distantPoint);
                CreateNewFaces(ref foundFaces, ref horizonEdges, ref pointList, ref freeFaces, ref freePointLists, currentPointList.idxDistance);

            }

            for (int i = 0; i < faces.Count; ++i)
            {
                if (faces[i].pointList != -2)
                {
                    resultFaces.Add(edges[faces[i].halfEdge].vertex);
                    resultFaces.Add(edges[faces[i].halfEdge + 2].vertex);
                    resultFaces.Add(edges[faces[i].halfEdge + 1].vertex);
                }
            }

        }

        public void CalculatePolygonEdges(ref List<List<int>> indices)
        {
            List<int> foundFaces = new List<int>();
            List<int> horizonEdges = new List<int>();
            List<Vec3> normals = new List<Vec3>();

            bool found = false;
            Vec3 normal = new Vec3();
            int face = -1;

            while (true)
            {
                found = false;
                for (int i = 0; i < faces.Count; ++i)
                {
                    if (faces[i].pointList != -2)
                    {
                        normal = faces[i].normal;
                        if (!normals.Contains(normal))
                        {
                            face = i;
                            found = true;
                            break;
                        }
                    }
                }
                if (!found)
                    break;

                normals.Add(normal);
                foundFaces.Clear();
                horizonEdges.Clear();
                FindHorizonParallel(ref foundFaces, ref horizonEdges, face, faces[face].halfEdge, normal);
                List<int> newIndices = new List<int>();
                for (int i = 0; i < horizonEdges.Count; ++i)
                    newIndices.Add(edges[horizonEdges[i]].vertex);
                indices.Add(newIndices);
            }

        }

        private void CreateStart()
        {
            float lowestX = points[0].x, highestX = points[0].x;
            float lowestY = points[0].y, highestY = points[0].y;
            float lowestZ = points[0].z, highestZ = points[0].z;
            int[] lowest = new int[3];
            int[] highest = new int[3];

            int listSize = points.Count;
            Vec3 point;

            for (int i = 0; i < listSize; ++i)
            {
                point = points[i];
                if (point.x < lowestX)
                {
                    lowestX = point.x;
                    lowest[0] = i;
                }
                if (point.x > highestX)
                {
                    highestX = point.x;
                    highest[0] = i;
                }
                if (point.y < lowestY)
                {
                    lowestY = point.y;
                    lowest[1] = i;
                }
                if (point.y > highestY)
                {
                    highestY = point.y;
                    highest[1] = i;
                }
                if (point.z < lowestZ)
                {
                    lowestZ = point.z;
                    lowest[2] = i;
                }
                if (point.z > highestZ)
                {
                    highestZ = point.z;
                    highest[2] = i;
                }
            }

            int greatestDistanceUp = lowest[0];
            int greatestDistanceDown = highest[0];
            Vec3 distanceVec = points[greatestDistanceDown] - points[greatestDistanceUp];
            float maxDistance = distanceVec.dot(distanceVec);
            float tmpDistance = 0;

            for (int i = 0; i < 3; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    distanceVec = points[lowest[i]] - points[highest[j]];
                    tmpDistance = distanceVec.dot(distanceVec);
                    if (tmpDistance > maxDistance)
                    {
                        greatestDistanceUp = lowest[i];
                        greatestDistanceDown = highest[j];
                        maxDistance = tmpDistance;
                    }
                }
            }

            distanceVec = points[greatestDistanceDown] - points[greatestDistanceUp];
            int distanceIdx = 0;
            Vec3 tmpVec;
            float newMaxDistance = 0;

            for (int i = 0; i < 3; ++i)
            {
                tmpVec = distanceVec.Cross(points[greatestDistanceUp] - points[lowest[i]]);
                tmpDistance = (tmpVec).dot(tmpVec) / maxDistance;
                if (tmpDistance > newMaxDistance)
                {
                    distanceIdx = lowest[i];
                    newMaxDistance = tmpDistance;
                }
            }

            for (int i = 0; i < 3; ++i)
            {
                tmpVec = distanceVec.Cross(points[greatestDistanceUp] - points[highest[i]]);
                tmpDistance = (tmpVec).dot(tmpVec) / maxDistance;
                if (tmpDistance > newMaxDistance)
                {
                    distanceIdx = highest[i];
                    newMaxDistance = tmpDistance;
                }
            }

            Vec3 normal = (distanceVec.Cross(points[greatestDistanceDown] - points[distanceIdx]));
            normal.Normalize();

            newMaxDistance = 0;
            int distanceIdx2 = 0;

            for (int i = 0; i < 3; ++i)
            {
                tmpDistance = Math.Abs(normal.dot(points[distanceIdx] - points[lowest[i]]));
                if (tmpDistance > newMaxDistance)
                {
                    distanceIdx2 = lowest[i];
                    newMaxDistance = tmpDistance;
                }
            }

            for (int i = 0; i < 3; ++i)
            {
                tmpDistance = Math.Abs(normal.dot(points[distanceIdx] - points[highest[i]]));
                if (tmpDistance > newMaxDistance)
                {
                    distanceIdx2 = highest[i];
                    newMaxDistance = tmpDistance;
                }
            }

            if (normal.dot(points[distanceIdx2] - points[distanceIdx]) > 0)
            {
                //Face 0
                HalfEdge newHalfEdge = new HalfEdge();
                newHalfEdge.vertex = greatestDistanceUp;
                newHalfEdge.face = 0;
                newHalfEdge.opposite = 3;
                edges.Add(newHalfEdge);

                newHalfEdge.vertex = distanceIdx;
                newHalfEdge.face = 0;
                newHalfEdge.opposite = 6;
                edges.Add(newHalfEdge);

                newHalfEdge.vertex = greatestDistanceDown;
                newHalfEdge.face = 0;
                newHalfEdge.opposite = 9;
                edges.Add(newHalfEdge);

                //Face 1
                newHalfEdge.vertex = greatestDistanceDown;
                newHalfEdge.face = 1;
                newHalfEdge.opposite = 0;
                edges.Add(newHalfEdge);

                newHalfEdge.vertex = distanceIdx2;
                newHalfEdge.face = 1;
                newHalfEdge.opposite = 11;
                edges.Add(newHalfEdge);

                newHalfEdge.vertex = greatestDistanceUp;
                newHalfEdge.face = 1;
                newHalfEdge.opposite = 7;
                edges.Add(newHalfEdge);

                //Face2
                newHalfEdge.vertex = greatestDistanceUp;
                newHalfEdge.face = 2;
                newHalfEdge.opposite = 1;
                edges.Add(newHalfEdge);

                newHalfEdge.vertex = distanceIdx2;
                newHalfEdge.face = 2;
                newHalfEdge.opposite = 5;
                edges.Add(newHalfEdge);

                newHalfEdge.vertex = distanceIdx;
                newHalfEdge.face = 2;
                newHalfEdge.opposite = 10;
                edges.Add(newHalfEdge);

                //Face3
                newHalfEdge.vertex = distanceIdx;
                newHalfEdge.face = 3;
                newHalfEdge.opposite = 2;
                edges.Add(newHalfEdge);

                newHalfEdge.vertex = distanceIdx2;
                newHalfEdge.face = 3;
                newHalfEdge.opposite = 8;
                edges.Add(newHalfEdge);

                newHalfEdge.vertex = greatestDistanceDown;
                newHalfEdge.face = 3;
                newHalfEdge.opposite = 4;
                edges.Add(newHalfEdge);
            }

            else
            {
                //Face 0
                HalfEdge newHalfEdge = new HalfEdge();
                newHalfEdge.vertex = distanceIdx;
                newHalfEdge.face = 0;
                newHalfEdge.opposite = 11;
                edges.Add(newHalfEdge);

                newHalfEdge.vertex = greatestDistanceUp;
                newHalfEdge.face = 0;
                newHalfEdge.opposite = 8;
                edges.Add(newHalfEdge);

                newHalfEdge.vertex = greatestDistanceDown;
                newHalfEdge.face = 0;
                newHalfEdge.opposite = 5;
                edges.Add(newHalfEdge);

                //Face 1
                newHalfEdge.vertex = distanceIdx2;
                newHalfEdge.face = 1;
                newHalfEdge.opposite = 7;
                edges.Add(newHalfEdge);

                newHalfEdge.vertex = greatestDistanceDown;
                newHalfEdge.face = 1;
                newHalfEdge.opposite = 9;
                edges.Add(newHalfEdge);

                newHalfEdge.vertex = greatestDistanceUp;
                newHalfEdge.face = 1;
                newHalfEdge.opposite = 2;
                edges.Add(newHalfEdge);

                //Face2
                newHalfEdge.vertex = distanceIdx2;
                newHalfEdge.face = 2;
                newHalfEdge.opposite = 10;
                edges.Add(newHalfEdge);

                newHalfEdge.vertex = greatestDistanceUp;
                newHalfEdge.face = 2;
                newHalfEdge.opposite = 3;
                edges.Add(newHalfEdge);

                newHalfEdge.vertex = distanceIdx;
                newHalfEdge.face = 2;
                newHalfEdge.opposite = 1;
                edges.Add(newHalfEdge);

                //Face3
                newHalfEdge.vertex = distanceIdx2;
                newHalfEdge.face = 3;
                newHalfEdge.opposite = 4;
                edges.Add(newHalfEdge);

                newHalfEdge.vertex = distanceIdx;
                newHalfEdge.face = 3;
                newHalfEdge.opposite = 6;
                edges.Add(newHalfEdge);

                newHalfEdge.vertex = greatestDistanceDown;
                newHalfEdge.face = 3;
                newHalfEdge.opposite = 0;
                edges.Add(newHalfEdge);
            }

            Face newFace = new Face();
            for (int i = 0; i < 4; ++i)
            {
                newFace.halfEdge = i * 3;
                newFace.normal = ((points[edges[i * 3 + 2].vertex] - points[edges[i * 3].vertex]).Cross(points[edges[i * 3 + 1].vertex] - points[edges[i * 3].vertex]));
                newFace.normal.Normalize();
                newFace.pointList = -1;
                faces.Add(newFace);
            }
        }

        private void CreateStartFaceList(ref List<PointList> pointList)
        {
            int i, j;
            int pointsCount = points.Count;
            int faceCount = faces.Count;

            float magnitude;
            Vec3 normal;
            Vec3 vertex;
            PointList newPointList = new PointList();

            Face newFace = new Face();

            for (j = 0; j < faceCount; ++j)
            {
                vertex = points[edges[faces[j].halfEdge].vertex];
                normal = faces[j].normal;
                newPointList.Face = j;
                newPointList.points = new List<int>();
                newPointList.idxDistance = 0;
                newPointList.greatestDistance = -10;

                for (i = 0; i < pointsCount; ++i)
                {
                    magnitude = normal.dot(points[i] - vertex);
                    if (magnitude > 0)
                    {
                        newPointList.points.Add(i);
                        if (magnitude > newPointList.greatestDistance)
                        {
                            newPointList.greatestDistance = magnitude;
                            newPointList.idxDistance = i;
                        }
                    }
                }
                if (newPointList.points.Count > 0)
                {

                    newFace.halfEdge = faces[j].halfEdge;
                    newFace.normal = faces[j].normal;
                    newFace.pointList = pointList.Count;
                    faces[j] = newFace;
                    pointList.Add(newPointList);
                }
            }
        }

        private bool FindHorizon(ref List<int> foundFaces, ref List<int> horizonEdges, int currentFace, int currentHalfEdge, ref Vec3 vertex)
        {
            Face current = faces[currentFace];
            int halfEdge = current.halfEdge;

            Vec3 vertexOnFace = points[edges[halfEdge].vertex];
            if (current.normal.dot(vertex - vertexOnFace) >= 0 - epsilon)
            {
                foundFaces.Add(currentFace);
                for (int i = 1; i <= 3; ++i)
                {
                    currentFace = edges[edges[currentHalfEdge].opposite].face;
                    if (!foundFaces.Contains(currentFace))
                        if (!FindHorizon(ref foundFaces, ref horizonEdges, currentFace, edges[currentHalfEdge].opposite, ref vertex))
                            horizonEdges.Add(currentHalfEdge);
                    currentHalfEdge = halfEdge + ((currentHalfEdge + 1) % 3);
                }

                return true;
            }
            return false;
        }

        private bool FindHorizonParallel(ref List<int> foundFaces, ref List<int> horizonEdges, int currentFace, int currentHalfEdge, Vec3 normal)
        {
            Face current = faces[currentFace];
            int halfEdge = current.halfEdge;

            Vec3 normalOnFace = current.normal;
            if (normalOnFace.dot(normal) >= 1f - epsilon2)
            {
                foundFaces.Add(currentFace);
                for (int i = 1; i <= 3; ++i)
                {
                    currentFace = edges[edges[currentHalfEdge].opposite].face;
                    if (!foundFaces.Contains(currentFace))
                        if (!FindHorizonParallel(ref foundFaces, ref horizonEdges, currentFace, edges[currentHalfEdge].opposite, normalOnFace))
                            horizonEdges.Add(currentHalfEdge);
                    currentHalfEdge = halfEdge + ((currentHalfEdge + 1) % 3);
                }

                return true;
            }
            return false;
        }

        private void CreateNewFaces(ref List<int> foundFaces, ref List<int> horzionEdges, ref List<PointList> pointList, ref List<int> freeFaces, ref List<int> freePointLists, int vertex)
        {
            int edgeCount = horzionEdges.Count;
            int i;

            HalfEdge newEdge = new HalfEdge();
            Face newFace = new Face();

            int tmpCount = freeFaces.Count < horzionEdges.Count ? freeFaces.Count : horzionEdges.Count;
            int offset = freeFaces.Count > horzionEdges.Count ? freeFaces.Count - horzionEdges.Count : 0;
            int doneHorizonEdges = 0;

            int iOffset;
            int opposite;
            int tmpHalfEdgeIdx;
            if (tmpCount > 3)
                newEdge = newEdge;
            for (i = 0; i < tmpCount; ++i)
            {
                iOffset = i + offset;
                tmpHalfEdgeIdx = faces[freeFaces[iOffset]].halfEdge;

                newEdge.vertex = edges[horzionEdges[i]].vertex;
                newEdge.opposite = edges[horzionEdges[i]].opposite;
                newEdge.face = freeFaces[iOffset];
                edges[tmpHalfEdgeIdx] = newEdge;

                opposite = newEdge.opposite;
                newEdge = edges[opposite];
                newEdge.opposite = tmpHalfEdgeIdx;
                edges[opposite] = newEdge;

                newEdge.vertex = vertex;
                newEdge.face = freeFaces[iOffset];
                opposite = ((i == tmpCount - 1) ? (freeFaces.Count < horzionEdges.Count ? edges.Count : (faces[freeFaces[offset]].halfEdge)) : faces[freeFaces[iOffset + 1]].halfEdge) + 2;
                newEdge.opposite = opposite;
                edges[tmpHalfEdgeIdx + 1] = newEdge;

                newEdge.vertex = edges[faces[edges[horzionEdges[i]].face].halfEdge + (horzionEdges[i] + 2) % 3].vertex;
                newEdge.face = freeFaces[iOffset];
                opposite = (i == 0 ? (freeFaces.Count < horzionEdges.Count ? (edges.Count + (horzionEdges.Count - freeFaces.Count - 1) * 3 + 1) : faces[freeFaces[offset + tmpCount - 1]].halfEdge + 1) : faces[freeFaces[iOffset - 1]].halfEdge + 1);
                newEdge.opposite = opposite;
                edges[tmpHalfEdgeIdx + 2] = newEdge;

                newFace = faces[freeFaces[iOffset]];
                newFace.normal = ((points[edges[faces[freeFaces[iOffset]].halfEdge + 2].vertex] - points[edges[faces[freeFaces[iOffset]].halfEdge].vertex]).Cross(points[edges[faces[freeFaces[iOffset]].halfEdge + 1].vertex] - points[edges[faces[freeFaces[iOffset]].halfEdge].vertex]));
                newFace.normal.Normalize();
                newFace.pointList = -1;
                faces[freeFaces[iOffset]] = newFace;

                ++doneHorizonEdges;
            }

            int count;
            int oldEdgeCount = edges.Count;
            int totalCountEdges;

            for (i = doneHorizonEdges; i < edgeCount; ++i)
            {
                count = horzionEdges[i];
                newEdge.vertex = edges[count].vertex;
                newEdge.face = faces.Count;
                newEdge.opposite = edges[count].opposite;
                edges.Add(newEdge);

                opposite = newEdge.opposite;
                newEdge = edges[opposite];
                newEdge.opposite = edges.Count - 1;
                edges[opposite] = newEdge;

                newEdge.vertex = vertex;
                newEdge.face = faces.Count;
                newEdge.opposite = (i == (edgeCount - 1) ? (doneHorizonEdges != 0 ? faces[freeFaces[offset]].halfEdge + 2 : oldEdgeCount + 2) : (edges.Count + 4));
                edges.Add(newEdge);

                newEdge.vertex = edges[faces[edges[count].face].halfEdge + ((count + 2) % 3)].vertex;
                newEdge.face = faces.Count;
                newEdge.opposite = (i == doneHorizonEdges ? (doneHorizonEdges != 0 ? faces[freeFaces[tmpCount + offset - 1]].halfEdge + 1 : oldEdgeCount + (3 * edgeCount) - 2) : edges.Count - 4);
                edges.Add(newEdge);

                totalCountEdges = edges.Count;
                newFace.halfEdge = totalCountEdges - 3;
                newFace.normal = ((points[edges[totalCountEdges - 1].vertex] - points[edges[totalCountEdges - 3].vertex]).Cross(points[edges[totalCountEdges - 2].vertex] - points[edges[totalCountEdges - 3].vertex]));
                newFace.normal.Normalize();
                newFace.pointList = -1;
                faces.Add(newFace);
            }

            PointList newPointList = new PointList();
            Vec3 normal;
            Vec3 vertexVec;
            int numPointInList;
            List<int> pointListPoints;
            float magnitude;
            for (i = 0; i < doneHorizonEdges; ++i)
            {
                iOffset = i + offset;
                normal = faces[freeFaces[iOffset]].normal;
                vertexVec = points[edges[faces[freeFaces[iOffset]].halfEdge].vertex];
                newPointList.Face = freeFaces[iOffset];
                newPointList.points = new List<int>();
                newPointList.idxDistance = 0;
                newPointList.greatestDistance = -10;

                for (int j = 0; j < foundFaces.Count; ++j)
                {
                    if (faces[foundFaces[j]].pointList > -1)
                    {
                        pointListPoints = pointList[faces[foundFaces[j]].pointList].points;
                        numPointInList = pointListPoints.Count;
                        for (int k = 0; k < numPointInList; ++k)
                        {
                            if (pointListPoints[k] != -1)
                            {
                                magnitude = normal.dot(points[pointListPoints[k]] - vertexVec);
                                if (magnitude > 0 + epsilon)
                                {
                                    newPointList.points.Add(pointListPoints[k]);
                                    if (magnitude > newPointList.greatestDistance)
                                    {
                                        newPointList.greatestDistance = magnitude;
                                        newPointList.idxDistance = pointListPoints[k];
                                    }
                                    pointListPoints[k] = -1;
                                }
                            }
                        }
                    }
                }
                if (newPointList.points.Count > 0)
                {

                    newFace.halfEdge = faces[freeFaces[iOffset]].halfEdge;
                    newFace.normal = faces[freeFaces[iOffset]].normal;
                    if (freePointLists.Count > 0)
                    {
                        newFace.pointList = freePointLists[freePointLists.Count - 1];
                        pointList[newFace.pointList] = newPointList;
                        freePointLists.RemoveAt(freePointLists.Count - 1);
                    }
                    else
                    {
                        newFace.pointList = pointList.Count;
                        pointList.Add(newPointList);
                    }
                    faces[freeFaces[iOffset]] = newFace;
                }
            }

            freeFaces.RemoveRange(offset, doneHorizonEdges);
            int faceCount = faces.Count;

            for (i = edgeCount - doneHorizonEdges; i > 0; --i)
            {
                normal = faces[faceCount - i].normal;
                vertexVec = points[edges[faces[faceCount - i].halfEdge].vertex];
                newPointList.Face = faces.Count - i;
                newPointList.points = new List<int>();
                newPointList.idxDistance = 0;
                newPointList.greatestDistance = -10;

                for (int j = 0; j < foundFaces.Count; ++j)
                {
                    if (faces[foundFaces[j]].pointList > -1)
                    {
                        pointListPoints = pointList[faces[foundFaces[j]].pointList].points;
                        numPointInList = pointListPoints.Count;
                        for (int k = 0; k < numPointInList; ++k)
                        {
                            if (pointListPoints[k] != -1)
                            {
                                magnitude = normal.dot(points[pointListPoints[k]] - vertexVec);
                                if (magnitude > 0 + epsilon)
                                {
                                    newPointList.points.Add(pointListPoints[k]);
                                    if (magnitude > newPointList.greatestDistance)
                                    {
                                        newPointList.greatestDistance = magnitude;
                                        newPointList.idxDistance = newPointList.points[newPointList.points.Count - 1];
                                    }
                                    pointListPoints[k] = -1;
                                }
                            }
                        }
                    }
                }
                if (newPointList.points.Count > 0)
                {

                    newFace.halfEdge = faces[faceCount - i].halfEdge;
                    newFace.normal = faces[faceCount - i].normal;
                    if (freePointLists.Count > 0)
                    {
                        newFace.pointList = freePointLists[freePointLists.Count - 1];
                        pointList[newFace.pointList] = newPointList;
                        freePointLists.RemoveAt(freePointLists.Count - 1);
                    }
                    else
                    {
                        newFace.pointList = pointList.Count;
                        pointList.Add(newPointList);
                    }
                    faces[faceCount - i] = newFace;
                }
            }

            for (i = 0; i < foundFaces.Count; ++i)
            {
                newFace = faces[foundFaces[i]];
                if (newFace.pointList >= 0)
                    freePointLists.Add(newFace.pointList);
                newFace.pointList = -2;
                freeFaces.Add(foundFaces[i]);
                faces[foundFaces[i]] = newFace;
            }
        }

    }
}
