using Unity.Mathematics;

namespace Assets
{
    [System.Serializable]
    public struct UniformVolume
    {
        public float3 VolumeDimensionsInMeters;
        public float VoxelSideLengthInMeters;

        public bool[] Volume;
    }
}
