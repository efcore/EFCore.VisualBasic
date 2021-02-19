Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Scaffolding
Imports Xunit

Namespace Scaffolding.Internal
    Public Class VisualBasicEntityTypeGeneratorTest

        <ConditionalFact>
        Public Sub KeylessAttribute_is_generated_for_key_less_entity()

            Dim expectedCode =
"Imports System
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    <Keyless>
    Public Partial Class Vista
    End Class
End Namespace
"
            Dim expectedDbContextCode =
"Imports System
Imports Microsoft.VisualBasic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata

Namespace TestNamespace
    Public Partial Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Vista As DbSet(Of Vista)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            If Not optionsBuilder.IsConfigured Then
                optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
            End If
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
New ModelCodeGenerationOptions With
{
                .UseDataAnnotations = True},
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
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    <Table(""Vistas"")>
    Public Partial Class Vista
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
                            b.[Property](Of Integer)("Id")
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
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Public Partial Class Vista
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
                            b.[Property](Of Integer)("Id")
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
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    <Table(""Vista"", Schema :=""custom"")>
    Public Partial Class Vista
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
                            b.[Property](Of Integer)("Id")
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
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    <Keyless>
    Public Partial Class Vista
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
        Public Sub IndexAttribute_is_generated_for_multiple_indexes_with_name_unique()

            Dim expectedCode =
"Imports System
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    <Index(""C"")>
    <Index(""A"", ""B"", Name :=""IndexOnAAndB"", IsUnique :=True)>
    <Index(""B"", ""C"", Name :=""IndexOnBAndC"")>
    Public Partial Class EntityWithIndexes
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
                          x.HasIndex({"A", "B"}, "IndexOnAAndB") _
                              .IsUnique()
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
        Public Sub Entity_with_indexes_generates_IndexAttribute_only_for_indexes_without_annotations()

            Dim expectedCode =
"Imports System
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    <Index(""A"", ""B"", Name :=""IndexOnAAndB"", IsUnique :=True)>
    Public Partial Class EntityWithIndexes
        <Key>
        Public Property Id As Integer
        Public Property A As Integer
        Public Property B As Integer
        Public Property C As Integer
    End Class
End Namespace
"

            Dim expectedDbContextCode =
"Imports System
Imports Microsoft.VisualBasic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata

