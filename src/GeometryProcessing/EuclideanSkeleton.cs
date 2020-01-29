using System;
using System.Collections.Generic;

namespace STLLoader
{
    public class EuclideanSkeleton
    {
        private const int MAXLUTENTRY = 10000;
        private const int MAXWEIGHTING = 15000;

        private MaskG mask;

        public EuclideanSkeleton()
        {
            mask.vg = new LutVec[MAXWEIGHTING];
            mask.ng = 0;
        }


        public struct TestStruct : IComparable
        {
            public float value;
            public Vec3 vector;

            public TestStruct(float value, Vec3 vec)
            {
                this.value = value;
                this.vector = vec;
            }

            public override bool Equals(object obj)
            {
                if (obj.GetType() != typeof(TestStruct))
                    return false;
                TestStruct rhs = (TestStruct)obj;

                return rhs.vector.x == vector.x && rhs.vector.y == vector.y && rhs.vector.z == vector.z && value == rhs.value;
            }

            public int CompareTo(object obj)
            {
                if (obj == null) return 1;
                TestStruct rhs = (TestStruct)obj;
                if (value != rhs.value)
                    return Math.Sign(value - rhs.value);
                else if (vector.x != rhs.vector.x)
                    return Math.Sign(vector.x - rhs.vector.x);
                else if (vector.y != rhs.vector.y)
                    return Math.Sign(vector.y - rhs.vector.y);
                return Math.Sign(vector.z - rhs.vector.z);

            }

            public static bool operator ==(TestStruct lhs, TestStruct rhs)
            {
                return rhs.vector.x == lhs.vector.x && rhs.vector.y == lhs.vector.y && rhs.vector.z == lhs.vector.z && lhs.value == rhs.value;
            }

            public static bool operator !=(TestStruct lhs, TestStruct rhs)
            {
                return rhs.vector.x != lhs.vector.x || rhs.vector.y != lhs.vector.y || rhs.vector.z != lhs.vector.z || lhs.value != rhs.value;
            }

            public static bool operator >(TestStruct lhs, TestStruct rhs)
            {
                if (lhs.value != rhs.value)
                    return lhs.value > rhs.value;
                else if (lhs.vector.x != rhs.vector.x)
                    return lhs.vector.x > rhs.vector.x;
                else if (lhs.vector.y != rhs.vector.y)
                    return lhs.vector.y > rhs.vector.y;
                return lhs.vector.z > rhs.vector.z;
            }

            public static bool operator <(TestStruct lhs, TestStruct rhs)
            {
                if (lhs.value != rhs.value)
                    return lhs.value < rhs.value;
                else if (lhs.vector.x != rhs.vector.x)
                    return lhs.vector.x < rhs.vector.x;
                else if (lhs.vector.y != rhs.vector.y)
                    return lhs.vector.y < rhs.vector.y;
                return lhs.vector.z < rhs.vector.z;
            }

            public static bool operator >=(TestStruct lhs, TestStruct rhs)
            {
                return lhs.value >= rhs.value;
            }

            public static bool operator <=(TestStruct lhs, TestStruct rhs)
            {
                return lhs.value <= rhs.value;
            }
        }

