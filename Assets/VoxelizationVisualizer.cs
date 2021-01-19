using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Assets
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    class VoxelizationVisualizer : MonoBehaviour
    {
        public static Mesh CreateDebugMesh(PackedUniformVolume packedUniformVolume)
        {
            int[] GetQuadIndicesArray(int i0, int i1, int i2, int i3)
            {
                return new[]{
                        i0,
                        i1,
                        i2,
                        i2,
                        i3,
                        i0,
                    };
            }

            var vertices = new List<Vector3>();
            var indices = new List<int>();

            var voxelHalfScale = packedUniformVolume.GetVolumeWorldScale() / packedUniformVolume.GetSideElementCount() / 2.0f;

            var index = 0;
            var volumeDimensions = packedUniformVolume.GetVolumeDimensions();

            for (var y = 0; y < volumeDimensions.y; y++)
            {
                for (var x = 0; x < volumeDimensions.x; x++)
                {
                    for (var z = 0; z < volumeDimensions.z; z++)
                    {
                        var position = new float3(x, y, z);
                        var worldPosition = position * packedUniformVolume.WorldScaleInMeters;

                        var packedIndex = index / 32;
                        var bitIndex = index % 32;
                        var isOccupied = ((1 << bitIndex) & packedUniformVolume.Data[packedIndex]) > 0;

                        if (isOccupied)
                        {
                            vertices.AddRange(new Vector3[] {
                                worldPosition + new float3(-voxelHalfScale.x, -voxelHalfScale.y, -voxelHalfScale.z),
                                worldPosition + new float3(-voxelHalfScale.x, -voxelHalfScale.y, +voxelHalfScale.z),
                                worldPosition + new float3(+voxelHalfScale.x, -voxelHalfScale.y, +voxelHalfScale.z),
                                worldPosition + new float3(+voxelHalfScale.x, -voxelHalfScale.y, -voxelHalfScale.z),

                                worldPosition + new float3(-voxelHalfScale.x, +voxelHalfScale.y, -voxelHalfScale.z),
                                worldPosition + new float3(-voxelHalfScale.x, +voxelHalfScale.y, +voxelHalfScale.z),
                                worldPosition + new float3(+voxelHalfScale.x, +voxelHalfScale.y, +voxelHalfScale.z),
                                worldPosition + new float3(+voxelHalfScale.x, +voxelHalfScale.y, -voxelHalfScale.z),
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

            var mesh = new Mesh
            {
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = vertices.ToArray(),
                triangles = indices.ToArray()
            };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
