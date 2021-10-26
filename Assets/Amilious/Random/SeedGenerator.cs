using System;
using System.Security.Cryptography;
using System.Text;

namespace Amilious.Random {
    
    public static class SeedGenerator {

        public static int GetSeedInt(string seed) {
            using var algo = SHA1.Create();
            var hash = BitConverter.ToInt32(algo.ComputeHash(Encoding.UTF8.GetBytes(seed)),0);
            return hash;
        }
        
        public static long GetSeedLong(string seed) {
            using var algo = SHA1.Create();
            var hash = BitConverter.ToInt64(algo.ComputeHash(Encoding.UTF8.GetBytes(seed)),0);
            return hash;
        }
        
    }
    
}