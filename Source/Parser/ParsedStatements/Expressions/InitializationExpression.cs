using LanguageCore.Compiler;
using LanguageCore.Tokenizing;

namespace LanguageCore.Parser.Statements;

public class InitializationExpression : Statement
{
    /// <summary>
    /// Set by the compiler
    /// </summary>
    public StructType? CompiledType { get; internal set; }

    public ImmutableArray<InitializationExpressionItem> Values { get; }
    public ImmutableArray<Token> Commas { get; }
    public TokenPair Brackets { get; }

    public override Position Position => new Position(Values).Union(Brackets);

    public InitializationExpression(
        ImmutableArray<InitializationExpressionItem> values,
        ImmutableArray<Token> commas,
        TokenPair brackets,
        Uri file) : base(file)
    {
        Values = values;
        Commas = commas;
        Brackets = brackets;
    }

    public static InitializationExpression CreateAnonymous(TokenPair brackets, Uri file) => new(
        ImmutableArray<InitializationExpressionItem>.Empty,
        ImmutableArray<Token>.Empty,
        brackets,
        file
    );

    public static InitializationExpression CreateAnonymous(Uri file) => new(
        ImmutableArray<InitializationExpressionItem>.Empty,
        ImmutableArray<Token>.Empty,
        TokenPair.CreateAnonymous("{", "}"),
        file
    );

    public override string ToString() => $"{{{string.Join(", ", Values)}}}";
}
