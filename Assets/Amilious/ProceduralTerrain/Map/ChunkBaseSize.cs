namespace Amilious.ProceduralTerrain.Map {
    
    /// <summary>
    /// This enum is used to represent a chunks base size.
    /// </summary>
    public enum ChunkBaseSize {
        
        /// <summary>
        /// This value indicates that the chunks have a base size of 8 by 8.  A
        /// border will also be added around the outside and there will be an
        /// extra vertex that is needed to make the chunk the correct size. This
        /// means that the total size will be 11 by 11.
        /// </summary>
        Base8X8 = 8,
        
        /// <summary>
        /// This value indicates that the chunks have a base size of 16 by 16.  A
        /// border will also be added around the outside and there will be an
        /// extra vertex that is needed to make the chunk the correct size. This
        /// means that the total size will be 19 by 19.
        /// </summary>
        Base16X16 = 16,
        
        /// <summary>
        /// This value indicates that the chunks have a base size of 32 by 32.  A
        /// border will also be added around the outside and there will be an
        /// extra vertex that is needed to make the chunk the correct size. This
        /// means that the total size will be 35 by 35.
        /// </summary>
        Base32X32 = 32,
        
        /// <summary>
        /// This value indicates that the chunks have a base size of 64 by 64.  A
        /// border will also be added around the outside and there will be an
        /// extra vertex that is needed to make the chunk the correct size. This
        /// means that the total size will be 67 by 67.
        /// </summary>
        Base64X64 = 64
    }
    
}