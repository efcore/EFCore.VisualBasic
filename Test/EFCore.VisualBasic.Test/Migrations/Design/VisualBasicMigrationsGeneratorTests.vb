Imports System.Linq.Expressions
Imports System.Reflection
Imports System.Text
Imports System.Text.RegularExpressions
Imports EntityFrameworkCore.VisualBasic.Design
Imports EntityFrameworkCore.VisualBasic.Design.Internal
Imports EntityFrameworkCore.VisualBasic.TestUtilities
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.ChangeTracking
Imports Microsoft.EntityFrameworkCore.Design
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
Imports Microsoft.EntityFrameworkCore.ValueGeneration
Imports Microsoft.Extensions.DependencyInjection
Imports Xunit


Namespace Migrations.Design

    Public Class VisualBasicMigrationsGeneratorTests

        Private Shared ReadOnly _nl As String = Environment.NewLine
        Private Shared ReadOnly _toTable As String = _nl & "modelBuilder.ToTable(""WithAnnotations"")" & _nl

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
                CoreAnnotationNames.OwnedTypes,
                CoreAnnotationNames.ValueConverter,
                CoreAnnotationNames.ValueComparer,
                CoreAnnotationNames.KeyValueComparer,
                CoreAnnotationNames.StructuralValueComparer,
                CoreAnnotationNames.BeforeSaveBehavior,
                CoreAnnotationNames.AfterSaveBehavior,
                CoreAnnotationNames.ProviderClrType,
                CoreAnnotationNames.EagerLoaded,
                CoreAnnotationNames.DuplicateServiceProperties,
                RelationalAnnotationNames.ColumnName,
                RelationalAnnotationNames.ColumnType,
                RelationalAnnotationNames.TableColumnMappings,
                RelationalAnnotationNames.ViewColumnMappings,
                RelationalAnnotationNames.SqlQueryColumnMappings,
                RelationalAnnotationNames.FunctionColumnMappings,
                RelationalAnnotationNames.RelationalOverrides,
                RelationalAnnotationNames.DefaultValueSql,
                RelationalAnnotationNames.ComputedColumnSql,
                RelationalAnnotationNames.DefaultValue,
                RelationalAnnotationNames.Name,
                RelationalAnnotationNames.Sequences,
                RelationalAnnotationNames.CheckConstraints,
                RelationalAnnotationNames.DefaultSchema,
                RelationalAnnotationNames.Filter,
                RelationalAnnotationNames.DbFunctions,
                RelationalAnnotationNames.MaxIdentifierLength,
                RelationalAnnotationNames.IsFixedLength,
                RelationalAnnotationNames.Collation}
#Enable Warning BC40000 ' Type or member is obsolete

            ' Add a line here if the code generator is supposed to handle this annotation
            ' Note that other tests should be added to check code is generated correctly

#Disable Warning BC40008 ' Type or member is obsolete
            Dim forEntityType = New Dictionary(Of String, (Object, String)) From {
                {
                    RelationalAnnotationNames.TableName, ("MyTable",
                        _nl &
                        "modelBuilder." &
                         NameOf(RelationalEntityTypeBuilderExtensions.ToTable) &
                         "(""MyTable"")" &
                         _nl)
                },
                {
                    RelationalAnnotationNames.Schema, ("MySchema",
                        _nl &
                        "modelBuilder." &
                        NameOf(RelationalEntityTypeBuilderExtensions.ToTable) &
                        "(""WithAnnotations"", ""MySchema"")" & _nl)
                },
                {
                    CoreAnnotationNames.DiscriminatorProperty, ("Id",
                        _toTable &
                         _nl &
                         "modelBuilder.HasDiscriminator" &
                         "(Of Integer)(""Id"")" &
                         _nl)
                },
                {
                    CoreAnnotationNames.DiscriminatorValue, ("MyDiscriminatorValue",
                        _toTable &
                        _nl &
                        "modelBuilder.HasDiscriminator" &
                        "()." &
                        NameOf(DiscriminatorBuilder.HasValue) &
                        "(""MyDiscriminatorValue"")" &
                        _nl)
                },
                {
                    RelationalAnnotationNames.Comment, ("My Comment",
                        _toTable &
                        _nl &
                        "modelBuilder" &
                        _nl &
                        "    .HasComment(""My Comment"")" &
                        _nl)
                },
                {
                    CoreAnnotationNames.DefiningQuery,
                    (Expression.Lambda(Expression.Constant(Nothing)), "")
                },
                {
                    RelationalAnnotationNames.ViewName, ("MyView",
                        _nl &
                        "modelBuilder." &
                        NameOf(RelationalEntityTypeBuilderExtensions.ToView) &
                        "(""MyView"")" &
                        _nl)
                },
                {
                    RelationalAnnotationNames.FunctionName,
                    (Nothing, "")
                },
                {
                    RelationalAnnotationNames.SqlQuery,
                    (Nothing, "")
                }
            }
