using Godot;
using LanguageExt;
using static LanguageExt.Prelude;

namespace CardGame;

public partial class GameScreen : Node2D
{
	[Export]
	Label? Label { get; set; }

	// Not necessary so far. It becomes necessary once threads are introduced
	private Atom<GameState> _gs = Atom(GameState.Zero);

	private void _on_player_screen_correct_name(string name)
	{
		var ap = Game.addPlayer(name);

		var run = 
			from _ in OptionT.lift(GDExtension.deferred(() => Visible = true))
			from res in OptionT.lift<IO, GameState>(ap.Run(GameState.Zero).Run().Map(o => o.State))
			from label in OptionT.lift<IO, Label>(Optional(Label))
			from _1 in OptionT.lift(GDExtension.deferred(() => label.Text = res.State.ToString()))
			select unit;

		run.Run().As().Run();
	}
}
