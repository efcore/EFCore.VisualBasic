Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Scaffolding
Imports Microsoft.EntityFrameworkCore.SqlServer.Design.Internal
Imports Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.DependencyInjection.Extensions
Imports Xunit
Imports Xunit.Abstractions

Namespace Scaffolding.Internal
    Public Class VisualBasicEntityTypeGeneratorTest
        Inherits VisualBasicModelCodeGeneratorTestBase

        Public Sub New(fixture As ModelCodeGeneratorTestFixture, output As ITestOutputHelper)
            MyBase.New(fixture, output)
        End Sub

        <ConditionalFact>
        Public Sub KeylessAttribute_is_generated_for_key_less_entity()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    <Keyless>
    Partial Public Class Vista
    End Class
End Namespace
"
            Dim expectedDbContextCode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Vista As DbSet(Of Vista)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Test(
                Function(modelBuilder) modelBuilder.Entity("Vista").HasNoKey(),
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Vista.vb"))

                    AssertFileContents(
                        expectedDbContextCode,
                        code.ContextFile)
                End Sub,
                Sub(model)
                    Dim entityType = model.FindEntityType("TestNamespace.Vista")
                    Assert.Null(entityType.FindPrimaryKey())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub TableAttribute_is_generated_for_custom_name()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    <Table(""Vistas"")>
    Partial Public Class Vista
        <Key>
        Public Property Id As Integer
    End Class
End Namespace
"

            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity(
                        "Vista",
                        Sub(b)
                            b.ToTable("Vistas") ' Default name is "Vista" in the absence of pluralizer
                            b.HasAnnotation(ScaffoldingAnnotationNames.DbSetName, "Vista")
                            b.Property(Of Integer)("Id")
                            b.HasKey("Id")
                        End Sub)
                End Sub,
                New ModelCodeGenerationOptions With {
                .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Vista.vb"))
                End Sub,
                Sub(model)
                    Dim entityType = model.FindEntityType("TestNamespace.Vista")
                    Assert.Equal("Vistas", entityType.GetTableName())
                    Assert.Null(entityType.GetSchema())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub TableAttribute_is_not_generated_for_default_schema()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Vista
        <Key>
        Public Property Id As Integer
    End Class
End Namespace
"

            Test(
                Sub(modelBuilder)
                    modelBuilder.HasDefaultSchema("dbo")
                    modelBuilder.Entity(
                        "Vista",
                        Sub(b)
                            b.ToTable("Vista", "dbo") ' Default name is "Vista" in the absence of pluralizer
                            b.Property(Of Integer)("Id")
                            b.HasKey("Id")
                        End Sub)
                End Sub,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Vista.vb"))
                End Sub,
                Sub(model)
                    Dim entityType = model.FindEntityType("TestNamespace.Vista")
                    Assert.Equal("Vista", entityType.GetTableName())
                    Assert.Null(entityType.GetSchema()) ' Takes through model default schema
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub TableAttribute_is_generated_for_non_default_schema()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    <Table(""Vista"", Schema:=""custom"")>
    Partial Public Class Vista
        <Key>
        Public Property Id As Integer
    End Class
End Namespace
"

            Test(
                Sub(modelBuilder)
                    modelBuilder.HasDefaultSchema("dbo")
                    modelBuilder.Entity(
                        "Vista",
                        Sub(b)
                            b.ToTable("Vista", "custom")
                            b.Property(Of Integer)("Id")
                            b.HasKey("Id")
                        End Sub)
                End Sub,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Vista.vb"))
                End Sub,
                Sub(model)
                    Dim entityType = model.FindEntityType("TestNamespace.Vista")
                    Assert.Equal("Vista", entityType.GetTableName())
                    Assert.Equal("custom", entityType.GetSchema())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub TableAttribute_is_not_generated_for_views()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    <Keyless>
    Partial Public Class Vista
    End Class
End Namespace
"

            Test(
                Function(modelBuilder) modelBuilder.Entity("Vista").ToView("Vistas", "dbo"),
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Vista.vb"))
                End Sub,
                Sub(model)
                    Dim entityType = model.FindEntityType("TestNamespace.Vista")
                    Assert.Equal("Vistas", entityType.GetViewName())
                    Assert.Null(entityType.GetTableName())
                    Assert.Equal("dbo", entityType.GetViewSchema())
                    Assert.Null(entityType.GetSchema())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub IndexAttribute_is_generated_for_multiple_indexes_with_name_unique_descending()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    <Index(""C"")>
    <Index(""A"", ""B"", Name:=""IndexOnAAndB"", IsUnique:=True, IsDescending:={True, False})>
    <Index(""B"", ""C"", Name:=""IndexOnBAndC"")>
    Partial Public Class EntityWithIndexes
        <Key>
        Public Property Id As Integer

        Public Property A As Integer

        Public Property B As Integer

        Public Property C As Integer
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                      "EntityWithIndexes",
                      Sub(x)
                          x.Property(Of Integer)("Id")
                          x.Property(Of Integer)("A")
                          x.Property(Of Integer)("B")
                          x.Property(Of Integer)("C")
                          x.HasKey("Id")
                          x.HasIndex({"A", "B"}, "IndexOnAAndB").
                            IsUnique().
                            IsDescending(True, False)
                          x.HasIndex({"B", "C"}, "IndexOnBAndC")
                          x.HasIndex("C")
                      End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "EntityWithIndexes.vb"))
                End Sub,
                Sub(model)
                    Dim entityType = model.FindEntityType("TestNamespace.EntityWithIndexes")
                    Dim indexes = entityType.GetIndexes()
                    Assert.Collection(
                        indexes,
                        Sub(t) Assert.Null(t.Name),
                        Sub(t) Assert.Equal("IndexOnAAndB", t.Name),
                        Sub(t) Assert.Equal("IndexOnBAndC", t.Name))
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub IndexAttribute_is_generated_with_ascending_descending()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    <Index(""A"", ""B"", Name:=""AllAscending"")>
    <Index(""A"", ""B"", Name:=""AllDescending"", AllDescending:=True)>
    <Index(""A"", ""B"", Name:=""PartiallyDescending"", IsDescending:={True, False})>
    Partial Public Class EntityWithAscendingDescendingIndexes
        <Key>
        Public Property Id As Integer

        Public Property A As Integer

        Public Property B As Integer
    End Class
End Namespace
"

            Test(
                Sub(ModelBuilder)
                    ModelBuilder.
                        Entity(
                            "EntityWithAscendingDescendingIndexes",
                            Sub(x)
                                x.Property(Of Integer)("Id")
                                x.Property(Of Integer)("A")
                                x.Property(Of Integer)("B")
                                x.HasKey("Id")
                                x.HasIndex({"A", "B"}, "AllAscending")
                                x.HasIndex({"A", "B"}, "PartiallyDescending").IsDescending(True, False)
                                x.HasIndex({"A", "B"}, "AllDescending").IsDescending()
                            End Sub)
                End Sub,
            New ModelCodeGenerationOptions With {.UseDataAnnotations = True},
            Sub(code)
                AssertFileContents(
                    expectedCode,
                    code.AdditionalFiles.Single(Function(f) f.Path = "EntityWithAscendingDescendingIndexes.vb"))
            End Sub,
            Sub(model)
                Dim entityType = model.FindEntityType("TestNamespace.EntityWithAscendingDescendingIndexes")
                Dim indexes = entityType.GetIndexes()
                Assert.Collection(
                    indexes,
                    Sub(i)
                        Assert.Equal("AllAscending", i.Name)
                        Assert.Null(i.IsDescending)
                    End Sub,
                    Sub(i)
                        Assert.Equal("AllDescending", i.Name)
                        Assert.Equal(Array.Empty(Of Boolean), i.IsDescending)
                    End Sub,
                    Sub(i)
                        Assert.Equal("PartiallyDescending", i.Name)
                        Assert.Equal({True, False}, i.IsDescending)
                    End Sub)
            End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Entity_with_indexes_generates_IndexAttribute_only_for_indexes_without_annotations()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    <Index(""A"", ""B"", Name:=""IndexOnAAndB"", IsUnique:=True)>
    Partial Public Class EntityWithIndexes
        <Key>
        Public Property Id As Integer

        Public Property A As Integer

        Public Property B As Integer

        Public Property C As Integer
    End Class
End Namespace
"

            Dim expectedDbContextCode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property EntityWithIndexes As DbSet(Of EntityWithIndexes)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of EntityWithIndexes)(
                Sub(entity)
                    entity.HasIndex(Function(e) New With {{e.B, e.C}}, ""IndexOnBAndC"").HasFilter(""Filter SQL"")
                End Sub)

            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"
            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                        "EntityWithIndexes",
                        Sub(x)
                            x.Property(Of Integer)("Id")
                            x.Property(Of Integer)("A")
                            x.Property(Of Integer)("B")
                            x.Property(Of Integer)("C")
                            x.HasKey("Id")
                            x.HasIndex({"A", "B"}, "IndexOnAAndB").IsUnique()
                            x.HasIndex({"B", "C"}, "IndexOnBAndC").HasFilter("Filter SQL")
                        End Sub
                    )
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "EntityWithIndexes.vb"))

                    AssertFileContents(
                        expectedDbContextCode,
                        code.ContextFile)
                End Sub,
                Sub(model) Assert.Equal(2, model.FindEntityType("TestNamespace.EntityWithIndexes").GetIndexes().Count()))
        End Sub

        <ConditionalFact>
        Public Sub KeyAttribute_is_generated_for_single_property_and_no_fluent_api()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Entity
        <Key>
        Public Property PrimaryKey As Integer
    End Class
