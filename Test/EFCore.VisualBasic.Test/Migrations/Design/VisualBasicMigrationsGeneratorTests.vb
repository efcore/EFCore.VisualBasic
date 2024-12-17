Imports System.Linq.Expressions
Imports System.Reflection
Imports System.Text.RegularExpressions
Imports EntityFrameworkCore.VisualBasic.Design
Imports EntityFrameworkCore.VisualBasic.Design.Internal
Imports EntityFrameworkCore.VisualBasic.TestUtilities
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.ChangeTracking
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Design.Internal
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Migrations
Imports Microsoft.EntityFrameworkCore.Migrations.Design
Imports Microsoft.EntityFrameworkCore.Migrations.Internal
Imports Microsoft.EntityFrameworkCore.Migrations.Operations
Imports Microsoft.EntityFrameworkCore.SqlServer.Design.Internal
Imports Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal
Imports Microsoft.EntityFrameworkCore.Storage
Imports Microsoft.EntityFrameworkCore.Storage.ValueConversion
Imports Microsoft.EntityFrameworkCore.TestUtilities
Imports Microsoft.Extensions.DependencyInjection
Imports Xunit

Namespace Migrations.Design

    Partial Public Class VisualBasicMigrationsGeneratorTests

        Private Shared ReadOnly _nl As String = Environment.NewLine
        Private Shared ReadOnly _toTable As String = _nl & "entityTypeBuilder.ToTable(""WithAnnotations"")"
        Private Shared ReadOnly _toNullTable As String = _nl & "entityTypeBuilder.ToTable(DirectCast(Nothing, String))"

        <ConditionalFact>
        Sub FileExtension_works()
            Dim generator = CreateMigrationsCodeGenerator()
            Assert.Equal(".vb", generator.FileExtension)
        End Sub

        <ConditionalFact>
        Sub Language_works()
            Dim generator = CreateMigrationsCodeGenerator()
            Assert.Equal("VB", generator.Language)
        End Sub

        <ConditionalFact>
        Public Sub Test_new_annotations_handled_for_entity_types()

            ' Only add the annotation here if it will never be present on IEntityType
#Disable Warning BC40000 ' Type or member is obsolete
            Dim notForEntityType As New HashSet(Of String) From {
                CoreAnnotationNames.MaxLength,
                CoreAnnotationNames.Precision,
                CoreAnnotationNames.Scale,
                CoreAnnotationNames.Unicode,
                CoreAnnotationNames.ProductVersion,
                CoreAnnotationNames.ValueGeneratorFactory,
                CoreAnnotationNames.ValueGeneratorFactoryType,
                CoreAnnotationNames.ValueConverter,
                CoreAnnotationNames.ValueConverterType,
                CoreAnnotationNames.ValueComparer,
                CoreAnnotationNames.ValueComparerType,
                CoreAnnotationNames.BeforeSaveBehavior,
                CoreAnnotationNames.AfterSaveBehavior,
                CoreAnnotationNames.ProviderClrType,
                CoreAnnotationNames.EagerLoaded,
                CoreAnnotationNames.LazyLoadingEnabled,
                CoreAnnotationNames.DuplicateServiceProperties,
                CoreAnnotationNames.AdHocModel,
                RelationalAnnotationNames.ColumnName,
                RelationalAnnotationNames.ColumnOrder,
                RelationalAnnotationNames.ColumnType,
                RelationalAnnotationNames.TableColumnMappings,
                RelationalAnnotationNames.ViewColumnMappings,
                RelationalAnnotationNames.SqlQueryColumnMappings,
                RelationalAnnotationNames.FunctionColumnMappings,
                RelationalAnnotationNames.InsertStoredProcedureParameterMappings,
                RelationalAnnotationNames.InsertStoredProcedureResultColumnMappings,
                RelationalAnnotationNames.DeleteStoredProcedureParameterMappings,
                RelationalAnnotationNames.UpdateStoredProcedureParameterMappings,
                RelationalAnnotationNames.UpdateStoredProcedureResultColumnMappings,
                RelationalAnnotationNames.DefaultColumnMappings,
                RelationalAnnotationNames.TableMappings,
                RelationalAnnotationNames.ViewMappings,
                RelationalAnnotationNames.FunctionMappings,
                RelationalAnnotationNames.InsertStoredProcedureMappings,
                RelationalAnnotationNames.DeleteStoredProcedureMappings,
                RelationalAnnotationNames.UpdateStoredProcedureMappings,
                RelationalAnnotationNames.SqlQueryMappings,
                RelationalAnnotationNames.DefaultMappings,
                RelationalAnnotationNames.ForeignKeyMappings,
                RelationalAnnotationNames.TableIndexMappings,
                RelationalAnnotationNames.UniqueConstraintMappings,
                RelationalAnnotationNames.RelationalOverrides,
                RelationalAnnotationNames.DefaultValueSql,
                RelationalAnnotationNames.ComputedColumnSql,
                RelationalAnnotationNames.DefaultValue,
                RelationalAnnotationNames.Name,
                RelationalAnnotationNames.SequencePrefix,
                RelationalAnnotationNames.Sequences,
                RelationalAnnotationNames.CheckConstraints,
                RelationalAnnotationNames.DefaultSchema,
                RelationalAnnotationNames.Filter,
                RelationalAnnotationNames.DbFunctions,
                RelationalAnnotationNames.MaxIdentifierLength,
                RelationalAnnotationNames.IsFixedLength,
                RelationalAnnotationNames.Collation,
                RelationalAnnotationNames.IsStored,
                RelationalAnnotationNames.TpcMappingStrategy,
                RelationalAnnotationNames.TphMappingStrategy,
                RelationalAnnotationNames.TptMappingStrategy,
                RelationalAnnotationNames.RelationalModel,
                RelationalAnnotationNames.RelationalModelFactory,
                RelationalAnnotationNames.ModelDependencies,
                RelationalAnnotationNames.FieldValueGetter,
                RelationalAnnotationNames.JsonPropertyName,
                RelationalAnnotationNames.ContainerColumnName, ' Appears On entity type but requires specific model (i.e. owned types that can map To json, otherwise validation throws)
                RelationalAnnotationNames.ContainerColumnType,
                RelationalAnnotationNames.ContainerColumnTypeMapping,
                RelationalAnnotationNames.StoreType}
