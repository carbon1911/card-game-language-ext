using Godot;
using LanguageExt;

namespace CardGame;

public partial class Main : Node
{
    Game<Unit> g = Game.play;

    public override void _Ready()
    {
        GD.Print(g);
    }
}