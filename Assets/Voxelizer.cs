using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Voxelizer : MonoBehaviour
{
    public float VoxelSideLengthInMeters = 0.1f;

    public bool RecreateVoxels = false;
    public bool RecreateMesh = false;

    public bool DebugDraw = false;
    public Material VisualizerMaterial;

    public int3 _volumeDimensions;
    private bool[] _volume;

    private void Update()
    {
        if (RecreateVoxels)
        {
            RecreateVoxels = false;
            CreateVoxelData();
        }

        if (RecreateMesh)
        {
            RecreateMesh = false;

            var mesh = CreateMesh();

            var gameObject = new GameObject("Voxelization Visualizer");
            gameObject.AddComponent<MeshFilter>().mesh = mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = VisualizerMaterial;
        }
    }

    private void CreateVoxelData()
    {
        var corner = transform.position - transform.localScale / 2;
        var oppositeCorner = transform.position + transform.localScale / 2;

        var min = math.min(corner, oppositeCorner);
        var max = math.max(corner, oppositeCorner);

        if (VoxelSideLengthInMeters == 0)
        {
            return;
        }

        _volumeDimensions = (int3)((max - min) / VoxelSideLengthInMeters);
        _volume = new bool[_volumeDimensions.x * _volumeDimensions.y * _volumeDimensions.z];

        var voxelScale = new float3(VoxelSideLengthInMeters, VoxelSideLengthInMeters, VoxelSideLengthInMeters);
        var originWorldPosition = transform.position - transform.localScale / 2;
        var index = 0;
        for (var y = 0; y < _volumeDimensions.y; y++)
        {
            for (var x = 0; x < _volumeDimensions.x; x++)
            {
                for (var z = 0; z < _volumeDimensions.z; z++)
                {
                    var volume_position = new Vector3(x, y, z);
                    var world_position = originWorldPosition + volume_position * VoxelSideLengthInMeters;

                    if (Physics.OverlapBox(world_position, voxelScale / 2).Length > 0)
                    {
                        _volume[index] = true;
                    }
                    index++;
                }
            }
        }
    }

    private Mesh CreateMesh()
    {
        int[] GetQuadIndicesArray(int i0, int i1, int i2, int i3)
        {
            return new int[]{
                i0,
                i1,
                i2,
                i2,
                i3,
                i0,
            };
        };

        var vertices = new List<Vector3>();
        var indices = new List<int>();

        float3 originWorldPosition = transform.position - transform.localScale / 2;
        var voxelHalfScale = new float3(VoxelSideLengthInMeters, VoxelSideLengthInMeters, VoxelSideLengthInMeters) / 2;

        var index = 0;
        for (var y = 0; y < _volumeDimensions.y; y++)
        {
            for (var x = 0; x < _volumeDimensions.x; x++)
            {
                for (var z = 0; z < _volumeDimensions.z; z++)
                {
                    if (_volume[index])
                    {
                        var volume_position = new float3(x, y, z);
                        var world_position = originWorldPosition + volume_position * VoxelSideLengthInMeters;

                        vertices.AddRange(new Vector3[] {
                            world_position + new float3(-voxelHalfScale.x, -voxelHalfScale.y, -voxelHalfScale.z),
                            world_position + new float3(-voxelHalfScale.x, -voxelHalfScale.y, +voxelHalfScale.z),
                            world_position + new float3(+voxelHalfScale.x, -voxelHalfScale.y, +voxelHalfScale.z),
                            world_position + new float3(+voxelHalfScale.x, -voxelHalfScale.y, -voxelHalfScale.z),

                            world_position + new float3(-voxelHalfScale.x, +voxelHalfScale.y, -voxelHalfScale.z),
                            world_position + new float3(-voxelHalfScale.x, +voxelHalfScale.y, +voxelHalfScale.z),
                            world_position + new float3(+voxelHalfScale.x, +voxelHalfScale.y, +voxelHalfScale.z),
                            world_position + new float3(+voxelHalfScale.x, +voxelHalfScale.y, -voxelHalfScale.z),
                        });
                        var vertexCount = vertices.Count;

                        indices.AddRange(GetQuadIndicesArray(vertexCount - 4, vertexCount - 3, vertexCount - 2, vertexCount - 1));
                        indices.AddRange(GetQuadIndicesArray(vertexCount - 5, vertexCount - 6, vertexCount - 7, vertexCount - 8));

                        indices.AddRange(GetQuadIndicesArray(vertexCount - 5, vertexCount - 1, vertexCount - 2, vertexCount - 6));
                        indices.AddRange(GetQuadIndicesArray(vertexCount - 8, vertexCount - 7, vertexCount - 3, vertexCount - 4));

                        indices.AddRange(GetQuadIndicesArray(vertexCount - 8, vertexCount - 4, vertexCount - 1, vertexCount - 5));
                        indices.AddRange(GetQuadIndicesArray(vertexCount - 7, vertexCount - 6, vertexCount - 2, vertexCount - 3));
                    }
                    index++;
                }
            }
        }

        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, transform.localScale);

        if (!DebugDraw)
        {
            return;
        }

        if (VoxelSideLengthInMeters == 0)
        {
            return;
        }

        var voxelScale = new float3(VoxelSideLengthInMeters, VoxelSideLengthInMeters, VoxelSideLengthInMeters);
        var worldOriginPosition = transform.position - transform.localScale / 2;

        var index = 0;
        for (var y = 0; y < _volumeDimensions.y; y++)
        {
            for (var x = 0; x < _volumeDimensions.x; x++)
            {
                for (var z = 0; z < _volumeDimensions.z; z++)
                {
                    var volumePosition = new Vector3(x, y, z);
                    var worldPosition = worldOriginPosition + volumePosition * VoxelSideLengthInMeters;

                    if (_volume[index])
                    {
                        Gizmos.DrawWireCube(worldPosition, voxelScale);
                    }

                    index++;
                }
            }
        }
    }
}
