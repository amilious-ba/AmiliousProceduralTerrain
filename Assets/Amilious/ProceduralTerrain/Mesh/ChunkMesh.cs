using System;
using UnityEngine;
using Amilious.Threading;
using Amilious.ProceduralTerrain.Noise;

//TODO: bake flat shaded normals
//TODO: smooth the seam between chunks of smaller lods

namespace Amilious.ProceduralTerrain.Mesh {
    public class ChunkMesh {

        private UnityEngine.Mesh _mesh;
        public event Action UpdateCallback;
        
        public Vector3[] vertices;
        public readonly int[] triangles;
        public Vector2[] uvs;
        public readonly Vector2[] uvs2;
        public Vector3[] bakedNormals;
        public readonly Vector3[] outOfMeshVertices;
        public readonly int[] outOfMeshTriangles;
        private readonly Vector3[] _flatShadedVertices;
        private readonly Vector2[] _flatShadedUvs;
        private readonly Vector2[] _flatShadedUvs2;
     
        public readonly EdgeConnectionVertexData[] edgeConnectionVertices;
        
        public bool UseFlatShading { get; }
        public int LevelOfDetail { get; }
        
        public bool HasRequestedMesh { get; private set; }
        public bool HasMesh { get; private set; }

        public void Reset() {
            HasRequestedMesh = false;
            HasMesh = false;
        }

        public void RequestMesh(NoiseMap heightMap, MeshSettings meshSettings, bool applyHeight = true) {
            HasRequestedMesh = true;
            var future = new Future<ChunkMesh>();
            future.OnSuccess(meshData=> {
                //if the mesh does not exist we need to create it.
                _mesh ??= new UnityEngine.Mesh();
                //apply the changes to the mesh
                ApplyChanges();
                HasMesh = true;
                UpdateCallback?.Invoke();
            });
            future.OnError(meshData => {
                Debug.LogError(meshData.error);
            });
            future.Process(()=> MeshChunkGenerator.Generate(heightMap, meshSettings, 
                LevelOfDetail, this, applyHeight));
        }

        public ChunkMesh(int numVertsPerLine, int skipIncrement, bool useFlatShading, int levelOfDetail) {
            UseFlatShading = useFlatShading;
            LevelOfDetail = levelOfDetail;
            var numMeshEdgeVertices = (numVertsPerLine - 2) * 4 - 4;
            var numEdgeConnectionVertices = (skipIncrement - 1) * (numVertsPerLine - 5) / skipIncrement * 4;
            var numMainVerticesPerLine = (numVertsPerLine - 5) / skipIncrement + 1;
            var numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;
            vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices];
            uvs = new Vector2[vertices.Length];
            uvs2 = new Vector2[vertices.Length];
            edgeConnectionVertices = new EdgeConnectionVertexData[numEdgeConnectionVertices];
            var numMeshEdgeTriangles = 8 * (numVertsPerLine - 4);
            var numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;
            triangles = new int[(numMeshEdgeTriangles + numMainTriangles) * 3];
            outOfMeshVertices = new Vector3[numVertsPerLine * 4 - 4];
            outOfMeshTriangles = new int[24 * (numVertsPerLine-2)];
            if(!UseFlatShading) return;
            _flatShadedVertices = new Vector3[triangles.Length];
            _flatShadedUvs = new Vector2[triangles.Length];
            _flatShadedUvs2 = new Vector2[triangles.Length];
        }
        
        public bool AssignTo(MeshFilter meshFilter) {
            if(InvalidMesh) return false;
            meshFilter.sharedMesh = _mesh;
            return true;
        }

        public bool AssignTo(MeshCollider meshCollider) {
            if(InvalidMesh) return false;
            meshCollider.sharedMesh = _mesh;
            return true;
        }
        
        public bool ApplyChanges(bool recalculateBounds=false) {
            if(InvalidMesh  || NotMainThread) return false;
            _mesh.vertices = UseFlatShading?_flatShadedVertices:vertices;
            _mesh.triangles = triangles;
            _mesh.uv = UseFlatShading?_flatShadedUvs:uvs;
            _mesh.uv2 = UseFlatShading?_flatShadedUvs2:uvs2;
            if(UseFlatShading) _mesh.RecalculateNormals();
            else _mesh.normals = bakedNormals;
            if(recalculateBounds) _mesh.RecalculateBounds();
            return true;
        }

