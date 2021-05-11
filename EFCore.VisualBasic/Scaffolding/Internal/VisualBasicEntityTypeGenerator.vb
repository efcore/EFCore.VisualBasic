' Copyright (c) .NET Foundation. All rights reserved.
' Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports EntityFrameworkCore.VisualBasic.Design
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Design.Internal
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal

Namespace Scaffolding.Internal

    Friend Class VisualBasicEntityTypeGenerator

        Private ReadOnly _annotationCodeGenerator As IAnnotationCodeGenerator
        Private ReadOnly _code As IVisualBasicHelper
        Private _sb As IndentedStringBuilder
        Private _useDataAnnotations As Boolean

        Public Sub New(annotationCodeGenerator As IAnnotationCodeGenerator,
                       vbHelper As IVisualBasicHelper)

            _annotationCodeGenerator = NotNull(annotationCodeGenerator, NameOf(annotationCodeGenerator))
            _code = NotNull(vbHelper, NameOf(vbHelper))
        End Sub

        Public Overridable Function WriteCode(entityType As IEntityType,
                                              rootNamespace As String,
                                              modelNamespace As String,
                                              useDataAnnotations As Boolean) As String

            NotNull(entityType, NameOf(entityType))
            NotNull(modelNamespace, NameOf(modelNamespace))

            _sb = New IndentedStringBuilder
            _useDataAnnotations = useDataAnnotations

            _sb.AppendLine("Imports System")
            _sb.AppendLine("Imports Microsoft.VisualBasic") 'Require for vbCrLf, vbCr, vbLf
            _sb.AppendLine("Imports System.Collections.Generic")

            If _useDataAnnotations Then
                _sb.AppendLine("Imports System.ComponentModel.DataAnnotations")
                _sb.AppendLine("Imports System.ComponentModel.DataAnnotations.Schema")
                _sb.AppendLine("Imports Microsoft.EntityFrameworkCore") ' For attributes coming out of Abstractions
            End If

            For Each ns In entityType.GetProperties().
                            SelectMany(Function(p) p.ClrType.GetNamespaces()).
                            Where(Function(x) x <> "System" AndAlso x <> "System.Collections.Generic").
                            Distinct().
                            OrderBy(Function(x) x, New NamespaceComparer)
                _sb.AppendLine($"Imports {ns}")
            Next

            modelNamespace = RemoveRootNamespaceFromNamespace(rootNamespace, modelNamespace)
            Dim addANamespace = Not String.IsNullOrWhiteSpace(modelNamespace)

            _sb.AppendLine()

            If addANamespace Then
                _sb.AppendLine($"Namespace {_code.Namespace(modelNamespace)}")
                _sb.IncrementIndent()
            End If

            GenerateClass(entityType)

            If addANamespace Then
                _sb.DecrementIndent()
                _sb.AppendLine("End Namespace")
            End If

            Return _sb.ToString()
        End Function

        Protected Overridable Sub GenerateClass(entityType As IEntityType)

            NotNull(entityType, NameOf(entityType))

            GenerateComment(entityType.GetComment())

            If _useDataAnnotations Then
                GenerateEntityTypeDataAnnotations(entityType)
            End If

            _sb.AppendLine($"Public Partial Class {entityType.Name}")

            Using _sb.Indent()
                GenerateConstructor(entityType)
                GenerateProperties(entityType)
                GenerateNavigationProperties(entityType)
            End Using

            _sb.AppendLine("End Class")
        End Sub

        Protected Overridable Sub GenerateEntityTypeDataAnnotations(entityType As IEntityType)

            NotNull(entityType, NameOf(entityType))

            GenerateKeylessAttribute(entityType)
            GenerateTableAttribute(entityType)
            GenerateIndexAttributes(entityType)

            Dim annotations = _annotationCodeGenerator _
                .FilterIgnoredAnnotations(entityType.GetAnnotations()) _
                .ToDictionary(Function(a) a.Name, Function(a) a)
            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(entityType, annotations)

            For Each attribute In _annotationCodeGenerator.GenerateDataAnnotationAttributes(entityType, annotations)
                Dim attributeWriter1 As AttributeWriter = New AttributeWriter(attribute.Type.Name)
                For Each argument In attribute.Arguments
                    attributeWriter1.AddParameter(_code.UnknownLiteral(argument))
                Next
            Next
        End Sub

        Private Sub GenerateKeylessAttribute(entityType As IEntityType)
            If entityType.FindPrimaryKey() Is Nothing Then
                _sb.AppendLine(New AttributeWriter(NameOf(KeylessAttribute)).ToString())
            End If
        End Sub

        Private Sub GenerateTableAttribute(entityType As IEntityType)
            Dim tableName = entityType.GetTableName()
            Dim schema = entityType.GetSchema()
            Dim defaultSchema = entityType.Model.GetDefaultSchema()

            Dim schemaParameterNeeded As Boolean = schema IsNot Nothing AndAlso schema <> defaultSchema
            Dim isView As Boolean = entityType.GetViewName() IsNot Nothing
            Dim tableAttributeNeeded = Not isView AndAlso (schemaParameterNeeded OrElse tableName IsNot Nothing AndAlso tableName <> entityType.GetDbSetName())
            If tableAttributeNeeded Then
                Dim tableAttribute1 As AttributeWriter = New AttributeWriter(NameOf(TableAttribute))

                tableAttribute1.AddParameter(_code.Literal(tableName))

                If schemaParameterNeeded Then
                    tableAttribute1.AddParameter($"{NameOf(TableAttribute.Schema)} :={_code.Literal(schema)}")
                End If

                _sb.AppendLine(tableAttribute1.ToString())
            End If
        End Sub

        Private Sub GenerateIndexAttributes(entityType As IEntityType)

            ' Do not generate IndexAttributes for indexes which
            ' would be generated anyway by convention.
            For Each index In entityType.GetIndexes().Where(
                Function(i) ConfigurationSource.Convention <> CType(i, IConventionIndex).GetConfigurationSource())
                ' If there are annotations that cannot be represented using an IndexAttribute then use fluent API instead.
                Dim annotations = _annotationCodeGenerator _
                    .FilterIgnoredAnnotations(index.GetAnnotations()) _
                    .ToDictionary(Function(a) a.Name, Function(a) a)
                _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(index, annotations)

                If annotations.Count = 0 Then
                    Dim indexAttribute1 As AttributeWriter = New AttributeWriter(NameOf(IndexAttribute))
                    For Each prop In index.Properties
                        indexAttribute1.AddParameter($"""{prop.Name}""")
                    Next

                    If index.Name IsNot Nothing Then
                        indexAttribute1.AddParameter($"{NameOf(IndexAttribute.Name)} :={_code.Literal(index.Name)}")
                    End If

                    If index.IsUnique Then
                        indexAttribute1.AddParameter($"{NameOf(IndexAttribute.IsUnique)} :={_code.Literal(index.IsUnique)}")
                    End If

                    _sb.AppendLine(indexAttribute1.ToString())
                End If
            Next
        End Sub

        Protected Overridable Sub GenerateConstructor(entityType As IEntityType)
            NotNull(entityType, NameOf(entityType))

            Dim collectionNavigations = entityType.GetNavigations().Where(Function(n) n.IsCollection).ToList()

            If collectionNavigations.Count > 0 Then
                _sb.AppendLine($"Public Sub New()")

                Using _sb.Indent()
                    For Each navigation In collectionNavigations
                        _sb.AppendLine($"{navigation.Name} = New HashSet(Of {navigation.TargetEntityType.Name})()")
                    Next
                End Using

                _sb.AppendLine("End Sub")
                _sb.AppendLine()
            End If
        End Sub

        Protected Overridable Sub GenerateProperties(entityType As IEntityType)
            NotNull(entityType, NameOf(entityType))

            For Each prop In entityType.GetProperties().OrderBy(Function(p) p.GetColumnOrdinal())
                GenerateComment(prop.GetComment())

                If _useDataAnnotations Then
                    GeneratePropertyDataAnnotations(prop)
                End If

                _sb.AppendLine($"Public Property {_code.Identifier(prop.Name)} As {_code.Reference(prop.ClrType)}")
            Next
        End Sub

        Protected Overridable Sub GeneratePropertyDataAnnotations(prop As IProperty)
            NotNull(prop, NameOf(prop))

            GenerateKeyAttribute(prop)
            GenerateRequiredAttribute(prop)
            GenerateColumnAttribute(prop)
            GenerateMaxLengthAttribute(prop)

            Dim annotations = _annotationCodeGenerator _
                .FilterIgnoredAnnotations(prop.GetAnnotations()) _
                .ToDictionary(Function(a) a.Name, Function(a) a)
            _annotationCodeGenerator.RemoveAnnotationsHandledByConventions(prop, annotations)

            For Each attribute In _annotationCodeGenerator.GenerateDataAnnotationAttributes(prop, annotations)
                Dim attributeWriter1 As AttributeWriter = New AttributeWriter(attribute.Type.Name)
                For Each argument In attribute.Arguments
                    attributeWriter1.AddParameter(_code.UnknownLiteral(argument))
                Next
            Next
        End Sub

        Private Sub GenerateKeyAttribute(prop As IProperty)
            Dim key = prop.FindContainingPrimaryKey()
            If key IsNot Nothing Then
                _sb.AppendLine(New AttributeWriter(NameOf(KeyAttribute)).ToString())
            End If
        End Sub

        Private Sub GenerateColumnAttribute(prop As IProperty)
            Dim columnName = prop.GetColumnBaseName()
            Dim columnType = prop.GetConfiguredColumnType()

            Dim delimitedColumnName = If(columnName IsNot Nothing AndAlso columnName <> prop.Name, _code.Literal(columnName), Nothing)
            Dim delimitedColumnType = If(columnType IsNot Nothing, _code.Literal(columnType), Nothing)

            If (If(delimitedColumnName, delimitedColumnType)) IsNot Nothing Then
                Dim columnAttribute1 As AttributeWriter = New AttributeWriter(NameOf(ColumnAttribute))

                If delimitedColumnName IsNot Nothing Then
                    columnAttribute1.AddParameter(delimitedColumnName)
                End If

                If delimitedColumnType IsNot Nothing Then
                    columnAttribute1.AddParameter($"{NameOf(ColumnAttribute.TypeName)} :={delimitedColumnType}")
                End If

                _sb.AppendLine(columnAttribute1.ToString())
            End If
        End Sub

        Private Sub GenerateRequiredAttribute(prop As IProperty)
            If Not prop.IsNullable _
                AndAlso prop.ClrType.IsNullableType() AndAlso Not prop.IsPrimaryKey() Then
                _sb.AppendLine(New AttributeWriter(NameOf(RequiredAttribute)).ToString())
            End If
        End Sub

        Private Sub GenerateMaxLengthAttribute(prop As IProperty)
            Dim maxLength = prop.GetMaxLength()

            If maxLength.HasValue Then
                Dim lengthAttribute As AttributeWriter = New AttributeWriter(
                If(prop.ClrType = GetType(String), NameOf(StringLengthAttribute), NameOf(MaxLengthAttribute)))

                lengthAttribute.AddParameter(_code.Literal(maxLength.Value))

                _sb.AppendLine(lengthAttribute.ToString())
            End If
        End Sub

        Protected Overridable Sub GenerateNavigationProperties(entityType As IEntityType)

            NotNull(entityType, NameOf(entityType))

            Dim sortedNavigations = entityType.GetNavigations().
                                    OrderBy(Function(n) If(n.IsOnDependent, 0, 1)).
                                    ThenBy(Function(n) If(n.IsCollection, 1, 0)).
                                    ToList()

            If sortedNavigations.Any() Then
                _sb.AppendLine()

                For Each navigation In sortedNavigations
                    If _useDataAnnotations Then
                        GenerateNavigationDataAnnotations(navigation)
                    End If

                    Dim referencedTypeName = navigation.TargetEntityType.Name
                    Dim navigationType = If(navigation.IsCollection, $"ICollection(Of {referencedTypeName})", referencedTypeName)
                    _sb.AppendLine($"Public Overridable Property {navigation.Name} As {navigationType}")
                Next
            End If
        End Sub

        Private Sub GenerateNavigationDataAnnotations(navigation As INavigation)
            GenerateForeignKeyAttribute(navigation)
            GenerateInversePropertyAttribute(navigation)
        End Sub

        Private Sub GenerateForeignKeyAttribute(navigation As INavigation)
            If navigation.IsOnDependent Then
                If navigation.ForeignKey.PrincipalKey.IsPrimaryKey() Then
                    Dim foreignKeyAttribute1 As AttributeWriter = New AttributeWriter(NameOf(ForeignKeyAttribute))

                    If navigation.ForeignKey.Properties.Count > 1 Then
                        foreignKeyAttribute1.AddParameter(_code.Literal(
                            String.Join(",", navigation.ForeignKey.Properties.
                                             Select(Function(p) p.Name))))
                    Else
                        foreignKeyAttribute1.AddParameter($"NameOf({navigation.ForeignKey.Properties.First().Name})")
                    End If

                    _sb.AppendLine(foreignKeyAttribute1.ToString())
                End If
            End If
        End Sub

        Private Sub GenerateInversePropertyAttribute(navigation As INavigation)
            If navigation.ForeignKey.PrincipalKey.IsPrimaryKey() Then
                Dim inverseNavigation = navigation.Inverse

                If inverseNavigation IsNot Nothing Then
                    Dim inversePropertyAttribute1 As AttributeWriter = New AttributeWriter(NameOf(InversePropertyAttribute))

                    inversePropertyAttribute1.AddParameter(
                        If(Not navigation.DeclaringEntityType.GetPropertiesAndNavigations().Any(
                            Function(m) m.Name = inverseNavigation.DeclaringEntityType.Name),
                            $"NameOf({inverseNavigation.DeclaringEntityType.Name}.{inverseNavigation.Name})",
                            _code.Literal(inverseNavigation.Name)))

                    _sb.AppendLine(inversePropertyAttribute1.ToString())
                End If
            End If
        End Sub

        Private Sub GenerateComment(comment As String)
            If Not String.IsNullOrWhiteSpace(comment) Then
                _sb.AppendLine("''' <summary>")

                For Each line As String In comment.Split({vbCrLf, vbCr, vbLf}, StringSplitOptions.None)
                    _sb.AppendLine($"''' {Security.SecurityElement.Escape(line)}")
                Next

                _sb.AppendLine("''' </summary>")
            End If
        End Sub

        Private NotInheritable Class AttributeWriter
            Private ReadOnly _attributeName As String
            Private ReadOnly _parameters As New List(Of String)

            Public Sub New(attributeName As String)
                NotEmpty(attributeName, NameOf(attributeName))

                _attributeName = attributeName
            End Sub
            Public Sub AddParameter(parameter As String)
                NotEmpty(parameter, NameOf(parameter))

                _parameters.Add(parameter)
            End Sub
            Public Overrides Function ToString() As String
                Return "<" &
                        If(_parameters.Count = 0, StripAttribute(_attributeName),
                            StripAttribute(_attributeName) & "(" & String.Join(", ", _parameters) & ")") &
                        ">"
            End Function
            Private Shared Function StripAttribute(attributeName As String) As String
                Return If(attributeName.EndsWith("Attribute", StringComparison.Ordinal),
                            attributeName.Substring(0, attributeName.Length - 9),
                            attributeName)
            End Function
        End Class
    End Class
End Namespace
