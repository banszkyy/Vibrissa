namespace LanguageCore.Tokenizing;

public enum TokenAnalyzedType
{
    None,
    Attribute,
    Type,
    TypeAlias,
    Struct,
    Keyword,
    FunctionName,
    VariableName,
    FieldName,
    ParameterName,
    Statement,
    BuiltinType,
    TemplateParameterType,
    TemplateParameterValue,
    ConstantName,
    MathOperator,
    OtherOperator,
    TypeModifier,
    InstructionLabel,
    Enum,
    EnumMember,
}
