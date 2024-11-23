using Godot;

namespace CardGame;

public partial class GameScreen : Node2D
{
	private void _on_player_screen_correct_name(string name)
	{
		Visible = true;
	}
}
