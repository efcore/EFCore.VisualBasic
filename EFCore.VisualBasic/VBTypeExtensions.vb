Imports System.Runtime.CompilerServices
Imports System.Text

Friend Module VBTypeExtensions

    <Extension()>
    Public Function IsDefaultValue(type As Type, value As Object) As Boolean
        Return (value Is Nothing) OrElse value.Equals(type.GetDefaultValue())
    End Function

    <Extension()>
    Public Function ShortDisplayName(type As Type) As String
        Return type.DisplayName(fullName:=False)
    End Function

    <Extension()>
    Public Function DisplayName(type As Type,
                                Optional fullName As Boolean = True,
                                Optional compilable As Boolean = False) As String

        Dim stringBuilder = New StringBuilder()
        ProcessType(stringBuilder, type, fullName, compilable)
        Return stringBuilder.ToString()
    End Function

    Private Sub ProcessType(builder As StringBuilder, type As Type, fullName As Boolean, compilable As Boolean)
        Dim builtInName As String = Nothing

        If type.IsGenericType Then
            Dim genericArguments = type.GetGenericArguments()
            ProcessGenericType(builder, type, genericArguments, genericArguments.Length, fullName, compilable)

        ElseIf type.IsArray Then
            ProcessArrayType(builder, type, fullName, compilable)

        ElseIf _builtInTypeNames.TryGetValue(type, builtInName) Then
            builder.Append(builtInName)

        ElseIf Not type.IsGenericParameter Then
            If compilable Then
                If type.IsNested Then
                    ProcessType(builder, type.DeclaringType, fullName, compilable)
                    builder.Append("."c)
                ElseIf fullName Then
                    builder.Append(type.Namespace).Append("."c)
                End If

                Dim typeName = type.Name
                If IsVisualBasicKeyword(typeName) Then
                    typeName = $"[{typeName}]"
                End If

                builder.Append(typeName)
            Else
                builder.Append(If(fullName, type.FullName, type.Name))
            End If
        End If
    End Sub

    Private Sub ProcessArrayType(builder As StringBuilder, type As Type, fullName As Boolean, compilable As Boolean)
        Dim innerType = type

        While innerType.IsArray
            innerType = innerType.GetElementType()
        End While

        ProcessType(builder, innerType, fullName, compilable)

        While type.IsArray
            builder.
                Append("("c).
                Append(","c, type.GetArrayRank() - 1).
                Append(")"c)
            type = type.GetElementType()
        End While
    End Sub

    Private Sub ProcessGenericType(builder As StringBuilder,
                                   type As Type, genericArguments As Type(),
                                   length As Integer,
                                   fullName As Boolean,
                                   compilable As Boolean)

        If type.IsConstructedGenericType AndAlso type.GetGenericTypeDefinition() = GetType(Nullable(Of)) Then
            ProcessType(builder, type.UnwrapNullableType(), fullName, compilable)
            builder.Append("?"c)
            Return
        End If

        Dim offset = If(type.IsNested, type.DeclaringType.GetGenericArguments().Length, 0)

        If compilable Then
            If type.IsNested Then
                ProcessType(builder, type.DeclaringType, fullName, compilable)
                builder.Append("."c)
            ElseIf fullName Then
                builder.Append(type.Namespace).
                        Append("."c)
            End If
        Else
            If fullName Then
                If type.IsNested Then
                    ProcessGenericType(builder, type.DeclaringType, genericArguments, offset, fullName, compilable)
                    builder.Append("+"c)
                Else
                    builder.Append(type.Namespace).
                            Append("."c)
                End If
            End If
        End If

        Dim genericPartIndex = type.Name.IndexOf("`"c)
        If genericPartIndex <= 0 Then
            builder.Append(type.Name)
            Return
        End If

        builder.Append(type.Name, 0, genericPartIndex)
        builder.Append("(Of ")

        For i = offset To length - 1
            ProcessType(builder, genericArguments(i), fullName, compilable)
            If i + 1 = length Then
                Continue For
            End If

            builder.Append(","c)
            If Not genericArguments(i + 1).IsGenericParameter Then
                builder.Append(" "c)
            End If
        Next

        builder.Append(")"c)
    End Sub

End Module