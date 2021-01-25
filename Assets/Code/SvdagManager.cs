using System.Collections.Generic;
using System.Linq;
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

        public List<PackedUniformVolume> DEBUG;

        private void Start()
        {
            _reduceOneKernelId = _reduceOneComputeShader.FindKernel("reduce_one");
            _reduceKernelId = _reduceComputeShader.FindKernel("reduce");
        }

        public PackedUniformVolume ReduceOnce(PackedUniformVolume srcPackedUniformVolume)
        {
            var srcPackedVolumeElementCount = srcPackedUniformVolume.Voxels.Length;

            var srcPackedVolume = new ComputeBuffer(srcPackedVolumeElementCount, sizeof(uint));
            srcPackedVolume.SetData(srcPackedUniformVolume.Voxels);
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
                Voxels = dstPackedVolumeData
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

            var dataCount = (int)math.ceil(bitCount / 32.0);

            // Setup an buffer that will contain all the reduction layers and fill it with the finest layer values
            // To start the reduction process
            var packedVolumes = new ComputeBuffer(dataCount, sizeof(uint));
            packedVolumes.SetData(packedUniformVolume.Voxels);
            _reduceComputeShader.SetBuffer(_reduceKernelId, "packed_volumes", packedVolumes);

            // Setup an buffer that will contain all the hashed reduction layers
            var hashedVolumes = new ComputeBuffer(bitCount, sizeof(uint));
            _reduceComputeShader.SetBuffer(_reduceKernelId, "hashed_volumes", hashedVolumes);

            // Bit offsets to use when reading from src layer or writing to the dst layer 
            var dstPackedVolumeStartOffsetBitIndex = 0;

            var dstHashedVolumeStartOffsetIndex = 0;

            for (var i = packedUniformVolume.Depth; i > 0; i--)
            {
                // Set the src packed volume to the dst packed volume for next iteration
                var srcPackedVolumeStartOffsetBitIndex = dstPackedVolumeStartOffsetBitIndex;

                var srcHashedVolumeStartOffsetIndex = dstHashedVolumeStartOffsetIndex;

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
                _reduceComputeShader.SetInt("dst_packed_volume_start_offset_bit_index", dstPackedVolumeStartOffsetBitIndex);


                _reduceComputeShader.SetInts("src_hashed_volume_dimensions", srcPackedVolumeBitDimensions.x, srcPackedVolumeBitDimensions.y, srcPackedVolumeBitDimensions.z);
                _reduceComputeShader.SetInt("src_hashed_volume_start_offset_index", srcHashedVolumeStartOffsetIndex);

                _reduceComputeShader.SetInts("dst_hashed_volume_dimensions", dstPackedVolumeBitDimensions.x, dstPackedVolumeBitDimensions.y, dstPackedVolumeBitDimensions.z);
                // Increment destination offset so we write to the area reserved for the next layer
                dstHashedVolumeStartOffsetIndex += PackedUniformVolume.GetVolumeBitCount(i);
                _reduceComputeShader.SetInt("dst_hashed_volume_start_offset_index", dstHashedVolumeStartOffsetIndex);


                // Dispatch to the GPU. /8 is used for better utilization of the GPU
                var dispatchVolumeDimensions = math.max(new int3(1), dstPackedVolumeBitDimensions / 8);
                _reduceComputeShader.Dispatch(
                    _reduceKernelId,
                    dispatchVolumeDimensions.x,
                    dispatchVolumeDimensions.y,
                    dispatchVolumeDimensions.z
                );
            }

            //Collect all the reduction data from the gpu
            var reducedPackedVolumes = new uint[dataCount];
            packedVolumes.GetData(reducedPackedVolumes);

            var reducedHashedVolumes = new uint[bitCount];
            hashedVolumes.GetData(reducedHashedVolumes);

            var packedVolumeList = new List<PackedUniformVolume>();
            var voxelWorldScaleInMeters = packedUniformVolume.VoxelWorldScaleInMeters;

            var bitOffset = 0;

            for (var i = packedUniformVolume.Depth; i > 0; i--)
            {
                bitCount = PackedUniformVolume.GetVolumeBitCount(i);
                var intCount = (int)math.ceil(bitCount / 32.0);
                var intOffset = (int)math.ceil(bitOffset / 32.0);
                packedVolumeList.Add(new PackedUniformVolume(voxelWorldScaleInMeters, i)
                {
                    Voxels = reducedPackedVolumes.Skip(intOffset).Take(intCount).ToArray(),
                    Hashes = reducedHashedVolumes.Skip(bitOffset).Take(bitCount).ToArray()
                });

                bitOffset += bitCount;
                
                voxelWorldScaleInMeters *= 2;
            }

            packedVolumes.Dispose();

            DEBUG = packedVolumeList;

            return packedVolumeList;
        }
    }
}
