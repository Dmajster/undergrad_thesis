using Unity.Mathematics;

namespace Assets
{
    [System.Serializable]
    public struct UniformVolume
    {
        public float3 WorldDimensionsInMeters;
        public float VoxelSideLengthInMeters;

        public bool[] Volume;

        public int3 VolumeDimensions()
        {
            return (int3)(WorldDimensionsInMeters / VoxelSideLengthInMeters);
        }

        public int VolumeCount()
        {
            var dimensions = VolumeDimensions();
            return dimensions.x * dimensions.y * dimensions.z;
        }
    }
}
