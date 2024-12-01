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
		OnPlayerNameProvided(name).Run().As().Run().Ignore();
	}
	
	private OptionT<IO, Unit> OnPlayerNameProvided(string name) =>
		from res in OptionT.lift<IO, GameState>(InitGame(name).Run(GameState.Zero).Run().Map(o => o.State))
		from label in OptionT.lift<IO, Label>(Optional(Label))
		from _ in OptionT.lift(IO.lift(async () =>
		{
			Visible = true;
			label.Text = $"'{name}' added to the game";
			await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
			label.Text += $"{System.Environment.NewLine}Let's play...";
		}))
		select unit;

	private static Game<Unit> InitGame(string playerName) => Game.addPlayer(playerName) >> Deck.shuffle;

	private static string GameStateToString(GameState gameState) =>
		$"{nameof(GameState.State)}: {gameState.State}{System.Environment.NewLine}" +
		$"{nameof(GameState.Deck)}: {gameState.Deck}";
}
