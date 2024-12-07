using Godot;
using LanguageExt;
using LanguageExt.Effects;
using LanguageExt.Pipes;
using static LanguageExt.Prelude;

using GD = Godot.GD;

namespace CardGame;

public partial class PlayerScreen : Node2D
{
	[Export]
	public Button? Proceed { get; set; }

	[Export]
	public Godot.Collections.Array<TextEdit>? PlayerNames { get; set; }

	[Signal]
	public delegate void CorrectNameEventHandler(Godot.Collections.Array<string> playerNames);

	public override void _Ready()
	{
		OnReady.Run().Run(Main.Runtime, Main.EnvIO).IfFail(e => e.Throw()).Ignore();
	}

	private OptionT<Eff<MinRT>, Unit> OnReady =>
		from btn in OptionT.lift<Eff<MinRT>, Button>(Optional(Proceed))
		from playerNames in OptionT.lift<Eff<MinRT>, Godot.Collections.Array<TextEdit>>(Optional(PlayerNames))
		from _ in OptionT.lift((SetUpProceedButtonEvent(btn, playerNames.Select(playerName => fun(() => playerName.Text))) | Proxy.repeat(OnButtonPressed)).RunEffect().ForkIO())
		select unit;

	private Consumer<IEnumerable<string>, Eff<MinRT>, Unit> OnButtonPressed =>
		from names in Proxy.awaiting<IEnumerable<string>>()
		from _ in GDExtension.deferred(HandlePlayerNames(names))
		select unit;

	private IO<Unit> CheckDuplicities(IEnumerable<string> names) =>
		iff(names.Distinct().SequenceEqual(names),
			Then:  IO.lift(() =>
			{
				Visible = false;
				EmitSignal(SignalName.CorrectName, Variant.From(names.ToArray()).AsGodotArray<string>());
			}),
			Else: IO.lift(() => GD.Print("Player names must be unique."))
		).As();

	private IO<Unit> HandlePlayerNames(IEnumerable<string> names) =>
		iff(names.All(isEmpty),
			Then: IO.lift(() => GD.Print("Please provide at least one player name")),
			Else: CheckDuplicities(names)).As();

	private static Producer<IEnumerable<string>, Eff<MinRT>, Unit> SetUpProceedButtonEvent(Button btn, IEnumerable<Func<string>> playerNamesProvider) =>
		from rtime in runtime<MinRT>()
		let queue = Proxy.Queue<Eff<MinRT>, IEnumerable<string>>()
		from _ in GDExtension.deferred(() => btn.Pressed += () => queue.Enqueue(playerNamesProvider.Select(f => f())))
		from result in queue
		select result;
}
