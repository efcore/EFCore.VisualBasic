Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Migrations.Design
Imports Microsoft.EntityFrameworkCore.Migrations.Operations
Imports Xunit

Public Class VisualBasicMigrationsGeneratorTests
    <Fact>
    Sub FileExtension_works()
        Dim generator = CreateGenerator()

        Assert.Equal(".vb", generator.FileExtension)
    End Sub

    <Fact>
    Sub Language_works()
        Dim generator = CreateGenerator()

        Assert.Equal("VB", generator.Language)
    End Sub

    <Fact>
    Sub GenerateMigration_works()
        Dim generator = CreateGenerator()

        Dim result = generator.GenerateMigration(
            "MyNamespace",
            "MyMigration",
            New MigrationOperation() {},
            New MigrationOperation() {})

        Assert.NotNull(result)
    End Sub

    <Fact>
    Sub GenerateMetadata_works()
        Dim generator = CreateGenerator()
        Dim model = (New TestDbContext).Model

        Dim result = generator.GenerateMetadata(
            "MyNamespace",
            GetType(TestDbContext),
            "MyMigration",
            "00000000000000_MyMigration",
            model)

        Assert.NotNull(result)
    End Sub

    <Fact>
    Sub GenerateSnapshot_works()
        Dim generator = CreateGenerator()
        Dim model = (New TestDbContext).Model

        Dim result = generator.GenerateSnapshot(
            "MyNamespace",
            GetType(TestDbContext),
            "MyModelSnapshot",
            model)

        Assert.NotNull(result)
    End Sub

    Private Function CreateGenerator() As VisualBasicMigrationsGenerator
        Return New VisualBasicMigrationsGenerator(New MigrationsCodeGeneratorDependencies())
    End Function

    Private Class TestDbContext
        Inherits DbContext

        Protected Overrides Sub OnConfiguring(options As DbContextOptionsBuilder)
            options.UseSqlite("Data Source=:memory:")
        End Sub
    End Class
End Class

