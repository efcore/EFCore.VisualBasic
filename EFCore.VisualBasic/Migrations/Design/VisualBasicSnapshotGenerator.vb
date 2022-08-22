Imports System.Reflection
Imports System.Text
Imports EntityFrameworkCore.VisualBasic.Design
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design
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
    ''' <remarks>
    '''     See <see href="https://aka.ms/efcore-docs-migrations">Database migrations</see>, And
    '''     <see href="https://aka.ms/efcore-docs-design-time-services">EF Core design-time services</see> for more information.
    ''' </remarks>
    Public Class VisualBasicSnapshotGenerator

        Private Shared ReadOnly HasAnnotationMethodInfo As MethodInfo =
            GetType(ModelBuilder).GetRuntimeMethod(NameOf(ModelBuilder.HasAnnotation), {GetType(String), GetType(String)})

        ''' <summary>
        '''     Initializes a New instance of the <see cref="VisualBasicSnapshotGenerator" /> class.
        ''' </summary>
        ''' <param name="vbHelper">The Visual Basic helper.</param>
        Public Sub New(annotationCodeGenerator As IAnnotationCodeGenerator,
                       relationalTypeMappingSource As IRelationalTypeMappingSource,
                       vbHelper As IVisualBasicHelper)

            Me.AnnotationCodeGenerator = NotNull(annotationCodeGenerator, NameOf(annotationCodeGenerator))
            Me.RelationalTypeMappingSource = NotNull(relationalTypeMappingSource, NameOf(relationalTypeMappingSource))
            Me.VBCode = NotNull(vbHelper, NameOf(vbHelper))
        End Sub

        ''' <summary>
        '''     The Visual Basic helper.
        ''' </summary>
        Private ReadOnly VBCode As IVisualBasicHelper

        ''' <summary>
        '''     The type mapper.
        ''' </summary>
        Private ReadOnly RelationalTypeMappingSource As IRelationalTypeMappingSource

        ''' <summary>
        '''     The annotation code generator.
        ''' </summary>
        Private ReadOnly AnnotationCodeGenerator As IAnnotationCodeGenerator


        ''' <summary>
        '''     Generates code for creating an <see cref="IModel" />.
        ''' </summary>
        ''' <param name="modelBuilderName">The <see cref="ModelBuilder" /> variable name.</param>
        ''' <param name="model">The model.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Public Overridable Sub Generate(modelBuilderName As String,
                                        model As IModel,
                                        stringBuilder As IndentedStringBuilder)

            NotEmpty(modelBuilderName, NameOf(modelBuilderName))
            NotNull(model, NameOf(model))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim annotations = AnnotationCodeGenerator.
                                FilterIgnoredAnnotations(model.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            If model.GetProductVersion() IsNot Nothing Then
                Dim productVersion = model.GetProductVersion()
                annotations(CoreAnnotationNames.ProductVersion) = New Annotation(CoreAnnotationNames.ProductVersion, productVersion)
            End If

            GenerateAnnotations(modelBuilderName, model, stringBuilder, annotations, inChainedCall:=False, leadingNewline:=False)

            For Each sequence In model.GetSequences()
                GenerateSequence(modelBuilderName, sequence, stringBuilder)
            Next

            GenerateEntityTypes(modelBuilderName, model.GetEntityTypesInHierarchicalOrder(), stringBuilder)
        End Sub

        ''' <summary>
        '''     Generates code for <see cref="IEntityType" /> objects.
        ''' </summary>
        ''' <param name="modelBuilderName">The name of the builder variable.</param>
        ''' <param name="entityTypes">The entity types.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateEntityTypes(modelBuilderName As String,
                                                      entityTypes As IEnumerable(Of IEntityType),
                                                      stringBuilder As IndentedStringBuilder)

            NotEmpty(modelBuilderName, NameOf(modelBuilderName))
            NotNull(entityTypes, NameOf(entityTypes))
            NotNull(stringBuilder, NameOf(stringBuilder))


            Dim nonOwnedTypes = entityTypes.Where(Function(e) e.FindOwnership() Is Nothing).ToList()
            For Each entityType In nonOwnedTypes
                stringBuilder.AppendLine()
                GenerateEntityType(modelBuilderName, entityType, stringBuilder)
            Next

            For Each entityType In nonOwnedTypes.Where(
                     Function(e) e.GetDeclaredForeignKeys().Any() OrElse
                                 e.GetDeclaredReferencingForeignKeys().Any(Function(fk) fk.IsOwnership))

                stringBuilder.AppendLine()
                GenerateEntityTypeRelationships(modelBuilderName, entityType, stringBuilder)
            Next

            For Each entityType In nonOwnedTypes.Where(
                     Function(e) e.GetDeclaredNavigations().Any(Function(n) Not n.IsOnDependent AndAlso
                                                                            Not n.ForeignKey.IsOwnership))

                stringBuilder.AppendLine()
                GenerateEntityTypeNavigations(modelBuilderName, entityType, stringBuilder)
            Next
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="IEntityType" />.
        ''' </summary>
        ''' <param name="modelBuilderName">The name of the builder variable.</param>
        ''' <param name="entityType">The entity type.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateEntityType(modelBuilderName As String,
                                                     entityType As IEntityType,
                                                     stringBuilder As IndentedStringBuilder)

            NotEmpty(modelBuilderName, NameOf(modelBuilderName))
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

            Dim entityTypeBuilderName = GenerateEntityTypeBuilderName(modelBuilderName)

            stringBuilder.
                Append(modelBuilderName).
                Append(If(ownerNavigation IsNot Nothing, If(ownership.IsUnique, ".OwnsOne(", ".OwnsMany("), ".Entity(")).
                Append(VBCode.Literal(entityTypeName))

            If ownerNavigation IsNot Nothing Then
                stringBuilder.
                    Append(", ").
                    Append(VBCode.Literal(ownerNavigation))
            End If

            stringBuilder.
                AppendLine(","c)

            Using stringBuilder.Indent()
                stringBuilder.
                    Append("Sub(").
                    Append(entityTypeBuilderName).
                    AppendLine(")"c)

                Using stringBuilder.Indent()
                    GenerateBaseType(entityTypeBuilderName, entityType.BaseType, stringBuilder)

                    GenerateProperties(entityTypeBuilderName, entityType.GetDeclaredProperties(), stringBuilder)

                    GenerateKeys(
                        entityTypeBuilderName,
                        entityType.GetDeclaredKeys(),
                        If(entityType.BaseType Is Nothing, entityType.FindPrimaryKey(), Nothing),
                        stringBuilder)

                    GenerateIndexes(entityTypeBuilderName, entityType.GetDeclaredIndexes(), stringBuilder)

                    GenerateEntityTypeAnnotations(entityTypeBuilderName, entityType, stringBuilder)

                    If ownerNavigation IsNot Nothing Then
                        GenerateRelationships(entityTypeBuilderName, entityType, stringBuilder)
                        GenerateNavigations(entityTypeBuilderName, entityType.GetDeclaredNavigations().
                                                Where(Function(n) Not n.IsOnDependent AndAlso
                                                                  Not n.ForeignKey.IsOwnership), stringBuilder)
                    End If

                    GenerateData(entityTypeBuilderName, entityType.GetProperties(), entityType.GetSeedData(providerValues:=True), stringBuilder)
                End Using

                stringBuilder.
                    AppendLine("End Sub)")
            End Using
        End Sub

        Private Function GenerateEntityTypeBuilderName(modelBuilderName As String) As String
            If modelBuilderName.StartsWith("b", StringComparison.Ordinal) Then
                Dim counter = 1
                If modelBuilderName.Length > 1 AndAlso
                   Integer.TryParse(modelBuilderName.Substring(1, modelBuilderName.Length - 1), counter) Then
                    counter += 1
                End If

                Return "b" & If(counter = 0, "", counter.ToString())
            End If

            Return "b"
        End Function

        ''' <summary>
        '''     Generates code for owned entity types.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="ownerships">The foreign keys identifying each entity type.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateOwnedTypes(entityTypeBuilderName As String,
                                                     ownerships As IEnumerable(Of IForeignKey),
                                                     stringBuilder As IndentedStringBuilder)

            NotEmpty(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(ownerships, NameOf(ownerships))
            NotNull(stringBuilder, NameOf(stringBuilder))

            For Each ownership In ownerships
                stringBuilder.AppendLine()
                GenerateOwnedType(entityTypeBuilderName, ownership, stringBuilder)
            Next
        End Sub

        ''' <summary>
        '''     Generates code for an owned entity types.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="ownerships">The foreign keys identifying each entity type.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateOwnedType(entityTypeBuilderName As String,
                                                    ownerships As IEnumerable(Of IForeignKey),
                                                    stringBuilder As IndentedStringBuilder)

            NotNull(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(ownerships, NameOf(ownerships))
            NotNull(stringBuilder, NameOf(stringBuilder))

            For Each ownership In ownerships
                stringBuilder.AppendLine()
                GenerateOwnedType(entityTypeBuilderName, ownership, stringBuilder)
            Next

        End Sub

        ''' <summary>
        '''     Generates code for an owned entity types.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="ownership">The foreign key identifying the entity type.</param>
        ''' <param name="stringBuilder">The builder code is added to.</param>
        Protected Overridable Sub GenerateOwnedType(entityTypeBuilderName As String,
                                                    ownership As IForeignKey,
                                                    stringBuilder As IndentedStringBuilder)

            NotNull(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(ownership, NameOf(ownership))
            NotNull(stringBuilder, NameOf(stringBuilder))

            GenerateEntityType(entityTypeBuilderName, ownership.DeclaringEntityType, stringBuilder)
        End Sub


        ''' <summary>
        '''     Generates code for the relationships of an <see cref="IEntityType"/>.
        ''' </summary>
        ''' <param name="modelBuilderName">The name of the builder variable.</param>
        ''' <param name="entityType">The entity type.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateEntityTypeRelationships(modelBuilderName As String,
                                                                  entityType As IEntityType,
                                                                  stringBuilder As IndentedStringBuilder)

            NotEmpty(modelBuilderName, NameOf(modelBuilderName))
            NotNull(entityType, NameOf(entityType))
            NotNull(stringBuilder, NameOf(stringBuilder))

            stringBuilder.
                Append(modelBuilderName).
                Append(".Entity(").
                Append(VBCode.Literal(entityType.Name)).
                AppendLine(","c)

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
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="entityType">The entity type.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateRelationships(entityTypeBuilderName As String,
                                                        entityType As IEntityType,
                                                        stringBuilder As IndentedStringBuilder)

            NotEmpty(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(entityType, NameOf(entityType))
            NotNull(stringBuilder, NameOf(stringBuilder))

            GenerateForeignKeys(entityTypeBuilderName, entityType.GetDeclaredForeignKeys(), stringBuilder)

            GenerateOwnedTypes(
                entityTypeBuilderName, entityType.GetDeclaredReferencingForeignKeys().Where(Function(fk) fk.IsOwnership), stringBuilder)

            GenerateNavigations(entityTypeBuilderName, entityType.GetDeclaredNavigations().
                                             Where(Function(n) n.IsOnDependent OrElse (Not n.IsOnDependent AndAlso n.ForeignKey.IsOwnership)), stringBuilder)
        End Sub

        ''' <summary>
        '''     Generates code for the base type of an <see cref="IEntityType"/>.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="baseType">The base entity type.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateBaseType(entityTypeBuilderName As String,
                                                   baseType As IEntityType,
                                                   stringBuilder As IndentedStringBuilder)

            NotNull(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(stringBuilder, NameOf(stringBuilder))

            If baseType IsNot Nothing Then
                stringBuilder.
                    AppendLine().
                    Append(entityTypeBuilderName).
                    Append(".HasBaseType(").
                    Append(VBCode.Literal(GetFullName(baseType))).
                    AppendLine(")"c)
            End If
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref= "ISequence"/>.
        ''' </summary>
        ''' <param name="modelBuilderName">The name of the builder variable.</param>
        ''' <param name="sequence">The sequence.</param>
        ''' <param name="stringBuilder">The builder code is added to.</param>
        Protected Overridable Sub GenerateSequence(modelBuilderName As String,
                                                   sequence As ISequence,
                                                   stringBuilder As IndentedStringBuilder)

            Dim sequenceBuilderNameBuilder As New StringBuilder()

            sequenceBuilderNameBuilder.
                Append(modelBuilderName).
                Append(".HasSequence")

            If sequence.Type <> Internal.Sequence.DefaultClrType Then
                sequenceBuilderNameBuilder.
                    Append("(Of ").
                    Append(VBCode.Reference(sequence.Type)).
                    Append(")"c)
            End If

            sequenceBuilderNameBuilder.
                Append("("c).
                Append(VBCode.Literal(sequence.Name))

            If Not String.IsNullOrEmpty(sequence.Schema) AndAlso
               sequence.Model.GetDefaultSchema() <> sequence.Schema Then

                sequenceBuilderNameBuilder.
                    Append(", ").
                    Append(VBCode.Literal(sequence.Schema))
            End If

            sequenceBuilderNameBuilder.Append(")"c)
            Dim sequenceBuilderName = sequenceBuilderNameBuilder.ToString()

            stringBuilder.
                AppendLine().
                Append(sequenceBuilderName)

            ' Note that GenerateAnnotations below does the corresponding decrement
            stringBuilder.IncrementIndent()

            If sequence.StartValue <> Internal.Sequence.DefaultStartValue Then
                stringBuilder.
                    AppendLine("."c).
                    Append("StartsAt(").
                    Append(VBCode.Literal(sequence.StartValue)).
                    Append(")"c)
            End If

            If sequence.IncrementBy <> Internal.Sequence.DefaultIncrementBy Then
                stringBuilder.
                    AppendLine("."c).
                    Append("IncrementsBy(").
                    Append(VBCode.Literal(sequence.IncrementBy)).
                    Append(")"c)
            End If

            If Not sequence.MinValue.Equals(Internal.Sequence.DefaultMinValue) Then
                stringBuilder.
                    AppendLine("."c).
                    Append("HasMin(").
                    Append(VBCode.Literal(sequence.MinValue)).
                    Append(")"c)
            End If

            If Not sequence.MaxValue.Equals(Internal.Sequence.DefaultMaxValue) Then
                stringBuilder.
                    AppendLine("."c).
                    Append("HasMax(").
                    Append(VBCode.Literal(sequence.MaxValue)).
                    Append(")"c)
            End If

            If sequence.IsCyclic <> Internal.Sequence.DefaultIsCyclic Then
                stringBuilder.
                    AppendLine("."c).
                    Append("IsCyclic()")
            End If

            GenerateSequenceAnnotations(sequenceBuilderName, sequence, stringBuilder)
        End Sub

        ''' <summary>
        '''     Generates code for sequence annotations.
        ''' </summary>
        ''' <param name="sequenceBuilderName">The name of the sequence builder variable.</param>
        ''' <param name="sequence">The sequence.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateSequenceAnnotations(sequenceBuilderName As String,
                                                              sequence As ISequence,
                                                              stringBuilder As IndentedStringBuilder)

            Dim annotations = AnnotationCodeGenerator.
                                FilterIgnoredAnnotations(sequence.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            GenerateAnnotations(sequenceBuilderName, sequence, stringBuilder, annotations, inChainedCall:=True)
        End Sub

        ''' <summary>
        '''     Generates code for <see cref="IProperty"/> objects.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="properties">The properties.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateProperties(entityTypeBuilderName As String,
                                                     properties As IEnumerable(Of IProperty),
                                                     stringBuilder As IndentedStringBuilder)

            NotNull(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(properties, NameOf(properties))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim first = True
            For Each [property] In properties
                If first Then
                    first = False
                Else
                    stringBuilder.AppendLine()
                End If
                GenerateProperty(entityTypeBuilderName, [property], stringBuilder)
            Next
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="IProperty"/>.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="property">The property.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateProperty(entityTypeBuilderName As String,
                                                   [property] As IProperty,
                                                   stringBuilder As IndentedStringBuilder)

            NotNull(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull([property], NameOf([property]))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim clrType = If(FindValueConverter([property])?.ProviderClrType.MakeNullable([property].IsNullable), [property].ClrType)

            Dim propertyBuilderName = $"{entityTypeBuilderName}.Property(Of {VBCode.Reference(clrType)})({VBCode.Literal([property].Name)})"

            stringBuilder.
                Append(propertyBuilderName)

            ' Note that GenerateAnnotations below does the corresponding decrement
            stringBuilder.IncrementIndent()

            If [property].IsConcurrencyToken Then
                stringBuilder.
                    AppendLine("."c).
                    Append("IsConcurrencyToken()")
            End If

            If [property].IsNullable <> (clrType.IsNullableType() AndAlso Not [property].IsPrimaryKey()) Then
                stringBuilder.
                    AppendLine("."c).
                    Append("IsRequired()")
            End If

            Select Case [property].ValueGenerated
                Case ValueGenerated.Never

                Case ValueGenerated.OnAdd
                    stringBuilder.
                        AppendLine("."c).
                        Append("ValueGeneratedOnAdd()")

                Case ValueGenerated.OnUpdate
                    stringBuilder.
                        AppendLine("."c).
                        Append("ValueGeneratedOnUpdate()")

                Case ValueGenerated.OnUpdateSometimes
                    stringBuilder.
                        AppendLine("."c).
                        Append("ValueGeneratedOnUpdateSometimes()")

                Case Else
                    stringBuilder.
                        AppendLine("."c).
                        Append("ValueGeneratedOnAddOrUpdate()")
            End Select

            GeneratePropertyAnnotations(propertyBuilderName, [property], stringBuilder)
        End Sub

        ''' <summary>
        '''     Generates code for the annotations on an <see cref="IProperty"/>.
        ''' </summary>
        ''' <param name="propertyBuilderName">The name of the builder variable.</param>
        ''' <param name="property">The property.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GeneratePropertyAnnotations(propertyBuilderName As String,
                                                              [property] As IProperty,
                                                              stringBuilder As IndentedStringBuilder)

            NotNull(propertyBuilderName, NameOf(propertyBuilderName))
            NotNull([property], NameOf([property]))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim annotations = AnnotationCodeGenerator.
                                FilterIgnoredAnnotations([property].GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            GenerateFluentApiForMaxLength([property], stringBuilder)
            GenerateFluentApiForPrecisionAndScale([property], stringBuilder)
            GenerateFluentApiForIsUnicode([property], stringBuilder)

            If Not annotations.ContainsKey(RelationalAnnotationNames.ColumnType) Then
                annotations(RelationalAnnotationNames.ColumnType) = New Annotation(
                    RelationalAnnotationNames.ColumnType,
                    [property].GetColumnType())
            End If

            Dim defaultValue As Object = Nothing
            If annotations.ContainsKey(RelationalAnnotationNames.DefaultValue) AndAlso
               [property].TryGetDefaultValue(defaultValue) AndAlso
               defaultValue IsNot DBNull.Value AndAlso
               TypeOf FindValueConverter([property]) Is ValueConverter Then

                Dim valueConverter = DirectCast(FindValueConverter([property]), ValueConverter)

                annotations(RelationalAnnotationNames.DefaultValue) = New Annotation(
                    RelationalAnnotationNames.DefaultValue,
                    ValueConverter.ConvertToProvider(defaultValue))
            End If

            GenerateAnnotations(propertyBuilderName, [property], stringBuilder, annotations, inChainedCall:=True)
        End Sub

        Private Function FindValueConverter([property] As IProperty) As ValueConverter
            Dim t = If([property].FindTypeMapping(), RelationalTypeMappingSource.FindMapping([property]))
            Return If([property].GetValueConverter(), t?.Converter)
        End Function


        ''' <summary>
        '''     Generates code for <see cref="IKey"/> objects.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="keys">The keys.</param>
        ''' <param name="primaryKey">The primary key.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateKeys(entityTypeBuilderName As String, keys As IEnumerable(Of IKey),
                                               primaryKey As IKey,
                                               stringBuilder As IndentedStringBuilder)

            NotNull(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(keys, NameOf(keys))
            NotNull(stringBuilder, NameOf(stringBuilder))

            If primaryKey IsNot Nothing Then
                GenerateKey(entityTypeBuilderName, primaryKey, stringBuilder, primary:=True)
            End If

            Dim IsOwned = primaryKey?.DeclaringEntityType.IsOwned()
            If Not IsOwned.HasValue OrElse Not IsOwned Then
                For Each key In keys.Where(Function(k)
                                               Return k IsNot primaryKey AndAlso
                                                      (Not k.GetReferencingForeignKeys().Any() OrElse
                                                       k.GetAnnotations().Any(Function(a) a.Name <> RelationalAnnotationNames.UniqueConstraintMappings))
                                           End Function)
                    GenerateKey(entityTypeBuilderName, key, stringBuilder)
                Next
            End If
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="IKey"/>.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="key">The key.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        ''' <param name="primary">A value indicating whether the key Is primary.</param>
        Protected Overridable Sub GenerateKey(entityTypeBuilderName As String,
                                              key As IKey,
                                              stringBuilder As IndentedStringBuilder,
                                              Optional primary As Boolean = False)

            NotNull(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(key, NameOf(key))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim keyBuilderName =
                New StringBuilder().
                    Append(entityTypeBuilderName).
                    Append(If(primary, ".HasKey(", ".HasAlternateKey(")).
                    Append(String.Join(", ", key.Properties.Select(Function(p) VBCode.Literal(p.Name)))).
                    Append(")"c).
                    ToString()

            stringBuilder.
                AppendLine().
                Append(keyBuilderName)

            ' Note that GenerateAnnotations below does the corresponding decrement
            stringBuilder.IncrementIndent()
            GenerateKeyAnnotations(keyBuilderName, key, stringBuilder)
        End Sub

        ''' <summary>
        '''     Generates code for the annotations on a key.
        ''' </summary>
        ''' <param name="keyBuilderName">The name of the builder variable.</param>
        ''' <param name="key">The key.</param>
        ''' <param name="stringBuilder">The builder code is added to.</param>
        Protected Overridable Sub GenerateKeyAnnotations(keyBuilderName As String,
                                                         key As IKey,
                                                         stringBuilder As IndentedStringBuilder)

            NotNull(keyBuilderName, NameOf(keyBuilderName))
            NotNull(key, NameOf(key))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim annotations = AnnotationCodeGenerator.
                                FilterIgnoredAnnotations(key.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            GenerateAnnotations(keyBuilderName, key, stringBuilder, annotations, inChainedCall:=True)
        End Sub

        ''' <summary>
        '''     Generates code for <see cref="IIndex"/> objects.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="indexes">The indexes.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateIndexes(entityTypeBuilderName As String,
                                                  indexes As IEnumerable(Of IIndex),
                                                  stringBuilder As IndentedStringBuilder)

            NotNull(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(indexes, NameOf(indexes))
            NotNull(stringBuilder, NameOf(stringBuilder))

            For Each index In indexes
                stringBuilder.AppendLine()
                GenerateIndex(entityTypeBuilderName, index, stringBuilder)
            Next
        End Sub

        ''' <summary>
        '''     Generates code an <see cref="IIndex"/>.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="index">The index.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateIndex(entityTypeBuilderName As String,
                                                index As IIndex,
                                                stringBuilder As IndentedStringBuilder)

            NotNull(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(index, NameOf(index))
            NotNull(stringBuilder, NameOf(stringBuilder))

            ' Note - method names below are meant to be hard-coded
            ' because old snapshot files will fail if they are changed

            Dim indexProperties = String.Join(", ", index.Properties.Select(Function(p) VBCode.Literal(p.Name)))
            Dim indexBuilderName = $"{entityTypeBuilderName}.HasIndex(" &
                If(index.Name Is Nothing,
                    indexProperties, $"{{{indexProperties}}}, {VBCode.Literal(index.Name)}") &
                ")"

            stringBuilder.
                Append(indexBuilderName)

            ' Note that GenerateIndexAnnotations below does the corresponding decrement
            stringBuilder.IncrementIndent()

            If index.IsUnique Then
                stringBuilder.
                    AppendLine("."c).
                    Append("IsUnique()")
            End If

            If index.IsDescending IsNot Nothing Then
                stringBuilder.
                    AppendLine("."c).
                    Append("IsDescending(").
                    Append(String.Join(", ", index.IsDescending.Select(AddressOf VBCode.Literal))).
                    Append(")"c)
            End If

            GenerateIndexAnnotations(indexBuilderName, index, stringBuilder)
        End Sub

        ''' <summary>
        '''     Generates code for the annotations on an index.
        ''' </summary>
        ''' <param name="indexBuilderName">The name of the builder variable.</param>
        ''' <param name="index">The index.</param>
        ''' <param name="stringBuilder">The builder code is added to.</param>
        Protected Overridable Sub GenerateIndexAnnotations(indexBuilderName As String,
                                                           index As IIndex,
                                                           stringBuilder As IndentedStringBuilder)

            NotNull(indexBuilderName, NameOf(indexBuilderName))
            NotNull(index, NameOf(index))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim annotations = AnnotationCodeGenerator.
                                FilterIgnoredAnnotations(index.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            GenerateAnnotations(indexBuilderName, index, stringBuilder, annotations, inChainedCall:=True)
        End Sub

        ''' <summary>
        '''     Generates code for the annotations on an entity type.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="entityType">The entity type.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateEntityTypeAnnotations(entityTypeBuilderName As String,
                                                                entityType As IEntityType,
                                                                stringBuilder As IndentedStringBuilder)

            NotNull(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(entityType, NameOf(entityType))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim discriminatorPropertyAnnotation As IAnnotation = Nothing
            Dim discriminatorValueAnnotation As IAnnotation = Nothing
            Dim discriminatorMappingCompleteAnnotation As IAnnotation = Nothing

            For Each annotation In entityType.GetAnnotations()
                Select Case annotation.Name
                    Case CoreAnnotationNames.DiscriminatorProperty
                        discriminatorPropertyAnnotation = annotation
                    Case CoreAnnotationNames.DiscriminatorValue
                        discriminatorValueAnnotation = annotation
                    Case CoreAnnotationNames.DiscriminatorMappingComplete
                        discriminatorMappingCompleteAnnotation = annotation
                End Select
            Next

            Dim annotations = AnnotationCodeGenerator.
                                FilterIgnoredAnnotations(entityType.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            GenerateTableMapping(entityTypeBuilderName, entityType, stringBuilder, annotations)
            GenerateSplitTableMapping(entityTypeBuilderName, entityType, stringBuilder)

            GenerateViewMapping(entityTypeBuilderName, entityType, stringBuilder, annotations)
            GenerateSplitViewMapping(entityTypeBuilderName, entityType, stringBuilder)

            Dim functionNameAnnotation = annotations.Find(RelationalAnnotationNames.FunctionName)
            If functionNameAnnotation IsNot Nothing OrElse entityType.BaseType Is Nothing Then
                Dim functionName = If(CStr(functionNameAnnotation?.Value), entityType.GetFunctionName())

                If functionName IsNot Nothing OrElse functionNameAnnotation IsNot Nothing Then
                    stringBuilder.
                        AppendLine().
                        Append(entityTypeBuilderName).
                        Append(".ToFunction(").
                        Append(VBCode.Literal(functionName)).
                        AppendLine(")"c)

                    If functionNameAnnotation IsNot Nothing Then
                        annotations.Remove(functionNameAnnotation.Name)
                    End If
                End If
            End If

            Dim sqlQueryAnnotation = annotations.Find(RelationalAnnotationNames.SqlQuery)

            If sqlQueryAnnotation IsNot Nothing OrElse entityType.BaseType Is Nothing Then
                Dim SqlQuery = If(CStr(sqlQueryAnnotation?.Value), entityType.GetSqlQuery())

                If SqlQuery IsNot Nothing OrElse sqlQueryAnnotation IsNot Nothing Then
                    stringBuilder.
                        AppendLine().
                        Append(entityTypeBuilderName).
                        Append(".ToSqlQuery(").
                        Append(VBCode.Literal(SqlQuery)).
                        AppendLine(")"c)

                    If sqlQueryAnnotation IsNot Nothing Then
                        annotations.Remove(sqlQueryAnnotation.Name)
                    End If
                End If
            End If

            If If(discriminatorPropertyAnnotation?.Value,
                    If(discriminatorMappingCompleteAnnotation?.Value,
                        discriminatorValueAnnotation?.Value)) IsNot Nothing Then

                stringBuilder.
                    AppendLine().
                    Append(entityTypeBuilderName).
                    Append("."c).
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
                        Append(")"c)
                Else
                    stringBuilder.
                        Append("()")
                End If

                If discriminatorMappingCompleteAnnotation?.Value IsNot Nothing Then
                    Dim value = CBool(discriminatorMappingCompleteAnnotation.Value)

                    stringBuilder.
                        Append("."c).
                        Append("IsComplete").
                        Append("("c).
                        Append(VBCode.Literal(value)).
                        Append(")"c)
                End If

                If discriminatorValueAnnotation?.Value IsNot Nothing Then
                    Dim value = discriminatorValueAnnotation.Value
                    Dim discriminatorProperty = entityType.FindDiscriminatorProperty()

                    If discriminatorProperty IsNot Nothing Then
                        Dim valueConverter = FindValueConverter(discriminatorProperty)
                        If valueConverter IsNot Nothing Then
                            value = valueConverter.ConvertToProvider(value)
                        End If
                    End If

                    stringBuilder.
                        Append("."c).
                        Append("HasValue").
                        Append("("c).
                        Append(VBCode.UnknownLiteral(value)).
                        Append(")"c)
                End If

                stringBuilder.AppendLine()
            End If

            GenerateAnnotations(entityTypeBuilderName, entityType, stringBuilder, annotations, inChainedCall:=False)
        End Sub

        Private Sub GenerateTableMapping(entityTypeBuilderName As String,
                                         entityType As IEntityType,
                                         stringBuilder As IndentedStringBuilder,
                                         annotations As Dictionary(Of String, IAnnotation))

            Dim tableNameAnnotation As IAnnotation = Nothing
            annotations.TryGetAndRemove(RelationalAnnotationNames.TableName, tableNameAnnotation)

            Dim table = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table)
            Dim tableName = If(CStr(tableNameAnnotation?.Value), table?.Name)

            Dim explicitName = tableNameAnnotation IsNot Nothing OrElse
                entityType.BaseType Is Nothing OrElse
                entityType.BaseType.GetTableName() <> tableName

            Dim schemaAnnotation As IAnnotation = Nothing
            annotations.TryGetAndRemove(RelationalAnnotationNames.Schema, schemaAnnotation)
            Dim schema = If(CStr(schemaAnnotation?.Value), table?.Schema)

            Dim isExcludedAnnotation As IAnnotation = Nothing
            annotations.TryGetAndRemove(RelationalAnnotationNames.IsTableExcludedFromMigrations, isExcludedAnnotation)
            Dim isExcludedFromMigrationsValue = If(TypeOf isExcludedAnnotation?.Value Is Boolean?, DirectCast(isExcludedAnnotation?.Value, Boolean?), Nothing)
            Dim isExcludedFromMigrations = isExcludedFromMigrationsValue.HasValue AndAlso isExcludedFromMigrationsValue.Value

            Dim commentAnnotation As IAnnotation = Nothing
            annotations.TryGetAndRemove(RelationalAnnotationNames.Comment, commentAnnotation)
            Dim comment = CStr(commentAnnotation?.Value)

            Dim hasTriggers = entityType.GetTriggers().Any(Function(t) t.TableName = tableName AndAlso t.TableSchema = schema)
            Dim hasOverrides = table IsNot Nothing AndAlso
                               entityType.GetProperties().Select(Function(p) p.FindOverrides(table.Value)).
                                                          Any(Function(o) o IsNot Nothing)

            Dim requiresTableBuilder = isExcludedFromMigrations OrElse
                                       comment IsNot Nothing OrElse
                                       hasTriggers OrElse
                                       hasOverrides OrElse
                                       entityType.GetCheckConstraints().Any()

            If Not explicitName AndAlso Not requiresTableBuilder Then
                Exit Sub
            End If

            stringBuilder.
                AppendLine().
                Append(entityTypeBuilderName).
                Append(".ToTable(")

            If explicitName Then
                If tableName Is Nothing AndAlso
                   (schemaAnnotation Is Nothing OrElse schema Is Nothing) Then
                    stringBuilder.
                        Append("DirectCast(").
                        Append(VBCode.Literal(tableName)).
                        Append(", String)")
                Else
                    stringBuilder.Append(VBCode.Literal(tableName))
                End If

                If isExcludedAnnotation IsNot Nothing Then
                    annotations.Remove(isExcludedAnnotation.Name)
                End If

                If schema IsNot Nothing OrElse
                   (schemaAnnotation IsNot Nothing AndAlso tableName IsNot Nothing) Then

                    stringBuilder.Append(", ")

                    If schema Is Nothing AndAlso Not requiresTableBuilder Then
                        stringBuilder.
                        Append("DirectCast(").
                        Append(VBCode.Literal(schema)).
                        Append(", String)")
                    Else
                        stringBuilder.Append(VBCode.Literal(schema))
                    End If
                End If
            End If

            If requiresTableBuilder Then

                If explicitName Then
                    stringBuilder.AppendLine(","c)
                End If

                Using stringBuilder.Indent()
                    stringBuilder.AppendLine("Sub(t)")

                    Using stringBuilder.Indent()
                        If isExcludedFromMigrations Then
                            stringBuilder.
                                AppendLine("t.ExcludeFromMigrations()").
                                AppendLine()
                        End If

                        If comment IsNot Nothing Then
                            stringBuilder.
                                AppendLine().
                                AppendLine($"t.{NameOf(TableBuilder.HasComment)}({VBCode.Literal(comment)})")
                        End If

                        GenerateCheckConstraints("t", entityType, stringBuilder)

                        If hasTriggers Then
                            GenerateTriggers("t", entityType, tableName, schema, stringBuilder)
                        End If

                        If hasOverrides Then
                            GeneratePropertyOverrides("t", entityType, table.Value, stringBuilder)
                        End If
                    End Using

                    stringBuilder.Append("End Sub")
                End Using
            End If

                stringBuilder.AppendLine(")"c)
        End Sub

        Private Sub GenerateSplitTableMapping(entityTypeBuilderName As String,
                                              entityType As IEntityType,
                                              stringBuilder As IndentedStringBuilder)

            For Each fragment In entityType.GetMappingFragments(StoreObjectType.Table)
                Dim Table = fragment.StoreObject
                stringBuilder.
                    AppendLine().
                    Append(entityTypeBuilderName).
                    Append(".SplitToTable(").
                    Append(VBCode.Literal(Table.Name)).
                    Append(", ").
                    Append(VBCode.Literal(Table.Schema)).
                    AppendLine(","c)

                Using stringBuilder.Indent()
                    stringBuilder.AppendLine("Sub(t)")

                    Using stringBuilder.Indent()
                        GenerateTriggers("t", entityType, Table.Name, Table.Schema, stringBuilder)
                        GeneratePropertyOverrides("t", entityType, Table, stringBuilder)
                        GenerateEntityTypeMappingFragmentAnnotations("t", fragment, stringBuilder)
                    End Using

                    stringBuilder.
                        Append("End Sub").
                        AppendLine(")"c)
                End Using
            Next
        End Sub

        Private Sub GenerateViewMapping(entityTypeBuilderName As String,
                                        entityType As IEntityType,
                                        stringBuilder As IndentedStringBuilder,
                                        annotations As Dictionary(Of String, IAnnotation))

            Dim viewNameAnnotation As IAnnotation = Nothing
            annotations.TryGetAndRemove(RelationalAnnotationNames.ViewName, viewNameAnnotation)

            Dim viewSchemaAnnotation As IAnnotation = Nothing
            annotations.TryGetAndRemove(RelationalAnnotationNames.ViewSchema, viewSchemaAnnotation)

            annotations.Remove(RelationalAnnotationNames.ViewDefinitionSql)

            Dim View = StoreObjectIdentifier.Create(entityType, StoreObjectType.View)
            Dim viewName = If(CStr(viewNameAnnotation?.Value), View?.Name)
            If viewNameAnnotation Is Nothing AndAlso
               (viewName Is Nothing OrElse entityType.BaseType?.GetViewName() = viewName) Then

                Exit Sub
            End If

            stringBuilder.
                AppendLine().
                Append(entityTypeBuilderName).
                Append(".ToView(").
                Append(VBCode.Literal(viewName))

            If viewNameAnnotation IsNot Nothing Then
                annotations.Remove(viewNameAnnotation.Name)
            End If

            Dim hasOverrides = View IsNot Nothing AndAlso
                               entityType.GetProperties().
                                          Select(Function(p) p.FindOverrides(View.Value)).
                                          Any(Function(o) o IsNot Nothing)

            Dim schema = If(CStr(viewSchemaAnnotation?.Value), View?.Schema)
            If schema IsNot Nothing OrElse
               viewSchemaAnnotation IsNot Nothing Then

                stringBuilder.Append(", ")

                If schema Is Nothing AndAlso Not hasOverrides Then
                    stringBuilder.
                        Append("DirectCast(").
                        Append(VBCode.Literal(schema)).
                        Append(", String)")
                Else
                    stringBuilder.Append(VBCode.Literal(schema))
                End If
            End If

            If hasOverrides Then
                stringBuilder.AppendLine(","c)

                Using stringBuilder.Indent()
                    stringBuilder.AppendLine("Sub(v)")

                    Using stringBuilder.Indent()
                        GeneratePropertyOverrides("v", entityType, View.Value, stringBuilder)
                    End Using

                    stringBuilder.Append("End Sub")
                End Using
            End If

            stringBuilder.AppendLine(")"c)
        End Sub

        Private Sub GenerateSplitViewMapping(entityTypeBuilderName As String,
                                         entityType As IEntityType,
                                         stringBuilder As IndentedStringBuilder)

            For Each fragment In entityType.GetMappingFragments(StoreObjectType.View)

                stringBuilder.
                    AppendLine().
                    Append(entityTypeBuilderName).
                    Append(".SplitToView(").
                    Append(VBCode.Literal(fragment.StoreObject.Name)).
                    Append(", ").
                    Append(VBCode.Literal(fragment.StoreObject.Schema)).
                    AppendLine(","c)

                Using stringBuilder.Indent()
                    stringBuilder.AppendLine("Sub(v)")

                    Using stringBuilder.Indent()
                        GeneratePropertyOverrides("v", entityType, fragment.StoreObject, stringBuilder)
                        GenerateEntityTypeMappingFragmentAnnotations("v", fragment, stringBuilder)
                    End Using

                    stringBuilder.
                        Append("End Sub").
                        AppendLine(")"c)
                End Using
            Next
        End Sub

        '' <summary>
        ''     Generates code for mapping fragment annotations.
        '' </summary>
        '' <param name="tableBuilderName">The name of the table builder variable.</param>
        '' <param name="fragment">The mapping fragment.</param>
        '' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateEntityTypeMappingFragmentAnnotations(tableBuilderName As String,
                                                                           fragment As IEntityTypeMappingFragment,
                                                                           stringBuilder As IndentedStringBuilder)

            Dim annotations = AnnotationCodeGenerator.
                                FilterIgnoredAnnotations(fragment.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            If annotations.Count > 0 Then
                GenerateAnnotations(tableBuilderName, fragment, stringBuilder, annotations, inChainedCall:=False)
            End If
        End Sub

        ''' <summary>
        '''     Generates code for <see cref= "ICheckConstraint"/> objects.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="entityType">The entity type.</param>
        ''' <param name="stringBuilder">The builder code is added to.</param>
        Protected Overridable Sub GenerateCheckConstraints(entityTypeBuilderName As String,
                                                           entityType As IEntityType,
                                                           stringBuilder As IndentedStringBuilder)

            NotNull(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(entityType, NameOf(entityType))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim constraintsForEntity = entityType.GetCheckConstraints()

            For Each checkConstraint In constraintsForEntity
                stringBuilder.AppendLine()

                GenerateCheckConstraint(entityTypeBuilderName, checkConstraint, stringBuilder)
            Next
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref= "ICheckConstraint"/>.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="checkConstraint">The check constraint.</param>
        ''' <param name="stringBuilder">The builder code is added to.</param>
        Protected Overridable Sub GenerateCheckConstraint(entityTypeBuilderName As String,
                                                          checkConstraint As ICheckConstraint,
                                                          stringBuilder As IndentedStringBuilder)

            NotNull(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(checkConstraint, NameOf(checkConstraint))
            NotNull(stringBuilder, NameOf(stringBuilder))

            stringBuilder.
                Append(entityTypeBuilderName).
                Append(".HasCheckConstraint(").
                Append(VBCode.Literal(checkConstraint.ModelName)).
                Append(", ").
                Append(VBCode.Literal(checkConstraint.Sql)).
                Append(")"c)

            GenerateCheckConstraintAnnotations(checkConstraint, stringBuilder)
        End Sub

        ''' <summary>
        '''     Generates code for check constraint annotations.
        ''' </summary>
        ''' <param name="checkConstraint">The check constraint.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateCheckConstraintAnnotations(checkConstraint As ICheckConstraint,
                                                                    stringBuilder As IndentedStringBuilder)

            Dim hasNonDefaultName = checkConstraint.Name IsNot Nothing AndAlso
                                    checkConstraint.Name <> If(checkConstraint.GetDefaultName(), checkConstraint.ModelName)

            Dim annotations = AnnotationCodeGenerator.
                                FilterIgnoredAnnotations(checkConstraint.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            If hasNonDefaultName Then
                stringBuilder.
                    AppendLine("."c).
                    Append("HasName(").
                    Append(VBCode.Literal(checkConstraint.Name)).
                    Append(")"c)
            End If

            If annotations.Count > 0 Then
                GenerateAnnotations("t", checkConstraint, stringBuilder, annotations, inChainedCall:=True)
                stringBuilder.IncrementIndent()
            End If
        End Sub

        ''' <summary>
        '''     Generates code for <see cref="ITrigger" /> objects.
        ''' </summary>
        ''' <param name="tableBuilderName">The name of the table builder variable.</param>
        ''' <param name="entityType">The entity type.</param>
        ''' <param name="table">The table name.</param>
        ''' <param name="schema">The table schema.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateTriggers(tableBuilderName As String,
                                                   entityType As IEntityType,
                                                   table As String,
                                                   schema As String,
                                                   stringBuilder As IndentedStringBuilder)

            For Each trigger In entityType.GetTriggers()

                If trigger.TableName <> table OrElse trigger.TableSchema <> schema Then
                    Continue For
                End If

                GenerateTrigger(tableBuilderName, trigger, stringBuilder)
            Next
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="ITrigger" />.
        ''' </summary>
        ''' <param name="tableBuilderName">The name of the table builder variable.</param>
        ''' <param name="trigger">The trigger.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateTrigger(tableBuilderName As String,
                                                  trigger As ITrigger,
                                                  stringBuilder As IndentedStringBuilder)

            Dim triggerBuilderName = $"{tableBuilderName}.HasTrigger({VBCode.Literal(trigger.ModelName)})"

            stringBuilder.
                AppendLine().
                Append(triggerBuilderName)

            ' Note that GenerateAnnotations below does the corresponding decrement
            stringBuilder.IncrementIndent()

            If trigger.Name <> trigger.GetDefaultName() Then
                stringBuilder.
                    AppendLine().
                    Append(".HasName(").
                    Append(VBCode.Literal(trigger.Name)).
                    Append(")"c)
            End If

            GenerateTriggerAnnotations(triggerBuilderName, trigger, stringBuilder)
        End Sub

        ''' <summary>
        '''     Generates code for trigger annotations.
        ''' </summary>
        ''' <param name="triggerBuilderName">The name of the builder variable.</param>
        ''' <param name="trigger">The trigger.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateTriggerAnnotations(triggerBuilderName As String,
                                                             trigger As ITrigger,
                                                             stringBuilder As IndentedStringBuilder)

            Dim annotations = AnnotationCodeGenerator.
                                FilterIgnoredAnnotations(trigger.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            GenerateAnnotations(triggerBuilderName, trigger, stringBuilder, annotations, inChainedCall:=True)
        End Sub

        ''' <summary>
        '''     Generates code for <see cref="IRelationalPropertyOverrides" /> objects.
        ''' </summary>
        ''' <param name="tableBuilderName">The name of the table builder variable.</param>
        ''' <param name="entityType">The entity type.</param>
        ''' <param name="storeObject">The store object identifier.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GeneratePropertyOverrides(tableBuilderName As String,
                                                            entityType As IEntityType,
                                                            storeObject As StoreObjectIdentifier,
                                                            stringBuilder As IndentedStringBuilder)

            For Each [property] In entityType.GetProperties()
                Dim [overrides] = [property].FindOverrides(storeObject)
                If [overrides] IsNot Nothing Then
                    GeneratePropertyOverride(tableBuilderName, [overrides], stringBuilder)
                End If
            Next
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="IRelationalPropertyOverrides" />.
        ''' </summary>
        ''' <param name="tableBuilderName">The name of the table builder variable.</param>
        ''' <param name="overrides">The overrides.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GeneratePropertyOverride(tableBuilderName As String,
                                                            [overrides] As IRelationalPropertyOverrides,
                                                            stringBuilder As IndentedStringBuilder)

            Dim propertyBuilderName = $"{tableBuilderName}.Property({VBCode.Literal([overrides].[Property].Name)})"
            stringBuilder.
                AppendLine().
                Append(propertyBuilderName)

            ' Note that GenerateAnnotations below does the corresponding decrement
            stringBuilder.IncrementIndent()

            If [overrides].IsColumnNameOverridden Then
                stringBuilder.
                    AppendLine("."c).
                    Append(NameOf(ColumnBuilder.HasColumnName)).
                    Append("("c).
                    Append(VBCode.Literal([overrides].ColumnName)).
                    Append(")"c)
            End If

            GeneratePropertyOverridesAnnotations(propertyBuilderName, [overrides], stringBuilder)
        End Sub

        ''' <summary>
        '''     Generates code for property overrides annotations.
        ''' </summary>
        ''' <param name="propertyBuilderName">The name of the builder variable.</param>
        ''' <param name="overrides">The overrides.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GeneratePropertyOverridesAnnotations(propertyBuilderName As String,
                                                                       [overrides] As IRelationalPropertyOverrides,
                                                                       stringBuilder As IndentedStringBuilder)

            Dim annotations = AnnotationCodeGenerator.
                                FilterIgnoredAnnotations([overrides].GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            GenerateAnnotations(propertyBuilderName, [overrides], stringBuilder, annotations, inChainedCall:=True)
        End Sub

        ''' <summary>
        '''     Generates code for <see cref="IForeignKey"/> objects.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="foreignKeys">The foreign keys.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateForeignKeys(entityTypeBuilderName As String,
                                                      foreignKeys As IEnumerable(Of IForeignKey),
                                                      stringBuilder As IndentedStringBuilder)

            NotNull(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(foreignKeys, NameOf(foreignKeys))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim isFirst = True
            For Each foreignKey In foreignKeys
                If isFirst Then
                    isFirst = False
                Else
                    stringBuilder.AppendLine()
                End If

                GenerateForeignKey(entityTypeBuilderName, foreignKey, stringBuilder)
            Next
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="IForeignKey"/>.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="foreignKey">The foreign key.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateForeignKey(entityTypeBuilderName As String,
                                                     foreignKey As IForeignKey,
                                                     stringBuilder As IndentedStringBuilder)

            NotNull(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(foreignKey, NameOf(foreignKey))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim foreignKeyBuilderNameStringBuilder As New StringBuilder()

            If Not foreignKey.IsOwnership Then
                foreignKeyBuilderNameStringBuilder.
                    Append(entityTypeBuilderName).
                    Append(".HasOne(").
                    Append(VBCode.Literal(GetFullName(foreignKey.PrincipalEntityType))).
                    Append(", ").
                    Append(VBCode.Literal(foreignKey.DependentToPrincipal?.Name))
            Else
                foreignKeyBuilderNameStringBuilder.
                    Append(entityTypeBuilderName).
                    Append(".WithOwner(")

                If foreignKey.DependentToPrincipal IsNot Nothing Then
                    foreignKeyBuilderNameStringBuilder.
                        Append(VBCode.Literal(foreignKey.DependentToPrincipal.Name))
                End If
            End If

            foreignKeyBuilderNameStringBuilder.Append(")"c)

            Dim foreignKeyBuilderName = foreignKeyBuilderNameStringBuilder.ToString()

            stringBuilder.
                Append(foreignKeyBuilderName)

            ' Note that GenerateAnnotations below does the corresponding decrement
            stringBuilder.IncrementIndent()

            If foreignKey.IsUnique AndAlso Not foreignKey.IsOwnership Then
                stringBuilder.
                        AppendLine("."c).
                        Append("WithOne(")

                If foreignKey.PrincipalToDependent IsNot Nothing Then
                    stringBuilder.
                            Append(VBCode.Literal(foreignKey.PrincipalToDependent.Name))
                End If

                stringBuilder.
                        AppendLine(").").
                        Append("HasForeignKey(").
                        Append(VBCode.Literal(GetFullName(foreignKey.DeclaringEntityType))).
                        Append(", ").
                        Append(String.Join(", ", foreignKey.Properties.Select(Function(p) VBCode.Literal(p.Name)))).
                        Append(")"c)

                If foreignKey.PrincipalKey IsNot foreignKey.PrincipalEntityType.FindPrimaryKey() Then
                    stringBuilder.
                            AppendLine("."c).
                            Append("HasPrincipalKey(").
                            Append(VBCode.Literal(GetFullName(foreignKey.PrincipalEntityType))).
                            Append(", ").
                            Append(String.Join(", ", foreignKey.PrincipalKey.Properties.Select(Function(p) VBCode.Literal(p.Name)))).
                            Append(")"c)
                End If

            Else
                If Not foreignKey.IsOwnership Then

                    stringBuilder.
                            AppendLine("."c).
                            Append("WithMany(")

                    If foreignKey.PrincipalToDependent IsNot Nothing Then
                        stringBuilder.Append(VBCode.Literal(foreignKey.PrincipalToDependent.Name))
                    End If

                    stringBuilder.Append(")"c)
                End If

                stringBuilder.
                        AppendLine("."c).
                        Append("HasForeignKey(").
                        Append(String.Join(", ", foreignKey.Properties.Select(Function(p) VBCode.Literal(p.Name)))).
                        Append(")"c)

                If foreignKey.PrincipalKey IsNot foreignKey.PrincipalEntityType.FindPrimaryKey() Then
                    stringBuilder.
                            AppendLine("."c).
                            Append("HasPrincipalKey(").
                            Append(String.Join(", ", foreignKey.PrincipalKey.Properties.Select(Function(p) VBCode.Literal(p.Name)))).
                            Append(")"c)
                End If
            End If

            If Not foreignKey.IsOwnership Then
                If foreignKey.DeleteBehavior <> DeleteBehavior.ClientSetNull Then
                    stringBuilder.
                            AppendLine("."c).
                            Append("OnDelete(").
                            Append(VBCode.Literal(DirectCast(foreignKey.DeleteBehavior, [Enum]))).
                            Append(")"c)
                End If

                If foreignKey.IsRequired Then
                    stringBuilder.
                            AppendLine("."c).
                            Append("IsRequired()")
                End If
            End If

            GenerateForeignKeyAnnotations(foreignKeyBuilderName, foreignKey, stringBuilder)
        End Sub

        ''' <summary>
        '''     Generates code for the annotations on a foreign key.
        ''' </summary>
        ''' <param name="foreignKeyBuilderName">The name of the builder variable.</param>
        ''' <param name="foreignKey">The foreign key.</param>
        ''' <param name="stringBuilder">The builder code Is added to.</param>
        Protected Overridable Sub GenerateForeignKeyAnnotations(foreignKeyBuilderName As String,
                                                                foreignKey As IForeignKey,
                                                                stringBuilder As IndentedStringBuilder)

            NotNull(foreignKeyBuilderName, NameOf(foreignKeyBuilderName))
            NotNull(foreignKey, NameOf(foreignKey))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim annotations = AnnotationCodeGenerator.
                FilterIgnoredAnnotations(foreignKey.GetAnnotations()).
                    ToDictionary(Function(a) a.Name, Function(a) a)

            GenerateAnnotations(foreignKeyBuilderName, foreignKey, stringBuilder, annotations, inChainedCall:=True)
        End Sub

        ''' <summary>
        '''     Generates code for the navigations of an <see cref= "IEntityType"/>.
        ''' </summary>
        ''' <param name="modelBuilderName">The name of the builder variable.</param>
        ''' <param name="entityType">The entity type.</param>
        ''' <param name="stringBuilder">The builder code is added to.</param>
        Protected Overridable Sub GenerateEntityTypeNavigations(modelBuilderName As String,
                                                                entityType As IEntityType,
                                                                stringBuilder As IndentedStringBuilder)

            NotEmpty(modelBuilderName, NameOf(modelBuilderName))
            NotNull(entityType, NameOf(entityType))
            NotNull(stringBuilder, NameOf(stringBuilder))

            stringBuilder.
                Append(modelBuilderName).
                Append(".Entity(").
                Append(VBCode.Literal(entityType.Name)).
                AppendLine(","c)

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
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="navigations">The navigations.</param>
        ''' <param name="stringBuilder">The builder code is added to.</param>
        Protected Overridable Sub GenerateNavigations(entityTypeBuilderName As String,
                                                      navigations As IEnumerable(Of INavigation),
                                                      stringBuilder As IndentedStringBuilder)

            NotNull(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(navigations, NameOf(navigations))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim isFirst = True
            For Each navigation In navigations
                If isFirst Then
                    isFirst = False
                Else
                    stringBuilder.AppendLine()
                End If

                GenerateNavigation(entityTypeBuilderName, navigation, stringBuilder)
            Next
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref= "INavigation"/>.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="navigation">The navigation.</param>
        ''' <param name="stringBuilder">The builder code is added to.</param>
        Protected Overridable Sub GenerateNavigation(entityTypeBuilderName As String,
                                                     navigation As INavigation,
                                                     stringBuilder As IndentedStringBuilder)

            NotNull(entityTypeBuilderName, NameOf(entityTypeBuilderName))
            NotNull(navigation, NameOf(navigation))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim navigationBuilderName = $"{entityTypeBuilderName}.Navigation({VBCode.Literal(navigation.Name)})"

            stringBuilder.
                Append(navigationBuilderName)

            ' Note that GenerateAnnotations below does the corresponding decrement
            stringBuilder.IncrementIndent()

            If Not navigation.IsOnDependent AndAlso
               Not navigation.IsCollection AndAlso
               navigation.ForeignKey.IsRequiredDependent Then

                stringBuilder.
                    AppendLine("."c).
                    Append("IsRequired()")
            End If

            GenerateNavigationAnnotations(navigationBuilderName, navigation, stringBuilder)
        End Sub

        ''' <summary>
        '''     Generates code for the annotations on a navigation.
        ''' </summary>
        ''' <param name="navigationBuilderName">The name of the builder variable.</param>
        ''' <param name="navigation">The navigation.</param>
        ''' <param name="stringBuilder">The builder code is added to.</param>
        Protected Overridable Sub GenerateNavigationAnnotations(navigationBuilderName As String,
                                                                navigation As INavigation,
                                                                stringBuilder As IndentedStringBuilder)

            NotNull(navigationBuilderName, NameOf(navigationBuilderName))
            NotNull(navigation, NameOf(navigation))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim annotations = AnnotationCodeGenerator.
                                FilterIgnoredAnnotations(navigation.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            GenerateAnnotations(navigationBuilderName, navigation, stringBuilder, annotations, inChainedCall:=True)
        End Sub

        ''' <summary>
        '''     Generates code for data seeding.
        ''' </summary>
        ''' <param name="entityTypeBuilderName">The name of the builder variable.</param>
        ''' <param name="properties">The properties to generate.</param>
        ''' <param name="data">The data to be seeded.</param>
        ''' <param name="stringBuilder">The builder code is added to.</param>
        Protected Overridable Sub GenerateData(entityTypeBuilderName As String,
                                               properties As IEnumerable(Of IProperty),
                                               data As IEnumerable(Of IDictionary(Of String, Object)),
                                               stringBuilder As IndentedStringBuilder)

            NotNull(properties, NameOf(properties))
            NotNull(data, NameOf(data))
            NotNull(stringBuilder, NameOf(stringBuilder))

            Dim dataList = data
            If Not dataList.Any Then Exit Sub

            Dim propertiesToOutput = properties

            stringBuilder.
                AppendLine().
                Append(entityTypeBuilderName).
                Append("."c).
                Append(NameOf(EntityTypeBuilder.HasData)).
                AppendLine("({")

            Using stringBuilder.Indent()
                Dim firstDatum As Boolean = True
                For Each o In dataList
                    If Not firstDatum Then
                        stringBuilder.AppendLine(","c)
                    Else
                        firstDatum = False
                    End If

                    stringBuilder.AppendLine("New With {")

                    Using stringBuilder.Indent()
                        Dim firstProperty As Boolean = True
                        For Each [property] In propertiesToOutput
                            Dim value As Object = Nothing
                            If o.TryGetValue([property].Name, value) AndAlso value IsNot Nothing Then
                                If Not firstProperty Then
                                    stringBuilder.AppendLine(","c)
                                Else
                                    firstProperty = False
                                End If

                                stringBuilder.
                                    Append("."c).
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
                AppendLine("."c).
                Append(NameOf(PropertyBuilder.HasMaxLength)).
                Append("("c).
                Append(VBCode.Literal(maxLength)).
                Append(")"c)
        End Sub

        Private Sub GenerateFluentApiForPrecisionAndScale([property] As IProperty,
                                                          stringBuilder As IndentedStringBuilder)

            NotNull([property], NameOf([property]))
            NotNull(stringBuilder, NameOf(stringBuilder))

            If Not [property].GetPrecision().HasValue Then Exit Sub

            Dim precision As Integer = [property].GetPrecision().Value

            stringBuilder.
                AppendLine("."c).
                Append(NameOf(PropertyBuilder.HasPrecision)).
                Append("("c).
                Append(VBCode.Literal(precision))


            Dim scale As Integer = [property].GetScale().GetValueOrDefault

            If scale <> 0 Then
                stringBuilder.
                    Append(", ").
                    Append(VBCode.Literal(scale))
            End If

            stringBuilder.Append(")"c)

        End Sub

        Private Sub GenerateFluentApiForIsUnicode([property] As IProperty,
                                                  stringBuilder As IndentedStringBuilder)

            NotNull([property], NameOf([property]))
            NotNull(stringBuilder, NameOf(stringBuilder))

            If Not [property].IsUnicode().HasValue Then Exit Sub

            Dim unicode As Boolean = [property].IsUnicode().Value

            stringBuilder.
                AppendLine("."c).
                Append(NameOf(PropertyBuilder.IsUnicode)).
                Append("("c).
                Append(VBCode.Literal(unicode)).
                Append(")"c)

        End Sub

        Private Sub GenerateAnnotations(builderName As String,
                                        annotatable As IAnnotatable,
                                        stringBuilder As IndentedStringBuilder,
                                        annotations As Dictionary(Of String, IAnnotation),
                                        inChainedCall As Boolean,
                                        Optional leadingNewline As Boolean = True)

            Dim fluentApiCalls = AnnotationCodeGenerator.GenerateFluentApiCalls(annotatable, annotations)

            Dim chainedCall As MethodCallCodeFragment = Nothing
            Dim typeQualifiedCalls = New List(Of MethodCallCodeFragment)()

            ' Chain together all Fluent API calls which we can, And leave the others to be generated as type-qualified
            For Each c In fluentApiCalls
                If c.MethodInfo IsNot Nothing AndAlso
                   c.MethodInfo.IsStatic AndAlso
                   (c.MethodInfo.DeclaringType Is Nothing OrElse c.MethodInfo.DeclaringType.Assembly <> GetType(RelationalModelBuilderExtensions).Assembly) Then

                    typeQualifiedCalls.Add(c)
                Else
                    chainedCall = If(chainedCall Is Nothing, c, chainedCall.Chain(c))
                End If
            Next

            ' Append remaining raw annotations which did Not get generated as Fluent API calls
            For Each annotation In annotations.Values.OrderBy(Function(a) a.Name)
                Dim c = New MethodCallCodeFragment(HasAnnotationMethodInfo, annotation.Name, annotation.Value)
                chainedCall = If(chainedCall Is Nothing, c, chainedCall.Chain(c))
            Next

            ' First generate single Fluent API call chain
            If chainedCall IsNot Nothing Then
                If inChainedCall Then
                    If chainedCall.ChainedCall Is Nothing Then
                        stringBuilder.
                            AppendLine("."c).
                            Append(VBCode.Fragment(chainedCall, stringBuilder.IndentCount, startWithDot:=False))
                    Else
                        stringBuilder.
                            Append(VBCode.Fragment(chainedCall, stringBuilder.IndentCount))
                    End If
                Else
                    If leadingNewline Then
                        stringBuilder.AppendLine()
                    End If

                    stringBuilder.Append(builderName)
                    stringBuilder.AppendLine(VBCode.Fragment(chainedCall, stringBuilder.IndentCount + 1))
                End If

                leadingNewline = True
            End If

            If inChainedCall Then
                stringBuilder.AppendLine()
                stringBuilder.DecrementIndent()
            End If

            ' Then generate separate fully-qualified calls
            If typeQualifiedCalls.Count > 0 Then
                If leadingNewline Then
                    stringBuilder.AppendLine()
                End If

                For Each c In typeQualifiedCalls
                    stringBuilder.AppendLine(VBCode.Fragment(c, builderName, typeQualified:=True))
                Next
            End If
        End Sub

        Private Shared Function GetFullName(entityType As IReadOnlyEntityType) As String
            Dim entityTypeName = entityType.Name
            Dim ownership = entityType.FindOwnership()

            If ownership Is Nothing Then
                Return entityTypeName
            End If

            If entityType.HasSharedClrType AndAlso
               entityTypeName = ownership.PrincipalEntityType.GetOwnedName(
                                    entityType.ClrType.ShortDisplayName(),
                                    ownership.PrincipalToDependent.Name) Then

                entityTypeName = entityType.ClrType.DisplayName()
            End If

            Return GetFullName(ownership.PrincipalEntityType) &
                   "."c &
                   ownership.PrincipalToDependent.Name &
                   "#"c &
                   entityTypeName
        End Function
    End Class
End Namespace
