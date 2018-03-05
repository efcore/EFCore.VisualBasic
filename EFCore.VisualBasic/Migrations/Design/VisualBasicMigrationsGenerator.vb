Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design.Internal
Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Migrations.Design
Imports Microsoft.EntityFrameworkCore.Migrations.Operations

Public Class VisualBasicMigrationsGenerator
    Inherits MigrationsCodeGenerator

    ''' <summary>
    '''     Initializes a New instance of the <see cref="VisualBasicMigrationsGenerator" /> class.
    ''' </summary>
    ''' <param name="dependencies"> The base dependencies. </param>
    ''' <param name="vbDependencies"> The dependencies. </param>
    Public Sub New(dependencies As MigrationsCodeGeneratorDependencies,
                   vbDependencies As VisualBasicMigrationsGeneratorDependencies)

        MyBase.New(dependencies)
        VisualBasicDependencies = vbDependencies

    End Sub

    ''' <summary>
    '''     Gets the file extension code files should use.
    ''' </summary>
    ''' <value> The file extension. </value>
    Public Overrides ReadOnly Property FileExtension As String
        Get
            Return ".vb"
        End Get
    End Property

    ''' <summary>
    '''     Gets the programming language supported by this service.
    ''' </summary>
    ''' <value> The language. </value>
    Public Overrides ReadOnly Property Language As String
        Get
            Return "VB"
        End Get
    End Property

    Protected Overridable ReadOnly Property VisualBasicDependencies As VisualBasicMigrationsGeneratorDependencies

    Private ReadOnly Property VBCode As IVisualBasicHelper
        Get
            Return VisualBasicDependencies.VisualBasicHelper
        End Get
    End Property

    ''' <summary>
    '''     Generates the migration code.
    ''' </summary>
    ''' <param name="migrationNamespace"> The migration's namespace. </param>
    ''' <param name="migrationName"> The migration's name. </param>
    ''' <param name="upOperations"> The migration's up operations. </param>
    ''' <param name="downOperations"> The migration's down operations. </param>
    ''' <returns> The migration code. </returns>
    Public Overrides Function GenerateMigration(
    migrationNamespace As String,
    migrationName As String,
    upOperations As IReadOnlyList(Of MigrationOperation),
    downOperations As IReadOnlyList(Of MigrationOperation)) As String

        Dim builder = New IndentedStringBuilder()
        Dim namespaces = New List(Of String) From {"System", "System.Collections.Generic", "Microsoft.EntityFrameworkCore.Migrations"}
        namespaces.AddRange(GetNamespaces(upOperations.Concat(downOperations)))
        For Each n In namespaces.OrderBy(Function(x) x, New NamespaceComparer()).Distinct()
            builder.Append("Imports ").AppendLine(n)
        Next

        builder.AppendLine().Append("Namespace ").AppendLine(VBCode.[Namespace](migrationNamespace))
        Using builder.Indent()
            builder.Append("Public Partial Class ").Append(VBCode.Identifier(migrationName)).AppendLine()
            Using builder.Indent()
                builder.AppendLine("Inherits Migration") _
                       .AppendLine()
            End Using
            Using builder.Indent()
                builder.AppendLine("Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)")
                Using builder.Indent()
                    VisualBasicDependencies.VisualBasicMigrationOperationGenerator.Generate("migrationBuilder", upOperations, builder)
                End Using

                builder.AppendLine().AppendLine("End Sub").AppendLine().AppendLine("Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)")
                Using builder.Indent()
                    VisualBasicDependencies.VisualBasicMigrationOperationGenerator.Generate("migrationBuilder", downOperations, builder)
                End Using

                builder.AppendLine().AppendLine("End Sub")
            End Using

            builder.AppendLine("End Class")
        End Using

        builder.AppendLine("End Namespace")
        Return builder.ToString()

    End Function

    ''' <summary>
    '''     Generates the migration metadata code.
    ''' </summary>
    ''' <param name="migrationNamespace"> The migration's namespace. </param>
    ''' <param name="contextType"> The migration's <see cref="DbContext" /> type. </param>
    ''' <param name="migrationName"> The migration's name. </param>
    ''' <param name="migrationId"> The migration's ID. </param>
    ''' <param name="targetModel"> The migration's target model. </param>
    ''' <returns> The migration metadata code. </returns>
    Public Overrides Function GenerateMetadata(migrationNamespace As String,
                                               contextType As Type,
                                               migrationName As String,
                                               migrationId As String,
                                               targetModel As IModel) As String

        Dim builder As New IndentedStringBuilder()

        AppendAutoGeneratedTag(builder)
        Dim namespaces = New List(Of String) From
            {
                "System",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.EntityFrameworkCore.Infrastructure",
                "Microsoft.EntityFrameworkCore.Metadata",
                "Microsoft.EntityFrameworkCore.Migrations"
            }

        If (Not String.IsNullOrEmpty(contextType.Namespace)) Then
            namespaces.Add(contextType.Namespace)
        End If

        namespaces.AddRange(GetNamespaces(targetModel))
        For Each n In namespaces.OrderBy(Function(x) x, New NamespaceComparer()).Distinct()
            builder.Append("Imports ").AppendLine(n)
        Next

        builder.AppendLine().Append("Namespace ").AppendLine(VBCode.Namespace(migrationNamespace))
        Using builder.Indent()
            builder.Append("<DbContext(GetType(").Append(VBCode.Reference(contextType)).AppendLine("))>") _
                   .Append("<Migration(").Append(VBCode.Literal(migrationId)).AppendLine(")>") _
                   .Append("Partial Class ").AppendLine(VBCode.Identifier(migrationName))

            Using builder.Indent()
                builder.AppendLine("Protected Overrides Sub BuildTargetModel(modelBuilder As ModelBuilder)")

                Using builder.Indent()
                    ' TODO: Optimize.This Is repeated below
                    VisualBasicDependencies.VisualBasicSnapshotGenerator.Generate("modelBuilder", targetModel, builder)
                End Using
                builder.AppendLine().AppendLine("End Sub")
            End Using
            builder.AppendLine("End Class")
        End Using
        builder.AppendLine("End Namespace")

        Return builder.ToString()
    End Function

    Private Sub AppendAutoGeneratedTag(builder As IndentedStringBuilder)
        builder.AppendLine("' <auto-generated />")
    End Sub

    ''' <summary>
    '''     Generates the model snapshot code.
    ''' </summary>
    ''' <param name="modelSnapshotNamespace"> The model snapshot's namespace. </param>
    ''' <param name="contextType"> The model snapshot's <see cref="DbContext" /> type. </param>
    ''' <param name="modelSnapshotName"> The model snapshot's name. </param>
    ''' <param name="model"> The model. </param>
    ''' <returns> The model snapshot code. </returns>
    Public Overrides Function GenerateSnapshot(
    modelSnapshotNamespace As String,
    contextType As Type,
    modelSnapshotName As String,
    model As IModel) As String

        Dim builder = New IndentedStringBuilder()
        AppendAutoGeneratedTag(builder)
        Dim namespaces = New List(Of String) From
            {
                "System",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.EntityFrameworkCore.Infrastructure",
                "Microsoft.EntityFrameworkCore.Metadata",
                "Microsoft.EntityFrameworkCore.Migrations"
            }
        If (Not String.IsNullOrEmpty(contextType.Namespace)) Then
            namespaces.Add(contextType.Namespace)
        End If
        namespaces.AddRange(GetNamespaces(model))
        For Each n In namespaces.OrderBy(Function(x) x, New NamespaceComparer()).Distinct()
            builder.Append("Imports ").AppendLine(n)
        Next

        builder.AppendLine() _
               .Append("Namespace ").AppendLine(VBCode.Namespace(modelSnapshotNamespace))

        Using builder.Indent()
            builder.Append("<DbContext(GetType(").Append(VBCode.Reference(contextType)).AppendLine("))>") _
            .Append("Partial Class ").AppendLine(VBCode.Identifier(modelSnapshotName))

            Using builder.Indent()

                builder.AppendLine("Inherits ModelSnapshot").AppendLine()

                builder.AppendLine("Protected Overrides Sub BuildModel(modelBuilder As ModelBuilder)")
                Using builder.Indent()
                    VisualBasicDependencies.VisualBasicSnapshotGenerator.Generate("modelBuilder", model, builder)
                End Using
                builder.AppendLine("End Sub")
            End Using

            builder.AppendLine("End Class")
        End Using
        builder.AppendLine("End Namespace")

        Return builder.ToString()
    End Function
End Class
