Imports System.Reflection
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Scaffolding
Imports Microsoft.EntityFrameworkCore.SqlServer.Design.Internal
Imports Microsoft.EntityFrameworkCore.SqlServer.Internal
Imports Microsoft.EntityFrameworkCore.SqlServer.Metadata.Internal
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.DependencyInjection.Extensions
Imports Xunit
Imports Xunit.Abstractions

Namespace Scaffolding.Internal
    Public Class VisualBasicDbContextGeneratorTest
        Inherits VisualBasicModelCodeGeneratorTestBase

        Public Sub New(fixture As ModelCodeGeneratorTestFixture, output As ITestOutputHelper)
            MyBase.New(fixture, output)
        End Sub

        <ConditionalFact>
        Public Sub Empty_model()

            Dim expectedCode As String =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

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
                Sub(modelBuilder)
                End Sub,
                New ModelCodeGenerationOptions,
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.ContextFile)

                    Assert.Empty(code.AdditionalFiles)
                End Sub,
                Sub(model) Assert.Empty(model.GetEntityTypes()))
        End Sub

        <ConditionalFact()>
        Public Sub SuppressConnectionStringWarning_works()

            Dim expectedCode As String =
"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
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
                Sub(modelBuilder)
                End Sub,
                New ModelCodeGenerationOptions With {
                    .SuppressConnectionStringWarning = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.ContextFile)

                    Assert.Empty(code.AdditionalFiles)
                End Sub,
                Sub(model) Assert.Empty(model.GetEntityTypes()))
        End Sub

        <ConditionalFact>
        Public Sub SuppressOnConfiguring_works()

            Dim expectedcode =
