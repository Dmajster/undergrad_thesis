using Unity.Mathematics;
using UnityEngine;

namespace Assets
{
    public class Voxelizer : MonoBehaviour
    {
        public bool RecreateVoxels;
        public bool RecreateMesh;
        public bool SaveToFile;
        public bool Reduce;

        public UniformVolume UniformVolume;
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

                var mesh = VoxelizationVisualizer.CreateDebugMesh(UniformVolume);

                var visualizerGameObject = new GameObject("Voxelization Visualizer");
                visualizerGameObject.transform.position = gameObject.transform.position - gameObject.transform.localScale / 2;
                visualizerGameObject.AddComponent<MeshFilter>().mesh = mesh;
                visualizerGameObject.AddComponent<MeshRenderer>().sharedMaterial = VisualizerMaterial;
            }

            if (SaveToFile)
            {
                SaveToFile = false;
                UniformVolumeStore.Save(UniformVolume, "sponza.uvd");
            }

            if (Reduce)
            {
                Reduce = false;
                FindObjectOfType<SvdagManager>().Execute(UniformVolume);
            }
        }

        private void CreateVoxelData()
        {
            var corner = transform.position - transform.localScale / 2;
            var oppositeCorner = transform.position + transform.localScale / 2;

            var min = math.min(corner, oppositeCorner);
            var max = math.max(corner, oppositeCorner);

            if (UniformVolume.VoxelSideLengthInMeters == 0)
            {
                return;
            }

            UniformVolume.WorldDimensionsInMeters = (max - min);

            UniformVolume.Volume = new bool[UniformVolume.VolumeCount()];

            var volumeDimensions = UniformVolume.VolumeDimensions();
            var voxelScale = new float3(UniformVolume.VoxelSideLengthInMeters);

            var originWorldPosition = transform.position - transform.localScale / 2;
            var index = 0;
            for (var y = 0; y < volumeDimensions.y; y++)
            {
                for (var x = 0; x < volumeDimensions.x; x++)
                {
                    for (var z = 0; z < volumeDimensions.z; z++)
                    {
                        var volumePosition = new Vector3(x, y, z);
                        var worldPosition = originWorldPosition + volumePosition * UniformVolume.VoxelSideLengthInMeters;

                        if (Physics.OverlapBox(worldPosition, voxelScale / 2).Length > 0)
                        {
                            UniformVolume.Volume[index] = true;
                        }
                        index++;
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }
}
