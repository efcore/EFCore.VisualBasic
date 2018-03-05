Imports System.Reflection
Imports System.Text
Imports System.Text.RegularExpressions
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.ChangeTracking
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Migrations
Imports Microsoft.EntityFrameworkCore.Migrations.Design
Imports Microsoft.EntityFrameworkCore.Migrations.Internal
Imports Microsoft.EntityFrameworkCore.Migrations.Operations
Imports Microsoft.EntityFrameworkCore.Storage
Imports Microsoft.EntityFrameworkCore.Storage.Converters
Imports Xunit

Public Class VisualBasicMigrationsGeneratorTests

    Private Shared ReadOnly _nl As String = Environment.NewLine
    Private Shared ReadOnly _nl2 As String = Environment.NewLine + Environment.NewLine
    Private Shared ReadOnly _toTable As String = _nl2 + "modelBuilder.ToTable(""WithAnnotations"")"

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
    Public Sub Test_new_annotations_handled_for_entity_types()
        Dim model = InMemoryTestHelpers.Instance.CreateConventionBuilder()
        Dim entityType = model.Entity(Of WithAnnotations).Metadata

        ' Only add the annotation here if it will never be present on IEntityType
        Dim notForEntityType = New HashSet(Of String) From
        {
            CoreAnnotationNames.MaxLengthAnnotation,
            CoreAnnotationNames.UnicodeAnnotation,
            CoreAnnotationNames.ProductVersionAnnotation,
            CoreAnnotationNames.ValueGeneratorFactoryAnnotation,
            CoreAnnotationNames.OwnedTypesAnnotation,
            CoreAnnotationNames.TypeMapping,
            CoreAnnotationNames.ValueConverter,
            CoreAnnotationNames.ValueComparer,
            CoreAnnotationNames.ProviderClrType,
            RelationalAnnotationNames.ColumnName,
            RelationalAnnotationNames.ColumnType,
            RelationalAnnotationNames.DefaultValueSql,
            RelationalAnnotationNames.ComputedColumnSql,
            RelationalAnnotationNames.DefaultValue,
            RelationalAnnotationNames.Name,
            RelationalAnnotationNames.SequencePrefix,
            RelationalAnnotationNames.DefaultSchema,
            RelationalAnnotationNames.Filter,
            RelationalAnnotationNames.DbFunction,
            RelationalAnnotationNames.MaxIdentifierLength,
            RelationalAnnotationNames.IsFixedLength
        }

        ' Add a line here if the code generator is supposed to handle this annotation
        ' Note that other tests should be added to check code is generated correctly
        Dim forEntityType = New Dictionary(Of String, (Object, String)) From
            {
                {
                    RelationalAnnotationNames.TableName,
                    ("MyTable", _nl2 + "modelBuilder." + NameOf(RelationalEntityTypeBuilderExtensions.ToTable) + "(""MyTable"")" + _nl)
                },
                {
                    RelationalAnnotationNames.Schema,
                    ("MySchema", _nl2 + "modelBuilder." + NameOf(RelationalEntityTypeBuilderExtensions.ToTable) + "(""WithAnnotations"",""MySchema"")" + _nl)
                },
                {
                    RelationalAnnotationNames.DiscriminatorProperty,
                    ("Id", _toTable + _nl2 + "modelBuilder." + NameOf(RelationalEntityTypeBuilderExtensions.HasDiscriminator) + "(Of Integer)(""Id"")" + _nl)
                },
                {
                    RelationalAnnotationNames.DiscriminatorValue,
                    ("MyDiscriminatorValue",
                    _toTable + _nl2 + "modelBuilder." + NameOf(RelationalEntityTypeBuilderExtensions.HasDiscriminator) +
                                      "()." + NameOf(DiscriminatorBuilder.HasValue) + "(""MyDiscriminatorValue"")" + _nl)
                }
            }

        MissingAnnotationCheck(
                entityType, notForEntityType, forEntityType,
                _toTable + _nl,
                Sub(g, m, b)
                    g.TestGenerateEntityTypeAnnotations("modelBuilder", CType(m, IEntityType), b)
                End Sub)
    End Sub

    <Fact>
    Public Sub Test_new_annotations_handled_for_properties()
        Dim Model = InMemoryTestHelpers.Instance.CreateConventionBuilder()
        Dim prop = Model.Entity(Of WithAnnotations).Property(Function(e) e.Id).Metadata

        ' Only add the annotation here if it will never be present on IProperty
        Dim notForProperty = New HashSet(Of String) From
            {
                CoreAnnotationNames.ProductVersionAnnotation,
                CoreAnnotationNames.OwnedTypesAnnotation,
                CoreAnnotationNames.ConstructorBinding,
                CoreAnnotationNames.NavigationAccessModeAnnotation,
                RelationalAnnotationNames.TableName,
                RelationalAnnotationNames.Schema,
                RelationalAnnotationNames.DefaultSchema,
                RelationalAnnotationNames.Name,
                RelationalAnnotationNames.SequencePrefix,
                RelationalAnnotationNames.DiscriminatorProperty,
                RelationalAnnotationNames.DiscriminatorValue,
                RelationalAnnotationNames.Filter,
                RelationalAnnotationNames.DbFunction,
                RelationalAnnotationNames.MaxIdentifierLength
            }

        ' Add a line here if the code generator Is supposed to handle this annotation
        ' Note that other tests should be added to check code Is generated correctly
        Dim forProperty = New Dictionary(Of String, (Object, String)) From
            {
                {
                    CoreAnnotationNames.MaxLengthAnnotation,
                    (256, " _" + _nl + "." + NameOf(PropertyBuilder.HasMaxLength) + "(256)")
                },
                {
                    CoreAnnotationNames.UnicodeAnnotation,
                    (False, " _" + _nl + "." + NameOf(PropertyBuilder.IsUnicode) + "(False)")
                },
                {
                    CoreAnnotationNames.ValueConverter,
                    (New ValueConverter(Of Integer, Long)(Function(v) v, Function(v) CInt(v)),
                    " _" + _nl + "." + NameOf(PropertyBuilder.HasConversion) + "(New " + NameOf(ValueConverter) + "(Of Long, Long)(Function(v) CType(Nothing, Long), Function(v) CType(Nothing, Long)))")
                },
                {
                    CoreAnnotationNames.ProviderClrType,
                    (GetType(Long), "")
                },
                {
                    RelationalAnnotationNames.ColumnName,
                    ("MyColumn", " _" + _nl + "." + NameOf(RelationalPropertyBuilderExtensions.HasColumnName) + "(""MyColumn"")")
                },
                {
                    RelationalAnnotationNames.ColumnType,
                    ("Integer", " _" + _nl + "." + NameOf(RelationalPropertyBuilderExtensions.HasColumnType) + "(""Integer"")")
                },
                {
                    RelationalAnnotationNames.DefaultValueSql,
                    ("some SQL", " _" + _nl + "." + NameOf(RelationalPropertyBuilderExtensions.HasDefaultValueSql) + "(""some SQL"")")
                },
                {
                    RelationalAnnotationNames.ComputedColumnSql,
                    ("some SQL", " _" + _nl + "." + NameOf(RelationalPropertyBuilderExtensions.HasComputedColumnSql) + "(""some SQL"")")
                },
                {
                    RelationalAnnotationNames.DefaultValue,
                    ("1", " _" + _nl + "." + NameOf(RelationalPropertyBuilderExtensions.HasDefaultValue) + "(""1"")")
                },
                {
                    CoreAnnotationNames.TypeMapping,
                    (New LongTypeMapping("bigint"), "")
                },
                {
                    RelationalAnnotationNames.IsFixedLength,
                    (True, " _" + _nl + "." + NameOf(RelationalPropertyBuilderExtensions.IsFixedLength) + "(True)")
                 }
            }

        MissingAnnotationCheck(
                prop, notForProperty, forProperty,
                "",
                Sub(g, m, b)
                    g.TestGeneratePropertyAnnotations(CType(m, IProperty), b)
                End Sub)
    End Sub

    Private Shared Sub MissingAnnotationCheck(
             metadataItem As IMutableAnnotatable,
             invalidAnnotations As HashSet(Of String),
             validAnnotations As Dictionary(Of String, (Value As Object, Expected As String)),
             generationDefault As String,
             test As Action(Of TestVisualBasicSnapshotGenerator, IMutableAnnotatable, IndentedStringBuilder))

        Dim codeHelper = New VisualBasicHelper()
        Dim generator = New TestVisualBasicSnapshotGenerator(New VisualBasicSnapshotGeneratorDependencies(codeHelper))

        For Each field In GetType(CoreAnnotationNames).GetFields().Concat(
                GetType(RelationalAnnotationNames).GetFields().Where(Function(f) f.Name <> "Prefix"))

            Dim annotationName = CStr(field.GetValue(Nothing))

            If Not invalidAnnotations.Contains(annotationName) Then

                metadataItem(annotationName) = If(validAnnotations.ContainsKey(annotationName),
                                                  validAnnotations(annotationName).Value,
                                                  New Random()) ' Something that cannot be scaffolded by default

                Dim sb = New IndentedStringBuilder()

                Try
                    ' Generator should Not throw--either update above, Or add to ignored list in generator
                    test(generator, metadataItem, sb)

                Catch e As Exception
                    Assert.False(True, $"Annotation '{annotationName}' was not handled by the code generator: {e.Message}")
                End Try

                If (validAnnotations.ContainsKey(annotationName)) Then
                    Assert.Equal(validAnnotations(annotationName).Expected, sb.ToString())
                Else
                    Assert.Equal(generationDefault, sb.ToString())
                End If

                metadataItem(annotationName) = Nothing
            End If
        Next
    End Sub

    Private Class WithAnnotations
        Public Property Id As Integer
    End Class

    Private Class TestVisualBasicSnapshotGenerator
        Inherits VisualBasicSnapshotGenerator

        Public Sub New(dependencies As VisualBasicSnapshotGeneratorDependencies)
            MyBase.New(dependencies)
        End Sub

        Public Overridable Sub TestGenerateEntityTypeAnnotations(builderName As String, entityType As IEntityType, stringBuilder As IndentedStringBuilder)
            GenerateEntityTypeAnnotations(builderName, entityType, stringBuilder)
        End Sub

        Public Overridable Sub TestGeneratePropertyAnnotations(prop As IProperty, stringBuilder As IndentedStringBuilder)
            GeneratePropertyAnnotations(prop, stringBuilder)
        End Sub

    End Class

    <Fact>
    Public Sub Value_converters_with_mapping_hints_are_scaffolded_correctly()
        Dim commonPrefix =
            " _" + _nl + "." + NameOf(PropertyBuilder.HasConversion) + "(New " + NameOf(ValueConverter) + "(Of Long, Long)(Function(v) CType(Nothing, Long), Function(v) CType(Nothing, Long)"

        AssertConverter(
            New ValueConverter(Of Integer, Long)(Function(v) v, Function(v) CInt(v)),
            commonPrefix + "))")

        AssertConverter(
            New ValueConverter(Of Integer, Long)(Function(v) v, Function(v) CInt(v), New ConverterMappingHints(size:=10)),
            commonPrefix + ", New ConverterMappingHints(size:= 10)))")

        AssertConverter(
            New ValueConverter(Of Integer, Long)(Function(v) v, Function(v) CInt(v), New ConverterMappingHints(precision:=10)),
            commonPrefix + ", New ConverterMappingHints(precision:= 10)))")

        AssertConverter(
            New ValueConverter(Of Integer, Long)(Function(v) v, Function(v) CInt(v), New ConverterMappingHints(scale:=10)),
            commonPrefix + ", New ConverterMappingHints(scale:= 10)))")

        AssertConverter(
            New ValueConverter(Of Integer, Long)(Function(v) v, Function(v) CInt(v), New ConverterMappingHints(unicode:=True)),
            commonPrefix + ", New ConverterMappingHints(unicode:= True)))")

        AssertConverter(
            New ValueConverter(Of Integer, Long)(Function(v) v, Function(v) CInt(v), New ConverterMappingHints(fixedLength:=False)),
            commonPrefix + ", New ConverterMappingHints(fixedLength:= False)))")

        AssertConverter(
            New ValueConverter(Of Integer, Long)(Function(v) v, Function(v) CInt(v), New ConverterMappingHints(fixedLength:=False, size:=77, scale:=-1)),
            commonPrefix + ", New ConverterMappingHints(size:= 77, scale:= -1, fixedLength:= False)))")

        AssertConverter(
             New ValueConverter(Of Integer, Long)(Function(v) v, Function(v) CInt(v), New ConverterMappingHints(sizeFunction:=Function(s) s / 10)),
             commonPrefix + ", New ConverterMappingHints(size:= 100)))")
    End Sub

    Private Shared Sub AssertConverter(valueConverter As ValueConverter, expected As String)
        Dim modelBuilder = InMemoryTestHelpers.Instance.CreateConventionBuilder()
        Dim prop = modelBuilder.Entity(Of WithAnnotations).Property(Function(e) e.Id).Metadata
        prop.SetMaxLength(1000)

        modelBuilder.GetInfrastructure().Metadata.Validate()

        Dim codeHelper = New VisualBasicHelper()
        Dim generator = New TestVisualBasicSnapshotGenerator(New VisualBasicSnapshotGeneratorDependencies(codeHelper))

        prop.SetValueConverter(valueConverter)

        Dim sb = New IndentedStringBuilder()

        generator.TestGeneratePropertyAnnotations(prop, sb)

        Assert.Equal(expected + " _" + _nl + ".HasMaxLength(1000)", sb.ToString())
    End Sub

    Private Enum RawEnum
        A
        B
    End Enum

    <Fact>
    Public Sub Snapshots_compile()
        Dim codeHelper = New VisualBasicHelper()
        Dim generator = New VisualBasicMigrationsGenerator(
                New MigrationsCodeGeneratorDependencies(),
                New VisualBasicMigrationsGeneratorDependencies(
                    codeHelper,
                    New VisualBasicMigrationOperationGenerator(
                        New VisualBasicMigrationOperationGeneratorDependencies(codeHelper)),
                    New VisualBasicSnapshotGenerator(New VisualBasicSnapshotGeneratorDependencies(codeHelper))))

        Dim modelBuilder = InMemoryTestHelpers.Instance.CreateConventionBuilder()

        Dim model = modelBuilder.Model
        CType(model, IMutableAnnotatable)("Some:EnumValue") = RegexOptions.Multiline
        CType(model, IMutableAnnotatable)("Relational:DbFunction:MyFunc") = New Object()

        Dim entityType = model.AddEntityType("Cheese")
        Dim property1 = entityType.AddProperty("Pickle", GetType(StringBuilder))
        property1.SetValueConverter(New ValueConverter(Of StringBuilder, String)(Function(v) v.ToString(), Function(v) New StringBuilder(v), New ConverterMappingHints(size:=10)))

        Dim property2 = entityType.AddProperty("Ham", GetType(RawEnum))
        property2.SetValueConverter(New ValueConverter(Of RawEnum, String)(Function(v) v.ToString(), Function(v) CType([Enum].Parse(GetType(RawEnum), v), RawEnum), New ConverterMappingHints(size:=10)))

        modelBuilder.GetInfrastructure().Metadata.Validate()

        Dim modelSnapshotCode = generator.GenerateSnapshot(
                "MyNamespace",
                GetType(MyContext),
                "MySnapshot",
                model)

        Assert.Equal(
                "' <auto-generated />
Imports System
Imports System.Text
Imports System.Text.RegularExpressions
Imports Bricelam.EntityFrameworkCore.VisualBasic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Migrations
Imports Microsoft.EntityFrameworkCore.Storage.Converters

Namespace MyNamespace
    <DbContext(GetType(VisualBasicMigrationsGeneratorTests.MyContext))>
    Partial Class MySnapshot
        Inherits ModelSnapshot

        Protected Overrides Sub BuildModel(modelBuilder As ModelBuilder)
            modelBuilder _
                .HasAnnotation(""Some:EnumValue"", RegexOptions.Multiline)
            modelBuilder.Entity(""Cheese"", Sub(b)

                    b.Property(Of String)(""Ham"") _
                        .HasConversion(New ValueConverter(Of String, String)(Function(v) CType(Nothing, String), Function(v) CType(Nothing, String), New ConverterMappingHints(size:= 10)))

                    b.Property(Of String)(""Pickle"") _
                        .HasConversion(New ValueConverter(Of String, String)(Function(v) CType(Nothing, String), Function(v) CType(Nothing, String), New ConverterMappingHints(size:= 10)))

                    b.ToTable(""Cheese"")
                End Sub)
        End Sub
    End Class
End Namespace
", modelSnapshotCode, ignoreLineEndingDifferences:=True)

        Dim snapshot = CompileModelSnapshot(modelSnapshotCode, "MyNamespace.MySnapshot")
        Assert.Equal(1, snapshot.Model.GetEntityTypes().Count())
    End Sub

    <Fact>
    Public Sub Migrations_compile()
        Dim codeHelper = New VisualBasicHelper()
        Dim generator = New VisualBasicMigrationsGenerator(
                    New MigrationsCodeGeneratorDependencies(),
                    New VisualBasicMigrationsGeneratorDependencies(
                        codeHelper,
                        New VisualBasicMigrationOperationGenerator(
                            New VisualBasicMigrationOperationGeneratorDependencies(codeHelper)),
                        New VisualBasicSnapshotGenerator(New VisualBasicSnapshotGeneratorDependencies(codeHelper))))

        Dim sqlOp = New SqlOperation With {.Sql = "-- TEST"}
        sqlOp("Some:EnumValue") = RegexOptions.Multiline

        Dim alterColumnOp = New AlterColumnOperation With
                        {
                            .Name = "C2",
                            .Table = "T1",
                            .ClrType = GetType(Storage.Database),  ''''''''''''''''''''''''''''''''''''''''''''''''''' ???????????
                            .OldColumn = New ColumnOperation With {.ClrType = GetType([Property])}
                        }

        Dim migrationCode = generator.GenerateMigration(
                    "MyNamespace",
                    "MyMigration",
                    {
                       sqlOp,
                        New AlterColumnOperation With
                        {
                            .Name = "C2",
                            .Table = "T1",
                            .ClrType = GetType(Storage.Database),  ''''''''''''''''''''''''''''''''''''''''''''''''''' ???????????
                            .OldColumn = New ColumnOperation With {.ClrType = GetType([Property])}
                        },
                        New AddColumnOperation With
                        {
                            .Name = "C3",
                            .Table = "T1",
                            .ClrType = GetType(PropertyEntry)
                        }
                    },
                    {})

        Assert.Equal(
"Imports System
Imports System.Collections.Generic
Imports System.Text.RegularExpressions
Imports Microsoft.EntityFrameworkCore.ChangeTracking
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Migrations
Imports Microsoft.EntityFrameworkCore.Storage

Namespace MyNamespace
    Public Partial Class MyMigration
        Inherits Migration

        Protected Overrides Sub Up(migrationBuilder As MigrationBuilder)
            migrationBuilder.Sql(""-- TEST"") _
                .Annotation(""Some:EnumValue"", RegexOptions.Multiline)

            migrationBuilder.AlterColumn(Of Database)(
                name:= ""C2"",
                table:= ""T1"",
                nullable:= False,
                oldClrType:= GetType([Property]))

            migrationBuilder.AddColumn(Of PropertyEntry)(
                name:= ""C3"",
                table:= ""T1"",
                nullable:= False)
        End Sub

        Protected Overrides Sub Down(migrationBuilder As MigrationBuilder)

        End Sub
    End Class
End Namespace
",
                    migrationCode,
                    ignoreLineEndingDifferences:=True)

        Dim model = New Model()
        model("Some:EnumValue") = RegexOptions.Multiline
        model("Relational:DbFunction:MyFunc") = New Object()


        Dim migrationMetadataCode = generator.GenerateMetadata(
                    "MyNamespace",
                    GetType(MyContext),
                    "MyMigration",
                    "20150511161616_MyMigration",
                    model)
        Assert.Equal(
"' <auto-generated />
Imports System
Imports System.Text.RegularExpressions
Imports Bricelam.EntityFrameworkCore.VisualBasic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Migrations

Namespace MyNamespace
    <DbContext(GetType(VisualBasicMigrationsGeneratorTests.MyContext))>
    <Migration(""20150511161616_MyMigration"")>
    Partial Class MyMigration
        Protected Overrides Sub BuildTargetModel(modelBuilder As ModelBuilder)
            modelBuilder _
                .HasAnnotation(""Some:EnumValue"", RegexOptions.Multiline)
        End Sub
    End Class
End Namespace
",
                        migrationMetadataCode,
                        ignoreLineEndingDifferences:=True)

        Dim build = New BuildSource With
        {
            .Sources = {migrationCode, migrationMetadataCode}
        }

        build.References.Add(BuildReference.ByName("Bricelam.EntityFrameworkCore.VisualBasic.Test"))
        build.References.Add(BuildReference.ByName("Microsoft.EntityFrameworkCore"))
        build.References.Add(BuildReference.ByName("Microsoft.EntityFrameworkCore.Relational"))

        Dim asm = build.BuildInMemory()

        Dim migrationType = asm.GetType("MyNamespace.MyMigration", throwOnError:=True, ignoreCase:=False)

        Dim contextTypeAttribute = migrationType.GetTypeInfo().GetCustomAttribute(Of DbContextAttribute)
        Assert.NotNull(contextTypeAttribute)
        Assert.Equal(GetType(MyContext), contextTypeAttribute.ContextType)

        Dim migr = CType(Activator.CreateInstance(migrationType), Migration)

        Assert.Equal("20150511161616_MyMigration", migr.GetId())

        Assert.Equal(3, migr.UpOperations.Count)
        Assert.Empty(migr.DownOperations)
        Assert.Empty(migr.TargetModel.GetEntityTypes())
    End Sub

    <Fact>
    Public Sub Snapshot_with_default_values_are_round_tripped()
        Dim codeHelper = New VisualBasicHelper()
        Dim generator = New VisualBasicMigrationsGenerator(
                    New MigrationsCodeGeneratorDependencies(),
                    New VisualBasicMigrationsGeneratorDependencies(
                        codeHelper,
                        New VisualBasicMigrationOperationGenerator(
                            New VisualBasicMigrationOperationGeneratorDependencies(codeHelper)),
                        New VisualBasicSnapshotGenerator(New VisualBasicSnapshotGeneratorDependencies(codeHelper))))

        Dim modelBuilder = InMemoryTestHelpers.Instance.CreateConventionBuilder()
        modelBuilder.Entity(Of EntityWithEveryPrimitive)(
                    Sub(eb)
                        eb.Property(Function(e) e.Boolean).HasDefaultValue(False)
                        eb.Property(Function(e) e.Byte).HasDefaultValue(CByte(0))
                        eb.Property(Function(e) e.ByteArray).HasDefaultValue({CByte(0)})
                        eb.Property(Function(e) e.Char).HasDefaultValue("0"c)
                        eb.Property(Function(e) e.DateTime).HasDefaultValue(New DateTime(1980, 1, 1))
                        eb.Property(Function(e) e.DateTimeOffset).HasDefaultValue(New DateTimeOffset(1980, 1, 1, 0, 0, 0, New TimeSpan(0, 0, 0)))
                        eb.Property(Function(e) e.Decimal).HasDefaultValue(0D)
                        eb.Property(Function(e) e.Double).HasDefaultValue(CDbl(0.0))
                        eb.Property(Function(e) e.Enum).HasDefaultValue(Enum1.Default)
                        eb.Property(Function(e) e.NullableEnum).HasDefaultValue(Enum1.Default).HasConversion(Of String)()
                        eb.Property(Function(e) e.Guid).HasDefaultValue(New Guid())
                        eb.Property(Function(e) e.Int16).HasDefaultValue(0S)
                        eb.Property(Function(e) e.Int32).HasDefaultValue(0)
                        eb.Property(Function(e) e.Int64).HasDefaultValue(0L)
                        eb.Property(Function(e) e.Single).HasDefaultValue(0.0F)
                        eb.Property(Function(e) e.SByte).HasDefaultValue(CSByte(0))
                        eb.Property(Function(e) e.String).HasDefaultValue("""")
                        eb.Property(Function(e) e.TimeSpan).HasDefaultValue(New TimeSpan(0, 0, 0))
                        eb.Property(Function(e) e.UInt16).HasDefaultValue(CUShort(0))
                        eb.Property(Function(e) e.UInt32).HasDefaultValue(CUInt(0))
                        eb.Property(Function(e) e.UInt64).HasDefaultValue(CULng(0))
                        eb.Property(Function(e) e.NullableBoolean).HasDefaultValue(True)
                        eb.Property(Function(e) e.NullableByte).HasDefaultValue(Byte.MaxValue)
                        eb.Property(Function(e) e.NullableChar).HasDefaultValue(""""c)
                        eb.Property(Function(e) e.NullableDateTime).HasDefaultValue(New DateTime(1900, 12, 31))
                        eb.Property(Function(e) e.NullableDateTimeOffset).HasDefaultValue(New DateTimeOffset(3000, 1, 1, 0, 0, 0, New TimeSpan(0, 0, 0)))
                        eb.Property(Function(e) e.NullableDecimal).HasDefaultValue(2D * CDec(Long.MaxValue))
                        eb.Property(Function(e) e.NullableDouble).HasDefaultValue(0.6822871999174)
                        eb.Property(Function(e) e.NullableEnum).HasDefaultValue(Enum1.Default)
                        eb.Property(Function(e) e.NullableStringEnum).HasDefaultValue(Enum1.Default).HasConversion(Of String)()
                        eb.Property(Function(e) e.NullableGuid).HasDefaultValue(New Guid())
                        eb.Property(Function(e) e.NullableInt16).HasDefaultValue(Short.MinValue)
                        eb.Property(Function(e) e.NullableInt32).HasDefaultValue(CInt(Integer.MinValue))
                        eb.Property(Function(e) e.NullableInt64).HasDefaultValue(CLng(Long.MinValue))
                        eb.Property(Function(e) e.NullableSingle).HasDefaultValue(0.3333333F)
                        eb.Property(Function(e) e.NullableSByte).HasDefaultValue(SByte.MinValue)
                        eb.Property(Function(e) e.NullableTimeSpan).HasDefaultValue(New TimeSpan(-1, 0, 0))
                        eb.Property(Function(e) e.NullableUInt16).HasDefaultValue(UShort.MaxValue)
                        eb.Property(Function(e) e.NullableUInt32).HasDefaultValue(CUInt(UInteger.MaxValue))
                        eb.Property(Function(e) e.NullableUInt64).HasDefaultValue(ULong.MaxValue)
                    End Sub)

        Dim modelSnapshotCode = generator.GenerateSnapshot(
                    "MyNamespace",
                    GetType(MyContext),
                    "MySnapshot",
                    modelBuilder.Model)

        Dim snapshot = CompileModelSnapshot(modelSnapshotCode, "MyNamespace.MySnapshot")
        Dim EntityType = snapshot.Model.GetEntityTypes().Single()
        Assert.Equal(GetType(EntityWithEveryPrimitive).FullName, EntityType.DisplayName())

        For Each prop In modelBuilder.Model.GetEntityTypes().Single().GetProperties()

            Dim snapshotProperty = EntityType.FindProperty(prop.Name)
            Assert.Equal(prop.Relational().DefaultValue, snapshotProperty.Relational().DefaultValue)
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
            Private Set(value As Integer)
                _privateSetter = value
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

    Public Enum Enum1
        [Default]
    End Enum

    Private Function CompileModelSnapshot(modelSnapshotCode As String, modelSnapshotTypeName As String) As ModelSnapshot
        Dim build = New BuildSource With {.Sources = {modelSnapshotCode}}

        build.References.Add(BuildReference.ByName("Microsoft.EntityFrameworkCore"))
        build.References.Add(BuildReference.ByName("Microsoft.EntityFrameworkCore.Relational"))
        build.References.Add(BuildReference.ByName("Bricelam.EntityFrameworkCore.VisualBasic"))
        build.References.Add(BuildReference.ByName("Bricelam.EntityFrameworkCore.VisualBasic.Test"))

        Dim assembly = build.BuildInMemory()

        Dim snapshotType = assembly.GetType(modelSnapshotTypeName, throwOnError:=True, ignoreCase:=False)

        Dim contextTypeAttribute = snapshotType.GetTypeInfo().GetCustomAttribute(Of DbContextAttribute)
        Assert.NotNull(contextTypeAttribute)
        Assert.Equal(GetType(MyContext), contextTypeAttribute.ContextType)

        Return CType(Activator.CreateInstance(snapshotType), ModelSnapshot)
    End Function

    Public Class MyContext
    End Class

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

    Private Function CreateGenerator() As VisualBasicMigrationsGenerator
        Return New VisualBasicMigrationsGenerator(New MigrationsCodeGeneratorDependencies(), New VisualBasicMigrationsGeneratorDependencies(New VisualBasicHelper(), New VisualBasicMigrationOperationGenerator(New VisualBasicMigrationOperationGeneratorDependencies(New VisualBasicHelper())), New VisualBasicSnapshotGenerator(New VisualBasicSnapshotGeneratorDependencies(New VisualBasicHelper()))))
    End Function

End Class