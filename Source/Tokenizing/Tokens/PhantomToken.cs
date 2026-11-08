namespace LanguageCore.Tokenizing;

public class PhantomToken : Token
{
    internal PhantomToken(string content) : base(TokenType.Identifier, content, true, Position.UnknownPosition) { }
    internal PhantomToken(string content, TokenType type) : base(type, content, true, Position.UnknownPosition) { }
    internal PhantomToken(string content, TokenType type, Position position) : base(type, content, true, position) { }
}
