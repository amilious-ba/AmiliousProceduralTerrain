using System;
using System.Security.Cryptography;
using System.Text;

namespace Amilious.Random {
    
    [Serializable]
    public struct Seed {

        private string _name;
        private int _value;
        private long _longValue;

        public Seed(int seed) {
            _value = seed;
            _name = seed.ToString();
            _longValue = GetSeedLong(_name);
        }

        public Seed(string seed) {
            _value = GetSeedInt(seed);
            _longValue = GetSeedLong(seed);
            _name = seed;
        }

        public override string ToString() => Name;
        public string Name { get => _name; }
        public int Value { get => _value; }
        
        public long LongValue { get => _longValue; }
        
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