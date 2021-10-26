namespace Amilious.Core.Extensions {
    
    /// <summary>
    /// This class is used to add extensions to the Int class.
    /// </summary>
    public static class IntExtension {
        
        /// <summary>
        /// This method is used to clamp an int within a given range.
        /// </summary>
        /// <param name="value">The value that you want to clamp withing the given range.</param>
        /// <param name="min">The inclusive minimum value.</param>
        /// <param name="max">The inclusive maximum value.</param>
        /// <returns>The value clamped within the given range.</returns>
        public static int Clamp(this int value, int min, int max) {
            if(value < min) return min;
            return value > max ? max : value;
        }
        
    }
}