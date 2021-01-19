using System;
using Unity.Mathematics;
using UnityEngine;

namespace Assets
{
    [Serializable]
    public struct PackedUniformVolume
    {
        public float WorldScaleInMeters;

        public int Depth;
        public uint[] Data;

        public PackedUniformVolume(float worldScaleInMeters, int depth)
        {
            WorldScaleInMeters = worldScaleInMeters;
            Depth = depth;
            Data = new uint[0];

            var dataCount = (int)math.ceil(GetVolumeElementCount() / 32.0f);

            Data = new uint[dataCount];

            Debug.Log($"Volume world scale: '{GetVolumeWorldScale()}'");
        }

        public float3 GetVolumeWorldScale()
        {
            return new float3(GetSideElementCount() * WorldScaleInMeters);
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
