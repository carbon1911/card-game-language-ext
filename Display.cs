using System;
using LanguageExt;
using static LanguageExt.Prelude;

namespace CardGame;

// TODO: come up with a better name
public static class Display2
{
	public static Game<string> Winners(Seq<(Player Player, int Score)> winners) =>
		Pure(winners switch
		{
			[]      => "Everyone's bust!",
			[var p] => $"{p.Player.Name} is the winner with {p.Score}!",
			var ps  => $"{ps.Map(p => p.Player.Name).ToFullString()} have won with {ps[0].Score}!"
		});

	public static Game<string> PlayerStates(Seq<(Player Player, PlayerState State)> players) =>
		players.Traverse(p => PlayerState(p.Player, p.State)).Map(seq => seq.Reduce((f, s) => f + System.Environment.NewLine + s)).As();

	public static Game<string> PlayerState(Player player, PlayerState state) =>
		Pure($"{player.Name} {state.Cards}, possible scores {state.Scores}" + (state.StickState ? "[STICK]" : string.Empty));
}