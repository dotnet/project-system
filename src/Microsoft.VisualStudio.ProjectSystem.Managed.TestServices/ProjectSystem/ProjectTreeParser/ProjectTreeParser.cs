// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using static Microsoft.VisualStudio.ProjectSystem.Tokenizer;

namespace Microsoft.VisualStudio.ProjectSystem
{
    // Parses a string into a project tree
    //
    internal partial class ProjectTreeParser
    {
        private readonly Tokenizer _tokenizer;
        private int _indentLevel;

        public ProjectTreeParser(string value)
        {
            Requires.NotNullOrEmpty(value, nameof(value));

            _tokenizer = new Tokenizer(new StringReader(value), Delimiters.Structural);
        }

        public static IProjectTree Parse(string value)
        {
            value = value.Trim(new char[] { '\r', '\n' });

            ProjectTreeParser parser = new ProjectTreeParser(value);

            return parser.Parse();
        }

        public IProjectTree Parse()
        {
            MutableProjectTree root = ReadProjectRoot();

            _tokenizer.Close();

            return root;
        }

        private MutableProjectTree ReadProjectRoot()
        {
            // We always start with the root, with zero indent
            MutableProjectTree root = ReadProjectItem();
            MutableProjectTree current = root;
            do
            {
                current = ReadNextProjectItem(current);

            } while (current != null);

            return root;
        }

        private MutableProjectTree ReadNextProjectItem(MutableProjectTree current)
        {
            if (!TryReadNewLine())
                return null;

            MutableProjectTree parent = current;

            int previousIndentLevel;
            int indent = ReadIndentLevel(out previousIndentLevel);

            while (indent <= previousIndentLevel)
            {
                parent = parent.Parent;
                indent++;
            }

            if (parent == null)
#pragma warning disable IDE0016 // Use 'throw' expression
                throw _tokenizer.FormatException(ProjectTreeFormatError.MultipleRoots, "Encountered another project root, when tree can only have one.");
#pragma warning restore IDE0016 // Use 'throw' expression

            var tree = ReadProjectItem();
            tree.Parent = parent;
            parent.Children.Add(tree);
            return tree;
        }

        private int ReadIndentLevel(out int previousIndentLevel)
        {
            // Attempts to read the indent level of the current project item
            //
            // Root              <--- IndentLevel: 0
            //     Parent        <--- IndentLevel: 1
            //         Child     <--- IndentLevel: 2

            previousIndentLevel = _indentLevel;
            int indentLevel = 0;

            while (_tokenizer.Peek() == TokenType.WhiteSpace)
            {
                _tokenizer.Skip(TokenType.WhiteSpace);
                _tokenizer.Skip(TokenType.WhiteSpace);
                _tokenizer.Skip(TokenType.WhiteSpace);
                _tokenizer.Skip(TokenType.WhiteSpace);

                indentLevel++;
            }

            if (indentLevel > previousIndentLevel + 1)
                throw _tokenizer.FormatException(ProjectTreeFormatError.IndentTooManyLevels, "Project item has been indented too many levels");

            return _indentLevel = indentLevel;
        }

        private bool TryReadNewLine()
        {
            if (_tokenizer.SkipIf(TokenType.CarriageReturn))
            {   // If we read '\r', it must be followed by a '\n'

                _tokenizer.Skip(TokenType.NewLine);
                return true;
            }

            return _tokenizer.SkipIf(TokenType.NewLine);
        }

        private MutableProjectTree ReadProjectItem()
        {
            MutableProjectTree tree = new MutableProjectTree();
            ReadProjectItemProperties(tree);

            return tree;
        }

        private void ReadProjectItemProperties(MutableProjectTree tree)
        {   // Parse "Root (visibility: visible, flags: {ProjectRoot}), FilePath: "C:\My Project\MyFile.txt", Icon: {1B5CF1ED-9525-42B4-85F0-2CB50530ECA9 1}, ExpandedIcon: {1B5CF1ED-9525-42B4-85F0-2CB50530ECA9 1}

            ReadCaption(tree);
            ReadProperties(tree);
            ReadFields(tree);
        }

        private void ReadCaption(MutableProjectTree tree)
        {
            Tokenizer tokenizer = Tokenizer(Delimiters.Caption);

            tree.Caption = tokenizer.ReadIdentifier(IdentifierParseOptions.Required);
        }

        private void ReadProperties(MutableProjectTree tree)
        {   // Parses "(visibility: visible, flags: {ProjectRoot})"

            // Properties section is optional
            if (!_tokenizer.SkipIf(TokenType.LeftParenthesis))
                return;

            // Empty properties
            if (_tokenizer.SkipIf(TokenType.RightParenthesis))
                return;

            ReadProperty(tree);

            while (_tokenizer.SkipIf(TokenType.Comma))
            {
                _tokenizer.Skip(TokenType.WhiteSpace);
                ReadProperty(tree);
            }

            _tokenizer.Skip(TokenType.RightParenthesis);
        }

        private void ReadProperty(MutableProjectTree tree)
        {
            Tokenizer tokenizer = Tokenizer(Delimiters.PropertyName);

            string propertyName = tokenizer.ReadIdentifier(IdentifierParseOptions.Required);

            switch (propertyName)
            {
                case "visibility":
                    tokenizer.Skip(TokenType.Colon);
                    tokenizer.Skip(TokenType.WhiteSpace);
                    ReadVisibility(tree);
                    break;

                case "flags":
                    tokenizer.Skip(TokenType.Colon);
                    tokenizer.Skip(TokenType.WhiteSpace);
                    ReadCapabilities(tree);
                    break;

                default:
                    throw _tokenizer.FormatException(ProjectTreeFormatError.UnrecognizedPropertyName, $"Expected 'visibility' or 'flags', but encountered '{propertyName}'.");
            }
        }

