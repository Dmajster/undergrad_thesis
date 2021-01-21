using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Assets.Code
{
    [Serializable]
    public struct PackedUniformVolume
    {
        public float VoxelWorldScaleInMeters;

        public int Depth;
        public uint[] Data;

        public PackedUniformVolume(float voxelWorldScaleInMeters, int depth)
        {
            VoxelWorldScaleInMeters = voxelWorldScaleInMeters;
            Depth = depth;
            Data = new uint[0];

            var dataCount = (int)math.ceil((float)GetVolumeElementCount() / 32);

            Data = new uint[dataCount];
        }

        public int GetBit(int bitIndex)
        {
            var packedValueIndex = bitIndex / 32;
            var packedValueBitIndex = bitIndex % 32;

            if (packedValueIndex >= Data.Length)
            {
                return 0;
            }

            return (Data[packedValueIndex] & (1u << packedValueBitIndex)) > 0 ? 1 : 0;
        }

        public void SetBit(int bitIndex)
        {
            var packedValueIndex = bitIndex / 32;
            var packedValueBitIndex = bitIndex % 32;

            Data[packedValueIndex] |= (1u << packedValueBitIndex);
        }

        public int3 GetBitPosition(int index)
        {
            var volumeDimensions = GetVolumeDimensions();

            return new int3(
                index % volumeDimensions.x,
                index / (volumeDimensions.x * volumeDimensions.z),
                (index / volumeDimensions.x) % volumeDimensions.z
            );
        }

        public byte ByteOctant(int bitIndex)
        {
            var inputVolumeDimensions = GetVolumeDimensions();
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
                var bitOccupied = (byte) GetBit(bitIndices[bit]);

                occupancy |= (byte) (bitOccupied << bit);
            }

            return occupancy;
        }

        public float3 GetVolumeWorldScale()
        {
            return new float3(GetSideElementCount() * VoxelWorldScaleInMeters);
        }

        public int GetSideElementCount()
        {
            return (int)math.pow(2, Depth);
        }

        public int GetVolumeElementCount()
        {
            return (int)math.pow(8, Depth);
        }

        public int3 GetVolumeDimensions()
        {
            return new int3(GetSideElementCount());
        }
    }
}
