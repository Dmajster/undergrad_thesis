using Unity.Mathematics;
using UnityEngine;

namespace Assets
{
    public class Voxelizer : MonoBehaviour
    {
        public bool RecreateVoxels;
        public bool RecreateMesh;
        public bool Reduce;

        public PackedUniformVolume PackedUniformVolume;
        public Material VisualizerMaterial;

        private void Update()
        {
            if (RecreateVoxels)
            {
                RecreateVoxels = false;
                CreateVoxelData();
            }

            if (RecreateMesh)
            {
                RecreateMesh = false;

                var mesh = VoxelizationVisualizer.CreateDebugMesh(PackedUniformVolume);

                var visualizerGameObject = new GameObject("Voxelization Visualizer");
                visualizerGameObject.AddComponent<MeshFilter>().mesh = mesh;
                visualizerGameObject.AddComponent<MeshRenderer>().sharedMaterial = VisualizerMaterial;
            }

            if (Reduce)
            {
                Reduce = false;
                //FindObjectOfType<SvdagManager>().Execute(UniformVolume);
            }
        }

        private void CreateVoxelData()
        {
            PackedUniformVolume =
                new PackedUniformVolume(PackedUniformVolume.WorldScaleInMeters, PackedUniformVolume.Depth);

            var voxelHalfScale = PackedUniformVolume.GetVolumeWorldScale() / PackedUniformVolume.GetSideElementCount() / 2.0f;
            
            var index = 0;
            var volumeDimensions = PackedUniformVolume.GetVolumeDimensions();

            for (var y = 0; y < volumeDimensions.y; y++)
            {
                for (var x = 0; x < volumeDimensions.x; x++)
                {
                    for (var z = 0; z < volumeDimensions.z; z++)
                    {
                        var position = new float3(x, y, z);
                        var worldPosition = position * PackedUniformVolume.WorldScaleInMeters;

                        if (Physics.OverlapBox(worldPosition, voxelHalfScale).Length > 0)
                        {
                            var packedIndex = index / 32;
                            var bitIndex = index % 32;

                            PackedUniformVolume.Data[packedIndex] |= 1u << bitIndex;
                        }

                        index++;
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            var scale = Vector3.one * PackedUniformVolume.GetSideElementCount() * PackedUniformVolume.WorldScaleInMeters;

            Gizmos.DrawWireCube(transform.position, scale);
        }
    }
}
