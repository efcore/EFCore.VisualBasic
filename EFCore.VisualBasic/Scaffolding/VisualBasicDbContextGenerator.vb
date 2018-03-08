Imports Bricelam.EntityFrameworkCore.VisualBasic
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.ChangeTracking.Internal
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Builders
Imports Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Scaffolding

' <summary>
'     This API supports the Entity Framework Core infrastructure and is not intended to be used
'     directly from your code. This API may change or be removed in future releases.
' </summary>
Public Class VisualBasicDbContextGenerator
    Implements IVisualBasicDbContextGenerator

    Private Const EntityLambdaIdentifier As String = "entity"

    Private Const Language As String = "VB"

    Private ReadOnly _visualBasicUtilities As IVisualBasicUtilities

#Disable Warning BC40000 ' Type Or member Is obsolete
    Private ReadOnly _legacyProviderCodeGenerator As IScaffoldingProviderCodeGenerator
#Enable Warning BC40000

    Private ReadOnly _providerCodeGenerator As IProviderCodeGenerator

    Private ReadOnly _annotationCodeGenerator As IAnnotationCodeGenerator

    Private _sb As IndentedStringBuilder

    Private _entityTypeBuilderInitialized As Boolean

#Disable Warning BC40000 ' Type Or member Is obsolete

    ' <summary>
    '     This API supports the Entity Framework Core infrastructure and is not intended to be used
    '     directly from your code. This API may change or be removed in future releases.
    ' </summary>
    Public Sub New(legacyProviderCodeGenerators As IEnumerable(Of IScaffoldingProviderCodeGenerator),
                   providerCodeGenerators As IEnumerable(Of IProviderCodeGenerator),
                   annotationCodeGenerator As IAnnotationCodeGenerator,
                   vbUtilities As IVisualBasicUtilities)
        If Not legacyProviderCodeGenerators.Any() AndAlso Not providerCodeGenerators.Any() Then
            Throw New ArgumentException(CoreStrings.CollectionArgumentIsEmpty(NameOf(providerCodeGenerators)))
        End If

        _legacyProviderCodeGenerator = legacyProviderCodeGenerators.LastOrDefault()
        _providerCodeGenerator = providerCodeGenerators.LastOrDefault()
        _annotationCodeGenerator = annotationCodeGenerator
        _visualBasicUtilities = vbUtilities
    End Sub
