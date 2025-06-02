using Godot;

[GlobalClass]
public abstract partial class Uniform : GodotObject {

    [Signal]
    public delegate void RidUpdatedEventHandler(Uniform uniform);

    protected RenderingDevice _rd;

    public abstract RDUniform GetRDUniform(int binding);

}
