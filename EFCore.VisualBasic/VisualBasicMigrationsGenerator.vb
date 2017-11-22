Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Migrations.Design
Imports Microsoft.EntityFrameworkCore.Migrations.Operations

Public Class VisualBasicMigrationsGenerator
    Inherits MigrationsCodeGenerator

    Public Sub New(dependencies As MigrationsCodeGeneratorDependencies)
        MyBase.New(dependencies)
    End Sub

    Public Overrides ReadOnly Property FileExtension As String
        Get
            Return ".vb"
        End Get
    End Property

    Public Overrides ReadOnly Property Language As String
        Get
            Return "VB"
        End Get
    End Property

    Public Overrides Function GenerateMigration(
        migrationNamespace As String,
        migrationName As String,
        upOperations As IReadOnlyList(Of MigrationOperation),
        downOperations As IReadOnlyList(Of MigrationOperation)) As String

        Return "' TODO: Generate migration"
    End Function

    Public Overrides Function GenerateMetadata(
        migrationNamespace As String,
        contextType As Type,
        migrationName As String,
        migrationId As String,
        targetModel As IModel) As String

        Return "' TODO: Generate metadata"
    End Function

    Public Overrides Function GenerateSnapshot(
        modelSnapshotNamespace As String,
        contextType As Type,
        modelSnapshotName As String,
        model As IModel) As String

        Return "' TODO: Generate snapshot"
    End Function
End Class
