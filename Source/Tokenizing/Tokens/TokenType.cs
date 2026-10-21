namespace LanguageCore.Tokenizing;

public enum TokenType
{
    Whitespace,
    LineBreak,

    Identifier,

    LiteralNumber,
    LiteralHex,
    LiteralBinary,
    LiteralString,
    LiteralCharacter,
    LiteralFloat,

    Operator,

    PreprocessSkipped,

    Comment,
    CommentMultiline,
}