        private void ReadVisibility(MutableProjectTree tree)
        {   // Parse 'visible' in 'visibility:visible' or 'invisible' in 'visibility:invisible"

            Tokenizer tokenizer = Tokenizer(Delimiters.PropertyValue);

            string visibility = tokenizer.ReadIdentifier(IdentifierParseOptions.Required);

            switch (visibility)
            {
                case "visible":
                    tree.Visible = true;
                    break;
                case "invisible":
                    tree.Visible = false;
                    break;

                default:
                    throw _tokenizer.FormatException(ProjectTreeFormatError.UnrecognizedPropertyValue, $"Expected 'visible' or 'invisible', but encountered '{visibility}'.");
            }
        }

        private void ReadCapabilities(MutableProjectTree tree)
        {   // Parse '{ProjectRoot Folder}'

            Tokenizer tokenizer = Tokenizer(Delimiters.BracedPropertyValueBlock);
            tokenizer.Skip(TokenType.LeftBrace);

            // Empty flags
            if (tokenizer.SkipIf(TokenType.RightBrace))
                return;

            do
            {
                ReadFlag(tree);
            }
            while (tokenizer.SkipIf(TokenType.WhiteSpace));
            tokenizer.Skip(TokenType.RightBrace);
        }

        private void ReadFlag(MutableProjectTree tree)
        {   // Parses 'AppDesigner' in '{AppDesigner Folder}'

            Tokenizer tokenizer = Tokenizer(Delimiters.BracedPropertyValue);

            string flag = tokenizer.ReadIdentifier(IdentifierParseOptions.Required);
            tree.AddFlag(flag);
        }

        private void ReadFields(MutableProjectTree tree)
        {   // Parses ', FilePath: "C:\Temp\Foo"'

            // This section is optional
            while (_tokenizer.SkipIf(TokenType.Comma))
            {
                _tokenizer.Skip(TokenType.WhiteSpace);

                Tokenizer tokenizer = Tokenizer(Delimiters.PropertyName);

                string fieldName = tokenizer.ReadIdentifier(IdentifierParseOptions.Required);

                switch (fieldName)
                {
                    case "FilePath":
                        tokenizer.Skip(TokenType.Colon);
                        tokenizer.Skip(TokenType.WhiteSpace);
                        ReadFilePath(tree);
                        break;

                    case "Icon":
                        tokenizer.Skip(TokenType.Colon);
                        tokenizer.Skip(TokenType.WhiteSpace);
                        ReadIcon(tree, expandedIcon: false);
                        break;

                    case "ExpandedIcon":
                        tokenizer.Skip(TokenType.Colon);
                        tokenizer.Skip(TokenType.WhiteSpace);
                        ReadIcon(tree, expandedIcon: true);
                        break;

                    default:
                        throw _tokenizer.FormatException(ProjectTreeFormatError.UnrecognizedPropertyName, $"Expected 'FilePath', 'Icon' or 'ExpandedIcon', but encountered '{fieldName}'.");
                }
            }
        }

        private void ReadFilePath(MutableProjectTree tree)
        {   // Parses ': "C:\Temp\Foo"'

            tree.FilePath = ReadQuotedPropertyValue();
        }

        private string ReadQuotedPropertyValue()
        {   // Parses '"C:\Temp"'

            Tokenizer tokenizer = Tokenizer(Delimiters.QuotedPropertyValue);

            tokenizer.Skip(TokenType.Quote);

            string value = tokenizer.ReadIdentifier(IdentifierParseOptions.None);

            tokenizer.Skip(TokenType.Quote);

            return value;
        }

        private void ReadIcon(MutableProjectTree tree, bool expandedIcon)
        {   // Parses '{1B5CF1ED-9525-42B4-85F0-2CB50530ECA9 1}'

            Tokenizer tokenizer = Tokenizer(Delimiters.BracedPropertyValueBlock);
            tokenizer.Skip(TokenType.LeftBrace);

            // Empty icon
            if (tokenizer.SkipIf(TokenType.RightBrace))
                return;

            var moniker = ReadProjectImageMoniker();

            if (expandedIcon)
            {
                tree.ExpandedIcon = moniker;
            }
            else
            {
                tree.Icon = moniker;
            }
            
            tokenizer.Skip(TokenType.RightBrace);
        }

        private ProjectImageMoniker ReadProjectImageMoniker()
        {
            Tokenizer tokenizer = Tokenizer(Delimiters.BracedPropertyValue);

            string guidAsString = tokenizer.ReadIdentifier(IdentifierParseOptions.Required);

            Guid guid;
            if (!Guid.TryParseExact(guidAsString, "D", out guid))
                throw _tokenizer.FormatException(ProjectTreeFormatError.GuidExpected, $"Expected GUID, but encountered '{guidAsString}'");

            tokenizer.Skip(TokenType.WhiteSpace);

            string idAsString = tokenizer.ReadIdentifier(IdentifierParseOptions.Required);

            int id;
            if (!int.TryParse(idAsString, out id))
                throw _tokenizer.FormatException(ProjectTreeFormatError.IntegerExpected, $"Expected integer, but encountered '{idAsString}'");

            return new ProjectImageMoniker(guid, id);
        }

        private Tokenizer Tokenizer(ImmutableArray<TokenType> delimiters)
        {
            return new Tokenizer(_tokenizer.UnderlyingReader, delimiters);
        }
    }
}
