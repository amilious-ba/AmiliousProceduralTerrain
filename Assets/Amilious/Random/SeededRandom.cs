using System;

namespace Amilious.Random {

    public class SeededRandom {

        private System.Random random;

        public string Seed { get; }

        public int SeedValue { get; }


        public SeededRandom(string seed = "seedless") {
            Seed = seed;
            SeedValue = SeedGenerator.GetSeedInt(seed);
            random = new System.Random(SeedValue);
        }

        public float NextFloat01() {
            return (float)random.NextDouble();
        }

        public float Range(float min, float max) {
            var val = random.NextDouble() * (min - max) + min;
            return (float)val;
        }

        public int IntRange(int min, int max) {
            return random.Next(min, max);
        }
        
        static float NextFloat(System.Random random) {
            double mantissa = (random.NextDouble() * 2.0) - 1.0;
            // choose -149 instead of -126 to also generate subnormal floats (*)
            double exponent = Math.Pow(2.0, random.Next(-126, 128));
            return (float)(mantissa * exponent);
        }
        
        
    }
}