#Enable Warning BC40000

    ' <summary>
    '     This API supports the Entity Framework Core infrastructure and is not intended to be used
    '     directly from your code. This API may change or be removed in future releases.
    ' </summary>
    Public Overridable Function WriteCode(model As IModel,
                              [namespace] As String,
                              contextName As String,
                              connectionString As String,
                              useDataAnnotations As Boolean,
                              suppressConnectionStringWarning As Boolean) As String Implements IVisualBasicDbContextGenerator.WriteCode
        _sb = New IndentedStringBuilder()
        _sb.AppendLine("Imports System") _
           .AppendLine("Imports Microsoft.EntityFrameworkCore") _
           .AppendLine("Imports Microsoft.EntityFrameworkCore.Metadata") _
           .AppendLine() _
           .AppendLine($"Namespace {[namespace]}")

        Using _sb.Indent()
            GenerateClass(model, contextName, connectionString, useDataAnnotations, suppressConnectionStringWarning)
        End Using

        _sb.AppendLine("End Namespace")

        Return _sb.ToString()
    End Function

    ' <summary>
    '     This API supports the Entity Framework Core infrastructure and is not intended to be used
    '     directly from your code. This API may change or be removed in future releases.
    ' </summary>
    Protected Overridable Sub GenerateClass(model As IModel,
                                            contextName As String,
                                            connectionString As String,
                                            useDataAnnotations As Boolean,
                                            suppressConnectionStringWarning As Boolean)
        _sb.AppendLine($"Public Partial Class {contextName}")
        Using _sb.Indent()
            _sb.AppendLine("Inherits DbContext").AppendLine()

            GenerateDbSets(model)
            GenerateEntityTypeErrors(model)
            GenerateOnConfiguring(connectionString, suppressConnectionStringWarning)
            GenerateOnModelCreating(model, useDataAnnotations)
        End Using

        _sb.AppendLine("End Class")
    End Sub

    ' <summary>
    '     This API supports the Entity Framework Core infrastructure and is not intended to be used
    '     directly from your code. This API may change or be removed in future releases.
    ' </summary>
    Private Sub GenerateDbSets(model As IModel)
        For Each entityType In model.GetEntityTypes()
            _sb.AppendLine($"Public Overridable Property {entityType.Scaffolding().DbSetName} As DbSet(Of {entityType.Name})")
        Next

        If model.GetEntityTypes().Any() Then
            _sb.AppendLine()
        End If
    End Sub

    Private Sub GenerateEntityTypeErrors(model As IModel)
        For Each entityTypeError In model.Scaffolding().EntityTypeErrors
            'TODO : See GenerateOnConfiguring for warning messages
            _sb.AppendLine($"' {entityTypeError.Value} Please see the warning messages.")
        Next

        If model.Scaffolding().EntityTypeErrors.Any() Then
            _sb.AppendLine()
        End If
    End Sub

    ' <summary>
    '     This API supports the Entity Framework Core infrastructure and is not intended to be used
    '     directly from your code. This API may change or be removed in future releases.
    ' </summary>
    Protected Overridable Sub GenerateOnConfiguring(connectionString As String, suppressConnectionStringWarning As Boolean)
        _sb.AppendLine("Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)")
        Using _sb.Indent()
            _sb.AppendLine("if Not optionsBuilder.IsConfigured")
            Using _sb.Indent()
                'TODO : We should find an acceptable way to suggest the user remove the connection string from code.
                'Warnings can not be created from VB code...

                'If Not suppressConnectionStringWarning Then
                '    _sb.DecrementIndent() _
                '       .DecrementIndent() _
                '       .DecrementIndent() _
                '       .DecrementIndent() _
                '       .AppendLine("#warning " & DesignStrings.SensitiveInformationWarning) _
                '       .IncrementIndent() _
                '       .IncrementIndent() _
                '       .IncrementIndent() _
                '       .IncrementIndent()
                'End If

#Disable Warning BC40000
                _sb.Append("optionsBuilder").Append(
                    If(Not _providerCodeGenerator Is Nothing,
                       _visualBasicUtilities.Generate(_providerCodeGenerator.GenerateUseProvider(connectionString)),
                       _legacyProviderCodeGenerator.GenerateUseProvider(connectionString, Language))) _
                    .AppendLine(";")
#Enable Warning BC40000
            End Using

            _sb.AppendLine("End If")
        End Using

        _sb.AppendLine("End Sub")
        _sb.AppendLine()
    End Sub


    Protected Overridable Sub GenerateOnModelCreating(model As IModel, useDataAnnotations As Boolean)
        _sb.AppendLine("Protected Overrides Sub OnModelCreating(modelBuilder As ModelBuilder)")

        Dim annotations = model.GetAnnotations().ToList()
        RemoveAnnotation(annotations, ChangeDetector.SkipDetectChangesAnnotation)
        RemoveAnnotation(annotations, RelationalAnnotationNames.MaxIdentifierLength)
        RemoveAnnotation(annotations, ScaffoldingAnnotationNames.DatabaseName)
        RemoveAnnotation(annotations, ScaffoldingAnnotationNames.EntityTypeErrors)

        Dim annotationsToRemove = New List(Of IAnnotation)()
        annotationsToRemove.AddRange(annotations.Where(Function(a) a.Name.StartsWith(RelationalAnnotationNames.SequencePrefix, StringComparison.Ordinal)))

        Dim lines = New List(Of String)()
        For Each annotation In annotations
            If _annotationCodeGenerator.IsHandledByConvention(model, annotation) Then
                annotationsToRemove.Add(annotation)
            Else
                Dim methodCall = _annotationCodeGenerator.GenerateFluentApi(model, annotation)

