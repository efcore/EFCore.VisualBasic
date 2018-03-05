Imports System.IO
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Scaffolding
Imports Xunit

Public Class VisualBasicModelGeneratorTests
    <Fact>
    Public Sub Language_works()
        Dim generator = CreateGenerator()

        Assert.Equal("VB", generator.Language)
    End Sub

    '<Fact>
    'Public Sub GenerateModel_works()
    '    Dim generator = CreateGenerator()
    '    Dim model = (New TestDbContext).Model

    '    Dim result = generator.GenerateModel(
    '        model,
    '        "MyNamespace",
    '        "ContextDir",
    '        "MyDbContext",
    '        "",
    '        True)

    '    Assert.Equal(Path.Combine("ContextDir", "MyDbContext.vb"), result.ContextFile.Path)
    '    Assert.NotEmpty(result.ContextFile.Code)
    '    Assert.Equal(0, result.AdditionalFiles.Count)
    'End Sub

    Private Function CreateGenerator() As VisualBasicModelGenerator
        Return New VisualBasicModelGenerator(New ModelCodeGeneratorDependencies())
    End Function

    'Private Class TestDbContext
    '    Inherits DbContext

    '    Protected Overrides Sub OnConfiguring(options As DbContextOptionsBuilder)
    '        options.UseSqlite("Data Source=:memory:")
    '    End Sub
    'End Class
End Class
