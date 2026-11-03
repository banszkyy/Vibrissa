using LanguageCore.Parser;

namespace LanguageCore.Compiler;

public class CompiledStructTypeExpression : CompiledTypeExpression,
    IEquatable<CompiledStructTypeExpression>,
    IReferenceableTo<CompiledStruct>
{
    public CompiledStruct Struct { get; }
    public ImmutableDictionary<string, CompiledTemplateArgumentExpression> TypeArguments { get; }
    public Uri File { get; }

    CompiledStruct? IReferenceableTo<CompiledStruct>.Reference
    {
        get => Struct;
        set => throw new InvalidOperationException();
    }

    [SetsRequiredMembers]
    public CompiledStructTypeExpression(CompiledStructTypeExpression other) : base(other.Location)
    {
        Struct = other.Struct;
        TypeArguments = other.TypeArguments;
        File = other.File;
    }

    [SetsRequiredMembers]
    public CompiledStructTypeExpression(CompiledStruct @struct, Uri originalFile, Location location) : base(location)
    {
        Struct = @struct;
        if (@struct.Definition.Template is not null)
        {
            TypeArguments = @struct.Definition.Template.Parameters
                .Select(v => new KeyValuePair<string, CompiledTemplateArgumentExpression>(v.Content, new CompiledTemplateArgumentTypeExpression()
                {
                    Value = new CompiledGenericTypeExpression(v, originalFile, location),
                }))
                .ToImmutableDictionary();
        }
        else
        { TypeArguments = ImmutableDictionary<string, CompiledTemplateArgumentExpression>.Empty; }
        File = originalFile;
    }

    [SetsRequiredMembers]
    public CompiledStructTypeExpression(CompiledStruct @struct, Uri originalFile, IReadOnlyList<CompiledTemplateArgumentExpression> typeArguments, Location location) : base(location)
    {
        Struct = @struct;
        if (@struct.Definition.Template is not null)
        {
            Dictionary<string, CompiledTemplateArgumentExpression> result = new(@struct.Definition.Template.Parameters.Length);

            if (@struct.Definition.Template.Parameters.Length != typeArguments.Count)
            { throw new InternalExceptionWithoutContext("Length of type parameters doesn't matching with length of type arguments"); }

            for (int i = 0; i < @struct.Definition.Template.Parameters.Length; i++)
            { result.Add(@struct.Definition.Template.Parameters[i].Content, typeArguments[i]); }

            TypeArguments = result.ToImmutableDictionary();
        }
        else
        { TypeArguments = ImmutableDictionary<string, CompiledTemplateArgumentExpression>.Empty; }
        File = originalFile;
    }

    [SetsRequiredMembers]
    public CompiledStructTypeExpression(CompiledStruct @struct, Uri originalFile, ImmutableDictionary<string, CompiledTemplateArgumentExpression> typeArguments, Location location) : base(location)
    {
        Struct = @struct;
        TypeArguments = typeArguments;
        File = originalFile;
    }

    public override bool Equals(object? other) => Equals(other as CompiledStructTypeExpression);
    public override bool Equals(CompiledTypeExpression? other) => Equals(other as CompiledStructTypeExpression);
    public bool Equals(CompiledStructTypeExpression? other)
    {
        if (other is null) return false;
        if (!Utils.ReferenceEquals(Struct, other.Struct)) return false;
        return true;
    }
    public override bool Equals(TypeInstance? other)
    {
        if (other is null) return false;

        if (other is not TypeInstanceSimple otherSimple)
        { return false; }

        if (TypeKeywords.BasicTypes.ContainsKey(otherSimple.Identifier.Content))
        { return false; }

        if (Struct.Identifier == otherSimple.Identifier.Content)
        { return true; }

        return false;
    }
    public override int GetHashCode() => HashCode.Combine(Struct);
    public override string ToString()
    {
        StringBuilder result = new();
        result.Append(Struct.Identifier);

        if (!TypeArguments.IsEmpty)
        { result.Append($"<{string.Join(", ", TypeArguments.Values)}>"); }
        else if (Struct.Definition.Template is not null)
        { result.Append($"<{string.Join(", ", Struct.Definition.Template.Parameters)}>"); }

        return result.ToString();
    }
    public static CompiledStructTypeExpression CreateAnonymous(StructType type, ILocated location)
    {
        return new(
            type.Struct,
            type.File,
            type.TypeArguments?.Select(v => new KeyValuePair<string, CompiledTemplateArgumentExpression>(v.Key, v.Value switch
            {
                CompiledTemplateArgumentType w => new CompiledTemplateArgumentTypeExpression()
                {
                    Value = CreateAnonymous(w.Value, location),
                },
                CompiledTemplateArgumentValue w => throw new NotImplementedException(),
                _ => throw new UnreachableException(),
            })).ToImmutableDictionary() ?? ImmutableDictionary<string, CompiledTemplateArgumentExpression>.Empty,
            location.Location
        );
    }
}
