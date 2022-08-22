Imports System.Globalization
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports CA = System.Diagnostics.CodeAnalysis

Public Module VisualBasicUtilities

    Private ReadOnly _keywords As IReadOnlyCollection(Of String) =
        {
            "AddHandler",
            "AddressOf",
            "Alias",
            "And",
            "AndAlso",
            "As",
            "Boolean",
            "ByRef",
            "Byte",
            "ByVal",
            "Call",
            "Case",
            "Catch",
            "CBool",
            "CByte",
            "CChar",
            "CDate",
            "CDbl",
            "CDec",
            "Char",
            "CInt",
            "Class",
            "CLng",
            "CObj",
            "Const",
            "Continue",
            "CSByte",
            "CShort",
            "CSng",
            "CStr",
            "CType",
            "CUInt",
            "CULng",
            "CUShort",
            "Date",
            "Decimal",
            "Declare",
            "Default",
            "Delegate",
            "Dim",
            "DirectCast",
            "Do",
            "Double",
            "Each",
            "Else",
            "ElseIf",
            "End",
            "EndIf",
            "Enum",
            "Erase",
            "Error",
            "Event",
            "Exit",
            "False",
            "Finally",
            "For",
            "Friend",
            "Function",
            "Get",
            "GetType",
            "GetXMLNamespace",
            "Global",
            "GoSub",
            "GoTo",
            "Handles",
            "If",
            "Implements",
            "Imports",
            "In",
            "Inherits",
            "Integer",
            "Interface",
            "Is",
            "IsNot",
            "Let",
            "Lib",
            "Like",
            "Long",
            "Loop",
            "Me",
            "Mod",
            "Module",
            "MustInherit",
            "MustOverride",
            "MyBase",
            "MyClass",
            "NameOf",
            "Namespace",
            "Narrowing",
            "New",
            "Next",
            "Not",
            "Nothing",
            "NotInheritable",
            "NotOverridable",
            "Object",
            "Of",
            "On",
            "Operator",
            "Option",
            "Optional",
            "Or",
            "OrElse",
            "Overloads",
            "Overridable",
            "Overrides",
            "ParamArray",
            "Partial",
            "Private",
            "Property",
            "Protected",
            "Public",
            "RaiseEvent",
            "ReadOnly",
            "ReDim",
            "REM",
            "RemoveHandler",
            "Resume",
            "Return",
            "SByte",
            "Select",
            "Set",
            "Shadows",
            "Shared",
            "Short",
            "Single",
            "Static",
            "Step",
            "Stop",
            "String",
            "Structure",
            "Sub",
            "SyncLock",
            "Then",
            "Throw",
            "To",
            "True",
            "Try",
            "TryCast",
            "TypeOf",
            "UInteger",
            "ULong",
            "UShort",
            "Using",
            "Variant",
            "Wend",
            "When",
            "While",
            "Widening",
            "With",
            "WithEvents",
            "WriteOnly",
            "Xor"
        }

    Friend ReadOnly _builtInTypeNames As IReadOnlyDictionary(Of Type, String) = New Dictionary(Of Type, String) From
        {
            {GetType(Boolean), "Boolean"},
            {GetType(Byte), "Byte"},
            {GetType(SByte), "SByte"},
            {GetType(Char), "Char"},
            {GetType(Short), "Short"},
            {GetType(Integer), "Integer"},
            {GetType(Long), "Long"},
            {GetType(UShort), "UShort"},
            {GetType(UInteger), "UInteger"},
            {GetType(ULong), "ULong"},
            {GetType(Decimal), "Decimal"},
            {GetType(Single), "Single"},
            {GetType(Double), "Double"},
            {GetType(String), "String"},
            {GetType(Date), "Date"},
            {GetType(Object), "Object"}
        }

    Public Function IsVisualBasicKeyword(identifier As String) As Boolean
        Return _keywords.Contains(identifier, StringComparer.OrdinalIgnoreCase)
    End Function

    Public Function IsIdentifierStartCharacter(ch As Char) As Boolean
        If ch < "a"c Then
            If ch < "A"c Then
                Return False
            End If

            Return ch <= "Z"c OrElse ch = "_"c
        End If

        If ch <= "z"c Then
            Return True
        End If

        If ch <= Convert.ToChar(&H7F) Then 'max ASCII
            Return False
        End If

        Return IsLetterChar(CharUnicodeInfo.GetUnicodeCategory(ch))
    End Function

    Public Function IsIdentifierPartCharacter(ch As Char) As Boolean
        If ch < "a"c Then
            If ch < "A"c Then
                Return ch >= "0"c AndAlso ch <= "9"c
            End If

            Return ch <= "Z"c OrElse ch = "_"c
        End If

        If ch <= "z"c Then
            Return True
        End If

        If ch <= Convert.ToChar(&H7F) Then 'max ASCII
            Return False
        End If

        Dim category = CharUnicodeInfo.GetUnicodeCategory(ch)
        If IsLetterChar(category) Then
            Return True
        End If

        Return category = UnicodeCategory.DecimalDigitNumber OrElse
               category = UnicodeCategory.ConnectorPunctuation OrElse
               category = UnicodeCategory.NonSpacingMark OrElse
               category = UnicodeCategory.SpacingCombiningMark OrElse
               category = UnicodeCategory.Format
    End Function

    Public Function IsLetterChar(category As UnicodeCategory) As Boolean
        Return category = UnicodeCategory.UppercaseLetter OrElse
               category = UnicodeCategory.LowercaseLetter OrElse
               category = UnicodeCategory.TitlecaseLetter OrElse
               category = UnicodeCategory.ModifierLetter OrElse
               category = UnicodeCategory.OtherLetter OrElse
               category = UnicodeCategory.LetterNumber
    End Function

    Public Function RemoveRootNamespaceFromNamespace(rootNamespace As String, FullNamespace As String) As String
        If String.IsNullOrWhiteSpace(rootNamespace) Then Return FullNamespace

        Dim finalNamespace = FullNamespace

        If FullNamespace.StartsWith(rootNamespace, StringComparison.OrdinalIgnoreCase) Then
            If FullNamespace.Length > rootNamespace.Length Then
                finalNamespace = FullNamespace.Substring(rootNamespace.Length + 1)
            Else
                finalNamespace = String.Empty
            End If
        End If

        Return finalNamespace
    End Function

