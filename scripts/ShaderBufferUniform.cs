using Godot;
using static Godot.GD;

public partial class ShaderBufferUniform : Uniform {

    byte[] _data;
    uint _bufferSize;
    Rid _buffer;

    public ShaderBufferUniform(RenderingDevice rd, byte[] data) {
        _rd = rd;
        _data = data;
        CreateBuffer();
    }

    public static ShaderBufferUniform From<T>(RenderingDevice rd, T value) {
        return new ShaderBufferUniform(rd, PrimitiveToByteArrayConverter.ToBytes<T>(value));
    }

    void CreateBuffer() {
        if (_buffer != null && _buffer.IsValid)
            _rd.FreeRid(_buffer);
        _bufferSize = (uint)_data.Length;
        _buffer = _rd.StorageBufferCreate(_bufferSize, _data);
        EmitSignal(SignalName.RidUpdated, this);
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

    public T[] GetDeviceData<T>() where T: struct {
        return PrimitiveToByteArrayConverter.FromBytes<T>(_rd.BufferGetData(_buffer));
    }

    public override RDUniform GetRDUniform(int binding) {
        var uniform = new RDUniform();
        uniform.UniformType = RenderingDevice.UniformType.StorageBuffer;
        uniform.Binding = binding;
        uniform.AddId(_buffer);
        return uniform;
    }

    // Sends data from CPU to GPU
    public void UpdateDeviceBuffer() {
        if (_data.Length == _bufferSize)
            _rd.BufferUpdate(_buffer, 0, _bufferSize, _data);
        else {
            CreateBuffer(); // Regenerate the buffer becuase it is the wrong size;
        }
    }

}
