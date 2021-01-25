using System;
using Unity.Mathematics;

namespace Assets.Code
{
    [Serializable]
    public struct PackedUniformVolume
    {
        public float VoxelWorldScaleInMeters;

        public int Depth;
        public uint[] Voxels;
        public uint[] Hashes;

        public PackedUniformVolume(float voxelWorldScaleInMeters, int depth)
        {
            VoxelWorldScaleInMeters = voxelWorldScaleInMeters;
            Depth = depth;
            Voxels = new uint[0];
            Hashes = new uint[0];

            var dataCount = (int)math.ceil(GetVolumeBitCount() / 32.0);

            Voxels = new uint[dataCount];
        }

        public static int GetVolumeBitCount(int depth)
        {
            return (int)math.pow(8, depth);
        }

        public static int GetSideBitCount(int depth)
        {
            return (int)math.pow(2, depth);
        }

        public static int3 GetVolumeBitDimensions(int depth)
        {
            return new int3(GetSideBitCount(depth));
        }

        public int GetBit(int bitIndex)
        {
            var packedValueIndex = bitIndex / 32;
            var packedValueBitIndex = bitIndex % 32;

            if (packedValueIndex >= Voxels.Length)
            {
                return 0;
            }

            return (Voxels[packedValueIndex] & (1u << packedValueBitIndex)) > 0 ? 1 : 0;
        }

        public void SetBit(int bitIndex)
        {
            var packedValueIndex = bitIndex / 32;
            var packedValueBitIndex = bitIndex % 32;

            Voxels[packedValueIndex] |= (1u << packedValueBitIndex);
        }

        public int3 GetBitPosition(int index)
        {
            var volumeDimensions = GetVolumeBitDimensions();

            return new int3(
                index % volumeDimensions.x,
                index / (volumeDimensions.x * volumeDimensions.z),
                (index / volumeDimensions.x) % volumeDimensions.z
            );
        }

        public byte GetNeighborOctantByte(int bitIndex)
        {
            var inputVolumeDimensions = GetVolumeBitDimensions();
            var sideBitCount = inputVolumeDimensions.x;
            var areaBitCount = inputVolumeDimensions.x * inputVolumeDimensions.z;
            var bitIndices = new[]
            {
                bitIndex,
                bitIndex + 1,
                bitIndex + sideBitCount,
                bitIndex + sideBitCount + 1,
                bitIndex + areaBitCount,
                bitIndex + areaBitCount + 1,
                bitIndex + areaBitCount + sideBitCount,
                bitIndex + areaBitCount + sideBitCount + 1
            };

            byte occupancy = 0;
            for (var bit = 0; bit < bitIndices.Length; bit++)
            {
                var bitOccupied = (byte)GetBit(bitIndices[bit]);

                occupancy |= (byte)(bitOccupied << bit);
            }

            return occupancy;
        }

        public float3 GetVolumeWorldScale()
        {
            return new float3(GetSideBitCount() * VoxelWorldScaleInMeters);
        }

        public int GetSideBitCount()
        {
            return GetSideBitCount(Depth);
        }

        public int GetVolumeBitCount()
        {
            return GetVolumeBitCount(Depth);
        }

        public int3 GetVolumeBitDimensions()
        {
            return GetVolumeBitDimensions(Depth);
        }
    }
}
