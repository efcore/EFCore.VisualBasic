Imports System.Text
Imports System
Imports IndentedStringBuilder = Microsoft.EntityFrameworkCore.Internal.IndentedStringBuilder
Imports Microsoft.EntityFrameworkCore.Utilities
Imports System.Collections.ObjectModel
Imports System.Globalization
Imports Bricelam.EntityFrameworkCore.VisualBasic

''' <summary>
'''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
'''     directly from your code. This API may change Or be removed in future releases.
''' </summary>
Public Class VisualBasicHelper
    Implements IVisualBasicHelper

    Private Shared ReadOnly _builtInTypes As IReadOnlyDictionary(Of Type, String) = New Dictionary(Of Type, String) From
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
        {GetType(Object), "Object"}
    }

    Private Shared ReadOnly _keywords As IReadOnlyCollection(Of String) =
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
            "Char", ''
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
            "Or", ''
            "OrElse",
            "Out",
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

    Private Shared ReadOnly _literalFuncs As IReadOnlyDictionary(Of Type, Func(Of IVisualBasicHelper, Object, String)) =
            New Dictionary(Of Type, Func(Of IVisualBasicHelper, Object, String)) From
            {
                {GetType(Boolean), Function(c, v) c.Literal(CBool(v))},
                {GetType(Byte), Function(c, v) c.Literal(CByte(v))},
                {GetType(Byte()), Function(c, v) c.Literal(CType(v, Byte()))},
                {GetType(Char), Function(c, v) c.Literal(CChar(v))},
                {GetType(DateTime), Function(c, v) c.Literal(CDate(v))},
                {GetType(DateTimeOffset), Function(c, v) c.Literal(CType(v, DateTimeOffset))},
                {GetType(Decimal), Function(c, v) c.Literal(CDec(v))},
                {GetType(Double), Function(c, v) c.Literal(CDbl(v))},
                {GetType(Single), Function(c, v) c.Literal(CSng(v))},
                {GetType(Guid), Function(c, v) c.Literal(CType(v, Guid))},
                {GetType(Integer), Function(c, v) c.Literal(CInt(v))},
                {GetType(Long), Function(c, v) c.Literal(CLng(v))},
                {GetType(SByte), Function(c, v) c.Literal(CSByte(v))},
                {GetType(Short), Function(c, v) c.Literal(CShort(v))},
                {GetType(String), Function(c, v) c.Literal(CStr(v))},
                {GetType(TimeSpan), Function(c, v) c.Literal(CType(v, TimeSpan))},
                {GetType(UInteger), Function(c, v) c.Literal(CUInt(v))},
                {GetType(ULong), Function(c, v) c.Literal(CULng(v))},
                {GetType(UShort), Function(c, v) c.Literal(CUShort(v))}
            }

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Lambda(properties As IReadOnlyList(Of String)) As String Implements IVisualBasicHelper.Lambda

        Dim builder = New StringBuilder()
        builder.Append("Function(x) ")

        If properties.Count = 1 Then
            builder.Append("x.") _
                   .Append(properties(0))
        Else
            builder.Append("New With {")
            builder.Append(String.Join(", ", properties.Select(Function(p) "x." + p)))
            builder.Append("}")
        End If

        Return builder.ToString()
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Reference(type As Type) As String Implements IVisualBasicHelper.Reference

        Dim builtInType As String = Nothing
        If (_builtInTypes.TryGetValue(type, builtInType)) Then
            Return builtInType
        End If

        If (type.IsConstructedGenericType AndAlso type.GetGenericTypeDefinition() = GetType(Nullable(Of ))) Then
            Return Reference(type.UnwrapNullableType()) + "?"
        End If

        Dim builder = New StringBuilder()

        If type.IsArray Then
            builder.Append(Reference(type.GetElementType())) _
                   .Append("(")

            Dim rank = type.GetArrayRank()
            For i = 1 To rank - 1
                builder.Append(",")
            Next

            builder.Append(")")

            Return builder.ToString()
        End If

        If (type.IsNested) Then
            Debug.Assert(Not type.DeclaringType Is Nothing)
            builder.Append(Reference(type.DeclaringType)) _
                   .Append(".")
        End If

        Dim typeName = type.ShortDisplayName()

        If _keywords.Contains(typeName, StringComparer.OrdinalIgnoreCase) Then
            builder.Append($"[{typeName}]")
        Else
            builder.Append(typeName)
        End If

        Return builder.ToString()
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Identifier(name As String, Optional scope As ICollection(Of String) = Nothing) As String Implements IVisualBasicHelper.Identifier

        Dim builder = New StringBuilder()
        Dim partStart = 0

        For i = 0 To name.Length - 1
            If Not IsIdentifierPartCharacter(name(i)) Then
                If partStart <> i Then
                    builder.Append(name.Substring(partStart, i - partStart))
                End If

                partStart = i + 1
            End If
        Next

        If partStart <> name.Length Then
            builder.Append(name.Substring(partStart))
        End If

        If builder.Length = 0 OrElse Not IsIdentifierStartCharacter(builder(0)) Then
            builder.Insert(0, "_")
        End If

        Dim baseIdentifier = builder.ToString()
        If Not scope Is Nothing Then
            Dim uniqueIdentifier = baseIdentifier
            Dim qualifier = 0
            While (scope.Contains(uniqueIdentifier))
                uniqueIdentifier = baseIdentifier + qualifier.ToString()
                qualifier += 1
            End While
            scope.Add(uniqueIdentifier)
            baseIdentifier = uniqueIdentifier
        End If

        If (_keywords.Contains(baseIdentifier)) Then
            Return "[" + baseIdentifier + "]"
        End If

        Return baseIdentifier
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function [Namespace](ParamArray name As String()) As String Implements IVisualBasicHelper.Namespace

        Dim ns = New StringBuilder()
        For Each piece In name.Where(Function(p) Not String.IsNullOrEmpty(p)).SelectMany(Function(p) p.Split({"."}, StringSplitOptions.RemoveEmptyEntries))
            Dim ident = Identifier(piece)
            If Not String.IsNullOrEmpty(ident) Then
                ns.Append(ident) _
                   .Append(".")
            End If
        Next
        Return If(ns.Length > 0, ns.Remove(ns.Length - 1, 1).ToString(), "_")
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As String) As String Implements IVisualBasicHelper.Literal
        Return $"""{value.Replace("""", """""")}"""
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure and is not intended to be used
    '''     directly from your code. This API may change or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As Boolean) As String Implements IVisualBasicHelper.Literal
        Return If(value, "True", "False")
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure and is not intended to be used
    '''     directly from your code. This API may change or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As Byte) As String Implements IVisualBasicHelper.Literal
        Return "CByte(" + value.ToString() + ")"
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure and is not intended to be used
    '''     directly from your code. This API may change or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(values As Byte()) As String Implements IVisualBasicHelper.Literal
        Return "New Byte() {" + String.Join(", ", values) + "}"
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As Char) As String Implements IVisualBasicHelper.Literal
        Return """" + If(value = """", """""", value.ToString()) + """c"
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As DateTime) As String Implements IVisualBasicHelper.Literal
        Return String.Format(
                CultureInfo.InvariantCulture,
                "New DateTime({0}, {1}, {2}, {3}, {4}, {5}, {6}, DateTimeKind.{7})",
                value.Year,
                value.Month,
                value.Day,
                value.Hour,
                value.Minute,
                value.Second,
                value.Millisecond,
                value.Kind)
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As DateTimeOffset) As String Implements IVisualBasicHelper.Literal
        Return "New DateTimeOffset(" + Literal(value.DateTime) + ", " + Literal(value.Offset) + ")"
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As Decimal) As String Implements IVisualBasicHelper.Literal
        Return value.ToString(CultureInfo.InvariantCulture) + "D"
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As Double) As String Implements IVisualBasicHelper.Literal
        Return EnsureDecimalPlaces(value.ToString("R", CultureInfo.InvariantCulture))
    End Function

    Private Function EnsureDecimalPlaces(number As String) As String
        Return If(number.IndexOf(".") >= 0, number, number + ".0")
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As Single) As String Implements IVisualBasicHelper.Literal
        Return value.ToString(CultureInfo.InvariantCulture) + "f"
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As Guid) As String Implements IVisualBasicHelper.Literal
        Return "New Guid(""" + value.ToString() + """)"
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As Integer) As String Implements IVisualBasicHelper.Literal
        Return value.ToString(CultureInfo.InvariantCulture)
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As Long) As String Implements IVisualBasicHelper.Literal
        Return value.ToString() + "L"
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As SByte) As String Implements IVisualBasicHelper.Literal
        Return "CSByte(" + value.ToString() + ")"
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As Short) As String Implements IVisualBasicHelper.Literal
        Return "CShort(" + value.ToString() + ")"
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As TimeSpan) As String Implements IVisualBasicHelper.Literal
        Return String.Format(
            CultureInfo.InvariantCulture,
            "New TimeSpan({0}, {1}, {2}, {3}, {4})",
            value.Days,
            value.Hours,
            value.Minutes,
            value.Seconds,
            value.Milliseconds)
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As UInteger) As String Implements IVisualBasicHelper.Literal
        Return "CUInt(" + value.ToString() + ")"
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As ULong) As String Implements IVisualBasicHelper.Literal
        Return value.ToString() + "ul"
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As UShort) As String Implements IVisualBasicHelper.Literal
        Return "CUShort(" + value.ToString() + ")"
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(Of T As Structure)(value As T?) As String Implements IVisualBasicHelper.Literal
        Return UnknownLiteral(value)
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(Of T)(values As IReadOnlyList(Of T)) As String Implements IVisualBasicHelper.Literal
        Return "{" + String.Join(", ", values.Cast(Of Object)().Select(Function(obj) UnknownLiteral(obj))) + "}"
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(values As IReadOnlyList(Of Object)) As String Implements IVisualBasicHelper.Literal
        Return Literal(values, vertical:=False)
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(values As IReadOnlyList(Of Object), vertical As Boolean) As String Implements IVisualBasicHelper.Literal
        If Not vertical Then
            Return "New Object() {" + String.Join(", ", values.Select(Function(obj) UnknownLiteral(obj))) + "}"
        End If

        Dim builder = New IndentedStringBuilder()

        builder.AppendLine("New Object() {")

        Using builder.Indent()
            For i = 0 To values.Count - 1
                If i <> 0 Then
                    builder.AppendLine(",")
                End If

                builder.Append(UnknownLiteral(values(i)))
            Next
        End Using

        builder.AppendLine() _
               .Append("}")

        Return builder.ToString()

    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(values As Object(,)) As String Implements IVisualBasicHelper.Literal
        Dim builder = New IndentedStringBuilder()

        builder.AppendLine("New Object(,) {")

        Using builder.Indent()
            Dim rowCount = values.GetLength(0)
            Dim valueCount = values.GetLength(1)
            For i = 0 To rowCount - 1
                If i <> 0 Then
                    builder.AppendLine(",")
                End If

                builder.Append("{")

                For j = 0 To valueCount - 1
                    If j <> 0 Then
                        builder.Append(", ")
                    End If

                    builder.Append(UnknownLiteral(values(i, j)))
                Next

                builder.Append("}")
            Next
        End Using

        builder.AppendLine() _
               .Append("}")

        Return builder.ToString()
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function Literal(value As [Enum]) As String Implements IVisualBasicHelper.Literal
        Return Reference(value.GetType()) + "." + value.ToString()
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Overridable Function UnknownLiteral(value As Object) As String Implements IVisualBasicHelper.UnknownLiteral
        If value Is Nothing OrElse value Is DBNull.Value Then
            Return "Nothing"
        End If

        Dim type = value.GetType().UnwrapNullableType()

        Dim literalFunc As Func(Of IVisualBasicHelper, Object, String) = Nothing
        If _literalFuncs.TryGetValue(type, literalFunc) Then
            Return literalFunc(CType(Me, IVisualBasicHelper), value)
        End If

        If (TypeOf value Is [Enum]) Then
            Return Literal(CType(value, [Enum]))
        End If

        Throw New InvalidOperationException(VBDesignStrings.UnknownLiteral(value.GetType()))
    End Function

    Private Shared Function IsIdentifierStartCharacter(ch As Char) As Boolean
        If ch < "a" Then
            If (ch < "A") Then
                Return False
            End If

            Return ch <= "Z" OrElse ch = "_"
        End If

        If ch <= "z" Then
            Return True
        End If

        If ch <= "\u007F" Then '// max ASCII
            Return False
        End If

        Return IsLetterChar(CharUnicodeInfo.GetUnicodeCategory(ch))
    End Function

    Private Shared Function IsIdentifierPartCharacter(ch As Char) As Boolean
        If ch < "a" Then
            If ch < "A" Then
                Return ch >= "0" AndAlso ch <= "9"
            End If

            Return ch <= "Z" OrElse ch = "_"
        End If

        If ch <= "z" Then
            Return True
        End If

        If ch <= "\u007F" Then
            Return False
        End If

        Dim cat = CharUnicodeInfo.GetUnicodeCategory(ch)
        If IsLetterChar(cat) Then
            Return True
        End If

        Select Case cat
            Case UnicodeCategory.DecimalDigitNumber
            Case UnicodeCategory.ConnectorPunctuation
            Case UnicodeCategory.NonSpacingMark
            Case UnicodeCategory.SpacingCombiningMark
            Case UnicodeCategory.Format
                Return True
        End Select

        Return False
    End Function

    Private Shared Function IsLetterChar(cat As UnicodeCategory) As Boolean
        Select Case cat
            Case UnicodeCategory.UppercaseLetter
            Case UnicodeCategory.LowercaseLetter
            Case UnicodeCategory.TitlecaseLetter
            Case UnicodeCategory.ModifierLetter
            Case UnicodeCategory.OtherLetter
            Case UnicodeCategory.LetterNumber
                Return True
        End Select

        Return False
    End Function
End Class