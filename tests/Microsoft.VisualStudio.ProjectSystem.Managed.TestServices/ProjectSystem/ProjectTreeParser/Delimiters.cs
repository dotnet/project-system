// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using static Microsoft.VisualStudio.ProjectSystem.Tokenizer;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal static class Delimiters
    {
        public static readonly ImmutableArray<TokenType> QuotedPropertyValue = ImmutableArray.Create(TokenType.Quote, TokenType.CarriageReturn, TokenType.NewLine);
        public static readonly ImmutableArray<TokenType> BracedPropertyValueBlock = ImmutableArray.Create(TokenType.RightBrace, TokenType.LeftBrace, TokenType.WhiteSpace, TokenType.CarriageReturn, TokenType.NewLine);
        public static readonly ImmutableArray<TokenType> BracedPropertyValue = ImmutableArray.Create(TokenType.RightBrace, TokenType.WhiteSpace, TokenType.CarriageReturn, TokenType.NewLine);
        public static readonly ImmutableArray<TokenType> Caption = ImmutableArray.Create(TokenType.LeftParenthesis, TokenType.Comma, TokenType.CarriageReturn, TokenType.NewLine);
        public static readonly ImmutableArray<TokenType> PropertyName = ImmutableArray.Create(TokenType.Colon, TokenType.CarriageReturn, TokenType.NewLine, TokenType.WhiteSpace);
        public static readonly ImmutableArray<TokenType> PropertyValue = ImmutableArray.Create(TokenType.WhiteSpace, TokenType.Comma, TokenType.RightParenthesis, TokenType.CarriageReturn, TokenType.NewLine);
        public static readonly ImmutableArray<TokenType> Structural = ImmutableArray.Create(TokenType.Comma, TokenType.LeftParenthesis, TokenType.RightParenthesis, TokenType.WhiteSpace, TokenType.NewLine, TokenType.CarriageReturn);
    }
}