End Namespace
"

            Dim expectedDbContextCode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Entity As DbSet(Of Entity)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                        "Entity",
                        Sub(x)
                            x.Property(Of Integer)("PrimaryKey")
                            x.HasKey("PrimaryKey")
                        End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Entity.vb"))

                    AssertFileContents(
                        expectedDbContextCode,
                        code.ContextFile)
                End Sub,
                Sub(model) Assert.Equal("PrimaryKey", model.FindEntityType("TestNamespace.Entity").FindPrimaryKey().Properties(0).Name))
        End Sub

        <ConditionalFact>
        Public Sub KeyAttribute_is_generated_on_multiple_properties_but_and_uses_PrimaryKeyAttribute_for_composite_key()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    <PrimaryKey(""Key"", ""Serial"")>
    Partial Public Class Post
        <Key>
        Public Property Key As Integer

        <Key>
        Public Property Serial As Integer
    End Class
End Namespace
"

            Dim expectedDbContextCode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Post As DbSet(Of Post)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                      "Post",
                      Sub(x)
                          x.Property(Of Integer)("Key")
                          x.Property(Of Integer)("Serial")
                          x.HasKey("Key", "Serial")
                      End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Post.vb"))

                    AssertFileContents(
                        expectedDbContextCode,
                        code.ContextFile)
                End Sub,
                Sub(model)
                    Dim postType = model.FindEntityType("TestNamespace.Post")
                    Assert.Equal({"Key", "Serial"}, postType.FindPrimaryKey().Properties.[Select](Function(p) p.Name))
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub RequiredAttribute_is_generated_for_property()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Entity
        <Key>
        Public Property Id As Integer

        <Required>
        Public Property RequiredString As String
    End Class
End Namespace
"
            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                        "Entity",
                          Sub(x)
                              x.Property(Of Integer)("Id")
                              x.Property(Of String)("RequiredString").IsRequired()
                          End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Entity.vb"))
                End Sub,
                Sub(model) Assert.[False](model.FindEntityType("TestNamespace.Entity").GetProperty("RequiredString").IsNullable))
        End Sub

        <ConditionalFact>
        Public Sub RequiredAttribute_is_not_generated_for_key_property()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Entity
        <Key>
        Public Property RequiredString As String
    End Class
End Namespace
"
            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                        "Entity",
                        Sub(x)
                            x.Property(Of String)("RequiredString")
                            x.HasKey("RequiredString")
                        End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Entity.vb"))
                End Sub,
                Sub(model) Assert.False(model.FindEntityType("TestNamespace.Entity").GetProperty("RequiredString").IsNullable))
        End Sub

        <ConditionalFact>
        Public Sub ColumnAttribute_is_generated_for_property()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Entity
        <Key>
        Public Property Id As Integer

        <Column(""propertyA"")>
        Public Property A As String

        <Column(TypeName:=""nchar(10)"")>
        Public Property B As String

        <Column(""random"", TypeName:=""varchar(200)"")>
        Public Property C As String

        <Column(TypeName:=""numeric(18, 2)"")>
        Public Property D As Decimal

        <StringLength(100)>
        Public Property E As String
    End Class
End Namespace
"
            Dim expectedDbContextCode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Entity As DbSet(Of Entity)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                        "Entity",
                        Sub(x)
                            x.Property(Of Integer)("Id")
                            x.Property(Of String)("A").HasColumnName("propertyA")
                            x.Property(Of String)("B").HasColumnType("nchar(10)")
                            x.Property(Of String)("C").HasColumnName("random").HasColumnType("varchar(200)")
                            x.Property(Of Decimal)("D").HasColumnType("numeric(18, 2)")
                            x.Property(Of String)("E").HasMaxLength(100)
                        End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Entity.vb"))

                    AssertFileContents(
                        expectedDbContextCode,
                        code.ContextFile)
                End Sub,
                Sub(model)
                    Dim entitType = model.FindEntityType("TestNamespace.Entity")
                    Assert.Equal("propertyA", entitType.GetProperty("A").GetColumnName())
                    Assert.Equal("nchar(10)", entitType.GetProperty("B").GetColumnType())
                    Assert.Equal("random", entitType.GetProperty("C").GetColumnName())
                    Assert.Equal("varchar(200)", entitType.GetProperty("C").GetColumnType())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub MaxLengthAttribute_is_generated_for_property()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Entity
        <Key>
        Public Property Id As Integer

        <StringLength(34)>
        Public Property A As String

        <MaxLength(10)>
        Public Property B As Byte()
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                        "Entity",
                        Sub(x)
                            x.Property(Of Integer)("Id")
                            x.Property(Of String)("A").HasMaxLength(34)
                            x.Property(Of Byte())("B").HasMaxLength(10)
                        End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Entity.vb"))
                End Sub,
                Sub(model)
                    Dim entitType = model.FindEntityType("TestNamespace.Entity")
                    Assert.Equal(34, entitType.GetProperty("A").GetMaxLength())
                    Assert.Equal(10, entitType.GetProperty("B").GetMaxLength())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub UnicodeAttribute_is_generated_for_property()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Entity
        <Key>
        Public Property Id As Integer

        <StringLength(34)>
        <Unicode>
        Public Property A As String

        <StringLength(34)>
        <Unicode(False)>
        Public Property B As String

        <StringLength(34)>
        Public Property C As String
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                        "Entity",
                        Sub(x)
                            x.Property(Of Integer)("Id")
                            x.Property(Of String)("A").HasMaxLength(34).IsUnicode()
                            x.Property(Of String)("B").HasMaxLength(34).IsUnicode(False)
                            x.Property(Of String)("C").HasMaxLength(34)
                        End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Entity.vb"))
                End Sub,
                Sub(model)
                    Dim entitType = model.FindEntityType("TestNamespace.Entity")
                    Assert.True(entitType.GetProperty("A").IsUnicode())
                    Assert.False(entitType.GetProperty("B").IsUnicode())
                    Assert.Null(entitType.GetProperty("C").IsUnicode())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub PrecisionAttribute_is_generated_for_property()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Entity
        <Key>
        Public Property Id As Integer

        <Precision(10)>
        Public Property A As Decimal

        <Precision(14, 3)>
        Public Property B As Decimal

        <Precision(5)>
        Public Property C As Date

        <Precision(3)>
        Public Property D As DateTimeOffset
    End Class
