using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace Assets
{
    public class SvdagManager : MonoBehaviour
    {
        public ComputeShader ComputeShader;

        private int _reductionKernelId;

        public bool[] DebugInputData;
        public bool[] DebugOutputData;

        private void Start()
        {
            _reductionKernelId = ComputeShader.FindKernel("reduction");
        }

        public void Execute(UniformVolume uniformVolume)
        {
            var volumeDimensions = uniformVolume.VolumeDimensions();

            if (math.any(!math.ispow2(volumeDimensions)))
            {
                return;
            }

            var inputBuffer = new ComputeBuffer(uniformVolume.VolumeCount(), sizeof(byte));
            var inputDimensions = new Vector4(volumeDimensions.x, volumeDimensions.y, volumeDimensions.z);
            inputBuffer.SetData(uniformVolume.Volume);

            DebugInputData = uniformVolume.Volume; //TODO: Remove

            var outputBuffer = new ComputeBuffer(uniformVolume.VolumeCount(), sizeof(byte));
            var outputDimensions = inputDimensions / 2;
            
            

            ComputeShader.SetBuffer(_reductionKernelId, "input", inputBuffer);
            ComputeShader.SetVector("input_dimensions", inputDimensions);

            ComputeShader.SetBuffer(_reductionKernelId, "output", outputBuffer);
            ComputeShader.SetVector("output_dimensions", outputDimensions);
            
            var dispatchDimensions = volumeDimensions / 8;
            ComputeShader.Dispatch(_reductionKernelId, dispatchDimensions.x, dispatchDimensions.y, dispatchDimensions.z );

            outputBuffer.GetData(DebugOutputData); //TODO: Remove

            inputBuffer.Dispose();
            outputBuffer.Dispose();
        }
    }
}
