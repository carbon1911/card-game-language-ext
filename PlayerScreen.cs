using Godot;
using LanguageExt;
using LanguageExt.Effects;
using LanguageExt.Pipes;
using LanguageExt.Traits;
using static LanguageExt.Prelude;

using GD = Godot.GD;

namespace CardGame;

public partial class PlayerScreen : Node2D
{
	[Export]
	public Button? Proceed { get; set; }

	[Export]
	public TextEdit? Player1Name { get; set; }

	[Signal]
	public delegate void CorrectNameEventHandler(string name);

	public override void _Ready()
	{
		OnReady.Run().Run(Main.Runtime, Main.EnvIO).IfFail(e => e.Throw()).Ignore();
	}

	private OptionT<Eff<MinRT>, Unit> OnReady =>
		from btn in OptionT.lift<Eff<MinRT>, Button>(Optional(Proceed))
		from p1Name in OptionT.lift<Eff<MinRT>, TextEdit>(Optional(Player1Name))
		from _ in OptionT.lift((SetUpProceedButtonEvent(btn, () => p1Name.Text) | Proxy.repeat(OnButtonPressed)).RunEffect().ForkIO())
		select unit;

	private Consumer<string, Eff<MinRT>, Unit> OnButtonPressed =>
		from name in Proxy.awaiting<string>()
		from _ in GDExtension.deferred(HandlePlayerName(name))
		select unit;

	private IO<Unit> HandlePlayerName(string name) =>
		from n in Pure(name)
		from _ in iff(notEmpty(n),
			Then: IO.lift(() =>
			{
				Visible = false;
				EmitSignal(SignalName.CorrectName, n);
			}),
			Else: IO.lift(() => GD.Print("Name is empty"))).As()
		select unit;

	private static Producer<string, Eff<MinRT>, Unit> SetUpProceedButtonEvent(Button btn, Func<string> player1NameProvider) =>
		from rtime in runtime<MinRT>()
		let queue = Proxy.Queue<Eff<MinRT>, string>()
		from _ in GDExtension.deferred(() => btn.Pressed += () => queue.Enqueue(player1NameProvider()))
		from result in queue
		select result;
}