#Enable Warning BC40000 ' Type or member is obsolete

            ' Add a line here if the code generator is supposed to handle this annotation
            ' Note that other tests should be added to check code is generated correctly

#Disable Warning BC40008 ' Type or member is obsolete
            Dim forEntityType = New Dictionary(Of String, (Object, String)) From {
                {
                    RelationalAnnotationNames.TableName, ("MyTable",
                        _nl &
                        "entityTypeBuilder." &
                        NameOf(RelationalEntityTypeBuilderExtensions.ToTable) &
                        "(""MyTable"")")
                },
                {
                    RelationalAnnotationNames.Schema, ("MySchema",
                        _nl &
                        "entityTypeBuilder." &
                        NameOf(RelationalEntityTypeBuilderExtensions.ToTable) &
                        "(""WithAnnotations"", ""MySchema"")")
                },
                {
                    RelationalAnnotationNames.MappingStrategy, (RelationalAnnotationNames.TphMappingStrategy,
                        _toTable &
                        _nl &
                        _nl &
                        "entityTypeBuilder.UseTphMappingStrategy()")
                },
                {
                    CoreAnnotationNames.DiscriminatorProperty, ("Id",
                        _toTable &
                        _nl &
                        _nl &
                        "entityTypeBuilder.HasDiscriminator" &
                        "(Of Integer)(""Id"")")
                },
                {
                    CoreAnnotationNames.DiscriminatorValue, ("MyDiscriminatorValue",
                        _toTable)
                },
                {
                    RelationalAnnotationNames.Comment, ("My Comment",
                        _nl &
                        "entityTypeBuilder.ToTable(""WithAnnotations""," & _nl &
                        "    Sub(t)" & _nl &
                        "        t.HasComment(""My Comment"")" & _nl &
                        "    End Sub)")
                },
                {
                    CoreAnnotationNames.DefiningQuery,
                    (Expression.Lambda(Expression.Constant(Nothing)), _toNullTable)
                },
                {
                    RelationalAnnotationNames.ViewName, ("MyView",
                        _toNullTable &
                        _nl &
                        _nl &
                        "entityTypeBuilder." &
                        NameOf(RelationalEntityTypeBuilderExtensions.ToView) &
                        "(""MyView"")")
                },
                {
                    RelationalAnnotationNames.FunctionName, (Nothing,
                        _toNullTable &
                        _nl &
                        _nl &
                        "entityTypeBuilder" &
                        $".{NameOf(RelationalEntityTypeBuilderExtensions.ToFunction)}(Nothing)")
                },
                {
                    RelationalAnnotationNames.SqlQuery, (Nothing,
                        _toNullTable &
                        _nl &
                        _nl &
                        "entityTypeBuilder" &
                        $".{NameOf(RelationalEntityTypeBuilderExtensions.ToSqlQuery)}(Nothing)")
                }
            }
#Enable Warning BC40008 ' Type or member is obsolete

            MissingAnnotationCheck(Function(b) b.Entity(Of WithAnnotations)().Metadata,
                                   notForEntityType, forEntityType,
                                   Function(a) _toTable,
                                   Sub(g, m, b) g.TestGenerateEntityTypeAnnotations("entityTypeBuilder", CType(m, IEntityType), b))
        End Sub

        <ConditionalFact>
        Public Sub Test_new_annotations_handled_for_properties()

            ' Only add the annotation here if it will never be present on IProperty

