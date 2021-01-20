using UnityEngine;

namespace Assets.Code
{
    public class SvdagManager : MonoBehaviour
    {
        public ComputeShader ComputeShader;

        private int _reductionKernelId;

        public uint[] DebugInputData;
        public uint[] DebugOutputData;

        public Material VisualizerMaterial;

        private void Start()
        {
            _reductionKernelId = ComputeShader.FindKernel("reduction");
        }

        public void Execute(PackedUniformVolume packedUniformVolume)
        {
            var packedVolumeDimensions = packedUniformVolume.GetVolumeDimensions() / 4;
            
            

            var inputBuffer = new ComputeBuffer(packedUniformVolume.GetVolumeElementCount(), sizeof(uint));
            var inputDimensions = new Vector4(packedVolumeDimensions.x, packedVolumeDimensions.y, packedVolumeDimensions.z);
            Debug.Log($"input volume dimensions: '{inputDimensions}'");

            inputBuffer.SetData(packedUniformVolume.Data);

            DebugInputData = packedUniformVolume.Data; //TODO: Remove

            var outputVolumeElementCount = packedUniformVolume.GetVolumeElementCount() / 256;
            var outputBuffer = new ComputeBuffer(outputVolumeElementCount, sizeof(int));
            var outputDimensions = inputDimensions / 2;

            Debug.Log($"output volume dimensions: '{outputDimensions}'");

            ComputeShader.SetBuffer(_reductionKernelId, "packed_input", inputBuffer);
            ComputeShader.SetVector("packed_input_dimensions", inputDimensions);

            ComputeShader.SetBuffer(_reductionKernelId, "packed_output", outputBuffer);
            ComputeShader.SetVector("packed_output_dimensions", outputDimensions);

            var dispatchDimensions = packedVolumeDimensions;
            ComputeShader.Dispatch(_reductionKernelId, dispatchDimensions.x, dispatchDimensions.y, dispatchDimensions.z);

            DebugOutputData = new uint[outputVolumeElementCount]; //TODO: Remove
            outputBuffer.GetData(DebugOutputData); 

            inputBuffer.Dispose();
            outputBuffer.Dispose();


            var mesh = VoxelizationVisualizer.CreateDebugMesh(new PackedUniformVolume
            {
                WorldScaleInMeters = 0.1f,
                Data = DebugInputData,
                Depth = 5
            });

            var visualizerGameObject = new GameObject("Voxelization 5 Visualizer");
            visualizerGameObject.AddComponent<MeshFilter>().mesh = mesh;
            visualizerGameObject.AddComponent<MeshRenderer>().sharedMaterial = VisualizerMaterial;
            visualizerGameObject.transform.position = new Vector3(0, 25, 0);

            mesh = VoxelizationVisualizer.CreateDebugMesh(new PackedUniformVolume
            {
                WorldScaleInMeters = 0.2f,
                Data = DebugOutputData,
                Depth = 4
            });

            visualizerGameObject = new GameObject("Voxelization 4 Visualizer");
            visualizerGameObject.AddComponent<MeshFilter>().mesh = mesh;
            visualizerGameObject.AddComponent<MeshRenderer>().sharedMaterial = VisualizerMaterial;
            visualizerGameObject.transform.position = new Vector3(0, 35, 0);

        }
    }
}
