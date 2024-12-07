using Godot;
using LanguageExt;
using LanguageExt.Effects;
using LanguageExt.Pipes;
using static LanguageExt.Prelude;

namespace CardGame;

public partial class GameScreen : Node2D
{
	[Export]
	private Label? Label { get; set; }

	[Export]
	private Button? PlayAgainButton { get; set; }

	[Export]
	private Button? StickButton { get; set; }

	[Export]
	private Button? TwistButton { get; set; }

	[Export]
	int DeckSize { get; set; } = 52;

	// Not necessary so far. It becomes necessary once threads are introduced
	private Atom<GameState> _gs = Atom(GameState.Zero);

	public override void _Ready()
	{
		OnReady.Run().Run(Main.Runtime, Main.EnvIO).ThrowIfFail().Ignore();
	}

	private static OptionT<Eff<MinRT>, T> LiftUiElement<T>(T? elem) =>
		OptionT.lift<Eff<MinRT>, T>(Optional(elem));

	private OptionT<Eff<MinRT>, Unit> OnReady =>
		from playAgain in LiftUiElement(PlayAgainButton)
		from stickButton in LiftUiElement(StickButton)
		from twistButton in LiftUiElement(TwistButton)
		from label in LiftUiElement(Label)
		let sat = new StickAndTwistButtons(stickButton, twistButton)
		from _1 in OptionT.lift((SetUpPlayAgainEvent(playAgain) | Proxy.repeat(OnPlayAgainButtonPressed)).RunEffect().ForkIO())
		from _2 in OptionT.lift((SetUpPlayAgainEvent(stickButton) | Proxy.repeat(OnStickOrTwistButtonPressed(Player.stick, _gs, sat, playAgain, label))).RunEffect().ForkIO())
		from _3 in OptionT.lift((SetUpPlayAgainEvent(twistButton) | Proxy.repeat(OnStickOrTwistButtonPressed(Twist(label), _gs, sat, playAgain, label))).RunEffect().ForkIO())
		select unit;

	private void _on_player_screen_correct_name(Godot.Collections.Array<string> names)
	{
		OnPlayerNameProvided(names).Run().As().Run().Ignore();
	}
	
	private OptionT<IO, Unit> OnPlayerNameProvided(Godot.Collections.Array<string> names) =>
		from gameState in OptionT.lift<IO, GameState>(_gs.Swap(gs => InitGame(names).Run(gs).Run().Map(o => o.State)))
		from label in OptionT.lift<IO, Label>(Optional(Label))
		from playAgain in OptionT.lift<IO, Button>(Optional(PlayAgainButton))
		from sat in OptionT.lift<IO, StickAndTwistButtons>(StickAndTwistButtons.From(StickButton, TwistButton))
		from t in OptionT.lift(IO.lift(async () =>
		{
			Visible = true;
			label.Text = $"'{Seq(names.AsEnumerable()).Reduce((n1, n2) => n1 + ", " + n2)}' added to the game";
			await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
			label.Text += $"{System.Environment.NewLine}Let's play...";
			await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
			label.Text = string.Empty;
			sat.Visible(true);
			playAgain.Visible = false;	
			_gs.Swap(gs => PlayHands.Run(gs).Run().Map(o => o.State));
		}))
		select unit;

	private Game<Unit> InitGame(Godot.Collections.Array<string> playerNames) =>
		Seq(playerNames.AsEnumerable()).Fold(Game.unitM, name => fun((Game<Unit> g) => g >> Game.addPlayer(name))) >>
		Shuffle;

	public Game<Unit> Shuffle =>
		from deck in Deck2.Generate(() => DeckSize)
		from _    in Deck.put(deck)
		select unit;

	private Game<Unit> PlayHands =>
		Game.initPlayers >> PlayHand;

	private Game<Unit> PlayHand =>
		Players.with(Game.players, DealHand);