#Region "Guards"
    <DebuggerStepThrough>
    Public Function NotNull(Of T)(value As T, parameterName As String) As T
        If value Is Nothing Then
            Throw New ArgumentNullException(parameterName)
        End If

        Return value
    End Function

    <DebuggerStepThrough>
    Public Function NotEmpty(Of T)(value As IReadOnlyList(Of T), parameterName As String) As IReadOnlyList(Of T)

        NotNull(value, parameterName)

        If Not value.Any() Then
            Throw New ArgumentException(AbstractionsStrings.CollectionArgumentIsEmpty(parameterName))
        End If

        Return value
    End Function

    <DebuggerStepThrough>
    Public Function NotEmpty(value As String, parameterName As String) As String

        NotNull(value, parameterName)

        If String.IsNullOrWhiteSpace(value) Then
            Throw New ArgumentException(AbstractionsStrings.ArgumentIsEmpty(parameterName))
        End If

        Return value
    End Function

    <DebuggerStepThrough>
    Public Function NullButNotEmpty(value As String, parameterName As String) As String
        If value IsNot Nothing AndAlso value.Length = 0 Then
            NotEmpty(parameterName, NameOf(parameterName))
            Throw New ArgumentException(AbstractionsStrings.ArgumentIsEmpty(parameterName))
        End If

        Return value
    End Function

    <Conditional("DEBUG")>
    Public Sub DebugAssert(<CA.DoesNotReturnIf(False)> condition As Boolean, message As String)
        If Not condition Then
            Throw New Exception($"Check.DebugAssert failed: {message}")
        End If
    End Sub

#End Region

End Module
