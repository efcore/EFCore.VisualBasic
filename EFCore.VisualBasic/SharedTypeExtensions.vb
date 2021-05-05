Imports System.Reflection
Imports System.Runtime.CompilerServices

'<DebuggerStepThrough>
Friend Module SharedTypeExtensions

    <Extension()>
    Public Function UnwrapNullableType(type As Type) As Type
        Return If(Nullable.GetUnderlyingType(type), type)
    End Function

    <Extension()>
    Function IsNullableType(type As Type) As Boolean
        Dim typeInfo = type.GetTypeInfo()
        Return Not typeInfo.IsValueType OrElse
               typeInfo.IsGenericType AndAlso typeInfo.GetGenericTypeDefinition() = GetType(Nullable(Of ))
    End Function

    <Extension()>
    Function IsValidEntityType(type As Type) As Boolean
        Return type.GetTypeInfo().IsClass
    End Function

    <Extension()>
    Function MakeNullable(type As Type) As Type
        Return If(type.IsNullableType(), type, GetType(Nullable(Of )).MakeGenericType(type))
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
    Function GetAnyProperty(type As Type, name As String) As PropertyInfo
        Dim props = type.GetRuntimeProperties().Where(Function(p) p.Name = name).ToList()
        If props.Count > 1 Then
            Throw New AmbiguousMatchException()
        End If

        Return props.SingleOrDefault()
    End Function

    <Extension()>
    Function IsInstantiable(type As Type) As Boolean
        Return IsInstantiable(type.GetTypeInfo())
    End Function

    Private Function IsInstantiable(type As TypeInfo) As Boolean
        Return Not type.IsAbstract AndAlso Not type.IsInterface AndAlso (Not type.IsGenericType OrElse Not type.IsGenericTypeDefinition)
    End Function

    <Extension()>
    Function IsNumeric(type As Type) As Boolean
        type = type.UnwrapNullableType()

        Return type.IsInteger() OrElse
               type = GetType(Decimal) OrElse
               type = GetType(Single) OrElse
               type = GetType(Double)
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
            Throw New ArgumentException()
        End If

        Return sequenceType
    End Function

    <Extension()>
    Function TryGetSequenceType(type As Type) As Type
        Return If(type.TryGetElementType(GetType(IEnumerable(Of ))), type.TryGetElementType(GetType(IAsyncEnumerable(Of ))))
    End Function

    <Extension()>
    Function TryGetElementType(type As Type, interfaceOrBaseType As Type) As Type
        If Not type.GetTypeInfo().IsGenericTypeDefinition Then
            Dim types = GetGenericTypeImplementations(type, interfaceOrBaseType).ToList()
            Return If(types.Count = 1, types(0).GetTypeInfo().GenericTypeArguments.FirstOrDefault(), Nothing)
        End If

        Return Nothing
    End Function

    <Extension()>
    Function GetGenericTypeImplementations(type As Type, interfaceOrBaseType As Type) As IEnumerable(Of Type)
        Dim typeInfo = type.GetTypeInfo()
        If Not typeInfo.IsGenericTypeDefinition Then
            Return (If(interfaceOrBaseType.GetTypeInfo().IsInterface, typeInfo.ImplementedInterfaces, type.GetBaseTypes())).Union({type}).Where(Function(t) t.GetTypeInfo().IsGenericType AndAlso t.GetGenericTypeDefinition() = interfaceOrBaseType)
        End If

        Return Enumerable.Empty(Of Type)()
    End Function

    <Extension()>
    Iterator Function GetBaseTypes(type As Type) As IEnumerable(Of Type)
        type = type.GetTypeInfo().BaseType
        While type IsNot Nothing
            Yield type
            type = type.GetTypeInfo().BaseType
        End While
    End Function

    <Extension()>
    Iterator Function GetTypesInHierarchy(type As Type) As IEnumerable(Of Type)
        While type IsNot Nothing
            Yield type
            type = type.GetTypeInfo().BaseType
        End While
    End Function

    <Extension()>
    Function GetDeclaredConstructor(type As Type, types As Type()) As ConstructorInfo
        types = If(types, New Type(-1) {})
        Return type.GetTypeInfo().DeclaredConstructors.SingleOrDefault(Function(c) Not c.IsStatic AndAlso c.GetParameters().[Select](Function(p) p.ParameterType).SequenceEqual(types))
    End Function

    <Extension()>
    Iterator Function GetPropertiesInHierarchy(type As Type, name As String) As IEnumerable(Of PropertyInfo)
        Do
            Dim typeInfo = type.GetTypeInfo()
            Dim propertyInfo = typeInfo.GetDeclaredProperty(name)
            If propertyInfo IsNot Nothing AndAlso Not (If(propertyInfo.GetMethod, propertyInfo.SetMethod)).IsStatic Then
                Yield propertyInfo
            End If

            type = typeInfo.BaseType
        Loop While type IsNot Nothing
    End Function

    <Extension()>
    Iterator Function GetMembersInHierarchy(type As Type, name As String) As IEnumerable(Of MemberInfo)
        For Each propertyInfo In type.GetRuntimeProperties().Where(Function(pi) pi.Name = name AndAlso Not (If(pi.GetMethod, pi.SetMethod)).IsStatic)
            Yield propertyInfo
        Next

        For Each fieldInfo In type.GetRuntimeFields().Where(Function(f) f.Name = name AndAlso Not f.IsStatic)
            Yield fieldInfo
        Next
    End Function


    Private ReadOnly _commonTypeDictionary As New Dictionary(Of Type, Object) From
        {{GetType(Integer), Nothing},
         {GetType(Guid), Nothing},
         {GetType(DateTime), Nothing},
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
        Return assembly.GetLoadableDefinedTypes().Where(Function(t) Not t.IsAbstract AndAlso Not t.IsGenericTypeDefinition)
    End Function

    <Extension()>
    Function GetLoadableDefinedTypes(assembly As Assembly) As IEnumerable(Of TypeInfo)
        Try
            Return assembly.DefinedTypes
        Catch ex As ReflectionTypeLoadException
            Return ex.Types.Where(Function(t) t IsNot Nothing).[Select](Function(t) IntrospectionExtensions.GetTypeInfo(t))
        End Try
    End Function

    <Extension()>
    Iterator Function GetNamespaces(type As Type) As IEnumerable(Of String)
        If _builtInTypeNames.ContainsKey(type) Then
            Return
        End If

        Yield type.[Namespace]

        If type.IsGenericType Then
            For Each typeArgument In type.GenericTypeArguments
                For Each ns In typeArgument.GetNamespaces()
                    Yield ns
                Next
            Next
        End If
    End Function

End Module