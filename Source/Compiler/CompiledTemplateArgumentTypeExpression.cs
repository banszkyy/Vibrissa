using LanguageCore.Parser;

namespace LanguageCore.Compiler;

public class CompiledTemplateArgumentTypeExpression : CompiledTemplateArgumentExpression, IReferenceable<TypeInstance>
{
    public required CompiledTypeExpression Value { get; init; }
    public List<Reference<TypeInstance>> References { get; } = new();

    public override bool SameAs(CompiledTemplateArgumentExpression other) => other is CompiledTemplateArgumentTypeExpression v && Value.SameAs(v.Value);
    public override string ToString() => Value.ToString();
}
