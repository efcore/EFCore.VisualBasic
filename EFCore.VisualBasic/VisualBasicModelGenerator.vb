Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Scaffolding

Public Class VisualBasicModelGenerator
    Inherits ModelCodeGenerator

    Public Sub New(dependencies As ModelCodeGeneratorDependencies)
        MyBase.New(dependencies)
    End Sub

    Public Overrides ReadOnly Property Language As String
        Get
            Return "VB"
        End Get
    End Property

    Public Overrides Function GenerateModel(
        model As IModel,
        [namespace] As String,
        contextName As String,
        connectionString As String,
        dataAnnotations As Boolean) As ScaffoldedModel

        Dim files As New ScaffoldedModel
        files.ContextFile = New ScaffoldedFile
        files.ContextFile.Path = contextName + ".vb"
        files.ContextFile.Code = "' TODO: Generate DbContext"

        For Each entityType In model.GetEntityTypes()
            Dim file As New ScaffoldedFile
            file.Path = entityType.DisplayName() + ".vb"
            file.Code = "' TODO: Generate " + entityType.DisplayName()
            files.AdditionalFiles.Add(file)
        Next

        Return files
    End Function
End Class