"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
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
                Sub(modelBuilder)
                End Sub,
                New ModelCodeGenerationOptions With {
                    .SuppressOnConfiguring = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedcode,
                        code.ContextFile)

                    Assert.Empty(code.AdditionalFiles)
                End Sub,
                Nothing)
        End Sub

        <ConditionalFact>
        Public Sub Required_options_to_GenerateModel_are_not_null()

            Dim generator = CreateServices().
                AddSingleton(Of IProviderCodeGeneratorPlugin, TestCodeGeneratorPlugin)().
                BuildServiceProvider(validateScopes:=True).
                GetServices(Of IModelCodeGenerator)().
                Last(Function(g) TypeOf g Is VisualBasicModelGenerator)

            Assert.StartsWith(
                CoreStrings.ArgumentPropertyNull(NameOf(ModelCodeGenerationOptions.ContextName), "options"),
                Assert.Throws(Of ArgumentException)(
                    Function()
                        Return generator.GenerateModel(
                        New Model(),
                        New ModelCodeGenerationOptions With {
                            .ContextName = Nothing,
                            .ConnectionString = "Initial Catalog=TestDatabase"
                        })
                    End Function).Message)

            Assert.StartsWith(
                CoreStrings.ArgumentPropertyNull(NameOf(ModelCodeGenerationOptions.ConnectionString), "options"),
                Assert.Throws(Of ArgumentException)(
                    Function()
                        Return generator.GenerateModel(
                        New Model(),
                        New ModelCodeGenerationOptions With {
                            .ContextName = "TestDbContext",
                            .ConnectionString = Nothing
                        })
                    End Function).Message)
        End Sub

        <ConditionalFact>
        Public Sub Plugins_work()

            Dim generator = CreateServices().
                AddSingleton(Of IProviderCodeGeneratorPlugin, TestCodeGeneratorPlugin)().
                BuildServiceProvider(validateScopes:=True).
                GetServices(Of IModelCodeGenerator)().
                Last(Function(g) TypeOf g Is VisualBasicModelGenerator)

            Dim scaffoldedModel = generator.GenerateModel(
                New Model(),
                New ModelCodeGenerationOptions With {
                    .SuppressConnectionStringWarning = True,
                    .ModelNamespace = "TestNamespace",
                    .ContextName = "TestDbContext",
                    .ConnectionString = "Initial Catalog=TestDatabase"
                })

            Assert.Contains(
"optionsBuilder.
                UseSqlServer(""Initial Catalog=TestDatabase"", Sub(x) x.SetProviderOption()).
                SetContextOption()",
                scaffoldedModel.ContextFile.Code)
        End Sub

        <ConditionalFact>
        Public Sub Comments_use_fluent_api()

            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity(
                      "Entity",
                      Sub(x)
                          x.Property(Of Integer)("Id")
                          x.Property(Of Integer)("Property") _
                              .HasComment("An integer property")
                      End Sub)
                End Sub,
                New ModelCodeGenerationOptions,
                Sub(code)
                    Assert.Contains(
                        ".HasComment(""An integer property"")",
                        code.ContextFile.Code)
                End Sub,
                Sub(model)
                    Assert.Equal(
                        "An integer property",
                        model.FindEntityType("TestNamespace.Entity").GetProperty("Property").GetComment())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Entity_comments_use_fluent_api()
            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity(
                      "Entity",
                      Sub(x)
                          x.ToTable(Function(tb) tb.HasComment("An entity comment"))
                      End Sub)
                End Sub,
                New ModelCodeGenerationOptions,
                Sub(code)
                    Assert.Contains(
                        ".HasComment(""An entity comment"")",
                        code.ContextFile.Code)
                End Sub,
                Sub(model)
                    Assert.Equal(
                        "An entity comment",
                        model.FindEntityType("TestNamespace.Entity").GetComment())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Views_work()
            Test(
                Sub(modelBuilder) modelBuilder.Entity("Vista").ToView("Vista"),
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code) Assert.Contains(".ToView(""Vista"")", code.ContextFile.Code),
                Sub(model)
                    Dim entityType = model.FindEntityType("TestNamespace.Vista")

                    Assert.NotNull(entityType.FindAnnotation(RelationalAnnotationNames.ViewDefinitionSql))
                    Assert.Equal("Vista", entityType.GetViewName())
                    Assert.Null(entityType.GetViewSchema())
                    Assert.Null(entityType.GetTableName())
                    Assert.Null(entityType.GetSchema())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub ModelInDifferentNamespaceDbContext_works()
            Dim modelGenerationOptions As New ModelCodeGenerationOptions With {
                .ContextNamespace = "TestNamespace",
                .ModelNamespace = "AnotherNamespaceOfModel"
            }

            Const entityInAnotherNamespaceTypeName As String = "EntityInAnotherNamespace"

            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity(entityInAnotherNamespaceTypeName)
                End Sub,
                modelGenerationOptions,
                Sub(code)
                    Assert.Contains(String.Concat("Imports ", modelGenerationOptions.ModelNamespace), code.ContextFile.Code)
                End Sub,
                Sub(model)
                    Assert.NotNull(model.FindEntityType(String.Concat(modelGenerationOptions.ModelNamespace, ".", entityInAnotherNamespaceTypeName)))
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub ModelSameNamespaceDbContext_works()
            Dim modelGenerationOptions As New ModelCodeGenerationOptions With {
                .ContextNamespace = "TestNamespace"
            }

            Const entityInAnotherNamespaceTypeName As String = "EntityInAnotherNamespace"

            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity(entityInAnotherNamespaceTypeName)
                End Sub,
                modelGenerationOptions,
                Sub(code)
                    Assert.DoesNotContain(String.Concat("Imports ", modelGenerationOptions.ModelNamespace), code.ContextFile.Code)
                End Sub,
                Sub(model)
                    Assert.NotNull(model.FindEntityType(String.Concat(modelGenerationOptions.ModelNamespace, ".", entityInAnotherNamespaceTypeName)))
                End Sub
                )
        End Sub

        <ConditionalFact>
        Public Sub ValueGenerated_works()
            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity(
                        "Entity",
                        Sub(x)
                            x.Property(Of Integer)("ValueGeneratedOnAdd").ValueGeneratedOnAdd()
                            x.Property(Of Integer)("ValueGeneratedOnAddOrUpdate").ValueGeneratedOnAddOrUpdate()
                            x.Property(Of Integer)("ConcurrencyToken").IsConcurrencyToken()
                            x.Property(Of Integer)("ValueGeneratedOnUpdate").ValueGeneratedOnUpdate()
                            x.Property(Of Integer)("ValueGeneratedNever").ValueGeneratedNever()
                        End Sub)
                End Sub,
                New ModelCodeGenerationOptions,
                Sub(code)
                    Assert.Contains($"Property(Function(e) e.ValueGeneratedOnAdd).
                        ValueGeneratedOnAdd()", code.ContextFile.Code)
                    Assert.Contains("Property(Function(e) e.ValueGeneratedOnAddOrUpdate).ValueGeneratedOnAddOrUpdate()", code.ContextFile.Code)
                    Assert.Contains("Property(Function(e) e.ConcurrencyToken).IsConcurrencyToken()", code.ContextFile.Code)
                    Assert.Contains("Property(Function(e) e.ValueGeneratedOnUpdate).ValueGeneratedOnUpdate()", code.ContextFile.Code)
                    Assert.Contains("Property(Function(e) e.ValueGeneratedNever).ValueGeneratedNever()", code.ContextFile.Code)
                End Sub,
                Sub(model)
                    Dim entity1 = model.FindEntityType("TestNamespace.Entity")
                    Assert.Equal(ValueGenerated.OnAdd, entity1.GetProperty("ValueGeneratedOnAdd").ValueGenerated)
                    Assert.Equal(ValueGenerated.OnAddOrUpdate, entity1.GetProperty("ValueGeneratedOnAddOrUpdate").ValueGenerated)
                    Assert.[True](entity1.GetProperty("ConcurrencyToken").IsConcurrencyToken)
                    Assert.Equal(ValueGenerated.OnUpdate, entity1.GetProperty("ValueGeneratedOnUpdate").ValueGenerated)
                    Assert.Equal(ValueGenerated.Never, entity1.GetProperty("ValueGeneratedNever").ValueGenerated)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub HasPrecision_works()
            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity(
                        "Entity",
                        Sub(x)
                            x.Property(Of Decimal)("HasPrecision").HasPrecision(12)
                            x.Property(Of Decimal)("HasPrecisionAndScale").HasPrecision(14, 7)
                        End Sub)
                End Sub,
                New ModelCodeGenerationOptions,
                Sub(code)
                    Assert.Contains("Property(Function(e) e.HasPrecision).HasPrecision(12)", code.ContextFile.Code)
                    Assert.Contains("Property(Function(e) e.HasPrecisionAndScale).HasPrecision(14, 7)", code.ContextFile.Code)
                End Sub,
                Sub(model)
                    Dim entity1 = model.FindEntityType("TestNamespace.Entity")
                    Assert.Equal(12, entity1.GetProperty("HasPrecision").GetPrecision())
                    Assert.Null(entity1.GetProperty("HasPrecision").GetScale())
                    Assert.Equal(14, entity1.GetProperty("HasPrecisionAndScale").GetPrecision())
                    Assert.Equal(7, entity1.GetProperty("HasPrecisionAndScale").GetScale())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Collation_works()
            Test(
                Sub(modelBuilder) modelBuilder.Entity("Entity").Property(Of String)("UseCollation").UseCollation("Some Collation"),
                New ModelCodeGenerationOptions,
                Sub(code) Assert.Contains("Property(Function(e) e.UseCollation).UseCollation(""Some Collation"")", code.ContextFile.Code),
                Sub(model)
                    Dim entity1 = model.FindEntityType("TestNamespace.Entity")
                    Assert.Equal("Some Collation", entity1.GetProperty("UseCollation").GetCollation())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub ComputedColumnSql_works()
            Test(
                Sub(modelBuilder) modelBuilder.Entity("Entity").Property(Of String)("ComputedColumn").HasComputedColumnSql("1 + 2"),
                New ModelCodeGenerationOptions,
                Sub(code) Assert.Contains(".HasComputedColumnSql(""1 + 2"")", code.ContextFile.Code),
                Sub(model)
                    Dim entity1 = model.FindEntityType("TestNamespace.Entity")
                    Assert.Equal("1 + 2", entity1.GetProperty("ComputedColumn").GetComputedColumnSql())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub IsUnicode_works()
            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity("Entity").Property(Of String)("UnicodeColumn").IsUnicode()
                    modelBuilder.Entity("Entity").Property(Of String)("NonUnicodeColumn").IsUnicode(False)
                End Sub,
                New ModelCodeGenerationOptions,
                Sub(code)
                    Assert.Contains("Property(Function(e) e.UnicodeColumn).IsUnicode()", code.ContextFile.Code)
                    Assert.Contains("Property(Function(e) e.NonUnicodeColumn).IsUnicode(False)", code.ContextFile.Code)
                End Sub,
                Sub(model)
                    Dim entity1 = model.FindEntityType("TestNamespace.Entity")
                    Assert.True(entity1.GetProperty("UnicodeColumn").IsUnicode())
                    Assert.False(entity1.GetProperty("NonUnicodeColumn").IsUnicode())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub ComputedColumnSql_works_stored()
            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity("Entity").Property(Of String)("ComputedColumn").
                                                         HasComputedColumnSql("1 + 2", stored:=True)
                End Sub,
                New ModelCodeGenerationOptions,
                Sub(code) Assert.Contains(".HasComputedColumnSql(""1 + 2"", True)", code.ContextFile.Code),
                Sub(model)
                    Dim entity1 = model.FindEntityType("TestNamespace.Entity")
                    Assert.[True](entity1.GetProperty("ComputedColumn").GetIsStored())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub ComputedColumnSql_works_unspecified()
            Test(
                Sub(modelBuilder) modelBuilder.Entity("Entity").Property(Of String)("ComputedColumn").HasComputedColumnSql(),
                New ModelCodeGenerationOptions,
                Sub(code) Assert.Contains(".HasComputedColumnSql()", code.ContextFile.Code),
                Sub(model)
                    Dim entity1 = model.FindEntityType("TestNamespace.Entity")
                    Assert.Empty(entity1.GetProperty("ComputedColumn").GetComputedColumnSql())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DefaultValue_works_unspecified()
            Test(
                Sub(modelBuilder) modelBuilder.Entity("Entity").Property(Of String)("DefaultedColumn").HasDefaultValue(),
                New ModelCodeGenerationOptions,
                Sub(code) Assert.Contains(".HasDefaultValue()", code.ContextFile.Code),
                Sub(model)
                    Dim entity1 = model.FindEntityType("TestNamespace.Entity")
                    Assert.Equal(DBNull.Value, entity1.GetProperty("DefaultedColumn").GetDefaultValue())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DefaultValueSql_works_unspecified()
            Test(
                Sub(modelBuilder) modelBuilder.Entity("Entity").Property(Of String)("DefaultedColumn").HasDefaultValueSql(),
                New ModelCodeGenerationOptions,
                Sub(code) Assert.Contains(".HasDefaultValueSql()", code.ContextFile.Code),
                Sub(model)
                    Dim entity1 = model.FindEntityType("TestNamespace.Entity")
                    Assert.Empty(entity1.GetProperty("DefaultedColumn").GetDefaultValueSql())
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub modelBuilder_annotation_generated_correctly()
            Test(
                Sub(modelBuilder) modelBuilder.HasAnnotation("TestAnnotation1", 1),
                New ModelCodeGenerationOptions,
                Sub(code) Assert.Contains("modelBuilder.HasAnnotation(""TestAnnotation1"", 1)", code.ContextFile.Code),
                Sub(model)
                    Assert.Equal(1, model.FindAnnotation("TestAnnotation1").Value)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub modelBuilder_annotations_generated_correctly()
            Test(
                Sub(modelBuilder) modelBuilder.HasAnnotation("TestAnnotation1", 1).HasAnnotation("TestAnnotation2", CByte(2)),
                New ModelCodeGenerationOptions,
                Sub(code) Assert.Contains(
"modelBuilder.
                HasAnnotation(""TestAnnotation1"", 1).
                HasAnnotation(""TestAnnotation2"", CByte(2))", code.ContextFile.Code),
                Sub(model)
                    Assert.Equal(1, model.FindAnnotation("TestAnnotation1").Value)
                    Assert.Equal(CByte(2), model.FindAnnotation("TestAnnotation2").Value)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Entity_with_indexes_and_use_data_annotations_false_always_generates_fluent_API()

            Dim expectedcode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore

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
                    entity.HasIndex(Function(e) New With {{e.A, e.B}}, ""IndexOnAAndB"").
                        IsUnique().
                        IsDescending(False, True)

                    entity.HasIndex(Function(e) New With {{e.B, e.C}}, ""IndexOnBAndC"").
                        HasFilter(""Filter SQL"").
                        HasAnnotation(""AnnotationName"", ""AnnotationValue"")

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
                Sub(modelBuilder)
                    modelBuilder.Entity(
                        "EntityWithIndexes",
                        Sub(x)
                            x.Property(Of Integer)("Id")
                            x.Property(Of Integer)("A")
                            x.Property(Of Integer)("B")
                            x.Property(Of Integer)("C")
                            x.HasKey("Id")
                            x.HasIndex({"A", "B"}, "IndexOnAAndB").
                                IsUnique().
                                IsDescending(False, True)
                            x.HasIndex({"B", "C"}, "IndexOnBAndC").
                                HasFilter("Filter SQL").
                                HasAnnotation("AnnotationName", "AnnotationValue")
                        End Sub)
                End Sub,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = False
                },
                Sub(code)
                    AssertFileContents(
                        expectedcode,
                        code.ContextFile)
                End Sub,
                Sub(model) Assert.Equal(2, model.FindEntityType("TestNamespace.EntityWithIndexes").GetIndexes().Count()))
        End Sub

        <ConditionalFact>
        Public Sub Entity_with_indexes_and_use_data_annotations_true_generates_fluent_API_only_for_indexes_with_annotations()

            Dim expectedCode As String =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore

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
                    entity.HasIndex(Function(e) New With {{e.B, e.C}}, ""IndexOnBAndC"").
                        HasFilter(""Filter SQL"").
                        HasAnnotation(""AnnotationName"", ""AnnotationValue"")

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
                Sub(modelBuilder)
                    modelBuilder.Entity(
                        "EntityWithIndexes",
                        Sub(x)
                            x.Property(Of Integer)("Id")
                            x.Property(Of Integer)("A")
                            x.Property(Of Integer)("B")
                            x.Property(Of Integer)("C")
                            x.HasKey("Id")
                            x.HasIndex({"A", "B"}, "IndexOnAAndB").
                                IsUnique()
                            x.HasIndex({"B", "C"}, "IndexOnBAndC").
                                HasFilter("Filter SQL").
                                HasAnnotation("AnnotationName", "AnnotationValue")
                        End Sub)
                End Sub,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.ContextFile)
                End Sub,
                Sub(model) Assert.Equal(2, model.FindEntityType("TestNamespace.EntityWithIndexes").GetIndexes().Count()))
        End Sub

        <ConditionalFact>
        Public Sub Indexes_with_descending()

            Dim expectedCode As String =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore

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
                    entity.HasIndex(Function(e) New With {{e.X, e.Y, e.Z}}, ""IX_all_ascending"")

                    entity.HasIndex(Function(e) New With {{e.X, e.Y, e.Z}}, ""IX_all_descending"").IsDescending()

                    entity.HasIndex(Function(e) New With {{e.X, e.Y, e.Z}}, ""IX_empty"").IsDescending()

                    entity.HasIndex(Function(e) New With {{e.X, e.Y, e.Z}}, ""IX_mixed"").IsDescending(False, True, False)

                    entity.HasIndex(Function(e) New With {{e.X, e.Y, e.Z}}, ""IX_unspecified"")

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
                Sub(modelBuilder)
                    modelBuilder.Entity(
                        "EntityWithIndexes",
                        Sub(x)
                            x.Property(Of Integer)("Id")
                            x.Property(Of Integer)("X")
                            x.Property(Of Integer)("Y")
                            x.Property(Of Integer)("Z")
                            x.HasKey("Id")
                            x.HasIndex({"X", "Y", "Z"}, "IX_unspecified")
                            x.HasIndex({"X", "Y", "Z"}, "IX_empty").
                                IsDescending()
                            x.HasIndex({"X", "Y", "Z"}, "IX_all_ascending").
                                IsDescending(False, False, False)
                            x.HasIndex({"X", "Y", "Z"}, "IX_all_descending").
                                IsDescending(True, True, True)
                            x.HasIndex({"X", "Y", "Z"}, "IX_mixed").
                                IsDescending(False, True, False)
                        End Sub)
                End Sub,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = False
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.ContextFile)
                End Sub,
                Sub(model)
                    Dim EntityType = model.FindEntityType("TestNamespace.EntityWithIndexes")
                    Assert.Equal(5, EntityType.GetIndexes().Count())

                    Dim unspecifiedIndex = Assert.Single(EntityType.GetIndexes(), Function(i) i.Name = "IX_unspecified")
                    Assert.Null(unspecifiedIndex.IsDescending)

                    Dim emptyIndex = Assert.Single(EntityType.GetIndexes(), Function(i) i.Name = "IX_empty")
                    Assert.Equal(Array.Empty(Of Boolean)(), emptyIndex.IsDescending)

                    Dim allAscendingIndex = Assert.Single(EntityType.GetIndexes(), Function(i) i.Name = "IX_all_ascending")
                    Assert.Null(allAscendingIndex.IsDescending)

                    Dim allDescendingIndex = Assert.Single(EntityType.GetIndexes(), Function(i) i.Name = "IX_all_descending")
                    Assert.Equal(Array.Empty(Of Boolean)(), allDescendingIndex.IsDescending)

                    Dim mixedIndex = Assert.Single(EntityType.GetIndexes(), Function(i) i.Name = "IX_mixed")
                    Assert.Equal({False, True, False}, mixedIndex.IsDescending)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Entity_lambda_uses_correct_identifiers()

            Dim expectedCode As String =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property DependentEntity As DbSet(Of DependentEntity)

        Public Overridable Property PrincipalEntity As DbSet(Of PrincipalEntity)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of DependentEntity)(
                Sub(entity)
                    entity.HasIndex(Function(e) e.DependentId, ""IX_DependentEntity_DependentId"").IsUnique()

                    entity.Property(Function(e) e.Id).UseIdentityColumn()

                    entity.HasOne(Function(d) d.NavigationToPrincipal).WithOne(Function(p) p.NavigationToDependent).
                        HasPrincipalKey(Of PrincipalEntity)(Function(p) p.PrincipalId).
                        HasForeignKey(Of DependentEntity)(Function(d) d.DependentId)
                End Sub)

            modelBuilder.Entity(Of PrincipalEntity)(
                Sub(entity)
                    entity.HasKey(Function(e) e.AlternateId)

                    entity.Property(Function(e) e.AlternateId).UseIdentityColumn()
                End Sub)

            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity(
                        "PrincipalEntity", Sub(b)
                                               b.Property(Of Integer)("Id")
                                               b.Property(Of Integer)("PrincipalId")
                                               b.Property(Of Integer)("AlternateId")
                                               b.HasKey("AlternateId")
                                           End Sub)
                    modelBuilder.Entity(
                        "DependentEntity", Sub(b)
                                               b.Property(Of Integer)("Id")
                                               b.Property(Of Integer)("DependentId")
                                               b.HasOne("PrincipalEntity", "NavigationToPrincipal").
                                                   WithOne("NavigationToDependent").
                                                   HasForeignKey("DependentEntity", "DependentId").
                                                   HasPrincipalKey("PrincipalEntity", "PrincipalId")
                                           End Sub)
                End Sub,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = False
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.ContextFile)
                End Sub,
                Sub(model)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Column_type_is_not_scaffolded_as_annotation()

            Dim expectedCode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Employee As DbSet(Of Employee)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Employee)(
                Sub(entity)
                    entity.Property(Function(e) e.Id).UseIdentityColumn()
                    entity.Property(Function(e) e.HireDate).
                        HasColumnType(""date"").
                        HasColumnName(""hiring_date"")
                End Sub)

            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"
            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity(
                        "Employee",
                        Sub(x)
                            x.Property(Of Integer)("Id")
                            x.Property(Of Date)("HireDate").HasColumnType("date").HasColumnName("hiring_date")
                        End Sub)
                End Sub,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = False
                },
                Sub(code)
                    AssertFileContents(
                        expectedCode,
                        code.ContextFile)
                End Sub,
                Sub(model) Assert.Equal("date", model.FindEntityType("TestNamespace.Employee").GetProperty("HireDate").GetConfiguredColumnType()))
        End Sub

        <ConditionalFact>
        Public Sub Is_fixed_length_annotation_should_be_scaffolded_without_optional_parameter()
            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity(
                        "Employee",
                        Sub(x)
                            x.Property(Of Integer)("Id")
                            x.Property(Of String)("Name").HasMaxLength(5).IsFixedLength()
                        End Sub)
                End Sub,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = False
                },
                Sub(code) Assert.Contains(".
                        IsFixedLength()", code.ContextFile.Code),
                Sub(model) Assert.Equal(True, model.FindEntityType("TestNamespace.Employee").GetProperty("Name").IsFixedLength())
                )
        End Sub

        <ConditionalFact>
        Public Sub Root_namespace_is_removed_from_files_namespace()

            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity("Entity").Property(Of Integer)("Id")
                End Sub,
                New ModelCodeGenerationOptions With {
                    .RootNamespace = "MyApp.Test",
                    .ModelNamespace = "myapp.TEST.TestNamespace"
                },
                Sub(code)
                    Assert.Contains("Namespace TestNamespace", code.ContextFile.Code)
                    Assert.DoesNotContain("Imports Namespace", code.ContextFile.Code)
                    Assert.Contains("Namespace TestNamespace", code.AdditionalFiles(0).Code)
                End Sub,
                Sub(model)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Namespace_is_removed_if_is_root()

            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity("Entity").Property(Of Integer)("Id")
                End Sub,
                New ModelCodeGenerationOptions With {
                    .RootNamespace = "TestNamespace",
                    .ModelNamespace = "TestNaMeSpAcE",
                    .ContextNamespace = "TESTNAMESPACE"
                },
                Sub(code)
                    Assert.DoesNotContain("Namespace", code.ContextFile.Code)
                    Assert.DoesNotContain("Imports Namespace", code.ContextFile.Code)
                    Assert.DoesNotContain("Namespace", code.AdditionalFiles(0).Code)
                End Sub,
                Sub(model)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Namespace_is_imported_if_model_is_in_another_namespace()

            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity("Entity").Property(Of Integer)("Id")
                End Sub,
                New ModelCodeGenerationOptions With {
                    .ModelNamespace = "TestNaMeSpAcE.test2",
                    .ContextNamespace = "TESTNAMESPACE"
                },
                Sub(code)
                    Assert.Contains("Namespace TESTNAMESPACE", code.ContextFile.Code)
                    Assert.Contains("Imports TestNaMeSpAcE.test2", code.ContextFile.Code)
                    Assert.Contains("Namespace TestNaMeSpAcE.test2", code.AdditionalFiles(0).Code)
                End Sub,
                Sub(model)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Namespace_is_not_imported_if_model_is_in_the_same_namespace()

            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity("Entity").Property(Of Integer)("Id")
                End Sub,
                New ModelCodeGenerationOptions With {
                    .RootNamespace = "TestNamespace.Test2",
                    .ModelNamespace = "Test3",
                    .ContextNamespace = "TestNamespace.Test2.Test3"
                },
                Sub(code)
                    Assert.Contains("Namespace Test3", code.ContextFile.Code)
                    Assert.DoesNotContain("Imports TestNamespace", code.ContextFile.Code)
                    Assert.Contains("Namespace Test3", code.AdditionalFiles(0).Code)
                End Sub,
                Sub(model)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Root_namespace_is_added_to_imported_namespace_even_if_it_is_not_specified()

            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity("Entity").Property(Of Integer)("Id")
                End Sub,
                New ModelCodeGenerationOptions With {
                    .RootNamespace = "TestNamespace",
                    .ModelNamespace = "test1",
                    .ContextNamespace = "TestNamespace.Test2"
                },
                Sub(code)
                    Assert.Contains("Imports TestNamespace.test1", code.ContextFile.Code)
                End Sub,
                Sub(model)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Invalid_identifier_for_namespace_are_escaped()

            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity("Entity").Property(Of Integer)("Id")
                End Sub,
                New ModelCodeGenerationOptions With {
                    .RootNamespace = "TestNamespace",
                    .ContextNamespace = "TestNamespace.integer",
                    .ModelNamespace = "TestNamespace.Integer"
                },
                Sub(code)
                    Assert.Contains("Namespace [integer]", code.ContextFile.Code)
                    Assert.Contains("Namespace [Integer]", code.AdditionalFiles(0).Code)
                End Sub,
                Sub(model)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Global_namespace_works()

            Dim expectedcode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore

    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property MyEntity As DbSet(Of MyEntity)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of MyEntity)(
                Sub(entity)
                    entity.HasNoKey()
                End Sub)

            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
