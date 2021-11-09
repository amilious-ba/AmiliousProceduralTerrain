namespace Amilious.Random {
    
    [System.Serializable]
    public struct Seed {

        private string _name;
        private int _value;

        public Seed(int seed) {
            _value = seed;
            _name = seed.ToString();
        }

        public Seed(string seed) {
            _value = SeedGenerator.GetSeedInt(seed);
            _name = seed;
        }

        public override string ToString() => Name;
        public string Name { get => _name; }
        public int Value { get => _value; }
    }
}