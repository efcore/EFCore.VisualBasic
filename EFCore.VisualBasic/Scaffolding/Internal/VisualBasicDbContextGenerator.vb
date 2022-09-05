Imports System.Text
Imports EntityFrameworkCore.VisualBasic.Design
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports Microsoft.EntityFrameworkCore.Metadata.Conventions
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Scaffolding

Namespace Scaffolding.Internal

    Friend Class VisualBasicDbContextGenerator

        Private Const EntityLambdaIdentifier As String = "entity"

        Private ReadOnly _VBCode As IVisualBasicHelper
        Private ReadOnly _providerConfigurationCodeGenerator As IProviderConfigurationCodeGenerator
        Private ReadOnly _annotationCodeGenerator As IAnnotationCodeGenerator

        Private ReadOnly _builder As New IndentedStringBuilder
        Private ReadOnly _namespaces As New HashSet(Of String)
        Private _entityTypeBuilderInitialized As Boolean
        Private _useDataAnnotations As Boolean

        Public Sub New(annotationCodeGenerator As IAnnotationCodeGenerator,
                       providerConfigurationCodeGenerator As IProviderConfigurationCodeGenerator,
                       vbHelper As IVisualBasicHelper)

            _annotationCodeGenerator = NotNull(annotationCodeGenerator, NameOf(annotationCodeGenerator))
            _providerConfigurationCodeGenerator = NotNull(providerConfigurationCodeGenerator, NameOf(providerConfigurationCodeGenerator))
            _VBCode = NotNull(vbHelper, NameOf(vbHelper))
        End Sub

        Public Overridable Function WriteCode(
            model As IModel,
            contextName As String,
            connectionString As String,
            rootNamespace As String,
            contextNamespace As String,
            modelNamespace As String,
            useDataAnnotations As Boolean,
            suppressConnectionStringWarning As Boolean,
            suppressOnConfiguring As Boolean) As String

            NotNull(model, NameOf(model))

            _useDataAnnotations = useDataAnnotations

            _builder.Clear()
            _namespaces.Clear()

            _namespaces.Add("Microsoft.EntityFrameworkCore")
            _namespaces.Add("Microsoft.EntityFrameworkCore.Metadata")

            ' The final namespaces list is calculated after code generation, since namespaces may be added during code generation

            Dim finalContextNamespace As String = If(contextNamespace, modelNamespace)
            Dim trimmedFinalContextNamespace = finalContextNamespace

            If finalContextNamespace IsNot Nothing Then
                If Not String.IsNullOrWhiteSpace(rootNamespace) Then
                    If Not modelNamespace.StartsWith(rootNamespace, StringComparison.OrdinalIgnoreCase) Then
                        modelNamespace = rootNamespace & "." & modelNamespace
                    End If
                    If finalContextNamespace.StartsWith(rootNamespace, StringComparison.OrdinalIgnoreCase) Then
                        trimmedFinalContextNamespace = RemoveRootNamespaceFromNamespace(rootNamespace, finalContextNamespace)
                    Else
                        finalContextNamespace = rootNamespace & "." & finalContextNamespace
                    End If
                End If
            End If

            Dim addANamespace = Not String.IsNullOrWhiteSpace(trimmedFinalContextNamespace)

            If addANamespace Then
                _builder.AppendLine($"Namespace {_VBCode.Namespace(trimmedFinalContextNamespace)}")
                _builder.IncrementIndent()
            End If

            GenerateClass(
                model,
                contextName,
                connectionString,
                suppressConnectionStringWarning,
                suppressOnConfiguring)

            If addANamespace Then
                _builder.DecrementIndent()
                _builder.AppendLine("End Namespace")
            End If

            Dim namespaceStringBuilder As New StringBuilder()

            Dim namespaces As IEnumerable(Of String) = _namespaces.OrderBy(
                    Function(ns)
                        Select Case True
                            Case ns.StartsWith("System", StringComparison.Ordinal) : Return 1
                            Case ns.StartsWith("Microsoft", StringComparison.Ordinal) : Return 2
                            Case Else : Return 3
                        End Select
                    End Function).
                    ThenBy(Function(ns) ns)

            If Not finalContextNamespace.Equals(modelNamespace, StringComparison.OrdinalIgnoreCase) Then
                If Not String.IsNullOrWhiteSpace(modelNamespace) Then
                    namespaces = namespaces.Append(_VBCode.Namespace(modelNamespace))
                End If
            End If

            For Each [namespace] In namespaces
                namespaceStringBuilder.Append("Imports ").AppendLine([namespace])
            Next

            namespaceStringBuilder.AppendLine()

            Return namespaceStringBuilder.ToString() & _builder.ToString()
        End Function

        Protected Overridable Sub GenerateClass(
            model As IModel,
            contextName As String,
            connectionString As String,
            suppressConnectionStringWarning As Boolean,
            suppressOnConfiguring As Boolean)

            NotNull(model, NameOf(model))
            NotNull(contextName, NameOf(contextName))
            NotNull(connectionString, NameOf(connectionString))

            _builder.AppendLine($"Public Partial Class {contextName}")
            Using _builder.Indent()
                _builder.AppendLine("Inherits DbContext")
            End Using

            _builder.AppendLine()

            Using _builder.Indent()
                GenerateConstructors(contextName, generateDefaultConstructor:=Not suppressOnConfiguring)
                GenerateDbSets(model)
                GenerateEntityTypeErrors(model)
                If Not suppressOnConfiguring Then
                    GenerateOnConfiguring(connectionString, suppressConnectionStringWarning)
                End If

                GenerateOnModelCreating(model)
            End Using

            _builder.AppendLine()

            Using _builder.Indent()
                _builder.AppendLine("Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)")
                _builder.AppendLine("End Sub")
            End Using

            _builder.AppendLine("End Class")
        End Sub

        Private Sub GenerateConstructors(contextName As String, generateDefaultConstructor As Boolean)

            If generateDefaultConstructor Then
                _builder.
                    AppendLine($"Public Sub New()").
                    AppendLine("End Sub").
                    AppendLine()
            End If

            _builder.AppendLine($"Public Sub New(options As DbContextOptions(Of {contextName}))").
                IncrementIndent().
                AppendLine("MyBase.New(options)").
                DecrementIndent().
                AppendLine("End Sub").
                AppendLine()
        End Sub

        Private Sub GenerateDbSets(model As IModel)

            Dim generated = False

            For Each entityType In model.GetEntityTypes()
                If entityType.IsSimpleManyToManyJoinEntityType Then Continue For
                _builder.AppendLine($"Public Overridable Property {_VBCode.Identifier(entityType.GetDbSetName())} As DbSet(Of {entityType.Name})")
                generated = True
            Next

            If generated Then
                _builder.AppendLine()
            End If
        End Sub

        Private Sub GenerateEntityTypeErrors(model As IModel)

            Dim errors = model.GetReverseEngineeringErrors()
            For Each entityTypeError In errors
                _builder.AppendLine($"' {entityTypeError} Please see the warning messages.")
            Next

            If errors.Any Then
                _builder.AppendLine()
            End If
        End Sub

        Protected Overridable Sub GenerateOnConfiguring(connectionString As String,
                                                        suppressConnectionStringWarning As Boolean)

            NotNull(connectionString, NameOf(connectionString))

            _builder.AppendLine("Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)")

            Using _builder.Indent()
                _builder.AppendLine("If Not optionsBuilder.IsConfigured Then")

                Using _builder.Indent()
                    If Not suppressConnectionStringWarning Then
                        _builder.AppendLine("'TODO /!\ " & DesignStrings.SensitiveInformationWarning)
                    End If

                    Dim useProviderCall = _providerConfigurationCodeGenerator.GenerateUseProvider(connectionString)
                    _builder.
                        Append("optionsBuilder").
                        AppendLine(_VBCode.Fragment(useProviderCall, _builder.CurrentIndent + 1))
                End Using

                _builder.AppendLine("End If")
            End Using

            _builder.AppendLine("End Sub")

            _builder.AppendLine()
        End Sub

        Protected Overridable Sub GenerateOnModelCreating(model As IModel)
            NotNull(model, NameOf(model))

            _builder.AppendLine("Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)")

            Dim annotations = _annotationCodeGenerator.FilterIgnoredAnnotations(model.GetAnnotations()).
                                                       ToDictionary(Function(a) a.Name, Function(a) a)

            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(model, annotations)

            annotations.Remove(CoreAnnotationNames.ProductVersion)
            annotations.Remove(RelationalAnnotationNames.MaxIdentifierLength)
            annotations.Remove(ScaffoldingAnnotationNames.DatabaseName)
            annotations.Remove(ScaffoldingAnnotationNames.ReverseEngineeringErrors)

            Dim lines As New List(Of String)

            GenerateAnnotations(model, annotations, lines)

            If lines.Any() Then
                Using _builder.Indent()
                    _builder.Append("modelBuilder")

                    If lines.Count = 1 Then
                        _builder.
                            Append("."c).
                            Append(lines(0))
                    Else
                        Using _builder.Indent()
                            For Each line In lines
                                _builder.
                                    AppendLine("."c).
                                    Append(line)
                            Next
                        End Using
                    End If
                End Using
                _builder.AppendLine()
            End If

            Using _builder.Indent()
                For Each entityType In model.GetEntityTypes()

                    If entityType.IsSimpleManyToManyJoinEntityType() Then Continue For

                    _entityTypeBuilderInitialized = False

                    GenerateEntityType(entityType)

                    If _entityTypeBuilderInitialized Then
                        _builder.AppendLine("End Sub)")
                        _builder.DecrementIndent()
                    End If
                Next

                For Each sequence In model.GetSequences()
                    GenerateSequence(sequence)
                Next
            End Using

            _builder.AppendLine()

            Using _builder.Indent()
                _builder.AppendLine("OnModelCreatingPartial(modelBuilder)")
            End Using

            _builder.AppendLine("End Sub")
        End Sub

        Private Sub InitializeEntityTypeBuilder(entityType As IEntityType)
            If Not _entityTypeBuilderInitialized Then
                _builder.AppendLine()
                _builder.AppendLine($"modelBuilder.Entity(Of {entityType.Name})(")
                _builder.Indent()
                _builder.Append($"Sub({EntityLambdaIdentifier})")
            End If

            _entityTypeBuilderInitialized = True
        End Sub

        Private Sub GenerateEntityType(entityType As IEntityType)

            GenerateKey(entityType.FindPrimaryKey(), entityType)

            Dim annotations = _annotationCodeGenerator.
                              FilterIgnoredAnnotations(entityType.GetAnnotations()).
                              ToDictionary(Function(a) a.Name, Function(a) a)

            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(entityType, annotations)

            annotations.Remove(RelationalAnnotationNames.TableName)
            annotations.Remove(RelationalAnnotationNames.Schema)
            annotations.Remove(RelationalAnnotationNames.ViewName)
            annotations.Remove(RelationalAnnotationNames.ViewSchema)
            annotations.Remove(ScaffoldingAnnotationNames.DbSetName)
            annotations.Remove(RelationalAnnotationNames.ViewDefinitionSql)

            If _useDataAnnotations Then
                ' Strip out any annotations handled as attributes - these are already handled when generating
                ' the entity's properties
                Call _annotationCodeGenerator.GenerateDataAnnotationAttributes(entityType, annotations)
            End If

            If Not _useDataAnnotations OrElse entityType.GetViewName() IsNot Nothing Then
                GenerateTableName(entityType)
            End If

            Dim lines As New List(Of String)

            GenerateAnnotations(entityType, annotations, lines)

            AppendMultiLineFluentApi(entityType, lines)

            For Each index In entityType.GetIndexes()
                ' If there are annotations that cannot be represented using an IndexAttribute then use fluent API even
                ' if useDataAnnotations is true.
                Dim indexAnnotations = _annotationCodeGenerator.FilterIgnoredAnnotations(index.GetAnnotations()).
                                                                ToDictionary(Function(a) a.Name, Function(a) a)
                _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(index, indexAnnotations)

                If Not _useDataAnnotations OrElse indexAnnotations.Count > 0 Then
                    GenerateIndex(index)
                End If
            Next

            For Each prop In entityType.GetProperties()
                GenerateProperty(prop)
            Next

            For Each foreignKey In entityType.GetForeignKeys()
                GenerateRelationship(foreignKey)
            Next

            For Each skipNavigation In entityType.GetSkipNavigations()
                If skipNavigation.JoinEntityType.FindPrimaryKey().Properties(0).GetContainingForeignKeys().Single().PrincipalEntityType Is entityType Then
                    ' We generate UsingEntity for entityType from first property's FK.
                    GenerateManyToMany(skipNavigation)
                End If
            Next

            Dim triggers = entityType.GetTriggers().ToArray()

            If triggers.Length > 0 Then
                Using _builder.Indent()
                    _builder.
                        AppendLine().
                        AppendLine($"{EntityLambdaIdentifier}.{NameOf(RelationalEntityTypeBuilderExtensions.ToTable)}(")

                    Using _builder.Indent()
                        _builder.Append("Sub(tb)")

                        For Each trigger In entityType.GetTriggers().Where(Function(t) t.Name IsNot Nothing)
                            GenerateTrigger("tb", trigger)
                        Next

                        _builder.AppendLine("End Sub)")
                    End Using
                End Using
            End If
        End Sub

        Private Sub GenerateTrigger(tableBuilderName As String, trigger As ITrigger)
            Dim lines = New List(Of String) From {$"HasTrigger({_VBCode.Literal(trigger.Name)})"}

            Dim annotations = _annotationCodeGenerator.
                                FilterIgnoredAnnotations(trigger.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(trigger, annotations)

            GenerateAnnotations(trigger, annotations, lines)

            AppendMultiLineFluentApi(Nothing, lines, tableBuilderName)
        End Sub

        Private Sub AppendMultiLineFluentApi(entityType As IEntityType, lines As IList(Of String), Optional builderName As String = Nothing)
            If lines.Count <= 0 Then Exit Sub

            If entityType IsNot Nothing Then
                InitializeEntityTypeBuilder(entityType)
            End If

            Using _builder.Indent()

                _builder.
                    AppendLine().
                    Append(If(builderName, EntityLambdaIdentifier)).
                    Append("."c).
                    AppendLines(lines(0), skipFinalNewline:=True)

                Using _builder.Indent()
                    For Each line In lines.Skip(1)
                        _builder.
                            AppendLine("."c).
                            Append(line)
                    Next
                End Using
                _builder.AppendLine()
            End Using
        End Sub

        Private Sub GenerateKey(key As IKey, entityType As IEntityType)
            If key Is Nothing Then
                If Not _useDataAnnotations Then
                    Dim line As New List(Of String) From {
                        $"{NameOf(EntityTypeBuilder.HasNoKey)}()"}

                    AppendMultiLineFluentApi(entityType, line)
                End If

                Exit Sub
            End If

            Dim annotations = _annotationCodeGenerator.
                FilterIgnoredAnnotations(key.GetAnnotations()).
                ToDictionary(Function(a) a.Name, Function(a) a)

            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(key, annotations)

            Dim explicitName As Boolean = key.GetName() <> key.GetDefaultName()
            annotations.Remove(RelationalAnnotationNames.Name)

            If key.Properties.Count = 1 AndAlso annotations.Count = 0 Then
                If TypeOf key Is IConventionKey Then
                    Dim conventionKey = DirectCast(key, IConventionKey)

                    If conventionKey.Properties.SequenceEqual(
                           KeyDiscoveryConvention.DiscoverKeyProperties(
                               conventionKey.DeclaringEntityType,
                               conventionKey.DeclaringEntityType.GetProperties())) Then
                        Exit Sub
                    End If
                End If

                If Not explicitName AndAlso _useDataAnnotations Then
                    Exit Sub
                End If
            End If

            Dim lines As New List(Of String) From {
                $"{NameOf(EntityTypeBuilder.HasKey)}({_VBCode.Lambda(key.Properties, "e")})"}

            If explicitName Then
                lines.Add($"{NameOf(RelationalKeyBuilderExtensions.HasName)}({_VBCode.Literal(key.GetName())})")
            End If

            GenerateAnnotations(key, annotations, lines)

            AppendMultiLineFluentApi(key.DeclaringEntityType, lines)
        End Sub

        Private Sub GenerateTableName(entityType As IEntityType)

            Dim tableName = entityType.GetTableName()
            Dim schema = entityType.GetSchema()
            Dim defaultSchema = entityType.Model.GetDefaultSchema()

            Dim explicitSchema = schema IsNot Nothing AndAlso schema <> defaultSchema
            Dim explicitTable = explicitSchema OrElse tableName IsNot Nothing AndAlso tableName <> entityType.GetDbSetName()

            If explicitTable Then
                Dim parameterString = _VBCode.Literal(tableName)
                If explicitSchema Then
                    parameterString &= ", " & _VBCode.Literal(schema)
                End If

                Dim lines As New List(Of String) From {
                    $"{NameOf(RelationalEntityTypeBuilderExtensions.ToTable)}({parameterString})"}

                AppendMultiLineFluentApi(entityType, lines)
            End If

            Dim viewName = entityType.GetViewName()
            Dim viewSchema = entityType.GetViewSchema()

            Dim explicitViewSchema = viewSchema IsNot Nothing AndAlso viewSchema <> defaultSchema
            Dim explicitViewTable = explicitViewSchema OrElse viewName IsNot Nothing

            If explicitViewTable Then
                Dim parameterString = _VBCode.Literal(viewName)
                If explicitViewSchema Then
                    parameterString &= ", " & _VBCode.Literal(viewSchema)
                End If

                Dim lines As New List(Of String) From {
                    $"{NameOf(RelationalEntityTypeBuilderExtensions.ToView)}({parameterString})"}

                AppendMultiLineFluentApi(entityType, lines)
            End If
        End Sub

        Private Sub GenerateIndex(index As IIndex)
            Dim annotations = _annotationCodeGenerator.
                FilterIgnoredAnnotations(index.GetAnnotations()).
                ToDictionary(Function(a) a.Name,
                             Function(a) a)

            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(index, annotations)

            Dim lines As New List(Of String) From {
                $"{NameOf(EntityTypeBuilder.HasIndex)}({_VBCode.Lambda(index.Properties, "e")}, {_VBCode.Literal(index.GetDatabaseName())})"
            }
            annotations.Remove(RelationalAnnotationNames.Name)

            If index.IsUnique Then
                lines.Add($"{NameOf(IndexBuilder.IsUnique)}()")
            End If

            If index.IsDescending IsNot Nothing Then
                lines.Add($"{NameOf(IndexBuilder.IsDescending)}({String.Join(", ", index.IsDescending.Select(Function(d) _VBCode.Literal(d)))})")
            End If

            GenerateAnnotations(index, annotations, lines)

            AppendMultiLineFluentApi(index.DeclaringEntityType, lines)
        End Sub

        Private Sub GenerateProperty(prop As IProperty)
            Dim lines As New List(Of String) From {
                $"{NameOf(EntityTypeBuilder.Property)}({_VBCode.Lambda({prop.Name}, "e")})"}

            Dim annotations = _annotationCodeGenerator.
                FilterIgnoredAnnotations(prop.GetAnnotations()).
                ToDictionary(Function(a) a.Name, Function(a) a)

            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(prop, annotations)
            annotations.Remove(RelationalAnnotationNames.ColumnOrder)

            If _useDataAnnotations Then
                ' Strip out any annotations handled as attributes - these are already handled when generating
                ' the entity's properties
                ' Only relational ones need to be removed here. Core ones are already removed by FilterIgnoredAnnotations
                annotations.Remove(RelationalAnnotationNames.ColumnName)
                annotations.Remove(RelationalAnnotationNames.ColumnType)

                Call _annotationCodeGenerator.GenerateDataAnnotationAttributes(prop, annotations)
            Else
                If Not prop.IsNullable AndAlso prop.ClrType.IsNullableType() AndAlso Not prop.IsPrimaryKey() Then
                    lines.Add($"{NameOf(PropertyBuilder.IsRequired)}()")
                End If

                Dim columnType = prop.GetConfiguredColumnType()
                If columnType IsNot Nothing Then
                    lines.Add($"{NameOf(RelationalPropertyBuilderExtensions.HasColumnType)}({_VBCode.Literal(columnType)})")
                    annotations.Remove(RelationalAnnotationNames.ColumnType)
                End If

                Dim maxLength = prop.GetMaxLength()
                If maxLength.HasValue Then
                    lines.Add($"{NameOf(PropertyBuilder.HasMaxLength)}({_VBCode.Literal(maxLength.Value)})")
                End If

                Dim precision = prop.GetPrecision()
                Dim scale = prop.GetScale()
                If precision.HasValue AndAlso scale.HasValue AndAlso scale <> 0 Then
                    lines.Add($"{NameOf(PropertyBuilder.HasPrecision)}({_VBCode.Literal(precision.Value)}, {_VBCode.Literal(scale.Value)})")
                ElseIf precision.HasValue Then
                    lines.Add($"{NameOf(PropertyBuilder.HasPrecision)}({_VBCode.Literal(precision.Value)})")
                End If

                If prop.IsUnicode().HasValue Then
                    lines.Add($"{NameOf(PropertyBuilder.IsUnicode)}({(If(prop.IsUnicode() = False, "false", ""))})")
                End If
            End If

            Dim defaultValue As Object = prop.GetDefaultValue()
            If prop.TryGetDefaultValue(defaultValue) Then
                If defaultValue Is DBNull.Value Then
                    lines.Add($"{NameOf(RelationalPropertyBuilderExtensions.HasDefaultValue)}()")
                    annotations.Remove(RelationalAnnotationNames.DefaultValue)
                ElseIf defaultValue IsNot Nothing Then
                    lines.Add($"{NameOf(RelationalPropertyBuilderExtensions.HasDefaultValue)}({_VBCode.UnknownLiteral(defaultValue)})")
                    annotations.Remove(RelationalAnnotationNames.DefaultValue)
                End If
            End If

            Dim valueGenerated = prop.ValueGenerated

            Dim ConventionProperty = DirectCast(prop, IConventionProperty)

            Dim isConfigurationSource = ConventionProperty.GetValueGeneratedConfigurationSource().HasValue
            If isConfigurationSource Then

                Dim valueGeneratedConfigurationSource = ConventionProperty.GetValueGeneratedConfigurationSource().Value
                Dim vgc = ValueGenerationConvention.GetValueGenerated(prop)

                If valueGeneratedConfigurationSource <> ConfigurationSource.Convention AndAlso
                   (vgc Is Nothing OrElse vgc <> valueGenerated) Then

                    Dim methodName As String = ""

                    Select Case valueGenerated
                        Case ValueGenerated.OnAdd
                            methodName = NameOf(PropertyBuilder.ValueGeneratedOnAdd)
                        Case ValueGenerated.OnAddOrUpdate
                            methodName = If(prop.IsConcurrencyToken,
                                            NameOf(PropertyBuilder.IsRowVersion),
                                            NameOf(PropertyBuilder.ValueGeneratedOnAddOrUpdate))
                        Case ValueGenerated.OnUpdate
                            methodName = NameOf(PropertyBuilder.ValueGeneratedOnUpdate)
                        Case ValueGenerated.Never
                            methodName = NameOf(PropertyBuilder.ValueGeneratedNever)
                        Case Else
                            Throw New InvalidOperationException(DesignStrings.UnhandledEnumValue($"{NameOf(valueGenerated)}.{valueGenerated}"))
                    End Select

                    lines.Add($"{methodName}()")
                End If
            End If

            If prop.IsConcurrencyToken Then
                lines.Add($"{NameOf(PropertyBuilder.IsConcurrencyToken)}()")
            End If

            GenerateAnnotations(prop, annotations, lines)

            Select Case lines.Count
                Case 1
                    Exit Sub
                Case 2
                    lines = New List(Of String) From {lines(0) & "." & lines(1)}
            End Select

            AppendMultiLineFluentApi(prop.DeclaringEntityType, lines)
        End Sub

        Private Sub GenerateRelationship(foreignKey As IForeignKey)

            Dim canUseDataAnnotations As Boolean = True

            Dim annotations = _annotationCodeGenerator.
                                FilterIgnoredAnnotations(foreignKey.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(foreignKey, annotations)

            Dim lines As New List(Of String) From {
                $"{NameOf(EntityTypeBuilder.HasOne)}(" &
                    If(foreignKey.DependentToPrincipal IsNot Nothing, $"Function(d) d.{foreignKey.DependentToPrincipal.Name}", Nothing) &
                ")",
                $"{If(foreignKey.IsUnique, NameOf(ReferenceNavigationBuilder.WithOne), NameOf(ReferenceNavigationBuilder.WithMany))}" &
                "(" &
                If(foreignKey.PrincipalToDependent IsNot Nothing, $"Function(p) p.{foreignKey.PrincipalToDependent.Name}", Nothing) &
                ")"
            }

            If Not foreignKey.PrincipalKey.IsPrimaryKey() Then
                canUseDataAnnotations = False
                lines.Add(
                    $"{NameOf(ReferenceReferenceBuilder.HasPrincipalKey)}" &
                     If(foreignKey.IsUnique,
                        $"(Of {foreignKey.PrincipalEntityType.Name})", "") &
                        $"({_VBCode.Lambda(foreignKey.PrincipalKey.Properties, "p")})")
            End If

            lines.Add(
            $"{NameOf(ReferenceReferenceBuilder.HasForeignKey)}" &
                If(foreignKey.IsUnique, $"(Of {foreignKey.DeclaringEntityType.Name})", "") &
                $"({_VBCode.Lambda(foreignKey.Properties, "d")})")

            Dim defaultOnDeleteAction = If(foreignKey.IsRequired, DeleteBehavior.Cascade, DeleteBehavior.ClientSetNull)

            If foreignKey.DeleteBehavior <> defaultOnDeleteAction Then
                canUseDataAnnotations = False
                lines.Add($"{NameOf(ReferenceReferenceBuilder.OnDelete)}({_VBCode.Literal(CType(foreignKey.DeleteBehavior, [Enum]))})")
            End If

            If Not String.IsNullOrEmpty(CStr(foreignKey(RelationalAnnotationNames.Name))) Then
                canUseDataAnnotations = False
            End If

            GenerateAnnotations(foreignKey, annotations, lines)

            If Not _useDataAnnotations OrElse Not canUseDataAnnotations Then
                AppendMultiLineFluentApi(foreignKey.DeclaringEntityType, lines)
            End If
        End Sub

        Private Sub GenerateManyToMany(skipNavigation As ISkipNavigation)

            If Not _entityTypeBuilderInitialized Then
                InitializeEntityTypeBuilder(skipNavigation.DeclaringEntityType)
            End If

            _builder.AppendLine()

            Dim inverse = skipNavigation.Inverse
            Dim joinEntityType = skipNavigation.JoinEntityType

            Using _builder.Indent()
                _builder.AppendLine($"{EntityLambdaIdentifier}.{NameOf(EntityTypeBuilder.HasMany)}(Function(d) d.{skipNavigation.Name}).")

                Using _builder.Indent()
                    _builder.AppendLine($"{NameOf(CollectionNavigationBuilder.WithMany)}(Function(p) p.{inverse.Name}).")
                    _builder.AppendLine($"{NameOf(CollectionCollectionBuilder.UsingEntity)}(Of {_VBCode.Reference(Model.DefaultPropertyBagType)})(")

                    Using _builder.Indent()
                        _builder.AppendLine($"{_VBCode.Literal(joinEntityType.Name)},")

                        GenerateForeignKeyConfigurationLines(inverse.ForeignKey, inverse.ForeignKey.PrincipalEntityType.Name, "l")
                        GenerateForeignKeyConfigurationLines(skipNavigation.ForeignKey, skipNavigation.ForeignKey.PrincipalEntityType.Name, "r")

                        _builder.AppendLine("Sub(j)")

                        Using _builder.Indent()

                            Dim lines As New List(Of String)

                            Dim key = joinEntityType.FindPrimaryKey()
                            Dim keyAnnotations = _annotationCodeGenerator.
                                                    FilterIgnoredAnnotations(key.GetAnnotations()).
                                                    ToDictionary(Function(a) a.Name, Function(a) a)

                            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(key, keyAnnotations)

                            Dim explicitName = key.GetName() <> key.GetDefaultName()
                            keyAnnotations.Remove(RelationalAnnotationNames.Name)

                            _builder.Append($"j.{NameOf(EntityTypeBuilder.HasKey)}({String.Join(", ", key.Properties.Select(Function(e) _VBCode.Literal(e.Name)))})")
                            If explicitName Then
                                lines.Add($"{NameOf(RelationalKeyBuilderExtensions.HasName)}({_VBCode.Literal(key.GetName())})")
                            End If

                            GenerateAnnotations(key, keyAnnotations, lines)
                            WriteLines(lines)

                            Dim annotations = _annotationCodeGenerator.
                                                FilterIgnoredAnnotations(joinEntityType.GetAnnotations()).
                                                ToDictionary(Function(a) a.Name, Function(a) a)

                            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(joinEntityType, annotations)

                            annotations.Remove(RelationalAnnotationNames.TableName)
                            annotations.Remove(RelationalAnnotationNames.Schema)
                            annotations.Remove(RelationalAnnotationNames.ViewName)
                            annotations.Remove(RelationalAnnotationNames.ViewSchema)
                            annotations.Remove(ScaffoldingAnnotationNames.DbSetName)
                            annotations.Remove(RelationalAnnotationNames.ViewDefinitionSql)

                            Dim tableName = joinEntityType.GetTableName()
                            Dim schema = joinEntityType.GetSchema()
                            Dim defaultSchema = joinEntityType.Model.GetDefaultSchema()

                            Dim explicitSchema = schema IsNot Nothing AndAlso schema <> defaultSchema
                            Dim parameterString = _VBCode.Literal(tableName)
                            If explicitSchema Then
                                parameterString &= ", " & _VBCode.Literal(schema)
                            End If

                            _builder.Append($"j.{NameOf(RelationalEntityTypeBuilderExtensions.ToTable)}({parameterString})")

                            GenerateAnnotations(joinEntityType, annotations, lines)
                            WriteLines(lines)

                            For Each index In joinEntityType.GetIndexes()
                                ' If there are annotations that cannot be represented using an IndexAttribute then use fluent API even
                                ' if useDataAnnotations is true.
                                Dim indexAnnotations = _annotationCodeGenerator.
                                                           FilterIgnoredAnnotations(index.GetAnnotations()).
                                                           ToDictionary(Function(a) a.Name, Function(a) a)
                                _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(index, indexAnnotations)

                                _builder.Append($"j.{NameOf(EntityTypeBuilder.HasIndex)}({_VBCode.Literal(index.Properties.Select(Function(e) e.Name).ToArray())}, {_VBCode.Literal(index.GetDatabaseName())})")
                                indexAnnotations.Remove(RelationalAnnotationNames.Name)

                                If index.IsUnique Then
                                    lines.Add($"{NameOf(IndexBuilder.IsUnique)}()")
                                End If

                                GenerateAnnotations(index, indexAnnotations, lines)
                                WriteLines(lines)
                            Next

                            For Each [property] In joinEntityType.GetProperties()

                                Dim propertyAnnotations = _annotationCodeGenerator.
                                                            FilterIgnoredAnnotations([property].GetAnnotations()).
                                                            ToDictionary(Function(a) a.Name, Function(a) a)

                                _annotationCodeGenerator.RemoveAnnotationsHandledByConventions([property], propertyAnnotations)
                                propertyAnnotations.Remove(RelationalAnnotationNames.ColumnOrder)

                                If [property].ClrType.IsValueType AndAlso
                                   [property].ClrType.IsNullableType() AndAlso
                                   [property].IsPrimaryKey() Then

                                    lines.Add($"{NameOf(PropertyBuilder.IsRequired)}()")
                                End If

                                Dim columnType = [property].GetConfiguredColumnType()
                                If columnType IsNot Nothing Then
                                    lines.Add($"{NameOf(RelationalPropertyBuilderExtensions.HasColumnType)}({_VBCode.Literal(columnType)})")
                                    propertyAnnotations.Remove(RelationalAnnotationNames.ColumnType)
                                End If

                                Dim maxLength = [property].GetMaxLength()
                                If maxLength.HasValue Then
                                    lines.Add($"{NameOf(PropertyBuilder.HasMaxLength)}({_VBCode.Literal(maxLength.Value)})")
                                End If

                                Dim precision = [property].GetPrecision()
                                Dim scale = [property].GetScale()
                                If precision.HasValue AndAlso scale.HasValue AndAlso scale.Value <> 0 Then
                                    lines.Add($"{NameOf(PropertyBuilder.HasPrecision)}({_VBCode.Literal(precision.Value)}, {_VBCode.Literal(scale.Value)})")
                                ElseIf precision.HasValue Then
                                    lines.Add($"{NameOf(PropertyBuilder.HasPrecision)}({_VBCode.Literal(precision.Value)})")
                                End If

                                If [property].IsUnicode().HasValue Then
                                    lines.Add($"{NameOf(PropertyBuilder.IsUnicode)}({If([property].IsUnicode().Value = False, "False", "")})")
                                End If

                                Dim defaultValue As Object = Nothing
                                If [property].TryGetDefaultValue(defaultValue) Then
                                    If defaultValue Is DBNull.Value Then
                                        lines.Add($"{NameOf(RelationalPropertyBuilderExtensions.HasDefaultValue)}()")
                                        propertyAnnotations.Remove(RelationalAnnotationNames.DefaultValue)
                                    ElseIf defaultValue IsNot Nothing Then
                                        lines.Add($"{NameOf(RelationalPropertyBuilderExtensions.HasDefaultValue)}({_VBCode.UnknownLiteral(defaultValue)})")
                                        propertyAnnotations.Remove(RelationalAnnotationNames.DefaultValue)
                                    End If
                                End If

                                Dim valueGenerated = [property].ValueGenerated

                                Dim cp = CType([property], IConventionProperty).GetValueGeneratedConfigurationSource()

                                If cp.HasValue AndAlso
                                   cp.Value <> ConfigurationSource.Convention AndAlso
                                   ValueGenerationConvention.GetValueGenerated([property]) <> valueGenerated Then

                                    Dim methodName As String = Nothing
                                    Select Case valueGenerated
                                        Case ValueGenerated.OnAdd : methodName = NameOf(PropertyBuilder.ValueGeneratedOnAdd)
                                        Case ValueGenerated.OnAddOrUpdate : methodName = If([property].IsConcurrencyToken,
                                                                                                NameOf(PropertyBuilder.IsRowVersion),
                                                                                                NameOf(PropertyBuilder.ValueGeneratedOnAddOrUpdate))
                                        Case ValueGenerated.OnUpdate : methodName = NameOf(PropertyBuilder.ValueGeneratedOnUpdate)
                                        Case ValueGenerated.Never : methodName = NameOf(PropertyBuilder.ValueGeneratedNever)
                                        Case Else : Throw New InvalidOperationException(DesignStrings.UnhandledEnumValue($"{NameOf(valueGenerated)}.{valueGenerated}"))
                                    End Select

                                    _builder.Append($"{methodName}()")
                                End If

                                If [property].IsConcurrencyToken Then
                                    lines.Add($"{NameOf(PropertyBuilder.IsConcurrencyToken)}()")
                                End If

                                GenerateAnnotations([property], propertyAnnotations, lines)

                                If lines.Count > 0 Then
                                    _builder.Append($"j.{NameOf(EntityTypeBuilder.IndexerProperty)}(Of {_VBCode.Reference([property].ClrType)})({_VBCode.Literal([property].Name)})")
                                    WriteLines(lines)
                                Else
                                    lines.Clear()
                                End If
                            Next
                        End Using

                        _builder.AppendLine("End Sub)")
                    End Using
                End Using
            End Using
        End Sub

        Private Sub WriteLines(lines As List(Of String))
            For Each line In lines
                _builder.AppendLine("."c)
                _builder.Append(line)
            Next

            _builder.AppendLine()
            lines.Clear()
        End Sub

        Private Sub GenerateForeignKeyConfigurationLines(foreignKey As IForeignKey, targetType As String, identifier As String)

            Dim annotations = _annotationCodeGenerator.
                                 FilterIgnoredAnnotations(foreignKey.GetAnnotations()).
                                 ToDictionary(Function(a) a.Name, Function(a) a)

            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(foreignKey, annotations)
            _builder.AppendLine($"Function({identifier})")

            Using _builder.Indent
                _builder.Append("Return ")
                _builder.Append($"{identifier}.{NameOf(EntityTypeBuilder.HasOne)}(Of {targetType})().{NameOf(ReferenceNavigationBuilder.WithMany)}()")

                Using _builder.Indent
                    If Not foreignKey.PrincipalKey.IsPrimaryKey() Then
                        _builder.AppendLine("."c)
                        _builder.Append($"{NameOf(ReferenceReferenceBuilder.HasPrincipalKey)}({String.Join(", ", foreignKey.PrincipalKey.Properties.Select(Function(e) _VBCode.Literal(e.Name)))})")
                    End If

                    _builder.AppendLine("."c)
                    _builder.Append($"{NameOf(ReferenceReferenceBuilder.HasForeignKey)}({String.Join(", ", foreignKey.Properties.Select(Function(e) _VBCode.Literal(e.Name)))})")

                    Dim defaultOnDeleteAction = If(foreignKey.IsRequired, DeleteBehavior.Cascade, DeleteBehavior.ClientSetNull)

                    If foreignKey.DeleteBehavior <> defaultOnDeleteAction Then
                        _builder.AppendLine("."c)
                        _builder.Append($"{NameOf(ReferenceReferenceBuilder.OnDelete)}({_VBCode.Literal(CType(foreignKey.DeleteBehavior, [Enum]))})")
                    End If

                    Dim lines As New List(Of String)
                    GenerateAnnotations(foreignKey, annotations, lines)
                    For Each l In lines
                        _builder.AppendLine("."c)
                        _builder.Append(l)
                    Next
                End Using
            End Using

            _builder.AppendLine()
            _builder.AppendLine("End Function,")
        End Sub

        Private Sub GenerateSequence(seq As ISequence)
            Dim methodName As String = NameOf(RelationalModelBuilderExtensions.HasSequence)

            If seq.Type <> Sequence.DefaultClrType Then
                methodName &= $"(Of {_VBCode.Reference(seq.Type)})"
            End If

            Dim parameters = _VBCode.Literal(seq.Name)

            If Not String.IsNullOrEmpty(seq.Schema) AndAlso seq.Model.GetDefaultSchema() <> seq.Schema Then
                parameters &= $", {_VBCode.Literal(seq.Schema)}"
            End If

            _builder.AppendLine().
                Append($"modelBuilder.{methodName}({parameters})")

            Dim lines As New List(Of String)

            If seq.StartValue <> Sequence.DefaultStartValue Then
                lines.Add($"{NameOf(SequenceBuilder.StartsAt)}({seq.StartValue})")
            End If

            If seq.IncrementBy <> Sequence.DefaultIncrementBy Then
                lines.Add($"{NameOf(SequenceBuilder.IncrementsBy)}({seq.IncrementBy})")
            End If

            If Not seq.MinValue.Equals(Sequence.DefaultMinValue) Then
                lines.Add($"{NameOf(SequenceBuilder.HasMin)}({seq.MinValue})")
            End If

            If Not seq.MaxValue.Equals(Sequence.DefaultMaxValue) Then
                lines.Add($"{NameOf(SequenceBuilder.HasMax)}({seq.MaxValue})")
            End If

            If seq.IsCyclic <> Sequence.DefaultIsCyclic Then
                lines.Add($"{NameOf(SequenceBuilder.IsCyclic)}()")
            End If

            If lines.Count = 1 Then
                _builder.
                    Append("."c).
                    Append(lines(0))
            Else
                Using _builder.Indent()
                    For Each line In lines
                        _builder.
                            AppendLine("."c).
                            Append(line)
                    Next
                End Using
            End If

            _builder.AppendLine()
        End Sub

        Private Sub GenerateAnnotations(annotatable As IAnnotatable, annotations As Dictionary(Of String, IAnnotation), lines As List(Of String))

            For Each fluentApiCall In _annotationCodeGenerator.GenerateFluentApiCalls(annotatable, annotations)

                ' Remove optional arguments
                If fluentApiCall.MethodInfo IsNot Nothing Then
                    Dim MethodInfo = fluentApiCall.MethodInfo
                    Dim methodParameters = MethodInfo.GetParameters()
                    Dim paramOffset = If(MethodInfo.IsStatic, 1, 0)

                    For i = fluentApiCall.Arguments.Count - 1 To 0 Step -1
                        If Not methodParameters(i + paramOffset).HasDefaultValue Then
                            Exit For
                        End If

                        Dim defaultValue = methodParameters(i + paramOffset).DefaultValue
                        Dim argument = fluentApiCall.Arguments(i)

                        If argument Is Nothing AndAlso defaultValue Is Nothing OrElse argument IsNot Nothing AndAlso argument.Equals(defaultValue) Then
                            fluentApiCall = New MethodCallCodeFragment(MethodInfo, fluentApiCall.Arguments.Take(i).ToArray())
                        Else
                            Exit For
                        End If
                    Next
                End If

                lines.Add(_VBCode.Fragment(fluentApiCall, indent:=1, startWithDot:=False))

                If fluentApiCall.Namespace IsNot Nothing Then
                    _namespaces.Add(fluentApiCall.Namespace)
                End If
            Next

            lines.AddRange(
                annotations.Values.
                    Select(Function(a) $"HasAnnotation({_VBCode.Literal(a.Name)}, {_VBCode.UnknownLiteral(a.Value)})"))
        End Sub
    End Class
End Namespace
