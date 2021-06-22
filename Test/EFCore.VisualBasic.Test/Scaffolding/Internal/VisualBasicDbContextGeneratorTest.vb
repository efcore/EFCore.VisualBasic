
Imports EntityFrameworkCore.VisualBasic.Design
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Scaffolding
Imports Microsoft.EntityFrameworkCore.SqlServer.Design.Internal
Imports Microsoft.Extensions.DependencyInjection
Imports Xunit

Namespace Scaffolding.Internal
    Public Class VisualBasicDbContextGeneratorTest

        <ConditionalFact>
        Public Sub Empty_model()

            Dim expectedCode As String =
$"Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata

Namespace TestNamespace
    Public Partial Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            If Not optionsBuilder.IsConfigured Then
                'TODO /!\ {DesignStrings.SensitiveInformationWarning}
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
"Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata

Namespace TestNamespace
    Public Partial Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

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
"Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata

Namespace TestNamespace
    Public Partial Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

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
            Dim services = New ServiceCollection
            services.AddEntityFrameworkDesignTimeServices()

            Dim SqlServerDesignTimeServices = New SqlServerDesignTimeServices
            SqlServerDesignTimeServices.ConfigureDesignTimeServices(services)

            services.AddSingleton(Of IProviderCodeGeneratorPlugin, TestCodeGeneratorPlugin)()

            Dim vbServices = New EFCoreVisualBasicServices
            vbServices.ConfigureDesignTimeServices(services)

            Dim generator = services _
                .BuildServiceProvider().GetRequiredService(Of IModelCodeGenerator)()

            Assert.StartsWith(
                CoreStrings.ArgumentPropertyNull(NameOf(ModelCodeGenerationOptions.ModelNamespace), "options"),
                Assert.Throws(Of ArgumentException)(
                    Function()
                        Return generator.GenerateModel(
                            New Model(),
                            New ModelCodeGenerationOptions With {
                                .ModelNamespace = Nothing,
                                .ContextName = "TestDbContext",
                                .ConnectionString = "Initial Catalog=TestDatabase"
                            })
                    End Function).Message)

            Assert.StartsWith(
                CoreStrings.ArgumentPropertyNull(NameOf(ModelCodeGenerationOptions.ContextName), "options"),
                Assert.Throws(Of ArgumentException)(
                    Function()
                        Return generator.GenerateModel(
                        New Model(),
                        New ModelCodeGenerationOptions With {
                            .ModelNamespace = "TestNamespace",
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
                            .ModelNamespace = "TestNamespace",
                            .ContextName = "TestDbContext",
                            .ConnectionString = Nothing
                        })
                    End Function).Message)
        End Sub

        <ConditionalFact>
        Public Sub Plugins_work()
            Dim services = New ServiceCollection
            services.AddEntityFrameworkDesignTimeServices()

            Dim SqlServerDesignTimeServices As New SqlServerDesignTimeServices
            SqlServerDesignTimeServices.ConfigureDesignTimeServices(services)

            services.AddSingleton(Of IProviderCodeGeneratorPlugin, TestCodeGeneratorPlugin)()

            Dim vbServices = New EFCoreVisualBasicServices
            vbServices.ConfigureDesignTimeServices(services)

            Dim generator = services.
                BuildServiceProvider().GetRequiredService(Of IModelCodeGenerator)()

            Dim scaffoldedModel = generator.GenerateModel(
                New Model(),
                New ModelCodeGenerationOptions With {
                    .SuppressConnectionStringWarning = True,
                    .ModelNamespace = "TestNamespace",
                    .ContextName = "TestDbContext",
                    .ConnectionString = "Initial Catalog=TestDatabase"
                })

            Assert.Contains(
                "optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"", Sub(x) x.SetProviderOption()).SetContextOption()",
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
                          x.HasComment("An entity comment")
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
                Sub(code) Assert.Contains("entity.ToView(""Vista"")", code.ContextFile.Code),
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
            Dim modelGenerationOptions As New ModelCodeGenerationOptions With
            {
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
                    Assert.Contains("Property(Function(e) e.NonUnicodeColumn).IsUnicode(false)", code.ContextFile.Code)
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
        Public Sub Sequence_works()
            Test(
                Sub(modelBuilder) modelBuilder.HasSequence(Of Integer)("OrderNumbers").
                                               StartsAt(250).
                                               IncrementsBy(5).
                                               HasMin(100).
                                               HasMax(1_000_000).
                                               IsCyclic(True),
                New ModelCodeGenerationOptions,
                Sub(code) Assert.Contains(
"modelBuilder.HasSequence(Of Integer)(""OrderNumbers"").
                StartsAt(250).
                IncrementsBy(5).
                HasMin(100).
                HasMax(1000000).
                IsCyclic()", code.ContextFile.Code),
                Sub(model)
                    Dim MySequence = model.FindSequence("OrderNumbers")
                    Assert.Equal(250, MySequence.StartValue)
                    Assert.Equal(5, MySequence.IncrementBy)
                    Assert.Equal(100, MySequence.MinValue)
                    Assert.Equal(1_000_000, MySequence.MaxValue)
                    Assert.True(MySequence.IsCyclic)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub Entity_with_indexes_and_use_data_annotations_false_always_generates_fluent_API()

            Dim expectedcode =
$"Imports Microsoft.EntityFrameworkCore
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
                'TODO /!\ {DesignStrings.SensitiveInformationWarning}
                optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
            End If
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)

            modelBuilder.Entity(Of EntityWithIndexes)(
                Sub(entity)
                    entity.HasIndex(Function(e) New With {{e.A, e.B}}, ""IndexOnAAndB"").
                        IsUnique()
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
                            x.HasIndex({"A", "B"}, "IndexOnAAndB") _
                                .IsUnique()
                            x.HasIndex({"B", "C"}, "IndexOnBAndC") _
                                .HasFilter("Filter SQL") _
                                .HasAnnotation("AnnotationName", "AnnotationValue")
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
$"Imports Microsoft.EntityFrameworkCore
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
                'TODO /!\ {DesignStrings.SensitiveInformationWarning}
                optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
            End If
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
                            x.HasIndex({"A", "B"}, "IndexOnAAndB") _
                                .IsUnique()
                            x.HasIndex({"B", "C"}, "IndexOnBAndC") _
                                .HasFilter("Filter SQL") _
                                .HasAnnotation("AnnotationName", "AnnotationValue")
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
        Public Sub Entity_lambda_uses_correct_identifiers()

            Dim expectedCode As String =
$"Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata

Namespace TestNamespace
    Public Partial Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property DependentEntity As DbSet(Of DependentEntity)
        Public Overridable Property PrincipalEntity As DbSet(Of PrincipalEntity)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            If Not optionsBuilder.IsConfigured Then
                'TODO /!\ {DesignStrings.SensitiveInformationWarning}
                optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
            End If
        End Sub

        Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)

            modelBuilder.Entity(Of DependentEntity)(
                Sub(entity)
                    entity.HasIndex(Function(e) e.DependentId, ""IX_DependentEntity_DependentId"").
                        IsUnique()
                    entity.Property(Function(e) e.Id).UseIdentityColumn()
                    entity.HasOne(Function(d) d.NavigationToPrincipal).
                        WithOne(Function(p) p.NavigationToDependent).
                        HasPrincipalKey(Of PrincipalEntity)(Function(e) e.PrincipalId).
                        HasForeignKey(Of DependentEntity)(Function(e) e.DependentId)
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
                                               b.HasOne("PrincipalEntity", "NavigationToPrincipal") _
                                                   .WithOne("NavigationToDependent") _
                                                   .HasForeignKey("DependentEntity", "DependentId") _
                                                   .HasPrincipalKey("PrincipalEntity", "PrincipalId")
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
$"Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Metadata

Namespace TestNamespace
    Public Partial Class TestDbContext
        Inherits DbContext

        Public Sub New()
        End Sub

        Public Sub New(options As DbContextOptions(Of TestDbContext))
            MyBase.New(options)
        End Sub

        Public Overridable Property Employee As DbSet(Of Employee)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            If Not optionsBuilder.IsConfigured Then
                'TODO /!\ {DesignStrings.SensitiveInformationWarning}
                optionsBuilder.UseSqlServer(""Initial Catalog=TestDatabase"")
            End If
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

        Private Class TestCodeGeneratorPlugin
            Inherits ProviderCodeGeneratorPlugin

            Public Overrides Function GenerateProviderOptions() As MethodCallCodeFragment
                Return New MethodCallCodeFragment("SetProviderOption")
            End Function
            Public Overrides Function GenerateContextOptions() As MethodCallCodeFragment
                Return New MethodCallCodeFragment("SetContextOption")
            End Function
        End Class

        Private Shared Sub AssertFileContents(
            expectedCode As String,
            file As ScaffoldedFile)

            Assert.Equal(expectedCode, file.Code, ignoreLineEndingDifferences:=True)
        End Sub

    End Class

End Namespace