        public void CalculateEuclideanDistanceTransform(ref Voxel[,,] grid, out float[,,] result, bool set)
        {
            int l = grid.GetLength(0);
            int m = grid.GetLength(1);
            int n = grid.GetLength(2);
            result = new float[l, m, n];

            float[] buff = new float[Math.Max(Math.Max(l, m), n)];

            int i, j, k, o;
            float df, db, d, w;
            float tmp;
            int rMax, rStart, rEnd;

            for (i = 0; i < l; ++i)
                for (j = 0; j < m; ++j)
                {
                    df = 0;
                    for (k = 0; k < n; ++k)
                    {
                        df = grid[i, j, k].set == set ? df + 1 : 0;
                        result[i, j, k] = df * df;
                    }
                }

            for (i = 0; i < l; ++i)
                for (j = 0; j < m; ++j)
                {
                    db = 0;
                    for (k = n - 1; k >= 0; --k)
                    {
                        db = grid[i, j, k].set == set ? db + 1 : 0;
                        tmp = db * db;
                        result[i, j, k] = tmp < result[i, j, k] ? tmp : result[i, j, k];
                    }
                }

            for (i = 0; i < l; ++i)
            {
                for (k = 0; k < n; ++k)
                {
                    for (j = 0; j < m; ++j)
                        buff[j] = result[i, j, k];
                    for (j = 0; j < m; ++j)
                    {
                        d = buff[j];
                        if (d != 0)
                        {
                            rMax = (int)Math.Sqrt(d) + 1;
                            rStart = Math.Min(rMax, j);
                            rEnd = Math.Min(rMax + 1, m - j);
                            for (o = -rStart; o < rEnd; ++o)
                            {
                                w = buff[j + o] + o * o;
                                d = w < d ? w : d;
                            }
                        }
                        result[i, j, k] = d;
                    }
                }
            }

            for (j = 0; j < m; ++j)
            {
                for (k = 0; k < n; ++k)
                {
                    for (i = 0; i < l; ++i)
                        buff[i] = result[i, j, k];
                    for (i = 0; i < l; ++i)
                    {
                        d = buff[i];
                        if (d != 0)
                        {
                            rMax = (int)Math.Sqrt(d) + 1;
                            rStart = Math.Min(rMax, i);
                            rEnd = Math.Min(rMax + 1, l - i);
                            for (o = -rStart; o < rEnd; ++o)
                            {
                                w = buff[i + o] + o * o;
                                d = w < d ? w : d;
                            }
                        }
                        result[i, j, k] = d;
                    }
                }
            }
        }

        double D_Intersec(int u, int gu, int v, int gv)
        {
            return (u + v + ((double)gu - gv) / (u - v)) / 2;
        }

        void CompDTg_Hirata(int lx, int ly, int lz, ref int[] CT, ref int[,,] DTg, int R)
        {
            int x, zM, y, z, l2 = ly * lz, p, dp, k, propag;

            int[] Si = new int[lx], Sv = new int[ly];
            int Sn;
            double[] Sr = new double[lz];

            for (zM = 0; zM < lz; ++zM) if (CT[zM] > R) break;
            if (zM >= lz) return;
            for (z = 0; z <= zM; ++z)
            {
                for (y = 0; y <= z; ++y)
                {
                    k = 0; propag = 0;
                    for (x = y; x >= 0; --x)
                    {
                        p = l2 * x + lz * y + z;
                        if (CT[p] > R)
                            propag = 1;
                        else if (propag == 1)
                        {
                            ++k; DTg[x, y, z] = k * k;
                        }
                        else DTg[x, y, z] = -1;
                    }
                }
            }

            for (z = 0; z <= zM; ++z)
                for (x = 0; x <= z; x++)
                {
                    Sn = 0;

                    for (y = x; y <= z; y++)
                    {
                        dp = DTg[x, y, z];
                        if (dp < 0) continue;

                        if (dp == 0 && y > x && DTg[x, y - 1, z] == 0) break;

                        while (Sn >= 2 && D_Intersec(Si[Sn - 1], Sv[Sn - 1], y, dp) < Sr[Sn - 1])
                            Sn--;

                        Si[Sn] = y; Sv[Sn] = dp;
                        if (Sn >= 1) Sr[Sn] = D_Intersec(Si[Sn - 1], Sv[Sn - 1], Si[Sn], Sv[Sn]);
                        Sn++;
                    }

                    if (Sn == 0) continue;

                    for (y = x; y >= z; y--)
                    {
                        if (DTg[x, y, z] == 0) continue;

                        while (Sn >= 2 && y < Sr[Sn - 1]) Sn--;

                        DTg[x, y, z] = (y - Si[Sn - 1]) * (y - Si[Sn - 1]) + Sv[Sn - 1];
                    }
                }


            for (y = 0; y <= zM; y++)
                for (x = 0; x <= y; x++)
                {
                    Sn = 0;

                    for (z = y; z <= zM; z++)
                    {
                        dp = DTg[x, y, z];
                        if (dp < 0) continue;

                        if (dp == 0 && z > y && DTg[x, y, z - 1] == 0) break;

                        while (Sn >= 2 && D_Intersec(Si[Sn - 1], Sv[Sn - 1], z, dp) < Sr[Sn - 1])
                            Sn--;

                        Si[Sn] = z; Sv[Sn] = dp;
                        if (Sn >= 1) Sr[Sn] = D_Intersec(Si[Sn - 1], Sv[Sn - 1], Si[Sn], Sv[Sn]);
                        Sn++;
                    }

                    if (Sn == 0) continue;

                    for (z = zM; z >= y; z--)
                    {
                        if (DTg[x, y, z] == 0) continue;

                        while (Sn >= 2 && z < Sr[Sn - 1]) Sn--;  /* pop */

                        DTg[x, y, z] = (z - Si[Sn - 1]) * (z - Si[Sn - 1]) + Sv[Sn - 1];
                    }
                }
        }

