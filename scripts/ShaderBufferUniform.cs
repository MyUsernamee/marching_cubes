using Godot;
using static Godot.GD;

public class ShaderBufferUniform : Uniform {

    byte[] _data;
    uint _bufferSize;
    Rid _buffer;

    public ShaderBufferUniform(RenderingDevice rd, byte[] data) {
        _rd = rd;
        _data = data;
        _buffer = rd.StorageBufferCreate((uint)_data.Length, _data);
    }

    public void SetData(byte[] data) {
        _data = data;
    }

    public byte[] GetLocalData() {
        return _data;
    }

    public byte[] GetDeviceData() {
        return _rd.BufferGetData(_buffer);
    }

    public override RDUniform GetRDUniform(int binding) {
        var uniform = new RDUniform();
        uniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        uniform.Binding = binding;
        uniform.AddId(_buffer);
        return uniform;
    }

}
