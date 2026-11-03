using LanguageCore.Parser.Statements;

namespace LanguageCore.Compiler;

public class CompiledTemplateArgumentValueExpression : CompiledTemplateArgumentExpression, IReferenceable<IdentifierExpression>
{
    public required CompiledExpression Value { get; init; }
    public List<Reference<IdentifierExpression>> References { get; } = new();

    public override bool SameAs(CompiledTemplateArgumentExpression other) => other is CompiledTemplateArgumentValueExpression v && Value.Equals(v.Value);
    public override string ToString() => Value.ToString();
}
