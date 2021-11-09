using System;

namespace Amilious.Random {

    public class SeededRandom {

        private readonly System.Random _random;
        
        public Seed Seed { get; }


        public SeededRandom(string seed = "seedless") {
            Seed = new Seed(seed);
            _random = new System.Random(Seed.Value);
        }

        public float NextFloat01() {
            return (float)_random.NextDouble();
        }

        public float Range(float min, float max) {
            var val = _random.NextDouble() * (min - max) + min;
            return (float)val;
        }

        public int IntRange(int min, int max) {
            return _random.Next(min, max);
        }
        
        static float NextFloat(System.Random random) {
            double mantissa = (random.NextDouble() * 2.0) - 1.0;
            // choose -149 instead of -126 to also generate subnormal floats (*)
            double exponent = Math.Pow(2.0, random.Next(-126, 128));
            return (float)(mantissa * exponent);
        }
        
        
    }
}