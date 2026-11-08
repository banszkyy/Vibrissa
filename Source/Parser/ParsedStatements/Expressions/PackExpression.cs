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

    public override string ToString() => $"{{{string.Join(", ", Values)}}}";
}