#Disable Warning BC40000 ' Type or Member is obsolete
                Dim line = If(methodCall Is Nothing, _annotationCodeGenerator.GenerateFluentApi(model, annotation, Language), _visualBasicUtilities.Generate(methodCall))
#Enable Warning BC40000

                If line IsNot Nothing Then
                    lines.Add(line)
                    annotationsToRemove.Add(annotation)
                End If
            End If
        Next

        lines.AddRange(GenerateAnnotations(annotations.Except(annotationsToRemove)))

        If lines.Count > 0 Then
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
                    _sb.AppendLine("});")
                End If
            Next

            For Each sequence In model.Relational().Sequences
                GenerateSequence(sequence)
            Next
        End Using

        _sb.AppendLine("End Sub")
    End Sub


    Private Sub InitializeEntityTypeBuilder(entityType As IEntityType)
        If Not _entityTypeBuilderInitialized Then
            _sb.AppendLine()
            _sb.AppendLine($"modelBuilder.Entity(Of {entityType.Name})(Sub({EntityLambdaIdentifier}")
        End If

        _entityTypeBuilderInitialized = True
    End Sub

    Private Sub GenerateEntityType(entityType As IEntityType, useDataAnnotations As Boolean)
        GenerateKey(entityType.FindPrimaryKey(), useDataAnnotations)

        Dim annotations = entityType.GetAnnotations().ToList()
        RemoveAnnotation(annotations, RelationalAnnotationNames.TableName)
        RemoveAnnotation(annotations, RelationalAnnotationNames.Schema)
        RemoveAnnotation(annotations, ScaffoldingAnnotationNames.DbSetName)

        If Not useDataAnnotations Then
            GenerateTableName(entityType)
        End If

        Dim annotationsToRemove = New List(Of IAnnotation)()
        Dim lines = New List(Of String)()

        For Each annotation In annotations
            If _annotationCodeGenerator.IsHandledByConvention(entityType, annotation) Then
                annotationsToRemove.Add(annotation)
            Else
                Dim methodCall = _annotationCodeGenerator.GenerateFluentApi(entityType, annotation)

#Disable Warning BC40000 ' Type or Member is obsolete
                Dim line = If(methodCall Is Nothing, _annotationCodeGenerator.GenerateFluentApi(entityType, annotation, Language), _visualBasicUtilities.Generate(methodCall))
#Enable Warning BC40000

                If line IsNot Nothing Then
                    lines.Add(line)
                    annotationsToRemove.Add(annotation)
                End If
            End If
        Next

        lines.AddRange(GenerateAnnotations(annotations.Except(annotationsToRemove)))
        AppendMultiLineFluentApi(entityType, lines)

        For Each index In entityType.GetIndexes()
            GenerateIndex(index)
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

        InitializeEntityTypeBuilder(entityType)

        Using _sb.Indent()
            _sb.AppendLine()
            _sb.Append(EntityLambdaIdentifier + lines(0))
            Using _sb.Indent()
                For Each line In lines.Skip(1)
                    _sb.AppendLine(" _")
                    _sb.Append(line)
                Next
            End Using
        End Using
    End Sub


    Private Sub GenerateKey(key As IKey, useDataAnnotations As Boolean)
        If key Is Nothing Then
            Return
        End If

        Dim annotations = key.GetAnnotations().ToList()

        Dim explicitName = key.Relational().Name <> ConstraintNamer.GetDefaultName(key)
        RemoveAnnotation(annotations, RelationalAnnotationNames.Name)

        If key.Properties.Count = 1 AndAlso annotations.Count = 0 Then

            If TypeOf key Is Key Then
                Dim concreteKey = CType(key, Key)
                If (concreteKey.Properties.SequenceEqual(
                        New KeyDiscoveryConvention(Nothing).DiscoverKeyProperties(
                            concreteKey.DeclaringEntityType,
                            concreteKey.DeclaringEntityType.GetProperties().ToList()))) Then
                    Return
                End If
            End If

            If Not explicitName AndAlso useDataAnnotations Then
                Return
            End If
        End If

        Dim lines = New List(Of String) From
            {
                $".{NameOf(EntityTypeBuilder.HasKey)}(Function(e) {GenerateLambdaToKey(key.Properties, "e")})"
        }

        If (explicitName) Then
            lines.Add($".{NameOf(RelationalKeyBuilderExtensions.HasName)}" +
                $"({_visualBasicUtilities.DelimitString(key.Relational().Name)})")
        End If

        Dim annotationsToRemove = New List(Of IAnnotation)

        For Each annotation In annotations
            If _annotationCodeGenerator.IsHandledByConvention(key, annotation) Then
                annotationsToRemove.Add(annotation)
            Else
                Dim methodCall = _annotationCodeGenerator.GenerateFluentApi(key, annotation)