#Enable Warning BC40008 ' Type or member is obsolete

            MissingAnnotationCheck(Function(b) b.Entity(Of WithAnnotations)().Metadata,
                                   notForEntityType, forEntityType,
                                   _toTable,
                                   Sub(g, m, b) g.TestGenerateEntityTypeAnnotations("modelBuilder", CType(m, IEntityType), b))

        End Sub

        <ConditionalFact>
        Public Sub Test_new_annotations_handled_for_properties()

            ' Only add the annotation here if it will never be present on IProperty

#Disable Warning BC40008 ' Type or member is obsolete
            Dim notForProperty As New HashSet(Of String) From {
                CoreAnnotationNames.ProductVersion,
                CoreAnnotationNames.OwnedTypes,
                CoreAnnotationNames.ConstructorBinding,
                CoreAnnotationNames.ServiceOnlyConstructorBinding,
                CoreAnnotationNames.NavigationAccessMode,
                CoreAnnotationNames.EagerLoaded,
                CoreAnnotationNames.QueryFilter,
                CoreAnnotationNames.DefiningQuery,
                CoreAnnotationNames.DiscriminatorProperty,
                CoreAnnotationNames.DiscriminatorValue,
                CoreAnnotationNames.InverseNavigations,
                CoreAnnotationNames.NavigationCandidates,
                CoreAnnotationNames.AmbiguousNavigations,
                CoreAnnotationNames.DuplicateServiceProperties,
                RelationalAnnotationNames.TableName,
                RelationalAnnotationNames.ViewName,
                RelationalAnnotationNames.Schema,
                RelationalAnnotationNames.ViewSchema,
                RelationalAnnotationNames.DefaultSchema,
                RelationalAnnotationNames.DefaultMappings,
                RelationalAnnotationNames.TableMappings,
                RelationalAnnotationNames.ViewMappings,
                RelationalAnnotationNames.SqlQueryMappings,
                RelationalAnnotationNames.Name,
                RelationalAnnotationNames.Sequences,
                RelationalAnnotationNames.CheckConstraints,
                RelationalAnnotationNames.Filter,
                RelationalAnnotationNames.DbFunctions,
                RelationalAnnotationNames.MaxIdentifierLength}
