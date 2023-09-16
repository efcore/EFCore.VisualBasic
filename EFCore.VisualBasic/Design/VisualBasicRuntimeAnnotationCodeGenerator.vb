Imports System.Reflection
Imports Microsoft.EntityFrameworkCore.ChangeTracking
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Storage
Imports Microsoft.EntityFrameworkCore.Storage.Internal
Imports Microsoft.EntityFrameworkCore.Storage.Json
Imports Microsoft.EntityFrameworkCore.Storage.ValueConversion

Namespace Design
    ''' <summary>
    '''     Base class to be used by database providers when implementing an <see cref="IVisualBasicRuntimeAnnotationCodeGenerator"/>
    ''' </summary>
    Public Class VisualBasicRuntimeAnnotationCodeGenerator
        Implements IVisualBasicRuntimeAnnotationCodeGenerator

        ''' <summary>
        '''     Initializes a new instance of this class.
        ''' </summary>
        ''' <param name="vbHelper">The Visual Basic helper.</param>
        Public Sub New(vbHelper As IVisualBasicHelper)
            VBCode = NotNull(vbHelper, NameOf(vbHelper))
        End Sub

        ''' <summary>
        '''     The Visual Basic helper.
        ''' </summary>
        Protected VBCode As IVisualBasicHelper

        ''' <inheritdoc/>
        Public Overridable Sub Generate(model As IModel, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) _
            Implements IVisualBasicRuntimeAnnotationCodeGenerator.Generate

            With parameters.Annotations
                If Not parameters.IsRuntime Then
                    For Each annotation In parameters.Annotations
                        If CoreAnnotationNames.AllNames.Contains(annotation.Key) AndAlso
                           annotation.Key <> CoreAnnotationNames.ProductVersion AndAlso
                           annotation.Key <> CoreAnnotationNames.FullChangeTrackingNotificationsRequired Then

                            .Remove(annotation.Key)
                        End If
                    Next
                Else
                    .Remove(CoreAnnotationNames.ModelDependencies)
                    .Remove(CoreAnnotationNames.ReadOnlyModel)
                End If
            End With

            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <inheritdoc/>
        Public Overridable Sub Generate(entityType As IEntityType, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) _
            Implements IVisualBasicRuntimeAnnotationCodeGenerator.Generate

            Dim annotations = parameters.Annotations

            If Not parameters.IsRuntime Then
                For Each annotation In annotations
                    If CoreAnnotationNames.AllNames.Contains(annotation.Key) AndAlso
                       annotation.Key <> CoreAnnotationNames.DiscriminatorMappingComplete Then

                        annotations.Remove(annotation.Key)
                    End If
                Next
            End If

            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <inheritdoc />
        Public Overridable Sub Generate(complexProperty As IComplexProperty, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) _
            Implements IVisualBasicRuntimeAnnotationCodeGenerator.Generate

            Dim annotations = parameters.Annotations

            If Not parameters.IsRuntime Then
                For Each annotation In annotations
                    If CoreAnnotationNames.AllNames.Contains(annotation.Key) AndAlso
                       annotation.Key <> CoreAnnotationNames.DiscriminatorMappingComplete Then

                        annotations.Remove(annotation.Key)
                    End If
                Next
            End If

            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <inheritdoc />
        Public Overridable Sub Generate(complexType As IComplexType, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) _
            Implements IVisualBasicRuntimeAnnotationCodeGenerator.Generate

            Dim annotations = parameters.Annotations
            If Not parameters.IsRuntime Then
                For Each annotation In annotations
                    If CoreAnnotationNames.AllNames.Contains(annotation.Key) AndAlso
                       annotation.Key <> CoreAnnotationNames.DiscriminatorMappingComplete Then

                        annotations.Remove(annotation.Key)
                    End If
                Next
            End If

            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <inheritdoc/>
        Public Overridable Sub Generate(prop As IProperty, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) _
            Implements IVisualBasicRuntimeAnnotationCodeGenerator.Generate

            If Not parameters.IsRuntime Then
                For Each annotation In parameters.Annotations
                    If CoreAnnotationNames.AllNames.Contains(annotation.Key) Then
                        parameters.Annotations.Remove(annotation.Key)
                    End If
                Next
            End If

            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <inheritdoc/>
        Public Overridable Sub Generate(prop As IServiceProperty, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) _
            Implements IVisualBasicRuntimeAnnotationCodeGenerator.Generate

            If Not parameters.IsRuntime Then
                For Each annotation In parameters.Annotations
                    If CoreAnnotationNames.AllNames.Contains(annotation.Key) Then
                        parameters.Annotations.Remove(annotation.Key)
                    End If
                Next
            End If

            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <inheritdoc/>
        Public Overridable Sub Generate(key As IKey, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) _
            Implements IVisualBasicRuntimeAnnotationCodeGenerator.Generate

            If Not parameters.IsRuntime Then
                For Each annotation In parameters.Annotations
                    If CoreAnnotationNames.AllNames.Contains(annotation.Key) Then
                        parameters.Annotations.Remove(annotation.Key)
                    End If
                Next
            End If

            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <inheritdoc/>
        Public Overridable Sub Generate(foreignKey As IForeignKey, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) _
            Implements IVisualBasicRuntimeAnnotationCodeGenerator.Generate

            If Not parameters.IsRuntime Then
                For Each annotation In parameters.Annotations
                    If CoreAnnotationNames.AllNames.Contains(annotation.Key) Then
                        parameters.Annotations.Remove(annotation.Key)
                    End If
                Next
            End If

            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <inheritdoc/>
        Public Overridable Sub Generate(navigation As INavigation, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) _
            Implements IVisualBasicRuntimeAnnotationCodeGenerator.Generate

            If Not parameters.IsRuntime Then
                For Each annotation In parameters.Annotations
                    If CoreAnnotationNames.AllNames.Contains(annotation.Key) Then
                        parameters.Annotations.Remove(annotation.Key)
                    End If
                Next
            End If

            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <inheritdoc/>
        Public Overridable Sub Generate(navigation As ISkipNavigation, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) _
            Implements IVisualBasicRuntimeAnnotationCodeGenerator.Generate

            If Not parameters.IsRuntime Then
                For Each annotation In parameters.Annotations
                    If CoreAnnotationNames.AllNames.Contains(annotation.Key) Then
                        parameters.Annotations.Remove(annotation.Key)
                    End If
                Next
            End If

            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <inheritdoc/>
        Public Overridable Sub Generate(index As IIndex, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) _
            Implements IVisualBasicRuntimeAnnotationCodeGenerator.Generate

            If Not parameters.IsRuntime Then
                For Each annotation In parameters.Annotations
                    If CoreAnnotationNames.AllNames.Contains(annotation.Key) Then
                        parameters.Annotations.Remove(annotation.Key)
                    End If
                Next
            End If

            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <inheritdoc />
        Public Overridable Sub Generate(trigger As ITrigger, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) _
            Implements IVisualBasicRuntimeAnnotationCodeGenerator.Generate

            If Not parameters.IsRuntime Then
                Dim annotations = parameters.Annotations
                For Each annotation In annotations
                    Dim Key = annotation.Key

                    If CoreAnnotationNames.AllNames.Contains(Key) Then
                        annotations.Remove(Key)
                    End If
                Next
            End If

            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <inheritdoc />
        Public Overridable Sub Generate(typeConfiguration As ITypeMappingConfiguration, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) _
            Implements IVisualBasicRuntimeAnnotationCodeGenerator.Generate

            If Not parameters.IsRuntime Then
                For Each annotation In parameters.Annotations
                    If CoreAnnotationNames.AllNames.Contains(annotation.Key) Then
                        parameters.Annotations.Remove(annotation.Key)
                    End If
                Next
            End If

            GenerateSimpleAnnotations(parameters)
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotations using literals.
        ''' </summary>
        ''' <param name="parameters">Parameters used during code generation.</param>
        Protected Overridable Sub GenerateSimpleAnnotations(parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            For Each Annotation In parameters.Annotations.OrderBy(Function(a) a.Key)

                If Annotation.Value IsNot Nothing Then
                    AddNamespace(If(TryCast(Annotation.Value, Type), Annotation.Value.GetType()), parameters.Namespaces)
                End If

                GenerateSimpleAnnotation(Annotation.Key, VBCode.UnknownLiteral(Annotation.Value), parameters)
            Next
        End Sub

        ''' <summary>
        '''     Generates code to create the given annotation.
        ''' </summary>
        ''' <param name="annotationName">The annotation name.</param>
        ''' <param name="valueString">The annotation value as a literal.</param>
        ''' <param name="parameters">Additional parameters used during code generation.</param>
        Protected Overridable Sub GenerateSimpleAnnotation(annotationName As String,
                                                           valueString As String,
                                                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            If parameters.TargetName <> "Me" Then
                parameters.MainBuilder.
                    Append(parameters.TargetName).
                    Append("."c)
            End If

            parameters.MainBuilder.
                Append(If(parameters.IsRuntime, "AddRuntimeAnnotation(", "AddAnnotation(")).
                Append(VBCode.Literal(annotationName)).
                Append(", ").
                Append(valueString).
                AppendLine(")"c)
        End Sub

        ''' <summary>
        '''     Adds the namespaces for the given type.
        ''' </summary>
        ''' <param name="type">A type.</param>
        ''' <param name="namespaces">The set of namespaces to add to.</param>
        Public Shared Sub AddNamespace(type As Type, namespaces As ISet(Of String))

            If type.IsNested Then
                AddNamespace(type.DeclaringType, namespaces)
            ElseIf Not String.IsNullOrEmpty(type.Namespace) Then
                namespaces.Add(type.Namespace)
            End If

            If type.IsGenericType Then
                For Each argument As Type In type.GenericTypeArguments
                    AddNamespace(argument, namespaces)
                Next
            End If

            If type.IsArray Then
                AddNamespace(type.GetSequenceType(), namespaces)
                Exit Sub
            End If
        End Sub

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Public Shared Sub Create(converter As ValueConverter,
                                 parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters,
                                 codeHelper As IVisualBasicHelper)

            Dim mainBuilder = parameters.MainBuilder
            Dim constructor = converter.GetType().GetDeclaredConstructor({GetType(JsonValueReaderWriter)})
            Dim jsonReaderWriterProperty = converter.GetType().GetProperty(NameOf(CollectionToJsonStringConverter(Of Object).JsonReaderWriter))

            If constructor Is Nothing OrElse jsonReaderWriterProperty Is Nothing Then
                AddNamespace(GetType(ValueConverter(Of ,)), parameters.Namespaces)
                AddNamespace(converter.ModelClrType, parameters.Namespaces)
                AddNamespace(converter.ProviderClrType, parameters.Namespaces)

                mainBuilder.
                    Append("New ValueConverter(Of ").
                    Append(codeHelper.Reference(converter.ModelClrType)).
                    Append(", ").
                    Append(codeHelper.
                    Reference(converter.ProviderClrType)).
                    AppendLine(")(").
                    IncrementIndent().
                    Append(codeHelper.Expression(converter.ConvertToProviderExpression, parameters.Namespaces)).
                    AppendLine(","c).
                    Append(codeHelper.Expression(converter.ConvertFromProviderExpression, parameters.Namespaces)).
                    Append(")"c).
                    DecrementIndent()
            Else
                AddNamespace(converter.GetType(), parameters.Namespaces)

                mainBuilder.
                    Append("New ").
                    Append(codeHelper.Reference(converter.GetType())).
                    Append("("c)

                CreateJsonValueReaderWriter(DirectCast(jsonReaderWriterProperty.GetValue(converter), JsonValueReaderWriter), parameters, codeHelper)

                mainBuilder.
                    Append(")"c).
                    DecrementIndent()
            End If
        End Sub

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Public Shared Sub Create(comparer As ValueComparer,
                                 parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters,
                                 codeHelper As IVisualBasicHelper)

            Dim mainBuilder = parameters.MainBuilder
            Dim constructor = comparer.GetType().GetDeclaredConstructor({GetType(ValueComparer)})
            Dim elementComparerProperty = comparer.GetType().GetProperty(NameOf(ListComparer(Of Object).ElementComparer))

            If constructor Is Nothing OrElse elementComparerProperty Is Nothing Then
                AddNamespace(GetType(ValueComparer()), parameters.Namespaces)
                AddNamespace(comparer.Type, parameters.Namespaces)

                mainBuilder.
                    Append("New ValueComparer(Of ").
                    Append(codeHelper.Reference(comparer.Type)).
                    AppendLine(")(").
                    IncrementIndent().
                    AppendLines(codeHelper.Expression(comparer.EqualsExpression, parameters.Namespaces), skipFinalNewline:=True).
                    AppendLine(","c).
                    AppendLines(codeHelper.Expression(comparer.HashCodeExpression, parameters.Namespaces), skipFinalNewline:=True).
                    AppendLine(","c).
                    AppendLines(codeHelper.Expression(comparer.SnapshotExpression, parameters.Namespaces), skipFinalNewline:=True).
                    Append(")"c).
                    DecrementIndent()
            Else
                AddNamespace(comparer.GetType(), parameters.Namespaces)

                mainBuilder.
                    Append("New ").
                    Append(codeHelper.Reference(comparer.GetType())).Append("("c)

                Create(DirectCast(elementComparerProperty.GetValue(comparer), ValueComparer), parameters, codeHelper)

                mainBuilder.
                    Append(")"c).
                    DecrementIndent()
            End If
        End Sub

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Public Shared Sub CreateJsonValueReaderWriter(jsonValueReaderWriter As JsonValueReaderWriter,
                                                      parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters,
                                                      codeHelper As IVisualBasicHelper)

            Dim mainBuilder = parameters.MainBuilder
            Dim jsonValueReaderWriterType = jsonValueReaderWriter.GetType()

            Dim jsonConvertedValueReaderWriter = TryCast(jsonValueReaderWriter, IJsonConvertedValueReaderWriter)
            If jsonConvertedValueReaderWriter IsNot Nothing Then
                AddNamespace(jsonValueReaderWriterType, parameters.Namespaces)

                mainBuilder.
                    Append("New ").
                    Append(codeHelper.Reference(jsonValueReaderWriterType)).
                    AppendLine("("c).
                    IncrementIndent()

                CreateJsonValueReaderWriter(jsonConvertedValueReaderWriter.InnerReaderWriter, parameters, codeHelper)
                mainBuilder.AppendLine(","c)
                Create(jsonConvertedValueReaderWriter.Converter, parameters, codeHelper)

                mainBuilder.
                    Append(")"c).
                    DecrementIndent()

                Exit Sub
            End If

            Dim compositeJsonValueReaderWriter = TryCast(jsonValueReaderWriter, ICompositeJsonValueReaderWriter)
            If compositeJsonValueReaderWriter IsNot Nothing Then
                AddNamespace(jsonValueReaderWriterType, parameters.Namespaces)

                mainBuilder.
                    Append("New ").
                    Append(codeHelper.Reference(jsonValueReaderWriterType)).
                    AppendLine("("c).
                    IncrementIndent()

                CreateJsonValueReaderWriter(compositeJsonValueReaderWriter.InnerReaderWriter, parameters, codeHelper)

                mainBuilder.
                    Append(")"c).
                    DecrementIndent()
                Exit Sub
            End If

            CreateJsonValueReaderWriter(jsonValueReaderWriterType, parameters, codeHelper)
        End Sub

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Public Shared Sub CreateJsonValueReaderWriter(jsonValueReaderWriterType As Type,
                                                      parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters,
                                                      codeHelper As IVisualBasicHelper)

            Dim mainBuilder = parameters.MainBuilder
            AddNamespace(jsonValueReaderWriterType, parameters.Namespaces)

            Dim instanceProperty = jsonValueReaderWriterType.GetProperty("Instance")

            If instanceProperty IsNot Nothing AndAlso
               instanceProperty.IsStatic() AndAlso
               instanceProperty.GetMethod?.IsPublic = True AndAlso
               jsonValueReaderWriterType.IsAssignableFrom(instanceProperty.PropertyType) AndAlso
               jsonValueReaderWriterType.IsPublic Then

                mainBuilder.
                    Append(codeHelper.Reference(jsonValueReaderWriterType)).
                    Append(".Instance")
            Else
                mainBuilder.
                    Append("New ").
                    Append(codeHelper.Reference(jsonValueReaderWriterType)).
                    Append("()")
            End If
        End Sub

        ''' <inheritdoc />
        Public Overridable Function Create(typeMapping As CoreTypeMapping,
                                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters,
                                           Optional valueComparer As ValueComparer = Nothing,
                                           Optional keyValueComparer As ValueComparer = Nothing,
                                           Optional providerValueComparer As ValueComparer = Nothing) As Boolean _
            Implements IVisualBasicRuntimeAnnotationCodeGenerator.Create

            Dim mainBuilder = parameters.MainBuilder
            Dim code = VBCode
            Dim defaultInstance = CreateDefaultTypeMapping(typeMapping, parameters)

            If defaultInstance Is Nothing Then
                Return True
            End If

            mainBuilder.
                AppendLine(".Clone(").
                IncrementIndent()

            mainBuilder.Append("comparer:=")
            Create(If(valueComparer, typeMapping.Comparer), parameters, code)

            mainBuilder.
                AppendLine(","c).
                Append("keyComparer:=")
            Create(If(keyValueComparer, typeMapping.KeyComparer), parameters, code)

            mainBuilder.
                AppendLine(","c).
                Append("providerValueComparer:=")
            Create(If(providerValueComparer, typeMapping.ProviderValueComparer), parameters, code)

            If typeMapping.Converter IsNot Nothing AndAlso
               typeMapping.Converter IsNot defaultInstance.Converter Then

                mainBuilder.
                    AppendLine(","c).
                    Append("converter:=")

                Create(typeMapping.Converter, parameters, code)
            End If

            If typeMapping.Converter Is Nothing AndAlso
               typeMapping.ClrType <> defaultInstance.ClrType Then

                mainBuilder.
                    AppendLine(","c).
                    Append($"clrType:={code.Literal(typeMapping.ClrType)}")
            End If

            If typeMapping.JsonValueReaderWriter IsNot Nothing AndAlso
               typeMapping.JsonValueReaderWriter IsNot defaultInstance.JsonValueReaderWriter Then

                mainBuilder.
                    AppendLine(","c).
                    Append("jsonValueReaderWriter:=")

                CreateJsonValueReaderWriter(typeMapping.JsonValueReaderWriter, parameters, code)
            End If

            If typeMapping.ElementTypeMapping IsNot Nothing AndAlso
               typeMapping.ElementTypeMapping IsNot defaultInstance.ElementTypeMapping Then

                mainBuilder.AppendLine(","c).Append("elementMapping:=")

                Create(typeMapping.ElementTypeMapping, parameters)
            End If

            mainBuilder.
                Append(")"c).
                DecrementIndent()

            Return True
        End Function

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Protected Overridable Function CreateDefaultTypeMapping(
            typeMapping As CoreTypeMapping,
            parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters) As CoreTypeMapping

            Dim typeMappingType = typeMapping.GetType()
            Dim mainBuilder = parameters.MainBuilder
            Dim code = VBCode
            Dim defaultProperty = typeMappingType.GetProperty("Default")

            If defaultProperty Is Nothing OrElse
               Not defaultProperty.IsStatic() OrElse
               defaultProperty.GetMethod?.IsPublic <> True OrElse
               Not typeMappingType.IsAssignableFrom(defaultProperty.PropertyType) OrElse
               Not typeMappingType.IsPublic Then

                Throw New InvalidOperationException(
                    VBDesignStrings.CompiledModelIncompatibleTypeMapping(typeMappingType.ShortDisplayName()))
            End If

            AddNamespace(typeMappingType, parameters.Namespaces)
            mainBuilder.
                Append(code.Reference(typeMappingType)).
                Append(".Default")

            Dim defaultInstance = DirectCast(defaultProperty.GetValue(Nothing), CoreTypeMapping)
            Return If(typeMapping Is defaultInstance, Nothing, defaultInstance)
        End Function

        Protected Shared Function TryGetAndRemove(Of TKey, TValue, TReturn)(source As IDictionary(Of TKey, TValue),
                                                                     key As TKey,
                                                                     ByRef annotationValue As TReturn) As Boolean

            Return source.TryGetAndRemove(key, annotationValue)
        End Function
    End Class
End Namespace