#Disable Warning BC40000 ' Type or Member is obsolete
                Dim line = If(methodCall Is Nothing,
                  _annotationCodeGenerator.GenerateFluentApi(key, annotation, Language),
                  _visualBasicUtilities.Generate(methodCall))
#Enable Warning BC40000

                If Not line Is Nothing Then
                    lines.Add(line)
                    annotationsToRemove.Add(annotation)
                End If
            End If
        Next

        lines.AddRange(GenerateAnnotations(annotations.Except(annotationsToRemove)))

        AppendMultiLineFluentApi(key.DeclaringEntityType, lines)
    End Sub


    Private Sub GenerateTableName(ByVal entityType As IEntityType)
        Dim tableName = entityType.Relational().TableName
        Dim schema = entityType.Relational().Schema
        Dim defaultSchema = entityType.Model.Relational().DefaultSchema
        Dim explicitSchema = schema IsNot Nothing AndAlso schema <> defaultSchema
        Dim explicitTable = explicitSchema OrElse tableName IsNot Nothing AndAlso tableName <> entityType.Scaffolding().DbSetName

        If explicitTable Then
            Dim parameterString = _visualBasicUtilities.DelimitString(tableName)
            If explicitSchema Then
                parameterString += ", " & _visualBasicUtilities.DelimitString(schema)
            End If

            Dim lines = New List(Of String) From {$".{NameOf(RelationalEntityTypeBuilderExtensions.ToTable)}({parameterString})"}
            AppendMultiLineFluentApi(entityType, lines)
        End If
    End Sub


    Private Sub GenerateIndex(index As IIndex)
        Dim lines = New List(Of String) From
            {
                $".{NameOf(EntityTypeBuilder.HasIndex)}(Function(e) {GenerateLambdaToKey(index.Properties, "e")})"
            }

        Dim annotations = index.GetAnnotations().ToList()

        If Not String.IsNullOrEmpty(CStr(index(RelationalAnnotationNames.Name))) Then

            lines.Add(
                    $".{NameOf(RelationalIndexBuilderExtensions.HasName)}" +
                    $"({_visualBasicUtilities.DelimitString(index.Relational().Name)})")
            RemoveAnnotation(annotations, RelationalAnnotationNames.Name)
        End If

        If index.IsUnique Then
            lines.Add($".{NameOf(IndexBuilder.IsUnique)}()")
        End If

        If Not index.Relational().Filter Is Nothing Then
            lines.Add(
                $".{NameOf(RelationalIndexBuilderExtensions.HasFilter)}" +
                $"({_visualBasicUtilities.DelimitString(index.Relational().Filter)})")
            RemoveAnnotation(annotations, RelationalAnnotationNames.Filter)
        End If

        Dim annotationsToRemove = New List(Of IAnnotation)

        For Each annotation In annotations

            If (_annotationCodeGenerator.IsHandledByConvention(index, annotation)) Then
                annotationsToRemove.Add(annotation)
            Else
                Dim methodCall = _annotationCodeGenerator.GenerateFluentApi(index, annotation)

#Disable Warning BC40000 ' Type or Member is obsolete
                Dim line = If(methodCall Is Nothing,
                               _annotationCodeGenerator.GenerateFluentApi(index, annotation, Language),
                               _visualBasicUtilities.Generate(methodCall))
