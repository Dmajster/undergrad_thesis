using Assets;
using Unity.Mathematics;
using UnityEngine;

public class Voxelizer : MonoBehaviour
{
    public bool RecreateVoxels = false;
    public bool RecreateMesh = false;
    public bool SaveToFile = false;

    public UniformVolume uniformVolume;
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

            var mesh = VoxelizationVisualizer.CreateDebugMesh(uniformVolume);

            var visualizerGameObject = new GameObject("Voxelization Visualizer");
            visualizerGameObject.transform.position = gameObject.transform.position - gameObject.transform.localScale / 2;
            visualizerGameObject.AddComponent<MeshFilter>().mesh = mesh;
            visualizerGameObject.AddComponent<MeshRenderer>().sharedMaterial = VisualizerMaterial;
        }

        if (SaveToFile){
            SaveToFile = false;
            UniformVolumeStore.Save(uniformVolume,"sponza.bin");
        }
    }

    private void CreateVoxelData()
    {
        var corner = transform.position - transform.localScale / 2;
        var oppositeCorner = transform.position + transform.localScale / 2;

        var min = math.min(corner, oppositeCorner);
        var max = math.max(corner, oppositeCorner);

        if (uniformVolume.VoxelSideLengthInMeters == 0)
        {
            return;
        }

        uniformVolume.VolumeDimensionsInMeters = ((max - min) / uniformVolume.VoxelSideLengthInMeters);
        
        var volumeDiscreteDimensions = (int3)uniformVolume.VolumeDimensionsInMeters;
        uniformVolume.Volume = new bool[volumeDiscreteDimensions.x * volumeDiscreteDimensions.y * volumeDiscreteDimensions.z];

        var voxelScale = new float3(uniformVolume.VoxelSideLengthInMeters);
        var originWorldPosition = transform.position - transform.localScale / 2;
        var index = 0;
        for (var y = 0; y < uniformVolume.VolumeDimensionsInMeters.y; y++)
        {
            for (var x = 0; x < uniformVolume.VolumeDimensionsInMeters.x; x++)
            {
                for (var z = 0; z < uniformVolume.VolumeDimensionsInMeters.z; z++)
                {
                    var volume_position = new Vector3(x, y, z);
                    var world_position = originWorldPosition + volume_position * uniformVolume.VoxelSideLengthInMeters;

                    if (Physics.OverlapBox(world_position, voxelScale / 2).Length > 0)
                    {
                        uniformVolume.Volume[index] = true;
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
