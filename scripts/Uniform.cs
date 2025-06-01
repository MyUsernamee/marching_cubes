using Godot;
using static Godot.GD;

[GlobalClass]
public abstract class Uniform {

    protected RenderingDevice _rd;

    public abstract RDUniform GetRDUniform();

}