#Enable Warning BC40000

                If Not line Is Nothing Then
                    lines.Add(line)
                    annotationsToRemove.Add(annotation)
                End If
            End If
        Next

        lines.AddRange(GenerateAnnotations(annotations.Except(annotationsToRemove)))

        AppendMultiLineFluentApi(index.DeclaringEntityType, lines)
    End Sub

    Private Sub GenerateProperty([property] As IProperty, useDataAnnotations As Boolean)
        Dim lines = New List(Of String) From {$".{NameOf(EntityTypeBuilder.[Property])}(Function(e) e.{[property].Name})"}
        Dim annotations = [property].GetAnnotations().ToList()

        RemoveAnnotation(annotations, RelationalAnnotationNames.ColumnName)
        RemoveAnnotation(annotations, RelationalAnnotationNames.ColumnType)
        RemoveAnnotation(annotations, CoreAnnotationNames.MaxLengthAnnotation)
        RemoveAnnotation(annotations, CoreAnnotationNames.UnicodeAnnotation)
        RemoveAnnotation(annotations, RelationalAnnotationNames.DefaultValue)
        RemoveAnnotation(annotations, RelationalAnnotationNames.DefaultValueSql)
        RemoveAnnotation(annotations, RelationalAnnotationNames.ComputedColumnSql)
        RemoveAnnotation(annotations, RelationalAnnotationNames.IsFixedLength)
        RemoveAnnotation(annotations, ScaffoldingAnnotationNames.ColumnOrdinal)

        If Not useDataAnnotations Then
            If Not [property].IsNullable AndAlso [property].ClrType.IsNullableType() AndAlso Not [property].IsPrimaryKey() Then
                lines.Add($".{NameOf(PropertyBuilder.IsRequired)}()")
            End If

            Dim columnName = [property].Relational().ColumnName
            If columnName IsNot Nothing AndAlso columnName <> [property].Name Then
                lines.Add($".{NameOf(RelationalPropertyBuilderExtensions.HasColumnName)}" & $"({_visualBasicUtilities.DelimitString(columnName)})")
            End If

            Dim columnType = [property].GetConfiguredColumnType()
            If columnType IsNot Nothing Then
                lines.Add($".{NameOf(RelationalPropertyBuilderExtensions.HasColumnType)}" & $"({_visualBasicUtilities.DelimitString(columnType)})")
            End If

            Dim maxLength = [property].GetMaxLength()
            If maxLength.HasValue Then
                lines.Add($".{NameOf(PropertyBuilder.HasMaxLength)}" & $"({_visualBasicUtilities.GenerateLiteral(maxLength.Value)})")
            End If
        End If

        If [property].IsUnicode() IsNot Nothing Then
            lines.Add($".{NameOf(PropertyBuilder.IsUnicode)}" & $"({(If([property].IsUnicode() = False, _visualBasicUtilities.GenerateLiteral(False), ""))})")
        End If

        If [property].Relational().IsFixedLength Then
            lines.Add($".{NameOf(RelationalPropertyBuilderExtensions.IsFixedLength)}()")
        End If

        If [property].Relational().DefaultValue IsNot Nothing Then
            lines.Add($".{NameOf(RelationalPropertyBuilderExtensions.HasDefaultValue)}" & $"({CType(_visualBasicUtilities, VisualBasicUtilities).ExecuteGenerateLiteral([property].Relational().DefaultValue)})")
        End If

        If [property].Relational().DefaultValueSql IsNot Nothing Then
            lines.Add($".{NameOf(RelationalPropertyBuilderExtensions.HasDefaultValueSql)}" & $"({_visualBasicUtilities.DelimitString([property].Relational().DefaultValueSql)})")
        End If

        If [property].Relational().ComputedColumnSql IsNot Nothing Then
            lines.Add($".{NameOf(RelationalPropertyBuilderExtensions.HasComputedColumnSql)}" & $"({_visualBasicUtilities.DelimitString([property].Relational().ComputedColumnSql)})")
        End If

        Dim valueGenerated = [property].ValueGenerated
        Dim isRowVersion = False
        If (CType([property], [Property])).GetValueGeneratedConfigurationSource().HasValue AndAlso New RelationalValueGeneratorConvention().GetValueGenerated(CType([property], [Property])) <> valueGenerated Then
            Dim methodName As String
            Select Case valueGenerated
                Case ValueGenerated.OnAdd
                    methodName = NameOf(PropertyBuilder.ValueGeneratedOnAdd)
                Case ValueGenerated.OnAddOrUpdate
                    isRowVersion = [property].IsConcurrencyToken
                    methodName = If(isRowVersion, NameOf(PropertyBuilder.IsRowVersion), NameOf(PropertyBuilder.ValueGeneratedOnAddOrUpdate))
                Case ValueGenerated.Never
                    methodName = NameOf(PropertyBuilder.ValueGeneratedNever)
                Case Else
                    methodName = ""
            End Select

            lines.Add($".{methodName}()")
        End If

        If [property].IsConcurrencyToken AndAlso Not isRowVersion Then
            lines.Add($".{NameOf(PropertyBuilder.IsConcurrencyToken)}()")
        End If

        Dim annotationsToRemove = New List(Of IAnnotation)()
        For Each annotation In annotations
            If _annotationCodeGenerator.IsHandledByConvention([property], annotation) Then
                annotationsToRemove.Add(annotation)
            Else
                Dim methodCall = _annotationCodeGenerator.GenerateFluentApi([property], annotation)

