Imports System.IO
Imports EntityFrameworkCore.VisualBasic.TestUtilities

Namespace Scaffolding.Internal

    Public Class ModelCodeGeneratorTestFixture
        Implements IDisposable

        Public ReadOnly Property ProjectDir As New TempDirectory()

        Public Sub New()
            Dim templatesDir = Path.Combine(ProjectDir, "CodeTemplates", "EFCore")
            Directory.CreateDirectory(templatesDir)

            Dim solutionDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.Parent.FullName
            Dim VBTemplatesDir = Path.Combine(solutionDir,
                                              "EFCore.VisualBasic.Templates",
                                              "templates",
                                              "ef-templates",
                                              "CodeTemplates",
                                              "EFCore")

            Dim destDbContext = Path.Combine(templatesDir, "DbContext.t4")
            File.Copy(Path.Combine(VBTemplatesDir, "DbContext.t4"), destDbContext, True)

            Dim destEntityType = Path.Combine(templatesDir, "EntityType.t4")
            File.Copy(Path.Combine(VBTemplatesDir, "EntityType.t4"), destEntityType, True)
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            ProjectDir.Dispose()
        End Sub
    End Class
End Namespace
