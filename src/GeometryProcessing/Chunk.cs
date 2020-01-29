using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;

namespace STLLoader
{
    struct ChunkHeader
    {
        public Chunk chunk;
        public bool visible;
        public int layerMin;
        public int layerMax;
    }

    [Serializable]
    class Chunk
    {
        public Vec3 pos;
        public Voxel[,,] voxel;

        public Chunk(uint resolution, Vec3 pos)
        {
            this.pos = pos;
            voxel = new Voxel[resolution, resolution, resolution];
        }

        public Chunk(String fileName, BinaryFormatter formatter)
        {
            Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            Chunk tmp = (Chunk)formatter.Deserialize(stream);
            pos = tmp.pos;
            voxel = tmp.voxel;
            stream.Close();
        }

        public void SaveChunk(String additionalInf, BinaryFormatter binFormatter)
        {
            Stream stream = new FileStream(additionalInf + "_" + (uint)pos.x + "_" + (uint)pos.y + "_" + (uint)pos.z, FileMode.Create, FileAccess.Write, FileShare.None);
            binFormatter.Serialize(stream, this);
            stream.Close();
        }

        public static Chunk LoadChunk(String fileName, BinaryFormatter formatter)
        {
            Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            Chunk tmp = (Chunk)formatter.Deserialize(stream);
            stream.Close();
            return tmp;
        }

        public void SetFalse()
        {
            int resolution = voxel.GetLength(0);
            for (uint i = 0; i < resolution; ++i)
            {
                for (uint j = 0; j < resolution; ++j)
                {
                    for (uint k = 0; k < resolution; ++k)
                    {
                        voxel[i, j, k].set = false;
                    }
                }
            }
        }
    }
}