	private Game<Unit> DealHand =>
		from cs     in DealCard >> DealCard
		from player in Player.current
		from state  in Player.state
		from playerString in Display2.PlayerState(player, state)
		from label 	in Game.lift(Optional(Label))
		from _		in Game.liftIO(GDExtension.deferred(() => label.Text += $"{playerString}{System.Environment.NewLine}"))
		select unit;

	private static Game<Unit> DealCard =>
		from card in Deck.deal
		from _    in Player.addCard(card)
		select unit;

	private static Game<Unit> Twist(Label label) =>
		from card in Deck.deal
		from _    in Player.addCard(card) >> 
					Game.liftIO(GDExtension.deferred(() => label.Text += $"\n\t{card}")) >>
					when(Player.isBust, Game.liftIO(GDExtension.deferred(() => label.Text += $"\n\tBust!")))
		select unit;

	private static Game<Unit> GameOver(Label label) =>
		from ws in Game.winners
		from ps in Game.playersState
		from winners in Display2.Winners(ws)
		from playerStates in Display2.PlayerStates(ps)
		from _  in Game.liftIO(GDExtension.deferred(() => label.Text += winners + System.Environment.NewLine + playerStates))
		select unit;

	private static Game<Unit> PlayRound(Game<Unit> stickOrTwist, Label label, Button playAgain) =>
		iff(Game.isGameActive,

			// TODO: fix here
			// Then: 	from currentPlayer in Player.current
			// 		from _ in Player.with(currentPlayer, stickOrTwist)
					from _ in Players.with(Game.players, stickOrTwist)
					from cardCount in Deck.cardsRemaining
					from _2 in GameOver(label) >>
						Game.liftIO(GDExtension.deferred(() => label.Text += $"{System.Environment.NewLine}{cardCount} cards remaining in the deck"))
					select unit,
			Else: Game.LiftIO(GDExtension.deferred(() => playAgain.Visible = true))
		).As();

	private static Producer<Unit, Eff<MinRT>, Unit> SetUpPlayAgainEvent(Button btn) =>
		from rtime in runtime<MinRT>()
		let queue = Proxy.Queue<Eff<MinRT>, Unit>()
		from _ in GDExtension.deferred(() => btn.Pressed += () => queue.Enqueue(unit))
		from result in queue
		select unit;

	private static Consumer<Unit, Eff<MinRT>, Unit> OnStickOrTwistButtonPressed(Game<Unit> operation, Atom<GameState> gameState, StickAndTwistButtons stickAndTwistButtons, Button playAgain, Label label) =>
		from _ in Proxy.awaiting<Unit>()
		from _1 in Pure(Run(PlayRound(operation, label, playAgain), gameState))
		from _2 in Pure(unless(Game.isGameActive,
			GDExtension.deferred(() =>
		{
			playAgain.Visible = true;
			stickAndTwistButtons.Visible(false);
		})))
		select unit;

	private Consumer<Unit, Eff<MinRT>, Unit> OnPlayAgainButtonPressed =>
		from _ in Proxy.awaiting<Unit>()
		from _1 in Pure(_gs.Swap(fun((GameState gs) => PlayHands.Run(gs).Run().Map(res => res.State).IfNone(() => throw new ValueIsNoneException()))))
		from _2 in GDExtension.deferred(() => Optional(PlayAgainButton).Map(async btn =>
		{
			btn.Disabled = true;
			await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
			btn.Disabled = false;
		}).Ignore())
		select unit;

	// private static string GameStateToString(GameState gameState) =>
	// 	$"{nameof(GameState.State)}: {gameState.State}{System.Environment.NewLine}" +
	// 	$"{nameof(GameState.Deck)}: {gameState.Deck}";

	private static GameState Run<A>(Game<A> game, Atom<GameState> gameState) =>
		gameState.Swap(gs => game.Run(gs).Run().Map(res => res.State).IfNone(() =>
		{
			GD.Print("error");
			throw new ValueIsNoneException();
		}));
}
