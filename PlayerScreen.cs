using System;
using System.ComponentModel.Design;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Godot;
using LanguageExt;
using LanguageExt.Effects;
using LanguageExt.Pipes;
using LanguageExt.Traits;
using static LanguageExt.Prelude;

namespace CardGame;

public partial class PlayerScreen : Node2D
{
	[Export]
	public Button? Proceed { get; set; }

	public override void _Ready(){

		// var res = 
		// 	from btn in OptionT.lift<IO, Button>(Optional(Proceed))
		// 	from _ in OptionT.lift(SetUpButton(btn))
		// 	select unit;
		// res.Run().As().Run();

		// var r = from s in Source<string>()
		// from f in fork(repeat(OnButtonPressed(s)))
		// from _ in (
		// 	// from btn in OptionT.lift<IO, Button>(Optional(Proceed))
		// 	from _ in SetUpButton2(Proceed!, s)
		// 	select unit
		// )
		// select unit;

		// r.Run().Run().Ignore();

		Eff<MinRT, Unit> rv =
			from _ in (SetUpButton3(Proceed!) | Proxy.repeat(OnButtonPressed2)).RunEffect().ForkIO().As()
			select unit;

		rv.Run(Main.Runtime, Main.EnvIO).IfFail(e => e.Throw()).Ignore();
	}

	private static void OnPressed()
	{
		(Main.GAME >> Game.modifyPlayers(s => s.Add(new Player("name"), PlayerState.Zero))).Ignore();
		var res = from players in Game.activePlayers
			from _ in players.Traverse(p => playerState(p)).Map(_ => unit).As()
			select unit;
		// Game.Gets(g => IO.lift(() => GD.Print(g.State.Keys)).Run());
		// (from gs in Game.gets(identity)
		// select res.Run(gs).Run()).Ignore();

		var rv = Game.modifyPlayers(s => s.Add(new Player("coffee"), PlayerState.Zero)).Run(GameState.Zero).Run();

		rv.Map(state => {
			return state;
		});

	}

	private static Game<Unit> playerState(Player player) =>
			IO.lift(() => GD.Print($"{player.Name}[STICK]"));

	private static IO<Unit> SetUpButton(Button btn)
	{
		return lift(() => {
			btn.Pressed += OnPressed;
		});
	}

	private static IO<Unit> SetUpButton2(Button btn, Source<string> source) =>
		lift(() =>
		{
			btn.Pressed += () =>
			{
				post(source, "abc");
			};
		});

	private static StreamT<IO, Unit> OnButtonPressed(Source<string> source) =>
		from name in await<IO, string>(source)
		from _ in IO.lift(() => GD.Print(name))
		select unit;

	private static Consumer<string, Eff<MinRT>, Unit> OnButtonPressed2 =>
		from name in Proxy.awaiting<string>()
		from _ in IO.lift(() =>
        {
            GD.Print(name);
        })
		select unit;

	private static Producer<string, Eff<MinRT>, Unit> SetUpButton3(Button btn) =>
		from rtime in runtime<MinRT>()
		let queue = Proxy.Queue<Eff<MinRT>, string>()
		from _ in IO.lift(() => Callable.From(() => btn.Pressed += () =>
		{
			queue.Enqueue("abc");
		}).CallDeferred())
		from result in queue
		select result;
}
