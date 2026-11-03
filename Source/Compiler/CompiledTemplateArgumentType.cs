using LanguageCore.Parser;

namespace LanguageCore.Compiler;

public class CompiledTemplateArgumentType : CompiledTemplateArgument, IReferenceable<TypeInstance>
{
    public required GeneralType Value { get; init; }
    public List<Reference<TypeInstance>> References { get; } = new();

    public override bool SameAs(CompiledTemplateArgument other) => other is CompiledTemplateArgumentType v && Value.SameAs(v.Value);
    public override string ToString() => Value.ToString();
}
