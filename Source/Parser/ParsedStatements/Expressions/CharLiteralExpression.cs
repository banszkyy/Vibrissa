using LanguageCore.Tokenizing;

namespace LanguageCore.Parser.Statements;

public class CharLiteralExpression : LiteralExpression
{
    public char Value { get; }

    public override LiteralType Type => LiteralType.Char;

    public CharLiteralExpression(char value, Token valueToken, Uri file) : base(valueToken, file)
    {
        Value = value;
    }

    public override string ToString()
    {
        StringBuilder result = new();
        result.Append(SurroundingBrackets?.Start);

        result.Append($"'{Value.Escape()}'");

        result.Append(SurroundingBrackets?.End);
        result.Append(Semicolon);
        return result.ToString();
    }
}
