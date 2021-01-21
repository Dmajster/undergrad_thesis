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
        public int ReductionBitIndex;

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

                var newPackedUniformVolume = ReduceCpu(PackedUniformVolume);

                var mesh = VoxelizationVisualizer.CreateDebugMesh(newPackedUniformVolume);

                var visualizerGameObject = new GameObject("New Voxelization Visualizer");
                visualizerGameObject.AddComponent<MeshFilter>().mesh = mesh;
                visualizerGameObject.AddComponent<MeshRenderer>().sharedMaterial = VisualizerMaterial;
                //Reduce = false;
                //FindObjectOfType<SvdagManager>().Execute(PackedUniformVolume);
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
                for (var z = 0; z < volumeDimensions.z; z++)
                {
                    for (var x = 0; x < volumeDimensions.x; x++)
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

        private PackedUniformVolume ReduceCpu(PackedUniformVolume packedUniformVolume)
        {
            var inputPackedData = packedUniformVolume.Data;
            var inputVolumeDimensions = packedUniformVolume.GetVolumeDimensions();

            var outputPackedData = new uint[inputPackedData.Length / 2];

            for (var outputPackedDataIndex = 0; outputPackedDataIndex < outputPackedData.Length; outputPackedDataIndex++)
            {
                int inputPackedDataIndex0 = outputPackedDataIndex * 2;
                int inputPackedDataIndex1 = outputPackedDataIndex * 2 + 1;

                var sideBitCount = inputVolumeDimensions.x;
                var areaBitCount = inputVolumeDimensions.x * inputVolumeDimensions.z;

                var outputPackedDataValue = 0u;
                for (var bit = 0; bit < 32; bit++)
                {
                    var bitIndex = inputPackedDataIndex0 * 32 + bit;

                    var occupancyPackedValue = (byte)(
                        packedUniformVolume.GetBit(bitIndex) |
                        (packedUniformVolume.GetBit(bitIndex + 1) << 1) |
                        (packedUniformVolume.GetBit(bitIndex + sideBitCount) << 2) |
                        (packedUniformVolume.GetBit(bitIndex + sideBitCount + 1) << 3) |
                        (packedUniformVolume.GetBit(bitIndex + areaBitCount) << 4) |
                        (packedUniformVolume.GetBit(bitIndex + areaBitCount + 1) << 5) |
                        (packedUniformVolume.GetBit(bitIndex + areaBitCount + sideBitCount) << 6) |
                        (packedUniformVolume.GetBit(bitIndex + areaBitCount + sideBitCount + 1) << 7));

                    if (occupancyPackedValue > 0 && occupancyPackedValue < 255)
                    {
                        outputPackedDataValue |= 1u << bit;
                    }
                }

                for (var bit = 0; bit < 32; bit++)
                {
                    var bitIndex = inputPackedDataIndex1 * 32 + bit;

                    var occupancyPackedValue = (byte)(
                        packedUniformVolume.GetBit(bitIndex) |
                        (packedUniformVolume.GetBit(bitIndex + 1) << 1) |
                        (packedUniformVolume.GetBit(bitIndex + sideBitCount) << 2) |
                        (packedUniformVolume.GetBit(bitIndex + sideBitCount + 1) << 3) |
                        (packedUniformVolume.GetBit(bitIndex + areaBitCount) << 4) |
                        (packedUniformVolume.GetBit(bitIndex + areaBitCount + 1) << 5) |
                        (packedUniformVolume.GetBit(bitIndex + areaBitCount + sideBitCount) << 6) |
                        (packedUniformVolume.GetBit(bitIndex + areaBitCount + sideBitCount + 1) << 7));

                    if (occupancyPackedValue > 0 && occupancyPackedValue < 255)
                    {
                        outputPackedDataValue |= 1u << bit << 16;
                    }
                }

                outputPackedData[outputPackedDataIndex] = outputPackedDataValue;
            }

            return new PackedUniformVolume(packedUniformVolume.VoxelWorldScaleInMeters * 2, packedUniformVolume.Depth - 1)
            {
                Data = outputPackedData
            };
        }


        private void OnDrawGizmos()
        {
            for (var packedIndex = 0; packedIndex < PackedUniformVolume.Data.Length / 2; packedIndex+=2)
            {
                Gizmos.color = Color.green;
                uint packedOutputHalfValue0 = Something(packedIndex * 2);

                Gizmos.color = Color.blue;
                uint packedOutputHalfValue1 = Something((packedIndex + 1) * 2);

                uint packedOutputValue = packedOutputHalfValue0 | (packedOutputHalfValue1 << 16);
            }
        }

        private ushort Something(int packedIndex)
        {
            ushort reducedShort = 0;

            for (var reductionBitIndex = 0; reductionBitIndex < 16; reductionBitIndex++)
            {
                var bitIndex = packedIndex * 32 + reductionBitIndex * 2; //THIS

                var byteOctant = PackedUniformVolume.ByteOctant(bitIndex);

                reducedShort |= (ushort)(1u << reductionBitIndex);

                if (byteOctant > 0 && byteOctant < 255)
                {
                    DrawBit(bitIndex);
                }
            }

            return reducedShort;
        }

        private void DrawBit(int bitIndex)
        {
            var voxelScale = new float3(PackedUniformVolume.VoxelWorldScaleInMeters);

            var bitPosition = PackedUniformVolume.GetBitPosition(bitIndex);

            var bitWorldPosition = (float3)transform.position +
                                   new Vector3(bitPosition.x, bitPosition.y, bitPosition.z) * voxelScale;


            Gizmos.DrawWireCube(bitWorldPosition, voxelScale);
        }
    }
}

