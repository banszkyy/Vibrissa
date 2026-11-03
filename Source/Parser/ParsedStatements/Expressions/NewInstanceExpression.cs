using LanguageCore.Tokenizing;

namespace LanguageCore.Parser.Statements;

public class NewInstanceExpression :
    Expression,
    IHaveType,
    IInFile
{
    public Token Keyword { get; }
    public TypeInstance Type { get; }
    public InitializationExpression? Initialization { get; }

    public override Position Position => new(Keyword, Type);

    public NewInstanceExpression(
        Token keyword,
        TypeInstance type,
        InitializationExpression? initialization,
        Uri file) : base(file)
    {
        Keyword = keyword;
        Type = type;
        Initialization = initialization;
    }

    public override string ToString()
        => $"{SurroundingBrackets?.Start}{Keyword} {Type}{Initialization}{SurroundingBrackets?.End}{Semicolon}";
}
