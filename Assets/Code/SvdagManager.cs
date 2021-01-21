using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Assets.Code
{
    public class SvdagManager : MonoBehaviour
    {
        public ComputeShader ComputeShader;

        private int _reductionKernelId;

        private uint WangHash(uint seed)
        {
            seed = (seed ^ 61) ^ (seed >> 16);
            seed *= 9;
            seed ^= (seed >> 4);
            seed *= 0x27d4eb2d;
            seed ^= (seed >> 15);
            return seed;
        }

        private void Start()
        {
            _reductionKernelId = ComputeShader.FindKernel("reduction");
        }

        public PackedUniformVolume ReduceOnce(PackedUniformVolume srcPackedUniformVolume)
        {
            var srcPackedVolumeElementCount = srcPackedUniformVolume.Data.Length;

            var srcPackedVolume = new ComputeBuffer(srcPackedVolumeElementCount, sizeof(uint));
            srcPackedVolume.SetData(srcPackedUniformVolume.Data);
            ComputeShader.SetBuffer(_reductionKernelId, "src_packed_volume", srcPackedVolume);

            var srcPackedVolumeDimensions = srcPackedUniformVolume.GetVolumeDimensions();
            ComputeShader.SetInts("src_packed_volume_bit_dimensions",
                srcPackedVolumeDimensions.x,
                srcPackedVolumeDimensions.y,
                srcPackedVolumeDimensions.z);



            var dstPackedVolumeElementCount = math.max(1, srcPackedVolumeElementCount / 2);
            var dstPackedVolume = new ComputeBuffer(dstPackedVolumeElementCount, sizeof(uint));
            ComputeShader.SetBuffer(_reductionKernelId, "dst_packed_volume", dstPackedVolume);

            var dstPackedVolumeDimensions = srcPackedVolumeDimensions / 2;
            ComputeShader.SetInts("dst_packed_volume_bit_dimensions",
                dstPackedVolumeDimensions.x,
                dstPackedVolumeDimensions.y,
                dstPackedVolumeDimensions.z);



            var dispatchVolumeDimensions = math.max(new int3(1), dstPackedVolumeDimensions / 8);
            ComputeShader.Dispatch(
                _reductionKernelId,
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

        public List<PackedUniformVolume> Reduce(PackedUniformVolume srcPackedUniformVolume)
        {
            var srcPackedVolumeElementCount = srcPackedUniformVolume.Data.Length;

            var srcPackedVolume = new ComputeBuffer(srcPackedVolumeElementCount, sizeof(uint));
            srcPackedVolume.SetData(srcPackedUniformVolume.Data);
            ComputeShader.SetBuffer(_reductionKernelId, "src_packed_volume", srcPackedVolume);

            var srcPackedVolumeDimensions = srcPackedUniformVolume.GetVolumeDimensions();
            ComputeShader.SetInts("src_packed_volume_bit_dimensions",
                srcPackedVolumeDimensions.x,
                srcPackedVolumeDimensions.y,
                srcPackedVolumeDimensions.z);

            while (srcPackedUniformVolume.GetSideElementCount() > 1)
            {
                var dstPackedVolumeElementCount = math.max(1, srcPackedVolumeElementCount / 2);
                var dstPackedVolume = new ComputeBuffer(dstPackedVolumeElementCount, sizeof(uint));
                ComputeShader.SetBuffer(_reductionKernelId, "dst_packed_volume", dstPackedVolume);

                var dstPackedVolumeDimensions = srcPackedVolumeDimensions / 2;
                ComputeShader.SetInts("dst_packed_volume_bit_dimensions",
                    dstPackedVolumeDimensions.x,
                    dstPackedVolumeDimensions.y,
                    dstPackedVolumeDimensions.z);

                Debug.Log("Ran");

                var dispatchVolumeDimensions = math.max(new int3(1), dstPackedVolumeDimensions / 8);
                ComputeShader.Dispatch(
                    _reductionKernelId,
                    dispatchVolumeDimensions.x,
                    dispatchVolumeDimensions.y,
                    dispatchVolumeDimensions.z
                );



                srcPackedVolume = dstPackedVolume;
                ComputeShader.SetBuffer(_reductionKernelId, "src_packed_volume", srcPackedVolume);

                srcPackedVolumeDimensions = dstPackedVolumeDimensions;
                ComputeShader.SetInts("src_packed_volume_bit_dimensions",
                    srcPackedVolumeDimensions.x,
                    srcPackedVolumeDimensions.y,
                    srcPackedVolumeDimensions.z);

                srcPackedUniformVolume = new PackedUniformVolume(
                        srcPackedUniformVolume.VoxelWorldScaleInMeters * 2,
                    srcPackedUniformVolume.Depth - 1);

                dstPackedVolume.Dispose();
            }

            srcPackedVolume.Dispose();

            return new List<PackedUniformVolume>();
        }
    }
}
