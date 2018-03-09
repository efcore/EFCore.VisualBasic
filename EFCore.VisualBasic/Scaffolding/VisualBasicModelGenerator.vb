Imports System.IO
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Scaffolding

Public Class VisualBasicModelGenerator
    Inherits ModelCodeGenerator

    Public Overridable Property VisualBasicDbContextGenerator As IVisualBasicDbContextGenerator

    Public Overridable Property VisualBasicEntityTypeGenerator As IVisualBasicEntityTypeGenerator

    Public Sub New(dependencies As ModelCodeGeneratorDependencies, vbDbContextGenerator As IVisualBasicDbContextGenerator, vbEntityTypeGenerator As IVisualBasicEntityTypeGenerator)
        MyBase.New(dependencies)
        VisualBasicDbContextGenerator = vbDbContextGenerator
        VisualBasicEntityTypeGenerator = vbEntityTypeGenerator
    End Sub

    Private Const FileExtension As String = ".vb"

    Public Overrides ReadOnly Property Language As String
        Get
            Return "VB"
        End Get
    End Property

    Public Overrides Function GenerateModel(ByVal model As IModel, ByVal [namespace] As String, ByVal contextDir As String, ByVal contextName As String, ByVal connectionString As String, ByVal options As ModelCodeGenerationOptions) As ScaffoldedModel
        Dim resultingFiles = New ScaffoldedModel()
        Dim generatedCode = VisualBasicDbContextGenerator.WriteCode(model, [namespace], contextName, connectionString, options.UseDataAnnotations, options.SuppressConnectionStringWarning)
        Dim dbContextFileName = contextName & FileExtension
        resultingFiles.ContextFile = New ScaffoldedFile With {.Path = Path.Combine(contextDir, dbContextFileName), .Code = generatedCode}

        For Each entityType In model.GetEntityTypes()
            generatedCode = VisualBasicEntityTypeGenerator.WriteCode(entityType, [namespace], options.UseDataAnnotations)
            Dim entityTypeFileName = entityType.DisplayName() & FileExtension
            resultingFiles.AdditionalFiles.Add(New ScaffoldedFile With {.Path = entityTypeFileName, .Code = generatedCode})
        Next

        Return resultingFiles
    End Function
End Class
