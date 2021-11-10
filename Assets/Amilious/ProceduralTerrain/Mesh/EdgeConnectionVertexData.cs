namespace Amilious.ProceduralTerrain.Mesh {
    
    /// <summary>
    /// This struct is used for storing edge connection information
    /// for the mesh.
    /// </summary>
    [System.Serializable]
    public struct EdgeConnectionVertexData {
        
        public int vertexIndex;
        public int mainVertexAIndex;
        public int mainVertexBIndex;
        public float dstPercentFromAToB;
 
        public EdgeConnectionVertexData (int vertexIndex, int mainVertexAIndex, int mainVertexBIndex, float dstPercentFromAToB) {
            this.vertexIndex = vertexIndex;
            this.mainVertexAIndex = mainVertexAIndex;
            this.mainVertexBIndex = mainVertexBIndex;
            this.dstPercentFromAToB = dstPercentFromAToB;
        }

    }
}