        struct LutVec
        {
            public int x, y, z, r;
        }

        struct MaskG
        {
            public LutVec[] vg;
            public int ng;
        }

        private LutVec CalculatePermutation(int i, ref LutVec vec)
        {
            LutVec result = vec;
            if (i >= 8 && i < 16)
            {
                result.x = vec.z;
                result.y = vec.x;
                result.z = vec.y;
            }
            else if (i >= 16 && i < 24)
            {
                result.x = vec.y;
                result.y = vec.z;
                result.z = vec.x;
            }
            else if (i >= 24 && i < 32)
            {
                result.x = vec.x;
                result.y = vec.z;
                result.z = vec.y;
            }
            else if (i >= 32 && i < 40)
            {
                result.x = vec.y;
                result.y = vec.x;
                result.z = vec.z;
            }
            else if (i >= 40 && i < 46)
            {
                result.x = vec.z;
                result.y = vec.y;
                result.z = vec.x;
            }

            if (i % 8 == 1)
            {
                result.x = -result.x;
                result.y = result.y;
                result.z = result.z;
            } else if (i % 8 == 2)
            {
                result.x = result.x;
                result.y = -result.y;
                result.z = result.z;
            }
            else if (i % 8 == 3)
            {
                result.x = result.x;
                result.y = result.y;
                result.z = -result.z;
            }
            else if (i % 8 == 4)
            {
                result.x = -result.x;
                result.y = -result.y;
                result.z = result.z;
            }
            else if (i % 8 == 5)
            {
                result.x = -result.x;
                result.y = result.y;
                result.z = -result.z;
            }
            else if (i % 8 == 6)
            {
                result.x = result.x;
                result.y = -result.y;
                result.z = -result.z;
            }
            else if (i % 8 == 7)
            {
                result.x = -result.x;
                result.y = -result.y;
                result.z = -result.z;
            }

            return result;
        }


        private void CompCTg(int lx, int ly, int lz, ref int[] CT)
        {
            int l2 = ly * lz;
            for (int i = 0; i < lx; ++i)
                for (int j = 0; j <= i && j < ly; ++j)
                    for (int k = 0; k <= j && k < lz; ++k)
                        CT[k * l2 + j * lz + i] = (int)(i * i + j * j + k * k);
        }

