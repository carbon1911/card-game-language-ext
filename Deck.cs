using LanguageExt;
using LanguageExt.UnsafeValueAccess;

namespace CardGame;

public static class Deck2
{
	public static IO<Deck> Generate(Func<int> deckSizeAccessor) => IO.lift(() =>
	{
		var random = new Random((int)DateTime.Now.Ticks);
		var array  = List.generate(deckSizeAccessor(), ix => new Card(ix)).ToArray();
		random.Shuffle(array);
		return new Deck(array.ToSeqUnsafe());
	});
}