"

            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity("MyEntity")
                End Sub,
                New ModelCodeGenerationOptions With {
                    .ModelNamespace = String.Empty
                },
                Sub(code)
                    AssertFileContents(expectedcode, code.ContextFile)
                    Assert.DoesNotContain("namespace ", Assert.Single(code.AdditionalFiles).Code)
                End Sub,
                Sub(Model)
                    Assert.NotNull(Model.FindEntityType("MyEntity"))
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Global_namespace_works_just_context()
            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity("MyEntity")
                End Sub,
                New ModelCodeGenerationOptions With {
                    .ModelNamespace = "TestNamespace",
                    .ContextNamespace = String.Empty
                },
                Sub(code)
                    Assert.Contains("Imports TestNamespace", code.ContextFile.Code)
                    Assert.DoesNotContain("Namespace ", code.ContextFile.Code)
                    Assert.Contains("Namespace TestNamespace", Assert.Single(code.AdditionalFiles).Code)
                End Sub,
                Sub(Model)
                    Assert.NotNull(Model.FindEntityType("TestNamespace.MyEntity"))
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Global_namespace_works_just_model()
            Test(
                Sub(modelBuilder)
                    modelBuilder.Entity("MyEntity")
                End Sub,
                New ModelCodeGenerationOptions With {
                    .ModelNamespace = String.Empty,
                    .ContextNamespace = "TestNamespace"
                },
                Sub(code)
                    Assert.Contains("Namespace TestNamespace", code.ContextFile.Code)
                    Assert.DoesNotContain("Namespace ", Assert.Single(code.AdditionalFiles).Code)
                End Sub,
                Sub(Model)
                    Assert.NotNull(Model.FindEntityType("MyEntity"))
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Fluent_calls_in_custom_namespaces_work()

            Dim expectedcode =
"Imports System
Imports System.Collections.Generic
Imports CustomTestNamespace
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.TestFluentApiCall()

            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"
            Test(
                Sub(ModelBuilder) CustomTestNamespace.TestFluentApiCall(ModelBuilder),
                New ModelCodeGenerationOptions With {
                    .SuppressOnConfiguring = True
                },
                Sub(code)
                    AssertFileContents(
                        expectedcode,
                        code.ContextFile)

                    Assert.Empty(code.AdditionalFiles)
                End Sub,
                Sub(Model)
                    Assert.Empty(Model.GetEntityTypes())
                End Sub,
                skipBuild:=True)
        End Sub

        <ConditionalFact>
        Public Sub Temporal_table_works()

            ' Shadow properties. Issue #26007.

            Dim expectedcode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Customer As DbSet(Of Customer)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Customer)(
                Sub(entity)
                    entity.ToTable(Sub(tb) tb.IsTemporal(Sub(ttb)
                            ttb.UseHistoryTable(""CustomerHistory"")
                            ttb.
                            HasPeriodStart(""PeriodStart"").
                            HasColumnName(""PeriodStart"")
                            ttb.
                            HasPeriodEnd(""PeriodEnd"").
                            HasColumnName(""PeriodEnd"")
                        End Sub))

                    entity.Property(Function(e) e.Id).UseIdentityColumn()
                End Sub)

            OnModelCreatingPartial(modelBuilder)
        End Sub

        Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)
        End Sub
    End Class
