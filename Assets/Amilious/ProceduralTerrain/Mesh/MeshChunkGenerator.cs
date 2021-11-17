using UnityEngine;
using System.Threading;
using Amilious.ProceduralTerrain.Mesh.Enums;
using Amilious.ProceduralTerrain.Noise;

namespace Amilious.ProceduralTerrain.Mesh {
    
    /// <summary>
    /// This class is used to generate a chunk.
    /// </summary>
    public static class MeshChunkGenerator {

        /// <summary>
        /// This method is used to generate the mesh values.
        /// </summary>
        /// <param name="heightMap">The height map used to generate the mesh.</param>
        /// <param name="meshSettings">The mesh settings.</param>
        /// <param name="levelOfDetail">The level of detail.</param>
        /// <param name="chunkMesh">The mesh that will be used.</param>
        /// <param name="token">A cancellation token that can be checked to see if the operation
        /// has been canceled.</param>
        /// <param name="applyHeight">True if the height value should be applied to the mesh.</param>
    public static void Generate(NoiseMap heightMap, MeshSettings meshSettings, LevelsOfDetail levelOfDetail,
        ChunkMesh chunkMesh, CancellationToken token, bool applyHeight = true) {

        var numVertsPerLine = meshSettings.VertsPerLine;
        var triangleIndex=0;
        var outOfMeshTriangleIndex=0;
        var edgeConnectionVertexIndex=0;
        var topLeft = new Vector2 (-1, 1) * meshSettings.MeshWorldSize / 2f;
        chunkMesh??= new ChunkMesh(meshSettings,levelOfDetail);
        var meshVertexIndex = 0;
        var outOfMeshVertexIndex = -1;
 
        for (var y = 0; y < numVertsPerLine; y++) {
            for (var x = 0; x < numVertsPerLine; x++) {
                token.ThrowIfCancellationRequested();
                var isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                var isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && 
                                      ((x - 2) % chunkMesh.SkipStep != 0 || (y - 2) % chunkMesh.SkipStep != 0);
                if (isOutOfMeshVertex) {
                    chunkMesh.verticesMap[x, y] = outOfMeshVertexIndex;
                    outOfMeshVertexIndex--;
                } else if (!isSkippedVertex) {
                    chunkMesh.verticesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }
 
        for (var y = 0; y < numVertsPerLine; y++) {
            for (var x = 0; x < numVertsPerLine; x++) {
                token.ThrowIfCancellationRequested();
                var isSkippedVertex = x > 2 && x < numVertsPerLine - 3 && y > 2 && y < numVertsPerLine - 3 && 
                                      ((x - 2) % chunkMesh.SkipStep != 0 || (y - 2) % chunkMesh.SkipStep != 0);
                if(isSkippedVertex) continue;
                var isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                var isMeshEdgeVertex = (y == 1 || y == numVertsPerLine - 2 || x == 1 || x == numVertsPerLine - 2) && 
                                       !isOutOfMeshVertex;
                var isMainVertex = (x - 2) % chunkMesh.SkipStep == 0 && (y - 2) % chunkMesh.SkipStep == 0 && 
                                   !isOutOfMeshVertex && !isMeshEdgeVertex;
                var isEdgeConnectionVertex = (y == 2 || y == numVertsPerLine - 3 || x == 2 || x == numVertsPerLine - 3) 
                                             && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;
                var vertexIndex = chunkMesh.verticesMap[x, y];
                var percent = new Vector2 (x - 1, y - 1) / (numVertsPerLine - 3);
                var vertexPosition2D = topLeft + new Vector2 (percent.x, -percent.y) * meshSettings.MeshWorldSize;
                var height = applyHeight?heightMap[x, y]:0f;
 
                if (isEdgeConnectionVertex) {
                    var isVertical = x == 2 || x == numVertsPerLine - 3;
                    var dstToMainVertexA = ((isVertical) ? y - 2 : x - 2) % chunkMesh.SkipStep;
                    var dstToMainVertexB = chunkMesh.SkipStep - dstToMainVertexA;
                    var dstPercentFromAToB = dstToMainVertexA / (float)chunkMesh.SkipStep;
 
                    var coordA = new Vector2Int (isVertical ? x : x - dstToMainVertexA, isVertical ? y - dstToMainVertexA : y);
                    var coordB = new Vector2Int (isVertical ? x : x + dstToMainVertexB, isVertical ? y + dstToMainVertexB : y);
 
                    var heightMainVertexA = heightMap [coordA.x,coordA.y];
                    var heightMainVertexB = heightMap [coordB.x,coordB.y];
 
                    if(applyHeight)
                        height = heightMainVertexA * (1 - dstPercentFromAToB) + heightMainVertexB * dstPercentFromAToB;
 
                    var edgeConnectionVertexData = new EdgeConnectionVertexData (vertexIndex, chunkMesh.verticesMap[coordA.x, coordA.y], chunkMesh.verticesMap[coordB.x, coordB.y], dstPercentFromAToB);
                    DeclareEdgeConnectionVertex (edgeConnectionVertexData);
                }
 
                AddVertex (new Vector3 (vertexPosition2D.x, height, vertexPosition2D.y), percent, new Vector2(1,heightMap[x,y]), vertexIndex);
 
                var createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));

                if(!createTriangle) continue;
                var currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3) ? chunkMesh.SkipStep : 1;
 
                var a = chunkMesh.verticesMap[x, y];
                var b = chunkMesh.verticesMap[x + currentIncrement, y];
                var c = chunkMesh.verticesMap[x, y + currentIncrement];
                var d = chunkMesh.verticesMap[x + currentIncrement, y + currentIncrement];
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

}