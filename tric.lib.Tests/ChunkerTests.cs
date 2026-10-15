namespace tric.lib.Tests;

public class ChunkerTests
{
    [Test]
    public void WhenValueIsEmptyThenSingleEmptyPieceIsReturned()
    {
        Assert.That(Chunker.Split(string.Empty, 4), Is.EqualTo(new[] { string.Empty }));
    }

    [Test]
    public void WhenValueFitsThenSinglePieceIsReturned()
    {
        Assert.That(Chunker.Split("abcd", 4), Is.EqualTo(new[] { "abcd" }));
    }

    [Test]
    public void WhenValueIsLongerThenItIsSplitIntoPiecesOfChunkSize()
    {
        Assert.That(Chunker.Split("abcdefghij", 4), Is.EqualTo(new[] { "abcd", "efgh", "ij" }));
    }

    [Test]
    public void WhenPiecesAreJoinedThenOriginalValueIsRestored()
    {
        var value = new string('x', 1000) + "end";
        Assert.That(string.Concat(Chunker.Split(value, 64)), Is.EqualTo(value));
    }

    [Test]
    public void WhenChunkSizeIsNotPositiveThenArgumentOutOfRangeIsThrown()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Chunker.Split("abc", 0));
    }
}
