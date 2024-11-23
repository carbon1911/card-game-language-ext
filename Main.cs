using Godot;
using LanguageExt;
using LanguageExt.Effects;

namespace CardGame;

public partial class Main : Node
{
	public static readonly MinRT Runtime = new();

	public static readonly EnvIO EnvIO = EnvIO.New();

	public static Game<Unit> GAME = Game.unitM;
}
