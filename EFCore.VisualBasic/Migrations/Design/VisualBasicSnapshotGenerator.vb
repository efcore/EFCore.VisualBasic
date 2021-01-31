' Copyright (c) .NET Foundation. All rights reserved.
' Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Bricelam.EntityFrameworkCore.VisualBasic.Utilities
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Storage
Imports Microsoft.EntityFrameworkCore.Storage.ValueConversion
Imports Microsoft.EntityFrameworkCore.Utilities

Namespace Migrations.Design

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
            NotNull(dependencies, NameOf(dependencies))
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

            NotEmpty(builderName, NameOf(builderName))
            NotNull(model, NameOf(model))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim annotations = VisualBasicDependencies.
                                AnnotationCodeGenerator.
                                FilterIgnoredAnnotations(model.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            Dim productVersion = model.GetProductVersion()

            If annotations.Any() OrElse productVersion IsNot Nothing Then
                stringBuilder.Append(builderName)

                Using stringBuilder.Indent()

                    ' Temporary patch: specifically exclude some annotations which are known to produce identical Fluent API calls across different
                    ' providers, generating them as raw annotations instead.
                    Dim ambiguousAnnotations =
                        RemoveAmbiguousFluentApiAnnotations(
                            annotations,
                            Function(name)
                                Return name.EndsWith(":ValueGenerationStrategy", StringComparison.Ordinal) _
                                          OrElse name.EndsWith(":IdentityIncrement", StringComparison.Ordinal) _
                                          OrElse name.EndsWith(":IdentitySeed", StringComparison.Ordinal) _
                                          OrElse name.EndsWith(":HiLoSequenceName", StringComparison.Ordinal) _
                                          OrElse name.EndsWith(":HiLoSequenceSchema", StringComparison.Ordinal)
                            End Function)

                    For Each methodCallCodeFragment In VisualBasicDependencies.AnnotationCodeGenerator.GenerateFluentApiCalls(model, annotations)
                        stringBuilder.AppendLine().Append(VBCode.Fragment(methodCallCodeFragment))
                    Next

                    Dim remainingAnnotations As IEnumerable(Of IAnnotation) = annotations.Values
                    If productVersion IsNot Nothing Then
                        remainingAnnotations = remainingAnnotations.Append(New Annotation(CoreAnnotationNames.ProductVersion, productVersion))
                    End If

                    GenerateAnnotations(remainingAnnotations.Concat(ambiguousAnnotations), stringBuilder)
                End Using

                stringBuilder.AppendLine()

            End If

            For Each sequence In model.GetSequences()
                GenerateSequence(builderName, sequence, stringBuilder)
            Next

            GenerateEntityTypes(builderName, Sort(model.GetEntityTypes()), stringBuilder)

        End Sub

        Private Function Sort(entityTypes As IEnumerable(Of IEntityType)) As IReadOnlyList(Of IEntityType)

            Dim entityTypeGraph As New Multigraph(Of IEntityType, Integer)
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

            NotEmpty(builderName, NameOf(builderName))
            NotNull(entityTypes, NameOf(entityTypes))
            NotNull(stringBuilder, NameOf(stringBuilder))

            For Each entityType In entityTypes.Where(Function(e) e.FindOwnership() Is Nothing)
                stringBuilder.AppendLine()

                GenerateEntityType(builderName, entityType, stringBuilder)
            Next

            For Each entityType In entityTypes.Where(
                Function(e) e.FindOwnership() Is Nothing AndAlso
                            (e.GetDeclaredForeignKeys().Any() OrElse e.GetDeclaredReferencingForeignKeys().Any(Function(fk) fk.IsOwnership)))

                stringBuilder.AppendLine()
                GenerateEntityTypeRelationships(builderName, entityType, stringBuilder)
            Next

            For Each entityType In entityTypes.Where(
                Function(e) e.FindOwnership() Is Nothing AndAlso
                            e.GetDeclaredNavigations().Any(Function(n) Not n.IsOnDependent AndAlso Not n.ForeignKey.IsOwnership))

                stringBuilder.AppendLine()
                GenerateEntityTypeNavigations(builderName, entityType, stringBuilder)
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

            NotEmpty(builderName, NameOf(builderName))
            NotNull(entityType, NameOf(entityType))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim ownership = entityType.FindOwnership()
            Dim ownerNavigation = ownership?.PrincipalToDependent.Name

            Dim GetOwnedName = Function(Type As ITypeBase, simpleName As String, ownershipNavigation As String) As String
                                   Return Type.Name & "." & ownershipNavigation & "#" & simpleName
                               End Function

            Dim entityTypeName = entityType.Name
            If ownerNavigation IsNot Nothing AndAlso
               entityType.HasSharedClrType AndAlso
               entityTypeName = GetOwnedName(ownership.PrincipalEntityType, entityType.ClrType.ShortDisplayName(), ownerNavigation) Then
                entityTypeName = entityType.ClrType.DisplayName()
            End If

            stringBuilder.
                Append(builderName).
                Append(If(ownerNavigation IsNot Nothing, If(ownership.IsUnique, ".OwnsOne(", ".OwnsMany("), ".Entity(")).
                Append(VBCode.Literal(entityTypeName))

            If ownerNavigation IsNot Nothing Then
                stringBuilder.
                    Append(", ").
                    Append(VBCode.Literal(ownerNavigation))
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

            Using stringBuilder.Indent()
                stringBuilder.
                    AppendLine(",").
                    Append("Sub(").
                    Append(builderName).
                    AppendLine(")")

                Using stringBuilder.Indent()
                    GenerateBaseType(builderName, entityType.BaseType, stringBuilder)

                    GenerateProperties(builderName, entityType.GetDeclaredProperties(), stringBuilder)

                    GenerateKeys(builderName, entityType.GetDeclaredKeys(), entityType.FindDeclaredPrimaryKey(), stringBuilder)

                    GenerateIndexes(builderName, entityType.GetDeclaredIndexes(), stringBuilder)

                    GenerateEntityTypeAnnotations(builderName, entityType, stringBuilder)

                    GenerateCheckConstraints(builderName, entityType, stringBuilder)

                    If ownerNavigation IsNot Nothing Then
                        GenerateRelationships(builderName, entityType, stringBuilder)
                        GenerateNavigations(builderName, entityType.GetDeclaredNavigations().Where(Function(n) Not n.IsOnDependent AndAlso Not n.ForeignKey.IsOwnership), stringBuilder)
                    End If

                    GenerateData(builderName, entityType.GetProperties(), entityType.GetSeedData(providerValues:=True), stringBuilder)
                End Using

                stringBuilder.
                    AppendLine("End Sub)")
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
        ''' <param name="ownerships"> The foreign keys identifying each entity type. </param>
        ''' <param name="stringBuilder"> The builder code Is added to. </param>
        Protected Overridable Sub GenerateOwnedType(builderName As String,
                                                    ownerships As IEnumerable(Of IForeignKey),
                                                    stringBuilder As IndentedStringBuilder)

            NotNull(builderName, NameOf(builderName))
            NotNull(ownerships, NameOf(ownerships))
            NotNull(stringBuilder, NameOf(stringBuilder))

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
        ''' <param name="stringBuilder"> The builder code is added to. </param>
        Protected Overridable Sub GenerateOwnedType(builderName As String,
                                                    ownership As IForeignKey,
                                                    stringBuilder As IndentedStringBuilder)

            NotNull(builderName, NameOf(builderName))
            NotNull(ownership, NameOf(ownership))
            NotNull(stringBuilder, NameOf(stringBuilder))

            GenerateEntityType(builderName, ownership.DeclaringEntityType, stringBuilder)
        End Sub


        ''' <summary>
        '''     Generates code for the relationships of an <see cref="IEntityType"/>.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="entityType"> The entity type. </param>
        ''' <param name="stringBuilder"> The builder code Is added to. </param>
        Protected Overridable Sub GenerateEntityTypeRelationships(builderName As String,
                                                                  entityType As IEntityType,
                                                                  stringBuilder As IndentedStringBuilder)

            NotEmpty(builderName, NameOf(builderName))
            NotNull(entityType, NameOf(entityType))
            NotNull(stringBuilder, NameOf(stringBuilder))

            stringBuilder.
                Append(builderName).
                Append(".Entity(").
                Append(VBCode.Literal(entityType.Name)).
                AppendLine(",")

            Using stringBuilder.Indent()
                stringBuilder.AppendLine("Sub(b)")

                Using stringBuilder.Indent()
                    GenerateRelationships("b", entityType, stringBuilder)
                End Using

                stringBuilder.AppendLine("End Sub)")
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for the relationships of an <see cref="IEntityType"/>.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="entityType"> The entity type. </param>
        ''' <param name="stringBuilder"> The builder code Is added to. </param>
        Protected Overridable Sub GenerateRelationships(builderName As String,
                                                        entityType As IEntityType,
                                                        stringBuilder As IndentedStringBuilder)

            NotEmpty(builderName, NameOf(builderName))
            NotNull(entityType, NameOf(entityType))
            NotNull(stringBuilder, NameOf(stringBuilder))

            GenerateForeignKeys(builderName, entityType.GetDeclaredForeignKeys(), stringBuilder)

            GenerateOwnedTypes(builderName, entityType.GetDeclaredReferencingForeignKeys().Where(Function(fk) fk.IsOwnership), stringBuilder)

            GenerateNavigations(builderName, entityType.GetDeclaredNavigations().
                                             Where(Function(n) n.IsOnDependent OrElse (Not n.IsOnDependent AndAlso n.ForeignKey.IsOwnership)), stringBuilder)
        End Sub

        ''' <summary>
        '''     Generates code for the base type of an <see cref="IEntityType"/>.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="baseType"> The base entity type. </param>
        ''' <param name="stringBuilder"> The builder code Is added to. </param>
        Protected Overridable Sub GenerateBaseType(builderName As String,
                                                   baseType As IEntityType,
                                                   stringBuilder As IndentedStringBuilder)

            NotNull(builderName, NameOf(builderName))
            'baseType can be nothing
            NotNull(stringBuilder, NameOf(stringBuilder))

            If baseType IsNot Nothing Then
                stringBuilder.
                AppendLine().
                Append(builderName).
                Append(".HasBaseType(").
                Append(VBCode.Literal(baseType.Name)).
                AppendLine(")")
            End If
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref= "ISequence"/>.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="sequence"> The sequence. </param>
        ''' <param name="stringBuilder"> The builder code is added to. </param>
        Protected Overridable Sub GenerateSequence(builderName As String,
                                                   sequence As ISequence,
                                                   stringBuilder As IndentedStringBuilder)

            stringBuilder.
                AppendLine().Append(builderName).
                Append(".HasSequence")

            If sequence.Type <> Internal.Sequence.DefaultClrType Then
                stringBuilder.
                    Append("<").
                    Append(VBCode.Reference(sequence.Type)).
                    Append(">")
            End If

            stringBuilder.
                Append("(").
                Append(VBCode.Literal(sequence.Name))

            If Not String.IsNullOrEmpty(sequence.Schema) AndAlso
               sequence.Model.GetDefaultSchema() <> sequence.Schema Then

                stringBuilder.
                    Append(", ").
                    Append(VBCode.Literal(sequence.Schema))
            End If

            stringBuilder.Append(")")

            Using stringBuilder.Indent()
                If sequence.StartValue <> Internal.Sequence.DefaultStartValue Then
                    stringBuilder.
                        AppendLine(".").
                        Append("StartsAt(").
                        Append(VBCode.Literal(sequence.StartValue)).
                        Append(")")
                End If

                If sequence.IncrementBy <> Internal.Sequence.DefaultIncrementBy Then
                    stringBuilder.
                        AppendLine(".").
                        Append("IncrementsBy(").
                        Append(VBCode.Literal(sequence.IncrementBy)).
                        Append(")")
                End If

                If sequence.MinValue <> Internal.Sequence.DefaultMinValue Then
                    stringBuilder.
                        AppendLine(".").
                        Append("HasMin(").
                        Append(VBCode.Literal(sequence.MinValue)).
                        Append(")")
                End If

                If sequence.MaxValue <> Internal.Sequence.DefaultMaxValue Then
                    stringBuilder.
                        AppendLine(".").
                        Append("HasMax(").
                        Append(VBCode.Literal(sequence.MaxValue)).
                        Append(")")
                End If

                If sequence.IsCyclic <> Internal.Sequence.DefaultIsCyclic Then
                    stringBuilder.
                        AppendLine().Append(".IsCyclic()")
                End If
            End Using

            stringBuilder.AppendLine("")
        End Sub


        ''' <summary>
        '''     Generates code for <see cref="IProperty"/> objects.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="properties"> The properties. </param>
        ''' <param name="stringBuilder"> The builder code Is added to. </param>
        Protected Overridable Sub GenerateProperties(builderName As String,
                                                     properties As IEnumerable(Of IProperty),
                                                     stringBuilder As IndentedStringBuilder)

            NotNull(builderName, NameOf(builderName))
            NotNull(properties, NameOf(properties))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim firstProperty = True
            For Each [property] In properties
                If Not firstProperty Then
                    stringBuilder.AppendLine()
                Else
                    firstProperty = False
                End If

                GenerateProperty(builderName, [property], stringBuilder)
                stringBuilder.AppendLine()
            Next
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="IProperty"/>.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="property"> The property. </param>
        ''' <param name="stringBuilder"> The builder code Is added to. </param>
        Protected Overridable Sub GenerateProperty(builderName As String,
                                                   [property] As IProperty,
                                                   stringBuilder As IndentedStringBuilder)

            NotNull(builderName, NameOf(builderName))
            NotNull([property], NameOf([property]))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim clrType = If(FindValueConverter([property])?.ProviderClrType.MakeNullable([property].IsNullable), [property].ClrType)

            stringBuilder.
            Append(builderName).
            Append(".Property(Of ").
            Append(VBCode.Reference(clrType)).
            Append(")(").
            Append(VBCode.Literal([property].Name)).
            Append(")")

            Using stringBuilder.Indent()
                If [property].IsConcurrencyToken Then
                    stringBuilder.
                        AppendLine(".").
                        Append("IsConcurrencyToken()")
                End If

                If [property].IsNullable <> (clrType.IsNullableType() AndAlso Not [property].IsPrimaryKey()) Then
                    stringBuilder.
                        AppendLine(".").
                        Append("IsRequired()")
                End If

                Select Case [property].ValueGenerated
                    Case ValueGenerated.Never

                    Case ValueGenerated.OnAdd
                        stringBuilder.
                            AppendLine(".").
                            Append("ValueGeneratedOnAdd()")

                    Case ValueGenerated.OnUpdate
                        stringBuilder.
                           AppendLine(".").
                           Append("ValueGeneratedOnUpdate()")

                    Case ValueGenerated.OnUpdateSometimes
                        stringBuilder.
                           AppendLine(".").
                           Append("ValueGeneratedOnUpdateSometimes()")

                    Case Else
                        stringBuilder.
                           AppendLine(".").
                           Append("ValueGeneratedOnAddOrUpdate()")
                End Select

                GeneratePropertyAnnotations([property], stringBuilder)
            End Using

        End Sub

        ''' <summary>
        '''     Generates code for the annotations on an <see cref="IProperty"/>.
        ''' </summary>
        ''' <param name="property"> The property. </param>
        ''' <param name="stringBuilder"> The builder code Is added to. </param>
        Protected Overridable Sub GeneratePropertyAnnotations([property] As IProperty,
                                                              stringBuilder As IndentedStringBuilder)

            NotNull([property], NameOf([property]))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim annotations = VisualBasicDependencies.AnnotationCodeGenerator.
                                FilterIgnoredAnnotations([property].GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            GenerateFluentApiForMaxLength([property], stringBuilder)
            GenerateFluentApiForPrecisionAndScale([property], stringBuilder)
            GenerateFluentApiForIsUnicode([property], stringBuilder)

            stringBuilder.
                AppendLine(".").
                Append(NameOf(RelationalPropertyBuilderExtensions.HasColumnType)).
                Append("(").
                Append(VBCode.Literal(If([property].GetColumnType(), VisualBasicDependencies.RelationalTypeMappingSource.GetMapping([property]).StoreType))).
                Append(")")

            annotations.Remove(RelationalAnnotationNames.ColumnType)

            GenerateFluentApiForDefaultValue([property], stringBuilder)
            annotations.Remove(RelationalAnnotationNames.DefaultValue)

            ' Temporary patch: specifically exclude some annotations which are known to produce identical Fluent API calls across different
            ' providers, generating them as raw annotations instead.
            Dim ambiguousAnnotations = RemoveAmbiguousFluentApiAnnotations(
                annotations,
                Function(name)
                    Return name.EndsWith(":ValueGenerationStrategy", StringComparison.Ordinal) _
                        OrElse name.EndsWith(":IdentityIncrement", StringComparison.Ordinal) _
                        OrElse name.EndsWith(":IdentitySeed", StringComparison.Ordinal) _
                        OrElse name.EndsWith(":HiLoSequenceName", StringComparison.Ordinal) _
                        OrElse name.EndsWith(":HiLoSequenceSchema", StringComparison.Ordinal)
                End Function
            )

            For Each methodCallCodeFragment In VisualBasicDependencies.AnnotationCodeGenerator.GenerateFluentApiCalls([property], annotations)
                stringBuilder.
                    AppendLine(" _").
                    Append(VBCode.Fragment(methodCallCodeFragment))
            Next

            GenerateAnnotations(annotations.Values.Concat(ambiguousAnnotations), stringBuilder)
        End Sub

        Private Function FindValueConverter([property] As IProperty) As ValueConverter
            Dim t = If([property].FindTypeMapping(), VisualBasicDependencies.RelationalTypeMappingSource.FindMapping([property]))
            Return If([property].GetValueConverter(), t?.Converter)
        End Function


        ''' <summary>
        '''     Generates code for <see cref="IKey"/> objects.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="keys"> The keys. </param>
        ''' <param name="primaryKey"> The primary key. </param>
        ''' <param name="stringBuilder"> The builder code Is added to. </param>
        Protected Overridable Sub GenerateKeys(
        builderName As String, keys As IEnumerable(Of IKey),
        primaryKey As IKey,
        stringBuilder As IndentedStringBuilder)

            NotNull(builderName, NameOf(builderName))
            NotNull(keys, NameOf(keys))
            NotNull(stringBuilder, NameOf(stringBuilder))

            If primaryKey IsNot Nothing Then
                GenerateKey(builderName, primaryKey, stringBuilder, primary:=True)
            End If

            If primaryKey?.DeclaringEntityType.IsOwned() <> True Then
                For Each key In keys.Where(Function(k)
                                               Return k IsNot primaryKey AndAlso
                                                      (Not k.GetReferencingForeignKeys().Any() OrElse
                                                       k.GetAnnotations().Any(Function(a) a.Name <> RelationalAnnotationNames.UniqueConstraintMappings))
                                           End Function)
                    GenerateKey(builderName, key, stringBuilder)
                Next
            End If
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="IKey"/>.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="key"> The key. </param>
        ''' <param name="stringBuilder"> The builder code Is added to. </param>
        ''' <param name="primary">A value indicating whether the key Is primary. </param>
        Protected Overridable Sub GenerateKey(builderName As String,
                                              key As IKey,
                                              stringBuilder As IndentedStringBuilder,
                                              Optional primary As Boolean = False)

            NotNull(builderName, NameOf(builderName))
            NotNull(key, NameOf(key))
            NotNull(stringBuilder, NameOf(stringBuilder))

            stringBuilder.
                AppendLine().
                Append(builderName).
                Append(If(primary, ".HasKey(", ".HasAlternateKey(")).
                Append(String.Join(", ", key.Properties.Select(Function(p) VBCode.Literal(p.Name)))).
                AppendLine(")")

            Using stringBuilder.Indent()
                GenerateKeyAnnotations(key, stringBuilder)
            End Using

        End Sub

        ''' <summary>
        '''     Generates code for the annotations on a key.
        ''' </summary>
        ''' <param name="key"> The key. </param>
        ''' <param name="stringBuilder"> The builder code is added to. </param>
        Protected Overridable Sub GenerateKeyAnnotations(key As IKey,
                                                         stringBuilder As IndentedStringBuilder)

            Dim annotations = VisualBasicDependencies.AnnotationCodeGenerator.
                                FilterIgnoredAnnotations(key.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            For Each methodCallCodeFragment In VisualBasicDependencies.AnnotationCodeGenerator.GenerateFluentApiCalls(key, annotations)
                stringBuilder.
                    AppendLine().Append(VBCode.Fragment(methodCallCodeFragment))
            Next

            GenerateAnnotations(annotations.Values, stringBuilder)
        End Sub

        ''' <summary>
        '''     Generates code for <see cref="IIndex"/> objects.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="indexes"> The indexes. </param>
        ''' <param name="stringBuilder"> The builder code Is added to. </param>
        Protected Overridable Sub GenerateIndexes(
        builderName As String,
        indexes As IEnumerable(Of IIndex),
        stringBuilder As IndentedStringBuilder)

            NotNull(builderName, NameOf(builderName))
            NotNull(indexes, NameOf(indexes))
            NotNull(stringBuilder, NameOf(stringBuilder))

            For Each index In indexes
                stringBuilder.AppendLine()
                GenerateIndex(builderName, index, stringBuilder)
            Next
        End Sub

        ''' <summary>
        '''     Generates code an <see cref="IIndex"/>.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="index"> The index. </param>
        ''' <param name="stringBuilder"> The builder code Is added to. </param>
        Protected Overridable Sub GenerateIndex(builderName As String,
                                                index As IIndex,
                                                stringBuilder As IndentedStringBuilder)

            NotNull(builderName, NameOf(builderName))
            NotNull(index, NameOf(index))
            NotNull(stringBuilder, NameOf(stringBuilder))

            ' Note - method names below are meant to be hard-coded
            ' because old snapshot files will fail if they are changed
            stringBuilder.
                Append(builderName).
                Append(".HasIndex(")

            If index.Name Is Nothing Then
                stringBuilder.
                    Append(String.Join(", ", index.Properties.Select(Function(p) VBCode.Literal(p.Name))))
            Else
                stringBuilder.
                    Append("{ ").
                    Append(String.Join(", ", index.Properties.Select(Function(p) VBCode.Literal(p.Name)))).
                    Append(" }, ").
                    Append(VBCode.Literal(index.Name))
            End If

            stringBuilder.
                Append(")")

            Using stringBuilder.Indent()
                If index.IsUnique Then
                    stringBuilder.
                        AppendLine().
                        Append(".IsUnique()")
                End If

                GenerateIndexAnnotations(index, stringBuilder)
            End Using

            stringBuilder.
                AppendLine()

        End Sub

        ''' <summary>
        '''     Generates code for the annotations on an index.
        ''' </summary>
        ''' <param name="index"> The index. </param>
        ''' <param name="stringBuilder"> The builder code is added to. </param>
        Protected Overridable Sub GenerateIndexAnnotations(index As IIndex,
                                                           stringBuilder As IndentedStringBuilder)

            Dim annotations = VisualBasicDependencies.AnnotationCodeGenerator.
                                FilterIgnoredAnnotations(index.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            ' Temporary patch: specifically exclude some annotations which are known to produce identical Fluent API calls across different
            ' providers, generating them as raw annotations instead.
            Dim ambiguousAnnotations = RemoveAmbiguousFluentApiAnnotations(
                annotations,
                Function(name) name.EndsWith(":Include", StringComparison.Ordinal))

            For Each methodCallCodeFragment In VisualBasicDependencies.AnnotationCodeGenerator.GenerateFluentApiCalls(index, annotations)
                stringBuilder.
                AppendLine().
                Append(VBCode.Fragment(methodCallCodeFragment))
            Next

            GenerateAnnotations(annotations.Values.Concat(ambiguousAnnotations), stringBuilder)
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

            NotNull(builderName, NameOf(builderName))
            NotNull(entityType, NameOf(entityType))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim annotationList = entityType.GetAnnotations().ToList()

            Dim discriminatorPropertyAnnotation = annotationList.FirstOrDefault(Function(a) a.Name = CoreAnnotationNames.DiscriminatorProperty)
            Dim discriminatorMappingCompleteAnnotation = annotationList.FirstOrDefault(Function(a) a.Name = CoreAnnotationNames.DiscriminatorMappingComplete)
            Dim discriminatorValueAnnotation = annotationList.FirstOrDefault(Function(a) a.Name = CoreAnnotationNames.DiscriminatorValue)

            Dim annotations = VisualBasicDependencies.AnnotationCodeGenerator.
                                FilterIgnoredAnnotations(entityType.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            Dim tableNameAnnotation = annotations.Find(RelationalAnnotationNames.TableName)

            If tableNameAnnotation?.Value IsNot Nothing OrElse entityType.BaseType Is Nothing Then
                Dim tableName = If(CStr(tableNameAnnotation?.Value), entityType.GetTableName())
                If tableName IsNot Nothing Then
                    stringBuilder.
                        AppendLine().Append(builderName).
                        Append(".ToTable(").
                        Append(VBCode.Literal(tableName))

                    If tableNameAnnotation IsNot Nothing Then
                        annotations.Remove(tableNameAnnotation.Name)
                    End If

                    Dim schemaAnnotation = annotations.Find(RelationalAnnotationNames.Schema)

                    If schemaAnnotation?.Value IsNot Nothing Then
                        stringBuilder.
                            Append(", ").
                            Append(VBCode.Literal(CStr(schemaAnnotation.Value)))

                        annotations.Remove(schemaAnnotation.Name)
                    End If

                    Dim isExcludedAnnotation = annotations.Find(RelationalAnnotationNames.IsTableExcludedFromMigrations)
                    If isExcludedAnnotation IsNot Nothing Then
                        If CType(isExcludedAnnotation.Value, Boolean?) = True Then
                            If entityType.IsOwned() Then
                                ' Issue #23173
                                stringBuilder.Append(", excludedFromMigrations:= true")
                            Else
                                stringBuilder.Append(", Function(t) t.ExcludeFromMigrations()")
                            End If
                        End If

                        annotations.Remove(isExcludedAnnotation.Name)
                    End If

                    stringBuilder.AppendLine(")")
                End If
            End If

            Dim viewNameAnnotation = annotations.Find(RelationalAnnotationNames.ViewName)
            If viewNameAnnotation?.Value IsNot Nothing OrElse entityType.BaseType Is Nothing Then
                Dim viewName = If(CStr(viewNameAnnotation?.Value), entityType.GetViewName())
                If viewName IsNot Nothing Then
                    stringBuilder.
                    AppendLine().Append(builderName).
                    Append(".ToView(").
                    Append(VBCode.Literal(viewName))

                    If viewNameAnnotation IsNot Nothing Then
                        annotations.Remove(viewNameAnnotation.Name)
                    End If

                    Dim viewSchemaAnnotation = annotations.Find(RelationalAnnotationNames.ViewSchema)
                    If viewSchemaAnnotation?.Value IsNot Nothing Then
                        stringBuilder.
                            Append(", ").
                            Append(VBCode.Literal(CStr(viewSchemaAnnotation.Value)))

                        annotations.Remove(viewSchemaAnnotation.Name)
                    End If

                    stringBuilder.AppendLine(")")
                End If
            End If

            Dim functionNameAnnotation = annotations.Find(RelationalAnnotationNames.FunctionName)
            If functionNameAnnotation?.Value IsNot Nothing OrElse entityType.BaseType Is Nothing Then
                Dim functionName = If(CStr(functionNameAnnotation?.Value), entityType.GetFunctionName())
                If functionName IsNot Nothing Then
                    stringBuilder.
                        AppendLine().
                        Append(builderName).
                        Append(".ToFunction(").
                        Append(VBCode.Literal(functionName))

                    If functionNameAnnotation IsNot Nothing Then
                        annotations.Remove(functionNameAnnotation.Name)
                    End If

                    stringBuilder.AppendLine(")")
                End If
            End If

            If If(discriminatorPropertyAnnotation?.Value,
                    If(discriminatorMappingCompleteAnnotation?.Value,
                        discriminatorValueAnnotation?.Value)) IsNot Nothing Then

                stringBuilder.
                    AppendLine().
                    Append(builderName).
                    Append(".").
                    Append("HasDiscriminator")

                If discriminatorPropertyAnnotation?.Value IsNot Nothing Then
                    Dim discriminatorProperty = entityType.FindProperty(CStr(discriminatorPropertyAnnotation.Value))
                    Dim propertyClrType = If(FindValueConverter(discriminatorProperty)?.
                                                    ProviderClrType.
                                                    MakeNullable(discriminatorProperty.IsNullable),
                                                discriminatorProperty.ClrType)

                    stringBuilder.
                        Append("(Of ").
                        Append(VBCode.Reference(propertyClrType)).
                        Append(")(").
                        Append(VBCode.Literal(CStr(discriminatorPropertyAnnotation.Value))).
                        Append(")")
                Else
                    stringBuilder.
                        Append("()")
                End If

                If discriminatorMappingCompleteAnnotation?.Value IsNot Nothing Then
                    Dim value = discriminatorMappingCompleteAnnotation.Value

                    stringBuilder.
                        Append(".").
                        Append("IsComplete").
                        Append("(").
                        Append(VBCode.UnknownLiteral(value)).
                        Append(")")
                End If

                If discriminatorValueAnnotation?.Value IsNot Nothing Then
                    Dim value = discriminatorValueAnnotation.Value
                    Dim discriminatorProperty = entityType.GetDiscriminatorProperty()

                    If discriminatorProperty IsNot Nothing Then
                        Dim valueConverter = FindValueConverter(discriminatorProperty)
                        If valueConverter IsNot Nothing Then
                            value = valueConverter.ConvertToProvider(value)
                        End If
                    End If

                    stringBuilder.
                        Append(".").
                        Append("HasValue").
                        Append("(").
                        Append(VBCode.UnknownLiteral(value)).
                        Append(")")
                End If

                stringBuilder.AppendLine()
            End If

            Dim fluentApiCalls = VisualBasicDependencies.AnnotationCodeGenerator.GenerateFluentApiCalls(entityType, annotations)
            If fluentApiCalls.Count > 0 OrElse annotations.Count > 0 Then

                stringBuilder.
                    AppendLine().
                    Append(builderName)

                Using stringBuilder.Indent()
                    For Each methodCallCodeFragment In fluentApiCalls
                        stringBuilder.
                            AppendLine().
                            Append(VBCode.Fragment(methodCallCodeFragment))
                    Next

                    GenerateAnnotations(annotations.Values, stringBuilder)

                    stringBuilder.
                        AppendLine("")
                End Using
            End If

        End Sub

        ''' <summary>
        '''     Generates code for <see cref= "ICheckConstraint"/> objects.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="entityType"> The entity type. </param>
        ''' <param name="stringBuilder"> The builder code is added to. </param>
        Protected Overridable Sub GenerateCheckConstraints(
                                                           builderName As String,
                                                           entityType As IEntityType,
                                                           stringBuilder As IndentedStringBuilder)

            NotNull(builderName, NameOf(builderName))
            NotNull(entityType, NameOf(entityType))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim constraintsForEntity = entityType.GetCheckConstraints()

            For Each checkConstraint In constraintsForEntity
                stringBuilder.AppendLine()

                GenerateCheckConstraint(builderName, checkConstraint, stringBuilder)
            Next
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref= "ICheckConstraint"/>.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="checkConstraint"> The check constraint. </param>
        ''' <param name="stringBuilder"> The builder code is added to. </param>
        Protected Overridable Sub GenerateCheckConstraint(builderName As String,
                                                          checkConstraint As ICheckConstraint,
                                                          stringBuilder As IndentedStringBuilder)

            NotNull(builderName, NameOf(builderName))
            NotNull(checkConstraint, NameOf(checkConstraint))
            NotNull(stringBuilder, NameOf(stringBuilder))

            stringBuilder.
                Append(builderName).
                Append(".HasCheckConstraint(").
                Append(VBCode.Literal(checkConstraint.Name)).
                Append(", ").
                Append(VBCode.Literal(checkConstraint.Sql)).
                AppendLine(")")

        End Sub

        ''' <summary>
        '''     Generates code for <see cref="IForeignKey"/> objects.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="foreignKeys"> The foreign keys. </param>
        ''' <param name="stringBuilder"> The builder code Is added to. </param>
        Protected Overridable Sub GenerateForeignKeys(builderName As String,
                                                      foreignKeys As IEnumerable(Of IForeignKey),
                                                      stringBuilder As IndentedStringBuilder)

            NotNull(builderName, NameOf(builderName))
            NotNull(foreignKeys, NameOf(foreignKeys))
            NotNull(stringBuilder, NameOf(stringBuilder))

            For Each foreignKey In foreignKeys
                stringBuilder.AppendLine()
                GenerateForeignKey(builderName, foreignKey, stringBuilder)
            Next
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="IForeignKey"/>.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="foreignKey"> The foreign key. </param>
        ''' <param name="stringBuilder"> The builder code Is added to. </param>
        Protected Overridable Sub GenerateForeignKey(builderName As String,
                                                     foreignKey As IForeignKey,
                                                     stringBuilder As IndentedStringBuilder)

            NotNull(builderName, NameOf(builderName))
            NotNull(foreignKey, NameOf(foreignKey))
            NotNull(stringBuilder, NameOf(stringBuilder))

            If Not foreignKey.IsOwnership Then
                stringBuilder.
                    Append(builderName).
                    Append(".HasOne(").
                    Append(VBCode.Literal(foreignKey.PrincipalEntityType.Name)).
                    Append(", ").
                    Append(If(foreignKey.DependentToPrincipal Is Nothing,
                                VBCode.UnknownLiteral(Nothing),
                                VBCode.Literal(foreignKey.DependentToPrincipal.Name))
                          )
            Else
                stringBuilder.
                    Append(builderName).
                    Append(".WithOwner(")

                If foreignKey.DependentToPrincipal IsNot Nothing Then
                    stringBuilder.
                        Append(VBCode.Literal(foreignKey.DependentToPrincipal.Name))
                End If
            End If

            stringBuilder.
                Append(")")

            Using stringBuilder.Indent()
                If foreignKey.IsUnique AndAlso Not foreignKey.IsOwnership Then
                    stringBuilder.
                        AppendLine(".").
                        Append("WithOne(")

                    If foreignKey.PrincipalToDependent IsNot Nothing Then
                        stringBuilder.
                            Append(VBCode.Literal(foreignKey.PrincipalToDependent.Name))
                    End If

                    stringBuilder.
                        AppendLine(").").
                        Append("HasForeignKey(").
                        Append(VBCode.Literal(foreignKey.DeclaringEntityType.Name)).
                        Append(", ").
                        Append(String.Join(", ", foreignKey.Properties.[Select](Function(p) VBCode.Literal(p.Name)))).
                        Append(")")

                    GenerateForeignKeyAnnotations(foreignKey, stringBuilder)

                    If foreignKey.PrincipalKey IsNot foreignKey.PrincipalEntityType.FindPrimaryKey() Then
                        stringBuilder.
                            AppendLine(".").
                            Append("HasPrincipalKey(").
                            Append(VBCode.Literal(foreignKey.PrincipalEntityType.Name)).
                            Append(", ").
                            Append(String.Join(", ", foreignKey.PrincipalKey.Properties.[Select](Function(p) VBCode.Literal(p.Name)))).
                            Append(")")
                    End If

                Else
                    If Not foreignKey.IsOwnership Then

                        stringBuilder.
                            AppendLine(".").
                            Append("WithMany(")

                        If foreignKey.PrincipalToDependent IsNot Nothing Then
                            stringBuilder.Append(VBCode.Literal(foreignKey.PrincipalToDependent.Name))
                        End If

                        stringBuilder.Append(")")
                    End If

                    stringBuilder.
                        AppendLine(".").
                        Append("HasForeignKey(").
                        Append(String.Join(", ", foreignKey.Properties.[Select](Function(p) VBCode.Literal(p.Name)))).
                        Append(")")

                    GenerateForeignKeyAnnotations(foreignKey, stringBuilder)

                    If foreignKey.PrincipalKey IsNot foreignKey.PrincipalEntityType.FindPrimaryKey() Then
                        stringBuilder.
                            AppendLine(".").
                            Append("HasPrincipalKey(").
                            Append(String.Join(", ", foreignKey.PrincipalKey.Properties.[Select](Function(p) VBCode.Literal(p.Name)))).
                            Append(")")
                    End If
                End If

                If Not foreignKey.IsOwnership Then
                    If foreignKey.DeleteBehavior <> DeleteBehavior.ClientSetNull Then
                        stringBuilder.
                            AppendLine(".").
                            Append("OnDelete(").
                            Append(VBCode.Literal(DirectCast(foreignKey.DeleteBehavior, [Enum]))).
                            Append(")")
                    End If

                    If foreignKey.IsRequired Then
                        stringBuilder.
                            AppendLine(".").
                            Append("IsRequired()")
                    End If
                End If
            End Using

            stringBuilder.AppendLine()
        End Sub

        ''' <summary>
        '''     Generates code for the annotations on a foreign key.
        ''' </summary>
        ''' <param name="foreignKey"> The foreign key. </param>
        ''' <param name="stringBuilder"> The builder code Is added to. </param>
        Protected Overridable Sub GenerateForeignKeyAnnotations(foreignKey As IForeignKey,
                                                                stringBuilder As IndentedStringBuilder)

            NotNull(foreignKey, NameOf(foreignKey))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim annotations = VisualBasicDependencies.AnnotationCodeGenerator.
                FilterIgnoredAnnotations(foreignKey.GetAnnotations()).
                ToDictionary(Function(a) a.Name, Function(a) a)

            For Each methodCallCodeFragment In VisualBasicDependencies.AnnotationCodeGenerator.GenerateFluentApiCalls(foreignKey, annotations)
                stringBuilder.
                    AppendLine().
                    Append(VBCode.Fragment(methodCallCodeFragment))
            Next

            GenerateAnnotations(annotations.Values, stringBuilder)
        End Sub


        ''' <summary>
        '''     Generates code for the navigations of an <see cref= "IEntityType"/>.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="entityType"> The entity type. </param>
        ''' <param name="stringBuilder"> The builder code is added to. </param>
        Protected Overridable Sub GenerateEntityTypeNavigations(builderName As String,
                                                                entityType As IEntityType,
                                                                stringBuilder As IndentedStringBuilder)

            NotEmpty(builderName, NameOf(builderName))
            NotNull(entityType, NameOf(entityType))
            NotNull(stringBuilder, NameOf(stringBuilder))

            stringBuilder.
                Append(builderName).
                Append(".Entity(").
                Append(VBCode.Literal(entityType.Name)).
                AppendLine(",")

            Using stringBuilder.Indent()

                stringBuilder.AppendLine("Sub(b)")

                Using stringBuilder.Indent()
                    GenerateNavigations("b", entityType.GetDeclaredNavigations().
                                                Where(Function(n) Not n.IsOnDependent AndAlso
                                                                  Not n.ForeignKey.IsOwnership), stringBuilder)
                End Using

                stringBuilder.AppendLine("End Sub)")
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for <see cref= "INavigation"/> objects.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="navigations"> The navigations. </param>
        ''' <param name="stringBuilder"> The builder code is added to. </param>
        Protected Overridable Sub GenerateNavigations(builderName As String,
                                                      navigations As IEnumerable(Of INavigation),
                                                      stringBuilder As IndentedStringBuilder)

            NotNull(builderName, NameOf(builderName))
            NotNull(navigations, NameOf(navigations))
            NotNull(stringBuilder, NameOf(stringBuilder))

            For Each navigation In navigations
                stringBuilder.AppendLine()
                GenerateNavigation(builderName, navigation, stringBuilder)
            Next
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref= "INavigation"/>.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="navigation"> The navigation. </param>
        ''' <param name="stringBuilder"> The builder code is added to. </param>
        Protected Overridable Sub GenerateNavigation(builderName As String,
                                                     navigation As INavigation,
                                                     stringBuilder As IndentedStringBuilder)

            NotNull(builderName, NameOf(builderName))
            NotNull(navigation, NameOf(navigation))
            NotNull(stringBuilder, NameOf(stringBuilder))

            stringBuilder.
                Append(builderName).
                Append(".Navigation(").
                Append(VBCode.Literal(navigation.Name)).
                Append(")")

            Using stringBuilder.Indent()
                If Not navigation.IsOnDependent AndAlso
                   Not navigation.IsCollection AndAlso
                   navigation.ForeignKey.IsRequiredDependent Then

                    stringBuilder.
                        AppendLine(".").
                        Append("IsRequired()")
                End If

                GenerateNavigationAnnotations(navigation, stringBuilder)
            End Using

            stringBuilder.AppendLine()
        End Sub

        ''' <summary>
        '''     Generates code for the annotations on a navigation.
        ''' </summary>
        ''' <param name="navigation"> The navigation. </param>
        ''' <param name="stringBuilder"> The builder code is added to. </param>
        Protected Overridable Sub GenerateNavigationAnnotations(navigation As INavigation,
                                                                stringBuilder As IndentedStringBuilder)

            NotNull(navigation, NameOf(navigation))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim annotations = VisualBasicDependencies.AnnotationCodeGenerator.
                                FilterIgnoredAnnotations(navigation.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            For Each methodCallCodeFragment In VisualBasicDependencies.AnnotationCodeGenerator.GenerateFluentApiCalls(navigation, annotations)
                stringBuilder.
                    AppendLine().Append(VBCode.Fragment(methodCallCodeFragment))
            Next

            GenerateAnnotations(annotations.Values, stringBuilder)
        End Sub

        ''' <summary>
        '''     Generates code for annotations.
        ''' </summary>
        ''' <param name="annotations"> The annotations. </param>
        ''' <param name="stringBuilder"> The builder code Is added to. </param>
        Protected Overridable Sub GenerateAnnotations(annotations As IEnumerable(Of IAnnotation),
                                                      stringBuilder As IndentedStringBuilder)

            NotNull(annotations, NameOf(annotations))
            NotNull(stringBuilder, NameOf(stringBuilder))

            For Each annotation In annotations
                stringBuilder.AppendLine(".")
                GenerateAnnotation(annotation, stringBuilder)
            Next
        End Sub

        ''' <summary>
        '''     Generates code for an annotation.
        ''' </summary>
        ''' <param name="annotation"> The annotation. </param>
        ''' <param name="stringBuilder"> The builder code Is added to. </param>
        Protected Overridable Sub GenerateAnnotation(
        annotation As IAnnotation,
        stringBuilder As IndentedStringBuilder)

            stringBuilder.
            Append("HasAnnotation(").
            Append(VBCode.Literal(annotation.Name)).
            Append(", ").
            Append(VBCode.UnknownLiteral(annotation.Value)).
            Append(")")
        End Sub

        ''' <summary>
        '''     Generates code for data seeding.
        ''' </summary>
        ''' <param name="builderName"> The name of the builder variable. </param>
        ''' <param name="properties"> The properties to generate. </param>
        ''' <param name="data"> The data to be seeded. </param>
        ''' <param name="stringBuilder"> The builder code is added to. </param>
        Protected Overridable Sub GenerateData(builderName As String,
                                               properties As IEnumerable(Of IProperty),
                                               data As IEnumerable(Of IDictionary(Of String, Object)),
                                               stringBuilder As IndentedStringBuilder)

            NotNull(properties, NameOf(properties))
            NotNull(data, NameOf(data))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim dataList = data.ToList()
            If dataList.Count = 0 Then
                Return
            End If

            Dim propertiesToOutput = properties.ToList()

            stringBuilder.
                AppendLine().Append(builderName).
                Append(".").
                Append(NameOf(EntityTypeBuilder.HasData)).
                AppendLine("{")

            Using stringBuilder.Indent()
                Dim firstDatum As Boolean = True
                For Each o In dataList
                    If Not firstDatum Then
                        stringBuilder.AppendLine(",")
                    Else
                        firstDatum = False
                    End If

                    stringBuilder.Append("New With { ")

                    Using stringBuilder.Indent()
                        Dim firstProperty As Boolean = True
                        For Each [property] In propertiesToOutput
                            Dim value As Object = Nothing
                            If o.TryGetValue([property].Name, value) AndAlso value IsNot Nothing Then
                                If Not firstProperty Then
                                    stringBuilder.AppendLine(", ")
                                Else
                                    firstProperty = False
                                End If

                                stringBuilder.
                                    Append(VBCode.Identifier([property].Name)).
                                    Append(" = ").
                                    Append(VBCode.UnknownLiteral(value))
                            End If
                        Next

                        stringBuilder.AppendLine()
                    End Using

                    stringBuilder.Append(" }")
                Next
            End Using

            stringBuilder.
                AppendLine().
                AppendLine("})")
        End Sub

        Private Sub GenerateFluentApiForMaxLength([property] As IProperty,
                                                  stringBuilder As IndentedStringBuilder)

            NotNull([property], NameOf([property]))
            NotNull(stringBuilder, NameOf(stringBuilder))

            If Not [property].GetMaxLength().HasValue Then Exit Sub

            Dim maxLength As Integer = [property].GetMaxLength().Value

            stringBuilder.
                AppendLine(".").
                Append(NameOf(PropertyBuilder.HasMaxLength)).
                Append("(").
                Append(VBCode.Literal(maxLength)).
                Append(")")
        End Sub

        Private Sub GenerateFluentApiForPrecisionAndScale([property] As IProperty,
                                                          stringBuilder As IndentedStringBuilder)

            NotNull([property], NameOf([property]))
            NotNull(stringBuilder, NameOf(stringBuilder))

            If Not [property].GetPrecision().HasValue Then Exit Sub

            Dim precision As Integer = [property].GetPrecision().Value

            stringBuilder.
                AppendLine(".").
                Append(NameOf(PropertyBuilder.HasPrecision)).
                Append("(").
                Append(VBCode.UnknownLiteral(precision))


            Dim scale As Integer = [property].GetScale().GetValueOrDefault

            If scale <> 0 Then
                stringBuilder.
                    Append(", ").
                    Append(VBCode.UnknownLiteral(scale))
            End If

            stringBuilder.Append(")")

        End Sub

        Private Sub GenerateFluentApiForIsUnicode([property] As IProperty,
                                                  stringBuilder As IndentedStringBuilder)

            NotNull([property], NameOf([property]))
            NotNull(stringBuilder, NameOf(stringBuilder))

            If Not [property].IsUnicode().HasValue Then Exit Sub

            Dim unicode As Boolean = [property].IsUnicode().Value

            stringBuilder.
                AppendLine(".").
                Append(NameOf(PropertyBuilder.IsUnicode)).
                Append("(").
                Append(VBCode.Literal(unicode)).
                Append(")")

        End Sub

        Private Sub GenerateFluentApiForDefaultValue([property] As IProperty,
                                                     stringBuilder As IndentedStringBuilder)

            NotNull([property], NameOf([property]))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim defaultValue = [property].GetDefaultValue()
            If defaultValue Is Nothing Then Exit Sub

            stringBuilder.
                AppendLine(".").
                Append(NameOf(RelationalPropertyBuilderExtensions.HasDefaultValue)).
                Append("(")

            If defaultValue IsNot DBNull.Value Then

                Dim value = defaultValue

                Dim t = FindValueConverter([property])
                If TypeOf t Is ValueConverter Then
                    Dim valueConverter = DirectCast(t, ValueConverter)
                    value = valueConverter.ConvertToProvider(defaultValue)
                End If

                stringBuilder _
                    .Append(VBCode.UnknownLiteral(value))
            End If

            stringBuilder.
                Append(")")
        End Sub

        Private Shared Function RemoveAmbiguousFluentApiAnnotations(
            annotations As Dictionary(Of String, IAnnotation),
            annotationNameMatcher As Func(Of String, Boolean)) As IReadOnlyList(Of IAnnotation)

            Dim ambiguousAnnotations As List(Of IAnnotation) = Nothing

            For Each kpv As KeyValuePair(Of String, IAnnotation) In annotations
                Dim name = kpv.Key
                Dim Annotation = kpv.Value

                If annotationNameMatcher(name) Then
                    annotations.Remove(name)
                    ambiguousAnnotations = If(ambiguousAnnotations, New List(Of IAnnotation))
                    ambiguousAnnotations.Add(Annotation)
                End If
            Next

            Return If(DirectCast(ambiguousAnnotations, IReadOnlyList(Of IAnnotation)), ImmutableList(Of IAnnotation).Empty)
        End Function

    End Class

End Namespace