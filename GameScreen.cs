using Godot;
using LanguageExt;
using LanguageExt.Effects;
using LanguageExt.Pipes;
using static LanguageExt.Prelude;

namespace CardGame;

public partial class GameScreen : Node2D
{
	[Export]
	Label? Label { get; set; }

	[Export]
	private Button? PlayAgainButton { get; set; }

	// Not necessary so far. It becomes necessary once threads are introduced
	private Atom<GameState> _gs = Atom(GameState.Zero);

	public override void _Ready()
	{
		OnReady.Run().Run(Main.Runtime, Main.EnvIO).ThrowIfFail().Ignore();
	}

	private OptionT<Eff<MinRT>, Unit> OnReady =>
		from btn in OptionT.lift<Eff<MinRT>, Button>(Optional(PlayAgainButton))
		from _1 in OptionT.lift((SetUpPlayAgainEvent(btn) | Proxy.repeat(OnButtonPressed)).RunEffect().ForkIO())
		select unit;

	private void _on_player_screen_correct_name(string name)
	{
		OnPlayerNameProvided(name).Run().As().Run().Ignore();
	}
	
	private OptionT<IO, Unit> OnPlayerNameProvided(string name) =>
		from gameState in OptionT.lift<IO, GameState>(_gs.Swap(gs => InitGame(name).Run(gs).Run().Map(o => o.State)))
		from label in OptionT.lift<IO, Label>(Optional(Label))
		from playAgain in OptionT.lift<IO, Button>(Optional(PlayAgainButton))
		from t in OptionT.lift(IO.lift(async () =>
		{
			Visible = true;
			label.Text = $"'{name}' added to the game";
			await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
			label.Text += $"{System.Environment.NewLine}Let's play...";
			await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
			playAgain.Visible = true;
			return unit;
		}))
		from taskGameState2 in OptionT.lift<IO, Task<GameState>>(t.ContinueWith(_ => _gs.Swap(gs => PlayHands.Run(gs).Run().Map(o => o.State))))
		select unit;

	private static Game<Unit> InitGame(string playerName) =>
		Game.addPlayer(playerName) >>
		Deck.shuffle;

	private Game<Unit> PlayHands =>
		Game.initPlayers >> PlayHand;

	private Game<Unit> PlayHand =>
		Players.with(Game.players, DealHand);

	private Game<Unit> DealHand =>
		from cs     in DealCard >> DealCard
		from player in Player.current
		from state  in Player.state
		from playerString in PlayerWrapper.playerState(player, state)
		from cardCount in Deck.cardsRemaining
		from label 	in Game.lift(Optional(Label))
		from _		in Game.liftIO(GDExtension.deferred(() => label.Text = $"{playerString}{System.Environment.NewLine}Cards remaining: {cardCount}"))
		select unit;

	private static Game<Unit> DealCard =>
		from card in Deck.deal
		from _    in Player.addCard(card)
		select unit;

	private static Producer<Unit, Eff<MinRT>, Unit> SetUpPlayAgainEvent(Button btn) =>
		from rtime in runtime<MinRT>()
		let queue = Proxy.Queue<Eff<MinRT>, Unit>()
		from _ in GDExtension.deferred(() => btn.Pressed += () => queue.Enqueue(unit))
		from result in queue
		select unit;

	private Consumer<Unit, Eff<MinRT>, Unit> OnButtonPressed =>
		from _ in Proxy.awaiting<Unit>()
		from _1 in Pure(_gs.Swap(fun((GameState gs) => PlayHands.Run(gs).Run().Map(res => res.State).IfNone(() => throw new ValueIsNoneException()))))
		from _2 in GDExtension.deferred(() => Optional(PlayAgainButton).Map(async btn =>
		{
			btn.Disabled = true;
			await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
			btn.Disabled = false;
		}).Ignore())
		select unit;

	private static string GameStateToString(GameState gameState) =>
		$"{nameof(GameState.State)}: {gameState.State}{System.Environment.NewLine}" +
		$"{nameof(GameState.Deck)}: {gameState.Deck}";
}
