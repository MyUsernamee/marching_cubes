using Godot;
using System;
using static Godot.GD;

[GlobalClass]
public partial class TestCompute : Node {

    public override void _Ready()
    {

        float[] data = {1, 2, 3, 4};

        var rd = RenderingServer.CreateLocalRenderingDevice() ;
        var compute_shader = new ComputeShader("res://scripts/shaders/surface_net.glsl", rd);
        var shader_buffer_uniform = ShaderBufferUniform.From(rd, data);

        compute_shader.AddUniform(shader_buffer_uniform);

        compute_shader.Run(new Vector3I(4, 1, 1));
        compute_shader.Sync();

        Print(shader_buffer_uniform.GetDeviceData<float>()[0]);
    }

}
