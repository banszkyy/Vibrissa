namespace LanguageCore.Compiler;

public abstract class CompiledConstantAccess : CompiledAccessExpression
{
    public abstract bool SameAs(CompiledConstantAccess other);
    public bool SameAs(CompiledValue other) => SameAs(new CompiledConstantValue()
    {
        Value = other,
        Type = BuiltinType.Create(other.Type),
        Location = default,
        SaveValue = false,
    });
}
