using System.Runtime.InteropServices;

namespace LanguageCore.Tokenizing;

public partial class Tokenizer
{
    static readonly ImmutableArray<char> Bracelets = ImmutableArray.Create(
        '{', '}',
        '(', ')',
        '[', ']'
    );

    static readonly ImmutableArray<char> Operators = ImmutableArray.Create(
        '+', '-',
        '*', '/',
        '=', '<', '>',
        '!', '%',
        '^', '|', '&', '~'
    );

    static readonly ImmutableArray<string> DoubleOperators = ImmutableArray.Create(
        "++", "--",
        "<<", ">>",
        "&&", "||",
        "=>"
    );

    static readonly ImmutableArray<char> SimpleOperators = ImmutableArray.Create(
        ';',
        ',',
        ':'
    );

    readonly List<Token> Tokens;
    readonly List<SimpleToken> UnicodeCharacters;
    readonly DiagnosticsCollection Diagnostics;
    readonly TokenizerSettings Settings;
    readonly Uri File;

    readonly PreparationToken CurrentToken;
    int CurrentColumn;
    int CurrentLine;
    string? SavedUnicode;

    public static TokenizerResult Tokenize(string text, DiagnosticsCollection diagnostics, Uri file, TokenizerSettings? settings = null)
        => new Tokenizer(settings ?? TokenizerSettings.Default, file, diagnostics)
        .TokenizeInternal(text);

    public static TokenizerResult Tokenize(PreprocessorResult preprocessorResult, DiagnosticsCollection diagnostics, Uri file, TokenizerSettings? settings = null)
        => new Tokenizer(settings ?? TokenizerSettings.Default, file, diagnostics)
        .TokenizeInternal(preprocessorResult);

    TokenizerResult TokenizeInternal(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            ProcessCharacter(text[i], i);
        }

        EndToken(text.Length);

        return new TokenizerResult(NormalizeTokens(Tokens, Settings).ToImmutableArray(), UnicodeCharacters.ToImmutableArray());
    }

    TokenizerResult TokenizeInternal(PreprocessorResult preprocessorResult)
    {
        int j = 0;
        int k = 0;
        for (int i = 0; i < preprocessorResult.Text.Length; i++)
        {
            if (k < preprocessorResult.TrimmedRanges.Length && preprocessorResult.TrimmedRanges[k].Index == j)
            {
                TrimmedRange trimmedRange = preprocessorResult.TrimmedRanges[k];

                Tokens.Add(new Token(TokenType.PreprocessSkipped, string.Empty, new Position(new Range<SinglePosition>(new SinglePosition(CurrentLine, CurrentColumn), new SinglePosition(CurrentLine + trimmedRange.Lines, 0)), new Range<int>(j, j + trimmedRange.Absolute))));

                j += trimmedRange.Absolute;
                CurrentLine += trimmedRange.Lines;
                k++;
            }

            ProcessCharacter(preprocessorResult.Text[i], j);
            j++;
        }

        EndToken(j);

        return new TokenizerResult(NormalizeTokens(Tokens, Settings).ToImmutableArray(), UnicodeCharacters.ToImmutableArray());
    }

    Tokenizer(TokenizerSettings settings, Uri file, DiagnosticsCollection diagnostics)
    {
        CurrentToken = new(default);
        CurrentColumn = 0;
        CurrentLine = 0;

        Tokens = new();
        UnicodeCharacters = new();
        Diagnostics = diagnostics;

        Settings = settings;
        SavedUnicode = null;
        File = file;
    }

    SinglePosition CurrentSinglePosition => new(CurrentLine, CurrentColumn);
    Position GetCurrentPosition(int offsetTotal) => new(new Range<SinglePosition>(CurrentSinglePosition, new SinglePosition(CurrentLine, CurrentColumn + 1)), new Range<int>(offsetTotal, offsetTotal + 1));

    void RefreshTokenPosition(int offsetTotal) => CurrentToken.Position = new Position(
        new Range<SinglePosition>(CurrentToken.Position.Range.Start, CurrentSinglePosition),
        new Range<int>(CurrentToken.Position.AbsoluteRange.Start, offsetTotal)
    );

    static ReadOnlySpan<Token> NormalizeTokens(List<Token> tokens, TokenizerSettings settings)
    {
        List<Token> result = new(tokens.Count);

        foreach (Token token in tokens)
        {
            if (result.Count == 0)
            {
                result.Add(token);
                continue;
            }

            Token lastToken = result[^1];

            if (token.TokenType == TokenType.Whitespace &&
                lastToken.TokenType == TokenType.Whitespace)
            {
                result[^1] = lastToken + token;
                continue;
            }

            if (settings.JoinLinebreaks &&
                token.TokenType == TokenType.LineBreak &&
                lastToken.TokenType == TokenType.LineBreak)
            {
                result[^1] = lastToken + token;
                continue;
            }

            result.Add(token);
        }

        return CollectionsMarshal.AsSpan(result);
    }
}
