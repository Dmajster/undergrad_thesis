using System.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Assets.Code
{
    public class VoxelizationManager : MonoBehaviour
    {
        public bool RecreateVoxels;
        public bool CreateDebugMesh;
        public bool ReduceOneCpu;
        public bool ReduceOneGpu;
        public bool ReduceGpu;

        public PackedUniformVolume PackedUniformVolume;

        public Material VisualizerMaterial;

        public uint[] SrcPackedVolume;
        public uint3 SrcPackedVolumeBitDimensions;

        public uint[] DstPackedVolume;
        public uint3 DstPackedVolumeBitDimensions;

        private SvdagManager _svdagManager;

        private void Start()
        {
            _svdagManager = FindObjectOfType<SvdagManager>();
        }

        private void Update()
        {
            if (RecreateVoxels)
            {
                RecreateVoxels = false;
                CreateVoxelData();

                var startTime = Time.realtimeSinceStartup;
                CreateVoxelData();
                var endTime = Time.realtimeSinceStartup;

                Debug.Log($"Create voxel data time: {endTime - startTime}s");
            }

            if (CreateDebugMesh)
            {
                CreateDebugMesh = false;

                var mesh = VoxelizationVisualizer.CreateDebugMesh(PackedUniformVolume);

                var visualizerGameObject = new GameObject("Debug mesh");
                visualizerGameObject.AddComponent<MeshFilter>().mesh = mesh;
                visualizerGameObject.AddComponent<MeshRenderer>().sharedMaterial = VisualizerMaterial;
            }

            if (ReduceOneCpu)
            {
                ReduceOneCpu = false;

                SrcPackedVolume = PackedUniformVolume.Data;
                SrcPackedVolumeBitDimensions = (uint3)PackedUniformVolume.GetVolumeBitDimensions();

                DstPackedVolume = new uint[SrcPackedVolume.Length / 2];
                DstPackedVolumeBitDimensions = SrcPackedVolumeBitDimensions / 2;

                Reduce(SrcPackedVolume, SrcPackedVolumeBitDimensions, DstPackedVolume, DstPackedVolumeBitDimensions);

                var srcPackedUniformVolume = new PackedUniformVolume(0.1f, 5)
                {
                    Data = SrcPackedVolume
                };

                var srcMesh = VoxelizationVisualizer.CreateDebugMesh(srcPackedUniformVolume);
                var srcGameObject = new GameObject("Reduce one CPU source debug mesh");
                srcGameObject.AddComponent<MeshFilter>().mesh = srcMesh;
                srcGameObject.AddComponent<MeshRenderer>().sharedMaterial = VisualizerMaterial;

                var dstPackedUniformVolume = new PackedUniformVolume(0.2f, 4)
                {
                    Data = DstPackedVolume
                };

                var dstMesh = VoxelizationVisualizer.CreateDebugMesh(dstPackedUniformVolume);
                var dstGameObject = new GameObject("Reduce one CPU destination debug mesh");
                dstGameObject.AddComponent<MeshFilter>().mesh = dstMesh;
                dstGameObject.AddComponent<MeshRenderer>().sharedMaterial = VisualizerMaterial;
            }

            if (ReduceOneGpu)
            {
                ReduceOneGpu = false;

                var startTime = Time.realtimeSinceStartup;
                var dstPackedUniformVolume = _svdagManager.ReduceOnce(PackedUniformVolume);
                var endTime = Time.realtimeSinceStartup;

                Debug.Log($"Reduce one GPU time: {endTime-startTime}s");

                var dstMesh = VoxelizationVisualizer.CreateDebugMesh(dstPackedUniformVolume);
                var dstGameObject = new GameObject("Reduce one GPU debug mesh");
                dstGameObject.AddComponent<MeshFilter>().mesh = dstMesh;
                dstGameObject.AddComponent<MeshRenderer>().sharedMaterial = VisualizerMaterial;
            }

            if (ReduceGpu)
            {
                ReduceGpu = false;

                _svdagManager.Reduce(PackedUniformVolume);
            }
        }
        private void OnDrawGizmos()
        {
            //Gizmos.color = Color.blue;
            //DrawVolume(SrcPackedVolume, SrcPackedVolumeBitDimensions, 0.1f);

            //Gizmos.color = Color.green;
            //DrawVolume(DstPackedVolume, DstPackedVolumeBitDimensions, 0.2f);
        }

        private void CreateVoxelData()
        {
            PackedUniformVolume =
                new PackedUniformVolume(PackedUniformVolume.VoxelWorldScaleInMeters, PackedUniformVolume.Depth);

            var voxelHalfScale = PackedUniformVolume.GetVolumeWorldScale() / PackedUniformVolume.GetSideBitCount() /
                                 2.0f;

            var index = 0;
            var volumeDimensions = PackedUniformVolume.GetVolumeBitDimensions();

            for (var y = 0; y < volumeDimensions.y; y++)
            {
                for (var z = 0; z < volumeDimensions.z; z++)
                {
                    for (var x = 0; x < volumeDimensions.x; x++)
                    {
                        var position = new float3(x, y, z);
                        var worldPosition = (float3)transform.position +
                                            position * PackedUniformVolume.VoxelWorldScaleInMeters;

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

            Debug.Log($"Volume dimensions: {PackedUniformVolume.GetVolumeBitDimensions()}");
        }

        private void Reduce(uint[] srcPackedVolume, uint3 srcPackedVolumeBitDimensions, uint[] dstPackedVolume, uint3 dstPackedVolumeBitDimensions)
        {
            for (var y = 0u; y < dstPackedVolumeBitDimensions.y; y++)
            {
                for (var z = 0u; z < dstPackedVolumeBitDimensions.z; z++)
                {
                    for (var x = 0u; x < dstPackedVolumeBitDimensions.x; x++)
                    {
                        var dstBitPosition = new uint3(x, y, z);
                        var srcBitPosition = dstBitPosition * 2;

                        var children = (byte)(GetBitFromSrcVolumeAsBoolByte(srcBitPosition) |
                                       (GetBitFromSrcVolumeAsBoolByte(srcBitPosition + new uint3(1, 0, 0)) << 1) |
                                       (GetBitFromSrcVolumeAsBoolByte(srcBitPosition + new uint3(0, 0, 1)) << 4) |
                                       (GetBitFromSrcVolumeAsBoolByte(srcBitPosition + new uint3(1, 0, 1)) << 5) |

                                       (GetBitFromSrcVolumeAsBoolByte(srcBitPosition + new uint3(0, 1, 0)) << 2) |
                                       (GetBitFromSrcVolumeAsBoolByte(srcBitPosition + new uint3(1, 1, 0)) << 3) |
                                       (GetBitFromSrcVolumeAsBoolByte(srcBitPosition + new uint3(0, 1, 1)) << 6) |
                                       (GetBitFromSrcVolumeAsBoolByte(srcBitPosition + new uint3(1, 1, 1)) << 7));

                        SetBitToDstVolumeFromBoolByte(dstBitPosition, children);
                    }
                }
            }

            byte GetBitFromSrcVolumeAsBoolByte(uint3 srcBitPosition)
            {
                var srcBitIndex = BitPositionToBitIndex(srcBitPosition, srcPackedVolumeBitDimensions);

                var packedIndex = srcBitIndex / 32;
                var packedIndexValue = srcPackedVolume[packedIndex];

                var bitConsecutiveIndex = (int)srcBitIndex % 32;
                var bitSet = (packedIndexValue & (1u << bitConsecutiveIndex)) > 0;
                var boolByte = (byte)(bitSet ? 1u : 0u);

                return boolByte;
            }

            void SetBitToDstVolumeFromBoolByte(uint3 dstBitPosition, byte isOccupiedBoolByte)
            {
                var dstBitIndex = BitPositionToBitIndex(dstBitPosition, dstPackedVolumeBitDimensions);

                var packedIndex = dstBitIndex / 32;

                var bitConsecutiveIndex = (int)dstBitIndex % 32;

                if (isOccupiedBoolByte > 0)
                {
                    //ATOMIC ON GPU
                    dstPackedVolume[packedIndex] |= 1u << bitConsecutiveIndex;
                }
            }
        }

        private static uint BitPositionToBitIndex(uint3 bitPosition, uint3 bitVolumeDimensions)
        {
            return bitPosition.y * bitVolumeDimensions.x * bitVolumeDimensions.z +
                   bitPosition.z * bitVolumeDimensions.x + bitPosition.x;
        }

        private static void DrawVolume(uint[] data, uint3 dimensions, float bitSizeInMeters)
        {
            for (var y = 0u; y < dimensions.y; y++)
            {
                for (var z = 0u; z < dimensions.z; z++)
                {
                    for (var x = 0u; x < dimensions.x; x++)
                    {
                        var bitPosition = new uint3(x, y, z);

                        var bitIndex = BitPositionToBitIndex(bitPosition, dimensions);

                        var packedIndex = (int)math.floor( bitIndex / 32.0);
                        var packedIndexValue = data[packedIndex];

                        var bitConsecutiveIndex = (int)bitIndex % 32;

                        if ((packedIndexValue & (1 << bitConsecutiveIndex)) > 0)
                        {
                            Gizmos.DrawCube(new float3(bitPosition) * bitSizeInMeters, new float3(bitSizeInMeters));
                        }
                    }
                }
            }
        }
    }
}