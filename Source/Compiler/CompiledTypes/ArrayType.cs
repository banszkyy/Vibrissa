using LanguageCore.Parser;

namespace LanguageCore.Compiler;

public class ArrayType : GeneralType,
    IEquatable<ArrayType>
{
    public GeneralType Of { get; }
    public CompiledConstantAccess? Length { get; }

    public ArrayType(GeneralType of, CompiledConstantAccess? length)
    {
        Of = of;
        Length = length;
    }

    public bool TryGetLength(out int length)
    {
        length = default;
        if (Length is null) return false;
        if (Length is not CompiledConstantValue v) return false;
        if (!v.Value.TryCast(RuntimeType.I32, out CompiledValue l)) return false;
        length = l.I32;
        return true;
    }

    public override bool Equals(object? other) => Equals(other as ArrayType);
    public override bool Equals(GeneralType? other) => Equals(other as ArrayType);
    public bool Equals(ArrayType? other)
    {
        if (other is null) return false;
        if (!Of.Equals(other.Of)) return false;

        if (Length is not null && other.Length is not null && Length.SameAs(other.Length)) return true;

        if (Length is not null)
        {
            if (other.Length is null) return false;
            if (Length is not null)
            {
                if (other.Length is null) return false;
                if (!Length.SameAs(other.Length)) return false;
            }
            else
            {
                if (other.Length is not null) return false;
            }
        }
        else
        {
            if (other.Length is not null) return false;
        }
        return true;
    }
    public override bool Equals(TypeInstance? other)
    {
        if (other is null) return false;
        if (other is not TypeInstanceStackArray otherStackArray) return false;
        if (!Of.Equals(otherStackArray.StackArrayOf)) return false;
        return true;
    }
    public override int GetHashCode() => HashCode.Combine(Of, Length);
    public override string ToString() => $"{Of}[{Length}]";
}
