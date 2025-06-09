using Godot;
using Godot.Collections;
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
        var shader_buffer_uniform_2 = ShaderBufferUniform.From(rd, new Vector3[2 * 2 * 2]);
        var shader_buffer_uniform_3 = ShaderBufferUniform.From(rd, new Vector3(2, 2, 2));

        compute_shader.AddUniform(shader_buffer_uniform);
        compute_shader.AddUniform(shader_buffer_uniform_2);
        compute_shader.AddUniform(shader_buffer_uniform_3);
        compute_shader.Run(new Vector3I(3, 3, 3));
        compute_shader.Sync();

        Print(new Array<Vector3>(shader_buffer_uniform_2.GetDeviceData<Vector3>()));
    }

}