#Disable Warning BC40000 ' Type or Member is obsolete
                Dim line = If(methodCall Is Nothing,
                              _annotationCodeGenerator.GenerateFluentApi([property], annotation, Language),
                              _visualBasicUtilities.Generate(methodCall))
#Enable Warning BC40000 ' Type or Member is obsolete

                If line IsNot Nothing Then
                    lines.Add(line)
                    annotationsToRemove.Add(annotation)
                End If
            End If
        Next

        lines.AddRange(GenerateAnnotations(annotations.Except(annotationsToRemove)))
        Select Case lines.Count
            Case 1
                Return
            Case 2
                lines = New List(Of String) From {lines(0) + lines(1)}
        End Select

        AppendMultiLineFluentApi([property].DeclaringEntityType, lines)
    End Sub

    Private Sub GenerateRelationship(ByVal foreignKey As IForeignKey, ByVal useDataAnnotations As Boolean)
        Dim canUseDataAnnotations = True
        Dim annotations = foreignKey.GetAnnotations().ToList()
        Dim lines = New List(Of String) From {$".{NameOf(EntityTypeBuilder.HasOne)}(Function(d) d.{foreignKey.DependentToPrincipal.Name})", $".{(If(foreignKey.IsUnique, NameOf(ReferenceNavigationBuilder.WithOne), NameOf(ReferenceNavigationBuilder.WithMany)))}" & $"(Function(p) p.{foreignKey.PrincipalToDependent.Name})"}

        If Not foreignKey.PrincipalKey.IsPrimaryKey() Then
            canUseDataAnnotations = False
            lines.Add($".{NameOf(ReferenceReferenceBuilder.HasPrincipalKey)}" &
                      $"{(If(foreignKey.IsUnique,
                          $"(Of {foreignKey.PrincipalEntityType.DisplayName()})", ""))}" & $"(Function(p) {GenerateLambdaToKey(foreignKey.PrincipalKey.Properties,
                          "p")})")
        End If

        lines.Add($".{NameOf(ReferenceReferenceBuilder.HasForeignKey)}" & $"{(If(foreignKey.IsUnique, $"(Of {foreignKey.DeclaringEntityType.DisplayName()})", ""))}" & $"(Function(d) {GenerateLambdaToKey(foreignKey.Properties, "d")})")
        Dim defaultOnDeleteAction = If(foreignKey.IsRequired, DeleteBehavior.Cascade, DeleteBehavior.ClientSetNull)

        If foreignKey.DeleteBehavior <> defaultOnDeleteAction Then
            canUseDataAnnotations = False
            lines.Add($".{NameOf(ReferenceReferenceBuilder.OnDelete)}" & $"({_visualBasicUtilities.GenerateLiteral(foreignKey.DeleteBehavior)})")
        End If

        If Not String.IsNullOrEmpty(CStr(foreignKey(RelationalAnnotationNames.Name))) Then
            canUseDataAnnotations = False
            lines.Add($".{NameOf(RelationalReferenceReferenceBuilderExtensions.HasConstraintName)}" & $"({_visualBasicUtilities.DelimitString(foreignKey.Relational().Name)})")
            RemoveAnnotation(annotations, RelationalAnnotationNames.Name)
        End If

        Dim annotationsToRemove = New List(Of IAnnotation)()
        For Each annotation In annotations
            If _annotationCodeGenerator.IsHandledByConvention(foreignKey, annotation) Then
                annotationsToRemove.Add(annotation)
            Else
                Dim methodCall = _annotationCodeGenerator.GenerateFluentApi(foreignKey, annotation)

