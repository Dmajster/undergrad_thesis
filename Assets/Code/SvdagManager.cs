using Unity.Mathematics;
using UnityEngine;

namespace Assets.Code
{
    public class SvdagManager : MonoBehaviour
    {
        public ComputeShader ComputeShader;

        private int _reductionKernelId;

        private void Start()
        {
            _reductionKernelId = ComputeShader.FindKernel("reduction");
        }

        public PackedUniformVolume Execute(PackedUniformVolume srcPackedUniformVolume)
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
    }
}
