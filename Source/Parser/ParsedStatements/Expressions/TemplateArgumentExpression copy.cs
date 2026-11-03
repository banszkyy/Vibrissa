namespace LanguageCore.Parser.Statements;

public class TemplateArgumentExpressionType : TemplateArgumentExpression
{
    public TypeInstance Value { get; }

    public override Position Position => Value.Position;

    public TemplateArgumentExpressionType(TypeInstance value, Uri file) : base(file)
    {
        Value = value;
    }

    public override string ToString() => Value.ToString();
}
