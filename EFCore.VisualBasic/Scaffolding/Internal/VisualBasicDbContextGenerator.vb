' Copyright (c) .NET Foundation. All rights reserved.
' Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
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

    ''' <summary>
    '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
    '''     any release. You should only use it directly in your code with extreme caution and knowing that
    '''     doing so can result in application failures when updating to a new Entity Framework Core release.
    ''' </summary>
    Public Class VisualBasicDbContextGenerator
        Implements IVisualBasicDbContextGenerator

        Private Const EntityLambdaIdentifier As String = "entity"

        Private ReadOnly _code As iVisualBasicHelper
        Private ReadOnly _providerConfigurationCodeGenerator As IProviderConfigurationCodeGenerator
        Private ReadOnly _annotationCodeGenerator As IAnnotationCodeGenerator

        Private _sb As IndentedStringBuilder = Nothing
        Private _entityTypeBuilderInitialized As Boolean

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Public Sub New(providerConfigurationCodeGenerator As IProviderConfigurationCodeGenerator,
                       annotationCodeGenerator As IAnnotationCodeGenerator,
                       VisualBasicHelper As IVisualBasicHelper)

            NotNull(providerConfigurationCodeGenerator, NameOf(providerConfigurationCodeGenerator))
            NotNull(annotationCodeGenerator, NameOf(annotationCodeGenerator))
            NotNull(VisualBasicHelper, NameOf(VisualBasicHelper))

            _providerConfigurationCodeGenerator = providerConfigurationCodeGenerator
            _annotationCodeGenerator = annotationCodeGenerator
            _code = VisualBasicHelper
        End Sub

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Public Overridable Function WriteCode(
            model As IModel,
            contextName As String,
            connectionString As String,
            rootNamespace As String,
            contextNamespace As String,
            modelNamespace As String,
            useDataAnnotations As Boolean,
            suppressConnectionStringWarning As Boolean,
            suppressOnConfiguring As Boolean) As String Implements IVisualBasicDbContextGenerator.WriteCode

            NotNull(model, NameOf(model))

            _sb = New IndentedStringBuilder

            _sb.AppendLine("Imports System") 'Guid default values require new Guid() which requires this import
            _sb.AppendLine("Imports Microsoft.VisualBasic") 'Require for vbCrLf, vbCr, vbLf
            _sb.AppendLine("Imports Microsoft.EntityFrameworkCore")
            _sb.AppendLine("Imports Microsoft.EntityFrameworkCore.Metadata")

            Dim finalContextNamespace As String = If(contextNamespace, modelNamespace)
            Dim trimmedFinalContextNamespace = finalContextNamespace

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

            If Not finalContextNamespace.Equals(modelNamespace, StringComparison.OrdinalIgnoreCase) Then
                If Not String.IsNullOrWhiteSpace(modelNamespace) Then
                    _sb.AppendLine($"Imports {_code.Namespace(modelNamespace)}")
                End If
            End If

            _sb.AppendLine()

            Dim addANamespace = Not String.IsNullOrWhiteSpace(trimmedFinalContextNamespace)

            If addANamespace Then
                _sb.AppendLine($"Namespace {_code.Namespace(trimmedFinalContextNamespace)}")
                _sb.IncrementIndent()
            End If

            GenerateClass(
                model,
                contextName,
                connectionString,
                useDataAnnotations,
                suppressConnectionStringWarning,
                suppressOnConfiguring)

            If addANamespace Then
                _sb.DecrementIndent()
                _sb.AppendLine("End Namespace")
            End If

            Return _sb.ToString()
        End Function

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Protected Overridable Sub GenerateClass(
            model As IModel,
            contextName As String,
            connectionString As String,
            useDataAnnotations As Boolean,
            suppressConnectionStringWarning As Boolean,
            suppressOnConfiguring As Boolean)

            NotNull(model, NameOf(model))
            NotNull(contextName, NameOf(contextName))
            NotNull(connectionString, NameOf(connectionString))

            _sb.AppendLine($"Public Partial Class {contextName}")
            Using _sb.Indent()
                _sb.AppendLine("Inherits DbContext")
            End Using

            _sb.AppendLine()

            Using _sb.Indent()
                GenerateConstructors(contextName)
                GenerateDbSets(model)
                GenerateEntityTypeErrors(model)
                If Not suppressOnConfiguring Then
                    GenerateOnConfiguring(connectionString, suppressConnectionStringWarning)
                End If

                GenerateOnModelCreating(model, useDataAnnotations)
            End Using

            _sb.AppendLine()

            Using _sb.Indent()
                _sb.AppendLine("Partial Private Sub OnModelCreatingPartial(modelBuilder As ModelBuilder)")
                _sb.AppendLine("End Sub")
            End Using

            _sb.AppendLine("End Class")
        End Sub

        Private Sub GenerateConstructors(contextName As String)
            _sb.AppendLine($"Public Sub New()").
                AppendLine("End Sub").
                AppendLine()

            _sb.AppendLine($"Public Sub New(options As DbContextOptions(Of {contextName}))").
                IncrementIndent().AppendLine("MyBase.New(options)").
                DecrementIndent().AppendLine("End Sub").
                AppendLine()
        End Sub

        Private Sub GenerateDbSets(model As IModel)
            For Each entityType In model.GetEntityTypes()
                _sb.AppendLine($"Public Overridable Property {entityType.GetDbSetName()} As DbSet(Of {entityType.Name})")
            Next

            If model.GetEntityTypes().Any() Then
                _sb.AppendLine()
            End If
        End Sub

        Private Sub GenerateEntityTypeErrors(model As IModel)
            For Each entityTypeError In model.GetEntityTypeErrors()
                _sb.AppendLine($"' {entityTypeError.Value} Please see the warning messages.")
            Next

            If model.GetEntityTypeErrors().Count > 0 Then
                _sb.AppendLine()
            End If
        End Sub

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Protected Overridable Sub GenerateOnConfiguring(connectionString As String,
                                                        suppressConnectionStringWarning As Boolean)

            NotNull(connectionString, NameOf(connectionString))

            _sb.AppendLine("Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)")

            Using _sb.Indent()
                _sb.AppendLine("If Not optionsBuilder.IsConfigured Then")

                Using _sb.Indent()
                    If Not suppressConnectionStringWarning Then
                        'TODO, set a SensitiveInformationWarning, (can we do that in VB?)
                        '_sb.DecrementIndent().DecrementIndent().DecrementIndent().DecrementIndent().
                        '    AppendLine("#warning " & DesignStrings.SensitiveInformationWarning).
                        '    IncrementIndent().IncrementIndent().IncrementIndent().IncrementIndent()
                    End If

                    _sb.Append("optionsBuilder")

                    Dim useProviderCall = _providerConfigurationCodeGenerator.GenerateUseProvider(connectionString)

                    _sb.Append(_code.Fragment(useProviderCall))
                End Using
                _sb.AppendLine()
                _sb.AppendLine("End If")
            End Using

            _sb.AppendLine("End Sub")

            _sb.AppendLine()
        End Sub

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Protected Overridable Sub GenerateOnModelCreating(model As IModel,
                                                          useDataAnnotations As Boolean)
            NotNull(model, NameOf(model))

            _sb.AppendLine("Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)")

            Dim annotations = _annotationCodeGenerator.FilterIgnoredAnnotations(model.GetAnnotations()) _
                .ToDictionary(Function(a) a.Name, Function(a) a)

            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(model, annotations)

            annotations.Remove(CoreAnnotationNames.ProductVersion)
            annotations.Remove(RelationalAnnotationNames.MaxIdentifierLength)
            annotations.Remove(ScaffoldingAnnotationNames.DatabaseName)
            annotations.Remove(ScaffoldingAnnotationNames.EntityTypeErrors)

            Dim lines As New List(Of String)

            lines.AddRange(
                _annotationCodeGenerator.GenerateFluentApiCalls(model, annotations).
                    Select(Function(m) _code.Fragment(m)).
                    Concat(GenerateAnnotations(annotations.Values)))

            If lines.Any() Then
                Using _sb.Indent()
                    _sb.AppendLine()
                    _sb.Append("modelBuilder" & lines(0))

                    Using _sb.Indent()
                        For Each line In lines.Skip(1)
                            _sb.AppendLine()
                            _sb.Append(line)
                        Next
                    End Using
                End Using
            End If

            Using _sb.Indent()
                For Each entityType In model.GetEntityTypes()
                    _entityTypeBuilderInitialized = False

                    GenerateEntityType(entityType, useDataAnnotations)

                    If _entityTypeBuilderInitialized Then
                        _sb.AppendLine("End Sub)")
                        _sb.DecrementIndent()
                    End If
                Next

                For Each sequence In model.GetSequences()
                    GenerateSequence(sequence)
                Next
            End Using

            _sb.AppendLine()

            Using _sb.Indent()
                _sb.AppendLine("OnModelCreatingPartial(modelBuilder)")
            End Using

            _sb.AppendLine("End Sub")
        End Sub

        Private Sub InitializeEntityTypeBuilder(entityType As IEntityType)
            If Not _entityTypeBuilderInitialized Then
                _sb.AppendLine()
                _sb.AppendLine($"modelBuilder.Entity(Of {entityType.Name})(")
                _sb.Indent()
                _sb.AppendLine($"Sub({EntityLambdaIdentifier})")
            End If

            _entityTypeBuilderInitialized = True
        End Sub

        Private Sub GenerateEntityType(entityType As IEntityType, useDataAnnotations As Boolean)

            GenerateKey(entityType.FindPrimaryKey(), entityType, useDataAnnotations)

            Dim annotations = _annotationCodeGenerator _
                .FilterIgnoredAnnotations(entityType.GetAnnotations()) _
                .ToDictionary(Function(a) a.Name, Function(a) a)
            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(entityType, annotations)

            annotations.Remove(RelationalAnnotationNames.TableName)
            annotations.Remove(RelationalAnnotationNames.Schema)
            annotations.Remove(RelationalAnnotationNames.ViewName)
            annotations.Remove(RelationalAnnotationNames.ViewSchema)
            annotations.Remove(ScaffoldingAnnotationNames.DbSetName)
            annotations.Remove(RelationalAnnotationNames.ViewDefinitionSql)

            If useDataAnnotations Then
                ' Strip out any annotations handled as attributes - these are already handled when generating
                ' the entity's properties
                call _annotationCodeGenerator.GenerateDataAnnotationAttributes(entityType, annotations)
            End If

            If Not useDataAnnotations OrElse entityType.GetViewName() IsNot Nothing Then
                GenerateTableName(entityType)
            End If

            Dim lines As New List(Of String)(
                _annotationCodeGenerator.GenerateFluentApiCalls(entityType, annotations).
                    Select(Function(m) _code.Fragment(m)).
                    Concat(GenerateAnnotations(annotations.Values)))

            AppendMultiLineFluentApi(entityType, lines)

            For Each index In entityType.GetIndexes()
                ' If there are annotations that cannot be represented using an IndexAttribute then use fluent API even
                ' if useDataAnnotations is true.
                Dim indexAnnotations = _annotationCodeGenerator _
                    .FilterIgnoredAnnotations(index.GetAnnotations()) _
                    .ToDictionary(Function(a) a.Name, Function(a) a)
                _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(index, indexAnnotations)

                If Not useDataAnnotations OrElse indexAnnotations.Count > 0 Then
                    GenerateIndex(index)
                End If
            Next

            For Each prop In entityType.GetProperties()
                GenerateProperty(prop, useDataAnnotations)
            Next

            For Each foreignKey In entityType.GetForeignKeys()
                GenerateRelationship(foreignKey, useDataAnnotations)
            Next
        End Sub

        Private Sub AppendMultiLineFluentApi(entityType As IEntityType, lines As IList(Of String))
            If lines.Count <= 0 Then
                Return
            End If

            If lines.Count > 1 Then
                For i = 1 To lines.Count - 1
                    lines(i) = If(lines(i).StartsWith("."), lines(i).Remove(0, 1), lines(i))
                Next
            End If

            InitializeEntityTypeBuilder(entityType)

            Using _sb.Indent()

                _sb.Append(EntityLambdaIdentifier & lines(0))

                Using _sb.Indent()
                    For Each line In lines.Skip(1)
                        _sb.AppendLine(".")
                        _sb.Append(line)
                    Next
                End Using
                _sb.AppendLine()
            End Using
        End Sub

        Private Sub GenerateKey(akey As IKey, entityType As IEntityType, useDataAnnotations As Boolean)
            If akey Is Nothing Then
                If Not useDataAnnotations Then
                    Dim line As New List(Of String) From {
                        $".{NameOf(EntityTypeBuilder.HasNoKey)}()"}

                    AppendMultiLineFluentApi(entityType, line)
                End If

                Return
            End If

            Dim annotations = _annotationCodeGenerator.
                FilterIgnoredAnnotations(akey.GetAnnotations()).
                ToDictionary(Function(a) a.Name, Function(a) a)

            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(akey, annotations)

            Dim explicitName As Boolean = akey.GetName() <> akey.GetDefaultName()
            annotations.Remove(RelationalAnnotationNames.Name)

            If akey.Properties.Count = 1 AndAlso annotations.Count = 0 Then                
                If TypeOf aKey Is Key then
                    Dim concreteKey = DirectCast(akey, key)

                     IF akey.Properties.SequenceEqual(
                            KeyDiscoveryConvention.DiscoverKeyProperties(
                                concreteKey.DeclaringEntityType,
                                concreteKey.DeclaringEntityType.GetProperties())) Then
                        Exit sub
                    End If
                End If                
               
                If Not explicitName AndAlso useDataAnnotations Then
                    Exit sub
                End If
            End If

            Dim lines As New List(Of String) From {
                $".{NameOf(EntityTypeBuilder.HasKey)}({_code.Lambda(akey.Properties, "e")})"}

            If explicitName Then
                lines.Add(
        $".{NameOf(RelationalKeyBuilderExtensions.HasName)}({_code.Literal(akey.GetName())})")
            End If

            lines.AddRange(
                _annotationCodeGenerator.GenerateFluentApiCalls(akey, annotations).
                    Select(Function(m) _code.Fragment(m)).
                    Concat(GenerateAnnotations(annotations.Values)))

            AppendMultiLineFluentApi(akey.DeclaringEntityType, lines)
        End Sub

        Private Sub GenerateTableName(entityType As IEntityType)

            Dim tableName1 = entityType.GetTableName()
            Dim schema1 = entityType.GetSchema()
            Dim defaultSchema = entityType.Model.GetDefaultSchema()

            Dim explicitSchema As Boolean = schema1 IsNot Nothing AndAlso schema1 <> defaultSchema
            Dim explicitTable As Boolean = explicitSchema OrElse tableName1 IsNot Nothing AndAlso tableName1 <> entityType.GetDbSetName()
            If explicitTable Then
                Dim parameterString = _code.Literal(tableName1)
                If explicitSchema Then
                    parameterString += ", " & _code.Literal(schema1)
                End If

                Dim lines As New List(Of String) From {
                    $".{NameOf(RelationalEntityTypeBuilderExtensions.ToTable)}({parameterString})"}

                AppendMultiLineFluentApi(entityType, lines)
            End If

            Dim viewName1 = entityType.GetViewName()
            Dim viewSchema1 = entityType.GetViewSchema()

            Dim explicitViewSchema As Boolean = viewSchema1 IsNot Nothing AndAlso viewSchema1 <> defaultSchema
            Dim explicitViewTable As Boolean = explicitViewSchema OrElse viewName1 IsNot Nothing

            If explicitViewTable Then
                Dim parameterString = _code.Literal(viewName1)
                If explicitViewSchema Then
                    parameterString += ", " & _code.Literal(viewSchema1)
                End If

                Dim lines As New List(Of String) From {
                    $".{NameOf(RelationalEntityTypeBuilderExtensions.ToView)}({parameterString})"}

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
                $".{NameOf(EntityTypeBuilder.HasIndex)}({_code.Lambda(index.Properties, "e")}, " &
                $"{_code.Literal(index.GetDatabaseName())})"
            }
            annotations.Remove(RelationalAnnotationNames.Name)

            If index.IsUnique Then
                lines.Add($".{NameOf(IndexBuilder.IsUnique)}()")
            End If

            lines.AddRange(
                _annotationCodeGenerator.GenerateFluentApiCalls(index, annotations).
                    Select(Function(m) _code.Fragment(m)).
                    Concat(GenerateAnnotations(annotations.Values)))

            AppendMultiLineFluentApi(index.DeclaringEntityType, lines)
        End Sub

        Private Sub GenerateProperty(prop As IProperty, useDataAnnotations As Boolean)
            Dim lines As New List(Of String) From {
                $".{NameOf(EntityTypeBuilder.Property)}({_code.Lambda({prop.Name}, "e")})"}

            Dim annotations = _annotationCodeGenerator.
                FilterIgnoredAnnotations(prop.GetAnnotations()).
                ToDictionary(Function(a) a.Name, Function(a) a)

            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(prop, annotations)
            annotations.Remove(ScaffoldingAnnotationNames.ColumnOrdinal)

            If useDataAnnotations Then
                ' Strip out any annotations handled as attributes - these are already handled when generating
                ' the entity's properties
                ' Only relational ones need to be removed here. Core ones are already removed by FilterIgnoredAnnotations
                annotations.Remove(RelationalAnnotationNames.ColumnName)
                annotations.Remove(RelationalAnnotationNames.ColumnType)

                call _annotationCodeGenerator.GenerateDataAnnotationAttributes(prop, annotations)
            Else
                If Not prop.IsNullable AndAlso prop.ClrType.IsNullableType() AndAlso Not prop.IsPrimaryKey() Then
                    lines.Add($".{NameOf(PropertyBuilder.IsRequired)}()")
                End If

                Dim columnType = prop.GetConfiguredColumnType()
                If columnType IsNot Nothing Then
                    lines.Add($".{NameOf(RelationalPropertyBuilderExtensions.HasColumnType)}({_code.Literal(columnType)})")
                    annotations.Remove(RelationalAnnotationNames.ColumnType)
                End If

                Dim maxLength = prop.GetMaxLength()
                If maxLength.HasValue Then
                    lines.Add($".{NameOf(PropertyBuilder.HasMaxLength)}({_code.Literal(maxLength.Value)})")
                End If

                Dim precision = prop.GetPrecision()
                Dim scale = prop.GetScale()
                If precision IsNot Nothing AndAlso scale IsNot Nothing AndAlso scale <> 0 Then
                    lines.Add($".{NameOf(PropertyBuilder.HasPrecision)}({_code.Literal(precision.Value)}, {_code.Literal(scale.Value)})")
                ElseIf precision IsNot Nothing Then
                    lines.Add($".{NameOf(PropertyBuilder.HasPrecision)}({_code.Literal(precision.Value)})")
                End If

                If prop.IsUnicode() IsNot Nothing Then
                    lines.Add($".{NameOf(PropertyBuilder.IsUnicode)}({(If(prop.IsUnicode() = False, "false", ""))})")
                End If
            End If

            Dim defaultValue = prop.GetDefaultValue()
            If defaultValue is DBNull.Value Then
                lines.Add($".{NameOf(RelationalPropertyBuilderExtensions.HasDefaultValue)}()")
                annotations.Remove(RelationalAnnotationNames.DefaultValue)
            ElseIf defaultValue IsNot Nothing Then
                lines.Add($".{NameOf(RelationalPropertyBuilderExtensions.HasDefaultValue)}({_code.UnknownLiteral(defaultValue)})")
                annotations.Remove(RelationalAnnotationNames.DefaultValue)
            End If

            Dim valueGenerated1 = prop.ValueGenerated
            Dim isRowVersion As Boolean = False

            Dim ConventionProperty = DirectCast(prop, IConventionProperty)

            Dim isConfigurationSource = ConventionProperty.GetValueGeneratedConfigurationSource().HasValue
            If isConfigurationSource Then

                Dim valueGeneratedConfigurationSource = ConventionProperty.GetValueGeneratedConfigurationSource().Value
                Dim vgc = ValueGenerationConvention.GetValueGenerated(prop)

                If valueGeneratedConfigurationSource <> ConfigurationSource.Convention AndAlso
                        (vgc Is Nothing OrElse vgc <> valueGenerated1) Then

                    Dim methodName As String = ""

                    Select Case valueGenerated1
                        Case = ValueGenerated.OnAdd
                            methodName = NameOf(PropertyBuilder.ValueGeneratedOnAdd)
                        Case = ValueGenerated.OnAddOrUpdate
                            methodName = If(prop.IsConcurrencyToken,
                                            NameOf(PropertyBuilder.IsRowVersion),
                                            NameOf(PropertyBuilder.ValueGeneratedOnAddOrUpdate))

                        Case = ValueGenerated.OnUpdate
                            methodName = NameOf(PropertyBuilder.ValueGeneratedOnUpdate)
                        Case = ValueGenerated.Never
                            methodName = NameOf(PropertyBuilder.ValueGeneratedNever)
                        Case Else
                            Throw New InvalidOperationException(DesignStrings.UnhandledEnumValue($"{NameOf(ValueGenerated)}.{valueGenerated1}"))
                    End Select

                    lines.Add($".{methodName}()")

                End If
            End If

            If prop.IsConcurrencyToken AndAlso Not isRowVersion Then
                lines.Add($".{NameOf(PropertyBuilder.IsConcurrencyToken)}()")
            End If

            lines.AddRange(
                _annotationCodeGenerator.GenerateFluentApiCalls(prop, annotations).
                    Select(Function(m) _code.Fragment(m)).
                    Concat(GenerateAnnotations(annotations.Values)))

            Select Case lines.Count
                Case 1
                    Exit sub
                Case 2
                    lines = New List(Of String) From {lines(0) & lines(1)}
            End Select

            AppendMultiLineFluentApi(prop.DeclaringEntityType, lines)
        End Sub

        Private Sub GenerateRelationship(foreignKey As IForeignKey, useDataAnnotations As Boolean)
            
            Dim canUseDataAnnotations As Boolean = True

            Dim annotations = _annotationCodeGenerator.
                                FilterIgnoredAnnotations(foreignKey.GetAnnotations()).
                                ToDictionary(Function(a) a.Name, Function(a) a)

            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(foreignKey, annotations)

            Dim lines As New List(Of String) From {
                $".{NameOf(EntityTypeBuilder.HasOne)}(" &
                If(foreignKey.DependentToPrincipal IsNot Nothing, $"Function(d) d.{foreignKey.DependentToPrincipal.Name}", Nothing) &
                ")",
                $".{If(foreignKey.IsUnique, NameOf(ReferenceNavigationBuilder.WithOne), NameOf(ReferenceNavigationBuilder.WithMany))}" &
                "(" _
                & If(foreignKey.PrincipalToDependent IsNot Nothing, $"Function(p) p.{foreignKey.PrincipalToDependent.Name}", Nothing) &
                ")"
            }

            If Not foreignKey.PrincipalKey.IsPrimaryKey() Then
                canUseDataAnnotations = False
                lines.Add(
                    $".{NameOf(ReferenceReferenceBuilder.HasPrincipalKey)}" &
                     If(foreignKey.IsUnique, 
                        $"(Of {foreignKey.PrincipalEntityType.Name})", "") &
                        $"({_code.Lambda(foreignKey.PrincipalKey.Properties, "p")})")
            End If

            lines.Add(
            $".{NameOf(ReferenceReferenceBuilder.HasForeignKey)}" &
            If(foreignKey.IsUnique, $"(Of {foreignKey.DeclaringEntityType.Name})", "") &
            $"({_code.Lambda(foreignKey.Properties, "d")})")

            Dim defaultOnDeleteAction = If(foreignKey.IsRequired, DeleteBehavior.Cascade, DeleteBehavior.ClientSetNull)

            If foreignKey.DeleteBehavior <> defaultOnDeleteAction Then
                canUseDataAnnotations = False
                lines.Add($".{NameOf(ReferenceReferenceBuilder.OnDelete)}({_code.Literal(CType(foreignKey.DeleteBehavior, [Enum]))})")
            End If

            If Not String.IsNullOrEmpty(CStr(foreignKey(RelationalAnnotationNames.Name))) Then
                canUseDataAnnotations = False
            End If

            lines.AddRange(
                _annotationCodeGenerator.GenerateFluentApiCalls(foreignKey, annotations).
                    Select(Function(m) _code.Fragment(m)).
                    Concat(GenerateAnnotations(annotations.Values)))

            If Not useDataAnnotations OrElse Not canUseDataAnnotations Then
                AppendMultiLineFluentApi(foreignKey.DeclaringEntityType, lines)
            End If
        End Sub

        Private Sub GenerateSequence(seq As ISequence)
            Dim methodName As String = NameOf(RelationalModelBuilderExtensions.HasSequence)

            If seq.Type <> Sequence.DefaultClrType Then
                methodName &= $"(Of {_code.Reference(seq.Type)})"
            End If

            Dim parameters = _code.Literal(seq.Name)

            If Not String.IsNullOrEmpty(seq.Schema) AndAlso seq.Model.GetDefaultSchema() <> seq.Schema Then
                parameters &= $", {_code.Literal(seq.Schema)}"
            End If

            Dim lines As New List(Of String) From {
                $"modelBuilder.{methodName}({parameters})"}

            If seq.StartValue <> Sequence.DefaultStartValue Then
                lines.Add($".{NameOf(SequenceBuilder.StartsAt)}({seq.StartValue})")
            End If

            If seq.IncrementBy <> Sequence.DefaultIncrementBy Then
                lines.Add($".{NameOf(SequenceBuilder.IncrementsBy)}({seq.IncrementBy})")
            End If

            If seq.MinValue <> Sequence.DefaultMinValue Then
                lines.Add($".{NameOf(SequenceBuilder.HasMin)}({seq.MinValue})")
            End If

            If seq.MaxValue <> Sequence.DefaultMaxValue Then
                lines.Add($".{NameOf(SequenceBuilder.HasMax)}({seq.MaxValue})")
            End If

            If seq.IsCyclic <> Sequence.DefaultIsCyclic Then
                lines.Add($".{NameOf(SequenceBuilder.IsCyclic)}()")
            End If

            If lines.Count = 2 Then
                lines = New List(Of String) From {
                    lines(0) + lines(1)}
            End If

            _sb.AppendLine()
            _sb.Append(lines(0))

            Using _sb.Indent()
                For Each line In lines.Skip(1)
                    _sb.AppendLine()
                    _sb.Append(line)
                Next
            End Using

            _sb.AppendLine()
        End Sub

        Private Function GenerateAnnotations(annotations As IEnumerable(Of IAnnotation)) As IList(Of String)
            Return annotations.
                   Select(Function(a) $".HasAnnotation({_code.Literal(a.Name)}, {_code.UnknownLiteral(a.Value)})").
                   ToList()
        End Function

    End Class

End Namespace