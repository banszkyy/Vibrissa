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
    public TokenPair Brackets { get; }

    public override Position Position => new Position(Values).Union(Brackets);

    public InitializationExpression(
        ImmutableArray<InitializationExpressionItem> values,
        TokenPair brackets,
        Uri file) : base(file)
    {
        Values = values;
        Brackets = brackets;
    }

    public override string ToString() => $"{{{string.Join(", ", Values)}}}";
}
