Imports Microsoft.EntityFrameworkCore.Cosmos.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Metadata

Namespace Design.AnnotationCodeGeneratorProvider

    ''' <summary>
    '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
    '''     any release. You should only use it directly in your code with extreme caution and knowing that
    '''     doing so can result in application failures when updating to a new Entity Framework Core release.
    ''' </summary>
    Public Class CosmosVisualBasicRuntimeAnnotationCodeGenerator
        Inherits VisualBasicRuntimeAnnotationCodeGenerator

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Public Sub New(vbHelper As IVisualBasicHelper)
            MyBase.New(vbHelper)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate(model As IModel, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            Dim annotations = parameters.Annotations
            If Not parameters.IsRuntime Then
                annotations.Remove(CosmosAnnotationNames.Throughput)
            End If

            MyBase.Generate(model, parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate(entityType As IEntityType, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            If Not parameters.IsRuntime Then
                With parameters.Annotations
                    .Remove(CosmosAnnotationNames.AnalyticalStoreTimeToLive)
                    .Remove(CosmosAnnotationNames.DefaultTimeToLive)
                    .Remove(CosmosAnnotationNames.Throughput)
                End With
            End If

            MyBase.Generate(entityType, parameters)
        End Sub
    End Class
End Namespace
