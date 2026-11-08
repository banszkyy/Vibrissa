using LanguageCore.Parser.Statements;
using LanguageCore.Tokenizing;

namespace LanguageCore.Parser;

public class TemplateInfo : IPositioned
{
    public TokenPair Brackets { get; }
    public ImmutableArray<Token> Parameters { get; }

    public Position Position =>
        new Position(Parameters.As<IPositioned>().DefaultIfEmpty(Brackets))
        .Union(Brackets);

    public TemplateInfo(TokenPair brackets, ImmutableArray<Token> parameters)
    {
        Brackets = brackets;
        Parameters = parameters;
    }

    public bool TryGetTemplateArgumentIndex(string templateArgumentName, out int index)
    {
        index = -1;
        for (int i = 0; i < Parameters.Length; i++)
        {
            if (Parameters[i].Content == templateArgumentName)
            {
                index = i;
                return true;
            }
        }
        return false;
    }

    public bool OrderTemplateArguments<TValue>(ImmutableDictionary<string, TValue> templateArguments, [NotNullWhen(true)] out ImmutableArray<TValue> result)
    {
        TValue[] _result = new TValue[Parameters.Length];

        foreach (KeyValuePair<string, TValue> item in templateArguments)
        {
            if (!TryGetTemplateArgumentIndex(item.Key, out int i))
            {
                result = default;
                return false;
            }

            _result[i] = item.Value;
        }

        result = _result.AsImmutableUnsafe();
        return true;
    }

    public override string ToString() => $"{Brackets.Start}{string.Join(", ", Parameters)}{Brackets.End}";
}