        private void CompLutCol(ref int[] CT, int lx, int ly, int lz, LutVec vec, int idx, int rMax, ref int[,] Lut)
        {
            int  r1, r2, rb, l2 = ly*lz;
            for (int r = 0; r <= rMax; ++r)
                Lut[idx, r] = 0;
            for(int z = 0; z < lz -vec.z; ++z)
            {
                for (int y = 0; y <= z && y < ly; ++y)
                {
                    for(int x = 0; x <= y && x < lx; ++x)
                    {
                        r1 = CT[x *l2 + y*lz + z] + 1;
                        r2 = CT[(x + vec.x)*l2 + (y + vec.y)*lz + z + vec.z] + 1;
                        if (r1 <= rMax && r2 > Lut[idx, r1])
                            Lut[idx, r1] = r2;
                    }
                }
            }

            rb = 0;
            for(int ra = 0; ra < rMax; ++ra)
            {
                if (Lut[idx, ra] > rb)
                    rb = Lut[idx, ra];
                else
                    Lut[idx, ra] = rb;
            }
        }

        public void CalculateMedialAx(ref Voxel[, ,] grid, ref float[, ,] disField, out bool[, ,] result)
        {
            int l = grid.GetLength(0);
            int m = grid.GetLength(1);
            int n = grid.GetLength(2);

            result = new bool[l, m, n];
            int r = (int)Math.Ceiling(FindMax(ref disField));
            int[,] lut = new int[MAXWEIGHTING, MAXLUTENTRY];

            CompLutMask(l, m, n, ref lut, 0, r*r);

            for (int i = 0; i < l; ++i)
                for (int j = 0; j < m; ++j)
                    for (int k = 0; k < n; ++k)
                    {
                        bool set = grid[i,j,k].set;
                        for(int vIdx = 0; vIdx < MAXWEIGHTING; ++vIdx)
                        {
                            LutVec org = mask.vg[vIdx];
                            for (int h = 0; h < 46; ++h)
                            {
                                LutVec v = CalculatePermutation(h, ref org);
                                if (v.x == 0 && v.y == 0 && v.z == 0 && v.r == 0)
                                    continue;
                                if (i + v.x >= 0 && i + v.x < l && j + v.y >= 0 && j + v.y < m && k + v.z >= 0 && k + v.z < n)
                                {
                                    set &= disField[i + v.x, j + v.y, k + v.z] <= lut[vIdx, (int)disField[i, j, k]];
                                }
                                if (!set)
                                    break;
                            }
                            if (!set)
                                break;
                        }
                        result[i, j, k] = set;
                    }

        }

        float FindMax(ref float[,,] disFiel)
        {
            int l = disFiel.GetLength(0);
            int m = disFiel.GetLength(1);
            int n = disFiel.GetLength(2);

            float max = 0;

            for (int i = 0; i < l; ++i)
                for (int j = 0; j < m; ++j)
                    for (int k = 0; k < n; ++k)
                        if (disFiel[i, j, k] > max)
                            max = disFiel[i, j, k];

            return max;
        }

        void CompLutMask(int lx, int ly, int lz, ref int[,] Lut, int Rknown, int Rtarget)
        {
            int[] Ct = new int[lx * ly * lz];
            int[, ,] dtG = new int[lx, ly, lz];

            CompCTg(lx, ly, lz, ref Ct);

            int l2 = ly * lz;

            for (int k = 0; k < lz; ++k)
                for (int j = 0; j < ly && j <= k; ++j)
                    for (int i = 0; i < lx && i <= j; ++i)
                        dtG[i, j, k] = 0;

            bool[] Possible = new bool[MAXLUTENTRY];
            for (int i = 0; i < MAXLUTENTRY; ++i)
                Possible[i] = false;

            int val;

            for (int i = 0; i < lx; ++i)
                for (int j = 0; j < ly && j <= i; ++j)
                    for (int k = 0; k < lz && k <= j; ++k)
                    {
                        val = Ct[i * l2 + j * lz + k];
                        if (val < MAXLUTENTRY) Possible[val] = true;
                    }

            int size = mask.ng;
            for (int i = 0; i < size; ++i)
                CompLutCol(ref Ct, lx, ly, lz, mask.vg[i], i, Rtarget, ref Lut);
            for (int R = Rknown + 1; R <= Rtarget; R++)
            {
                if (Possible[R])
                {
                    CompDTg_Hirata(lx, ly, lz, ref Ct, ref dtG, R);

                    for (int z = 0; z < lz; z++)
                    {
                        if (dtG[0, 0, z] == 0) break;

                        for (int y = 0; y <= z; y++)
                        {
                            if (dtG[0, y, z] == 0) break;

                            for (int x = 0; x <= y; x++)
                            {
                                if (dtG[x, y, z] == 0) break;

                                if (IsMAg(x, y, z, ref mask, ref Lut, ref dtG, lx, ly, lz) ==1)
                                {
                                    int i = AddWeighting(ref mask, x, y, z, R);
                                    CompLutCol(ref Ct, lx, ly, lz, mask.vg[i], i, Rtarget, ref Lut);
                                }
                            }
                        }
                    }
                }
            }
        }

