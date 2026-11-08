using LanguageCore.Parser;
using LanguageCore.Tokenizing;

namespace LanguageCore.Compiler;

public class UnresolvedTemplateType : GeneralType,
    IEquatable<UnresolvedTemplateType>,
    IReferenceableTo<Token>
{
    public string Identifier { get; }
    public Token Definition { get; }
    public Uri File { get; }

    Token? IReferenceableTo<Token>.Reference
    {
        get => Definition;
        set => throw new InvalidOperationException();
    }

    public UnresolvedTemplateType(Token definition, Uri originalFile)
    {
        Identifier = definition.Content;
        Definition = definition;
        File = originalFile;
    }

    public override bool Equals(object? other) => Equals(other as UnresolvedTemplateType);
    public override bool Equals(GeneralType? other) => Equals(other as UnresolvedTemplateType);
    public bool Equals(UnresolvedTemplateType? other)
    {
        if (other is null) return false;
        if (!Identifier.Equals(other.Identifier)) return false;
        return true;
    }
    public override bool Equals(TypeInstance? other)
    {
        if (other is null) return false;

        if (other is not TypeInstanceSimple otherSimple)
        { return false; }

        if (TypeKeywords.BasicTypes.ContainsKey(otherSimple.Identifier.Content))
        { return false; }

        return false;
    }
    public override int GetHashCode() => HashCode.Combine(Identifier);
    public override string ToString() => Identifier;
}
