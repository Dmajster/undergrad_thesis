using System.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Assets.Code
{
    public class Voxelizer : MonoBehaviour
    {
        public bool RecreateVoxels;
        public bool RecreateMesh;
        public bool Reduce;

        public PackedUniformVolume PackedUniformVolume;
        public Material VisualizerMaterial;

        public int PackedIndex;
        public int BitIndex;

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

                var mesh = VoxelizationVisualizer.CreateDebugMesh(PackedUniformVolume);

                var visualizerGameObject = new GameObject("Voxelization Visualizer");
                visualizerGameObject.AddComponent<MeshFilter>().mesh = mesh;
                visualizerGameObject.AddComponent<MeshRenderer>().sharedMaterial = VisualizerMaterial;
            }

            if (Reduce)
            {
                Reduce = false;
                FindObjectOfType<SvdagManager>().Execute(PackedUniformVolume);
            }
        }

        private void CreateVoxelData()
        {
            PackedUniformVolume =
                new PackedUniformVolume(PackedUniformVolume.VoxelWorldScaleInMeters, PackedUniformVolume.Depth);

            var voxelHalfScale = PackedUniformVolume.GetVolumeWorldScale() / PackedUniformVolume.GetSideElementCount() / 2.0f;

            var index = 0;
            var volumeDimensions = PackedUniformVolume.GetVolumeDimensions();

            for (var y = 0; y < volumeDimensions.y; y++)
            {
                for (var x = 0; x < volumeDimensions.x; x++)
                {
                    for (var z = 0; z < volumeDimensions.z; z++)
                    {
                        var position = new float3(x, y, z);
                        var worldPosition = (float3)transform.position + position * PackedUniformVolume.VoxelWorldScaleInMeters;

                        if (Physics.OverlapBox(worldPosition, voxelHalfScale).Length > 0)
                        {
                            var packedIndex = index / 32;
                            var bitIndex = index % 32;

                            PackedUniformVolume.Data[packedIndex] |= 1u << bitIndex;
                        }

                        index++;
                    }
                }
            }

            Debug.Log($"Volume dimensions: {PackedUniformVolume.GetVolumeDimensions()}");
        }

        private void OnDrawGizmos()
        {
            var volumeScale = PackedUniformVolume.GetVolumeWorldScale();


            Gizmos.color = Color.white;
            Gizmos.DrawWireCube((float3)transform.position + volumeScale / 2, volumeScale);

            for (var bit = 0; bit < 32; bit++)
            {
                var bitIndex = PackedIndex * 32 + bit;

                Gizmos.color = Color.blue;
                DrawBit(bitIndex);
            }

            var testBitIndex = BitIndex * 2;
            var volumeDimensions = PackedUniformVolume.GetVolumeDimensions();

            var sideBitCount = volumeDimensions.x;
            var areaBitCount = volumeDimensions.x * volumeDimensions.z;
            
            var testedBitIndices = new[]
            {
                testBitIndex,
                testBitIndex+1,
                testBitIndex + sideBitCount,
                testBitIndex + sideBitCount + 1,
                testBitIndex + areaBitCount,
                testBitIndex + areaBitCount+1,
                testBitIndex + areaBitCount+ sideBitCount,
                testBitIndex + areaBitCount+ sideBitCount + 1
            };

            for (var i = 0; i < testedBitIndices.Length; i++)
            {
                Gizmos.color = Color.red;
                DrawBit(testedBitIndices[i]);
            }
        }

        private void DrawBit(int bitIndex)
        {
            var voxelScale = new float3(PackedUniformVolume.VoxelWorldScaleInMeters);

            var bitPosition = GetBitPosition(bitIndex, PackedUniformVolume.GetVolumeDimensions());

            var bitWorldPosition = (float3)transform.position + voxelScale / 2 +
                                   new Vector3(bitPosition.x, bitPosition.y, bitPosition.z) * voxelScale;


            Gizmos.DrawWireCube(bitWorldPosition, voxelScale);
        }

        private static int3 GetBitPosition(int index, int3 dimensions)
        {
            return new int3(
                index % dimensions.z,
                index / (dimensions.x * dimensions.z),
                index % (dimensions.x * dimensions.z) / dimensions.z
            );
        }
    }
}


//Gizmos.color = Color.white;
//Gizmos.DrawWireCube(transform.position + volumeScale / 2, volumeScale);

//Gizmos.color = Color.blue;
//var packedScale = new Vector3(32, 1, 1) * PackedUniformVolume.VoxelWorldScaleInMeters;

//var packedDimensions = math.max(new int3(1, 8, 8), PackedUniformVolume.GetVolumeDimensions() / new int3(32, 1, 1));

//var position = transform.position + packedScale / 2;
//var index = 0;
//for (var y = 0; y<packedDimensions.y; y++)
//{
//    for (var x = 0; x<packedDimensions.x; x++)
//    {
//        for (var z = 0; z<packedDimensions.z; z++)
//        {
//            var worldPosition = new Vector3(x * packedScale.x, y * packedScale.y, z * packedScale.z);

//            if (index<PackedUniformVolume.Data.Length)
//            {
//                Gizmos.DrawWireCube(position + worldPosition, packedScale);
//            }

//            index++;
//        }
//    }
//}