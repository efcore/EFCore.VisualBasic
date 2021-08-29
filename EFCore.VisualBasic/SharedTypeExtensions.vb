Imports System.Reflection
Imports System.Runtime.CompilerServices

<DebuggerStepThrough>
Friend Module SharedTypeExtensions

    <Extension()>
    Public Function UnwrapNullableType(type As Type) As Type
        Return If(Nullable.GetUnderlyingType(type), type)
    End Function

    <Extension()>
    Public Function IsNullableValueType(type As Type) As Boolean
        Return type.IsGenericType AndAlso type.GetGenericTypeDefinition() = GetType(Nullable(Of))
    End Function

    <Extension>
    Public Function IsNullableType(type As Type) As Boolean
        Return Not type.IsValueType OrElse type.IsNullableValueType()
    End Function

    <Extension()>
    Function IsValidEntityType(type As Type) As Boolean
        Return type.IsClass
    End Function

    <Extension()>
    Function MakeNullable(type As Type) As Type
        Return If(type.IsNullableType(), type, GetType(Nullable(Of )).MakeGenericType(type))
    End Function

    <Extension()>
    Public Function IsPropertyBagType(type As Type) As Boolean
        If type.IsGenericTypeDefinition Then Return False

        Dim types = GetGenericTypeImplementations(type, GetType(IDictionary(Of ,)))
        Return types.Any(Function(t) t.GetGenericArguments()(0) = GetType(String) AndAlso
                                     t.GetGenericArguments()(1) = GetType(Object))
    End Function

    <Extension()>
    Function MakeNullable(Type As Type, Optional nullable As Boolean = True) As Type

        If Type.IsNullableType() = nullable Then
            Return Type
        Else
            Return If(nullable,
                        GetType(Nullable(Of)).MakeGenericType(Type),
                        Type.UnwrapNullableType())
        End If

    End Function

    <Extension()>
    Public Function IsNumeric(type As Type) As Boolean
        type = type.UnwrapNullableType()

        Return type.IsInteger() OrElse
               type = GetType(Decimal) OrElse
               type = GetType(Single) OrElse
               type = GetType(Double)
    End Function

    <Extension()>
    Function IsInteger(type As Type) As Boolean
        type = type.UnwrapNullableType()
        Return type = GetType(Integer) OrElse
               type = GetType(Long) OrElse
               type = GetType(Short) OrElse
               type = GetType(Byte) OrElse
               type = GetType(UInteger) OrElse
               type = GetType(ULong) OrElse
               type = GetType(UShort) OrElse
               type = GetType(SByte) OrElse
               type = GetType(Char)
    End Function

    <Extension()>
    Public Function IsSignedInteger(type As Type) As Boolean
        Return type = GetType(Integer) OrElse
               type = GetType(Long) OrElse
               type = GetType(Short) OrElse
               type = GetType(SByte)
    End Function

    <Extension()>
    Public Function IsTupleType(type As Type) As Boolean
        If type = GetType(Tuple) Then Return True

        If type.IsGenericType Then
            Dim genericDefinition = type.GetGenericTypeDefinition()
            If genericDefinition = GetType(Tuple(Of)) OrElse
               genericDefinition = GetType(Tuple(Of ,)) OrElse
               genericDefinition = GetType(Tuple(Of ,,)) OrElse
               genericDefinition = GetType(Tuple(Of ,,,)) OrElse
               genericDefinition = GetType(Tuple(Of ,,,,)) OrElse
               genericDefinition = GetType(Tuple(Of ,,,,,)) OrElse
               genericDefinition = GetType(Tuple(Of ,,,,,,)) OrElse
               genericDefinition = GetType(Tuple(Of ,,,,,,,)) Then
                Return True
            End If
        End If

        Return False
    End Function

    <Extension()>
    Function GetAnyProperty(type As Type, name As String) As PropertyInfo
        Dim props = type.GetRuntimeProperties().Where(Function(p) p.Name = name).ToList()
        If props.Count > 1 Then
            Throw New AmbiguousMatchException()
        End If

        Return props.SingleOrDefault()
    End Function

    <Extension()>
    Public Function GetRequiredMethod(type As Type, name As String, ParamArray parameters As Type()) As MethodInfo
        Dim method = type.GetTypeInfo().GetMethod(name, parameters)

        If method Is Nothing AndAlso parameters.Length = 0 Then
            method = type.GetMethod(name)
        End If

        If method Is Nothing Then
            Throw New InvalidOperationException
        End If

        Return method
    End Function

    <Extension()>
    Function IsInstantiable(type As Type) As Boolean
        Return Not type.IsAbstract AndAlso
               Not type.IsInterface AndAlso
               (Not type.IsGenericType OrElse Not type.IsGenericTypeDefinition)
    End Function

    <Extension()>
    Function UnwrapEnumType(type As Type) As Type
        Dim isNullable = type.IsNullableType()
        Dim underlyingNonNullableType = If(isNullable, type.UnwrapNullableType(), type)
        If Not underlyingNonNullableType.GetTypeInfo().IsEnum Then
            Return type
        End If

        Dim underlyingEnumType = [Enum].GetUnderlyingType(underlyingNonNullableType)
        Return If(isNullable, MakeNullable(underlyingEnumType), underlyingEnumType)
    End Function

    <Extension()>
    Function GetSequenceType(type As Type) As Type
        Dim sequenceType = TryGetSequenceType(type)
        If sequenceType Is Nothing Then
            Throw New ArgumentException($"The type {type.Name} does not represent a sequence")
        End If

        Return sequenceType
    End Function

    <Extension()>
    Function TryGetSequenceType(type As Type) As Type
        Return If(type.TryGetElementType(GetType(IEnumerable(Of ))), type.TryGetElementType(GetType(IAsyncEnumerable(Of ))))
    End Function

    <Extension()>
    Public Function TryGetElementType(type As Type, interfaceOrBaseType As Type) As Type
        If type.IsGenericTypeDefinition Then
            Return Nothing
        End If

        Dim types = GetGenericTypeImplementations(type, interfaceOrBaseType)

        Dim singleImplementation As Type = Nothing
        For Each implementation In types
            If singleImplementation Is Nothing Then
                singleImplementation = implementation
            Else
                singleImplementation = Nothing
                Exit For
            End If
        Next

        Return singleImplementation?.GenericTypeArguments.FirstOrDefault()
    End Function

    <Extension()>
    Public Iterator Function GetGenericTypeImplementations(type As Type, interfaceOrBaseType As Type) As IEnumerable(Of Type)
        Dim typeInfo = type.GetTypeInfo()
        If Not typeInfo.IsGenericTypeDefinition Then
            Dim baseTypes = If(interfaceOrBaseType.GetTypeInfo().IsInterface,
                                typeInfo.ImplementedInterfaces,
                                type.GetBaseTypes())

            For Each baseType In baseTypes
                If baseType.IsGenericType AndAlso
                   baseType.GetGenericTypeDefinition() = interfaceOrBaseType Then
                    Yield baseType
                End If
            Next

            If type.IsGenericType AndAlso
               type.GetGenericTypeDefinition() = interfaceOrBaseType Then
                Yield type
            End If
        End If
    End Function

    <Extension()>
    Public Function GetRequiredRuntimeMethod(type As Type, name As String, ParamArray parameters As Type()) As MethodInfo
        Dim r = type.GetTypeInfo().GetRuntimeMethod(name, parameters)
        If r Is Nothing Then Throw New InvalidOperationException($"Could Not find method '{name}' on type '{type}'")
        Return r
    End Function

    <Extension()>
    Iterator Function GetBaseTypes(type As Type) As IEnumerable(Of Type)
        Dim currentType = type.BaseType
        While currentType IsNot Nothing
            Yield currentType
            currentType = currentType.BaseType
        End While
    End Function

    <Extension()>
    Iterator Function GetTypesInHierarchy(type As Type) As IEnumerable(Of Type)
        Dim currentType = type
        While currentType IsNot Nothing
            Yield currentType
            currentType = currentType.BaseType
        End While
    End Function

    <Extension()>
    Function GetDeclaredConstructor(type As Type, types As Type()) As ConstructorInfo
        types = If(types, Array.Empty(Of Type)())
        Return type.GetTypeInfo().DeclaredConstructors.SingleOrDefault(
            Function(c)
                Return Not c.IsStatic AndAlso
                       c.GetParameters().Select(Function(p) p.ParameterType).SequenceEqual(types)
            End Function)
    End Function

    <Extension()>
    Iterator Function GetPropertiesInHierarchy(type As Type, name As String) As IEnumerable(Of PropertyInfo)
        Dim currentType = type
        Do
            Dim typeInfo = currentType.GetTypeInfo()
            For Each propertyInfo In typeInfo.DeclaredProperties
                If propertyInfo.Name.Equals(name, StringComparison.Ordinal) AndAlso
                   Not (If(propertyInfo.GetMethod, propertyInfo.SetMethod)).IsStatic Then
                    Yield propertyInfo
                End If
            Next

            currentType = typeInfo.BaseType
        Loop While currentType IsNot Nothing
    End Function

    <Extension()>
    Public Iterator Function GetMembersInHierarchy(type As Type) As IEnumerable(Of MemberInfo)
        Dim currentType = type

        Do
            ' Do the whole hierarchy for properties first since looking for fields is slower.
            For Each propertyInfo In currentType.GetRuntimeProperties().
                                        Where(Function(pi) Not If(pi.GetMethod, pi.SetMethod).IsStatic)
                Yield propertyInfo
            Next

            For Each fieldInfo In currentType.GetRuntimeFields().
                                    Where(Function(f) Not f.IsStatic)
                Yield fieldInfo
            Next

            currentType = currentType.BaseType
        Loop While currentType IsNot Nothing
    End Function


    Private ReadOnly _commonTypeDictionary As New Dictionary(Of Type, Object) From
        {{GetType(Integer), Nothing},
         {GetType(Guid), Nothing},
         {GetType(Date), Nothing},
         {GetType(DateTimeOffset), Nothing},
         {GetType(Long), Nothing},
         {GetType(Boolean), Nothing},
         {GetType(Double), Nothing},
         {GetType(Short), Nothing},
         {GetType(Single), Nothing},
         {GetType(Byte), Nothing},
         {GetType(Char), Nothing},
         {GetType(UInteger), Nothing},
         {GetType(UShort), Nothing},
         {GetType(ULong), Nothing},
         {GetType(SByte), Nothing}}

    <Extension()>
    Function GetDefaultValue(type As Type) As Object
        If Not type.GetTypeInfo().IsValueType Then
            Return Nothing
        End If

        Dim value As Object = Nothing
        Return If(_commonTypeDictionary.TryGetValue(type, value), value, Activator.CreateInstance(type))
    End Function

    <Extension()>
    Function GetConstructibleTypes(assembly As Assembly) As IEnumerable(Of TypeInfo)
        Return assembly.GetLoadableDefinedTypes().
                            Where(Function(t) Not t.IsAbstract AndAlso Not t.IsGenericTypeDefinition)
    End Function

    <Extension()>
    Function GetLoadableDefinedTypes(assembly As Assembly) As IEnumerable(Of TypeInfo)
        Try
            Return assembly.DefinedTypes
        Catch ex As ReflectionTypeLoadException
            Return ex.Types.Where(Function(t) t IsNot Nothing).
                            Select(Function(t) IntrospectionExtensions.GetTypeInfo(t))
        End Try
    End Function

    <Extension()>
    Iterator Function GetNamespaces(type As Type) As IEnumerable(Of String)
        If _builtInTypeNames.ContainsKey(type) Then
            Return
        End If

        Yield type.Namespace

        If type.IsGenericType Then
            For Each typeArgument In type.GenericTypeArguments
                For Each ns In typeArgument.GetNamespaces()
                    Yield ns
                Next
            Next
        End If
    End Function

End Module