End Namespace
"

            Test(
                   Function(modelBuilder)
                       Return modelBuilder _
     .Entity(
         "Entity",
         Sub(x)
             x.Property(Of Integer)("Id")
             x.Property(Of Decimal)("A").HasPrecision(10)
             x.Property(Of Decimal)("B").HasPrecision(14, 3)
             x.Property(Of Date)("C").HasPrecision(5)
             x.Property(Of DateTimeOffset)("D").HasPrecision(3)
         End Sub)
                   End Function,
New ModelCodeGenerationOptions With
{
                .UseDataAnnotations = True},
                   Sub(code)
                       AssertFileContents(
                           expectedCode,
                           code.AdditionalFiles.Single(Function(f) f.Path = "Entity.vb"))
                   End Sub,
                   Sub(model)
                       Dim entitType = model.FindEntityType("TestNamespace.Entity")
                       Assert.Equal(10, entitType.GetProperty("A").GetPrecision())
                       Assert.Equal(14, entitType.GetProperty("B").GetPrecision())
                       Assert.Equal(3, entitType.GetProperty("B").GetScale())
                       Assert.Equal(5, entitType.GetProperty("C").GetPrecision())
                       Assert.Equal(3, entitType.GetProperty("D").GetPrecision())
                   End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Comments_are_generated()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    ''' <summary>
    ''' Entity Comment
    ''' </summary>
    Partial Public Class Entity
        ''' <summary>
        ''' Property Comment
        ''' </summary>
        <Key>
        Public Property Id As Integer
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.
                      Entity(
                        "Entity",
                        Sub(x)
                            x.ToTable(Function(tb) tb.HasComment("Entity Comment"))
                            x.Property(Of Integer)("Id").HasComment("Property Comment")
                        End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(expectedCode,
                                        code.AdditionalFiles.Single(Function(f) f.Path = "Entity.vb"))
                End Sub,
                Sub(model)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Comments_complex_are_generated()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    ''' <summary>
    ''' Entity Comment
    ''' On multiple lines
    ''' With XML content &lt;br/&gt;
    ''' </summary>
    Partial Public Class Entity
        ''' <summary>
        ''' Property Comment
        ''' On multiple lines
        ''' With XML content &lt;br/&gt;
        ''' </summary>
        <Key>
        Public Property Id As Integer
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                      "Entity",
                      Sub(x)
                          x.ToTable(Function(tb) tb.HasComment(
"Entity Comment
On multiple lines
With XML content <br/>"))
                          x.Property(Of Integer)("Id").HasComment(
"Property Comment
On multiple lines
With XML content <br/>")
                      End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Entity.vb"))
                End Sub,
                Sub(model)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Properties_names_are_escaped_for_reserved_keywords()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Entity
        <Key>
        Public Property [Property] As Integer

        Public Property [Function] As String

        Public Property [Integer] As Integer
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                    "Entity",
                    Sub(x)
                        x.Property(Of Integer)("Property")
                        x.Property(Of String)("Function")
                        x.Property(Of Integer)("Integer")
                        x.HasKey("Property")
                    End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Entity.vb"))
                End Sub,
                Sub(model)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Properties_are_sorted_in_order_of_definition_in_table()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Entity
        <Key>
        Public Property Id As Integer

        Public Property FirstProperty As String

        Public Property LastProperty As String
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                    "Entity",
                    Sub(x)
                        ' Order would be PK first and then rest alphabetically since they are all shadow
                        x.Property(Of Integer)("Id")
                        x.Property(Of String)("LastProperty")
                        x.Property(Of String)("FirstProperty")
                    End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Entity.vb"))
                End Sub,
                Sub(model)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Navigation_properties_are_sorted_after_properties_and_collection_are_initialized_in_ctor()

            Dim expectedCodePost =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Post
        <Key>
        Public Property Id As Integer

        Public Property AuthorId As Integer?

        <ForeignKey(""AuthorId"")>
        <InverseProperty(""Posts"")>
        Public Overridable Property Author As Person

        <InverseProperty(""Post"")>
        Public Overridable Property Contributions As ICollection(Of Contribution) = New List(Of Contribution)()
    End Class
End Namespace
"

            Dim expectedCodePerson =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Person
        <Key>
        Public Property Id As Integer

        <InverseProperty(""Author"")>
        Public Overridable Property Posts As ICollection(Of Post) = New List(Of Post)()
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                        "Person",
                        Function(x) x.Property(Of Integer)("Id")) _
                    .Entity(
                        "Contribution",
                        Function(x) x.Property(Of Integer)("Id")) _
                    .Entity(
                        "Post",
                        Sub(x)
                            x.Property(Of Integer)("Id")

                            x.HasOne("Person", "Author").WithMany("Posts")
                            x.HasMany("Contribution", "Contributions").WithOne("Post")
                        End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCodePost,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Post.vb"))

                    AssertFileContents(
                        expectedCodePerson,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Person.vb"))
                End Sub,
                Sub(model)
                    Dim postType = model.FindEntityType("TestNamespace.Post")
                    Dim authorNavigation = postType.FindNavigation("Author")
                    Assert.[True](authorNavigation.IsOnDependent)
                    Assert.Equal("TestNamespace.Person", authorNavigation.ForeignKey.PrincipalEntityType.Name)

                    Dim contributionsNav = postType.FindNavigation("Contributions")
                    Assert.[False](contributionsNav.IsOnDependent)
                    Assert.Equal("TestNamespace.Contribution", contributionsNav.ForeignKey.DeclaringEntityType.Name)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub ForeignKeyAttribute_is_generated_for_composite_fk()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Post
        <Key>
        Public Property Id As Integer

        Public Property BlogId1 As Integer?

        Public Property BlogId2 As Integer?

        <ForeignKey(""BlogId1, BlogId2"")>
        <InverseProperty(""Posts"")>
        Public Overridable Property BlogNavigation As Blog
    End Class
End Namespace
"

            Dim expectedDbContextCode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Blog As DbSet(Of Blog)

        Public Overridable Property Post As DbSet(Of Post)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder _
                    .Entity(
                        "Blog",
                        Sub(x)
                            x.Property(Of Integer)("Id1")
                            x.Property(Of Integer)("Id2")
                            x.HasKey("Id1", "Id2")
                        End Sub) _
                    .Entity(
                        "Post",
                        Sub(x)
                            x.Property(Of Integer)("Id")

                            x.HasOne("Blog", "BlogNavigation").WithMany("Posts").HasForeignKey("BlogId1", "BlogId2")
                        End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Post.vb"))

                    AssertFileContents(
                        expectedDbContextCode,
                        code.ContextFile)
                End Sub,
                Sub(model)
                    Dim postType = model.FindEntityType("TestNamespace.Post")
                    Dim blogNavigation = postType.FindNavigation("BlogNavigation")
                    Assert.Equal("TestNamespace.Blog", blogNavigation.ForeignKey.PrincipalEntityType.Name)
                    Assert.Equal({"BlogId1", "BlogId2"}, blogNavigation.ForeignKey.Properties.[Select](Function(p) p.Name))
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub ForeignKeyAttribute_InversePropertyAttribute_when_composite_alternate_key()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Post
        <Key>
        Public Property Id As Integer

        Public Property BlogId1 As Integer?

        Public Property BlogId2 As Integer?

        <ForeignKey(""BlogId1, BlogId2"")>
        <InverseProperty(""Posts"")>
        Public Overridable Property BlogNavigation As Blog
    End Class
End Namespace
"

            Dim expectedDbContextCode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Blog As DbSet(Of Blog)

        Public Overridable Property Post As DbSet(Of Post)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Post)(
                Sub(entity)
                    entity.HasOne(Function(d) d.BlogNavigation).WithMany(Function(p) p.Posts).
                        HasPrincipalKey(Function(p) New With {{p.Id1, p.Id2}}).
                        HasForeignKey(Function(d) New With {{d.BlogId1, d.BlogId2}})
                End Sub)

            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                    "Blog",
                    Sub(x)
                        x.Property(Of Integer)("Id")
                        x.Property(Of Integer)("Id1")
                        x.Property(Of Integer)("Id2")
                    End Sub) _
                .Entity(
                    "Post",
                    Sub(x)
                        x.Property(Of Integer)("Id")

                        x.HasOne("Blog", "BlogNavigation").WithMany("Posts").
                            HasPrincipalKey("Id1", "Id2").
                            HasForeignKey("BlogId1", "BlogId2")
                    End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Post.vb"))

                    AssertFileContents(
                        expectedDbContextCode,
                        code.ContextFile)
                End Sub,
                Sub(model)
                    Dim postType = model.FindEntityType("TestNamespace.Post")
                    Dim blogNavigation = postType.FindNavigation("BlogNavigation")
                    Assert.Equal("TestNamespace.Blog", blogNavigation.ForeignKey.PrincipalEntityType.Name)
                    Assert.Equal({"BlogId1", "BlogId2"}, blogNavigation.ForeignKey.Properties.[Select](Function(p) p.Name))
                    Assert.Equal({"Id1", "Id2"}, blogNavigation.ForeignKey.PrincipalKey.Properties.[Select](Function(p) p.Name))
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub ForeignKeyAttribute_is_generated_for_fk_referencing_ak()

            Dim expectedColorCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Color
        <Key>
        Public Property Id As Integer

        <Required>
        Public Property ColorCode As String

        <InverseProperty(""Color"")>
        Public Overridable Property Cars As ICollection(Of Car) = New List(Of Car)()
    End Class
End Namespace
"

            Dim expectedCarCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Car
        <Key>
        Public Property Id As Integer

        Public Property ColorCode As String

        <ForeignKey(""ColorCode"")>
        <InverseProperty(""Cars"")>
        Public Overridable Property Color As Color
    End Class
End Namespace
"

            Dim expectedDbContextCode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Car As DbSet(Of Car)

        Public Overridable Property Color As DbSet(Of Color)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Car)(
                Sub(entity)
                    entity.HasOne(Function(d) d.Color).WithMany(Function(p) p.Cars).
                        HasPrincipalKey(Function(p) p.ColorCode).
                        HasForeignKey(Function(d) d.ColorCode)
                End Sub)

            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                        "Color",
                        Sub(x)
                            x.Property(Of Integer)("Id")
                            x.Property(Of String)("ColorCode")
                        End Sub) _
                    .Entity(
                        "Car",
                        Sub(x)
                            x.Property(Of Integer)("Id")

                            x.HasOne("Color", "Color").WithMany("Cars").
                              HasPrincipalKey("ColorCode").
                              HasForeignKey("ColorCode")
                        End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedColorCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Color.vb"))

                    AssertFileContents(
                        expectedCarCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Car.vb"))

                    AssertFileContents(
                        expectedDbContextCode,
                        code.ContextFile)
                End Sub,
                Sub(model)
                    Dim carType = model.FindEntityType("TestNamespace.Car")
                    Dim colorNavigation = carType.FindNavigation("Color")
                    Assert.Equal("TestNamespace.Color", colorNavigation.ForeignKey.PrincipalEntityType.Name)
                    Assert.Equal({"ColorCode"}, colorNavigation.ForeignKey.Properties.Select(Function(p) p.Name))
                    Assert.Equal({"ColorCode"}, colorNavigation.ForeignKey.PrincipalKey.Properties.Select(Function(p) p.Name))
                End Sub)
        End Sub
        <ConditionalFact>
        Public Sub Foreign_key_from_keyless_table()

            Dim expectedDbContextCode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Blog As DbSet(Of Blog)

        Public Overridable Property Post As DbSet(Of Post)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Post)(
                Sub(entity)
                    entity.HasNoKey()

                    entity.HasIndex(Function(e) e.BlogId, ""IX_Post_BlogId"")

                    entity.HasOne(Function(d) d.Blog).WithMany().HasForeignKey(Function(d) d.BlogId)
                End Sub)

            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Dim expectedBlogCode =
"Imports System
Imports System.Collections.Generic
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Blog
        Public Property Id As Integer
    End Class
End Namespace
"

            Dim expectedPostCode =
"Imports System
Imports System.Collections.Generic
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Post
        Public Property BlogId As Integer?

        Public Overridable Property Blog As Blog
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                        "Blog",
                        Sub(x)
                            x.Property(Of Integer)("Id")
                        End Sub) _
                    .Entity(
                        "Post",
                        Sub(x)
                            x.HasOne("Blog", "Blog").WithMany()
                        End Sub)
                End Function,
                New ModelCodeGenerationOptions,
                Sub(code)
                    AssertFileContents(
                        expectedDbContextCode,
                        code.ContextFile)

                    AssertFileContents(
                        expectedBlogCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Blog.vb"))

                    AssertFileContents(
                        expectedPostCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Post.vb"))
                End Sub,
                Sub(model)
                    Dim post = model.FindEntityType("TestNamespace.Post")
                    Dim foreignKey = Assert.Single(post.GetForeignKeys())
                    Assert.Equal("Blog", foreignKey.DependentToPrincipal.Name)
                    Assert.Null(foreignKey.PrincipalToDependent)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub InverseProperty_when_navigation_property_with_same_type_and_navigation_name()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Post
        <Key>
        Public Property Id As Integer

        Public Property BlogId As Integer?

        <ForeignKey(""BlogId"")>
        <InverseProperty(""Posts"")>
        Public Overridable Property Blog As Blog
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder _
                    .Entity(
                        "Blog",
                        Function(x) x.Property(Of Integer)("Id")) _
                    .Entity(
                        "Post",
                        Sub(x)
                            x.Property(Of Integer)("Id")
                            x.HasOne("Blog", "Blog").WithMany("Posts")
                        End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Post.vb"))
                End Sub,
                Sub(model)
                    Dim postType = model.FindEntityType("TestNamespace.Post")
                    Dim blogNavigation = postType.FindNavigation("Blog")

                    Dim foreignKeyProperty = Assert.Single(blogNavigation.ForeignKey.Properties)
                    Assert.Equal("BlogId", foreignKeyProperty.Name)

                    Dim inverseNavigation = blogNavigation.Inverse
                    Assert.Equal("TestNamespace.Blog", inverseNavigation.DeclaringEntityType.Name)
                    Assert.Equal("Posts", inverseNavigation.Name)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub InverseProperty_when_navigation_property_with_same_type_and_property_name()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Post
        <Key>
        Public Property Id As Integer

        Public Property Blog As Integer?

        <ForeignKey(""Blog"")>
        <InverseProperty(""Posts"")>
        Public Overridable Property BlogNavigation As Blog
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                        "Blog",
                        Function(x) x.Property(Of Integer)("Id")) _
                    .Entity(
                        "Post",
                        Sub(x)
                            x.Property(Of Integer)("Id")
                            x.HasOne("Blog", "BlogNavigation").WithMany("Posts").HasForeignKey("Blog")
                        End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Post.vb"))
                End Sub,
                Sub(model)
                    Dim postType = model.FindEntityType("TestNamespace.Post")
                    Dim blogNavigation = postType.FindNavigation("BlogNavigation")

                    Dim foreignKeyProperty = Assert.Single(blogNavigation.ForeignKey.Properties)
                    Assert.Equal("Blog", foreignKeyProperty.Name)

                    Dim inverseNavigation = blogNavigation.Inverse
                    Assert.Equal("TestNamespace.Blog", inverseNavigation.DeclaringEntityType.Name)
                    Assert.Equal("Posts", inverseNavigation.Name)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub InverseProperty_when_navigation_property_with_same_type_and_other_navigation_name()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Post
        <Key>
        Public Property Id As Integer

        Public Property BlogId As Integer?

        Public Property OriginalBlogId As Integer?

        <ForeignKey(""BlogId"")>
        <InverseProperty(""Posts"")>
        Public Overridable Property Blog As Blog

        <ForeignKey(""OriginalBlogId"")>
        <InverseProperty(""OriginalPosts"")>
        Public Overridable Property OriginalBlog As Blog
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                        "Blog",
                        Function(x) x.Property(Of Integer)("Id")) _
                    .Entity(
                        "Post",
                        Sub(x)
                            x.Property(Of Integer)("Id")
                            x.HasOne("Blog", "Blog").WithMany("Posts")
                            x.HasOne("Blog", "OriginalBlog").WithMany("OriginalPosts").HasForeignKey("OriginalBlogId")
                        End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Post.vb"))
                End Sub,
                Sub(model)
                    Dim postType = model.FindEntityType("TestNamespace.Post")

                    Dim blogNavigation = postType.FindNavigation("Blog")

                    Dim foreignKeyProperty = Assert.Single(blogNavigation.ForeignKey.Properties)
                    Assert.Equal("BlogId", foreignKeyProperty.Name)

                    Dim inverseNavigation = blogNavigation.Inverse
                    Assert.Equal("TestNamespace.Blog", inverseNavigation.DeclaringEntityType.Name)
                    Assert.Equal("Posts", inverseNavigation.Name)

                    Dim originalBlogNavigation = postType.FindNavigation("OriginalBlog")

                    Dim originalForeignKeyProperty = Assert.Single(originalBlogNavigation.ForeignKey.Properties)
                    Assert.Equal("OriginalBlogId", originalForeignKeyProperty.Name)

                    Dim originalInverseNavigation = originalBlogNavigation.Inverse
                    Assert.Equal("TestNamespace.Blog", originalInverseNavigation.DeclaringEntityType.Name)
                    Assert.Equal("OriginalPosts", originalInverseNavigation.Name)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub InverseProperty_when_navigation_property_and_keyless()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    <Keyless>
    Partial Public Class Post
        Public Property BlogId As Integer?

        <ForeignKey(""BlogId"")>
        Public Overridable Property Blog As Blog
    End Class
End Namespace
"
            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                        "Blog",
                        Function(x) x.Property(Of Integer)("Id")) _
                    .Entity(
                        "Post",
                        Sub(x)
                            x.HasNoKey()
                            x.HasOne("Blog", "Blog").WithMany()
                        End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "Post.vb"))
                End Sub,
                Sub(model)
                    Dim postType = model.FindEntityType("TestNamespace.Post")
                    Dim blogNavigation = postType.FindNavigation("Blog")

                    Dim foreignKeyProperty = Assert.Single(blogNavigation.ForeignKey.Properties)
                    Assert.Equal("BlogId", foreignKeyProperty.Name)

                    Assert.Null(blogNavigation.Inverse)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Entity_with_custom_annotation()

            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    <CustomEntityDataAnnotation(""first argument"")>
    Partial Public Class EntityWithAnnotation
        <Key>
        Public Property Id As Integer
    End Class
End Namespace
"

            Dim expectedDbContextCode =
 $"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property EntityWithAnnotation As DbSet(Of EntityWithAnnotation)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                        "EntityWithAnnotation",
                        Sub(x)
                            x.HasAnnotation("Custom:EntityAnnotation", "first argument")
                            x.Property(Of Integer)("Id")
                            x.HasKey("Id")
                        End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(expectedCode,
                                       code.AdditionalFiles.Single(Function(f) f.Path = "EntityWithAnnotation.vb"))

                    AssertFileContents(expectedDbContextCode,
                                       code.ContextFile)
                End Sub,
                assertModel:=Nothing,
                skipBuild:=True)
        End Sub

        <ConditionalFact>
        Public Sub Entity_property_with_custom_annotation()


            Dim expectedCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class EntityWithPropertyAnnotation
        <Key>
        <CustomPropertyDataAnnotation(""first argument"")>
        Public Property Id As Integer
    End Class
End Namespace
"

            Dim expectedDbContextCode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property EntityWithPropertyAnnotation As DbSet(Of EntityWithPropertyAnnotation)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                        "EntityWithPropertyAnnotation",
                        Sub(x)
                            x.Property(Of Integer)("Id").
                                HasAnnotation("Custom:PropertyAnnotation", "first argument")
                            x.HasKey("Id")
                        End Sub)
                End Function,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.AdditionalFiles.Single(Function(f) f.Path = "EntityWithPropertyAnnotation.vb"))

                    AssertFileContents(expectedDbContextCode,
                                       code.ContextFile)
                End Sub,
                assertModel:=Nothing,
                skipBuild:=True)
        End Sub

        <ConditionalFact>
        Public Sub Scaffold_skip_navigations_default()

            Dim expectedDbContextCode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Blog As DbSet(Of Blog)

        Public Overridable Property Post As DbSet(Of Post)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Blog)(
                Sub(entity)
                    entity.HasMany(Function(d) d.Posts).WithMany(Function(p) p.Blogs).
                        UsingEntity(Of Dictionary(Of String, Object))(
                            ""BlogPost"",
                            Function(r) r.HasOne(Of Post)().WithMany().HasForeignKey(""PostsId""),
                            Function(l) l.HasOne(Of Blog)().WithMany().HasForeignKey(""BlogsId""),
                            Sub(j)
                                j.HasKey(""BlogsId"", ""PostsId"")
                                j.HasIndex({{""PostsId""}}, ""IX_BlogPost_PostsId"")
                            End Sub)
                End Sub)

            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Dim expectedEntityBlog =
