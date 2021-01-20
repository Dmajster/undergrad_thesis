using System;
using Unity.Mathematics;
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

            var dataCount = (int)math.ceil((float)GetVolumeElementCount() / sizeof(int) / 8);

            Data = new uint[dataCount];
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