End Namespace
"

            Assert.Equal(
                SqlServerStrings.TemporalPeriodPropertyMustBeInShadowState("Customer", "PeriodStart"),
                Assert.Throws(Of InvalidOperationException)(
                    Sub()
                        Test(
                            Sub(ModelBuilder)
                                ModelBuilder.Entity(
                                    "Customer",
                                    Sub(e)
                                        e.Property(Of Integer)("Id")
                                        e.Property(Of String)("Name")
                                        e.HasKey("Id")
                                        e.ToTable(Sub(tb) tb.IsTemporal())
                                    End Sub)
                            End Sub,
                            New ModelCodeGenerationOptions With {.UseDataAnnotations = False},
                            Sub(code)
                                AssertFileContents(
                                    expectedcode,
                                    code.ContextFile)
                            End Sub,
                            Sub(Model)
                                'TODO
                            End Sub)
                    End Sub).Message)
        End Sub

        <ConditionalFact>
        Public Sub Sequences_work()
            Test(
                Sub(ModelBuilder)
                    ModelBuilder.
                        HasSequence(Of Integer)("EvenNumbers", "dbo").
                        StartsAt(2).
                        IncrementsBy(2).
                        HasMin(2).
                        HasMax(100).
                        IsCyclic()
                End Sub,
                New ModelCodeGenerationOptions(),
                Sub(code)
                    Assert.Contains(
".HasSequence(Of Integer)(""EvenNumbers"", ""dbo"").
                StartsAt(2L).
                IncrementsBy(2).
                HasMin(2L).
                HasMax(100L).
                IsCyclic()",
                    code.ContextFile.Code)
                End Sub,
                Sub(Model)
                    Dim Sequence = Model.FindSequence("EvenNumbers", "dbo")
                    Assert.NotNull(Sequence)

                    Assert.Equal(GetType(Integer), Sequence.Type)
                    Assert.Equal(2, Sequence.StartValue)
                    Assert.Equal(2, Sequence.IncrementBy)
                    Assert.Equal(2, Sequence.MinValue)
                    Assert.Equal(100, Sequence.MaxValue)
                    Assert.True(Sequence.IsCyclic)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Trigger_works()

            Dim expectedcode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Employee As DbSet(Of Employee)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Employee)(
                Sub(entity)
                    entity.ToTable(Sub(tb)
                        tb.HasTrigger(""Trigger1"")
                        tb.HasTrigger(""Trigger2"")
                    End Sub)

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
                Sub(ModelBuilder)
                    ModelBuilder.Entity(
                        "Employee",
                        Sub(x)
                            x.Property(Of Integer)("Id")
                            x.ToTable(
                                Sub(tb)
                                    tb.HasTrigger("Trigger1")
                                    tb.HasTrigger("Trigger2")
                                End Sub)
                        End Sub)
                End Sub,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = False
                },
                Sub(code)
                    AssertFileContents(
                        expectedcode,
                        code.ContextFile)
                End Sub,
                Sub(Model)
                    Dim EntityType = Model.FindEntityType("TestNamespace.Employee")
                    Dim triggers = EntityType.GetDeclaredTriggers()

                    Assert.Collection(
                        triggers,
                        Sub(t) Assert.Equal("Trigger1", t.GetDatabaseName),
                        Sub(t) Assert.Equal("Trigger2", t.GetDatabaseName))
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub ValueGenerationStrategy_works_when_none()

            Dim expectedcode =
