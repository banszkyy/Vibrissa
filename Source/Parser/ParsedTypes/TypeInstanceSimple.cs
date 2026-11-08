using LanguageCore.Compiler;
using LanguageCore.Parser.Statements;
using LanguageCore.Tokenizing;

namespace LanguageCore.Parser;

public class TypeInstanceSimple : TypeInstance,
    IEquatable<TypeInstanceSimple?>,
    IReferenceableTo<object>
{
    /// <summary> Set by the compiler </summary>
    public GeneralType? CompiledType { get; set; }

    /// <summary> Set by the compiler </summary>
    public CompiledTypeExpression? CompiledTypeExpression { get; set; }

    public Token Identifier { get; }
    TokenPair? TemplateArgumentsBrackets { get; }
    public ImmutableArray<TemplateArgumentExpression>? TemplateArguments { get; }
    public object? Reference { get; set; }

    public override Position Position => TemplateArguments is null
        ? Identifier.Position
        : new Position(Identifier).Union(TemplateArguments).Union(TemplateArgumentsBrackets);

    public TypeInstanceSimple(Token identifier, Uri file) : base(file)
    {
        Identifier = identifier;
        TemplateArguments = null;
        TemplateArgumentsBrackets = null;
    }

    public TypeInstanceSimple(Token identifier, Uri file, ImmutableArray<TemplateArgumentExpression> templateArguments, TokenPair templateArgumentsBrackets) : base(file)
    {
        Identifier = identifier;
        TemplateArguments = templateArguments;
        TemplateArgumentsBrackets = templateArgumentsBrackets;
    }

    public override bool Equals(object? obj) => obj is TypeInstanceSimple other && Equals(other);
    public override bool Equals(TypeInstance? other) => other is TypeInstanceSimple other_ && Equals(other_);
    public bool Equals(TypeInstanceSimple? other)
    {
        if (other is null) return false;
        if (Identifier.Content != other.Identifier.Content) return false;

        if (!TemplateArguments.HasValue) return other.TemplateArguments is null;
        if (!other.TemplateArguments.HasValue) return false;

        if (TemplateArguments.Value.Length != other.TemplateArguments.Value.Length) return false;
        for (int i = 0; i < TemplateArguments.Value.Length; i++)
        {
            if (!TemplateArguments.Value[i].Equals(other.TemplateArguments.Value[i]))
            { return false; }
        }
        return true;
    }

    public override int GetHashCode() => HashCode.Combine((byte)3, Identifier, TemplateArguments);

    public static TypeInstanceSimple CreateAnonymous(string name, Uri file)
        => new(new PhantomToken(name), file);

    public static TypeInstanceSimple CreateAnonymous(string name, Uri file, ImmutableArray<Token>? templateArguments)
    {
        if (templateArguments == null)
        {
            return new TypeInstanceSimple(new PhantomToken(name), file);
        }
        else
        {
            TemplateArgumentExpression[] templateTypesConverted = new TemplateArgumentExpression[templateArguments.Value.Length];
            for (int i = 0; i < templateArguments.Value.Length; i++)
            {
                templateTypesConverted[i] = new TemplateArgumentExpressionType(CreateAnonymous(templateArguments.Value[i].Content, file), file);
            }
            return new TypeInstanceSimple(new PhantomToken(name), file, templateTypesConverted.ToImmutableArray(), TokenPair.CreateAnonymous("<", ">"));
        }
    }

    public override string ToString()
    {
        if (TemplateArguments is null) return Identifier.Content;
        return $"{Identifier.Content}<{string.Join(", ", TemplateArguments)}>";
    }
    public override string ToString(IReadOnlyDictionary<string, CompiledTemplateArgument>? templateArguments)
    {
        string identifier = Identifier.Content;
        if (templateArguments is not null && templateArguments.TryGetValue(Identifier.Content, out CompiledTemplateArgument? replaced))
        { identifier = replaced.ToString(); }

        if (!TemplateArguments.HasValue)
        { return identifier; }

        StringBuilder result = new(identifier);
        result.Append('<');
        for (int i = 0; i < TemplateArguments.Value.Length; i++)
        {
            if (i > 0) result.Append(", ");
            result.Append(TemplateArguments.Value[i] switch
            {
                TemplateArgumentExpressionType v => v.Value.ToString(templateArguments),
                TemplateArgumentExpressionValue v => v.Value.ToString(),
                _ => throw new UnreachableException(),
            });
        }
        result.Append('>');
        return result.ToString();
    }
}