"Imports System
Imports System.Collections.Generic
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Blog
        Public Property Id As Integer

        Public Overridable Property Posts As ICollection(Of Post) = New List(Of Post)()
    End Class
End Namespace
"

            Dim expectedEntityPost =
"Imports System
Imports System.Collections.Generic
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Post
        Public Property Id As Integer

        Public Overridable Property Blogs As ICollection(Of Blog) = New List(Of Blog)()
    End Class
End Namespace
"

            Test(
                Sub(ModelBuilder)
                    ModelBuilder.
                        Entity("Blog",
                            Sub(x) x.Property(Of Integer)("Id")).
                        Entity("Post",
                            Sub(x) x.Property(Of Integer)("Id")).
                        Entity("BlogPost",
                               Sub(x)
                               End Sub).
                        Entity("Blog").
                        HasMany("Post", "Posts").
                        WithMany("Blogs").
                        UsingEntity("BlogPost")
                End Sub,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = False
                },
                Sub(code)
                    AssertFileContents(
                        expectedDbContextCode,
                        code.ContextFile)

                    AssertFileContents(
                        expectedEntityBlog,
                        code.AdditionalFiles.Single(Function(e) e.Path = "Blog.vb"))

                    AssertFileContents(
                        expectedEntityPost,
                        code.AdditionalFiles.Single(Function(e) e.Path = "Post.vb"))

                    Assert.Equal(2, code.AdditionalFiles.Count)
                End Sub,
                Sub(model)
                    Dim blogType = model.FindEntityType("TestNamespace.Blog")
                    Assert.Empty(blogType.GetNavigations())
                    Dim postsNavigation = Assert.Single(blogType.GetSkipNavigations())
                    Assert.Equal("Posts", postsNavigation.Name)

                    Dim postType = model.FindEntityType("TestNamespace.Post")
                    Assert.Empty(postType.GetNavigations())
                    Dim blogsNavigation = Assert.Single(postType.GetSkipNavigations())
                    Assert.Equal("Blogs", blogsNavigation.Name)

                    Assert.Equal(postsNavigation, blogsNavigation.Inverse)
                    Assert.Equal(blogsNavigation, postsNavigation.Inverse)

                    Dim joinEntityType = blogsNavigation.ForeignKey.DeclaringEntityType
                    Assert.Equal("BlogPost", joinEntityType.Name)
                    Assert.Equal(GetType(Dictionary(Of String, Object)), joinEntityType.ClrType)
                    Assert.Single(joinEntityType.GetIndexes())
                    Assert.Equal(2, joinEntityType.GetForeignKeys().Count())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Scaffold_skip_navigations_different_key_type()

            Dim expectedDbContextCode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Blog As DbSet(Of Blog)

        Public Overridable Property Post As DbSet(Of Post)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Blog)(
                Sub(entity)
                    entity.HasMany(Function(d) d.Posts).WithMany(Function(p) p.Blogs).
                        UsingEntity(Of Dictionary(Of String, Object))(
                            ""BlogPost"",
                            Function(r) r.HasOne(Of Post)().WithMany().HasForeignKey(""PostsId""),
                            Function(l) l.HasOne(Of Blog)().WithMany().HasForeignKey(""BlogsId""),
                            Sub(j)
                                j.HasKey(""BlogsId"", ""PostsId"")
                                j.HasIndex({{""PostsId""}}, ""IX_BlogPost_PostsId"")
                            End Sub)
                End Sub)

            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Dim expectedEntityBlog =
