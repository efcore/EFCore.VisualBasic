
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Migrations
Imports Microsoft.EntityFrameworkCore.Migrations.Operations

Namespace TestUtilities
    Public Class TestRelationalMigrationSqlGenerator
        Inherits MigrationsSqlGenerator
        Public Sub New(dependencies As MigrationsSqlGeneratorDependencies)
            MyBase.New(dependencies)
        End Sub
        Protected Overrides Sub Generate(operation As RenameTableOperation, model As IModel, builder As MigrationCommandListBuilder)
        End Sub
        Protected Overrides Sub Generate(
            operation As DropIndexOperation,
            model As IModel,
            builder As MigrationCommandListBuilder,
            Optional terminate As Boolean = True)
        End Sub
        Protected Overrides Sub Generate(operation As RenameSequenceOperation, model As IModel, builder As MigrationCommandListBuilder)
        End Sub
        Protected Overrides Sub Generate(operation As RenameColumnOperation, model As IModel, builder As MigrationCommandListBuilder)
        End Sub
        Protected Overrides Sub Generate(operation As EnsureSchemaOperation, model As IModel, builder As MigrationCommandListBuilder)
        End Sub
        Protected Overrides Sub Generate(operation As RenameIndexOperation, model As IModel, builder As MigrationCommandListBuilder)
        End Sub
        Protected Overrides Sub Generate(operation As AlterColumnOperation, model As IModel, builder As MigrationCommandListBuilder)
        End Sub
    End Class
End Namespace
