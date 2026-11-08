using LanguageCore.Tokenizing;

namespace LanguageCore.Parser.Statements;

public class TemplateArgumentListExpression : Statement
{
    public ImmutableArray<TypeInstance> Arguments { get; }
    public ImmutableArray<Token> Commas { get; }
    public TokenPair Brackets { get; }

    public override Position Position => new Position(Arguments).Union(Brackets);

    public TemplateArgumentListExpression(
        ImmutableArray<TypeInstance> arguments,
        ImmutableArray<Token> commas,
        TokenPair brackets,
        Uri file) : base(file)
    {
        Arguments = arguments;
        Commas = commas;
        Brackets = brackets;
    }

    public override string ToString() => $"<{string.Join(", ", Arguments)}>";
}