        int IsMAg(int x, int y, int z, ref MaskG MgL, ref int[,] Lut, ref int [,,] DTg, int lx, int ly, int lz)
        {
            int xx, yy, zz, val, i, L2 = ly * lz;

            val = DTg[x,y,z];

            for (i = 0; i < MgL.ng; i++)
            {
                xx = x - MgL.vg[i].x;
                yy = y - MgL.vg[i].y;
                zz = z - MgL.vg[i].z;

                if (0 <= zz && zz <= yy && yy <= xx)
                {
                    if (DTg[xx,yy,zz] >= Lut[i,val])
                        return 0;
                }
            }

            return 1;
        }

        private int AddWeighting (ref MaskG M, int x, int y, int z, int r)
        {
        int i = M.ng;
        
        M.vg[i].x = x;
        M.vg[i].y = y;
        M.vg[i].z = z;
        M.vg[i].r = r;
        M.ng++;
        return i;
        }



        public void CalculateMedialAxis(ref Voxel[,,] grid, ref float[,,] disField, out bool[,,] result)
        {
            int l = grid.GetLength(0);
            int m = grid.GetLength(1);
            int n = grid.GetLength(2);

            result = new bool[l, m, n];
            bool isEle;
            float sqrtDis;

            for (int i = 0; i < l; ++i)
            {
                for (int j = 0; j < m; ++j)
                {
                    for (int k = 0; k < n; ++k)
                    {
                        isEle = true;

                        for (int x = i != 0 ? -1 : 0; x < (i != (l - 1) ? 2 : 1); ++x)
                            for (int y = j != 0 ? -1 : 0; y < (j != (m - 1) ? 2 : 1); ++y)
                                for (int z = k != 0 ? -1 : 0; z < (k != (n - 1) ? 2 : 1); ++z)
                                {
                                    sqrtDis = (float)Math.Sqrt(disField[i, j, k]) + (float)Math.Sqrt(x * x + y * y + z * z);
                                    isEle = (sqrtDis > Math.Sqrt(disField[i + x, j + y, k + z]) || (x == 0 && y == 0 && z == 0)) ? isEle : false;
                                    //isEle = disField[i, j, k] >= disField[i + x, j + y, k + z] ? isEle : false;
                                }
                        result[i, j, k] = isEle && grid[i, j, k].set;
                    }
                }
            }
        }

