using LanguageCore.Parser.Statements;

namespace LanguageCore.Parser;

public class ParameterDefinitionCollection :
    IPositioned,
    IInFile
{
    public TokenPair Brackets { get; }
    public Uri File { get; }

    public int Length => Parameters.Length;
    public Position Position =>
        new Position(Brackets)
        .Union(Parameters);

    public ParameterDefinition this[int index] => Parameters[index];
    public ParameterDefinition this[Index index] => Parameters[index];

    public readonly ImmutableArray<ParameterDefinition> Parameters;

    public ParameterDefinitionCollection(ParameterDefinitionCollection other)
    {
        Parameters = other.Parameters;
        Brackets = other.Brackets;
        File = other.File;
    }

    public ParameterDefinitionCollection(ImmutableArray<ParameterDefinition> parameterDefinitions, TokenPair brackets, Uri file)
    {
        Parameters = parameterDefinitions;
        Brackets = brackets;
        File = file;
    }

    public override string ToString()
    {
        StringBuilder result = new();

        result.Append(Brackets.Start);

        for (int i = 0; i < Parameters.Length; i++)
        {
            if (i > 0) result.Append(", ");
            if (Parameters[i].Modifiers.Length > 0)
            {
                result.AppendJoin(", ", Parameters[i].Modifiers);
                result.Append(' ');
            }
            result.Append(Parameters[i].Type);
        }

        result.Append(Brackets.End);

        return result.ToString();
    }
}
