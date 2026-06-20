using Godot;

namespace VastDark.Presentation;

public partial class Main : Node
{
    public override void _Ready()
    {
        AddChild(new MapScreen());
    }
}
