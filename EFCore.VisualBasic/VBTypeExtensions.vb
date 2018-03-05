
Imports System.Runtime.CompilerServices
Imports System.Text
''' <summary>
'''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
'''     directly from your code. This API may change Or be removed in future releases.
''' </summary>
Public Module VBTypeExtensions

    Private ReadOnly _builtInTypeNames As Dictionary(Of Type, String) = New Dictionary(Of Type, String) From {
            {GetType(Boolean), "Boolean"},
            {GetType(Byte), "Byte"},
            {GetType(Char), "Char"},
            {GetType(Decimal), "Decimal"},
            {GetType(Double), "Double"},
            {GetType(Single), "Single"},
            {GetType(Integer), "Integer"},
            {GetType(Long), "Long"},
            {GetType(Object), "Object"},
            {GetType(SByte), "SByte"},
            {GetType(Short), "Short"},
            {GetType(String), "String"},
            {GetType(UInteger), "UInteger"},
            {GetType(ULong), "ULong"},
            {GetType(UShort), "UShort"}
        }

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    <Extension()>
    Public Function IsDefaultValue(type As Type, value As Object) As Boolean
        Return (value Is Nothing) OrElse value.Equals(type.GetDefaultValue())
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    <Extension()>
    Public Function ShortDisplayName(type As Type) As String
        Return type.DisplayName(fullName:=False)
    End Function

    ''' <summary>
    '''     This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''     directly from your code. This API may change Or be removed in future releases.
    ''' </summary>
    <Extension()>
    Public Function DisplayName(type As Type, Optional fullName As Boolean = True) As String
        Dim stringBuilder = New StringBuilder()
        ProcessType(stringBuilder, type, fullName)
        Return stringBuilder.ToString()
    End Function

    Private Sub ProcessType(builder As StringBuilder, type As Type, fullName As Boolean)
        Dim builtInName As String = Nothing

        If type.IsGenericType Then
            Dim genericArguments = type.GetGenericArguments()
            ProcessGenericType(builder, type, genericArguments, genericArguments.Length, fullName)
        ElseIf type.IsArray Then
            ProcessArrayType(builder, type, fullName)
        ElseIf _builtInTypeNames.TryGetValue(type, builtInName) Then
            builder.Append(builtInName)
        ElseIf Not type.IsGenericParameter Then
            builder.Append(If(fullName, type.FullName, type.Name))
        End If
    End Sub

    Private Sub ProcessArrayType(builder As StringBuilder, type As Type, fullName As Boolean)
        Dim innerType = type
        While innerType.IsArray
            innerType = innerType.GetElementType()
        End While

        ProcessType(builder, innerType, fullName)

        While type.IsArray
            builder.Append("(")
            builder.Append(",", type.GetArrayRank() - 1)
            builder.Append(")")
            type = type.GetElementType()
        End While
    End Sub

    Private Sub ProcessGenericType(builder As StringBuilder, type As Type, genericArguments As Type(), length As Integer, fullName As Boolean)
        Dim offset = If(type.IsNested, type.DeclaringType.GetGenericArguments().Length, 0)

        If fullName Then
            If (type.IsNested) Then
                ProcessGenericType(builder, type.DeclaringType, genericArguments, offset, fullName)
                builder.Append("+")
            Else
                builder.Append(type.Namespace)
                builder.Append(".")
            End If
        End If

        Dim genericPartIndex = type.Name.IndexOf("`")
        If genericPartIndex <= 0 Then
            builder.Append(type.Name)
            Return
        End If

        builder.Append(type.Name, 0, genericPartIndex)
        builder.Append("(Of ")

        For i = offset To length - 1
            ProcessType(builder, genericArguments(i), fullName)
            If (i + 1 = length) Then
                Continue For
            End If

            builder.Append(",")

            If Not genericArguments(i + 1).IsGenericParameter Then
                builder.Append(" ")
            End If
        Next

        builder.Append(")")
    End Sub
End Module