#Disable Warning BC40000 ' Type or Member is obsolete
                Dim line = If(methodCall Is Nothing, _annotationCodeGenerator.GenerateFluentApi(foreignKey, annotation, Language), _visualBasicUtilities.Generate(methodCall))
#Enable Warning BC40000 ' Type or Member is obsolete

                If line IsNot Nothing Then
                    canUseDataAnnotations = False
                    lines.Add(line)
                    annotationsToRemove.Add(annotation)
                End If
            End If
        Next

        lines.AddRange(GenerateAnnotations(annotations.Except(annotationsToRemove)))
        If Not useDataAnnotations OrElse Not canUseDataAnnotations Then
            AppendMultiLineFluentApi(foreignKey.DeclaringEntityType, lines)
        End If
    End Sub

    Private Sub GenerateSequence(ByVal seq As ISequence)
        Dim methodName = NameOf(RelationalModelBuilderExtensions.HasSequence)
        If seq.ClrType <> Sequence.DefaultClrType Then
            methodName += $"(Of {_visualBasicUtilities.GetTypeName(seq.ClrType)})"
        End If

        Dim parameters = _visualBasicUtilities.DelimitString(seq.Name)
        If String.IsNullOrEmpty(seq.Schema) AndAlso seq.Model.Relational().DefaultSchema <> seq.Schema Then
            parameters += $", {_visualBasicUtilities.DelimitString(seq.Schema)}"
        End If

        Dim lines = New List(Of String) From {$"modelBuilder.{methodName}({parameters})"}
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
            lines = New List(Of String) From {lines(0) + lines(1)}
        End If

        _sb.AppendLine()
        _sb.Append(lines(0))
        Using _sb.Indent()
            For Each line In lines.Skip(1)
                _sb.AppendLine()
                _sb.Append(line)
            Next
        End Using
    End Sub

    Private Function GenerateLambdaToKey(ByVal properties As IReadOnlyList(Of IProperty), ByVal lambdaIdentifier As String) As String
        If properties.Count <= 0 Then
            Return ""
        End If

        Return If(properties.Count = 1, $"{lambdaIdentifier}.{properties(0).Name}", $"new With {{ {String.Join(", ", properties.[Select](Function(p) lambdaIdentifier & "." + p.Name))} }}")
    End Function

    Private Sub RemoveAnnotation(ByRef annotations As List(Of IAnnotation), ByVal annotationName As String)
        annotations.Remove(annotations.SingleOrDefault(Function(a) a.Name = annotationName))
    End Sub

    Private Function GenerateAnnotations(ByVal annotations As IEnumerable(Of IAnnotation)) As IList(Of String)
        Return annotations.[Select](Function(a) GenerateAnnotation(a)).ToList()
    End Function

    Private Function GenerateAnnotation(ByVal annotation As IAnnotation) As String
        Return $".HasAnnotation({_visualBasicUtilities.DelimitString(annotation.Name)}, " & $"{CType(_visualBasicUtilities, VisualBasicUtilities).ExecuteGenerateLiteral(annotation.Value)})"
    End Function


End Class