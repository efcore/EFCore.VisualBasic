Imports System.IO
Imports EntityFrameworkCore.VisualBasic.TestUtilities

Namespace Scaffolding.Internal

    Public Class ModelCodeGeneratorTestFixture
        Implements IDisposable

        Public ReadOnly Property ProjectDir As TempDirectory = New TempDirectory()

        Public Sub New()
            Dim templatesDir = Path.Combine(ProjectDir, "CodeTemplates", "EFCore")
            Directory.CreateDirectory(templatesDir)

            Using input = GetType(VisualBasicModelCodeGeneratorTestBase).Assembly.GetManifestResourceStream("Microsoft.EntityFrameworkCore.Resources.CSharpDbContextGenerator.tt")
                'Using output = File.OpenWrite(Path.Combine(templatesDir, "DbContext.t4"))
                '    input.CopyTo(output)
                'End Using
            End Using

            Using input = GetType(VisualBasicModelCodeGeneratorTestBase).Assembly.GetManifestResourceStream("Microsoft.EntityFrameworkCore.Resources.CSharpEntityTypeGenerator.tt")
                'Using output = File.OpenWrite(Path.Combine(templatesDir, "EntityType.t4"))
                '    input.CopyTo(output)
                'End Using
            End Using
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            ProjectDir.Dispose()
        End Sub
    End Class
End Namespace
