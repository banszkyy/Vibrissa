using LanguageCore.Parser;

namespace LanguageCore.Compiler;

public class CompiledFunctionDefinition :
    ICompiledDefinition<FunctionDefinition>,
    ICompiledDefinition<FunctionThingDefinition>,
    IReferenceable<ILocated>,
    IInContext<CompiledStruct?>,
    ICompiledFunctionDefinition,
    IExternalFunctionDefinition,
    IExposeable,
    IIdentifiable<string>
{
    public bool IsMsilCompatible { get; set; } = true;

    public FunctionDefinition Definition { get; }
    public GeneralType Type { get; }
    public ImmutableArray<CompiledParameter> Parameters { get; }
    public CompiledStruct? Context { get; }
    public List<Reference<ILocated>> References { get; }

    FunctionThingDefinition ICompiledDefinition<FunctionThingDefinition>.Definition => Definition;
    public bool ReturnSomething => !Type.SameAs(BasicType.Void);
    public Uri File => Definition.File;
    public Location Location => Definition.Location;
    public string? ExternalFunctionName => Definition.ExternalFunctionName;
    public string? ExposedFunctionName => Definition.ExposedFunctionName;
    public string Identifier => Definition.Identifier.Content;
    CanUseOn IHaveAttributes.AttributeUsageKind => (Definition as IHaveAttributes).AttributeUsageKind;
    public ImmutableArray<AttributeUsage> Attributes => Definition.Attributes;

    public CompiledFunctionDefinition(GeneralType type, ImmutableArray<CompiledParameter> parameters, CompiledStruct? context, FunctionDefinition functionDefinition)
    {
        Definition = functionDefinition;
        Type = type;
        Parameters = parameters;

        Context = context;
        References = new List<Reference<ILocated>>();
    }

    public CompiledFunctionDefinition(GeneralType type, ImmutableArray<CompiledParameter> parameters, CompiledFunctionDefinition other)
    {
        Definition = other.Definition;
        Type = type;
        Parameters = parameters;

        Context = other.Context;
        References = new List<Reference<ILocated>>(other.References);
    }

    public override string ToString()
    {
        StringBuilder result = new();
        if (Definition.IsExported)
        { result.Append("export "); }

        result.Append(Type);
        result.Append(' ');

        result.Append(Identifier);

        if (Definition.Template is not null)
        {
            result.Append('<');
            result.AppendJoin(", ", Definition.Template.Parameters.Select(v => v.Content));
            result.Append('>');
        }

        result.Append('(');
        if (Parameters.Length > 0)
        {
            for (int i = 0; i < Parameters.Length; i++)
            {
                if (i > 0) result.Append(", ");
                result.Append(Parameters[i].Type);
                result.Append(Parameters[i].Definition.SpreadOperator);
            }
        }
        result.Append(')');

        if (Definition.Block is not null)
        {
            result.Append(' ');
            result.Append(Definition.Block);
        }
        else
        { result.Append(';'); }

        return result.ToString();
    }

    public string ToReadable(IReadOnlyDictionary<string, CompiledTemplateArgument>? templateArguments = null)
    {
        StringBuilder result = new();
        result.Append(GeneralType.TryInsertTemplateParameters(Type, templateArguments).ToString());
        result.Append(' ');
        result.Append(Identifier);
        result.Append('(');
        for (int i = 0; i < Parameters.Length; i++)
        {
            if (i > 0) result.Append(", ");
            result.Append(GeneralType.TryInsertTemplateParameters(Parameters[i].Type, templateArguments).ToString());
            result.Append(Parameters[i].Definition.SpreadOperator);
        }
        result.Append(')');
        return result.ToString();
    }

    public static string ToReadable(string identifier, IEnumerable<string?>? parameters, string? returnType)
    {
        StringBuilder result = new();
        if (returnType is not null)
        {
            result.Append(returnType);
            result.Append(' ');
        }
        result.Append(identifier);
        if (parameters is not null)
        {
            result.Append('(');
            result.AppendJoin(", ", parameters.Select(v => v ?? "?"));
            result.Append(')');
        }
        return result.ToString();
    }

    string IReadable.ToReadable() => ToReadable(null);
}
