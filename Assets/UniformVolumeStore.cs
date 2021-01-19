using System.IO;
using Unity.Mathematics;

namespace Assets
{
    public class UniformVolumeStore
    {
        public static void Save(UniformVolume uniformVolume, string path)
        {
            using (var binaryWriter = new BinaryWriter(File.Open(path, FileMode.Create)))
            {
                binaryWriter.Write(uniformVolume.VoxelSideLengthInMeters);

                binaryWriter.Write(uniformVolume.WorldDimensionsInMeters.x);
                binaryWriter.Write(uniformVolume.WorldDimensionsInMeters.y);
                binaryWriter.Write(uniformVolume.WorldDimensionsInMeters.z);

                foreach (var occupied in uniformVolume.Volume)
                {
                    binaryWriter.Write(occupied);
                }
            }
        }

        public static UniformVolume Load(string path)
        {
            using (var binaryReader = new BinaryReader(File.Open(path, FileMode.Open)))
            {
                var voxelSideLengthInMeters = binaryReader.ReadSingle();

                var volumeDimensionsInMeters = new float3(
                    binaryReader.ReadSingle(),
                    binaryReader.ReadSingle(),
                    binaryReader.ReadSingle()
                );

                var volumeDimensions = (int3)(volumeDimensionsInMeters / voxelSideLengthInMeters);
                var volumeVoxelCount = volumeDimensions.x * volumeDimensions.y * volumeDimensions.z;

                var volume = new bool[volumeVoxelCount];

                for (var i = 0; i < volumeVoxelCount; i++)
                {
                    volume[i] = binaryReader.ReadBoolean();
                }

                return new UniformVolume
                {
                    VoxelSideLengthInMeters = voxelSideLengthInMeters,
                    WorldDimensionsInMeters = volumeDimensionsInMeters,
                    Volume = volume
                };
            }
        }
    }
}

