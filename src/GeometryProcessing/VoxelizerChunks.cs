using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

namespace STLLoader
{
    public struct FlatTriangle
    {
        public Vec3 p1;
        public Vec3 p2;
        public Vec3 p3;

        public Vec3 minBound;
        public Vec3 maxBound;

        public int idxInMesh;

        public void CalcBounding()
        {
            minBound = p1;
            maxBound = p1;
            minBound.x = p2.x < minBound.x ? (p3.x < p2.x ? p3.x : p2.x) : (p3.x < minBound.x ? p3.x : minBound.x);
            minBound.y = p2.y < minBound.y ? (p3.y < p2.y ? p3.y : p2.y) : (p3.y < minBound.y ? p3.y : minBound.y);
            minBound.z = p2.z < minBound.z ? (p3.z < p2.z ? p3.z : p2.z) : (p3.z < minBound.z ? p3.z : minBound.z);

            maxBound.x = p2.x > maxBound.x ? (p3.x > p2.x ? p3.x : p2.x) : (p3.x > maxBound.x ? p3.x : maxBound.x);
            maxBound.y = p2.y > maxBound.y ? (p3.y > p2.y ? p3.y : p2.y) : (p3.y > maxBound.y ? p3.y : maxBound.y);
            maxBound.z = p2.z > maxBound.z ? (p3.z > p2.z ? p3.z : p2.z) : (p3.z > maxBound.z ? p3.z : maxBound.z);

            minBound.x = (float)Math.Floor(minBound.x);
            minBound.y = (float)Math.Floor(minBound.y);
            minBound.z = (float)Math.Floor(minBound.z);

            maxBound.x = (float)Math.Ceiling(minBound.x);
            maxBound.y = (float)Math.Ceiling(minBound.y);
            maxBound.z = (float)Math.Ceiling(minBound.z);
        }
    }

    public class VoxelizerChunks
    {
        ChunkHeader[,,] chunkHeader;
        uint numChunksX;
        uint numChunksY;
        uint numChunksZ;
        uint chunkSize;

        uint xResolution;
        uint yResolution;
        uint zResolution;

        BinaryFormatter binFormatter;

        float sizeVoxel;
        Vec3 minOffset;

        string fileName;

        public VoxelizerChunks(uint xResolution, uint yResolution, uint zResolution, uint sizeChunk, string fileName, float sizeVoxel, Vec3 minOffset)
        {
            this.sizeVoxel = sizeVoxel;
            this.xResolution = xResolution;
            this.yResolution = yResolution;
            this.zResolution = zResolution;

            chunkSize = sizeChunk;
            numChunksX = (uint)Math.Ceiling((float)xResolution / sizeChunk);
            numChunksY = (uint)Math.Ceiling((float)yResolution / sizeChunk);
            numChunksZ = (uint)Math.Ceiling((float)zResolution / sizeChunk);

            chunkHeader = new ChunkHeader[numChunksX, numChunksY, numChunksZ];
            ChunkHeader tmpHeader = new ChunkHeader();
            tmpHeader.chunk = null;
            tmpHeader.layerMax = 0;
            tmpHeader.layerMin = 0;
            tmpHeader.visible = false;

            int i, j, k;

            for (i = 0; i < numChunksX; ++i)
            {
                for(j = 0; j < numChunksY; ++j)
                {
                    for(k = 0; k < numChunksZ; ++k)
                    {
                        chunkHeader[i, j, k] = tmpHeader;
                    }
                }
            }

            binFormatter = new BinaryFormatter();
            Chunk tmpChunk = new Chunk(sizeChunk, new Vec3(0,0,0));
            for (i = 0; i < numChunksX; ++i)
            {
                for (j = 0; j < numChunksY; ++j)
                {
                    for (k = 0; k < numChunksZ; ++k)
                    {
                        tmpChunk.pos = new Vec3(i, j, k);
                        tmpChunk.SaveChunk(fileName, binFormatter);
                    }
                }
            }
        }
    }
}