#Disable Warning BC40000, BC40008 ' Type or member is obsolete
            Dim notForProperty As New HashSet(Of String) From {
                CoreAnnotationNames.ProductVersion,
                CoreAnnotationNames.NavigationAccessMode,
                CoreAnnotationNames.EagerLoaded,
                CoreAnnotationNames.LazyLoadingEnabled,
                CoreAnnotationNames.QueryFilter,
                CoreAnnotationNames.DefiningQuery,
                CoreAnnotationNames.DiscriminatorProperty,
                CoreAnnotationNames.DiscriminatorValue,
                CoreAnnotationNames.InverseNavigations,
                CoreAnnotationNames.InverseNavigationsNoAttribute,
                CoreAnnotationNames.NavigationCandidates,
                CoreAnnotationNames.NavigationCandidatesNoAttribute,
                CoreAnnotationNames.AmbiguousNavigations,
                CoreAnnotationNames.DuplicateServiceProperties,
                CoreAnnotationNames.AdHocModel,
                RelationalAnnotationNames.TableName,
                RelationalAnnotationNames.IsTableExcludedFromMigrations,
                RelationalAnnotationNames.ViewName,
                RelationalAnnotationNames.Schema,
                RelationalAnnotationNames.ViewSchema,
                RelationalAnnotationNames.ViewDefinitionSql,
                RelationalAnnotationNames.FunctionName,
                RelationalAnnotationNames.SqlQuery,
                RelationalAnnotationNames.DefaultSchema,
                RelationalAnnotationNames.DefaultMappings,
                RelationalAnnotationNames.TableColumnMappings,
                RelationalAnnotationNames.ViewColumnMappings,
                RelationalAnnotationNames.SqlQueryColumnMappings,
                RelationalAnnotationNames.FunctionColumnMappings,
                RelationalAnnotationNames.InsertStoredProcedureParameterMappings,
                RelationalAnnotationNames.InsertStoredProcedureResultColumnMappings,
                RelationalAnnotationNames.DeleteStoredProcedureParameterMappings,
                RelationalAnnotationNames.UpdateStoredProcedureParameterMappings,
                RelationalAnnotationNames.UpdateStoredProcedureResultColumnMappings,
                RelationalAnnotationNames.DefaultColumnMappings,
                RelationalAnnotationNames.TableMappings,
                RelationalAnnotationNames.ViewMappings,
                RelationalAnnotationNames.FunctionMappings,
                RelationalAnnotationNames.InsertStoredProcedureMappings,
                RelationalAnnotationNames.DeleteStoredProcedureMappings,
                RelationalAnnotationNames.UpdateStoredProcedureMappings,
                RelationalAnnotationNames.SqlQueryMappings,
                RelationalAnnotationNames.ForeignKeyMappings,
                RelationalAnnotationNames.TableIndexMappings,
                RelationalAnnotationNames.UniqueConstraintMappings,
                RelationalAnnotationNames.MappingFragments,
                RelationalAnnotationNames.Name,
                RelationalAnnotationNames.Sequences,
                RelationalAnnotationNames.SequencePrefix,
                RelationalAnnotationNames.CheckConstraints,
                RelationalAnnotationNames.Filter,
                RelationalAnnotationNames.DbFunctions,
                RelationalAnnotationNames.MaxIdentifierLength,
                RelationalAnnotationNames.MappingStrategy,
                RelationalAnnotationNames.TpcMappingStrategy,
                RelationalAnnotationNames.TphMappingStrategy,
                RelationalAnnotationNames.TptMappingStrategy,
                RelationalAnnotationNames.RelationalModel,
                RelationalAnnotationNames.RelationalModelFactory,
                RelationalAnnotationNames.ModelDependencies,
                RelationalAnnotationNames.FieldValueGetter,
                RelationalAnnotationNames.ContainerColumnName,
                RelationalAnnotationNames.ContainerColumnType,
                RelationalAnnotationNames.ContainerColumnTypeMapping,
                RelationalAnnotationNames.JsonPropertyName,
                RelationalAnnotationNames.StoreType}
