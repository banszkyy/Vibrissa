using LanguageCore.Tokenizing;

namespace LanguageCore.Parser.Statements;

public class TypeArgumentListExpression : Statement
{
    public ImmutableArray<TypeInstance> Arguments { get; }
    public ImmutableArray<Token> Commas { get; }
    public TokenPair Brackets { get; }

    public override Position Position => new Position(Arguments).Union(Brackets);

    public TypeArgumentListExpression(
        ImmutableArray<TypeInstance> arguments,
        ImmutableArray<Token> commas,
        TokenPair brackets,
        Uri file) : base(file)
    {
        Arguments = arguments;
        Commas = commas;
        Brackets = brackets;
    }

    public static TypeArgumentListExpression CreateAnonymous(TokenPair brackets, Uri file) => new(
        ImmutableArray<TypeInstance>.Empty,
        ImmutableArray<Token>.Empty,
        brackets,
        file
    );

    public static TypeArgumentListExpression CreateAnonymous(Uri file) => new(
        ImmutableArray<TypeInstance>.Empty,
        ImmutableArray<Token>.Empty,
        TokenPair.CreateAnonymous("<", ">"),
        file
    );

    public override string ToString() => $"<{string.Join(", ", Arguments)}>";
}
