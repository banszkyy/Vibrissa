using LanguageCore.Tokenizing;

namespace LanguageCore.Compiler;

public abstract class CompiledTemplateArgument
{
    public required Token Definition { get; init; }

    public abstract bool SameAs(CompiledTemplateArgument other);
    public abstract override string ToString();
}