//var volumeScale = PackedUniformVolume.GetVolumeWorldScale();

//Gizmos.color = Color.white;
//Gizmos.DrawWireCube((float3)transform.position + volumeScale / 2, volumeScale);

//for (var bit = 0; bit < 32; bit++)
//{
//    var bitIndex = PackedIndex * 32 + bit;

//    Gizmos.color = Color.blue;
//    DrawBit(bitIndex);
//}

//var testBitIndex = PackedIndex * 32 + ReductionBitIndex * 2;
//var volumeDimensions = PackedUniformVolume.GetVolumeDimensions();

//var sideBitCount = volumeDimensions.x;
//var areaBitCount = volumeDimensions.x * volumeDimensions.z;

//var testedBitIndices = new[]
//{
//    testBitIndex,
//    testBitIndex + 1,
//    testBitIndex + sideBitCount,
//    testBitIndex + sideBitCount + 1,
//    testBitIndex + areaBitCount,
//    testBitIndex + areaBitCount + 1,
//    testBitIndex + areaBitCount + sideBitCount,
//    testBitIndex + areaBitCount + sideBitCount + 1
//};

//for (var i = 0; i < testedBitIndices.Length; i++)
//{
//    Gizmos.color = Color.red;

//    var testedBitIndex = testedBitIndices[i];

//    if (PackedUniformVolume.GetBit(testedBitIndex))
//    {
//        DrawBit(testedBitIndex);
//    }
//}



//var outputPackedData = new uint[PackedUniformVolume.Data.Length / 2];

////for (var outputPackedDataIndex = 0; outputPackedDataIndex < 1; outputPackedDataIndex++)
////{
//int inputPackedDataIndex0 = PackedIndex * 2;

//var inputVolumeDimensions = PackedUniformVolume.GetVolumeDimensions();
//var sideBitCount = inputVolumeDimensions.x;
//var areaBitCount = inputVolumeDimensions.x * inputVolumeDimensions.z;

//var outputPackedDataValue = 0u;
////for (var bit = 0; bit < 32; bit++)
////{
////var bitIndex = inputPackedDataIndex0 * 32 + bit;
//var bitIndex = inputPackedDataIndex0 * 32 + ReductionBitIndex;

//var bitIndices = new[]
//{
//    bitIndex,
//    bitIndex + 1,
//    bitIndex + sideBitCount,
//    bitIndex + sideBitCount + 1,
//    bitIndex + areaBitCount,
//    bitIndex + areaBitCount + 1,
//    bitIndex + areaBitCount + sideBitCount,
//    bitIndex + areaBitCount + sideBitCount + 1
//};
//foreach (var index in bitIndices)
//{
//    DrawBit(index);
//}
////}

////outputPackedData[outputPackedDataIndex] = outputPackedDataValue;
////}