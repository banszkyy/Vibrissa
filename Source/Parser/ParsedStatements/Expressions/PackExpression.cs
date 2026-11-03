using LanguageCore.Tokenizing;

namespace LanguageCore.Parser.Statements;

public class PackExpression : Expression
{
    public ImmutableArray<Expression> Values { get; }
    public ImmutableArray<Token> Commas { get; }
    public TokenPair Brackets { get; }

    public override Position Position => new Position(Values).Union(Brackets);

    public PackExpression(
        ImmutableArray<Expression> values,
        ImmutableArray<Token> commas,
        TokenPair brackets,
        Uri file) : base(file)
    {
        Values = values;
        Commas = commas;
        Brackets = brackets;
    }

    public static PackExpression CreateAnonymous(TokenPair brackets, Uri file) => new(
        ImmutableArray<Expression>.Empty,
        ImmutableArray<Token>.Empty,
        brackets,
        file
    );

    public static PackExpression CreateAnonymous(Uri file) => new(
        ImmutableArray<Expression>.Empty,
        ImmutableArray<Token>.Empty,
        TokenPair.CreateAnonymous("{", "}"),
        file
    );

    public override string ToString() => $"{{{string.Join(", ", Values)}}}";
}