"Imports System
Imports System.Collections.Generic
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Blog
        Public Property Id As Integer

        Public Overridable Property Posts As ICollection(Of Post) = New List(Of Post)()
    End Class
End Namespace
"
            Dim expectedEntityPost =
"Imports System
Imports System.Collections.Generic
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Post
        Public Property Id As String

        Public Overridable Property Blogs As ICollection(Of Blog) = New List(Of Blog)()
    End Class
End Namespace
"

            Test(
                Sub(ModelBuilder)
                    ModelBuilder.
                        Entity("Blog", Sub(x) x.Property(Of Integer)("Id")).
                        Entity("Post", Sub(x) x.Property(Of String)("Id")).
                        Entity("BlogPost", Sub(x)
                                           End Sub).
                        Entity("Blog").
                        HasMany("Post", "Posts").
                        WithMany("Blogs").
                        UsingEntity("BlogPost")
                End Sub,
                New ModelCodeGenerationOptions With {.UseDataAnnotations = False},
                Sub(code)
                    AssertFileContents(expectedDbContextCode, code.ContextFile)
                    AssertFileContents(expectedEntityBlog, code.AdditionalFiles.Single(Function(e) e.Path = "Blog.vb"))
                    AssertFileContents(expectedEntityPost, code.AdditionalFiles.Single(Function(e) e.Path = "Post.vb"))

                    Assert.Equal(2, code.AdditionalFiles.Count)
                End Sub,
                Sub(model)
                    Dim blogType = model.FindEntityType("TestNamespace.Blog")
                    Assert.Empty(blogType.GetNavigations())
                    Dim postsNavigation = Assert.Single(blogType.GetSkipNavigations())
                    Assert.Equal("Posts", postsNavigation.Name)

                    Dim postType = model.FindEntityType("TestNamespace.Post")
                    Assert.Empty(postType.GetNavigations())
                    Dim blogsNavigation = Assert.Single(postType.GetSkipNavigations())
                    Assert.Equal("Blogs", blogsNavigation.Name)

                    Assert.Equal(postsNavigation, blogsNavigation.Inverse)
                    Assert.Equal(blogsNavigation, postsNavigation.Inverse)

                    Dim joinEntityType = blogsNavigation.ForeignKey.DeclaringEntityType
                    Assert.Equal("BlogPost", joinEntityType.Name)
                    Assert.Equal(GetType(Dictionary(Of String, Object)), joinEntityType.ClrType)
                    Assert.Single(joinEntityType.GetIndexes())
                    Assert.Equal(2, joinEntityType.GetForeignKeys().Count())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Scaffold_skip_navigations_default_data_annotations()

            Dim expectedDbContextCode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Blog As DbSet(Of Blog)

        Public Overridable Property Post As DbSet(Of Post)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Blog)(
                Sub(entity)
                    entity.HasMany(Function(d) d.Posts).WithMany(Function(p) p.Blogs).
                        UsingEntity(Of Dictionary(Of String, Object))(
                            ""BlogPost"",
                            Function(r) r.HasOne(Of Post)().WithMany().HasForeignKey(""PostsId""),
                            Function(l) l.HasOne(Of Blog)().WithMany().HasForeignKey(""BlogsId""),
                            Sub(j)
                                j.HasKey(""BlogsId"", ""PostsId"")
                                j.HasIndex({{""PostsId""}}, ""IX_BlogPost_PostsId"")
                            End Sub)
                End Sub)

            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Dim expectedEntityBlog =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Blog
        <Key>
        Public Property Id As Integer

        <ForeignKey(""BlogsId"")>
        <InverseProperty(""Blogs"")>
        Public Overridable Property Posts As ICollection(Of Post) = New List(Of Post)()
    End Class
