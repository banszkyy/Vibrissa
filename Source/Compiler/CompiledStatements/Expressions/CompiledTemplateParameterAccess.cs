using LanguageCore.Tokenizing;

namespace LanguageCore.Compiler;

public class CompiledTemplateParameterAccess : CompiledConstantAccess
{
    public required Token Parameter { get; init; }

    public override string ToString() => $"{Parameter.Content}";

    public override bool SameAs(CompiledConstantAccess other) => false; // TODO
}