#Enable Warning BC40000, BC40008 ' Type or member is obsolete

            Dim columnMapping = $".{_nl}{NameOf(RelationalPropertyBuilderExtensions.HasColumnType)}(""default_int_mapping"")"

            ' Add a line here if the code generator is supposed to handle this annotation
            ' Note that other tests should be added to check code is generated correctly

            Dim forProperty As New Dictionary(Of String, (Object, String)) From {
                {
                    CoreAnnotationNames.MaxLength,
                    (256, $".{_nl}{NameOf(PropertyBuilder.HasMaxLength)}(256){columnMapping}")
                },
                {
                    CoreAnnotationNames.Precision,
                    (4, $".{_nl}{NameOf(PropertyBuilder.HasPrecision)}(4){columnMapping}")
                },
                {
                    CoreAnnotationNames.Scale,
                    (Nothing, $"{columnMapping}")
                },
                {
                    CoreAnnotationNames.Unicode,
                    (False, $".{_nl}{NameOf(PropertyBuilder.IsUnicode)}(False){columnMapping}")
                },
                {
                    CoreAnnotationNames.ValueConverter,
                    (New ValueConverter(Of Integer, Long)(Function(v) v, Function(v) CInt(v)), $".{_nl}{NameOf(RelationalPropertyBuilderExtensions.HasColumnType)}(""default_long_mapping"")")
                },
                {
                    CoreAnnotationNames.ProviderClrType,
                    (GetType(Long), $".{_nl}{NameOf(RelationalPropertyBuilderExtensions.HasColumnType)}(""default_long_mapping"")")
                },
                {
                    RelationalAnnotationNames.ColumnName,
                    ("MyColumn", $"{columnMapping}.{_nl}{NameOf(RelationalPropertyBuilderExtensions.HasColumnName)}(""MyColumn"")")
                },
                {
                    RelationalAnnotationNames.ColumnOrder,
                    (1, $"{columnMapping}.{_nl}{NameOf(RelationalPropertyBuilderExtensions.HasColumnOrder)}(1)")
                },
                {
                    RelationalAnnotationNames.ColumnType,
                    ("int", $".{_nl}{NameOf(RelationalPropertyBuilderExtensions.HasColumnType)}(""int"")")
                },
                {
                    RelationalAnnotationNames.DefaultValueSql,
                    ("some SQL", $"{columnMapping}.{_nl}{NameOf(RelationalPropertyBuilderExtensions.HasDefaultValueSql)}(""some SQL"")")
                },
                {
                    RelationalAnnotationNames.ComputedColumnSql,
                    ("some SQL", $"{columnMapping}.{_nl}{NameOf(RelationalPropertyBuilderExtensions.HasComputedColumnSql)}(""some SQL"")")
                },
                {
                    RelationalAnnotationNames.DefaultValue,
                    ("1", $"{columnMapping}.{_nl}{NameOf(RelationalPropertyBuilderExtensions.HasDefaultValue)}(""1"")")
                },
                {
                    RelationalAnnotationNames.IsFixedLength,
                    (True, $"{columnMapping}.{_nl}{NameOf(RelationalPropertyBuilderExtensions.IsFixedLength)}()")
                },
                {
                    RelationalAnnotationNames.Comment,
                    ("My Comment", $"{columnMapping}.{_nl}{NameOf(RelationalPropertyBuilderExtensions.HasComment)}(""My Comment"")")
                },
                {
                    RelationalAnnotationNames.Collation,
                    ("Some Collation", $"{columnMapping}.{_nl}{NameOf(RelationalPropertyBuilderExtensions.UseCollation)}(""Some Collation"")")
                },
                {
                    RelationalAnnotationNames.IsStored,
                    (Nothing, $"{columnMapping}.{_nl}HasAnnotation(""{RelationalAnnotationNames.IsStored}"", Nothing)")
                }
            }

            MissingAnnotationCheck(Function(b) b.Entity(Of WithAnnotations)().
                                                 Property(Function(e) e.Id).
                                                 Metadata,
                                   notForProperty,
                                   forProperty,
                                   Function(a) columnMapping,
                                   Sub(g, m, b) g.TestGeneratePropertyAnnotations("propertyBuilder", CType(m, IProperty), b))
        End Sub

        Private Shared Sub MissingAnnotationCheck(
            createMetadataItem As Func(Of ModelBuilder, IMutableAnnotatable),
            invalidAnnotations As HashSet(Of String),
            validAnnotations As Dictionary(Of String, (Value As Object, Expected As String)),
            generationDefault As Func(Of String, String),
            test As Action(Of TestVisualBasicSnapshotGenerator, IMutableAnnotatable, IndentedStringBuilder))

            Dim sqlServerTypeMappingSource As New SqlServerTypeMappingSource(
                TestServiceFactory.Instance.Create(Of TypeMappingSourceDependencies)(),
                TestServiceFactory.Instance.Create(Of RelationalTypeMappingSourceDependencies)())

            Dim sqlServerAnnotationCodeGenerator As New SqlServerAnnotationCodeGenerator(
                New AnnotationCodeGeneratorDependencies(sqlServerTypeMappingSource))

            Dim codeHelper As New VisualBasicHelper(sqlServerTypeMappingSource)

            Dim generator As New TestVisualBasicSnapshotGenerator(
                codeHelper,
                sqlServerTypeMappingSource,
                sqlServerAnnotationCodeGenerator)

            Dim coreAnnotations = GetType(CoreAnnotationNames).GetFields().
                                    Where(Function(f) f.FieldType = GetType(String)).ToList()

            For Each field In coreAnnotations
                Dim annotationName As String = CStr(field.GetValue(Nothing))

                Assert.True(CoreAnnotationNames.AllNames.Contains(annotationName),
                              NameOf(CoreAnnotationNames) & "." & NameOf(CoreAnnotationNames.AllNames) & " doesn't contain " & annotationName)
            Next

            Dim relationalAnnotations =
                GetType(RelationalAnnotationNames).
                    GetFields().
                    Where(Function(f) f.FieldType = GetType(String) AndAlso f.Name <> "Prefix").ToList()

            For Each field In relationalAnnotations
                Dim annotationName = CStr(field.GetValue(Nothing))

                If field.Name <> NameOf(RelationalAnnotationNames.TpcMappingStrategy) AndAlso
                   field.Name <> NameOf(RelationalAnnotationNames.TptMappingStrategy) AndAlso
                   field.Name <> NameOf(RelationalAnnotationNames.TphMappingStrategy) Then

                    Assert.True(
                        RelationalAnnotationNames.AllNames.Contains(annotationName),
                        NameOf(RelationalAnnotationNames) &
                            "." &
                            NameOf(RelationalAnnotationNames.AllNames) &
                            " doesn't contain " &
                            annotationName)
                End If
            Next

            For Each field In coreAnnotations.Concat(relationalAnnotations)
                Dim annotationName = CStr(field.GetValue(Nothing))

                If Not invalidAnnotations.Contains(annotationName) Then
                    Dim modelBuilder = FakeRelationalTestHelpers.Instance.CreateConventionBuilder()
                    Dim metadataItem = createMetadataItem(modelBuilder)
                    metadataItem.SetAnnotation(annotationName, If(validAnnotations.ContainsKey(annotationName),
                                                                    validAnnotations(annotationName).Value,
                                                                    Nothing))

                    modelBuilder.FinalizeModel(designTime:=True, skipValidation:=True)

                    Dim sb As New IndentedStringBuilder

                    Try
                        ' Generator should not throw--either update above, or add to ignored list in generator
                        test(generator, metadataItem, sb)
                    Catch e As Exception
                        Assert.Fail($"Annotation '{annotationName}' was not handled by the code generator: {e.Message}")
                    End Try

                    Try

                        Dim expected = If(validAnnotations.ContainsKey(annotationName),
                                            validAnnotations(annotationName).Expected,
                                            generationDefault(annotationName))

                        Assert.Equal(If(String.IsNullOrEmpty(expected), expected, $"{expected}{_nl}"),
                                     sb.ToString())

                    Catch e As Exception
                        Throw New Exception(annotationName, e)
                    End Try
                End If
            Next
        End Sub

        Private Class TestVisualBasicSnapshotGenerator
            Inherits VisualBasicSnapshotGenerator

            Public Sub New(vbHelper As IVisualBasicHelper,
                           relationalTypeMappingSource As IRelationalTypeMappingSource,
                           annotationCodeGenerator As IAnnotationCodeGenerator)

                MyBase.New(annotationCodeGenerator, relationalTypeMappingSource, vbHelper)
            End Sub

            Public Overridable Sub TestGenerateEntityTypeAnnotations(builderName As String, entityType As IEntityType, stringBuilder As IndentedStringBuilder)
                GenerateEntityTypeAnnotations(builderName, entityType, stringBuilder)
            End Sub

            Public Overridable Sub TestGeneratePropertyAnnotations(builderName As String,
                                                                   prop As IProperty,
                                                                   stringBuilder As IndentedStringBuilder)
                GeneratePropertyAnnotations("propertyBuilder", prop, stringBuilder)
            End Sub

            Public Overridable Sub TestGenerateSequence(builderName As String, sequence As ISequence, stringBuilder As IndentedStringBuilder)
                GenerateSequence(builderName, sequence, stringBuilder)
            End Sub

            Public Overridable Sub TestGenerateIndexes(builderName As String, indexes As IEnumerable(Of IIndex), stringBuilder As IndentedStringBuilder)
                GenerateIndexes(builderName, indexes, stringBuilder)
            End Sub
        End Class

        Private Class WithAnnotations
            Public Property Id As Integer
        End Class

        Private Class Derived
            Inherits WithAnnotations
        End Class

        <ConditionalFact>
        Public Sub Snapshot_with_enum_discriminator_uses_converted_values()

            Dim sqlServerTypeMappingSource As New SqlServerTypeMappingSource(
                TestServiceFactory.Instance.Create(Of TypeMappingSourceDependencies)(),
                TestServiceFactory.Instance.Create(Of RelationalTypeMappingSourceDependencies)())

            Dim codeHelper As New VisualBasicHelper(sqlServerTypeMappingSource)

            Dim sqlServerAnnotationCodeGenerator As New SqlServerAnnotationCodeGenerator(
                New AnnotationCodeGeneratorDependencies(sqlServerTypeMappingSource))

            Dim generator As New VisualBasicMigrationsGenerator(
                New MigrationsCodeGeneratorDependencies(
                    sqlServerTypeMappingSource,
                    sqlServerAnnotationCodeGenerator),
                codeHelper)

            Dim modelBuilder = FakeRelationalTestHelpers.Instance.CreateConventionBuilder()
            modelBuilder.Model.RemoveAnnotation(CoreAnnotationNames.ProductVersion)
            modelBuilder.Entity(Of WithAnnotations)(
                Sub(eb)
                    eb.HasDiscriminator(Of RawEnum)("EnumDiscriminator").
                        HasValue(RawEnum.A).
                        HasValue(Of Derived)(RawEnum.B)
                    eb.Property(Of RawEnum)("EnumDiscriminator").HasConversion(Of Integer)()
                End Sub)

            Dim finalizedModel = modelBuilder.FinalizeModel(designTime:=True)

            Dim modelSnapshotCode = generator.GenerateSnapshot("MyNamespace",
                                                                GetType(MyContext),
                                                                "MySnapshot",
                                                                finalizedModel)

            Dim snapshotModel = CompileModelSnapshot(modelSnapshotCode, "MyNamespace.MySnapshot").Model

            Assert.Equal(CInt(Fix(RawEnum.A)), snapshotModel.FindEntityType(GetType(WithAnnotations)).GetDiscriminatorValue())
            Assert.Equal(CInt(Fix(RawEnum.B)), snapshotModel.FindEntityType(GetType(Derived)).GetDiscriminatorValue())
        End Sub

        Private Shared Sub AssertConverter(valueConverter1 As ValueConverter, expected As String)

            Dim modelBuilder = FakeRelationalTestHelpers.Instance.CreateConventionBuilder()

            Dim prop = modelBuilder.Entity(Of WithAnnotations)().Property(Function(e) e.Id).Metadata
            prop.SetMaxLength(1000)
            prop.SetValueConverter(valueConverter1)

            modelBuilder.FinalizeModel()

            Dim sqlServerTypeMappingSource As New SqlServerTypeMappingSource(
               TestServiceFactory.Instance.Create(Of TypeMappingSourceDependencies)(),
               TestServiceFactory.Instance.Create(Of RelationalTypeMappingSourceDependencies)())

            Dim codeHelper As New VisualBasicHelper(sqlServerTypeMappingSource)

            Dim sqlServerAnnotationCodeGenerator1 As New SqlServerAnnotationCodeGenerator(
                New AnnotationCodeGeneratorDependencies(sqlServerTypeMappingSource))

            Dim generator As New TestVisualBasicSnapshotGenerator(codeHelper,
                                                                  sqlServerTypeMappingSource,
                                                                  sqlServerAnnotationCodeGenerator1)

            Dim sb As New IndentedStringBuilder

            generator.TestGeneratePropertyAnnotations("propertyBuilder", DirectCast(prop, IProperty), sb)

            Assert.Equal(expected & "." & _nl & "HasMaxLength(1000)", sb.ToString())
        End Sub

        <ConditionalFact>
        Public Sub Migrations_compile()

            Dim generator = CreateMigrationsCodeGenerator()

            Dim sqlOp = New SqlOperation With {.Sql = "-- TEST"}
            sqlOp("Some:EnumValue") = RegexOptions.Multiline

            Dim alterColumnOp As New AlterColumnOperation With {
                .Name = "C2",
                .Table = "T1",
                .ClrType = GetType(Database),
                .OldColumn = New AddColumnOperation With {.ClrType = GetType([Property])}
            }

            Dim AddColumnOperation As New AddColumnOperation With {
                .Name = "C3",
                .Table = "T1",
                .ClrType = GetType(PropertyEntry)
            }

            Dim InsertDataOperation As New InsertDataOperation With {
                .Table = "T1",
                .Columns = {"Id", "C2", "C3"},
                .Values = New Object(,) {{1, Nothing, -1}}
            }

            Dim migrationCode = generator.GenerateMigration("MyNamespace",
                                                            "MyMigration",
                                                            {
                                                                sqlOp,
                                                                alterColumnOp,
                                                                AddColumnOperation,
                                                                InsertDataOperation
                                                            },
                                                            Array.Empty(Of MigrationOperation)())

            Assert.Equal(
"Imports System.Text.RegularExpressions
Imports Microsoft.EntityFrameworkCore.ChangeTracking
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Migrations
Imports Microsoft.EntityFrameworkCore.Storage
Imports Microsoft.VisualBasic

Namespace Global.MyNamespace
    ''' <inheritdoc />
    Partial Public Class MyMigration
        Inherits Migration

        ''' <inheritdoc />
        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql(""-- TEST"").
                Annotation(""Some:EnumValue"", RegexOptions.Multiline)

            migrationBuilder.AlterColumn(Of Database)(
                name:=""C2"",
                table:=""T1"",
                nullable:=False,
                oldClrType:=GetType([Property]))

            migrationBuilder.AddColumn(Of PropertyEntry)(
                name:=""C3"",
                table:=""T1"",
                nullable:=False)

            migrationBuilder.InsertData(
                table:=""T1"",
                columns:={""Id"", ""C2"", ""C3""},
                values:=New Object() {1, Nothing, -1})
        End Sub

        ''' <inheritdoc />
        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)

        End Sub
    End Class
End Namespace
",
                    migrationCode,
                    ignoreLineEndingDifferences:=True)

            Dim modelBuilder = SqlServerTestHelpers.Instance.CreateConventionBuilder(configureConventions:=Sub(c) c.RemoveAllConventions())
            modelBuilder.HasAnnotation("Some:EnumValue", RegexOptions.Multiline)
            modelBuilder.HasAnnotation(RelationalAnnotationNames.DbFunctions, New Dictionary(Of String, IDbFunction)())
            modelBuilder.Entity("T1", Sub(eb)
                                          eb.Property(Of Integer)("Id")
                                          eb.Property(Of String)("C2").IsRequired()
                                          eb.Property(Of Integer)("C3")
                                          eb.HasKey("Id")
                                      End Sub)

            modelBuilder.HasAnnotation(CoreAnnotationNames.ProductVersion, Nothing)

            Dim finalizedModel = modelBuilder.FinalizeModel(designTime:=True)

            Dim migrationMetadataCode = generator.GenerateMetadata(
                    "MyNamespace",
                    GetType(MyContext),
                    "MyMigration",
                    "20150511161616_MyMigration",
                    finalizedModel)

            Assert.Equal(
"' <auto-generated />
Imports System.Text.RegularExpressions
Imports EntityFrameworkCore.VisualBasic.Migrations.Design
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Migrations
Imports Microsoft.EntityFrameworkCore.Storage.ValueConversion
Imports Microsoft.VisualBasic

Namespace Global.MyNamespace
    <DbContext(GetType(VisualBasicMigrationsGeneratorTests.MyContext))>
    <Migration(""20150511161616_MyMigration"")>
    Partial Class MyMigration
        ''' <inheritdoc />
        Protected Overrides Sub BuildTargetModel(modelBuilder As ModelBuilder)
            modelBuilder.HasAnnotation(""Some:EnumValue"", RegexOptions.Multiline)

            modelBuilder.Entity(""T1"",
                Sub(b)
                    b.Property(Of Integer)(""Id"").
                        HasColumnType(""int"")

                    b.Property(Of String)(""C2"").
                        IsRequired().
                        HasColumnType(""nvarchar(max)"")

                    b.Property(Of Integer)(""C3"").
                        HasColumnType(""int"")

                    b.HasKey(""Id"")

                    b.ToTable(""T1"")
                End Sub)
        End Sub
    End Class
End Namespace
",
            migrationMetadataCode,
            ignoreLineEndingDifferences:=True)

            Dim build = New BuildSource With {
                .Sources = New Dictionary(Of String, String) From {
                    {"Migration.vb", migrationCode},
                    {"MigrationSnapshot.vb", migrationMetadataCode}
                },
                .EmitDocumentationDiagnostics = True
            }

            With build.References
                .Add(BuildReference.ByName("Microsoft.EntityFrameworkCore"))
                .Add(BuildReference.ByName("Microsoft.EntityFrameworkCore.Relational"))
                .Add(BuildReference.ByName(GetType(VisualBasicMigrationsGeneratorTests).Assembly.GetName().Name))
            End With

            Dim asm = build.BuildInMemory()

            Dim migrationType = asm.GetType("MyNamespace.MyMigration", throwOnError:=True, ignoreCase:=False)

            Dim contextTypeAttribute = migrationType.GetTypeInfo().GetCustomAttribute(Of DbContextAttribute)
            Assert.NotNull(contextTypeAttribute)
            Assert.Equal(GetType(MyContext), contextTypeAttribute.ContextType)

            Dim Migration = CType(Activator.CreateInstance(migrationType), Migration)

            Assert.Equal("20150511161616_MyMigration", Migration.GetId())

            Assert.Equal(4, Migration.UpOperations.Count)
            Assert.Empty(Migration.DownOperations)
            Assert.Single(Migration.TargetModel.GetEntityTypes())
        End Sub

        Private Enum RawEnum
            A
            B
        End Enum

        Private Shared Function MyDbFunction() As Integer
            Throw New NotImplementedException
        End Function

        Private Class EntityWithConstructorBinding
            Public Sub New(id As Integer)
                Me.Id = id
            End Sub

            Public ReadOnly Property Id As Integer
        End Class

        Private Function CompileModelSnapshot(code As String, modelSnapshotTypeName As String) As ModelSnapshot

            Dim build As New BuildSource With {
                 .Sources = New Dictionary(Of String, String) From {{"Snapshot.vb", code}}
            }

            For Each buildReference In GetReferences()
                build.References.Add(buildReference)
            Next

            Dim assembly = build.BuildInMemory()

            Dim snapshotType = assembly.GetType(modelSnapshotTypeName, throwOnError:=True, ignoreCase:=False)

            Dim contextTypeAttribute = snapshotType.GetCustomAttribute(Of DbContextAttribute)()
            Assert.NotNull(contextTypeAttribute)
            Assert.Equal(GetType(MyContext), contextTypeAttribute.ContextType)

            Return CType(Activator.CreateInstance(snapshotType), ModelSnapshot)
        End Function

        Public Class MyContext
        End Class

        <ConditionalFact>
        Public Sub Namespaces_imported_for_insert_data()
            Dim generator = CreateMigrationsCodeGenerator()

            Dim migration = generator.GenerateMigration(
                "MyNamespace",
                "MyMigration",
                {New InsertDataOperation With {
                    .Table = "MyTable",
                    .Columns = {"Id", "MyColumn"},
                    .Values = New Object(,) {{1, Nothing}, {2, RegexOptions.Multiline}}}
                },
                Array.Empty(Of MigrationOperation)())

            Assert.Contains("Imports System.Text.RegularExpressions", migration)
        End Sub

        <ConditionalFact>
        Public Sub Namespaces_imported_for_update_data_Values()
            Dim generator = CreateMigrationsCodeGenerator()

            Dim migration = generator.GenerateMigration(
                "MyNamespace",
                "MyMigration",
                {New UpdateDataOperation With {
                    .Table = "MyTable",
                    .KeyColumns = {"Id"},
                    .KeyValues = New Object(,) {{1}},
                    .Columns = {"MyColumn"},
                    .Values = New Object(,) {{RegexOptions.Multiline}}}},
                Array.Empty(Of MigrationOperation)())

            Assert.Contains("Imports System.Text.RegularExpressions", migration)
        End Sub

        <ConditionalFact>
        Public Sub Namespaces_imported_for_update_data_KeyValues()
            Dim generator = CreateMigrationsCodeGenerator()

            Dim migration = generator.GenerateMigration(
                "MyNamespace",
                "MyMigration",
                {New UpdateDataOperation With {
                    .Table = "MyTable",
                    .KeyColumns = {"Id"},
                    .KeyValues = New Object(,) {{RegexOptions.Multiline}},
                    .Columns = {"MyColumn"},
                    .Values = New Object(,) {{1}}}
                },
                Array.Empty(Of MigrationOperation)())

            Assert.Contains("Imports System.Text.RegularExpressions", migration)
        End Sub

        <ConditionalFact>
        Public Sub Namespaces_imported_for_delete_data()
            Dim generator = CreateMigrationsCodeGenerator()

            Dim migration = generator.GenerateMigration(
                "MyNamespace",
                "MyMigration",
                {New DeleteDataOperation With {
                    .Table = "MyTable",
                    .KeyColumns = {"Id"},
                    .KeyValues = New Object(,) {{RegexOptions.Multiline}}}
                },
                Array.Empty(Of MigrationOperation)())

            Assert.Contains("Imports System.Text.RegularExpressions", migration)
        End Sub

        Private Shared Function CreateMigrationsCodeGenerator() As IMigrationsCodeGenerator

            Dim testAssembly As Assembly = GetType(VisualBasicMigrationsGeneratorTests).Assembly
            Dim reporter As New TestOperationReporter

            Dim services = New DesignTimeServicesBuilder(testAssembly, testAssembly, reporter, New String() {}).
                CreateServiceCollection(SqlServerTestHelpers.Instance.CreateContext())

            Dim vbServices = New EFCoreVisualBasicServices
            vbServices.ConfigureDesignTimeServices(services)

            Return services.
                BuildServiceProvider(validateScopes:=True).
                GetRequiredService(Of IMigrationsCodeGenerator)()
        End Function

        Private Shared Sub AssertContains(expected As String, actual As String)
            ' Normalize line endings to Environment.Newline
            expected = expected.Replace(vbCrLf, vbLf).
                                Replace(vbLf & vbCr, vbLf).
                                Replace(vbCr, vbLf).
                                Replace(vbLf, Environment.NewLine)

            Assert.Contains(expected, actual)
        End Sub
    End Class
End Namespace
