using Godot;
using LanguageExt;
using LanguageExt.Effects;
using LanguageExt.Sys.Live;

namespace CardGame;

public partial class Main : Node
{

	public static readonly MinRT Runtime = new();

	public static readonly EnvIO EnvIO = EnvIO.New();

	public static Game<Unit> GAME = Game.unitM;

	public override void _Ready()
	{
		// GD.Print(g);
	}
}
