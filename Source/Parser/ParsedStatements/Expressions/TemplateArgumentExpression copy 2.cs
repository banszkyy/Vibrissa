namespace LanguageCore.Parser.Statements;

public class TemplateArgumentExpressionValue : TemplateArgumentExpression
{
    public Expression Value { get; }

    public override Position Position => Value.Position;

    public TemplateArgumentExpressionValue(Expression value, Uri file) : base(file)
    {
        Value = value;
    }

    public override string ToString() => Value.ToString();
}
