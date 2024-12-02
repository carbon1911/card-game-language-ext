using System;
using LanguageExt;
using static LanguageExt.Prelude;
using Godot;

namespace CardGame;

public static class PlayerWrapper
{
	public static Game<string> playerState(Player player, PlayerState state) =>
		Pure($"{player.Name} {state.Cards}, possible scores {state.Scores}" + (state.StickState ? "[STICK]" : string.Empty));
}