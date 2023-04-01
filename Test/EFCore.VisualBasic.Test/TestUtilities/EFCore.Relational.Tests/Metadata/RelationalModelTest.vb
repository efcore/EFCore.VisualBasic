Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.TestUtilities
Imports Xunit

Namespace Metadata
    Public Module RelationalModelTest

        Public Sub AssertEqual(expectedModel As IRelationalModel, actualModel As IRelationalModel)
            DirectCast(expectedModel, RelationalModel).DefaultTables.Values.ZipAssert(
                DirectCast(actualModel, RelationalModel).DefaultTables.Values, AddressOf AssertEqual)

            expectedModel.Tables.ZipAssert(actualModel.Tables, AddressOf AssertEqual)
            expectedModel.Views.ZipAssert(actualModel.Views, AddressOf AssertEqual)
            expectedModel.Queries.ZipAssert(actualModel.Queries, AddressOf AssertEqual)
            expectedModel.Functions.ZipAssert(actualModel.Functions, AddressOf AssertEqual)
            expectedModel.StoredProcedures.ZipAssert(actualModel.StoredProcedures, AddressOf AssertEqual)

            Assert.Equal(DirectCast(expectedModel, RelationalModel).IsReadOnly, (DirectCast(actualModel, RelationalModel)).IsReadOnly)
            Assert.Equal(expectedModel.GetAnnotations(), actualModel.GetAnnotations(), AnnotationComparer.Instance)
            Assert.Equal(expectedModel.GetRuntimeAnnotations(), actualModel.GetRuntimeAnnotations(), AnnotationComparer.Instance)
        End Sub

        Private Sub AssertEqualBase(expected As ITableBase, actual As ITableBase)
            Assert.Equal(expected.Name, actual.Name)
            Assert.Equal(expected.Schema, actual.Schema)
            Assert.Equal(expected.IsShared, actual.IsShared)

            For Each expectedEntityType In expected.EntityTypeMappings.Select(Function(m) m.EntityType)
                Dim actualEntityType = actual.EntityTypeMappings.Single(Function(m) m.EntityType.Name = expectedEntityType.Name).EntityType
                Assert.Equal(expected.GetRowInternalForeignKeys(expectedEntityType).Count(),
                    actual.GetRowInternalForeignKeys(actualEntityType).Count())
                Assert.Equal(expected.GetReferencingRowInternalForeignKeys(expectedEntityType).Count(),
                    actual.GetReferencingRowInternalForeignKeys(actualEntityType).Count())
            Next

            Assert.Equal(expected.GetAnnotations(), actual.GetAnnotations(), AnnotationComparer.Instance)
            Assert.Equal(expected.GetRuntimeAnnotations(), actual.GetRuntimeAnnotations(), AnnotationComparer.Instance)
        End Sub

        Public Sub AssertEqual(expected As ITableBase, actual As ITableBase)
            AssertEqualBase(expected, actual)

            expected.Columns.ZipAssert(actual.Columns, AddressOf AssertEqual)
            expected.EntityTypeMappings.ZipAssert(actual.EntityTypeMappings, AddressOf AssertEqual)

            Assert.Same(actual, DirectCast(actual.Model, RelationalModel).DefaultTables(actual.Name))
        End Sub

        Public Sub AssertEqualBase(expected As ITableMappingBase, actual As ITableMappingBase)
            Assert.Equal(expected.EntityType.Name, actual.EntityType.Name)
            Assert.Equal(expected.Table.SchemaQualifiedName, actual.Table.SchemaQualifiedName)
            Assert.Equal(expected.IncludesDerivedTypes, actual.IncludesDerivedTypes)
            Assert.Equal(expected.IsSharedTablePrincipal, actual.IsSharedTablePrincipal)
            Assert.Equal(expected.IsSplitEntityTypePrincipal, actual.IsSplitEntityTypePrincipal)

            Assert.Equal(expected.GetAnnotations(), actual.GetAnnotations(), AnnotationComparer.Instance)
            Assert.Equal(expected.GetRuntimeAnnotations(), actual.GetRuntimeAnnotations(), AnnotationComparer.Instance)
        End Sub

        Public Sub AssertEqual(expected As ITableMappingBase, actual As ITableMappingBase)
            AssertEqualBase(expected, actual)

            expected.ColumnMappings.ZipAssert(actual.ColumnMappings, AddressOf AssertEqual)
        End Sub

        Public Sub AssertEqualBase(expected As IColumnBase, actual As IColumnBase)
            Assert.Equal(expected.Name, actual.Name)
            Assert.Equal(expected.IsNullable, actual.IsNullable)
            Assert.Equal(expected.ProviderClrType, actual.ProviderClrType)
            Assert.Equal(expected.StoreType, actual.StoreType)
            Assert.Equal(expected.StoreTypeMapping.StoreType, actual.StoreTypeMapping.StoreType)

            Assert.Equal(expected.GetAnnotations(), actual.GetAnnotations(), AnnotationComparer.Instance)
            Assert.Equal(expected.GetRuntimeAnnotations(), actual.GetRuntimeAnnotations(), AnnotationComparer.Instance)
        End Sub

        Public Sub AssertEqual(expected As IColumnBase, actual As IColumnBase)
            AssertEqualBase(expected, actual)

            expected.PropertyMappings.ZipAssert(actual.PropertyMappings, AddressOf AssertEqual)

            Assert.Same(actual, actual.Table.FindColumn(actual.Name))
        End Sub

        Public Sub AssertEqualBase(expected As IColumnMappingBase, actual As IColumnMappingBase)
            Assert.Equal(expected.Column.Name, actual.Column.Name)
            Assert.Equal(expected.Property.Name, actual.Property.Name)
            Assert.Equal(expected.TypeMapping.StoreType, actual.TypeMapping.StoreType)

            Assert.Equal(expected.GetAnnotations(), actual.GetAnnotations(), AnnotationComparer.Instance)
            Assert.Equal(expected.GetRuntimeAnnotations(), actual.GetRuntimeAnnotations(), AnnotationComparer.Instance)
        End Sub

        Public Sub AssertEqual(expected As IColumnMappingBase, actual As IColumnMappingBase)
            AssertEqualBase(expected, actual)

            Assert.Contains(actual, actual.TableMapping.ColumnMappings)
        End Sub

        Public Sub AssertEqual(expected As ITable, actual As ITable)
            AssertEqualBase(expected, actual)

            expected.Columns.ZipAssert(actual.Columns, AddressOf AssertEqual)
            expected.Indexes.ZipAssert(actual.Indexes, AddressOf AssertEqual)
            expected.ForeignKeyConstraints.ZipAssert(actual.ForeignKeyConstraints, AddressOf AssertEqual)
            expected.ReferencingForeignKeyConstraints.ZipAssert(actual.ReferencingForeignKeyConstraints, AddressOf AssertEqual)
            expected.UniqueConstraints.ZipAssert(actual.UniqueConstraints, AddressOf AssertEqual)
            expected.Triggers.ZipAssert(actual.Triggers, AddressOf AssertEqual)

            Assert.Same(actual, actual.Model.FindTable(actual.Name, actual.Schema))
            expected.EntityTypeMappings.ZipAssert(actual.EntityTypeMappings, AddressOf AssertEqual)
        End Sub

        Public Sub AssertEqual(expected As ITableMapping, actual As ITableMapping)
            AssertEqualBase(expected, actual)

            AssertEqual(expected.DeleteStoredProcedureMapping, actual.DeleteStoredProcedureMapping)
            AssertEqual(expected.InsertStoredProcedureMapping, actual.InsertStoredProcedureMapping)
            AssertEqual(expected.UpdateStoredProcedureMapping, actual.UpdateStoredProcedureMapping)

            expected.ColumnMappings.ZipAssert(actual.ColumnMappings, AddressOf AssertEqual)
        End Sub

        Public Sub AssertEqual(expected As IColumn, actual As IColumn)
            AssertEqualBase(expected, actual)

            expected.PropertyMappings.ZipAssert(actual.PropertyMappings, AddressOf AssertEqual)

            Assert.Same(actual, actual.Table.FindColumn(actual.Name))
        End Sub

        Public Sub AssertEqual(expected As IColumnMapping, actual As IColumnMapping)
            AssertEqualBase(expected, actual)

            Assert.Contains(actual, actual.TableMapping.ColumnMappings)
        End Sub

        Public Sub AssertEqual(expected As ITableIndex, actual As ITableIndex)
            Assert.Equal(expected.Columns.Select(Function(c) c.Name), actual.Columns.Select(Function(c) c.Name))
            Assert.Equal(expected.Name, actual.Name)
            Assert.Contains(actual, actual.Table.Indexes)
            Assert.Equal(actual.MappedIndexes.Select(Function(i) i.Properties.Select(Function(p) p.Name)),
                expected.MappedIndexes.Select(Function(i) i.Properties.Select(Function(p) p.Name)))

            Assert.Equal(expected.GetAnnotations(), actual.GetAnnotations(), AnnotationComparer.Instance)
            Assert.Equal(expected.GetRuntimeAnnotations(), actual.GetRuntimeAnnotations(), AnnotationComparer.Instance)
        End Sub

        Public Sub AssertEqual(expected As IForeignKeyConstraint, actual As IForeignKeyConstraint)
            Assert.Equal(expected.Columns.Select(Function(c) c.Name), actual.Columns.Select(Function(c) c.Name))
            Assert.Equal(expected.PrincipalColumns.Select(Function(c) c.Name), actual.PrincipalColumns.Select(Function(c) c.Name))
            Assert.Equal(expected.Name, actual.Name)
            Assert.Equal(expected.OnDeleteAction, actual.OnDeleteAction)
            Assert.Equal(expected.PrincipalUniqueConstraint.Name, actual.PrincipalUniqueConstraint.Name)
            Assert.Equal(expected.PrincipalTable.SchemaQualifiedName, actual.PrincipalTable.SchemaQualifiedName)
            Assert.Contains(actual, actual.Table.ForeignKeyConstraints)
            Assert.Equal(actual.MappedForeignKeys.Select(Function(i) i.Properties.Select(Function(p) p.Name)),
                expected.MappedForeignKeys.Select(Function(i) i.Properties.Select(Function(p) p.Name)))

            Assert.Equal(expected.GetAnnotations(), actual.GetAnnotations(), AnnotationComparer.Instance)
            Assert.Equal(expected.GetRuntimeAnnotations(), actual.GetRuntimeAnnotations(), AnnotationComparer.Instance)
        End Sub

        Public Sub AssertEqual(expected As IUniqueConstraint, actual As IUniqueConstraint)
            Assert.Equal(expected.Columns.Select(Function(c) c.Name), actual.Columns.Select(Function(c) c.Name))
            Assert.Equal(expected.Name, actual.Name)
            Assert.Equal(expected.GetIsPrimaryKey(), actual.GetIsPrimaryKey())
            Assert.Contains(actual, actual.Table.UniqueConstraints)
            Assert.Equal(actual.MappedKeys.Select(Function(i) i.Properties.Select(Function(p) p.Name)),
                expected.MappedKeys.Select(Function(i) i.Properties.Select(Function(p) p.Name)))

            Assert.Equal(expected.GetAnnotations(), actual.GetAnnotations(), AnnotationComparer.Instance)
            Assert.Equal(expected.GetRuntimeAnnotations(), actual.GetRuntimeAnnotations(), AnnotationComparer.Instance)
        End Sub

        Public Sub AssertEqual(expected As ITrigger, actual As ITrigger)
            Assert.Equal(expected.ModelName, actual.ModelName)
            Assert.Equal(expected.GetTableName(), actual.GetTableName())
            Assert.Equal(expected.GetTableSchema(), actual.GetTableSchema())

            Assert.Equal(expected.GetAnnotations(), actual.GetAnnotations(), AnnotationComparer.Instance)
            Assert.Equal(expected.GetRuntimeAnnotations(), actual.GetRuntimeAnnotations(), AnnotationComparer.Instance)
        End Sub

        Public Sub AssertEqual(expected As IView, actual As IView)
            AssertEqualBase(expected, actual)

            expected.Columns.ZipAssert(actual.Columns, AddressOf AssertEqual)

            Assert.Same(actual, actual.Model.FindView(actual.Name, actual.Schema))
            expected.EntityTypeMappings.ZipAssert(actual.EntityTypeMappings, AddressOf AssertEqual)
        End Sub

        Public Sub AssertEqual(expected As IViewMapping, actual As IViewMapping)
            AssertEqualBase(expected, actual)

            expected.ColumnMappings.ZipAssert(actual.ColumnMappings, AddressOf AssertEqual)
        End Sub

        Public Sub AssertEqual(expected As IViewColumn, actual As IViewColumn)
            AssertEqualBase(expected, actual)

            expected.PropertyMappings.ZipAssert(actual.PropertyMappings, AddressOf AssertEqual)

            Assert.Same(actual, actual.View.FindColumn(actual.Name))
        End Sub

        Public Sub AssertEqual(expected As IViewColumnMapping, actual As IViewColumnMapping)
            AssertEqualBase(expected, actual)

            Assert.Contains(actual, actual.ViewMapping.ColumnMappings)
        End Sub

        Public Sub AssertEqual(expected As ISqlQuery, actual As ISqlQuery)
            AssertEqualBase(expected, actual)

            expected.Columns.ZipAssert(actual.Columns, AddressOf AssertEqual)
            Assert.Equal(expected.Sql, actual.Sql)

            Assert.Same(actual, actual.Model.FindView(actual.Name, actual.Schema))
            expected.EntityTypeMappings.ZipAssert(actual.EntityTypeMappings, AddressOf AssertEqual)
        End Sub

        Public Sub AssertEqual(expected As ISqlQueryMapping, actual As ISqlQueryMapping)
            AssertEqualBase(expected, actual)

            Assert.Equal(expected.IsDefaultSqlQueryMapping, actual.IsDefaultSqlQueryMapping)

            expected.ColumnMappings.ZipAssert(actual.ColumnMappings, AddressOf AssertEqual)
        End Sub

        Public Sub AssertEqual(expected As ISqlQueryColumn, actual As ISqlQueryColumn)
            AssertEqualBase(expected, actual)

            expected.PropertyMappings.ZipAssert(actual.PropertyMappings, AddressOf AssertEqual)

            Assert.Same(actual, expected.SqlQuery.FindColumn(actual.Name))
        End Sub

        Public Sub AssertEqual(expected As ISqlQueryColumnMapping, actual As ISqlQueryColumnMapping)
            AssertEqualBase(expected, actual)

            Assert.Contains(actual, actual.SqlQueryMapping.ColumnMappings)
        End Sub

        Public Sub AssertEqual(expected As IStoreFunction, actual As IStoreFunction)

            AssertEqualBase(expected, actual)

            expected.Parameters.ZipAssert(actual.Parameters, AddressOf AssertEqual)
            expected.Columns.ZipAssert(actual.Columns, AddressOf AssertEqual)
            Assert.Equal(expected.ReturnType, actual.ReturnType)
            Assert.Equal(expected.IsBuiltIn, actual.IsBuiltIn)

            Assert.Same(actual, actual.Model.FindFunction(actual.Name, actual.Schema, actual.Parameters.Select(Function(p) p.StoreType).ToArray()))
            Assert.Equal(actual.DbFunctions.Select(Function(p) p.ModelName),
                expected.DbFunctions.Select(Function(p) p.ModelName))
            expected.EntityTypeMappings.ZipAssert(actual.EntityTypeMappings, AddressOf AssertEqual)
        End Sub

        Public Sub AssertEqual(expected As IFunctionMapping, actual As IFunctionMapping)
            AssertEqualBase(expected, actual)

            expected.ColumnMappings.ZipAssert(actual.ColumnMappings, AddressOf AssertEqual)

            Assert.Equal(expected.IsDefaultFunctionMapping, actual.IsDefaultFunctionMapping)
            Assert.Contains(expected.DbFunction.Name, actual.DbFunction.Name)

            Assert.Equal(expected.GetAnnotations(), actual.GetAnnotations(), AnnotationComparer.Instance)
            Assert.Equal(expected.GetRuntimeAnnotations(), actual.GetRuntimeAnnotations(), AnnotationComparer.Instance)
        End Sub

        Public Sub AssertEqual(expected As IFunctionColumn, actual As IFunctionColumn)
            AssertEqualBase(expected, actual)

            expected.PropertyMappings.ZipAssert(actual.PropertyMappings, AddressOf AssertEqual)

            Assert.Same(actual, actual.Function.FindColumn(actual.Name))
        End Sub

        Public Sub AssertEqual(expected As IFunctionColumnMapping, actual As IFunctionColumnMapping)
            AssertEqualBase(expected, actual)

            Assert.Contains(actual, actual.FunctionMapping.ColumnMappings)
        End Sub

        Public Sub AssertEqual(expected As IStoreFunctionParameter, actual As IStoreFunctionParameter)

            Assert.Equal(expected.Name, actual.Name)
            Assert.Equal(expected.StoreType, actual.StoreType)
            Assert.Contains(actual, actual.Function.Parameters)
            Assert.Equal(expected.DbFunctionParameters.Select(Function(p) p.Name), actual.DbFunctionParameters.Select(Function(p) p.Name))

            Assert.Equal(expected.GetAnnotations(), actual.GetAnnotations(), AnnotationComparer.Instance)
            Assert.Equal(expected.GetRuntimeAnnotations(), actual.GetRuntimeAnnotations(), AnnotationComparer.Instance)
        End Sub

        Public Sub AssertEqual(expected As IStoreStoredProcedure, actual As IStoreStoredProcedure)
            AssertEqualBase(expected, actual)

            expected.Parameters.ZipAssert(actual.Parameters, AddressOf AssertEqual)
            expected.ResultColumns.ZipAssert(actual.ResultColumns, AddressOf AssertEqual)

            If expected.ReturnValue Is Nothing Then
                Assert.Null(actual.ReturnValue)
                Exit Sub
            Else
                AssertEqualBase(expected.ReturnValue, actual.ReturnValue)
                Assert.Same(actual, actual.ReturnValue.StoredProcedure)
                expected.ReturnValue.PropertyMappings.ZipAssert(actual.ReturnValue.PropertyMappings, AddressOf AssertEqual)
            End If

            Assert.Same(actual, actual.Model.FindStoredProcedure(actual.Name, actual.Schema))
            Assert.Equal(actual.StoredProcedures.Select(Function(p) p.Name),
                expected.StoredProcedures.Select(Function(p) p.Name))
            expected.EntityTypeMappings.ZipAssert(actual.EntityTypeMappings, AddressOf AssertEqual)
        End Sub

        Public Sub AssertEqual(expected As IStoredProcedureMapping, actual As IStoredProcedureMapping)
            If expected Is Nothing Then
                Assert.Null(actual)
                Exit Sub
            End If

            AssertEqualBase(expected, actual)
            expected.ResultColumnMappings.ZipAssert(actual.ResultColumnMappings, AddressOf AssertEqual)
            expected.ParameterMappings.ZipAssert(actual.ParameterMappings, AddressOf AssertEqual)
            Assert.Equal(expected.StoredProcedure.GetSchemaQualifiedName(), actual.StoredProcedure.GetSchemaQualifiedName())
            Assert.Equal(expected.StoreStoredProcedure.SchemaQualifiedName, actual.StoreStoredProcedure.SchemaQualifiedName)

            Assert.Contains(expected.TableMapping?.Table.SchemaQualifiedName, actual.TableMapping?.Table.SchemaQualifiedName)

            Assert.Equal(expected.GetAnnotations(), actual.GetAnnotations(), AnnotationComparer.Instance)
            Assert.Equal(expected.GetRuntimeAnnotations(), actual.GetRuntimeAnnotations(), AnnotationComparer.Instance)
        End Sub

        Public Sub AssertEqual(expected As IStoreStoredProcedureResultColumn, actual As IStoreStoredProcedureResultColumn)
            AssertEqualBase(expected, actual)

            expected.PropertyMappings.ZipAssert(actual.PropertyMappings, AddressOf AssertEqual)
            Assert.Equal(expected.Position, actual.Position)

            Assert.Same(actual, actual.StoredProcedure.FindResultColumn(actual.Name))
        End Sub

        Public Sub AssertEqual(expected As IStoredProcedureResultColumnMapping, actual As IStoredProcedureResultColumnMapping)
            AssertEqualBase(expected, actual)

            Assert.Contains(actual, actual.StoredProcedureMapping.ResultColumnMappings)
        End Sub

        Public Sub AssertEqual(expected As IStoreStoredProcedureParameter, actual As IStoreStoredProcedureParameter)
            AssertEqualBase(expected, actual)

            expected.PropertyMappings.ZipAssert(actual.PropertyMappings, AddressOf AssertEqual)
            Assert.Equal(expected.Direction, actual.Direction)
            Assert.Equal(expected.Position, actual.Position)

            Assert.Same(actual, actual.StoredProcedure.FindParameter(actual.Name))
        End Sub

        Public Sub AssertEqual(expected As IStoredProcedureParameterMapping, actual As IStoredProcedureParameterMapping)
            AssertEqualBase(expected, actual)

            Assert.Contains(actual, actual.StoredProcedureMapping.ParameterMappings)
        End Sub
    End Module
End Namespace