        public void CalculateEuclideanSkeleton(ref Voxel[,,] grid, ref float[,,] disDielf, bool[,,] medialAxis, out bool[,,] result)
        {
            int l = grid.GetLength(0);
            int m = grid.GetLength(1);
            int n = grid.GetLength(2);

            SortedList<TestStruct, Vec3> Q = new SortedList<TestStruct, Vec3>();
            SortedList<TestStruct, Vec3> R = new SortedList<TestStruct, Vec3>();

            float distance;
            Vec3 tmp, tmp2 = new Vec3();
            result = new bool[l, m, n];
            for (int i = 0; i < l; ++i)
                for (int j = 0; j < m; ++j)
                    for (int k = 0; k < n; ++k)
                    {
                        result[i, j, k] = grid[i, j, k].set;
                        if (result[i, j, k] && !medialAxis[i, j, k])
                        {
                            tmp.x = i;
                            tmp.y = j;
                            tmp.z = k;

                            Q.Add(new TestStruct(disDielf[i, j, k], tmp), tmp);
                            distance = CheckAdjacent(ref medialAxis, ref disDielf, tmp, ref tmp2);
                            if (distance != float.MaxValue)
                                R.Add(new TestStruct(distance, tmp), tmp);
                        }
                    }
            while(Q.Count > 0 || R.Count > 0)
            {
                if (Q.Count > 0 && R.Count > 0)
                {
                    TestStruct tmpKVPQ = Q.Keys[0];
                    TestStruct tmpKVPR = R.Keys[0];


                    if (tmpKVPQ.value < tmpKVPR.value)
                    {
                        tmp = Q.Values[0];
                        Q.RemoveAt(0);
                    }
                    else
                    {
                        tmp = R.Values[0];
                        if (Q.ContainsKey(tmpKVPR))
                        {
                            Q.Remove(tmpKVPR);
                        }
                        R.RemoveAt(0);
                    }
                }
                else if (Q.Count > 0)
                {
                    tmp = Q.Values[0];
                    Q.RemoveAt(0);
                }
                else
                {
                    tmp = R.Values[0];
                    R.RemoveAt(0);
                }

                if(result[(int)tmp.x, (int)tmp.y, (int)tmp.z] && !medialAxis[(int)tmp.x, (int)tmp.y, (int)tmp.z])
                {
                    int setCom, notSetCom;
                    T_Test(ref result, tmp, out setCom, out notSetCom);
                    if (setCom == 1 && notSetCom == 1)
                        result[(int)tmp.x, (int)tmp.y, (int)tmp.z] = false;
                    else
                    {
                        medialAxis[(int)tmp.x, (int)tmp.y, (int)tmp.z] = true;
                        for (int i = (int)tmp.x - 1 >= 0 ? (int)tmp.x - 1 : 0; i <= ((int)tmp.x + 1 < l ? tmp.x + 1 : l - 1); ++i)
                            for (int j = (int)tmp.y - 1 >= 0 ? (int)tmp.y - 1 : 0; j <= ((int)tmp.y + 1 < l ? tmp.y + 1 : m - 1); ++j)
                                for (int k = (int)tmp.z - 1 >= 0 ? (int)tmp.z - 1 : 0; k <= ((int)tmp.z + 1 < l ? tmp.z + 1 : n - 1); ++k)
                                {
                                    if(result[i,j,k] && !medialAxis[i,j,k] && !R.ContainsValue(new Vec3(i, j, k)))
                                    {
                                        R.Add(new TestStruct(disDielf[(int)tmp.x, (int)tmp.y, (int)tmp.z] + (disDielf[i, j, k] - disDielf[(int)tmp.x, (int)tmp.y, (int)tmp.z]) / ((i - tmp.x) * (i - tmp.x) + (j - tmp.y) * (j - tmp.y) + (k - tmp.z) * (k - tmp.z)),new Vec3(i,j, k)), new Vec3(i, j, k));
                                    }
                                }
                    }
                }
            }
        }

        public void CompleteHCAPlus(ref Voxel[,,] grid, out bool [,,] newPoints)
        {
            float[,,] invDistMap;
            CalculateEuclideanDistanceTransform(ref grid, out invDistMap, false);

            int l = grid.GetLength(0);
            int m = grid.GetLength(1);
            int n = grid.GetLength(2);

            bool[,,] newGrid = new bool[l, m, n];

            for (int i = 0; i < l; ++i)
                for (int j = 0; j < m; ++j)
                    for (int k = 0; k < n; ++k)
                        newGrid[i, j, k] = grid[i, j, k].set;
            HCA(ref invDistMap, ref newGrid, out newPoints);

        }