Namespace TestNamespace
    Public Partial Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property EntityWithIndexes As DbSet(Of EntityWithIndexes)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            If Not optionsBuilder.IsConfigured Then
                optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
            End If
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)

            modelBuilder.Entity(Of EntityWithIndexes)(
                Sub(entity)
                    entity.HasIndex(Function(e) New With {e.B, e.C}, ""IndexOnBAndC"").
                        HasFilter(""Filter SQL"")
                    entity.Property(Function(e) e.Id).UseIdentityColumn()
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
                            x.HasIndex({"A", "B"}, "IndexOnAAndB") _
                                .IsUnique()
                            x.HasIndex({"B", "C"}, "IndexOnBAndC") _
                                .HasFilter("Filter SQL")
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
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Public Partial Class Entity
        <Key>
        Public Property PrimaryKey As Integer
    End Class
End Namespace
"

            Dim expectedDbContextCode =
"Imports System
Imports Microsoft.VisualBasic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata

Namespace TestNamespace
    Public Partial Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Entity As DbSet(Of Entity)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            If Not optionsBuilder.IsConfigured Then
                optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
            End If
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)

            modelBuilder.Entity(Of Entity)(
                Sub(entity)
                    entity.Property(Function(e) e.PrimaryKey).UseIdentityColumn()
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
        Public Sub KeyAttribute_is_generated_on_multiple_properties_but_configuring_Imports_fluent_api_for_composite_key()

            Dim expectedCode =
"Imports System
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Public Partial Class Post
        <Key>
        Public Property Key As Integer
        <Key>
        Public Property Serial As Integer
    End Class
End Namespace
"

            Dim expectedDbContextCode =
"Imports System
Imports Microsoft.VisualBasic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata

Namespace TestNamespace
    Public Partial Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Post As DbSet(Of Post)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            If Not optionsBuilder.IsConfigured Then
                optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
            End If
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)

            modelBuilder.Entity(Of Post)(
                Sub(entity)
                    entity.HasKey(Function(e) New With {e.Key, e.Serial})
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
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Public Partial Class Entity
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
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Public Partial Class Entity
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
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Public Partial Class Entity
        <Key>
        Public Property Id As Integer
        <Column(""propertyA"")>
        Public Property A As String
        <Column(TypeName :=""nchar(10)"")>
        Public Property B As String
        <Column(""random"", TypeName :=""varchar(200)"")>
        Public Property C As String
        <Column(TypeName :=""numeric(18, 2)"")>
        Public Property D As Decimal
        <StringLength(100)>
        Public Property E As String
    End Class
End Namespace
"
            Dim expectedDbContextCode =
"Imports System
Imports Microsoft.VisualBasic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata

Namespace TestNamespace
    Public Partial Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Entity As DbSet(Of Entity)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            If Not optionsBuilder.IsConfigured Then
                optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
            End If
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)

            modelBuilder.Entity(Of Entity)(
                Sub(entity)
                    entity.Property(Function(e) e.Id).UseIdentityColumn()
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
                    Assert.Equal("propertyA", entitType.GetProperty("A").GetColumnBaseName())
                    Assert.Equal("nchar(10)", entitType.GetProperty("B").GetColumnType())
                    Assert.Equal("random", entitType.GetProperty("C").GetColumnBaseName())
                    Assert.Equal("varchar(200)", entitType.GetProperty("C").GetColumnType())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub MaxLengthAttribute_is_generated_for_property()

            Dim expectedCode =
"Imports System
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Public Partial Class Entity
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
        Public Sub Comments_are_generated()

            Dim expectedCode =
"Imports System
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    ''' <summary>
    ''' Entity Comment
    ''' </summary>
    Public Partial Class Entity
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
                            x.HasComment("Entity Comment")
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
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    ''' <summary>
    ''' Entity Comment
    ''' On multiple lines
    ''' With XML content &lt;br/&gt;
    ''' </summary>
    Public Partial Class Entity
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
                          x.HasComment(
"Entity Comment
On multiple lines
With XML content <br/>")
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
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Public Partial Class Entity
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
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Public Partial Class Entity
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
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Public Partial Class Post
        Public Sub New()
            Contributions = New HashSet(Of Contribution)()
        End Sub

        <Key>
        Public Property Id As Integer
        Public Property AuthorId As Integer?

        <ForeignKey(NameOf(AuthorId))>
        <InverseProperty(NameOf(Person.Posts))>
        Public Overridable Property Author As Person
        Public Overridable Property Contributions As ICollection(Of Contribution)
    End Class
End Namespace
"

            Dim expectedCodePerson =
"Imports System
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Public Partial Class Person
        Public Sub New()
            Posts = New HashSet(Of Post)()
        End Sub

        <Key>
        Public Property Id As Integer

        <InverseProperty(NameOf(Post.Author))>
        Public Overridable Property Posts As ICollection(Of Post)
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
                            x.HasMany("Contribution", "Contributions").WithOne()
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
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Public Partial Class Post
        <Key>
        Public Property Id As Integer
        Public Property BlogId1 As Integer?
        Public Property BlogId2 As Integer?

        <ForeignKey(""BlogId1,BlogId2"")>
        <InverseProperty(NameOf(Blog.Posts))>
        Public Overridable Property BlogNavigation As Blog
    End Class
End Namespace
"

            Dim expectedDbContextCode =
"Imports System
Imports Microsoft.VisualBasic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata

Namespace TestNamespace
    Public Partial Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Blog As DbSet(Of Blog)
        Public Overridable Property Post As DbSet(Of Post)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            If Not optionsBuilder.IsConfigured Then
                optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
            End If
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)

            modelBuilder.Entity(Of Blog)(
                Sub(entity)
                    entity.HasKey(Function(e) New With {e.Id1, e.Id2})
                End Sub)

            modelBuilder.Entity(Of Post)(
                Sub(entity)
                    entity.Property(Function(e) e.Id).UseIdentityColumn()
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
New ModelCodeGenerationOptions With
{
                .UseDataAnnotations = True},
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
        Public Sub ForeignKeyAttribute_InversePropertyAttribute_is_not_generated_for_alternate_key()

            Dim expectedCode =
"Imports System
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Public Partial Class Post
        <Key>
        Public Property Id As Integer
        Public Property BlogId1 As Integer?
        Public Property BlogId2 As Integer?

        Public Overridable Property BlogNavigation As Blog
    End Class
End Namespace
"

            Dim expectedDbContextCode =
"Imports System
Imports Microsoft.VisualBasic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata

Namespace TestNamespace
    Public Partial Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Blog As DbSet(Of Blog)
        Public Overridable Property Post As DbSet(Of Post)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            If Not optionsBuilder.IsConfigured Then
                optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
            End If
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)

            modelBuilder.Entity(Of Blog)(
                Sub(entity)
                    entity.Property(Function(e) e.Id).UseIdentityColumn()
                End Sub)

            modelBuilder.Entity(Of Post)(
                Sub(entity)
                    entity.Property(Function(e) e.Id).UseIdentityColumn()
                    entity.HasOne(Function(d) d.BlogNavigation).
                        WithMany(Function(p) p.Posts).
                        HasPrincipalKey(Function(e) New With {e.Id1, e.Id2}).
                        HasForeignKey(Function(e) New With {e.BlogId1, e.BlogId2})
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

                        x.HasOne("Blog", "BlogNavigation").WithMany("Posts") _
                            .HasPrincipalKey("Id1", "Id2") _
                            .HasForeignKey("BlogId1", "BlogId2")
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
        Public Sub InverseProperty_when_navigation_property_with_same_type_and_navigation_name()

            Dim expectedCode =
"Imports System
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Public Partial Class Post
        <Key>
        Public Property Id As Integer
        Public Property BlogId As Integer?

        <ForeignKey(NameOf(BlogId))>
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
New ModelCodeGenerationOptions With
{
                .UseDataAnnotations = True},
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
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Public Partial Class Post
        <Key>
        Public Property Id As Integer
        Public Property Blog As Integer?

        <ForeignKey(NameOf(Blog))>
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
Imports Microsoft.VisualBasic
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Public Partial Class Post
        <Key>
        Public Property Id As Integer
        Public Property BlogId As Integer?
        Public Property OriginalBlogId As Integer?

        <ForeignKey(NameOf(BlogId))>
        <InverseProperty(""Posts"")>
        Public Overridable Property Blog As Blog
        <ForeignKey(NameOf(OriginalBlogId))>
        <InverseProperty(""OriginalPosts"")>
        Public Overridable Property OriginalBlog As Blog
    End Class
End Namespace
"

            Test(
                Function(modelBuilder)
                    Return modelBuilder.Entity(
                        "Blog",
                        Function(x) x.Property(Of Integer)("Id")).Entity(
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

        Private Shared Sub AssertFileContents(
            expectedCode As String,
            file As ScaffoldedFile)

            Assert.Equal(expectedCode, file.Code, ignoreLineEndingDifferences:=True)
        End Sub
    End Class

End Namespace