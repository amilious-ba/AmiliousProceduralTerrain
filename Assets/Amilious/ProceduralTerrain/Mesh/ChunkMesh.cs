using System;
using UnityEngine;
using Amilious.Saving;
using System.Threading;
using Amilious.Threading;
using Amilious.ProceduralTerrain.Noise;

namespace Amilious.ProceduralTerrain.Mesh {
    
    /// <summary>
    /// This class is used to hold a mesh that can pe used for
    /// a chunk in the map.
    /// </summary>
    public class ChunkMesh {

        public const string PREFIX = "MeshLod{0}";
        public const string HAS_DATA = "HasData";

        # region Instance Variables
        public event Action UpdateCallback;
        public readonly Vector3[] vertices;
        public readonly int[] triangles;
        public readonly Vector2[] uvs;
        public readonly Vector2[] uvs2;
        public readonly int[,] verticesMap;
        public Vector3[] bakedNormals;
        public readonly Vector3[] outOfMeshVertices;
        public readonly int[] outOfMeshTriangles;
        public readonly EdgeConnectionVertexData[] edgeConnectionVertices;
        private readonly Vector3[] _flatShadedVertices;
        private readonly Vector2[] _flatShadedUvs;
        private readonly Vector2[] _flatShadedUvs2;

        private UnityEngine.Mesh _mesh;
        private int _meshId;
        private bool _bakedCollisionMesh;
        private readonly MeshSettings _meshSettings;
        private readonly ReusableFuture<bool,NoiseMap,bool> _meshRequester;
        private readonly ReusableFuture<MeshCollider,MeshCollider> _collisionBaker;
        #endregion

        #region Public Properties
        
        /// <summary>
        /// This property is used to check if the mesh is setup for flat shading.
        /// </summary>
        public bool UseFlatShading { get; }
        
        /// <summary>
        /// This property is used to get the meshes level of detail.
        /// </summary>
        public LevelsOfDetail LevelOfDetail { get; }
        
        public int SkipStep { get; }
        
        /// <summary>
        /// This property is used to check if the mesh has been requested yet.
        /// </summary>
        public bool HasRequestedMesh { get; private set; }
        
        public bool HasMeshData { get; private set; }
        
        /// <summary>
        /// This property is used to check if the mesh has been generated.
        /// </summary>
        public bool HasMesh { get; private set; }
        
        #endregion

        #region Private Properties
        
        /// <summary>
        /// This property is used to check if the mesh is invalid.  If the
        /// mesh is invalid a warning will be logged.
        /// </summary>
        private bool InvalidMesh {
            get {
                if(_mesh != null) return false;
                Debug.LogWarning("Trying to access the ChunkMeshes mesh before it has been created.");
                return true;
            }
        }
        
        #endregion

        #region Constructors

