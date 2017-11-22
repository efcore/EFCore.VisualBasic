Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Scaffolding.Internal

Public Class VisualBasicScaffoldingGenerator
    Inherits ScaffoldingCodeGenerator

    Public Sub New(fileService As IFileService)
        MyBase.New(fileService)
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

    Public Overrides Function WriteCode(
        model As IModel,
        outputPath As String,
        [namespace] As String,
        contextName As String,
        connectionString As String,
        dataAnnotations As Boolean) As ReverseEngineerFiles

        Dim files As New ReverseEngineerFiles
        files.ContextFile = FileService.OutputFile(
            outputPath,
            contextName + FileExtension,
            "' TODO: Generate DbContext")

        For Each entityType In model.GetEntityTypes()
            files.EntityTypeFiles.Add(
                FileService.OutputFile(
                    outputPath,
                    entityType.DisplayName() + FileExtension,
                    "' TODO: Generate " + entityType.DisplayName()))
        Next

        Return files
    End Function
End Class
