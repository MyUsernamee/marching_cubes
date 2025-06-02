
using Godot;
using static Godot.GD;
using Godot.Collections;
using System;

[GlobalClass]
public partial class ComputeShader : Node3D
{

    RenderingDevice _rd;
    Rid _shader;
    Rid _pipeline;
    Array<Uniform> _uniforms;
    Rid _uniformSet;
    bool _uniformSetDirty = true;
    
    public ComputeShader (String shader_path, RenderingDevice rd) {
        var shader_file = Load<RDShaderFile>(shader_path);
        if (shader_file == null)
            throw new ArgumentException("ComputeShader::new() - shader_path not vaild, error loading.");
        
        _rd = rd;

        var shader_byte_code  = shader_file.GetSpirV(); // TODO: Throw execption is compilation is failed
        _shader = rd.ShaderCreateFromSpirV(shader_byte_code);
        _pipeline = _rd.ComputePipelineCreate(_shader);
        _uniforms = new Array<Uniform>();

    }

    public void Sync() {
        _rd.Barrier(RenderingDevice.BarrierMask.Compute);
    }

    public void AddUniform(Uniform uniform) {
        _uniforms.Add(uniform);
        uniform.Connect(Uniform.SignalName.RidUpdated, Callable.From<Uniform>(MakeUniformSetDirty));
        _uniformSetDirty = true;
    }

    public void AddUniformArray(Array<Uniform> uniforms) {
        _uniforms.AddRange(uniforms);
        foreach (var uniform in uniforms) 
            uniform.Connect(Uniform.SignalName.RidUpdated, Callable.From<Uniform>(MakeUniformSetDirty));
        _uniformSetDirty = true;
    }

    public void Run(Vector3I groups, Array<byte> push_constant = null) {
        if (_uniformSetDirty) {
            Array<RDUniform> bindings = new Array<RDUniform>();
            for (int i = 0; i < _uniforms.Count; i++) 
            {
                var uniform = _uniforms[i];
                bindings.Add(uniform.GetRDUniform(i));
            }
            if (_uniformSet.IsValid && _rd.UniformSetIsValid(_uniformSet))
            {
                _rd.FreeRid(_uniformSet);
            }
            _uniformSet = _rd.UniformSetCreate(bindings, _shader, 0);
            _uniformSetDirty = false;

        }
        var compute_list = _rd.ComputeListBegin();
        _rd.ComputeListBindComputePipeline(compute_list, _pipeline);
        _rd.ComputeListBindUniformSet(compute_list, _uniformSet, 0);

        if (push_constant != null) {
            while (push_constant.Count % 16 != 0)
                push_constant.Add(0);
            byte[] packed_push_constant = new byte[push_constant.Count];
            push_constant.CopyTo(packed_push_constant, 0);
            _rd.ComputeListSetPushConstant(compute_list, packed_push_constant, (uint)push_constant.Count);
        }
        
        _rd.ComputeListDispatch(compute_list, (uint)groups.X, (uint)groups.Y, (uint)groups.Z);
        _rd.ComputeListEnd();
    
    } 

    public void MakeUniformSetDirty(Uniform _) {
        _uniformSetDirty = true;
    }
    // ## Called from a bound uniform when it's RID changes.
    // func make_uniform_set_dirty(_uniform: Uniform) -> void:
    // 	uniform_set_dirty = true

}
