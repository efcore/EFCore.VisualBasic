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
        Public Overrides Sub Generate(model As IRelationalModel, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            If Not parameters.IsRuntime Then
                Dim annotations = parameters.Annotations
                annotations.Remove(SqlServerAnnotationNames.MemoryOptimized)
                annotations.Remove(SqlServerAnnotationNames.EditionOptions)
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
        Public Overrides Sub Generate(column As IColumn, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            If Not parameters.IsRuntime Then
                Dim annotations = parameters.Annotations
                annotations.Remove(SqlServerAnnotationNames.Identity)
                annotations.Remove(SqlServerAnnotationNames.Sparse)
                annotations.Remove(SqlServerAnnotationNames.IsTemporal)
                annotations.Remove(SqlServerAnnotationNames.TemporalHistoryTableName)
                annotations.Remove(SqlServerAnnotationNames.TemporalHistoryTableSchema)
                annotations.Remove(SqlServerAnnotationNames.TemporalPeriodStartColumnName)
                annotations.Remove(SqlServerAnnotationNames.TemporalPeriodEndColumnName)
            End If

            MyBase.Generate(column, parameters)
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
        Public Overrides Sub Generate(index As ITableIndex, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
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
                With parameters.Annotations
                    .Remove(SqlServerAnnotationNames.Clustered)
                End With
            End If

            MyBase.Generate(aKey, parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate(uniqueConstraint As IUniqueConstraint, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            If Not parameters.IsRuntime Then
                Dim annotations = parameters.Annotations
                annotations.Remove(SqlServerAnnotationNames.Clustered)
            End If

            MyBase.Generate(uniqueConstraint, parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate(entityType As IEntityType, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            If Not parameters.IsRuntime Then
                With parameters.Annotations
                    .Remove(SqlServerAnnotationNames.TemporalHistoryTableName)
                    .Remove(SqlServerAnnotationNames.TemporalHistoryTableSchema)
                    .Remove(SqlServerAnnotationNames.TemporalPeriodEndPropertyName)
                    .Remove(SqlServerAnnotationNames.TemporalPeriodStartPropertyName)
                End With
            End If

            MyBase.Generate(entityType, parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate(table As ITable, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)

            If Not parameters.IsRuntime Then
                Dim annotations = parameters.Annotations
                annotations.Remove(SqlServerAnnotationNames.MemoryOptimized)
                annotations.Remove(SqlServerAnnotationNames.TemporalHistoryTableName)
                annotations.Remove(SqlServerAnnotationNames.TemporalHistoryTableSchema)
                annotations.Remove(SqlServerAnnotationNames.TemporalPeriodEndColumnName)
                annotations.Remove(SqlServerAnnotationNames.TemporalPeriodStartColumnName)
            End If

            MyBase.Generate(table, parameters)
        End Sub

        ''' <inheritdoc />
        Public Overrides Sub Generate([overrides] As IRelationalPropertyOverrides, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            If Not parameters.IsRuntime Then
                Dim annotations = parameters.Annotations
                annotations.Remove(SqlServerAnnotationNames.IdentityIncrement)
                annotations.Remove(SqlServerAnnotationNames.IdentitySeed)
            End If

            MyBase.Generate([overrides], parameters)
        End Sub
    End Class
End Namespace
