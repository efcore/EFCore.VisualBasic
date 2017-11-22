Imports System.IO
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Scaffolding.Internal
Imports Xunit

Public Class VisualBasicScaffoldingGeneratorTests
    <Fact>
    Public Sub FileExtension_works()
        Dim generator = CreateGenerator()

        Assert.Equal(".vb", generator.FileExtension)
    End Sub

    <Fact>
    Public Sub Language_works()
        Dim generator = CreateGenerator()

        Assert.Equal("VB", generator.Language)
    End Sub

    <Fact>
    Public Sub WriteCode_works()
        Dim generator = CreateGenerator()
        Dim model = (New TestDbContext).Model

        Dim result = generator.WriteCode(
            model,
            ".",
            "MyNamespace",
            "MyDbContext",
            "",
            True)

        Assert.True(File.Exists(result.ContextFile))
        Assert.Equal(0, result.EntityTypeFiles.Count)
    End Sub

    Private Function CreateGenerator() As VisualBasicScaffoldingGenerator
        Return New VisualBasicScaffoldingGenerator(New FileSystemFileService())
    End Function

    Private Class TestDbContext
        Inherits DbContext

        Protected Overrides Sub OnConfiguring(options As DbContextOptionsBuilder)
            options.UseSqlite("Data Source=:memory:")
        End Sub
    End Class
End Class
