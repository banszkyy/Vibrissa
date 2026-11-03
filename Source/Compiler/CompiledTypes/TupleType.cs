using LanguageCore.Parser;

namespace LanguageCore.Compiler;

public class TupleType : GeneralType,
    IEquatable<TupleType>
{
    public ImmutableArray<GeneralType> Items { get; }

    public TupleType(ImmutableArray<GeneralType> parameters)
    {
        Items = parameters;
    }

    public override bool Equals(object? other) => Equals(other as TupleType);
    public override bool Equals(GeneralType? other) => Equals(other as TupleType);
    public bool Equals(TupleType? other)
    {
        if (other is null) return false;
        if (!Utils.SequenceEquals(Items, other.Items)) return false;
        return true;
    }
    public override bool Equals(TypeInstance? other)
    {
        if (other is null) return false;
        if (other is not TypeInstanceFunction otherFunction) return false;
        if (!Utils.SequenceEquals(Items, otherFunction.FunctionParameterTypes)) return false;
        return true;
    }
    public override int GetHashCode() => HashCode.Combine(Items);

    public override string ToString()
    {
        StringBuilder result = new();
        result.Append("tuple");
        result.Append('(');
        for (int i = 0; i < Items.Length; i++)
        {
            if (i > 0) result.Append(", ");
            result.Append(Items[i].ToString());
        }
        result.Append(')');

        return result.ToString();
    }
}