End Namespace
"

            Dim expectedEntityPost =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Post
        <Key>
        Public Property Id As Integer

        <ForeignKey(""PostsId"")>
        <InverseProperty(""Posts"")>
        Public Overridable Property Blogs As ICollection(Of Blog) = New List(Of Blog)()
    End Class
End Namespace
"

            Test(
                Sub(ModelBuilder)
                    ModelBuilder.
                        Entity("Blog",
                            Function(x) x.Property(Of Integer)("Id")).
                        Entity("Post",
                            Function(x) x.Property(Of Integer)("Id")).
                        Entity("BlogPost", Sub(x)
                                           End Sub).
                        Entity("Blog").
                            HasMany("Post", "Posts").
                            WithMany("Blogs").
                            UsingEntity("BlogPost")
                End Sub,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedDbContextCode,
                        code.ContextFile)

                    AssertFileContents(
                        expectedEntityBlog,
                        code.AdditionalFiles.Single(Function(e) e.Path = "Blog.vb"))

                    AssertFileContents(
                        expectedEntityPost,
                        code.AdditionalFiles.Single(Function(e) e.Path = "Post.vb"))

                    Assert.Equal(2, code.AdditionalFiles.Count)
                End Sub,
                Sub(model)
                    Dim blogType = model.FindEntityType("TestNamespace.Blog")
                    Assert.Empty(blogType.GetNavigations())
                    Dim postsNavigation = Assert.Single(blogType.GetSkipNavigations())
                    Assert.Equal("Posts", postsNavigation.Name)

                    Dim postType = model.FindEntityType("TestNamespace.Post")
                    Assert.Empty(postType.GetNavigations())
                    Dim blogsNavigation = Assert.Single(postType.GetSkipNavigations())
                    Assert.Equal("Blogs", blogsNavigation.Name)

                    Assert.Equal(postsNavigation, blogsNavigation.Inverse)
                    Assert.Equal(blogsNavigation, postsNavigation.Inverse)

                    Dim joinEntityType = blogsNavigation.ForeignKey.DeclaringEntityType
                    Assert.Equal("BlogPost", joinEntityType.Name)
                    Assert.Equal(GetType(Dictionary(Of String, Object)), joinEntityType.ClrType)
                    Assert.Single(joinEntityType.GetIndexes())
                    Assert.Equal(2, joinEntityType.GetForeignKeys().Count())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Scaffold_skip_navigations_alternate_key_data_annotations()

            Dim expectedDbContextCode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Blog As DbSet(Of Blog)

        Public Overridable Property Post As DbSet(Of Post)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Blog)(
                Sub(entity)
                    entity.HasMany(Function(d) d.Posts).WithMany(Function(p) p.Blogs).
                        UsingEntity(Of Dictionary(Of String, Object))(
                            ""BlogPost"",
                            Function(r) r.HasOne(Of Post)().WithMany().HasForeignKey(""PostsId""),
                            Function(l) l.HasOne(Of Blog)().WithMany().
                                HasPrincipalKey(""Key"").
                                HasForeignKey(""BlogsKey""),
                            Sub(j)
                                j.HasKey(""BlogsKey"", ""PostsId"")
                                j.HasIndex({{""PostsId""}}, ""IX_BlogPost_PostsId"")
                            End Sub)
                End Sub)

            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Dim expectedBlogCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Blog
        <Key>
        Public Property Id As Integer

        Public Property Key As Integer

        <ForeignKey(""BlogsKey"")>
        <InverseProperty(""Blogs"")>
        Public Overridable Property Posts As ICollection(Of Post) = New List(Of Post)()
    End Class
