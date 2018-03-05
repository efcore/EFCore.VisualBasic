Imports System.Reflection
Imports System.Runtime.CompilerServices

<DebuggerStepThrough>
Module SharedTypeExtensions

    <Extension()>
    Public Function UnwrapNullableType(type As Type) As Type
        Return If(Nullable.GetUnderlyingType(type), type)
    End Function

    <Extension()>
    Function IsNullableType(ByVal type As Type) As Boolean
        Dim typeInfo = type.GetTypeInfo()
        Return Not typeInfo.IsValueType OrElse typeInfo.IsGenericType AndAlso typeInfo.GetGenericTypeDefinition() = GetType(Nullable(Of ))
    End Function

    <Extension()>
    Function IsValidEntityType(ByVal type As Type) As Boolean
        Return type.GetTypeInfo().IsClass
    End Function

    <Extension()>
    Function MakeNullable(ByVal type As Type) As Type
        Return If(type.IsNullableType(), type, GetType(Nullable(Of )).MakeGenericType(type))
    End Function

    <Extension()>
    Function IsInteger(ByVal type As Type) As Boolean
        type = type.UnwrapNullableType()
        Return type = GetType(Integer) OrElse type = GetType(Long) OrElse type = GetType(Short) OrElse type = GetType(Byte) OrElse type = GetType(UInteger) OrElse type = GetType(ULong) OrElse type = GetType(UShort) OrElse type = GetType(SByte) OrElse type = GetType(Char)
    End Function

    <Extension()>
    Function GetAnyProperty(ByVal type As Type, ByVal name As String) As PropertyInfo
        Dim props = type.GetRuntimeProperties().Where(Function(p) p.Name = name).ToList()
        If props.Count > 1 Then
            Throw New AmbiguousMatchException()
        End If

        Return props.SingleOrDefault()
    End Function

    <Extension()>
    Function IsInstantiable(ByVal type As Type) As Boolean
        Return IsInstantiable(type.GetTypeInfo())
    End Function

    Private Function IsInstantiable(ByVal type As TypeInfo) As Boolean
        Return Not type.IsAbstract AndAlso Not type.IsInterface AndAlso (Not type.IsGenericType OrElse Not type.IsGenericTypeDefinition)
    End Function

    <Extension()>
    Function IsGrouping(ByVal type As Type) As Boolean
        Return IsGrouping(type.GetTypeInfo())
    End Function

    Private Function IsGrouping(ByVal type As TypeInfo) As Boolean
        Return type.IsGenericType AndAlso (type.GetGenericTypeDefinition() = GetType(IGrouping(Of , )) OrElse type.GetGenericTypeDefinition() = GetType(IAsyncGrouping(Of , )))
    End Function

    <Extension()>
    Function UnwrapEnumType(ByVal type As Type) As Type
        Dim isNullable = type.IsNullableType()
        Dim underlyingNonNullableType = If(isNullable, type.UnwrapNullableType(), type)
        If Not underlyingNonNullableType.GetTypeInfo().IsEnum Then
            Return type
        End If

        Dim underlyingEnumType = [Enum].GetUnderlyingType(underlyingNonNullableType)
        Return If(isNullable, MakeNullable(underlyingEnumType), underlyingEnumType)
    End Function

    <Extension()>
    Function GetSequenceType(ByVal type As Type) As Type
        Dim sequenceType = TryGetSequenceType(type)
        If sequenceType Is Nothing Then
            Throw New ArgumentException()
        End If

        Return sequenceType
    End Function

    <Extension()>
    Function TryGetSequenceType(ByVal type As Type) As Type
        Return If(type.TryGetElementType(GetType(IEnumerable(Of ))), type.TryGetElementType(GetType(IAsyncEnumerable(Of ))))
    End Function

    <Extension()>
    Function TryGetElementType(ByVal type As Type, ByVal interfaceOrBaseType As Type) As Type
        If Not type.GetTypeInfo().IsGenericTypeDefinition Then
            Dim types = GetGenericTypeImplementations(type, interfaceOrBaseType).ToList()
            Return If(types.Count = 1, types(0).GetTypeInfo().GenericTypeArguments.FirstOrDefault(), Nothing)
        End If

        Return Nothing
    End Function

    <Extension()>
    Function GetGenericTypeImplementations(ByVal type As Type, ByVal interfaceOrBaseType As Type) As IEnumerable(Of Type)
        Dim typeInfo = type.GetTypeInfo()
        If Not typeInfo.IsGenericTypeDefinition Then
            Return (If(interfaceOrBaseType.GetTypeInfo().IsInterface, typeInfo.ImplementedInterfaces, type.GetBaseTypes())).Union({type}).Where(Function(t) t.GetTypeInfo().IsGenericType AndAlso t.GetGenericTypeDefinition() = interfaceOrBaseType)
        End If

        Return Enumerable.Empty(Of Type)()
    End Function

    <Extension()>
    Iterator Function GetBaseTypes(ByVal type As Type) As IEnumerable(Of Type)
        type = type.GetTypeInfo().BaseType
        While type IsNot Nothing
            Yield type
            type = type.GetTypeInfo().BaseType
        End While
    End Function

    <Extension()>
    Iterator Function GetTypesInHierarchy(ByVal type As Type) As IEnumerable(Of Type)
        While type IsNot Nothing
            Yield type
            type = type.GetTypeInfo().BaseType
        End While
    End Function

    <Extension()>
    Function GetDeclaredConstructor(ByVal type As Type, ByVal types As Type()) As ConstructorInfo
        types = If(types, New Type(-1) {})
        Return type.GetTypeInfo().DeclaredConstructors.SingleOrDefault(Function(c) Not c.IsStatic AndAlso c.GetParameters().[Select](Function(p) p.ParameterType).SequenceEqual(types))
    End Function

    <Extension()>
    Iterator Function GetPropertiesInHierarchy(ByVal type As Type, ByVal name As String) As IEnumerable(Of PropertyInfo)
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
    Iterator Function GetMembersInHierarchy(ByVal type As Type, ByVal name As String) As IEnumerable(Of MemberInfo)
        For Each propertyInfo In type.GetRuntimeProperties().Where(Function(pi) pi.Name = name AndAlso Not (If(pi.GetMethod, pi.SetMethod)).IsStatic)
            Yield propertyInfo
        Next

        For Each fieldInfo In type.GetRuntimeFields().Where(Function(f) f.Name = name AndAlso Not f.IsStatic)
            Yield fieldInfo
        Next
    End Function

    Private ReadOnly _commonTypeDictionary As Dictionary(Of Type, Object) = New Dictionary(Of Type, Object) From {{GetType(Integer), Nothing}, {GetType(Guid), Nothing}, {GetType(DateTime), Nothing}, {GetType(DateTimeOffset), Nothing}, {GetType(Long), Nothing}, {GetType(Boolean), Nothing}, {GetType(Double), Nothing}, {GetType(Short), Nothing}, {GetType(Single), Nothing}, {GetType(Byte), Nothing}, {GetType(Char), Nothing}, {GetType(UInteger), Nothing}, {GetType(UShort), Nothing}, {GetType(ULong), Nothing}, {GetType(SByte), Nothing}}

    <Extension()>
    Function GetDefaultValue(ByVal type As Type) As Object
        If Not type.GetTypeInfo().IsValueType Then
            Return Nothing
        End If

        Dim value As Object = Nothing
        Return If(_commonTypeDictionary.TryGetValue(type, value), value, Activator.CreateInstance(type))
    End Function

    <Extension()>
    Function GetConstructibleTypes(ByVal assembly As Assembly) As IEnumerable(Of TypeInfo)
        Return assembly.GetLoadableDefinedTypes().Where(Function(t) Not t.IsAbstract AndAlso Not t.IsGenericTypeDefinition)
    End Function

    <Extension()>
    Function GetLoadableDefinedTypes(ByVal assembly As Assembly) As IEnumerable(Of TypeInfo)
        Try
            Return assembly.DefinedTypes
        Catch ex As ReflectionTypeLoadException
            Return ex.Types.Where(Function(t) t IsNot Nothing).[Select](Function(t) IntrospectionExtensions.GetTypeInfo(t))
        End Try
    End Function
End Module