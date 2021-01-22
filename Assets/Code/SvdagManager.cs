﻿using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Code
{
    public class SvdagManager : MonoBehaviour
    {
        [SerializeField] private ComputeShader _reduceComputeShader;
        [SerializeField] private ComputeShader _reduceOneComputeShader;

        private int _reduceOneKernelId;
        private int _reduceKernelId;

        public uint[] DebugReductionData;

        private void Start()
        {
            _reduceOneKernelId = _reduceOneComputeShader.FindKernel("reduce_one");
            _reduceKernelId = _reduceComputeShader.FindKernel("reduce");
        }

        public PackedUniformVolume ReduceOnce(PackedUniformVolume srcPackedUniformVolume)
        {
            var srcPackedVolumeElementCount = srcPackedUniformVolume.Data.Length;

            var srcPackedVolume = new ComputeBuffer(srcPackedVolumeElementCount, sizeof(uint));
            srcPackedVolume.SetData(srcPackedUniformVolume.Data);
            _reduceOneComputeShader.SetBuffer(_reduceOneKernelId, "src_packed_volume", srcPackedVolume);

            var srcPackedVolumeDimensions = srcPackedUniformVolume.GetVolumeBitDimensions();
            _reduceOneComputeShader.SetInts("src_packed_volume_bit_dimensions",
                srcPackedVolumeDimensions.x,
                srcPackedVolumeDimensions.y,
                srcPackedVolumeDimensions.z);



            var dstPackedVolumeElementCount = math.max(1, srcPackedVolumeElementCount / 2);
            var dstPackedVolume = new ComputeBuffer(dstPackedVolumeElementCount, sizeof(uint));
            _reduceOneComputeShader.SetBuffer(_reduceOneKernelId, "dst_packed_volume", dstPackedVolume);

            var dstPackedVolumeDimensions = srcPackedVolumeDimensions / 2;
            _reduceOneComputeShader.SetInts("dst_packed_volume_bit_dimensions",
                dstPackedVolumeDimensions.x,
                dstPackedVolumeDimensions.y,
                dstPackedVolumeDimensions.z);



            var dispatchVolumeDimensions = math.max(new int3(1), dstPackedVolumeDimensions / 8);
            _reduceOneComputeShader.Dispatch(
                _reduceOneKernelId,
                dispatchVolumeDimensions.x,
                dispatchVolumeDimensions.y,
                dispatchVolumeDimensions.z
            );



            var dstPackedVolumeData = new uint[srcPackedVolumeElementCount / 2];
            dstPackedVolume.GetData(dstPackedVolumeData);



            srcPackedVolume.Release();
            dstPackedVolume.Release();

            return new PackedUniformVolume(
                srcPackedUniformVolume.VoxelWorldScaleInMeters * 2,
                srcPackedUniformVolume.Depth - 1)
            {
                Data = dstPackedVolumeData
            };
        }

        public List<PackedUniformVolume> Reduce(PackedUniformVolume packedUniformVolume)
        {
            // Calculate the size of the array containing all the voxel data for all layers
            var bitCount = 0;

            for (var i = 0; i <= packedUniformVolume.Depth; i++)
            {
                bitCount += PackedUniformVolume.GetVolumeBitCount(i);
            }

            Debug.Log($"bit count: {bitCount}");

            var dataCount = (int)math.ceil(bitCount / 32.0);

            // Setup an buffer that will contain all the reduction layers and fill it with the finest layer values
            // To start the reduction process
            var packedVolumes = new ComputeBuffer(dataCount, sizeof(uint));
            packedVolumes.SetData(packedUniformVolume.Data);
            _reduceComputeShader.SetBuffer(_reduceKernelId, "packed_volumes", packedVolumes);

            // Bit offsets to use when reading from src layer or writing to the dst layer 
            var dstPackedVolumeStartOffsetBitIndex = 0;

            for (var i = packedUniformVolume.Depth; i > 0; i--)
            {
                // Set the src volume to the dst volume for next iteration
                var srcPackedVolumeStartOffsetBitIndex = dstPackedVolumeStartOffsetBitIndex;
                Debug.Log($"src bit offset: {srcPackedVolumeStartOffsetBitIndex} int: {srcPackedVolumeStartOffsetBitIndex / 32.0}");

                // Assign the dimensions of the source volume
                var srcPackedVolumeBitDimensions = PackedUniformVolume.GetVolumeBitDimensions(i);
                _reduceComputeShader.SetInts("src_packed_volume_bit_dimensions", srcPackedVolumeBitDimensions.x, srcPackedVolumeBitDimensions.y, srcPackedVolumeBitDimensions.z);
                // Assign the offset to use when reading bits from the src layer
                _reduceComputeShader.SetInt("src_packed_volume_start_offset_bit_index", srcPackedVolumeStartOffsetBitIndex);



                // Assign dimensions of the destination volume
                var dstPackedVolumeBitDimensions = srcPackedVolumeBitDimensions / 2;
                _reduceComputeShader.SetInts("dst_packed_volume_bit_dimensions", dstPackedVolumeBitDimensions.x, dstPackedVolumeBitDimensions.y, dstPackedVolumeBitDimensions.z);
                // Increment destination offset so we write to the area reserved for the next layer
                dstPackedVolumeStartOffsetBitIndex += PackedUniformVolume.GetVolumeBitCount(i);
                Debug.Log($"dst bit offset: {dstPackedVolumeStartOffsetBitIndex} int: {dstPackedVolumeStartOffsetBitIndex / 32.0}");

                _reduceComputeShader.SetInt("dst_packed_volume_start_offset_bit_index", dstPackedVolumeStartOffsetBitIndex);



                // Dispatch to the GPU. /8 is used for better utilization of the GPU
                var dispatchVolumeDimensions = math.max(new int3(1), dstPackedVolumeBitDimensions / 8);
                _reduceComputeShader.Dispatch(
                    _reduceKernelId,
                    dispatchVolumeDimensions.x,
                    dispatchVolumeDimensions.y,
                    dispatchVolumeDimensions.z
                );
            }

            DebugReductionData = new uint[dataCount];

            packedVolumes.GetData(DebugReductionData);

            packedVolumes.Dispose();

            return new List<PackedUniformVolume>();
        }
    }
}
