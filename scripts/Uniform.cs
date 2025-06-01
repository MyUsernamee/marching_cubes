using Godot;

[GlobalClass]
public abstract class Uniform : GodotObject {

    protected RenderingDevice _rd;

    public abstract RDUniform GetRDUniform(int binding);

}
