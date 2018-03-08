Imports System.Globalization
Imports System.Reflection
Imports System.Text
Imports System.Text.RegularExpressions
Imports Microsoft.EntityFrameworkCore.Design

Public Class VisualBasicUtilities
    Implements IVisualBasicUtilities

    Private Shared ReadOnly _cSharpKeywords As HashSet(Of String) = New HashSet(Of String) From {"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"}

    Private Shared ReadOnly _invalidCharsRegex As Regex = New Regex("[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]", Nothing, TimeSpan.FromMilliseconds(1000))

    Public Overridable Function DelimitString(value As String) As String Implements IVisualBasicUtilities.DelimitString
        Return """" & EscapeString(value) & """"
    End Function

    Public Overridable Function EscapeString(str As String) As String Implements IVisualBasicUtilities.EscapeString
        Return str.Replace("""", """""").Replace(vbTab, """ & vbTab & """)
    End Function

    Public Overridable Function GenerateLiteral(value As Byte()) As String Implements IVisualBasicUtilities.GenerateLiteral
        Return "{" & String.Join(", ", $"CByte({value})") & "}"
    End Function

    Public Overridable Function GenerateLiteral(value As Boolean) As String Implements IVisualBasicUtilities.GenerateLiteral
        Return If(value, "True", "False")
    End Function

    Public Overridable Function GenerateLiteral(value As Integer) As String Implements IVisualBasicUtilities.GenerateLiteral
        Return value.ToString(CultureInfo.InvariantCulture)
    End Function

    Public Overridable Function GenerateLiteral(value As Long) As String Implements IVisualBasicUtilities.GenerateLiteral
        Return value.ToString(CultureInfo.InvariantCulture) & "L"
    End Function

    Public Overridable Function GenerateLiteral(value As Decimal) As String Implements IVisualBasicUtilities.GenerateLiteral
        Return value.ToString(CultureInfo.InvariantCulture) & "m"
    End Function

    Public Overridable Function GenerateLiteral(value As Single) As String Implements IVisualBasicUtilities.GenerateLiteral
        Return value.ToString(CultureInfo.InvariantCulture) & "f"
    End Function

    Public Overridable Function GenerateLiteral(value As Double) As String Implements IVisualBasicUtilities.GenerateLiteral
        Return value.ToString(CultureInfo.InvariantCulture) & "D"
    End Function

    Public Overridable Function GenerateLiteral(value As TimeSpan) As String Implements IVisualBasicUtilities.GenerateLiteral
        Return "New TimeSpan(" & value.Ticks & ")"
    End Function

    Public Overridable Function GenerateLiteral(value As DateTime) As String Implements IVisualBasicUtilities.GenerateLiteral
        Return "New DateTime(" & value.Ticks & ", DateTimeKind." + [Enum].GetName(GetType(DateTimeKind), value.Kind) & ")"
    End Function

    Public Overridable Function GenerateLiteral(value As DateTimeOffset) As String Implements IVisualBasicUtilities.GenerateLiteral
        Return "New DateTimeOffset(" & value.Ticks & ", " & GenerateLiteral(value.Offset) & ")"
    End Function

    Public Overridable Function GenerateLiteral(value As Guid) As String Implements IVisualBasicUtilities.GenerateLiteral
        Return "New Guid(" & GenerateLiteral(value.ToString()) & ")"
    End Function

    Public Overridable Function GenerateLiteral(value As String) As String Implements IVisualBasicUtilities.GenerateLiteral
        Return """" & EscapeString(value) & """"
    End Function

    Public Overridable Function GenerateLiteral(value As Object) As String Implements IVisualBasicUtilities.GenerateLiteral
        If value.[GetType]().GetTypeInfo().IsEnum Then
            Return value.[GetType]().Name & "." + [Enum].Format(value.[GetType](), value, "G")
        End If

        Return String.Format(CultureInfo.InvariantCulture, "{0}", value)
    End Function

    Public Overridable Function GenerateLiteral(value As NestedClosureCodeFragment) As String
        Return "Function(" + value.Parameter + ") " + value.Parameter & Generate(value.MethodCall)
    End Function

    Public Overridable Function IsCSharpKeyword(identifier As String) As Boolean Implements IVisualBasicUtilities.IsCSharpKeyword
        Return _cSharpKeywords.Contains(identifier)
    End Function

    Public Overridable Function GenerateCSharpIdentifier(identifier As String, existingIdentifiers As ICollection(Of String), singularizePluralizer As Func(Of String, String)) As String Implements IVisualBasicUtilities.GenerateCSharpIdentifier
        Return GenerateCSharpIdentifier(identifier, existingIdentifiers, singularizePluralizer, AddressOf Uniquifier)
    End Function

    Public Overridable Function GenerateCSharpIdentifier(identifier As String, existingIdentifiers As ICollection(Of String), singularizePluralizer As Func(Of String, String), uniquifier As Func(Of String, ICollection(Of String), String)) As String Implements IVisualBasicUtilities.GenerateCSharpIdentifier
        Dim proposedIdentifier = If(identifier.Length > 1 AndAlso identifier(0) = "@"c, "@" & _invalidCharsRegex.Replace(identifier.Substring(1), "_"), _invalidCharsRegex.Replace(identifier, "_"))
        If String.IsNullOrEmpty(proposedIdentifier) Then
            proposedIdentifier = "_"
        End If

        Dim firstChar = proposedIdentifier(0)
        If Not Char.IsLetter(firstChar) AndAlso firstChar <> "_"c AndAlso firstChar <> "@"c Then
            proposedIdentifier = "_" & proposedIdentifier
        ElseIf IsCSharpKeyword(proposedIdentifier) Then
            proposedIdentifier = "_" & proposedIdentifier
        End If

        If singularizePluralizer IsNot Nothing Then
            proposedIdentifier = singularizePluralizer(proposedIdentifier)
        End If

        Return uniquifier(proposedIdentifier, existingIdentifiers)
    End Function

    Public Overridable Function Uniquifier(proposedIdentifier As String, existingIdentifiers As ICollection(Of String)) As String Implements IVisualBasicUtilities.Uniquifier
        If existingIdentifiers Is Nothing Then
            Return proposedIdentifier
        End If

        Dim finalIdentifier = proposedIdentifier
        Dim suffix = 1
        While existingIdentifiers.Contains(finalIdentifier)
            finalIdentifier = proposedIdentifier & suffix
            suffix += 1
        End While

        Return finalIdentifier
    End Function

    Private Shared ReadOnly _primitiveTypeNames As Dictionary(Of Type, String) = New Dictionary(Of Type, String) From {{GetType(Boolean), "bool"}, {GetType(Byte), "byte"}, {GetType(Byte()), "byte[]"}, {GetType(SByte), "sbyte"}, {GetType(Short), "short"}, {GetType(UShort), "ushort"}, {GetType(Integer), "int"}, {GetType(UInteger), "uint"}, {GetType(Long), "long"}, {GetType(ULong), "ulong"}, {GetType(Char), "char"}, {GetType(Single), "float"}, {GetType(Double), "double"}, {GetType(String), "string"}, {GetType(Decimal), "decimal"}}

    Public Overridable Function GetTypeName(type As Type) As String Implements IVisualBasicUtilities.GetTypeName
        If _builtinTypes.ContainsKey(type) Then
            Return _builtinTypes(type)
        End If

        If type.IsArray Then
            Return GetTypeName(type.GetElementType()) & "()"
        End If

        If type.GetTypeInfo().IsGenericType Then
            If type.GetGenericTypeDefinition() = GetType(Nullable(Of )) Then
                Return GetTypeName(Nullable.GetUnderlyingType(type)) & "?"c
            End If

            Dim genericTypeDefName = type.Name.Substring(0, type.Name.IndexOf("`"c))
            Dim genericTypeArguments = String.Join(", ", type.GenericTypeArguments.[Select](AddressOf GetTypeName))
            Return $"{genericTypeDefName}(Of {genericTypeArguments})"
        End If

        Dim typeName As String = Nothing
        Return If(_primitiveTypeNames.TryGetValue(type, typeName), typeName, type.Name)
    End Function

    Dim _builtinTypes As IDictionary(Of Type, String) = New Dictionary(Of Type, String) From {
        {GetType(Boolean), "Boolean"},
        {GetType(Integer), "Integer"},
        {GetType(UInteger), "UInteger"},
        {GetType(Short), "Short"},
        {GetType(UShort), "UShort"},
        {GetType(Long), "Long"},
        {GetType(ULong), "ULong"},
        {GetType(Decimal), "Decimal"},
        {GetType(Single), "Single"},
        {GetType(String), "String"},
        {GetType(SByte), "SByte"},
        {GetType(Byte), "Byte"},
        {GetType(Char), "Char"},
        {GetType(Double), "Double"},
        {GetType(Object), "Object"}
    }

    Public Overridable Function IsValidIdentifier(name As String) As Boolean Implements IVisualBasicUtilities.IsValidIdentifier
        If String.IsNullOrEmpty(name) Then
            Return False
        End If

        If Not IsIdentifierStartCharacter(name(0)) Then
            Return False
        End If

        Dim nameLength = name.Length
        For i = 1 To nameLength - 1
            If Not IsIdentifierPartCharacter(name(i)) Then
                Return False
            End If
        Next

        Return True
    End Function

    Public Overridable Function Generate(methodCallCodeFragment As MethodCallCodeFragment) As String Implements IVisualBasicUtilities.Generate
        Dim builder = New StringBuilder()
        Dim current = methodCallCodeFragment
        While current IsNot Nothing
            builder.Append(".").Append(current.Method).Append("(")
            For i = 0 To current.Arguments.Count - 1
                If i <> 0 Then
                    builder.Append(", ")
                End If

                Dim method = GetType(VisualBasicUtilities).GetMethod(NameOf(GenerateLiteral), BindingFlags.Instance Or BindingFlags.Public, Nothing, New Type() {current.Arguments(i).GetType()}, Nothing)

                If Not method Is Nothing Then
                    builder.Append(CStr(method.Invoke(Me, {current.Arguments(i)})))
                Else
                    Throw New ArgumentException("Type of argument is not supported")
                End If
            Next

            builder.Append(")")
            current = current.ChainedCall
        End While

        Return builder.ToString()
    End Function

    Private Shared Function IsIdentifierStartCharacter(ch As Char) As Boolean
        If ch < "a"c Then
            If ch < "A"c Then
                Return False
            End If

            Return ch <= "Z"c OrElse ch = "_"c
        End If

        If ch <= "z"c Then
            Return True
        End If

        If ch <= ChrW(127) Then
            Return False
        End If

        Return IsLetterChar(CharUnicodeInfo.GetUnicodeCategory(ch))
    End Function

    Private Shared Function IsIdentifierPartCharacter(ch As Char) As Boolean
        If ch < "a"c Then
            If ch < "A"c Then
                Return ch >= "0"c AndAlso ch <= "9"c
            End If

            Return ch <= "Z"c OrElse ch = "_"c
        End If

        If ch <= "z"c Then
            Return True
        End If

        If ch <= ChrW(127) Then
            Return False
        End If

        Dim cat = CharUnicodeInfo.GetUnicodeCategory(ch)
        If IsLetterChar(cat) Then
            Return True
        End If

        Select Case cat
            Case UnicodeCategory.DecimalDigitNumber, UnicodeCategory.ConnectorPunctuation, UnicodeCategory.NonSpacingMark, UnicodeCategory.SpacingCombiningMark, UnicodeCategory.Format
                Return True
        End Select

        Return False
    End Function

    Private Shared Function IsLetterChar(cat As UnicodeCategory) As Boolean
        Select Case cat
            Case UnicodeCategory.UppercaseLetter, UnicodeCategory.LowercaseLetter, UnicodeCategory.TitlecaseLetter, UnicodeCategory.ModifierLetter, UnicodeCategory.OtherLetter, UnicodeCategory.LetterNumber
                Return True
        End Select

        Return False
    End Function
End Class