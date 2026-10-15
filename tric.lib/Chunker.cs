namespace tric.lib;

/// <summary>
/// Splits large strings into fixed-size pieces so they can be transferred in small messages.
/// </summary>
public static class Chunker
{
    /// <summary>
    /// Splits <paramref name="value"/> into pieces of at most <paramref name="chunkSize"/> characters.
    /// An empty string yields a single empty piece so that it can be stored and restored.
    /// </summary>
    public static IReadOnlyList<string> Split(string value, int chunkSize)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(chunkSize, 0);

        if (value.Length == 0)
        {
            return [string.Empty];
        }

        var pieces = new List<string>((value.Length + chunkSize - 1) / chunkSize);
        for (var offset = 0; offset < value.Length; offset += chunkSize)
        {
            pieces.Add(value.Substring(offset, Math.Min(chunkSize, value.Length - offset)));
        }

        return pieces;
    }
}
