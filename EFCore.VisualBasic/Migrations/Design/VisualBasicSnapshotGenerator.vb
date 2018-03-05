Imports Bricelam.EntityFrameworkCore.VisualBasic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Storage.Converters

''' <summary>
'''     Used to generate Visual Basic code for creating an <see cref="IModel" />.
''' </summary>
Public Class VisualBasicSnapshotGenerator
    Implements IVisualBasicSnapshotGenerator

    ''' <summary>
    '''     Initializes a New instance of the <see cref="VisualBasicSnapshotGenerator" /> class.
    ''' </summary>
    ''' <param name="dependencies"> The dependencies. </param>
    Public Sub New(dependencies As VisualBasicSnapshotGeneratorDependencies)
        VisualBasicDependencies = dependencies
    End Sub

    ''' <summary>
    '''     Parameter object containing dependencies for this service.
    ''' </summary>
    Protected Overridable ReadOnly Property VisualBasicDependencies As VisualBasicSnapshotGeneratorDependencies

    Private ReadOnly Property VBCode As IVisualBasicHelper
        Get
            Return VisualBasicDependencies.VisualBasicHelper
        End Get
    End Property

    ''' <summary>
    '''     Generates code for creating an <see cref="IModel" />.
    ''' </summary>
    ''' <param name="builderName"> The <see cref="ModelBuilder" /> variable name. </param>
    ''' <param name="model"> The model. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Public Overridable Sub Generate(builderName As String,
                                    model As IModel,
                                    stringBuilder As IndentedStringBuilder) Implements IVisualBasicSnapshotGenerator.Generate

        Dim annotations = model.GetAnnotations().Cast(Of IAnnotation).ToList()
        If annotations.Any() Then
            stringBuilder.Append(builderName)
            Using stringBuilder.Indent()
                GenerateFluentApiForAnnotation(annotations, RelationalAnnotationNames.DefaultSchema, NameOf(RelationalModelBuilderExtensions.HasDefaultSchema), stringBuilder)
                IgnoreAnnotationTypes(annotations, RelationalAnnotationNames.DbFunction)
                IgnoreAnnotationTypes(annotations, RelationalAnnotationNames.MaxIdentifierLength)
                IgnoreAnnotationTypes(annotations, CoreAnnotationNames.OwnedTypesAnnotation)
                GenerateAnnotations(annotations, stringBuilder)
            End Using

        End If

        GenerateEntityTypes(builderName, Sort(model.GetEntityTypes().Where(Function(et) Not et.IsQueryType).ToList()), stringBuilder)
    End Sub

    Private Function Sort(entityTypes As IReadOnlyList(Of IEntityType)) As IReadOnlyList(Of IEntityType)
        Dim entityTypeGraph = New Multigraph(Of IEntityType, Integer)()
        entityTypeGraph.AddVertices(entityTypes)
        For Each entityType In entityTypes.Where(Function(et) et.BaseType IsNot Nothing)
            entityTypeGraph.AddEdge(entityType.BaseType, entityType, 0)
        Next

        Return entityTypeGraph.TopologicalSort()
    End Function

    ''' <summary>
    '''     Generates code for <see cref="IEntityType" /> objects.
    ''' </summary>
    ''' <param name="builderName"> The name of the builder variable. </param>
    ''' <param name="entityTypes"> The entity types. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateEntityTypes(builderName As String,
                                                  entityTypes As IReadOnlyList(Of IEntityType),
                                                  stringBuilder As IndentedStringBuilder)
        For Each entityType In entityTypes.Where(Function(e) Not e.HasDefiningNavigation() AndAlso e.FindOwnership() Is Nothing)
            stringBuilder.AppendLine()
            GenerateEntityType(builderName, entityType, stringBuilder)
        Next

        For Each entityType In entityTypes.Where(Function(e) Not e.HasDefiningNavigation() AndAlso e.FindOwnership() Is Nothing AndAlso (e.GetDeclaredForeignKeys().Any() OrElse e.GetDeclaredReferencingForeignKeys().Any(Function(fk) fk.IsOwnership)))
            stringBuilder.AppendLine()
            GenerateEntityTypeRelationships(builderName, entityType, stringBuilder)
        Next
    End Sub

    ''' <summary>
    '''     Generates code for an <see cref="IEntityType" />.
    ''' </summary>
    ''' <param name="builderName"> The name of the builder variable. </param>
    ''' <param name="entityType"> The entity type. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateEntityType(builderName As String,
                                                 entityType As IEntityType,
                                                 stringBuilder As IndentedStringBuilder)

        Dim ownerNavigation = entityType.FindOwnership()?.PrincipalToDependent.Name
        stringBuilder.Append(builderName).Append(If(ownerNavigation IsNot Nothing, ".OwnsOne(", ".Entity(")).Append(VBCode.Literal(entityType.Name))
        If ownerNavigation IsNot Nothing Then
            stringBuilder.Append(", ").Append(VBCode.Literal(ownerNavigation))
        End If

        If builderName.StartsWith("b", StringComparison.Ordinal) Then
            Dim counter = 1
            If builderName.Length > 1 AndAlso Integer.TryParse(builderName.Substring(1, builderName.Length - 1), counter) Then
                counter += 1
            End If

            builderName = "b" & (If(counter = 0, "", counter.ToString()))
        Else
            builderName = "b"
        End If

        stringBuilder.Append(", Sub(").Append(builderName).AppendLine(")")
        Using stringBuilder.Indent()
            Using stringBuilder.Indent()
                GenerateBaseType(builderName, entityType.BaseType, stringBuilder)
                GenerateProperties(builderName, entityType.GetDeclaredProperties(), stringBuilder)
                If ownerNavigation Is Nothing Then
                    GenerateKeys(builderName, entityType.GetDeclaredKeys(), entityType.FindDeclaredPrimaryKey(), stringBuilder)
                End If

                GenerateIndexes(builderName, entityType.GetDeclaredIndexes(), stringBuilder)
                GenerateEntityTypeAnnotations(builderName, entityType, stringBuilder)
                If ownerNavigation IsNot Nothing Then
                    GenerateRelationships(builderName, entityType, stringBuilder)
                End If

                GenerateSeedData(entityType.GetProperties(), entityType.GetSeedData(), stringBuilder)
            End Using

            stringBuilder.AppendLine("End Sub)")
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for owned entity types.
    ''' </summary>
    ''' <param name="builderName"> The name of the builder variable. </param>
    ''' <param name="ownerships"> The foreign keys identifying each entity type. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateOwnedTypes(builderName As String,
                                                 ownerships As IEnumerable(Of IForeignKey),
                                                 stringBuilder As IndentedStringBuilder)
        For Each ownership In ownerships
            stringBuilder.AppendLine()
            GenerateOwnedType(builderName, ownership, stringBuilder)
        Next
    End Sub

    ''' <summary>
    '''     Generates code for an owned entity types.
    ''' </summary>
    ''' <param name="builderName"> The name of the builder variable. </param>
    ''' <param name="ownership"> The foreign key identifying the entity type. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateOwnedType(builderName As String,
                                                ownership As IForeignKey,
                                                stringBuilder As IndentedStringBuilder)
        GenerateEntityType(builderName, ownership.DeclaringEntityType, stringBuilder)
    End Sub

    ''' <summary>
    '''     Generates code for the relationships of an <see cref="IEntityType" />.
    ''' </summary>
    ''' <param name="builderName"> The name of the builder variable. </param>
    ''' <param name="entityType"> The entity type. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateEntityTypeRelationships(builderName As String,
                                                              entityType As IEntityType,
                                                              stringBuilder As IndentedStringBuilder)

        stringBuilder.Append(builderName).Append(".Entity(").Append(VBCode.Literal(entityType.Name)).AppendLine(", Function (b)")
        Using stringBuilder.Indent()
            'stringBuilder.Append("{")
            Using stringBuilder.Indent()
                GenerateRelationships("b", entityType, stringBuilder)
            End Using

            stringBuilder.AppendLine().AppendLine("End Function)")
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for the relationships of an <see cref="IEntityType" />.
    ''' </summary>
    ''' <param name="builderName"> The name of the builder variable. </param>
    ''' <param name="entityType"> The entity type. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateRelationships(builderName As String,
                                                    entityType As IEntityType,
                                                    stringBuilder As IndentedStringBuilder)
        GenerateForeignKeys(builderName, entityType.GetDeclaredForeignKeys(), stringBuilder)
        GenerateOwnedTypes(builderName, entityType.GetDeclaredReferencingForeignKeys().Where(Function(fk) fk.IsOwnership), stringBuilder)
    End Sub

    ''' <summary>
    '''     Generates code for the base type of an <see cref="IEntityType" />.
    ''' </summary>
    ''' <param name="builderName"> The name of the builder variable. </param>
    ''' <param name="baseType"> The base entity type. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateBaseType(builderName As String,
                                               baseType As IEntityType,
                                               stringBuilder As IndentedStringBuilder)
        If baseType IsNot Nothing Then
            stringBuilder.AppendLine().Append(builderName).Append(".HasBaseType(").Append(VBCode.Literal(baseType.Name)).AppendLine(")")
        End If
    End Sub

    ''' <summary>
    '''     Generates code for <see cref="IProperty" /> objects.
    ''' </summary>
    ''' <param name="builderName"> The name of the builder variable. </param>
    ''' <param name="properties"> The properties. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateProperties(builderName As String,
                                                 properties As IEnumerable(Of IProperty),
                                                 stringBuilder As IndentedStringBuilder)
        Dim firstProperty = True
        For Each [property] In properties
            If Not firstProperty Then
                stringBuilder.AppendLine()
            Else
                firstProperty = False
            End If

            GenerateProperty(builderName, [property], stringBuilder)
        Next
    End Sub

    ''' <summary>
    '''     Generates code for an <see cref="IProperty" />.
    ''' </summary>
    ''' <param name="builderName"> The name of the builder variable. </param>
    ''' <param name="property"> The property. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateProperty(builderName As String,
                                               [property] As IProperty,
                                               stringBuilder As IndentedStringBuilder)

        Dim clrType = If(FindValueConverter([property])?.ProviderClrType, [property].ClrType)

        stringBuilder.AppendLine() _
                     .Append(builderName) _
                     .Append(".Property(Of ") _
                     .Append(VBCode.Reference(clrType)) _
                     .Append(")(") _
                     .Append(VBCode.Literal([property].Name)) _
                     .Append(")")
        Using stringBuilder.Indent()
            If [property].IsConcurrencyToken Then
                stringBuilder.AppendLine(" _").Append(".IsConcurrencyToken()")
            End If

            If [property].IsNullable <> ([property].ClrType.IsNullableType() AndAlso Not [property].IsPrimaryKey()) Then
                stringBuilder.AppendLine(" _").Append(".IsRequired()")
            End If

            If [property].ValueGenerated <> ValueGenerated.Never Then
                stringBuilder.AppendLine(" _").Append(If([property].ValueGenerated = ValueGenerated.OnAdd, ".ValueGeneratedOnAdd()", If([property].ValueGenerated = ValueGenerated.OnUpdate, ".ValueGeneratedOnUpdate()", ".ValueGeneratedOnAddOrUpdate()")))
            End If

            GeneratePropertyAnnotations([property], stringBuilder)
        End Using

    End Sub

    ''' <summary>
    '''     Generates code for the annotations on an <see cref="IProperty" />.
    ''' </summary>
    ''' <param name="property"> The property. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GeneratePropertyAnnotations([property] As IProperty,
                                                          stringBuilder As IndentedStringBuilder)
        Dim annotations = [property].GetAnnotations().ToList()
        Dim valueConverter = FindValueConverter([property])
        If valueConverter IsNot Nothing Then
            Dim storeType = VBCode.Reference(valueConverter.ProviderClrType)

            stringBuilder.AppendLine(" _") _
                         .Append(".") _
                         .Append(NameOf(PropertyBuilder.HasConversion)) _
                         .Append("(New ") _
                         .Append(NameOf(Storage.Converters.ValueConverter)) _
                         .Append("(Of ") _
                         .Append(storeType) _
                         .Append(", ") _
                         .Append(storeType) _
                         .Append(")(Function(v) CType(Nothing, ") _
                         .Append(storeType) _
                         .Append("), Function(v) CType(Nothing, ") _
                         .Append(storeType)
            Dim hints = valueConverter.MappingHints
            If Not hints.IsEmpty Then
                Dim nonNulls = New List(Of String)()
                If hints.Size IsNot Nothing Then
                    nonNulls.Add("size:= " & VBCode.Literal(hints.Size.Value))
                ElseIf hints.SizeFunction IsNot Nothing Then
                    Dim maxLength = [property].GetMaxLength()
                    If maxLength IsNot Nothing Then
                        nonNulls.Add("size:= " & VBCode.Literal(hints.SizeFunction(maxLength.Value)))
                    End If
                End If

                If hints.Precision IsNot Nothing Then
                    nonNulls.Add("precision:= " & VBCode.Literal(hints.Precision.Value))
                End If

                If hints.Scale IsNot Nothing Then
                    nonNulls.Add("scale:= " & VBCode.Literal(hints.Scale.Value))
                End If

                If hints.IsUnicode IsNot Nothing Then
                    nonNulls.Add("unicode:= " & VBCode.Literal(hints.IsUnicode.Value))
                End If

                If hints.IsFixedLength IsNot Nothing Then
                    nonNulls.Add("fixedLength:= " & VBCode.Literal(hints.IsFixedLength.Value))
                End If

                stringBuilder.Append("), New ConverterMappingHints(").Append(String.Join(", ", nonNulls))
            End If

            stringBuilder.Append(")))")

        End If

        For Each consumed In annotations.Where(
                Function(a) a.Name = CoreAnnotationNames.ValueConverter OrElse
                    a.Name = CoreAnnotationNames.ProviderClrType).ToList()
            annotations.Remove(consumed)
        Next

        GenerateFluentApiForAnnotation(annotations, RelationalAnnotationNames.ColumnName, NameOf(RelationalPropertyBuilderExtensions.HasColumnName), stringBuilder)
        GenerateFluentApiForAnnotation(annotations, RelationalAnnotationNames.ColumnType, NameOf(RelationalPropertyBuilderExtensions.HasColumnType), stringBuilder)
        GenerateFluentApiForAnnotation(annotations, RelationalAnnotationNames.DefaultValueSql, NameOf(RelationalPropertyBuilderExtensions.HasDefaultValueSql), stringBuilder)
        GenerateFluentApiForAnnotation(annotations, RelationalAnnotationNames.ComputedColumnSql, NameOf(RelationalPropertyBuilderExtensions.HasComputedColumnSql), stringBuilder)
        GenerateFluentApiForAnnotation(annotations, RelationalAnnotationNames.IsFixedLength, NameOf(RelationalPropertyBuilderExtensions.IsFixedLength), stringBuilder)
        GenerateFluentApiForAnnotation(annotations, CoreAnnotationNames.MaxLengthAnnotation, NameOf(PropertyBuilder.HasMaxLength), stringBuilder)
        GenerateFluentApiForAnnotation(annotations, CoreAnnotationNames.UnicodeAnnotation, NameOf(PropertyBuilder.IsUnicode), stringBuilder)
        GenerateFluentApiForAnnotation(
                annotations,
                RelationalAnnotationNames.DefaultValue,
                Function(a) If(valueConverter Is Nothing, a?.Value, valueConverter.ConvertToStore(a?.Value)),
                NameOf(RelationalPropertyBuilderExtensions.HasDefaultValue),
                stringBuilder)
        IgnoreAnnotations(annotations, CoreAnnotationNames.ValueGeneratorFactoryAnnotation, CoreAnnotationNames.PropertyAccessModeAnnotation, CoreAnnotationNames.TypeMapping, CoreAnnotationNames.ValueComparer)
        GenerateAnnotations(annotations, stringBuilder)
    End Sub

    Private Shared Function FindValueConverter(prop As IProperty) As ValueConverter
        Return If(prop.FindMapping()?.Converter, prop.GetValueConverter())
    End Function

    ''' <summary>
    '''     Generates code for <see cref="IKey" /> objects.
    ''' </summary>
    ''' <param name="builderName"> The name of the builder variable. </param>
    ''' <param name="keys"> The keys. </param>
    ''' <param name="primaryKey"> The primary key. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateKeys(builderName As String, keys As IEnumerable(Of IKey),
                                           primaryKey As IKey,
                                           stringBuilder As IndentedStringBuilder)
        If primaryKey IsNot Nothing Then
            GenerateKey(builderName, primaryKey, stringBuilder, primary:=True)
        End If

        Dim firstKey = True
        For Each key In keys.Where(Function(k) Not k Is primaryKey AndAlso (Not k.GetReferencingForeignKeys().Any() OrElse k.GetAnnotations().Any()))
            If Not firstKey Then
                stringBuilder.AppendLine()
            Else
                firstKey = False
            End If

            GenerateKey(builderName, key, stringBuilder)
        Next
    End Sub

    ''' <summary>
    '''     Generates code for an <see cref="IKey" />.
    ''' </summary>
    ''' <param name="builderName"> The name of the builder variable. </param>
    ''' <param name="key"> The key. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    ''' <param name="primary">A value indicating whether the key Is primary. </param>
    Protected Overridable Sub GenerateKey(builderName As String,
                                          key As IKey,
                                          stringBuilder As IndentedStringBuilder,
                                          Optional primary As Boolean = False)
        stringBuilder.AppendLine().AppendLine().Append(builderName).Append(If(primary, ".HasKey(", ".HasAlternateKey(")).Append(String.Join(", ", key.Properties.[Select](Function(p) VBCode.Literal(p.Name)))).Append(")")
        Using stringBuilder.Indent()
            Dim annotations = key.GetAnnotations().ToList()
            GenerateFluentApiForAnnotation(annotations, RelationalAnnotationNames.Name, NameOf(RelationalKeyBuilderExtensions.HasName), stringBuilder)
            GenerateAnnotations(annotations, stringBuilder)
        End Using

    End Sub

    ''' <summary>
    '''     Generates code for <see cref="IIndex" /> objects.
    ''' </summary>
    ''' <param name="builderName"> The name of the builder variable. </param>
    ''' <param name="indexes"> The indexes. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateIndexes(builderName As String,
                                              indexes As IEnumerable(Of IIndex),
                                              stringBuilder As IndentedStringBuilder)
        For Each index In indexes
            stringBuilder.AppendLine()
            GenerateIndex(builderName, index, stringBuilder)
        Next
    End Sub

    ''' <summary>
    '''     Generates code an <see cref="IIndex" />.
    ''' </summary>
    ''' <param name="builderName"> The name of the builder variable. </param>
    ''' <param name="index"> The index. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateIndex(builderName As String,
                                            index As IIndex,
                                            stringBuilder As IndentedStringBuilder)
        stringBuilder.AppendLine().Append(builderName).Append(".HasIndex(").Append(String.Join(", ", index.Properties.[Select](Function(p) VBCode.Literal(p.Name)))).Append(")")
        Using stringBuilder.Indent()
            If index.IsUnique Then
                stringBuilder.AppendLine().Append(".IsUnique()")
            End If

            Dim annotations = index.GetAnnotations().ToList()
            GenerateFluentApiForAnnotation(annotations, RelationalAnnotationNames.Name, NameOf(RelationalIndexBuilderExtensions.HasName), stringBuilder)
            GenerateFluentApiForAnnotation(annotations, RelationalAnnotationNames.Filter, NameOf(RelationalIndexBuilderExtensions.HasFilter), stringBuilder)
            GenerateAnnotations(annotations, stringBuilder)
        End Using

    End Sub

    ''' <summary>
    '''     Generates code for the annotations on an entity type.
    ''' </summary>
    ''' <param name="builderName"> The name of the builder variable. </param>
    ''' <param name="entityType"> The entity type. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateEntityTypeAnnotations(builderName As String,
                                                            entityType As IEntityType,
                                                            stringBuilder As IndentedStringBuilder)
        Dim annotations = entityType.GetAnnotations().ToList()
        Dim tableNameAnnotation = annotations.FirstOrDefault(Function(a) a.Name = RelationalAnnotationNames.TableName)
        Dim schemaAnnotation = annotations.FirstOrDefault(Function(a) a.Name = RelationalAnnotationNames.Schema)
        stringBuilder.AppendLine().AppendLine().Append(builderName).Append(".").Append(NameOf(RelationalEntityTypeBuilderExtensions.ToTable)).Append("(").Append(VBCode.Literal(If(CStr(tableNameAnnotation?.Value), entityType.DisplayName())))
        annotations.Remove(tableNameAnnotation)
        If schemaAnnotation?.Value IsNot Nothing Then
            stringBuilder.Append(",").Append(VBCode.Literal(CStr(schemaAnnotation.Value)))
            annotations.Remove(schemaAnnotation)
        End If

        stringBuilder.AppendLine(")")
        Dim discriminatorPropertyAnnotation = annotations.FirstOrDefault(Function(a) a.Name = RelationalAnnotationNames.DiscriminatorProperty)
        Dim discriminatorValueAnnotation = annotations.FirstOrDefault(Function(a) a.Name = RelationalAnnotationNames.DiscriminatorValue)
        If (If(discriminatorPropertyAnnotation, discriminatorValueAnnotation)) IsNot Nothing Then
            stringBuilder.AppendLine().Append(builderName).Append(".").Append(NameOf(RelationalEntityTypeBuilderExtensions.HasDiscriminator))
            If discriminatorPropertyAnnotation?.Value IsNot Nothing Then
                Dim propertyClrType = entityType.FindProperty(CStr(discriminatorPropertyAnnotation.Value))?.ClrType
                stringBuilder.Append("(Of ") _
                             .Append(VBCode.Reference(propertyClrType.UnwrapEnumType())) _
                             .Append(")(") _
                             .Append(VBCode.UnknownLiteral(discriminatorPropertyAnnotation.Value)) _
                             .Append(")") _
                             .AppendLine()
            Else
                stringBuilder.Append("()")
            End If

            If discriminatorValueAnnotation?.Value IsNot Nothing Then
                stringBuilder.Append(".") _
                             .Append(NameOf(DiscriminatorBuilder.HasValue)) _
                             .Append("(") _
                             .Append(VBCode.UnknownLiteral(discriminatorValueAnnotation.Value)) _
                             .Append(")") _
                             .AppendLine()

            End If

            annotations.Remove(discriminatorPropertyAnnotation)
            annotations.Remove(discriminatorValueAnnotation)
        End If

        IgnoreAnnotations(annotations, RelationshipDiscoveryConvention.NavigationCandidatesAnnotationName, RelationshipDiscoveryConvention.AmbiguousNavigationsAnnotationName, InversePropertyAttributeConvention.InverseNavigationsAnnotationName, CoreAnnotationNames.NavigationAccessModeAnnotation, CoreAnnotationNames.PropertyAccessModeAnnotation, CoreAnnotationNames.ConstructorBinding)
        If annotations.Any() Then
            For Each annotation In annotations
                stringBuilder.AppendLine().Append(builderName).AppendLine(" _")
                GenerateAnnotation(annotation, stringBuilder)
                stringBuilder.AppendLine()
            Next
        End If
    End Sub

    ''' <summary>
    '''     Generates code for <see cref="IForeignKey" /> objects.
    ''' </summary>
    ''' <param name="builderName"> The name of the builder variable. </param>
    ''' <param name="foreignKeys"> The foreign keys. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateForeignKeys(builderName As String,
                                                  foreignKeys As IEnumerable(Of IForeignKey),
                                                  stringBuilder As IndentedStringBuilder)
        For Each foreignKey In foreignKeys
            stringBuilder.AppendLine()
            GenerateForeignKey(builderName, foreignKey, stringBuilder)
        Next
    End Sub

    ''' <summary>
    '''     Generates code for an <see cref="IForeignKey" />.
    ''' </summary>
    ''' <param name="builderName"> The name of the builder variable. </param>
    ''' <param name="foreignKey"> The foreign key. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateForeignKey(builderName As String,
                                                 foreignKey As IForeignKey,
                                                 stringBuilder As IndentedStringBuilder)
        stringBuilder.Append(builderName).Append(".HasOne(").Append(VBCode.Literal(foreignKey.PrincipalEntityType.Name))
        If foreignKey.DependentToPrincipal IsNot Nothing Then
            stringBuilder.Append(", ").Append(VBCode.Literal(foreignKey.DependentToPrincipal.Name))
        End If

        stringBuilder.Append(")").AppendLine()
        Using stringBuilder.Indent()
            If foreignKey.IsUnique Then
                stringBuilder.Append(".WithOne(")
                If foreignKey.PrincipalToDependent IsNot Nothing Then
                    stringBuilder.Append(VBCode.Literal(foreignKey.PrincipalToDependent.Name))
                End If

                stringBuilder.AppendLine(")").Append(".HasForeignKey(").Append(VBCode.Literal(foreignKey.DeclaringEntityType.Name)).Append(", ").Append(String.Join(", ", foreignKey.Properties.[Select](Function(p) VBCode.Literal(p.Name)))).Append(")")
                GenerateForeignKeyAnnotations(foreignKey, stringBuilder)
                If Not foreignKey.PrincipalKey Is foreignKey.PrincipalEntityType.FindPrimaryKey() Then
                    stringBuilder.AppendLine().Append(".HasPrincipalKey(").Append(VBCode.Literal(foreignKey.PrincipalEntityType.Name)).Append(", ").Append(String.Join(", ", foreignKey.PrincipalKey.Properties.[Select](Function(p) VBCode.Literal(p.Name)))).Append(")")
                End If
            Else
                stringBuilder.Append(".WithMany(")
                If foreignKey.PrincipalToDependent IsNot Nothing Then
                    stringBuilder.Append(VBCode.Literal(foreignKey.PrincipalToDependent.Name))
                End If

                stringBuilder.AppendLine(")").Append(".HasForeignKey(").Append(String.Join(", ", foreignKey.Properties.[Select](Function(p) VBCode.Literal(p.Name)))).Append(")")
                GenerateForeignKeyAnnotations(foreignKey, stringBuilder)
                If Not foreignKey.PrincipalKey Is foreignKey.PrincipalEntityType.FindPrimaryKey() Then
                    stringBuilder.AppendLine().Append(".HasPrincipalKey(").Append(String.Join(", ", foreignKey.PrincipalKey.Properties.[Select](Function(p) VBCode.Literal(p.Name)))).Append(")")
                End If
            End If

            If foreignKey.DeleteBehavior <> DeleteBehavior.ClientSetNull Then
                stringBuilder.AppendLine().Append(".OnDelete(").Append(VBCode.Literal(CType(foreignKey.DeleteBehavior, [Enum]))).Append(")")
            End If
        End Using

    End Sub

    ''' <summary>
    '''     Generates code for the annotations on a foreign key.
    ''' </summary>
    ''' <param name="foreignKey"> The foreign key. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateForeignKeyAnnotations(foreignKey As IForeignKey,
                                                            stringBuilder As IndentedStringBuilder)
        Dim annotations = foreignKey.GetAnnotations().ToList()
        GenerateFluentApiForAnnotation(annotations, RelationalAnnotationNames.Name, If(foreignKey.IsUnique, NameOf(RelationalReferenceReferenceBuilderExtensions.HasConstraintName), NameOf(RelationalReferenceCollectionBuilderExtensions.HasConstraintName)), stringBuilder)
        GenerateAnnotations(annotations, stringBuilder)
    End Sub

    ''' <summary>
    '''     Removes ignored annotations.
    ''' </summary>
    ''' <param name="annotations"> The annotations to remove from. </param>
    ''' <param name="annotationNames"> The ignored annotation names. </param>
    Protected Overridable Sub IgnoreAnnotations(annotations As IList(Of IAnnotation),
                                                ParamArray annotationNames As String())
        For Each annotationName In annotationNames
            Dim annotation = annotations.FirstOrDefault(Function(a) a.Name = annotationName)
            If annotation IsNot Nothing Then
                annotations.Remove(annotation)
            End If
        Next
    End Sub

    ''' <summary>
    '''     Removes ignored annotations.
    ''' </summary>
    ''' <param name="annotations"> The annotations to remove from. </param>
    ''' <param name="annotationPrefixes"> The ignored annotation prefixes. </param>
    Protected Overridable Sub IgnoreAnnotationTypes(annotations As IList(Of IAnnotation),
                                                    ParamArray annotationPrefixes As String())
        For Each ignoreAnnotation In annotations.Where(Function(a) annotationPrefixes.Any(Function(pre) a.Name.StartsWith(pre, StringComparison.OrdinalIgnoreCase))).ToList()
            annotations.Remove(ignoreAnnotation)
        Next
    End Sub

    ''' <summary>
    '''     Generates code for annotations.
    ''' </summary>
    ''' <param name="annotations"> The annotations. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateAnnotations(annotations As IReadOnlyList(Of IAnnotation),
                                                  stringBuilder As IndentedStringBuilder)
        For Each annotation In annotations
            stringBuilder.AppendLine(" _")
            GenerateAnnotation(annotation, stringBuilder)
        Next
    End Sub

    ''' <summary>
    '''     Generates a Fluent API calls for an annotation.
    ''' </summary>
    ''' <param name="annotations"> The list of annotations. </param>
    ''' <param name="annotationName"> The name of the annotation to generate code for. </param>
    ''' <param name="fluentApiMethodName"> The Fluent API method name. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateFluentApiForAnnotation(ByRef annotations As List(Of IAnnotation),
                                                             annotationName As String,
                                                             fluentApiMethodName As String,
                                                             stringBuilder As IndentedStringBuilder)
        GenerateFluentApiForAnnotation(annotations, annotationName, Function(a) a?.Value, fluentApiMethodName, stringBuilder)
    End Sub

    ''' <summary>
    '''     Generates a Fluent API calls for an annotation.
    ''' </summary>
    ''' <param name="annotations"> The list of annotations. </param>
    ''' <param name="annotationName"> The name of the annotation to generate code for. </param>
    ''' <param name="annotationValueFunc"> A delegate to generate the value from the annotation. </param>
    ''' <param name="fluentApiMethodName"> The Fluent API method name. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateFluentApiForAnnotation(ByRef annotations As List(Of IAnnotation),
                                                             annotationName As String,
                                                             annotationValueFunc As Func(Of IAnnotation, Object),
                                                             fluentApiMethodName As String,
                                                             stringBuilder As IndentedStringBuilder)
        GenerateFluentApiForAnnotation(annotations, annotationName, annotationValueFunc, fluentApiMethodName, Nothing, stringBuilder)
    End Sub

    ''' <summary>
    '''     Generates a Fluent API calls for an annotation.
    ''' </summary>
    ''' <param name="annotations"> The list of annotations. </param>
    ''' <param name="annotationName"> The name of the annotation to generate code for. </param>
    ''' <param name="annotationValueFunc"> A delegate to generate the value from the annotation. </param>
    ''' <param name="fluentApiMethodName"> The Fluent API method name. </param>
    ''' <param name="genericTypesFunc"> A delegate to generate the generic types to use for the method call. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateFluentApiForAnnotation(ByRef annotations As List(Of IAnnotation),
                                                             annotationName As String,
                                                              annotationValueFunc As Func(Of IAnnotation, Object),
                                                              fluentApiMethodName As String,
                                                              genericTypesFunc As Func(Of IAnnotation, IReadOnlyList(Of Type)),
                                                              stringBuilder As IndentedStringBuilder)
        Dim annotation = annotations.FirstOrDefault(Function(a) a.Name = annotationName)
        Dim annotationValue = annotationValueFunc?.Invoke(annotation)
        Dim genericTypes = genericTypesFunc?.Invoke(annotation)
        Dim hasGenericTypes = genericTypes?.All(Function(t) t IsNot Nothing) = True
        If annotationValue IsNot Nothing OrElse hasGenericTypes Then
            stringBuilder.AppendLine(" _").Append(".").Append(fluentApiMethodName)
            If hasGenericTypes Then
                stringBuilder.Append("(Of ").Append(String.Join(", ", genericTypes.[Select](Function(t) VBCode.Reference(t)))).Append(")")
            End If

            stringBuilder.Append("(")
            If annotationValue IsNot Nothing Then
                stringBuilder.Append(VBCode.UnknownLiteral(annotationValue))
            End If

            stringBuilder.Append(")")
            annotations.Remove(annotation)
        End If
    End Sub

    ''' <summary>
    '''     Generates code for an annotation.
    ''' </summary>
    ''' <param name="annotation"> The annotation. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateAnnotation(annotation As IAnnotation,
                                                 stringBuilder As IndentedStringBuilder)
        stringBuilder.Append(".HasAnnotation(").Append(VBCode.Literal(annotation.Name)).Append(", ").Append(VBCode.UnknownLiteral(annotation.Value)).Append(")")
    End Sub

    ''' <summary>
    '''     Generates code for data seeding.
    ''' </summary>
    ''' <param name="properties"> The properties to generate. </param>
    ''' <param name="data"> The data to be seeded. </param>
    ''' <param name="stringBuilder"> The builder code Is added to. </param>
    Protected Overridable Sub GenerateSeedData(properties As IEnumerable(Of IProperty),
                                               data As IEnumerable(Of IDictionary(Of String, Object)),
                                               stringBuilder As IndentedStringBuilder)
        Dim dataList = data.ToList()
        If dataList.Count = 0 Then
            Return
        End If

        Dim propertiesToOutput = properties.ToList()
        stringBuilder.AppendLine().AppendLine($"b.{NameOf(EntityTypeBuilder.SeedData)}(").AppendLine("{")
        Using stringBuilder.Indent()
            Dim firstDatum = True
            For Each o In dataList
                If Not firstDatum Then
                    stringBuilder.AppendLine(",")
                Else
                    firstDatum = False
                End If

                stringBuilder.Append("New With { ")
                Dim firstProperty = True
                For Each [property] In propertiesToOutput
                    Dim value As Object = Nothing
                    If o.TryGetValue([property].Name, value) AndAlso value IsNot Nothing Then
                        If Not firstProperty Then
                            stringBuilder.Append(", ")
                        Else
                            firstProperty = False
                        End If

                        stringBuilder.Append(VBCode.Identifier([property].Name)).Append(" = ").Append(VBCode.UnknownLiteral(value))
                    End If
                Next

                stringBuilder.Append(" }")
            Next
        End Using

        stringBuilder.AppendLine().AppendLine("})")
    End Sub

End Class