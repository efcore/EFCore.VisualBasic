Imports System
Imports System.Collections.Generic
Imports System.ComponentModel.DataAnnotations
Imports System.ComponentModel.DataAnnotations.Schema
Imports System.Linq
Imports JetBrains.Annotations
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design.Internal
Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal
Imports Microsoft.EntityFrameworkCore.Metadata.Internal

' <summary>
'     This API supports the Entity Framework Core infrastructure and is not intended to be used
'     directly from your code. This API may change or be removed in future releases.
' </summary>
Public Class VisualBasicEntityTypeGenerator
    Implements IVisualBasicEntityTypeGenerator

    Private ReadOnly Property VisualBasicUtilities As IVisualBasicUtilities

    Private _sb As IndentedStringBuilder
    Private _useDataAnnotations As Boolean

    ' <summary>
    '     This API supports the Entity Framework Core infrastructure and is not intended to be used
    '     directly from your code. This API may change or be removed in future releases.
    ' </summary>
    Public Sub New(vbBasicUtilities As IVisualBasicUtilities)
        VisualBasicUtilities = vbBasicUtilities
    End Sub

    ' <summary>
    '     This API supports the Entity Framework Core infrastructure and is not intended to be used
    '     directly from your code. This API may change or be removed in future releases.
    ' </summary>
    Public Overridable Function WriteCode(entityType As IEntityType, [namespace] As String, useDataAnnotations As Boolean) As String Implements IVisualBasicEntityTypeGenerator.WriteCode
        _sb = New IndentedStringBuilder()
        _useDataAnnotations = useDataAnnotations

        _sb.AppendLine("Imports System")
        _sb.AppendLine("Imports System.Collections.Generic")

        If _useDataAnnotations Then
            _sb.AppendLine("Imports System.ComponentModel.DataAnnotations")
            _sb.AppendLine("Imports System.ComponentModel.DataAnnotations.Schema")
        End If

        For Each ns In entityType.GetProperties() _
                                 .SelectMany(Function(p) p.ClrType.GetNamespaces()) _
                                 .Where(Function(nsp) nsp <> "System" AndAlso nsp <> "System.Collections.Generic") _
                                 .Distinct() _
                                 .OrderBy(Function(x) x, New NamespaceComparer())
            _sb.AppendLine($"Imports {ns}")
        Next

        _sb.AppendLine()
        _sb.AppendLine($"Namespace {[namespace]}")

        Using _sb.Indent()
            GenerateClass(entityType)
        End Using

        _sb.AppendLine("End Namespace")

        Return _sb.ToString()
    End Function

    ' <summary>
    '     This API supports the Entity Framework Core infrastructure and is not intended to be used
    '     directly from your code. This API may change or be removed in future releases.
    ' </summary>
    Protected Overridable Sub GenerateClass(entityType As IEntityType)
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

    ' <summary>
    '     This API supports the Entity Framework Core infrastructure and is not intended to be used
    '     directly from your code. This API may change or be removed in future releases.
    ' </summary>
    Protected Overridable Sub GenerateEntityTypeDataAnnotations(entityType As IEntityType)
        GenerateTableAttribute(entityType)
    End Sub

    Private Sub GenerateTableAttribute(entityType As IEntityType)
        Dim tableName = entityType.Relational().TableName
        Dim schema = entityType.Relational().Schema
        Dim defaultSchema = entityType.Model.Relational().DefaultSchema

        Dim schemaParameterNeeded = Not schema Is Nothing AndAlso schema <> defaultSchema
        Dim tableAttributeNeeded = schemaParameterNeeded OrElse Not tableName Is Nothing AndAlso tableName <> entityType.Scaffolding().DbSetName

        If tableAttributeNeeded Then
            Dim tableAttr = New AttributeWriter(NameOf(TableAttribute))

            tableAttr.AddParameter(VisualBasicUtilities.DelimitString(tableName))

            If schemaParameterNeeded Then
                tableAttr.AddParameter($"{NameOf(TableAttribute.Schema)} = {VisualBasicUtilities.DelimitString(schema)}")
            End If

            _sb.AppendLine(tableAttr.ToString())
        End If
    End Sub

    ' <summary>
    '     This API supports the Entity Framework Core infrastructure and is not intended to be used
    '     directly from your code. This API may change or be removed in future releases.
    ' </summary>
    Protected Overridable Sub GenerateConstructor(entityType As IEntityType)
        Dim collectionNavigations = entityType.GetNavigations().Where(Function(n) n.IsCollection()).ToList()

        If collectionNavigations.Count > 0 Then
            _sb.AppendLine($"Public Sub New()")

            Using _sb.Indent()
                For Each navigation In collectionNavigations
                    _sb.AppendLine($"{navigation.Name} = New HashSet(Of {navigation.GetTargetType().Name})();")
                Next
            End Using

            _sb.AppendLine("End Sub")
            _sb.AppendLine()
        End If
    End Sub

    ' <summary>
    '     This API supports the Entity Framework Core infrastructure and is not intended to be used
    '     directly from your code. This API may change or be removed in future releases.
    ' </summary>
    Protected Overridable Sub GenerateProperties(entityType As IEntityType)
        For Each prop In entityType.GetProperties().OrderBy(Function(p) p.Scaffolding().ColumnOrdinal)
            If _useDataAnnotations Then
                GeneratePropertyDataAnnotations(prop)
            End If

            _sb.AppendLine($"Public Property {prop.Name} As {VisualBasicUtilities.GetTypeName(prop.ClrType)}")
        Next
    End Sub

    ' <summary>
    '     This API supports the Entity Framework Core infrastructure and is not intended to be used
    '     directly from your code. This API may change or be removed in future releases.
    ' </summary>
    Protected Overridable Sub GeneratePropertyDataAnnotations(prop As IProperty)
        GenerateKeyAttribute(prop)
        GenerateRequiredAttribute(prop)
        GenerateColumnAttribute(prop)
        GenerateMaxLengthAttribute(prop)
    End Sub

    Private Sub GenerateKeyAttribute(prop As IProperty)
        Dim key = prop.AsProperty().PrimaryKey

        If key?.Properties.Count = 1 Then
            If TypeOf key Is Key AndAlso key.Properties.SequenceEqual(New KeyDiscoveryConvention(Nothing).DiscoverKeyProperties(CType(key, Key).DeclaringEntityType, CType(key, Key).DeclaringEntityType.GetProperties().ToList())) Then
                Return
            End If

            If (key.Relational().Name <> ConstraintNamer.GetDefaultName(key)) Then
                Return
            End If

            _sb.AppendLine(New AttributeWriter(NameOf(KeyAttribute)))
        End If
    End Sub

    Private Sub GenerateColumnAttribute(prop As IProperty)
        Dim columnName = prop.Relational().ColumnName
        Dim columnType = prop.GetConfiguredColumnType()

        Dim delimitedColumnName = If(Not columnName Is Nothing AndAlso columnName <> prop.Name, VisualBasicUtilities.DelimitString(columnName), Nothing)
        Dim delimitedColumnType = If(Not columnType Is Nothing, VisualBasicUtilities.DelimitString(columnType), Nothing)

        If Not If(delimitedColumnName, delimitedColumnType) Is Nothing Then
            Dim columnAttr = New AttributeWriter(NameOf(ColumnAttribute))

            If Not delimitedColumnName Is Nothing Then
                columnAttr.AddParameter(delimitedColumnName)
            End If

            If Not delimitedColumnType Is Nothing Then
                columnAttr.AddParameter($"{NameOf(ColumnAttribute.TypeName)} = {delimitedColumnType}")
            End If

            _sb.AppendLine(columnAttr)
        End If
    End Sub

    Private Sub GenerateMaxLengthAttribute(prop As IProperty)
        Dim maxLength = prop.GetMaxLength()

        If (maxLength.HasValue) Then
            Dim lengthAttribute = New AttributeWriter(If(prop.ClrType = GetType(String),
                                                        NameOf(StringLengthAttribute),
                                                        NameOf(MaxLengthAttribute)))

            lengthAttribute.AddParameter(VisualBasicUtilities.GenerateLiteral(maxLength.Value))

            _sb.AppendLine(lengthAttribute.ToString())
        End If
    End Sub

    Private Sub GenerateRequiredAttribute(prop As IProperty)
        If Not prop.IsNullable AndAlso prop.ClrType.IsNullableType() AndAlso Not prop.IsPrimaryKey() Then
            _sb.AppendLine(New AttributeWriter(NameOf(RequiredAttribute)).ToString())
        End If
    End Sub

    ' <summary>
    '     This API supports the Entity Framework Core infrastructure and is not intended to be used
    '     directly from your code. This API may change or be removed in future releases.
    ' </summary>
    Protected Overridable Sub GenerateNavigationProperties(entityType As IEntityType)
        Dim sortedNavigations = entityType.GetNavigations() _
                                          .OrderBy(Function(n) If(n.IsDependentToPrincipal(), 0, 1)) _
                                          .ThenBy(Function(n) If(n.IsCollection(), 1, 0))

        If sortedNavigations.Any() Then
            _sb.AppendLine()

            For Each navigation In sortedNavigations
                If (_useDataAnnotations) Then
                    GenerateNavigationDataAnnotations(navigation)
                End If

                Dim referencedTypeName = navigation.GetTargetType().Name
                Dim navigationType = If(navigation.IsCollection(), $"ICollection(Of {referencedTypeName})", referencedTypeName)
                _sb.AppendLine($"Public Property {navigation.Name} As {navigationType}")
            Next
        End If
    End Sub

    Private Sub GenerateNavigationDataAnnotations(navigation As INavigation)
        GenerateForeignKeyAttribute(navigation)
        GenerateInversePropertyAttribute(navigation)
    End Sub

    Private Sub GenerateForeignKeyAttribute(navigation As INavigation)
        If navigation.IsDependentToPrincipal() Then
            If navigation.ForeignKey.PrincipalKey.IsPrimaryKey() Then
                Dim foreignKeyAttr = New AttributeWriter(NameOf(ForeignKeyAttribute))
                foreignKeyAttr.AddParameter(VisualBasicUtilities.DelimitString(String.Join(",", navigation.ForeignKey.Properties.[Select](Function(p) p.Name))))
                _sb.AppendLine(foreignKeyAttr.ToString())
            End If
        End If
    End Sub

    Private Sub GenerateInversePropertyAttribute(navigation As INavigation)
        If navigation.ForeignKey.PrincipalKey.IsPrimaryKey() Then
            Dim inverseNavigation = navigation.FindInverse()

            If Not inverseNavigation Is Nothing Then
                Dim InversePropertyAttr = New AttributeWriter(NameOf(InversePropertyAttribute))

                InversePropertyAttr.AddParameter(VisualBasicUtilities.DelimitString(inverseNavigation.Name))

                _sb.AppendLine(InversePropertyAttr.ToString())
            End If
        End If
    End Sub

    Private Class AttributeWriter

        Private ReadOnly _attributeName As String

        Private ReadOnly _parameters As List(Of String) = New List(Of String)()

        Public Sub New(attributeName As String)
            _attributeName = attributeName
        End Sub

        Public Sub AddParameter(parameter As String)
            _parameters.Add(parameter)
        End Sub

        Public Overrides Function ToString() As String
            Return "<" & (If(_parameters.Count = 0, StripAttribute(_attributeName), StripAttribute(_attributeName) & "(" + String.Join(", ", _parameters) & ")")) & ">"
        End Function

        Private Shared Function StripAttribute(attributeName As String) As String
            Return If(attributeName.EndsWith("Attribute", StringComparison.Ordinal), attributeName.Substring(0, attributeName.Length - 9), attributeName)
        End Function

    End Class
End Class