#Enable Warning BC40008 ' Type or member is obsolete

            Dim columnMapping As String =
                $".{_nl}{NameOf(RelationalPropertyBuilderExtensions.HasColumnType)}(""default_int_mapping"")"


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
                    ("MyColumn", $"{columnMapping} _{_nl}.{NameOf(RelationalPropertyBuilderExtensions.HasColumnName)}(""MyColumn"")")
                },
                {
                    RelationalAnnotationNames.ColumnType,
                    ("int", $".{_nl}{NameOf(RelationalPropertyBuilderExtensions.HasColumnType)}(""int"")")
                },
                {
                    RelationalAnnotationNames.DefaultValueSql,
                    ("some SQL", $"{columnMapping} _{_nl}.{NameOf(RelationalPropertyBuilderExtensions.HasDefaultValueSql)}(""some SQL"")")
                },
                {
                    RelationalAnnotationNames.ComputedColumnSql,
                    ("some SQL", $"{columnMapping} _{_nl}.{NameOf(RelationalPropertyBuilderExtensions.HasComputedColumnSql)}(""some SQL"")")
                },
                {
                    RelationalAnnotationNames.DefaultValue,
                    ("1", $"{columnMapping}.{_nl}{NameOf(RelationalPropertyBuilderExtensions.HasDefaultValue)}(""1"")")
                },
                {
                    RelationalAnnotationNames.IsFixedLength,
                    (True, $"{columnMapping} _{_nl}.{NameOf(RelationalPropertyBuilderExtensions.IsFixedLength)}(True)")
                },
                {
                    RelationalAnnotationNames.Comment,
                    ("My Comment", $"{columnMapping} _{_nl}.{NameOf(RelationalPropertyBuilderExtensions.HasComment)}(""My Comment"")")
                },
                {RelationalAnnotationNames.Collation,
                    ("Some Collation", $"{columnMapping} _{_nl}.{NameOf(RelationalPropertyBuilderExtensions.UseCollation)}(""Some Collation"")")
                }
            }

            MissingAnnotationCheck(Function(b) b.Entity(Of WithAnnotations)().
                                                 Property(Function(e) e.Id).
                                                 Metadata,
                                   notForProperty,
                                   forProperty,
                                   columnMapping,
                                   Sub(g, m, b) g.TestGeneratePropertyAnnotations(CType(m, IProperty), b))
        End Sub

        Private Shared Sub MissingAnnotationCheck(
            createMetadataItem As Func(Of ModelBuilder, IMutableAnnotatable),
            invalidAnnotations As HashSet(Of String),
            validAnnotations As Dictionary(Of String, (Value As Object, Expected As String)),
            generationDefault As String,
            test As Action(Of TestVisualBasicSnapshotGenerator, IMutableAnnotatable, IndentedStringBuilder))

            Dim sqlServerTypeMappingSource1 As New SqlServerTypeMappingSource(
                TestServiceFactory.Instance.Create(Of TypeMappingSourceDependencies)(),
                TestServiceFactory.Instance.Create(Of RelationalTypeMappingSourceDependencies)())

            Dim sqlServerAnnotationCodeGenerator1 As New SqlServerAnnotationCodeGenerator(
                New AnnotationCodeGeneratorDependencies(sqlServerTypeMappingSource1))

            Dim codeHelper As New VisualBasicHelper(sqlServerTypeMappingSource1)

            Dim generator As New TestVisualBasicSnapshotGenerator(
                codeHelper,
                sqlServerTypeMappingSource1,
                sqlServerAnnotationCodeGenerator1)

            Dim coreAnnotations = GetType(CoreAnnotationNames).GetFields().
                                    Where(Function(f) f.FieldType = GetType(String)).ToList()

            For Each field In coreAnnotations
                Dim annotationName As String = CStr(field.GetValue(Nothing))

                Assert.True(CoreAnnotationNames.AllNames.Contains(annotationName),
                              NameOf(CoreAnnotationNames) & "." & NameOf(CoreAnnotationNames.AllNames) & " doesn't contain " & annotationName)
            Next

            For Each field In coreAnnotations.Concat(GetType(RelationalAnnotationNames).
                                                     GetFields().
                                                     Where(Function(f) f.Name <> "Prefix"))

                Dim annotationName As String = CStr(field.GetValue(Nothing))

                If Not invalidAnnotations.Contains(annotationName) Then
                    Dim modelBuilder1 = RelationalTestHelpers.Instance.CreateConventionBuilder()
                    Dim metadataItem = createMetadataItem(modelBuilder1)
                    metadataItem.SetAnnotation(annotationName, If(validAnnotations.ContainsKey(annotationName), validAnnotations(annotationName).Value _
                , Nothing))

                    modelBuilder1.FinalizeModel()

                    Dim sb As New IndentedStringBuilder

                    Try
                        ' Generator should not throw--either update above, or add to ignored list in generator
                        test(generator, metadataItem, sb)
                    Catch e As Exception
                        Assert.False(True, $"Annotation '{annotationName}' was not handled by the code generator: {e.Message}")
                    End Try

                    Try
                        Assert.Equal(If(validAnnotations.ContainsKey(annotationName),
                                            validAnnotations(annotationName).Expected,
                                            generationDefault),
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

            Public Overridable Sub TestGeneratePropertyAnnotations(prop As IProperty, stringBuilder As IndentedStringBuilder)
                GeneratePropertyAnnotations(prop, stringBuilder)
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

            Dim sqlServerTypeMappingSource1 As New SqlServerTypeMappingSource(
                TestServiceFactory.Instance.Create(Of TypeMappingSourceDependencies)(),
                TestServiceFactory.Instance.Create(Of RelationalTypeMappingSourceDependencies)())

            Dim codeHelper As New VisualBasicHelper(sqlServerTypeMappingSource1)

            Dim sqlServerAnnotationCodeGenerator1 As New SqlServerAnnotationCodeGenerator(
                New AnnotationCodeGeneratorDependencies(sqlServerTypeMappingSource1))

            Dim generator As New VisualBasicMigrationsGenerator(
                New MigrationsCodeGeneratorDependencies(
                    sqlServerTypeMappingSource1,
                    sqlServerAnnotationCodeGenerator1),
                sqlServerAnnotationCodeGenerator1,
                sqlServerTypeMappingSource1,
                codeHelper)

            Dim modelBuilder = RelationalTestHelpers.Instance.CreateConventionBuilder()
            modelBuilder.Model.RemoveAnnotation(CoreAnnotationNames.ProductVersion)
            modelBuilder.Entity(Of WithAnnotations)(Sub(eb)
                                                        eb.HasDiscriminator(Of RawEnum)("EnumDiscriminator") _
                                                            .HasValue(RawEnum.A) _
                                                            .HasValue(Of Derived)(RawEnum.B)
                                                        eb.Property(Of RawEnum)("EnumDiscriminator").HasConversion(Of Integer)()
                                                    End Sub)

            modelBuilder.FinalizeModel()

            Dim modelSnapshotCode = generator.GenerateSnapshot("MyNamespace",
                                                                GetType(MyContext),
                                                                "MySnapshot",
                                                                modelBuilder.Model)

            Dim snapshotModel = CompileModelSnapshot(modelSnapshotCode, "MyNamespace.MySnapshot").Model

            Assert.Equal(CInt(Fix(RawEnum.A)), snapshotModel.FindEntityType(GetType(WithAnnotations)).GetDiscriminatorValue())
            Assert.Equal(CInt(Fix(RawEnum.B)), snapshotModel.FindEntityType(GetType(Derived)).GetDiscriminatorValue())
        End Sub

        Private Shared Sub AssertConverter(valueConverter1 As ValueConverter, expected As String)

            Dim modelBuilder = RelationalTestHelpers.Instance.CreateConventionBuilder()

            Dim prop = modelBuilder.Entity(Of WithAnnotations)().Property(Function(e) e.Id).Metadata
            prop.SetMaxLength(1000)
            prop.SetValueConverter(valueConverter1)

            modelBuilder.FinalizeModel()

            Dim sqlServerTypeMappingSource1 As New SqlServerTypeMappingSource(
               TestServiceFactory.Instance.Create(Of TypeMappingSourceDependencies)(),
               TestServiceFactory.Instance.Create(Of RelationalTypeMappingSourceDependencies)())

            Dim codeHelper As New VisualBasicHelper(sqlServerTypeMappingSource1)

            Dim sqlServerAnnotationCodeGenerator1 As New SqlServerAnnotationCodeGenerator(
                New AnnotationCodeGeneratorDependencies(sqlServerTypeMappingSource1))

            Dim generator As New TestVisualBasicSnapshotGenerator(codeHelper,
                                                                  sqlServerTypeMappingSource1,
                                                                  sqlServerAnnotationCodeGenerator1)

            Dim sb As New IndentedStringBuilder

            generator.TestGeneratePropertyAnnotations(prop, sb)

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
                                                            {sqlOp,
                                                                alterColumnOp,
                                                                AddColumnOperation,
                                                                InsertDataOperation
                                                            },
                                                            Array.Empty(Of MigrationOperation)())

            Assert.Equal(
"Imports System
Imports System.Collections.Generic
Imports System.Text.RegularExpressions
Imports Microsoft.EntityFrameworkCore.ChangeTracking
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Migrations
Imports Microsoft.EntityFrameworkCore.Storage

Namespace Global.MyNamespace
    Partial Public Class MyMigration
        Inherits Migration

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

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)

        End Sub
    End Class
