using System;

namespace BeMyShotgunSir.Scripts.Utils
{
    public static class TeamIdGenerator
    {
        public static long GetTeamIdBP(int id1, int id2)
        {
            uint min = (uint)Math.Min(id1, id2);
            uint max = (uint)Math.Max(id1, id2);

            return ((long)min << 32) | max;
        }

        public static int GetTeamId(int id1, int id2) => id1;

        public static (int id1, int id2) DeconstructTeamIdBP(long key)
        {
            int id1 = (int)(key >> 32);
            int id2 = (int)(key & 0xFFFFFFFF);
            return (id1, id2);
        }
    }
}