        public void HCA(ref float[,,] disDielf, ref bool [,,] grid, out bool[,,] hull)
        {
            int l = grid.GetLength(0);
            int m = grid.GetLength(1);
            int n = grid.GetLength(2);

            hull = new bool[l, m, n];
            bool[,,] tmp = new bool[l, m, n];

            SortedList<TestStruct, Vec3> boundary = new SortedList<TestStruct, Vec3>();

            for (int i = 0; i < l; ++i)
                for(int j = 0; j < m; ++j)
                    for(int k = 0; k < n; ++k)
                    {
                        hull[i, j, k] = true;
                        if ((i <= 1 || j <= 1 || k <= 0 || i >= l - 2 || j >= m - 2 || k >= m - 2))
                        {
                            boundary.Add(new TestStruct(disDielf[i, j, k],new Vec3(i,j,k)), new Vec3(i, j, k));
                            tmp[i, j, k] = true;
                        }
                    }
            while (boundary.Count > 0)
            {
                float x = boundary.Keys[boundary.Count - 1].value;
                Vec3 point = boundary.Values[boundary.Count - 1];
                boundary.RemoveAt(boundary.Count - 1);
                int setCom, notSetCom;
                tmp[(int)point.x, (int)point.y, (int)point.z] = false;
                T_Test(ref hull, point, out setCom, out notSetCom);
                if (notSetCom == 1 && !grid[(int)point.x, (int)point.y, (int) point.z])
                {
                    hull[(int)point.x, (int)point.y, (int)point.z] = false;
                    for (int i = (int)point.x - 1 >= 0 ? (int)point.x - 1 : 0; i <= ((int)point.x + 1 < l ? point.x + 1 : l - 1); ++i)
                        for (int j = (int)point.y - 1 >= 0 ? (int)point.y - 1 : 0; j <= ((int)point.y + 1 < l ? point.y + 1 : m - 1); ++j)
                            for (int k = (int)point.z - 1 >= 0 ? (int)point.z - 1 : 0; k <= ((int)point.z + 1 < l ? point.z + 1 : n - 1); ++k)
                            {
                                if (!tmp[i, j, k] && disDielf[i, j, k] > 0 && hull[i,j,k])
                                {
                                    tmp[i, j, k] = true;
                                    boundary.Add(new TestStruct(disDielf[i, j, k], new Vec3(i,j,k)), new Vec3(i, j, k));
                                }
                            }
                }
            }
        }

