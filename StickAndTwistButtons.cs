using Godot;
using LanguageExt;
using static LanguageExt.Prelude;

namespace CardGame;

public record StickAndTwistButtons(Button Stick, Button Twist)
{
    public static Option<StickAndTwistButtons> From(Button? stick, Button? twist) =>
        from s in Optional(stick)
        from t in Optional(twist)
        select new StickAndTwistButtons(s, t);

    public void Visible(bool visible)
    {
        Stick.Visible = visible;
        Twist.Visible = visible;
    }
}