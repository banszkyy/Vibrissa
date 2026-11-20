namespace LanguageCore.Tokenizing;

[ExcludeFromCodeCoverage]
public readonly struct TokenizerResult
{
    public static TokenizerResult Empty => new(
        ImmutableArray<Token>.Empty
    );

    public readonly ImmutableArray<Token> Tokens;

    public TokenizerResult(ImmutableArray<Token> tokens)
    {
        Tokens = tokens;
    }
}