$"Imports System
Imports System.Collections.Generic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata

Namespace TestNamespace
    Partial Public Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Channel As DbSet(Of Channel)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            'TODO /!\ {DesignStrings.SensitiveInformationWarning}
            optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)
            modelBuilder.Entity(Of Channel)(
                Sub(entity)
                    entity.Property(Function(e) e.Id).HasAnnotation(""SqlServer:ValueGenerationStrategy"", SqlServerValueGenerationStrategy.None)
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
                    ModelBuilder.Entity(
                    "Channel",
                    Sub(x)
                        x.Property(Of Integer)("Id").
                            Metadata.SetValueGenerationStrategy(SqlServerValueGenerationStrategy.None)
                    End Sub)
                End Sub,
                New ModelCodeGenerationOptions(),
                Sub(code)
                    AssertFileContents(
                        expectedcode,
                        code.ContextFile)
                End Sub,
                Sub(Model)
                    Dim entityType = Assert.Single(model.GetEntityTypes())
                    Dim prop = Assert.Single(entityType.GetProperties())
                    Assert.Equal(SqlServerValueGenerationStrategy.None, prop.GetValueGenerationStrategy())
                End Sub)
        End Sub

        <ConditionalTheory>
        <InlineData(False)>
        <InlineData(True)>
        Public Sub ColumnOrder_is_ignored(useDataAnnotations As Boolean)
            Test(
                Sub(ModelBuilder)
                    ModelBuilder.Entity("Entity").Property(Of String)("Property").HasColumnOrder(1)
                End Sub,
                New ModelCodeGenerationOptions With {
                    .UseDataAnnotations = useDataAnnotations
                },
                Sub(code)
                    Assert.DoesNotContain(".HasColumnOrder(1)", code.ContextFile.Code)
                    Assert.DoesNotContain("[Column(Order = 1)]", code.AdditionalFiles(0).Code)
                End Sub,
                Sub(Model)
                    Dim entity = Model.FindEntityType("TestNamespace.Entity")
                    Assert.Null(entity.GetProperty("Property").GetColumnOrder())
                End Sub)
        End Sub

        Protected Overrides Sub AddModelServices(services As IServiceCollection)
            services.Replace(ServiceDescriptor.Singleton(Of IRelationalAnnotationProvider, TestModelAnnotationProvider)())
        End Sub

        Protected Overrides Sub AddScaffoldingServices(services As IServiceCollection)
            services.Replace(ServiceDescriptor.Singleton(Of IAnnotationCodeGenerator, TestModelAnnotationCodeGenerator)())
        End Sub

        Private Class TestModelAnnotationProvider
            Inherits SqlServerAnnotationProvider

            Public Sub New(dependencies As RelationalAnnotationProviderDependencies)
                MyBase.New(dependencies)
            End Sub

            Public Overrides Iterator Function [For](database As IRelationalModel, designTime As Boolean) As IEnumerable(Of IAnnotation)
                For Each annotation In MyBase.For(database, designTime)
                    Yield annotation
                Next

                If TypeOf database("Test:TestModelAnnotation") Is String Then
                    Dim annotationValue = database.Item("Test:TestModelAnnotation")
                    Yield New Annotation("Test:TestModelAnnotation", annotationValue)
                End If
            End Function
        End Class

        Private Class TestModelAnnotationCodeGenerator
            Inherits SqlServerAnnotationCodeGenerator

            Private Shared ReadOnly _testFluentApiCallMethodInfo As MethodInfo =
                GetType(CustomTestNamespace.TestModelBuilderExtensions).GetRuntimeMethod(
                    NameOf(CustomTestNamespace.TestModelBuilderExtensions.TestFluentApiCall), {GetType(ModelBuilder)})

            Public Sub New(dependencies As AnnotationCodeGeneratorDependencies)
                MyBase.New(dependencies)
            End Sub

            Protected Overrides Function GenerateFluentApi(model As IModel, annotation As IAnnotation) As MethodCallCodeFragment
                Select Case annotation.Name
                    Case "Test:TestModelAnnotation" : Return New MethodCallCodeFragment(_testFluentApiCallMethodInfo)
                    Case Else : Return MyBase.GenerateFluentApi(model, annotation)
                End Select
            End Function
        End Class

        Private Class TestCodeGeneratorPlugin
            Inherits ProviderCodeGeneratorPlugin

            Private Shared ReadOnly _setProviderOptionMethodInfo As MethodInfo =
                GetType(TestCodeGeneratorPlugin).
                    GetRuntimeMethod(NameOf(SetProviderOption), {GetType(SqlServerDbContextOptionsBuilder)})

            Private Shared ReadOnly _setContextOptionMethodInfo As MethodInfo =
                GetType(TestCodeGeneratorPlugin).
                    GetRuntimeMethod(NameOf(SetContextOption), {GetType(DbContextOptionsBuilder)})

            Public Overrides Function GenerateProviderOptions() As MethodCallCodeFragment
                Return New MethodCallCodeFragment(_setProviderOptionMethodInfo)
            End Function

            Public Overrides Function GenerateContextOptions() As MethodCallCodeFragment
                Return New MethodCallCodeFragment(_setContextOptionMethodInfo)
            End Function

            Public Shared Function SetProviderOption(optionsBuilder As SqlServerDbContextOptionsBuilder) As SqlServerDbContextOptionsBuilder
                Throw New NotSupportedException()
            End Function

            Public Shared Function SetContextOption(optionsBuilder As DbContextOptionsBuilder) As SqlServerDbContextOptionsBuilder
                Throw New NotSupportedException()
            End Function
        End Class

    End Class

End Namespace

Namespace Global.CustomTestNamespace
    Friend Module TestModelBuilderExtensions
        Public Function TestFluentApiCall(modelBuilder As ModelBuilder) As ModelBuilder
            modelBuilder.Model.SetAnnotation("Test:TestModelAnnotation", "foo")
            Return modelBuilder
        End Function
    End Module
End Namespace
