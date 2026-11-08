using LanguageCore.Parser;

namespace LanguageCore.Compiler;

public class StructType : GeneralType,
    IEquatable<StructType>,
    IReferenceableTo<CompiledStruct>
{
    public CompiledStruct Struct { get; }
    public ImmutableDictionary<string, CompiledTemplateArgument> TemplateArguments { get; }
    public Uri File { get; }

    CompiledStruct? IReferenceableTo<CompiledStruct>.Reference
    {
        get => Struct;
        set => throw new InvalidOperationException();
    }

    public StructType(StructType other)
    {
        Struct = other.Struct;
        TemplateArguments = other.TemplateArguments;
        File = other.File;
    }

    public StructType(CompiledStruct @struct, Uri originalFile)
    {
        Struct = @struct;
        if (@struct.Definition.Template is not null)
        {
            TemplateArguments = @struct.Definition.Template.Parameters
                .Select(v => new KeyValuePair<string, CompiledTemplateArgument>(v.Content, new CompiledTemplateArgumentType()
                {
                    Definition = v,
                    Value = new UnresolvedTemplateType(v, originalFile),
                }))
                .ToImmutableDictionary();
        }
        else
        { TemplateArguments = ImmutableDictionary<string, CompiledTemplateArgument>.Empty; }
        File = originalFile;
    }

    public StructType(CompiledStruct @struct, Uri originalFile, IReadOnlyList<CompiledTemplateArgument> templateArguments)
    {
        Struct = @struct;
        if (@struct.Definition.Template is not null)
        {
            Dictionary<string, CompiledTemplateArgument> result = new(@struct.Definition.Template.Parameters.Length);

            if (@struct.Definition.Template.Parameters.Length != templateArguments.Count)
            { throw new InternalExceptionWithoutContext($"Wrong number of template arguments defined: expected {@struct.Definition.Template.Parameters.Length} passed {templateArguments.Count}"); }

            for (int i = 0; i < @struct.Definition.Template.Parameters.Length; i++)
            { result.Add(@struct.Definition.Template.Parameters[i].Content, templateArguments[i]); }

            TemplateArguments = result.ToImmutableDictionary();
        }
        else
        { TemplateArguments = ImmutableDictionary<string, CompiledTemplateArgument>.Empty; }
        File = originalFile;
    }

    public StructType(CompiledStruct @struct, Uri originalFile, ImmutableDictionary<string, CompiledTemplateArgument> templateArguments)
    {
        Struct = @struct;
        TemplateArguments = templateArguments;
        File = originalFile;
    }

    public bool GetField(string name, [NotNullWhen(true)] out CompiledField? field, [NotNullWhen(false)] out PossibleDiagnostic? error)
    {
        foreach (CompiledField _field in Struct.Fields)
        {
            if (_field.Identifier == name)
            {
                field = _field;
                error = null;
                return true;
            }
        }

        field = null;
        error = new PossibleDiagnostic($"Field `{name}` not found in struct `{Struct.Identifier}`", isTellingNothing: true);
        return false;
    }

    public GeneralType ReplaceType(GeneralType type, out PossibleDiagnostic? error)
    {
        error = null;

        if (!type.Is(out UnresolvedTemplateType? templateType))
        { return type; }

        if (!TemplateArguments.TryGetValue(templateType.Identifier, out CompiledTemplateArgument? result))
        {
            error = new PossibleDiagnostic($"Template parameter `{templateType.Identifier}` not found", isTellingNothing: true);
            return type;
        }

        if (result is not CompiledTemplateArgumentType w) throw new NotImplementedException();

        return w.Value;
    }

    public override bool Equals(object? other) => Equals(other as StructType);
    public override bool Equals(GeneralType? other) => Equals(other as StructType);
    public bool Equals(StructType? other)
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

        if (Struct.Definition.Template is not null)
        { result.Append($"<{string.Join(", ", Struct.Definition.Template.Parameters)}>"); }

        return result.ToString();
    }
}
