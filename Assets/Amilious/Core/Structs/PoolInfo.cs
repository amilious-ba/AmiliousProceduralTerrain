namespace Amilious.Core.Structs {
    public readonly struct PoolInfo {

        public int Size { get; }
        public int CheckedOut { get; }
        public int Available { get; }

        public PoolInfo(int poolSize, int available, int checkedOut) {
            Size = poolSize;
            Available = available;
            CheckedOut = checkedOut;
        }

        public static PoolInfo FromCheckedOutAndAvailable(int checkedOut, int available) {
            return new PoolInfo(available + checkedOut, available, checkedOut);
        }

        public static PoolInfo FromSizeAndCheckedOut(int size, int checkedOut) {
            return new PoolInfo(size, checkedOut, size - checkedOut);
        }

        public static PoolInfo FromSizeAndAvailable(int size, int available) {
            return new PoolInfo(size, size - available, available);
        }
        
    }
}