        /// <summary>
        /// This constructor is used to create a new chunk mesh.
        /// </summary>
        /// <param name="meshSettings">The <see cref="MeshSettings"/> that will be used for the mesh.</param>
        /// <param name="levelOfDetail">The meshes level of detail.</param>
        public ChunkMesh(MeshSettings meshSettings, LevelsOfDetail levelOfDetail) {
            _meshSettings = meshSettings;
            SkipStep = (int)levelOfDetail;
            //setup the mesh requester
            _meshRequester = new ReusableFuture<bool, NoiseMap, bool>();
            _meshRequester.OnSuccess(MeshReceived).OnProcess(MeshRequest);
            //setup the collision baker
            _collisionBaker = new ReusableFuture<MeshCollider,MeshCollider>();
            _collisionBaker.OnProcess(BakeCollisionMesh).OnSuccess(CollisionMeshBaked);
            UseFlatShading = meshSettings.UseFlatShading;
            LevelOfDetail = levelOfDetail;
            var numVertsPerLine = meshSettings.VertsPerLine;
            var numMeshEdgeVertices = (numVertsPerLine - 2) * 4 - 4;
            var numEdgeConnectionVertices = (SkipStep - 1) * (numVertsPerLine - 5) / SkipStep * 4;
            var numMainVerticesPerLine = (numVertsPerLine - 5) / SkipStep + 1;
            var numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;
            verticesMap = new int[numVertsPerLine, numVertsPerLine];
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

        /// <summary>
        /// This method is called when the collision mesh has completed baking.
        /// </summary>
        /// <param name="meshCollider">The mesh collider that requested the baked collision mesh.</param>
        private void CollisionMeshBaked(MeshCollider meshCollider) {
            _bakedCollisionMesh = true;
            AssignTo(meshCollider);
        }

        /// <summary>
        /// This method is used by the collision baker to make the collision mesh.
        /// </summary>
        /// <param name="meshCollider">The mesh collider that wants the collision mesh.</param>
        /// <param name="token">A cancellation token that can be used to cancel the baking.</param>
        /// <returns>The mesh collider that is requesting the collision mesh.</returns>
        private MeshCollider BakeCollisionMesh(MeshCollider meshCollider, CancellationToken token) {
            if(_bakedCollisionMesh) return meshCollider;
            Physics.BakeMesh(_meshId,false);
            return meshCollider;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// This method is used to reset the mesh.  This clears the loading variables
        /// so that the mesh can be used for a new chunk.
        /// </summary>
        public void Reset() {
            CancelProcessing();
            HasRequestedMesh = false;
            HasMeshData = false;
            HasMesh = false;
            _bakedCollisionMesh = false;
        }

        /// <summary>
        /// This method is used to request the chunks mesh.
        /// </summary>
        /// <param name="heightMap">The height map that you want to use to generate the mesh.</param>
        /// <param name="applyHeight">If true the height map's height values will be applied,
        /// otherwise the heights will be set to zero.</param>
        public void RequestMesh(NoiseMap heightMap, bool applyHeight = true) {
            HasRequestedMesh = true;
            _meshRequester.Process(heightMap,applyHeight);
        }

        public void CancelProcessing() {
            _meshRequester.Cancel();
            _collisionBaker.Cancel();
        }
        
        /// <summary>
        /// This method is used to assign the mesh to a given mesh filter.
        /// </summary>
        /// <param name="meshFilter">The mesh filter that you want to apply the mesh to.</param>
        public void AssignTo(MeshFilter meshFilter) {
            if(InvalidMesh) return;
            meshFilter.sharedMesh = _mesh;
        }

        /// <summary>
        /// This method is used to assign the mesh to a give mesh collider.
        /// </summary>
        /// <param name="meshCollider">The mesh collider that you want to apply the mesh to.</param>
        public void AssignTo(MeshCollider meshCollider) {
            if(InvalidMesh) return;
            if(!_bakedCollisionMesh && _meshSettings.BakeCollisionMeshes) {
                _collisionBaker.Process(meshCollider);
                return;
            }
            meshCollider.sharedMesh = _mesh;
        }
        
        /// <summary>
        /// This method is used to apply any changes that have been made to the mesh.
        /// </summary>
        /// <param name="recalculateBounds"></param>
        /// <returns>True if the method was executed or queued to the dispatcher, otherwise
        /// returns false if the mesh is invalid.</returns>
        public bool ApplyChanges(bool recalculateBounds=false) {
            if(InvalidMesh) return false;
            if(!Dispatcher.IsMainThread) {
                Dispatcher.InvokeAsync(() => { ApplyChanges(recalculateBounds);});
                return true;
            }
            _mesh.vertices = UseFlatShading?_flatShadedVertices:vertices;
            _mesh.triangles = triangles;
            _mesh.uv = UseFlatShading?_flatShadedUvs:uvs;
            _mesh.uv2 = UseFlatShading?_flatShadedUvs2:uvs2;
            if(UseFlatShading) _mesh.RecalculateNormals();
            else _mesh.normals = bakedNormals;
            if(recalculateBounds) _mesh.RecalculateBounds();
            return true;
        }

        /// <summary>
        /// This method is used to process the mesh to be used.  It will apply flat shadding
        /// and bake normals.
        /// </summary>
        public void ProcessMesh() {
            if (UseFlatShading) {
                FlatShading ();
            } else {
                BakeNormals ();
                ProcessEdgeConnectionVertices();
            }
        }

        /// <summary>
        /// This method is used to save the mesh data.
        /// </summary>
        /// <param name="saveData">The save data you want to add the mesh data to.</param>
        public void Save(SaveData saveData) {
            saveData.SetPrefix(PREFIX,LevelOfDetail);
            saveData.SetPrefix(HAS_DATA,HasMesh);
            if(!HasMesh) {
                saveData.ClearPrefix();
                return;
            }
            saveData.StoreData(nameof(vertices),vertices);
            saveData.StoreData(nameof(triangles),triangles);
            saveData.StoreData(nameof(uvs),uvs);
            saveData.StoreData(nameof(uvs2),uvs2);
            saveData.StoreData(nameof(bakedNormals),bakedNormals);
            saveData.StoreData(nameof(outOfMeshVertices), outOfMeshVertices);
            saveData.StoreData(nameof(outOfMeshTriangles), outOfMeshTriangles);
            saveData.StoreData(nameof(edgeConnectionVertices),edgeConnectionVertices);
            if(UseFlatShading) {
                saveData.StoreData(nameof(_flatShadedVertices),_flatShadedVertices);
                saveData.StoreData(nameof(_flatShadedUvs),_flatShadedUvs);
                saveData.StoreData(nameof(_flatShadedUvs2),_flatShadedUvs2);
            }
            saveData.ClearPrefix();
        }

        /// <summary>
        /// This method is used to load the mesh data.
        /// </summary>
        /// <param name="saveData">The save data you want to get the mesh data from.</param>
        /// <returns>True if the mesh data was loaded, otherwise returns false.</returns>
        public bool Load(SaveData saveData) {
            saveData.SetPrefix(PREFIX,LevelOfDetail);
            if(!saveData.FetchData<bool>(HAS_DATA)) {
                saveData.ClearPrefix();
                return false;
            }
            HasRequestedMesh = true;
            saveData.RestoreVector3Array(nameof(vertices),vertices);
            saveData.RestoreSerializableArray(nameof(triangles),triangles);
            saveData.RestoreVector2Array(nameof(uvs),uvs);
            saveData.RestoreVector2Array(nameof(uvs2),uvs2);
            saveData.RestoreVector3Array(nameof(bakedNormals),bakedNormals);
            saveData.RestoreVector3Array(nameof(outOfMeshVertices),outOfMeshVertices);
            saveData.RestoreSerializableArray(nameof(outOfMeshTriangles),outOfMeshTriangles);
            saveData.RestoreSerializableArray(nameof(edgeConnectionVertices),edgeConnectionVertices);
            if(UseFlatShading) {
                saveData.RestoreVector3Array(nameof(_flatShadedVertices),_flatShadedVertices);
                saveData.RestoreVector2Array(nameof(_flatShadedUvs),_flatShadedUvs);
                saveData.RestoreVector2Array(nameof(_flatShadedUvs2),_flatShadedUvs2);
            }
            saveData.ClearPrefix();
            _bakedCollisionMesh = false;
            HasMeshData = true;
            return true;
        }
        
        /// <summary>
        /// This method is used to apply the loaded mesh data.
        /// </summary>
        /// <returns>True if the mesh data was loaded and applied, otherwise
        /// false.</returns>
        public bool ApplyLoadedMesh() {
            if(!HasMeshData || HasMesh) return false;
            //if the mesh does not exist we need to create it.
            _mesh ??= new UnityEngine.Mesh();
            _meshId = _mesh.GetInstanceID();
            //apply the changes to the mesh
            ApplyChanges(true);
            HasMesh = true;
            return true;
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// This method is executed by the mesh requester to generate the mesh.
        /// </summary>
        /// <returns>true</returns>
        private bool MeshRequest(NoiseMap heightMap, bool applyHeight, CancellationToken token) {
            MeshChunkGenerator.Generate(heightMap, _meshSettings, LevelOfDetail, this, token, applyHeight);
            return true;
        }

        /// <summary>
        /// This method is called when the mesh data is received.
        /// </summary>
        /// <param name="success">Contains true if the mesh data was
        /// loaded correctly.</param>
        private void MeshReceived(bool success) {
            HasMeshData = true;
            //if the mesh does not exist we need to create it.
            _mesh ??= new UnityEngine.Mesh();
            _meshId = _mesh.GetInstanceID();
            //apply the changes to the mesh
            ApplyChanges(true);
            HasMesh = true;
            UpdateCallback?.Invoke();
        }
        
        /// <summary>
        /// This method is used to calculate the normals.
        /// </summary>
        /// <returns>Returns the calculated normals.</returns>
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

        /// <summary>
        /// This method is used to align edge vertices and apply the correct normals.
        /// </summary>
        private void ProcessEdgeConnectionVertices() {
            foreach (var e in edgeConnectionVertices) {
                bakedNormals[e.vertexIndex] = bakedNormals [e.mainVertexAIndex] * (1 - e.dstPercentFromAToB) 
                    + bakedNormals [e.mainVertexBIndex] * e.dstPercentFromAToB;
            }
        }
        
        /// <summary>
        /// This method is sued to calculate a surface normal from the given indices.
        /// </summary>
        /// <param name="indexA">The first index.</param>
        /// <param name="indexB">The second index.</param>
        /// <param name="indexC">The third index.</param>
        /// <returns>The calculated surface normal.</returns>
        private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
            var pointA = indexA < 0?outOfMeshVertices[-indexA-1] : vertices [indexA];
            var pointB = indexB < 0?outOfMeshVertices[-indexB-1] : vertices [indexB];
            var pointC = indexC < 0?outOfMeshVertices[-indexC-1] : vertices [indexC];
            var sideAb = pointB - pointA;
            var sideAc = pointC - pointA;
            return Vector3.Cross (sideAb, sideAc).normalized;
        }

        /// <summary>
        /// This method is used to bake the normals.
        /// </summary>
        private void BakeNormals() {
            bakedNormals = CalculateNormals ();
        }

        /// <summary>
        /// This method is used to apply flat shading.
        /// </summary>
        private void FlatShading() {
            var flatShadedVertices = new Vector3[triangles.Length];
            var flatShadedUvs = new Vector2[triangles.Length];
            for (var i = 0; i < triangles.Length; i++) {
                flatShadedVertices [i] = vertices[triangles [i]];
                flatShadedUvs [i] = uvs[triangles [i]];
                _flatShadedUvs2[i] = uvs2[triangles[i]];
                triangles[i] = i;
            }
        }

        #endregion
        
    }
}