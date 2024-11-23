using Godot;
using LanguageExt;
using static LanguageExt.Prelude;

namespace CardGame;

public static class GDExtension
{
	public static IO<Unit> deferred(Action a) =>
		lift(() => Callable.From(a).CallDeferred());

	public static IO<Unit> deferred(IO<Unit> io) =>
		lift(() => Callable.From(() =>
        {
            io.Run();
        }).CallDeferred());
}