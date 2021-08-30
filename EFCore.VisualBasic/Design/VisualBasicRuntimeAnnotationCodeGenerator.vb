Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal

Namespace Design
    ''' <summary>
    '''     <para>
    '''         Base class to be used by database providers when implementing an <see cref="IVisualBasicRuntimeAnnotationCodeGenerator"/>
    '''     </para>
    ''' </summary>
    Partial Public Class VisualBasicRuntimeAnnotationCodeGenerator
        Implements IVisualBasicRuntimeAnnotationCodeGenerator

        ''' <summary>
        '''     Initializes a new instance of this class.
        ''' </summary>
        ''' <param name="vbHelper"> The Visual Basic helper. </param>
        Public Sub New(vbHelper As IVisualBasicHelper)
            Me.VBCode = NotNull(vbHelper, NameOf(vbHelper))
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

            If Not parameters.IsRuntime Then
                With parameters.Annotations
                    .Remove(CoreAnnotationNames.PropertyAccessMode)
                    .Remove(CoreAnnotationNames.NavigationAccessMode)
                    .Remove(CoreAnnotationNames.DiscriminatorProperty)
                    .Remove(CoreAnnotationNames.AmbiguousNavigations)
                    .Remove(CoreAnnotationNames.NavigationCandidates)
                End With
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
        ''' <param name="parameters"> Parameters used during code generation. </param>
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
        ''' <param name="annotationName"> The annotation name. </param>
        ''' <param name="valueString"> The annotation value as a literal. </param>
        ''' <param name="parameters"> Additional parameters used during code generation. </param>
        Protected Overridable Sub GenerateSimpleAnnotation(annotationName As String,
                                                           valueString As String,
                                                           parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            If parameters.TargetName <> "this" Then
                parameters.MainBuilder.
                    Append(parameters.TargetName).
                    Append("."c)
            End If

            parameters.MainBuilder.
                Append(If(parameters.IsRuntime, "AddRuntimeAnnotation(", "AddAnnotation(")).
                Append(VBCode.Literal(annotationName)).
                Append(", ").
                Append(valueString).
                AppendLine(")")
        End Sub

        ''' <summary>
        '''     Adds the namespaces for the given type.
        ''' </summary>
        ''' <param name="type"> A type. </param>
        ''' <param name="namespaces"> The set of namespaces to add to. </param>
        Protected Overridable Sub AddNamespace(type As Type, namespaces As ISet(Of String))
            If type.IsNested Then
                AddNamespace(type.DeclaringType, namespaces)
            End If

            If type.Namespace IsNot Nothing Then
                namespaces.Add(type.Namespace)
            End If

            If type.IsGenericType Then
                For Each argument As Type In type.GenericTypeArguments
                    AddNamespace(argument, namespaces)
                Next
            End If

            Dim sequenceType = type.TryGetSequenceType()
            If sequenceType IsNot Nothing Then
                AddNamespace(sequenceType, namespaces)
            End If
        End Sub

        Protected Function TryGetAndRemove(Of TKey, TValue, TReturn)(source As IDictionary(Of TKey, TValue),
                                                                     key As TKey,
                                                                     ByRef annotationValue As TReturn) As Boolean

            Return source.TryGetAndRemove(key, annotationValue)
        End Function

    End Class
End Namespace
