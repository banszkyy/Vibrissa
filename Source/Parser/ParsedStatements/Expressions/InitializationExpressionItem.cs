using LanguageCore.Tokenizing;

namespace LanguageCore.Parser.Statements;

public class InitializationExpressionItem : Statement
{
    public Token Operator { get; }
    public Token MemberIdentifier { get; }
    public Expression Value { get; }

    public override Position Position => new(Operator, MemberIdentifier, Value);

    public InitializationExpressionItem(
        Token @operator,
        Token memberIdentifier,
        Expression value,
        Uri file) : base(file)
    {
        Operator = @operator;
        MemberIdentifier = memberIdentifier;
        Value = value;
    }

    public override string ToString()
    {
        StringBuilder result = new();

        result.Append(MemberIdentifier);
        result.Append(' ');
        result.Append(Operator);
        result.Append(' ');

        if (result.Length + Value.ToString().Length > Stringify.CozyLength)
        { result.Append("..."); }
        else
        { result.Append(Value); }

        result.Append(Semicolon);
        return result.ToString();
    }
}
