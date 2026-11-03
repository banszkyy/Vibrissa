using LanguageCore.Tokenizing;

namespace LanguageCore.Compiler;

public abstract class CompiledTemplateArgumentExpression
{
    public abstract bool SameAs(CompiledTemplateArgumentExpression other);
    public abstract override string ToString();
}
