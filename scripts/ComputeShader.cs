

using System;
using Godot;
using static Godot.GD;

[GlobalClass]
public partial class ComputeShader : Node3D
{

    RenderingDevice rd;
    Rid shader;
    
    ComputeShader (String shader_path, RenderingDevice rd) {
        var shader_file = Load<RDShaderFile>(shader_path);
        if (shader_file == null)
            throw new ArgumentException("ComputeShader::new() - shader_path not vaild, error loading.");

        var shader_byte_code  = shader_file.GetSpirV(); // TODO: Throw execption is compilation is failed
        shader = rd.ShaderCreateFromSpirV(shader_byte_code);
    }

    public class ShaderBufferUniform {

        RenderingDevice rd;
        

        public ShaderBufferUniform(RenderingDevice rd, byte[] data) {
            
        }

    }

    public override void _Ready()
    {

        rd = RenderingServer.CreateLocalRenderingDevice();

        var shader_file = Load<RDShaderFile>("res://scripts/shaders/surface_net.glsl");
        var shader_byte_code = shader_file.GetSpirV();
        shader = rd.ShaderCreateFromSpirV(shader_byte_code);

        float[] test_input = [1, 2, 3, 4];
        var bytes = new byte[test_input.Length * sizeof(float)];
        Buffer.BlockCopy(test_input, 0, bytes, 0, test_input.Length * sizeof(float));

        var buffer = rd.StorageBufferCreate((uint)bytes.Length, bytes);

        var uniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 0
        };
        uniform.AddId(buffer);
        var uniform_set = rd.UniformSetCreate([uniform], shader, 0);

        var pipeline = rd.ComputePipelineCreate(shader);
        var computeList = rd.ComputeListBegin();
        rd.ComputeListBindComputePipeline(computeList, pipeline);
        rd.ComputeListBindUniformSet(computeList, uniform_set, 0);
        rd.ComputeListDispatch(computeList, 2, 1, 1);
        rd.ComputeListEnd();

        rd.Submit();
        rd.Sync();

        var outputBytes = rd.BufferGetData(buffer);
        var output = new float[test_input.Length];
        Buffer.BlockCopy(outputBytes, 0, output, 0, outputBytes.Length);

        for (int i = 0; i < 4; i++)
            Print(output[i]);

    }

}
