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
                With parameters.Annotations
                    .Remove(SqlServerAnnotationNames.IdentityIncrement)
                    .Remove(SqlServerAnnotationNames.IdentitySeed)
                    .Remove(SqlServerAnnotationNames.MaxDatabaseSize)
                    .Remove(SqlServerAnnotationNames.PerformanceLevelSql)
                    .Remove(SqlServerAnnotationNames.ServiceTierSql)
                End With
            End If

            MyBase.Generate(model, parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate([Property] As IProperty, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            If Not parameters.IsRuntime Then
                With parameters.Annotations
                    .Remove(SqlServerAnnotationNames.IdentityIncrement)
                    .Remove(SqlServerAnnotationNames.IdentitySeed)
                    .Remove(SqlServerAnnotationNames.Sparse)

                    If Not .ContainsKey(SqlServerAnnotationNames.ValueGenerationStrategy) Then
                        .Item(SqlServerAnnotationNames.ValueGenerationStrategy) = [Property].GetValueGenerationStrategy()
                    End If
                End With
            End If

            MyBase.Generate([Property], parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate(index As IIndex, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            If Not parameters.IsRuntime Then
                With parameters.Annotations
                    .Remove(SqlServerAnnotationNames.Clustered)
                    .Remove(SqlServerAnnotationNames.CreatedOnline)
                    .Remove(SqlServerAnnotationNames.Include)
                    .Remove(SqlServerAnnotationNames.FillFactor)
                End With
            End If

            MyBase.Generate(index, parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate(aKey As IKey, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            If Not parameters.IsRuntime Then
                With parameters.Annotations
                    .Remove(SqlServerAnnotationNames.Clustered)
                End With
            End If

            MyBase.Generate(aKey, parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate(entityType As IEntityType, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            If Not parameters.IsRuntime Then
                With parameters.Annotations
                    .Remove(SqlServerAnnotationNames.TemporalHistoryTableName)
                    .Remove(SqlServerAnnotationNames.TemporalHistoryTableSchema)
                    .Remove(SqlServerAnnotationNames.TemporalPeriodEndColumnName)
                    .Remove(SqlServerAnnotationNames.TemporalPeriodEndPropertyName)
                    .Remove(SqlServerAnnotationNames.TemporalPeriodStartColumnName)
                    .Remove(SqlServerAnnotationNames.TemporalPeriodStartPropertyName)
                End With
            End If

            MyBase.Generate(entityType, parameters)
        End Sub

    End Class

End Namespace
