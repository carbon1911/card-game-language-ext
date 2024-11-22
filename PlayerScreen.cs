using System;
using System.ComponentModel.Design;
using System.Security.Cryptography;
using Godot;
using LanguageExt;
using LanguageExt.Traits;
using static LanguageExt.Prelude;

namespace CardGame;

public partial class PlayerScreen : Node2D
{
	[Export]
	public Button? Proceed { get; set; }

	public override void _Ready(){
		var res = 
			from btn in OptionT.lift<IO, Button>(Optional(Proceed))
			from _ in OptionT.lift(SetUpButton(btn))
			select unit;
		res.Run().As().Run();
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
}