End Namespace
"

            Dim expectedPostCode =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Post
        <Key>
        Public Property Id As Integer

        <ForeignKey(""PostsId"")>
        <InverseProperty(""Posts"")>
        Public Overridable Property Blogs As ICollection(Of Blog) = New List(Of Blog)()
    End Class
End Namespace
"

            Test(
                Sub(ModelBuilder)
                    ModelBuilder.
                        Entity("Blog",
                            Sub(x)
                                x.Property(Of Integer)("Id")
                                x.Property(Of Integer)("Key")
                            End Sub).
                        Entity("Post",
                            Sub(x)
                                x.Property(Of Integer)("Id")
                            End Sub).
                        Entity("Blog").HasMany("Post", "Posts").WithMany("Blogs").
                        UsingEntity("BlogPost",
                                    Function(r) r.HasOne("Post").WithMany(),
                                    Function(l) l.HasOne("Blog").WithMany().HasPrincipalKey("Key"))
                End Sub,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedDbContextCode,
                        code.ContextFile)

                    AssertFileContents(
                        expectedBlogCode,
                        code.AdditionalFiles.Single(Function(e) e.Path = "Blog.vb"))

                    AssertFileContents(
                        expectedPostCode,
                        code.AdditionalFiles.Single(Function(e) e.Path = "Post.vb"))

                    Assert.Equal(2, code.AdditionalFiles.Count)
                End Sub,
                Sub(model)
                    Dim blogType = model.FindEntityType("TestNamespace.Blog")
                    Assert.Empty(blogType.GetNavigations())
                    Dim postsNavigation = Assert.Single(blogType.GetSkipNavigations())
                    Assert.Equal("Posts", postsNavigation.Name)

                    Dim postType = model.FindEntityType("TestNamespace.Post")
                    Assert.Empty(postType.GetNavigations())
                    Dim blogsNavigation = Assert.Single(postType.GetSkipNavigations())
                    Assert.Equal("Blogs", blogsNavigation.Name)

                    Assert.Equal(postsNavigation, blogsNavigation.Inverse)
                    Assert.Equal(blogsNavigation, postsNavigation.Inverse)

                    Dim joinEntityType = blogsNavigation.ForeignKey.DeclaringEntityType
                    Assert.Equal("BlogPost", joinEntityType.Name)
                    Assert.Equal(GetType(Dictionary(Of String, Object)), joinEntityType.ClrType)
                    Assert.Single(joinEntityType.GetIndexes())
                    Assert.Equal(2, joinEntityType.GetForeignKeys().Count())

                    Dim fk = Assert.Single(joinEntityType.FindDeclaredForeignKeys({joinEntityType.GetProperty("BlogsKey")}))
                    Assert.False(fk.PrincipalKey.IsPrimaryKey())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Many_to_many_ef6()

            Dim expectedDbContextCode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Blogs As DbSet(Of Blog)

        Public Overridable Property Posts As DbSet(Of Post)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Blog)(
                Sub(entity)
                    entity.HasMany(Function(d) d.Posts).WithMany(Function(p) p.Blogs).
                        UsingEntity(Of Dictionary(Of String, Object))(
                            ""PostBlog"",
                            Function(r) r.HasOne(Of Post)().WithMany().
                                HasForeignKey(""PostId"").
                                HasConstraintName(""Post_Blogs_Source""),
                            Function(l) l.HasOne(Of Blog)().WithMany().
                                HasForeignKey(""BlogId"").
                                HasConstraintName(""Post_Blogs_Target""),
                            Sub(j)
                                j.HasKey(""BlogId"", ""PostId"")
                                j.ToTable(""PostBlogs"")
                                j.HasIndex({{""PostId""}}, ""IX_PostBlogs_Post_Id"")
                                j.IndexerProperty(Of Integer)(""BlogId"").HasColumnName(""Blog_Id"")
                                j.IndexerProperty(Of Integer)(""PostId"").HasColumnName(""Post_Id"")
                            End Sub)
                End Sub)

            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Test(
                Sub(ModelBuilder)
                    ModelBuilder.
                        Entity(
                            "Blog",
                            Sub(x)
                                x.ToTable("Blogs")
                                x.HasAnnotation(ScaffoldingAnnotationNames.DbSetName, "Blogs")
                                x.Property(Of Integer)("Id")
                            End Sub).
                        Entity(
                            "Post",
                            Sub(x)
                                x.ToTable("Posts")
                                x.HasAnnotation(ScaffoldingAnnotationNames.DbSetName, "Posts")

                                x.Property(Of Integer)("Id")

                                x.HasMany("Blog", "Blogs").WithMany("Posts").
                                    UsingEntity(
                                        "PostBlog",
                                        Function(r) r.HasOne("Blog", Nothing).WithMany().HasForeignKey("BlogId").HasConstraintName("Post_Blogs_Target"),
                                        Function(l) l.HasOne("Post", Nothing).WithMany().HasForeignKey("PostId").HasConstraintName("Post_Blogs_Source"),
                                        Sub(j)
                                            j.ToTable("PostBlogs")
                                            j.HasAnnotation(ScaffoldingAnnotationNames.DbSetName, "PostBlogs")

                                            j.Property(Of Integer)("BlogId").HasColumnName("Blog_Id")
                                            j.Property(Of Integer)("PostId").HasColumnName("Post_Id")
                                        End Sub)
                            End Sub)
                End Sub,
                New ModelCodeGenerationOptions(),
                Sub(code)
                    AssertFileContents(
                        expectedDbContextCode,
                        code.ContextFile)
                End Sub,
                Sub(model)
                    Assert.Collection(
                        model.GetEntityTypes().OrderBy(Function(e) e.Name),
                        Sub(t1)
                            Assert.Equal("PostBlog", t1.Name)
                            Assert.Equal("PostBlogs", t1.GetTableName())
                            Assert.Collection(
                                t1.GetForeignKeys().OrderBy(Function(fk) fk.GetConstraintName()),
                                Sub(fk1)
                                    Assert.Equal("Post_Blogs_Source", fk1.GetConstraintName())
                                    Dim prop = Assert.Single(fk1.Properties)
                                    Assert.Equal("PostId", prop.Name)
                                    Assert.Equal("Post_Id", prop.GetColumnName(StoreObjectIdentifier.Table(t1.GetTableName())))
                                    Assert.Equal("TestNamespace.Post", fk1.PrincipalEntityType.Name)
                                    Assert.Equal(DeleteBehavior.Cascade, fk1.DeleteBehavior)
                                End Sub,
                                Sub(fk2)
                                    Assert.Equal("Post_Blogs_Target", fk2.GetConstraintName())
                                    Dim prop = Assert.Single(fk2.Properties)
                                    Assert.Equal("BlogId", prop.Name)
                                    Assert.Equal("Blog_Id", prop.GetColumnName(StoreObjectIdentifier.Table(t1.GetTableName())))
                                    Assert.Equal("TestNamespace.Blog", fk2.PrincipalEntityType.Name)
                                    Assert.Equal(DeleteBehavior.Cascade, fk2.DeleteBehavior)
                                End Sub)
                        End Sub,
                        Sub(t2)
                            Assert.Equal("TestNamespace.Blog", t2.Name)
                            Assert.Equal("Blogs", t2.GetTableName())
                            Assert.Empty(t2.GetDeclaredForeignKeys())
                            Dim SkipNavigation = Assert.Single(t2.GetSkipNavigations())
                            Assert.Equal("Posts", SkipNavigation.Name)
                            Assert.Equal("Blogs", SkipNavigation.Inverse.Name)
                            Assert.Equal("PostBlog", SkipNavigation.JoinEntityType.Name)
                            Assert.Equal("Post_Blogs_Target", SkipNavigation.ForeignKey.GetConstraintName())
                        End Sub,
                        Sub(t3)
                            Assert.Equal("TestNamespace.Post", t3.Name)
                            Assert.Equal("Posts", t3.GetTableName())
                            Assert.Empty(t3.GetDeclaredForeignKeys())
                            Dim SkipNavigation = Assert.Single(t3.GetSkipNavigations())
                            Assert.Equal("Blogs", SkipNavigation.Name)
                            Assert.Equal("Posts", SkipNavigation.Inverse.Name)
                            Assert.Equal("PostBlog", SkipNavigation.JoinEntityType.Name)
                            Assert.Equal("Post_Blogs_Source", SkipNavigation.ForeignKey.GetConstraintName())
                        End Sub)
                End Sub)
        End Sub

        <Fact>
        Public Sub RequiredAttribute_is_generated_no_matter_NRT_option()
            ' UseNullableReferenceTypes option should be ignored for VB

            Dim expected =
"Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.VisualBasic

Namespace TestNamespace
    Partial Public Class Color
        <Key>
        Public Property Id As Integer

        <Required>
        Public Property ColorCode As String
    End Class
End Namespace
"
            Dim mb As Action(Of ModelBuilder) =
                Sub(modelBuilder As ModelBuilder)
                    modelBuilder.Entity(
                        "Color",
                        Sub(x)
                            x.Property(Of Integer)("Id")
                            x.Property(Of String)("ColorCode").IsRequired()
                        End Sub)
                End Sub

            Test(
                mb,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True,
                    .UseNullableReferenceTypes = True
                },
                Sub(code)
                    AssertFileContents(
                        expected,
                        code.AdditionalFiles.First())
                End Sub,
                Sub(model)
                End Sub)

            Test(
                mb,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True,
                    .UseNullableReferenceTypes = False
                },
                Sub(code)
                    AssertFileContents(
                        expected,
                        code.AdditionalFiles.First())
                End Sub,
                Sub(model)
                End Sub)
        End Sub

        Protected Overrides Function AddModelServices(services As IServiceCollection) As IServiceCollection
            Return services.Replace(
                ServiceDescriptor.Singleton(Of IRelationalAnnotationProvider, TestModelAnnotationProvider)())
        End Function

        Protected Overrides Function AddScaffoldingServices(services As IServiceCollection) As IServiceCollection
            Return services.Replace(
                ServiceDescriptor.Singleton(Of IAnnotationCodeGenerator, TestModelAnnotationCodeGenerator)())
        End Function

        Private Class TestModelAnnotationProvider
            Inherits SqlServerAnnotationProvider

            Public Sub New(dependencies As RelationalAnnotationProviderDependencies)
                MyBase.New(dependencies)
            End Sub

            Public Overrides Iterator Function [For](table As ITable, designTime As Boolean) As IEnumerable(Of IAnnotation)
                For Each annotation In MyBase.For(table, designTime)
                    Yield annotation
                Next

                Dim entityType = table.EntityTypeMappings.First().TypeBase

                For Each annotation In entityType.GetAnnotations().Where(Function(a) a.Name = "Custom:EntityAnnotation")
                    Yield annotation
                Next
            End Function

            Public Overrides Iterator Function [For](column As IColumn, designTime As Boolean) As IEnumerable(Of IAnnotation)
                For Each annotation In MyBase.For(column, designTime)
                    Yield annotation
                Next

                Dim properties = column.PropertyMappings.Select(Function(m) m.Property)
                Dim annotations = properties.SelectMany(Function(p) p.GetAnnotations()).GroupBy(Function(a) a.Name).Select(Function(g) g.First())

                For Each annotation In annotations.Where(Function(a) a.Name = "Custom:PropertyAnnotation")
                    Yield annotation
                Next
            End Function
        End Class

        Private Class TestModelAnnotationCodeGenerator
            Inherits SqlServerAnnotationCodeGenerator

            Public Sub New(dependencies As AnnotationCodeGeneratorDependencies)
                MyBase.New(dependencies)
            End Sub

            Protected Overrides Function GenerateDataAnnotation(entityType As IEntityType, annotation As IAnnotation) As AttributeCodeFragment
                Select Case annotation.Name
                    Case "Custom:EntityAnnotation"
                        Return New AttributeCodeFragment(GetType(CustomEntityDataAnnotationAttribute),
                                                         TryCast(annotation.Value, String))
                    Case Else
                        Return MyBase.GenerateDataAnnotation(entityType, annotation)
                End Select
            End Function

            Protected Overrides Function GenerateDataAnnotation([property] As IProperty, annotation As IAnnotation) As AttributeCodeFragment
                Select Case annotation.Name
                    Case "Custom:PropertyAnnotation"
                        Return New AttributeCodeFragment(GetType(CustomPropertyDataAnnotationAttribute),
                                                         TryCast(annotation.Value, String))
                    Case Else
                        Return MyBase.GenerateDataAnnotation([property], annotation)
                End Select
            End Function
        End Class

        <AttributeUsage(AttributeTargets.Class)>
        Public Class CustomEntityDataAnnotationAttribute
            Inherits Attribute

            Public Sub New(argument As String)
                Me.Argument = argument
            End Sub

            Public Overridable ReadOnly Property Argument As String
        End Class

        <AttributeUsage(AttributeTargets.Property Or AttributeTargets.Field)>
        Public Class CustomPropertyDataAnnotationAttribute
            Inherits Attribute

            Public Sub New(argument As String)
                Me.Argument = argument
            End Sub

            Public Overridable ReadOnly Property Argument As String
        End Class
    End Class
End Namespace