        public void T_Test(ref bool[, ,] grid, Vec3 point, out int connectedSet, out int connectedNotSet)
        {
            int l = grid.GetLength(0);
            int m = grid.GetLength(1);
            int n = grid.GetLength(2);

            connectedSet = 0;
            connectedNotSet = 0;

            bool[, ,] test = new bool[3, 3, 3];
            bool[, ,] newField = new bool[3, 3, 3];

            bool done = false;
            Vec3 start = new Vec3(0, 0, 0);
            Vec3 gridPoint;
            List<Vec3> points = new List<Vec3>();
            Vec3 current;
            Vec3 tmp;
            Vec3 tmp2 = new Vec3();
            Vec3 oneVec = new Vec3(1, 1, 1);
            bool onEdge = ((int)point.x - 1) < 0 || ((int)point.y - 1) < 0 || ((int)point.z - 1) < 0 || ((int)point.x + 1) >= l || ((int)point.y + 1) >= m || ((int)point.z + 1) >= n;

            int xMin = ((int)point.x - 1) < 0 ? 0 : (int)point.x - 1;
            int yMin = ((int)point.y - 1) < 0 ? 0 : (int)point.y - 1;
            int zMin = ((int)point.z - 1) < 0 ? 0 : (int)point.z - 1;
            int xMax = ((int)point.x + 1) >= l ? l - 1 : (int)point.x + 1;
            int yMax = ((int)point.y + 1) >= m ? m - 1 : (int)point.y + 1;
            int zMax = ((int)point.z + 1) >= n ? n - 1 : (int)point.z + 1;

            Vec3 leftEdge = point - new Vec3(1, 1, 1);

            for (int i = xMin; i <= xMax; ++i)
                for (int j = yMin; j <= yMax; ++j)
                    for (int k = zMin; k <= zMax; ++k)
                    {
                        newField[(int)(i - leftEdge.x), (int)(j - leftEdge.y), (int)(k - leftEdge.z)] = grid[(int)(i), (int)(j), (int)(k)];
                    }

            while (!done)
            {
                points.Add(start);
                test[(int)start.x, (int)start.y, (int)start.z] = true;
                connectedSet += newField[(int)start.x, (int)start.y, (int)start.z] ? 1 : 0;
                connectedNotSet += newField[(int)start.x, (int)start.y, (int)start.z] ? 0 : 1;
                while (points.Count > 0)
                {
                    current = points[points.Count - 1];
                    points.RemoveAt(points.Count - 1);

                    for (int i = -1; i <= 1; ++i)
                        for (int j = -1; j <= 1; ++j)
                            for (int k = -1; k <= 1; ++k)
                            {
                                tmp2.x = current.x + i;
                                tmp2.y = current.y + j;
                                tmp2.z = current.z + k;
                                if ((tmp2.x - oneVec.x) * (tmp2.x - oneVec.x) > 1 || (tmp2.y - oneVec.y) * (tmp2.y - oneVec.y) > 1 || (tmp2.z - oneVec.z) * (tmp2.z - oneVec.z) > 1)
                                    continue;
                                if (newField[(int)current.x, (int)current.y, (int)current.z] && (i * i + j * j + k * k) > 1)
                                    continue;
                                if (newField[(int)current.x, (int)current.y, (int)current.z] == newField[(int)tmp2.x, (int)tmp2.y, (int)tmp2.z] && !test[(int)tmp2.x, (int)tmp2.y, (int)tmp2.z])
                                {
                                    points.Add(tmp2);
                                    test[(int)(tmp2.x), (int)(tmp2.y), (int)(tmp2.z)] = true;
                                }
                            }
                }


                done = true;
                for (int i = 0; i <= 2; ++i)
                    for (int j = 0; j <= 2; ++j)
                        for (int k = 0; k <= 2; ++k)
                        {
                            tmp = new Vec3(i, j, k);
                            if (!test[i, j, k])
                            {
                                done = false;
                                start = new Vec3(i, j, k);
                            }
                        }

            }
        }

        private float CheckAdjacent(ref bool[,,] grid, ref float[,,] disDielf, Vec3 point, ref Vec3 found)
        {
            int l = grid.GetLength(0);
            int m = grid.GetLength(1);
            int n = grid.GetLength(2);

            float result = float.MaxValue;
            float tmp;
            for (int i = (int)point.x - 1 >= 0 ? (int)point.x - 1 : 0; i <= ((int)point.x + 1 < l ? point.x + 1 : l - 1); ++i)
                for (int j = (int)point.y - 1 >= 0 ? (int)point.y - 1 : 0; j <= ((int)point.y + 1 < l ? point.y + 1 : m - 1); ++j)
                    for (int k = (int)point.z - 1 >= 0 ? (int)point.z - 1 : 0; k <= ((int)point.z + 1 < l ? point.z + 1 : n - 1); ++k)
                        if (grid[i, j, k])
                        {
                            tmp = disDielf[i, j, k] + (disDielf[(int)point.x, (int)point.y, (int)point.z] - disDielf[i, j, k]) / ((i - point.x) * (i - point.x) + (j - point.y) * (j - point.y) + (k - point.z) * (k - point.z));
                            if(tmp < result)
                            {
                                result = tmp;
                                found = new Vec3(i,j,k);
                            }
                        }
            return result;
        }
    }
}
