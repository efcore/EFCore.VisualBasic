Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal

Namespace Design.AnnotationCodeGeneratorProvider

    ''' <summary>
    '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
    '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
    '''     any release. You should only use it directly in your code with extreme caution And knowing that
    '''     doing so can result in application failures when updating to a New Entity Framework Core release.
    ''' </summary>
    Public Class SqlServerVisualBasicRuntimeAnnotationCodeGenerator
        Inherits RelationalVisualBasicRuntimeAnnotationCodeGenerator

        ''' <summary>
        '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
        '''     the same compatibility standards as public APIs. It may be changed Or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution And knowing that
        '''     doing so can result in application failures when updating to a New Entity Framework Core release.
        ''' </summary>
        Public Sub New(vbHelper As IVisualBasicHelper)
            MyBase.New(vbHelper)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate(model As IModel, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            If Not parameters.IsRuntime Then
                Dim annotations = parameters.Annotations
                annotations.Remove(SqlServerAnnotationNames.IdentityIncrement)
                annotations.Remove(SqlServerAnnotationNames.IdentitySeed)
                annotations.Remove(SqlServerAnnotationNames.MaxDatabaseSize)
                annotations.Remove(SqlServerAnnotationNames.PerformanceLevelSql)
                annotations.Remove(SqlServerAnnotationNames.ServiceTierSql)
            End If

            MyBase.Generate(model, parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate([Property] As IProperty, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            If Not parameters.IsRuntime Then
                Dim annotations = parameters.Annotations
                annotations.Remove(SqlServerAnnotationNames.IdentityIncrement)
                annotations.Remove(SqlServerAnnotationNames.IdentitySeed)
                annotations.Remove(SqlServerAnnotationNames.Sparse)

                If Not annotations.ContainsKey(SqlServerAnnotationNames.ValueGenerationStrategy) Then
                    annotations(SqlServerAnnotationNames.ValueGenerationStrategy) = [Property].GetValueGenerationStrategy()
                End If
            End If

            MyBase.Generate([Property], parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate(index As IIndex, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            If Not parameters.IsRuntime Then
                Dim annotations = parameters.Annotations
                annotations.Remove(SqlServerAnnotationNames.Clustered)
                annotations.Remove(SqlServerAnnotationNames.CreatedOnline)
                annotations.Remove(SqlServerAnnotationNames.Include)
                annotations.Remove(SqlServerAnnotationNames.FillFactor)
            End If

            MyBase.Generate(index, parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate(aKey As IKey, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            If Not parameters.IsRuntime Then
                Dim annotations = parameters.Annotations
                annotations.Remove(SqlServerAnnotationNames.Clustered)
            End If

            MyBase.Generate(aKey, parameters)
        End Sub
    End Class

End Namespace
