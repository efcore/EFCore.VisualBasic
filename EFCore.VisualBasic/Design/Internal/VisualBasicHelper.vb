Imports System.Globalization
Imports System.Linq.Expressions
Imports System.Numerics
Imports System.Text
Imports EntityFrameworkCore.VisualBasic
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Storage
Imports IndentedStringBuilder = Microsoft.EntityFrameworkCore.Infrastructure.IndentedStringBuilder

Namespace Design.Internal

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    Public Class VisualBasicHelper
        Implements IVisualBasicHelper

        Private ReadOnly _typeMappingSource As ITypeMappingSource

        Sub New(typeMappingSource As ITypeMappingSource)
            NotNull(typeMappingSource, NameOf(typeMappingSource))

            _typeMappingSource = typeMappingSource
        End Sub

        Private Shared ReadOnly _literalFuncs As New Dictionary(Of Type, Func(Of VisualBasicHelper, Object, String)) From
        {
            {GetType(Boolean), Function(c, v) c.Literal(CBool(v))},
            {GetType(Byte), Function(c, v) c.Literal(CByte(v))},
            {GetType(Byte()), Function(c, v) c.Literal(CType(v, Byte()))},
            {GetType(Char), Function(c, v) c.Literal(CChar(v))},
            {GetType(DateOnly), Function(c, v) c.Literal(DirectCast(v, DateOnly))},
            {GetType(Date), Function(c, v) c.Literal(CDate(v))},
            {GetType(DateTimeOffset), Function(c, v) c.Literal(CType(v, DateTimeOffset))},
            {GetType(Decimal), Function(c, v) c.Literal(CDec(v))},
            {GetType(Double), Function(c, v) c.Literal(CDbl(v))},
            {GetType(Single), Function(c, v) c.Literal(CSng(v))},
            {GetType(Guid), Function(c, v) c.Literal(CType(v, Guid))},
            {GetType(Integer), Function(c, v) c.Literal(CInt(v))},
            {GetType(Long), Function(c, v) c.Literal(CLng(v))},
            {GetType(NestedClosureCodeFragment), Function(c, v) c.Fragment(DirectCast(v, NestedClosureCodeFragment))},
            {GetType(Object()), Function(c, v) c.Literal(CType(v, Object()))},
            {GetType(Object(,)), Function(c, v) c.Literal(CType(v, Object(,)))},
            {GetType(SByte), Function(c, v) c.Literal(CSByte(v))},
            {GetType(Short), Function(c, v) c.Literal(CShort(v))},
            {GetType(String), Function(c, v) c.Literal(CStr(v))},
            {GetType(TimeOnly), Function(c, v) c.Literal(DirectCast(v, TimeOnly))},
            {GetType(TimeSpan), Function(c, v) c.Literal(CType(v, TimeSpan))},
            {GetType(UInteger), Function(c, v) c.Literal(CUInt(v))},
            {GetType(ULong), Function(c, v) c.Literal(CULng(v))},
            {GetType(UShort), Function(c, v) c.Literal(CUShort(v))},
            {GetType(BigInteger), Function(c, v) c.Literal(DirectCast(v, BigInteger))},
            {GetType(Type), Function(c, v) c.Literal(DirectCast(v, Type))}
        }

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Lambda(properties As IReadOnlyList(Of String), Optional lambdaIdentifier As String = Nothing) As String Implements IVisualBasicHelper.Lambda

            NotNull(properties, NameOf(properties))
            NullButNotEmpty(lambdaIdentifier, NameOf(lambdaIdentifier))

            lambdaIdentifier = If(lambdaIdentifier, "x")

            Dim builder = New StringBuilder()
            builder.
                Append("Function(").
                Append(lambdaIdentifier).
                Append(") ")

            If properties.Count = 1 Then
                builder.
                    Append(lambdaIdentifier).
                    Append(".").
                    Append(properties(0))
            Else
                builder.
                    Append("New With {").
                    AppendJoin(", ", properties.Select(Function(p) $"{lambdaIdentifier}.{p}")).
                    Append("}")
            End If

            Return builder.ToString()
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Lambda(properties As IEnumerable(Of IProperty), Optional lambdaIdentifier As String = Nothing) As String Implements IVisualBasicHelper.Lambda
            Return Lambda(properties.Select(Function(p) p.Name).ToList(), lambdaIdentifier)
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Reference(type As Type, Optional fullName As Boolean? = Nothing) As String Implements IVisualBasicHelper.Reference
            NotNull(type, NameOf(type))
            If Not fullName.HasValue Then
                fullName = If(type.IsNested, ShouldUseFullName(type.DeclaringType), ShouldUseFullName(type))
            End If
            Return type.DisplayName(fullName:=fullName.Value, compilable:=True)
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function ShouldUseFullName(type As Type) As Boolean
            Return ShouldUseFullName(type.Name)
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function ShouldUseFullName(shortTypeName As String) As Boolean
            Return False
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Identifier(name As String,
                                               Optional scope As ICollection(Of String) = Nothing,
                                               Optional capitalize As Boolean? = Nothing) As String Implements IVisualBasicHelper.Identifier
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

            If capitalize IsNot Nothing Then
                ChangeFirstLetterCase(builder, capitalize.Value)
            End If

            Dim baseIdentifier = builder.ToString()
            If scope IsNot Nothing Then
                Dim uniqueIdentifier = baseIdentifier
                Dim qualifier = 0
                While scope.Contains(uniqueIdentifier, StringComparer.Create(CultureInfo.InvariantCulture, ignoreCase:=True))
                    uniqueIdentifier = baseIdentifier & qualifier.ToString(CultureInfo.InvariantCulture)
                    qualifier += 1
                End While
                scope.Add(uniqueIdentifier)
                baseIdentifier = uniqueIdentifier
            End If

            If IsVisualBasicKeyword(baseIdentifier) Then
                Return "[" & baseIdentifier & "]"
            End If

            Return baseIdentifier
        End Function

        Private Shared Sub ChangeFirstLetterCase(builder As StringBuilder, capitalize As Boolean)
            If builder.Length = 0 Then
                Exit Sub
            End If

            Dim first = builder(0)
            If Char.IsUpper(first) = capitalize Then
                Exit Sub
            End If

            builder.Remove(0, 1).
                Insert(0, If(capitalize, Char.ToUpperInvariant(first), Char.ToLowerInvariant(first)))
        End Sub

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function [Namespace](ParamArray name As String()) As String Implements IVisualBasicHelper.Namespace
            Dim namespaces = New StringBuilder()
            For Each piece In name.Where(Function(p) Not String.IsNullOrEmpty(p)).
                                   SelectMany(Function(p) p.Split({"."c}, StringSplitOptions.RemoveEmptyEntries))
                Dim identify = Identifier(piece)

                If Not String.IsNullOrEmpty(identify) Then
                    namespaces.Append(identify).Append("."c)
                End If
            Next

            Return If(namespaces.Length > 0, namespaces.Remove(namespaces.Length - 1, 1).ToString(), "_")
        End Function


        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As String) As String Implements IVisualBasicHelper.Literal

            Return """" & value.Replace("""", """""").
                                Replace(vbCrLf, """ & vbCrLf & """).
                                Replace(vbCr, """ & vbCr & """).
                                Replace(vbLf, """ & vbLf & """) & """"
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
            Return "CByte(" & value.ToString(CultureInfo.InvariantCulture) & ")"
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As Char) As String Implements IVisualBasicHelper.Literal
            Return """" & If(value = """", """""", value.ToString()) & """c"
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As DateOnly) As String Implements IVisualBasicHelper.Literal
            Return String.Format(
                CultureInfo.InvariantCulture,
                "New DateOnly({0}, {1}, {2})",
                value.Year,
                value.Month,
                value.Day)
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As Date) As String Implements IVisualBasicHelper.Literal
            Return String.Format(
            CultureInfo.InvariantCulture,
            "New Date({0}, {1}, {2}, {3}, {4}, {5}, {6}, DateTimeKind.{7})",
            value.Year,
            value.Month,
            value.Day,
            value.Hour,
            value.Minute,
            value.Second,
            value.Millisecond,
            value.Kind) &
            If(value.Ticks Mod 10000 = 0, "", String.Format(CultureInfo.InvariantCulture,
                                                            ".AddTicks({0})",
                                                            value.Ticks Mod 10000))
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As DateTimeOffset) As String Implements IVisualBasicHelper.Literal
            Return "New DateTimeOffset(" & Literal(value.DateTime) & ", " & Literal(value.Offset) & ")"
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As Decimal) As String Implements IVisualBasicHelper.Literal
            Return value.ToString(CultureInfo.InvariantCulture) & "D"
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As Double) As String Implements IVisualBasicHelper.Literal

            Dim str = value.ToString("G17", CultureInfo.InvariantCulture)

            If Double.IsNaN(value) Then
                Return $"Double.{NameOf(Double.NaN)}"
            End If

            If Double.IsNegativeInfinity(value) Then
                Return $"Double.{NameOf(Double.NegativeInfinity)}"
            End If

            If Double.IsPositiveInfinity(value) Then
                Return $"Double.{NameOf(Double.PositiveInfinity)}"
            End If

            Return If(Not str.Contains("E"c) AndAlso
                      Not str.Contains("e"c) AndAlso
                      Not str.Contains("."c), str & ".0", str)
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As Single) As String Implements IVisualBasicHelper.Literal
            Return value.ToString(CultureInfo.InvariantCulture) & "F"
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As Guid) As String Implements IVisualBasicHelper.Literal
            Return "New Guid(""" & value.ToString() & """)"
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As Integer) As String Implements IVisualBasicHelper.Literal
            If value = Integer.MinValue Then Return "Integer.MinValue"
            Return value.ToString(CultureInfo.InvariantCulture)
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As Long) As String Implements IVisualBasicHelper.Literal
            If value = Long.MinValue Then Return "Long.MinValue"
            Return value.ToString(CultureInfo.InvariantCulture) & "L"
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As SByte) As String Implements IVisualBasicHelper.Literal
            Return "CSByte(" & value.ToString(CultureInfo.InvariantCulture) & ")"
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As Short) As String Implements IVisualBasicHelper.Literal
            If value = Short.MinValue Then Return "Short.MinValue"
            Return value.ToString(CultureInfo.InvariantCulture) & "S"
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As TimeOnly) As String Implements IVisualBasicHelper.Literal
            Dim result = If(value.Millisecond = 0,
                                String.Format(CultureInfo.InvariantCulture, "New TimeOnly({0}, {1}, {2})", value.Hour, value.Minute, value.Second),
                                String.Format(CultureInfo.InvariantCulture, "New TimeOnly({0}, {1}, {2}, {3})", value.Hour, value.Minute, value.Second, value.Millisecond))

            If value.Ticks Mod 10000 > 0 Then
                result &= String.Format(
                    CultureInfo.InvariantCulture,
                    ".Add(TimeSpan.FromTicks({0}))",
                    value.Ticks Mod 10000)
            End If

            Return result
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As TimeSpan) As String Implements IVisualBasicHelper.Literal

            If value.Ticks Mod 10000 = 0 Then
                Return String.Format(CultureInfo.InvariantCulture,
                                    "New TimeSpan({0}, {1}, {2}, {3}, {4})",
                                    value.Days,
                                    value.Hours,
                                    value.Minutes,
                                    value.Seconds,
                                    value.Milliseconds)
            Else
                Return String.Format(CultureInfo.InvariantCulture,
                                    "New TimeSpan({0})",
                                    Literal(value.Ticks))
            End If

        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As UInteger) As String Implements IVisualBasicHelper.Literal
            Return value.ToString(CultureInfo.InvariantCulture) & "UI"
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As ULong) As String Implements IVisualBasicHelper.Literal
            Return value.ToString(CultureInfo.InvariantCulture) & "UL"
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As UShort) As String Implements IVisualBasicHelper.Literal
            Return value.ToString(CultureInfo.InvariantCulture) & "US"
        End Function

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Public Overridable Function Literal(value As BigInteger) As String Implements IVisualBasicHelper.Literal
            Return $"BigInteger.Parse(""{value.ToString(NumberFormatInfo.InvariantInfo)}"", NumberFormatInfo.InvariantInfo)"
        End Function

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Public Overridable Function Literal(value As Type, Optional fullName As Boolean? = Nothing) As String Implements IVisualBasicHelper.Literal
            Return $"GetType({Reference(value, fullName)})"
        End Function

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Public Overridable Function Literal(Of T As Structure)(value As T?) As String Implements IVisualBasicHelper.Literal
            Return UnknownLiteral(value)
        End Function

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Public Overridable Function Literal(Of T)(values As T(), Optional vertical As Boolean = False) As String Implements IVisualBasicHelper.Literal
            Return ArrayLitetal(GetType(T), values, vertical)
        End Function

        Private Function ArrayLitetal(Type As Type, values As IEnumerable, Optional vertical As Boolean = False) As String
            Dim builder As New IndentedStringBuilder

            Dim valuesList = values.Cast(Of Object)().ToList()

            If valuesList.Count = 0 Then
                builder.
                    Append("New ").
                    Append(Reference(Type)).
                    Append("() {}")
            Else

                Dim byteArray As Boolean
                Dim addingType As Boolean

                If Type = GetType(Byte) Then
                    builder.Append("New Byte")
                    addingType = True
                    byteArray = True
                ElseIf Type = GetType(Object) Then
                    builder.Append("New Object")
                    addingType = True
                End If

                If addingType Then
                    builder.Append("() ")
                End If

                builder.Append("{"c)

                If vertical Then
                    builder.AppendLine()
                    builder.IncrementIndent()
                End If

                Dim first As Boolean = True
                For Each value In valuesList
                    If first Then
                        first = False
                    Else
                        builder.Append(","c)

                        If vertical Then
                            builder.AppendLine()
                        Else
                            builder.Append(" "c)
                        End If
                    End If

                    builder.Append(If(byteArray, Literal(CInt(DirectCast(value, Byte))), UnknownLiteral(value)))
                Next

                If vertical Then
                    builder.AppendLine()
                    builder.DecrementIndent()
                End If

                builder.Append("}"c)
            End If

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

                    builder.Append("{"c)

                    For j = 0 To valueCount - 1
                        If j <> 0 Then
                            builder.Append(", ")
                        End If

                        builder.Append(UnknownLiteral(values(i, j)))
                    Next

                    builder.Append("}"c)
                Next
            End Using

            builder.
            AppendLine().
            Append("}"c)

            Return builder.ToString()
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(Of T)(values As List(Of T), Optional vertical As Boolean = False) As String
            Return ListLitetal(GetType(T), values, vertical)
        End Function

        Private Function ListLitetal(type As Type, values As IEnumerable, Optional vertical As Boolean = False) As String

            Dim builder As New IndentedStringBuilder()

            builder.
                Append("New List(Of ").
                Append(Reference(type)).
                Append(")")

            Dim hasData = False
            Dim first = True

            For Each value In values
                hasData = True
                If first Then
                    builder.Append(" From {")
                    If vertical Then
                        builder.AppendLine()
                        builder.IncrementIndent()
                    End If
                    first = False
                Else
                    builder.Append(","c)

                    If vertical Then
                        builder.AppendLine()
                    Else
                        builder.Append(" "c)
                    End If
                End If

                builder.Append(UnknownLiteral(value))
            Next

            If hasData Then
                If vertical Then
                    builder.AppendLine()
                    builder.DecrementIndent()
                End If

                builder.Append("}"c)
            End If

            Return builder.ToString()
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function Literal(value As [Enum]) As String Implements IVisualBasicHelper.Literal

            Dim type = value.GetType()
            Dim name = [Enum].GetName(type, value)

            Return If(name Is Nothing,
                        GetCompositeEnumValue(type, value),
                        GetSimpleEnumValue(type, name))
        End Function

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Protected Overridable Function GetSimpleEnumValue(type As Type, name As String) As String

            NotNull(type, NameOf(type))
            NotNull(name, NameOf(name))

            Return Reference(type) & "." & name
        End Function

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Protected Overridable Function GetCompositeEnumValue(type As Type, flags As [Enum]) As String

            NotNull(type, NameOf(type))
            NotNull(flags, NameOf(flags))

            Dim allValues As New HashSet(Of [Enum])(GetFlags(flags))

            For Each currentValue In allValues.ToList()
                Dim decomposedValues = GetFlags(currentValue)
                If decomposedValues.Count > 1 Then
                    allValues.ExceptWith(decomposedValues.Where(Function(v) Not Equals(v, currentValue)))
                End If
            Next

            Return allValues.Aggregate(Of String)(
                        Nothing,
                        Function(previous, current)
                            Return If(previous Is Nothing,
                                        GetSimpleEnumValue(type, [Enum].GetName(type, current)),
                                        previous & " Or " & GetSimpleEnumValue(type, [Enum].GetName(type, current)))
                        End Function)
        End Function

        Friend Shared Function GetFlags(flags As [Enum]) As IReadOnlyCollection(Of [Enum])
            Dim values As New List(Of [Enum])

            Dim type = flags.[GetType]()
            Dim defaultValue = [Enum].ToObject(type, value:=0)

            For Each currValue As [Enum] In [Enum].GetValues(type)
                If currValue.Equals(defaultValue) Then
                    Continue For
                End If

                If flags.HasFlag(currValue) Then
                    values.Add(currValue)
                End If
            Next

            Return values
        End Function

        ''' <summary>
        '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
        '''     directly from your code. This API may change Or be removed in future releases.
        ''' </summary>
        Public Overridable Function UnknownLiteral(value As Object) As String Implements IVisualBasicHelper.UnknownLiteral
            If value Is Nothing Then
                Return "Nothing"
            End If

            Dim LiteralType = value.GetType().UnwrapNullableType()

            Dim literalFunc As Func(Of VisualBasicHelper, Object, String) = Nothing
            If _literalFuncs.TryGetValue(LiteralType, literalFunc) Then
                Return literalFunc(Me, value)
            End If

            If TypeOf value Is [Enum] Then
                Return Literal(CType(value, [Enum]))
            End If

            If TypeOf value Is Type Then
                Return Literal(DirectCast(value, Type))
            End If

            If TypeOf value Is Array Then
                Return ArrayLitetal(LiteralType.GetElementType(), DirectCast(value, Array))
            End If

            If TypeOf value Is IList AndAlso
               value.GetType().IsGenericType AndAlso
               value.GetType().GetGenericTypeDefinition() Is GetType(List(Of)) Then

                Return ListLitetal(value.GetType().GetGenericArguments()(0), DirectCast(value, IList))
            End If

            Dim mapping = _typeMappingSource.FindMapping(LiteralType)
            If mapping IsNot Nothing Then
                Dim builder As New StringBuilder
                Dim expression = mapping.GenerateCodeLiteral(value)
                Dim handled = HandleExpression(expression, builder)

                If Not handled Then
                    Throw New NotSupportedException(DesignStrings.LiteralExpressionNotSupported(
                                                    expression.ToString(),
                                                    LiteralType.ShortDisplayName()))
                End If

                Return builder.ToString()
            End If

            Throw New InvalidOperationException(VBDesignStrings.UnknownLiteral(value.GetType()))
        End Function

        Private Function HandleExpression(exp As Expression, builder As StringBuilder, Optional simple As Boolean = False) As Boolean
            ' Only handle trivially simple cases for `new` and factory methods

            Select Case exp.NodeType
                Case ExpressionType.NewArrayInit
                    builder.
                        Append("New ").
                        Append(Reference(exp.Type.GetElementType())).
                        Append("() { ")

                    HandleList(CType(exp, NewArrayExpression).Expressions, builder, simple:=True)

                    builder.
                        Append(" }")

                    Return True

                Case ExpressionType.Convert
                    builder.Append("CType(")

                    Dim b = HandleExpression(CType(exp, UnaryExpression).Operand, builder)

                    builder.Append(", ").
                        Append(Reference(exp.Type, fullName:=True)).
                        Append(")"c)

                    Return b

                Case ExpressionType.New
                    builder.
                        Append("New ").
                        Append(Reference(exp.Type, fullName:=True))

                    Return HandleArguments(CType(exp, NewExpression).Arguments, builder)

                Case ExpressionType.Call
                    Dim callExpression = CType(exp, MethodCallExpression)
                    If callExpression.Method.IsStatic Then
                        builder.
                          Append(Reference(callExpression.Method.DeclaringType, fullName:=True))
                    Else
                        If Not HandleExpression(callExpression.[Object], builder) Then
                            Return False
                        End If
                    End If

                    builder.
                        Append("."c).
                        Append(callExpression.Method.Name)

                    Return HandleArguments(callExpression.Arguments, builder)

                Case ExpressionType.Constant
                    Dim Value = CType(exp, ConstantExpression).Value

                    builder.
                        Append(If(simple AndAlso Value?.GetType()?.IsNumeric() = True,
                                  Value,
                                  UnknownLiteral(Value)))

                    Return True

                Case ExpressionType.MemberAccess
                    Dim memberExpression = CType(exp, MemberExpression)
                    If memberExpression.Expression Is Nothing Then
                        builder.
                            Append(Reference(memberExpression.Member.DeclaringType, fullName:=True))
                    Else
                        If Not HandleExpression(memberExpression.Expression, builder) Then
                            Return False
                        End If
                    End If

                    builder.
                        Append("."c).
                        Append(memberExpression.Member.Name)

                    Return True

                Case ExpressionType.Add
                    Dim binaryExpression = CType(exp, BinaryExpression)
                    If Not HandleExpression(binaryExpression.Left, builder) Then
                        Return False
                    End If

                    builder.Append(" + ")

                    Return HandleExpression(binaryExpression.Right, builder)
            End Select

            Return False
        End Function

        Private Function HandleArguments(argumentExpressions As IEnumerable(Of Expression), builder As StringBuilder) As Boolean
            builder.Append("("c)

            If Not HandleList(argumentExpressions, builder) Then
                Return False
            End If

            builder.Append(")"c)

            Return True
        End Function

        Private Function HandleList(argumentExpressions As IEnumerable(Of Expression), builder As StringBuilder, Optional simple As Boolean = False) As Boolean
            Dim separator As String = String.Empty

            For Each exp In argumentExpressions
                builder.Append(separator)

                If Not HandleExpression(exp, builder, simple) Then
                    Return False
                End If

                separator = ", "
            Next

            Return True
        End Function

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Public Function Fragment(frag As MethodCallCodeFragment,
                          Optional instanceIdentifier As String = Nothing,
                          Optional typeQualified As Boolean = False,
                          Optional startWithDot As Boolean = True) As String Implements IVisualBasicHelper.Fragment

            Return Fragment(frag, typeQualified, instanceIdentifier, startWithDot)
        End Function

        Private Function Fragment(frag As MethodCallCodeFragment,
                                  typeQualified As Boolean,
                                  instanceIdentifier As String,
                                  Optional startWithDot As Boolean = True) As String

            Dim builder As New IndentedStringBuilder
            Dim current = frag

            If typeQualified Then
                If instanceIdentifier Is Nothing OrElse
                   frag.MethodInfo Is Nothing OrElse
                   frag.ChainedCall IsNot Nothing Then

                    Throw New ArgumentException(DesignStrings.CannotGenerateTypeQualifiedMethodCall)
                End If

                builder.
                    Append(frag.DeclaringType).
                    Append(".").
                    Append(frag.Method).
                    Append("(").
                    Append(instanceIdentifier)

                For i = 0 To frag.Arguments.Count - 1
                    builder.Append(", ")
                    Argument(frag.Arguments(i), builder)
                Next

                builder.Append(")")

                Return builder.ToString()
            End If

            Dim IsFirstLine As Boolean = True
            Dim DotAlreadyAdded = False

            ' Non-type-qualified fragment
            If instanceIdentifier IsNot Nothing Then

                builder.Append(instanceIdentifier)

                If current.ChainedCall IsNot Nothing Then
                    builder.
                        AppendLine("."c).
                        IncrementIndent()
                    IsFirstLine = False
                    DotAlreadyAdded = True
                End If
            End If

            While True
                If IsFirstLine Then
                    If startWithDot Then
                        builder.Append("."c)
                    End If
                    IsFirstLine = False
                    Else
                        If Not DotAlreadyAdded Then
                        builder.AppendLine("."c)
                    Else
                        DotAlreadyAdded = False
                    End If
                End If

                builder.
                    Append(current.Method).
                    Append("("c)

                For i = 0 To current.Arguments.Count - 1
                    If i > 0 Then
                        builder.Append(", ")
                    End If

                    Argument(current.Arguments(i), builder)
                Next

                builder.Append(")"c)

                If current.ChainedCall Is Nothing Then Exit While

                current = current.ChainedCall
            End While

            Return builder.ToString()

        End Function

        Private Sub Argument(argument As Object, builder As IndentedStringBuilder)
            If TypeOf argument Is NestedClosureCodeFragment Then
                Dim nestedFragment = DirectCast(argument, NestedClosureCodeFragment)
                builder.AppendLines(Fragment(nestedFragment), skipFinalNewline:=True)
            Else
                builder.Append(UnknownLiteral(argument))
            End If
        End Sub

        Private Function Fragment(frag As NestedClosureCodeFragment) As String
            If frag.MethodCalls.Count = 1 Then
                Return $"Sub({frag.Parameter}) {Fragment(frag.MethodCalls(0), typeQualified:=False, frag.Parameter)}"
            End If

            Dim builder = New IndentedStringBuilder()
            builder.AppendLine($"Sub({frag.Parameter})")

            Using builder.Indent()
                For Each methodCall In frag.MethodCalls
                    builder.AppendLines(Fragment(methodCall, typeQualified:=False, frag.Parameter))
                Next
            End Using

            builder.Append("End Sub")

            Return builder.ToString()
        End Function

    End Class

End Namespace