End Namespace
",
                    migrationCode,
                    ignoreLineEndingDifferences:=True)

            Dim modelBuilder1 As New ModelBuilder
            modelBuilder1.HasAnnotation("Some:EnumValue", RegexOptions.Multiline)
            modelBuilder1.HasAnnotation(RelationalAnnotationNames.DbFunctions, New Object)
            modelBuilder1.Entity("T1", Sub(eb)
                                           eb.Property(Of Integer)("Id")
                                           eb.Property(Of String)("C2").IsRequired()
                                           eb.Property(Of Integer)("C3")
                                           eb.HasKey("Id")
                                       End Sub)

            Dim migrationMetadataCode = generator.GenerateMetadata(
                    "MyNamespace",
                    GetType(MyContext),
                    "MyMigration",
                    "20150511161616_MyMigration",
                    modelBuilder1.Model)

            Assert.Equal(
"' <auto-generated />
Imports System
Imports System.Text.RegularExpressions
Imports EntityFrameworkCore.VisualBasic.Migrations.Design
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Migrations

Namespace Global.MyNamespace
    <DbContext(GetType(VisualBasicMigrationsGeneratorTests.MyContext))>
    <Migration(""20150511161616_MyMigration"")>
    Partial Class MyMigration
        Protected Overrides Sub BuildTargetModel(modelBuilder As ModelBuilder)
            modelBuilder.
                HasAnnotation(""Some:EnumValue"", RegexOptions.Multiline)

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
                .References = {
                        BuildReference.ByName("netstandard"),
                        BuildReference.ByName("System.Runtime"),
                        BuildReference.ByName("EntityFrameworkCore.VisualBasic.Test"),
                        BuildReference.ByName("Microsoft.EntityFrameworkCore"),
                        BuildReference.ByName("Microsoft.EntityFrameworkCore.Relational"),
                        BuildReference.ByName("System.Text.RegularExpressions")
                },
                .Sources = {migrationCode, migrationMetadataCode}
            }

            Dim asm = build.BuildInMemory()

            Dim migrationType = asm.GetType("MyNamespace.MyMigration", throwOnError:=True, ignoreCase:=False)

            Dim contextTypeAttribute = migrationType.GetTypeInfo().GetCustomAttribute(Of DbContextAttribute)
            Assert.NotNull(contextTypeAttribute)
            Assert.Equal(GetType(MyContext), contextTypeAttribute.ContextType)

            Dim migr = CType(Activator.CreateInstance(migrationType), Migration)

            Assert.Equal("20150511161616_MyMigration", migr.GetId())
            Dim AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA = migr.TargetModel.GetEntityTypes()
            Assert.Equal(4, migr.UpOperations.Count)
            Assert.Empty(migr.DownOperations)
            Assert.Single(migr.TargetModel.GetEntityTypes())
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

        <ConditionalFact>
        Public Sub Snapshots_compile()
            Dim generator = CreateMigrationsCodeGenerator()

            Dim modelBuilder = RelationalTestHelpers.Instance.CreateConventionBuilder(skipValidation:=True)

            modelBuilder.Model.RemoveAnnotation(CoreAnnotationNames.ProductVersion)
            modelBuilder.Entity(Of EntityWithConstructorBinding)(
                    Sub(x)
                        x.Property(Function(e) e.Id)

                        x.Property(Of Guid)("PropertyWithValueGenerator").HasValueGenerator(Of GuidValueGenerator)()
                    End Sub)
            modelBuilder.HasDbFunction(Function() MyDbFunction())

            Dim model1 = modelBuilder.Model
            model1.Item("Some:EnumValue") = RegexOptions.Multiline

            Dim entityType = model1.AddEntityType("Cheese")
            Dim property1 = entityType.AddProperty("Pickle", GetType(StringBuilder))
            property1.SetValueConverter(New ValueConverter(Of StringBuilder, String)(
                                            Function(v) v.ToString(), Function(v) New StringBuilder(v), New ConverterMappingHints(size:=10)))

            Dim property2 = entityType.AddProperty("Ham", GetType(RawEnum))
            property2.SetValueConverter(New ValueConverter(Of RawEnum, String)(
                                            Function(v) v.ToString(),
                                            Function(v) DirectCast([Enum].Parse(GetType(RawEnum), v), RawEnum),
                                            New ConverterMappingHints(size:=10)))

            entityType.SetPrimaryKey(property2)

            modelBuilder.FinalizeModel()

            Dim modelSnapshotCode = generator.GenerateSnapshot(
                "MyNamespace",
                GetType(MyContext),
                "MySnapshot",
                model1)

            Assert.Equal(
"' <auto-generated />
Imports System
Imports System.Text.RegularExpressions
Imports EntityFrameworkCore.VisualBasic.Migrations.Design
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Migrations

Namespace Global.MyNamespace
    <DbContext(GetType(VisualBasicMigrationsGeneratorTests.MyContext))>
    Partial Class MySnapshot
        Inherits ModelSnapshot

        Protected Overrides Sub BuildModel(modelBuilder As ModelBuilder)
            modelBuilder.
                HasAnnotation(""Some:EnumValue"", RegexOptions.Multiline)

            modelBuilder.Entity(""Cheese"",
                Sub(b)
                    b.Property(Of String)(""Ham"").
                        HasColumnType(""just_string(10)"")

                    b.Property(Of String)(""Pickle"").
                        HasColumnType(""just_string(10)"")

                    b.HasKey(""Ham"")

                    b.ToTable(""Cheese"")
                End Sub)

            modelBuilder.Entity(""EntityFrameworkCore.VisualBasic.Migrations.Design.VisualBasicMigrationsGeneratorTests+EntityWithConstructorBinding"",
                Sub(b)
                    b.Property(Of Integer)(""Id"").
                        ValueGeneratedOnAdd().
                        HasColumnType(""default_int_mapping"")

                    b.Property(Of Guid)(""PropertyWithValueGenerator"").
                        HasColumnType(""default_guid_mapping"")

                    b.HasKey(""Id"")

                    b.ToTable(""EntityWithConstructorBinding"")
                End Sub)
        End Sub
    End Class
End Namespace
", modelSnapshotCode, ignoreLineEndingDifferences:=True)

            Dim snapshot = CompileModelSnapshot(modelSnapshotCode, "MyNamespace.MySnapshot")
            Assert.Equal(2, snapshot.Model.GetEntityTypes().Count())
        End Sub


        <ConditionalFact>
        Public Sub Snapshot_with_default_values_are_round_tripped()
            Dim generator = CreateMigrationsCodeGenerator()

            Dim modelBuilder = RelationalTestHelpers.Instance.CreateConventionBuilder()
            modelBuilder.Entity(Of EntityWithEveryPrimitive)(
            Sub(eb)
                eb.Property(Function(e) e.Boolean).HasDefaultValue(False)
                eb.Property(Function(e) e.Byte).HasDefaultValue(Byte.MinValue)
                eb.Property(Function(e) e.ByteArray).HasDefaultValue(New Byte() {0})
                eb.Property(Function(e) e.Char).HasDefaultValue("0"c)
                eb.Property(Function(e) e.DateTime).HasDefaultValue(DateTime.MinValue)
                eb.Property(Function(e) e.DateTimeOffset).HasDefaultValue(DateTimeOffset.MinValue)
                eb.Property(Function(e) e.Decimal).HasDefaultValue(Decimal.MinValue)
                eb.Property(Function(e) e.Double).HasDefaultValue(Double.MinValue) 'double.NegativeInfinity
                eb.Property(Function(e) e.Enum).HasDefaultValue(Enum1.Default)
                eb.Property(Function(e) e.NullableEnum).HasDefaultValue(Enum1.Default).HasConversion(Of String)()
                eb.Property(Function(e) e.Guid).HasDefaultValue(Guid.NewGuid())
                eb.Property(Function(e) e.Int16).HasDefaultValue(Short.MaxValue)
                eb.Property(Function(e) e.Int32).HasDefaultValue(Integer.MaxValue)
                eb.Property(Function(e) e.Int64).HasDefaultValue(Long.MaxValue)
                eb.Property(Function(e) e.Single).HasDefaultValue(Single.Epsilon)
                eb.Property(Function(e) e.SByte).HasDefaultValue(SByte.MinValue)
                eb.Property(Function(e) e.String).HasDefaultValue("""")
                eb.Property(Function(e) e.TimeSpan).HasDefaultValue(TimeSpan.MaxValue)
                eb.Property(Function(e) e.UInt16).HasDefaultValue(UShort.MinValue)
                eb.Property(Function(e) e.UInt32).HasDefaultValue(UInteger.MinValue)
                eb.Property(Function(e) e.UInt64).HasDefaultValue(ULong.MinValue)
                eb.Property(Function(e) e.NullableBoolean).HasDefaultValue(True)
                eb.Property(Function(e) e.NullableByte).HasDefaultValue(Byte.MaxValue)
                eb.Property(Function(e) e.NullableChar).HasDefaultValue("'"c)
                eb.Property(Function(e) e.NullableDateTime).HasDefaultValue(DateTime.MaxValue)
                eb.Property(Function(e) e.NullableDateTimeOffset).HasDefaultValue(DateTimeOffset.MaxValue)
                eb.Property(Function(e) e.NullableDecimal).HasDefaultValue(Decimal.MaxValue)
                eb.Property(Function(e) e.NullableDouble).HasDefaultValue(0.6822871999174)
                eb.Property(Function(e) e.NullableEnum).HasDefaultValue(Enum1.One Or Enum1.Two)
                eb.Property(Function(e) e.NullableStringEnum).HasDefaultValue(Enum1.One).HasConversion(Of String)()
                eb.Property(Function(e) e.NullableGuid).HasDefaultValue(New Guid)
                eb.Property(Function(e) e.NullableInt16).HasDefaultValue(Short.MinValue)
                eb.Property(Function(e) e.NullableInt32).HasDefaultValue(Integer.MinValue)
                eb.Property(Function(e) e.NullableInt64).HasDefaultValue(Long.MinValue)
                eb.Property(Function(e) e.NullableSingle).HasDefaultValue(0.3333333F)
                eb.Property(Function(e) e.NullableSByte).HasDefaultValue(SByte.MinValue)
                eb.Property(Function(e) e.NullableTimeSpan).HasDefaultValue(TimeSpan.MinValue.Add(New TimeSpan))
                eb.Property(Function(e) e.NullableUInt16).HasDefaultValue(UShort.MaxValue)
                eb.Property(Function(e) e.NullableUInt32).HasDefaultValue(UInteger.MaxValue)
                eb.Property(Function(e) e.NullableUInt64).HasDefaultValue(ULong.MaxValue)

                eb.HasKey(Function(e) e.Boolean)
            End Sub)

            modelBuilder.FinalizeModel()

            Dim modelSnapshotCode = generator.GenerateSnapshot(
            "MyNamespace",
            GetType(MyContext),
            "MySnapshot",
            modelBuilder.Model)

            Dim snapshot = CompileModelSnapshot(modelSnapshotCode, "MyNamespace.MySnapshot")
            Dim entityType = snapshot.Model.GetEntityTypes().Single()
            Assert.Equal(GetType(EntityWithEveryPrimitive).FullName, entityType.DisplayName())


            For Each prop In modelBuilder.Model.GetEntityTypes().Single().GetProperties()
                Dim expected = prop.GetDefaultValue()
                Dim actual = entityType.FindProperty(prop.Name).GetDefaultValue()

                If actual IsNot Nothing AndAlso expected IsNot Nothing Then
                    If expected.GetType().IsEnum Then
                        If TypeOf actual Is String Then
                            Dim actualString = actual.ToString()
                            actual = [Enum].Parse(expected.GetType(), actualString)
                        Else
                            actual = [Enum].ToObject(expected.GetType(), actual)
                        End If
                    End If

                    If actual.GetType() <> expected.GetType() Then
                        actual = Convert.ChangeType(actual, expected.GetType())
                    End If
                End If

                Assert.Equal(expected, actual)
            Next
        End Sub

        Private Class EntityWithEveryPrimitive
            Public Property [Boolean] As Boolean
            Public Property [Byte] As Byte
            Public Property ByteArray As Byte()
            Public Property [Char] As Char
            Public Property DateTime As DateTime
            Public Property DateTimeOffset As DateTimeOffset
            Public Property [Decimal] As Decimal
            Public Property [Double] As Double
            Public Property [Enum] As Enum1
            Public Property StringEnum As Enum1
            Public Property Guid As Guid
            Public Property Int16 As Short
            Public Property Int32 As Integer
            Public Property Int64 As Long
            Public Property NullableBoolean As Boolean?
            Public Property NullableByte As Byte?
            Public Property NullableChar As Char?
            Public Property NullableDateTime As DateTime?
            Public Property NullableDateTimeOffset As DateTimeOffset?
            Public Property NullableDecimal As Decimal?
            Public Property NullableDouble As Double?
            Public Property NullableEnum As Enum1?
            Public Property NullableStringEnum As Enum1?
            Public Property NullableGuid As Guid?
            Public Property NullableInt16 As Short?
            Public Property NullableInt32 As Integer?
            Public Property NullableInt64 As Long?
            Public Property NullableSByte As SByte?
            Public Property NullableSingle As Single?
            Public Property NullableTimeSpan As TimeSpan?
            Public Property NullableUInt16 As UShort?
            Public Property NullableUInt32 As UInteger?
            Public Property NullableUInt64 As ULong?
            Private _privateSetter As Integer
            Public Property PrivateSetter As Integer
                Get
                    Return _privateSetter
                End Get
                Private Set(Value As Integer)
                    _privateSetter = Value
                End Set
            End Property
            Public Property [SByte] As SByte
            Public Property [Single] As Single
            Public Property [String] As String
            Public Property TimeSpan As TimeSpan
            Public Property UInt16 As UShort
            Public Property UInt32 As UInteger
            Public Property UInt64 As ULong
        End Class

        <Flags>
        Public Enum Enum1
            [Default] = 0
            One = 1
            Two = 2
        End Enum

        Private Function CompileModelSnapshot(modelSnapshotCode As String, modelSnapshotTypeName As String) As ModelSnapshot
            Dim build As New BuildSource With
                {.References =
                            {
                                BuildReference.ByName("netstandard"),
                                BuildReference.ByName("System.Runtime"),
                                BuildReference.ByName("EntityFrameworkCore.VisualBasic.Test"),
                                BuildReference.ByName("Microsoft.EntityFrameworkCore"),
                                BuildReference.ByName("Microsoft.EntityFrameworkCore.Relational"),
                                BuildReference.ByName("System.Text.RegularExpressions")
                            },
                 .Sources = {modelSnapshotCode}}

            Dim assembly = build.BuildInMemory()

            Dim snapshotType = assembly.[GetType](modelSnapshotTypeName, throwOnError:=True, ignoreCase:=False)

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

            Return New ServiceCollection().
                        AddEntityFrameworkSqlServer().
                        AddEntityFrameworkDesignTimeServices().
                        AddSingleton(Of IVisualBasicHelper, VisualBasicHelper)().
                        AddSingleton(Of IMigrationsCodeGenerator, VisualBasicMigrationsGenerator)().
                        BuildServiceProvider().
                        GetRequiredService(Of IMigrationsCodeGenerator)()

        End Function

    End Class

End Namespace