        private bool InvalidMesh {
            get {
                if(_mesh != null) return false;
                Debug.LogWarning("Trying to access the ChunkMeshes mesh before it has been created.");
                return true;
            }
        }

        private static bool NotMainThread {
            get {
                if(Dispatcher.IsMainThread) return false;
                Debug.LogWarning("Trying to apply changes to the ChunkMeshes mesh outside main thread.");
                return true;
            }
        }
        
        private Vector3[] CalculateNormals() {
     
            var vertexNormals = new Vector3[vertices.Length];
            var triangleCount = triangles.Length / 3;
            for (var i = 0; i < triangleCount; i++) {
                var normalTriangleIndex = i * 3;
                var vertexIndexA = triangles [normalTriangleIndex++];
                var vertexIndexB = triangles [normalTriangleIndex++];
                var vertexIndexC = triangles [normalTriangleIndex];
                var triangleNormal = SurfaceNormalFromIndices (vertexIndexA, vertexIndexB, vertexIndexC);
                vertexNormals [vertexIndexA] += triangleNormal;
                vertexNormals [vertexIndexB] += triangleNormal;
                vertexNormals [vertexIndexC] += triangleNormal;
            }
     
            var borderTriangleCount = outOfMeshTriangles.Length / 3;
            for (var i = 0; i < borderTriangleCount; i++) {
                var normalTriangleIndex = i * 3;
                var vertexIndexA = outOfMeshTriangles [normalTriangleIndex++];
                var vertexIndexB = outOfMeshTriangles [normalTriangleIndex++];
                var vertexIndexC = outOfMeshTriangles [normalTriangleIndex];
                var triangleNormal = SurfaceNormalFromIndices (vertexIndexA, vertexIndexB, vertexIndexC);
                if (vertexIndexA >= 0) vertexNormals [vertexIndexA] += triangleNormal;
                if (vertexIndexB >= 0) vertexNormals [vertexIndexB] += triangleNormal;
                if (vertexIndexC >= 0) vertexNormals [vertexIndexC] += triangleNormal;
            }

            for (var i = 0; i < vertexNormals.Length; i++)
                vertexNormals [i].Normalize ();
            return vertexNormals;
     
        }

        private void ProcessEdgeConnectionVertices() {
            foreach (var e in edgeConnectionVertices) {
                bakedNormals [e.vertexIndex] = bakedNormals [e.mainVertexAIndex] 
                    * (1 - e.dstPercentFromAToB) + bakedNormals [e.mainVertexBIndex] * e.dstPercentFromAToB;
            }
        }

        private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
            var pointA = indexA < 0?outOfMeshVertices[-indexA-1] : vertices [indexA];
            var pointB = indexB < 0?outOfMeshVertices[-indexB-1] : vertices [indexB];
            var pointC = indexC < 0?outOfMeshVertices[-indexC-1] : vertices [indexC];
            var sideAB = pointB - pointA;
            var sideAC = pointC - pointA;
            return Vector3.Cross (sideAB, sideAC).normalized;
        }
     
        public void ProcessMesh() {
            if (UseFlatShading) {
                FlatShading ();
            } else {
                BakeNormals ();
                ProcessEdgeConnectionVertices();
            }
        }

        private void BakeNormals() {
            bakedNormals = CalculateNormals ();
        }

        private void FlatShading() {
            var flatShadedVertices = new Vector3[triangles.Length];
            var flatShadedUvs = new Vector2[triangles.Length];
     
            for (var i = 0; i < triangles.Length; i++) {
                flatShadedVertices [i] = vertices [triangles [i]];
                flatShadedUvs [i] = uvs [triangles [i]];
                triangles [i] = i;
            }
     
            vertices = flatShadedVertices;
            uvs = flatShadedUvs;
        }
     
        /*public UnityEngine.Mesh CreateMesh() {
            _mesh = new UnityEngine.Mesh {
                vertices = vertices,
                triangles = triangles,
                uv = uvs
            };
            if (UseFlatShading) {
                _mesh.RecalculateNormals ();
            } else {
                _mesh.uv2 = uvs2;
                _mesh.normals = bakedNormals;
            }
            return _mesh;
        }*/

    }
}