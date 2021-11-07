using System.Threading;
using Amilious.ProceduralTerrain.Noise;
using UnityEngine;
namespace Amilious.ProceduralTerrain.Mesh {
    
    /// <summary>
    /// This class is used to generate a chunk.
    /// </summary>
    public static class MeshChunkGenerator {

    public static void Generate(NoiseMap heightMap, MeshSettings meshSettings, LevelsOfDetail levelOfDetail,
        ChunkMesh chunkMesh, CancellationToken token, bool applyHeight = true) {

        var skipStep = (int)levelOfDetail;
        var numVertsPerLine = meshSettings.VertsPerLine;
        var triangleIndex=0;
        var outOfMeshTriangleIndex=0;
        var edgeConnectionVertexIndex=0;
        var topLeft = new Vector2 (-1, 1) * meshSettings.MeshWorldSize / 2f;
        chunkMesh??= new ChunkMesh (meshSettings, skipStep, levelOfDetail);
        var vertexIndicesMap = new int[numVertsPerLine, numVertsPerLine];
        var meshVertexIndex = 0;
        var outOfMeshVertexIndex = -1;
 
        for (var y = 0; y < numVertsPerLine; y++) {
            for (var x = 0; x < numVertsPerLine; x++) {
                token.ThrowIfCancellationRequested();
                var isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                var isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && 
                                      ((x - 2) % skipStep != 0 || (y - 2) % skipStep != 0);
                if (isOutOfMeshVertex) {
                    vertexIndicesMap [x, y] = outOfMeshVertexIndex;
                    outOfMeshVertexIndex--;
                } else if (!isSkippedVertex) {
                    vertexIndicesMap [x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }
 
        for (var y = 0; y < numVertsPerLine; y++) {
            for (var x = 0; x < numVertsPerLine; x++) {
                token.ThrowIfCancellationRequested();
                var isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && 
                                      ((x - 2) % skipStep != 0 || (y - 2) % skipStep != 0);
                if(isSkippedVertex) continue;
                var isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                var isMeshEdgeVertex = (y == 1 || y == numVertsPerLine - 2 || x == 1 || x == numVertsPerLine - 2) && 
                                       !isOutOfMeshVertex;
                var isMainVertex = (x - 2) % skipStep == 0 && (y - 2) % skipStep == 0 && 
                                   !isOutOfMeshVertex && !isMeshEdgeVertex;
                var isEdgeConnectionVertex = (y == 2 || y == numVertsPerLine - 3 || x == 2 || x == numVertsPerLine - 3) 
                                             && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;
                var vertexIndex = vertexIndicesMap [x, y];
                var percent = new Vector2 (x - 1, y - 1) / (numVertsPerLine - 3);
                var vertexPosition2D = topLeft + new Vector2 (percent.x, -percent.y) * meshSettings.MeshWorldSize;
                var height = applyHeight?heightMap[x, y]:0f;
 
                if (isEdgeConnectionVertex) {
                    var isVertical = x == 2 || x == numVertsPerLine - 3;
                    var dstToMainVertexA = ((isVertical) ? y - 2 : x - 2) % skipStep;
                    var dstToMainVertexB = skipStep - dstToMainVertexA;
                    var dstPercentFromAToB = dstToMainVertexA / (float)skipStep;
 
                    var coordA = new Vector2Int (isVertical ? x : x - dstToMainVertexA, isVertical ? y - dstToMainVertexA : y);
                    var coordB = new Vector2Int (isVertical ? x : x + dstToMainVertexB, isVertical ? y + dstToMainVertexB : y);
 
                    var heightMainVertexA = heightMap [coordA.x,coordA.y];
                    var heightMainVertexB = heightMap [coordB.x,coordB.y];
 
                    if(applyHeight)
                        height = heightMainVertexA * (1 - dstPercentFromAToB) + heightMainVertexB * dstPercentFromAToB;
 
                    var edgeConnectionVertexData = new EdgeConnectionVertexData (vertexIndex, vertexIndicesMap [coordA.x, coordA.y], vertexIndicesMap [coordB.x, coordB.y], dstPercentFromAToB);
                    DeclareEdgeConnectionVertex (edgeConnectionVertexData);
                }
 
                AddVertex (new Vector3 (vertexPosition2D.x, height, vertexPosition2D.y), percent, new Vector2(1,heightMap[x,y]), vertexIndex);
 
                var createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));

                if(!createTriangle) continue;
                var currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3) ? skipStep : 1;
 
                var a = vertexIndicesMap [x, y];
                var b = vertexIndicesMap [x + currentIncrement, y];
                var c = vertexIndicesMap [x, y + currentIncrement];
                var d = vertexIndicesMap [x + currentIncrement, y + currentIncrement];
                AddTriangle (a, d, c);
                AddTriangle (d, a, b);
            }
            
            void AddVertex(Vector3 vertexPosition, Vector2 uv, Vector2 uv2, int vertexIndex) {
                if (vertexIndex < 0) {
                    chunkMesh.outOfMeshVertices [-vertexIndex - 1] = vertexPosition;
                } else {
                    chunkMesh.vertices [vertexIndex] = vertexPosition;
                    chunkMesh.uvs [vertexIndex] = uv;
                    chunkMesh.uvs2[vertexIndex] = uv2;
                }
            }

            void DeclareEdgeConnectionVertex(EdgeConnectionVertexData vertexData) {
                chunkMesh.edgeConnectionVertices[edgeConnectionVertexIndex++] = vertexData;
            }

            void AddTriangle(int a, int b, int c) {
                if (a < 0 || b < 0 || c < 0) {
                    chunkMesh.outOfMeshTriangles [outOfMeshTriangleIndex] = a;
                    chunkMesh.outOfMeshTriangles [outOfMeshTriangleIndex + 1] = b;
                    chunkMesh.outOfMeshTriangles [outOfMeshTriangleIndex + 2] = c;
                    outOfMeshTriangleIndex += 3;
                } else {
                    chunkMesh.triangles [triangleIndex] = a;
                    chunkMesh.triangles [triangleIndex + 1] = b;
                    chunkMesh.triangles [triangleIndex + 2] = c;
                    triangleIndex += 3;
                }
            }
            
        }
 
        chunkMesh.ProcessMesh ();

        }
        
    }
    
    public readonly struct EdgeConnectionVertexData {
        public readonly int vertexIndex;
        public readonly int mainVertexAIndex;
        public readonly int mainVertexBIndex;
        public readonly float dstPercentFromAToB;
 
        public EdgeConnectionVertexData (int vertexIndex, int mainVertexAIndex, int mainVertexBIndex, float dstPercentFromAToB) {
            this.vertexIndex = vertexIndex;
            this.mainVertexAIndex = mainVertexAIndex;
            this.mainVertexBIndex = mainVertexBIndex;
            this.dstPercentFromAToB = dstPercentFromAToB;
        }

    }

}