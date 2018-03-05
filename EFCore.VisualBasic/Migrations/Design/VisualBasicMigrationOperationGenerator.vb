Imports System.Reflection
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.Migrations
Imports Microsoft.EntityFrameworkCore.Migrations.Operations

''' <summary>
'''     Used to generate Visual Basic code for creating <see cref="MigrationOperation" /> objects.
''' </summary>
Public Class VisualBasicMigrationOperationGenerator
    Implements IVisualBasicMigrationOperationGenerator

    ''' <summary>
    '''     Initializes a New instance of the <see cref="VisualBasicMigrationOperationGenerator" /> class.
    ''' </summary>
    ''' <param name="dependencies"> The dependencies. </param>
    Public Sub New(dependencies As VisualBasicMigrationOperationGeneratorDependencies)

        VisualBasicDependencies = dependencies

    End Sub

    ''' <summary>
    '''     Parameter object containing dependencies for this service.
    ''' </summary>
    Protected Overridable Overloads ReadOnly Property VisualBasicDependencies As VisualBasicMigrationOperationGeneratorDependencies

    Private ReadOnly Property VBCode As IVisualBasicHelper
        Get
            Return VisualBasicDependencies.VisualBasicHelper
        End Get
    End Property
    ''' <summary>
    '''     Generates code for creating <see cref="MigrationOperation" /> objects.
    ''' </summary>
    ''' <param name="builderName"> The <see cref="MigrationOperation" /> variable name. </param>
    ''' <param name="operations"> The operations. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Public Overloads Sub Generate(builderName As String, operations As IReadOnlyList(Of MigrationOperation), builder As IndentedStringBuilder) Implements IVisualBasicMigrationOperationGenerator.Generate

        Dim first = True
        For Each operation In operations
            If first Then
                first = False
            Else
                builder.AppendLine().AppendLine()
            End If

            builder.Append(builderName)

            ExecuteGenerate(operation, builder)
        Next
    End Sub

    Private Sub ExecuteGenerate(operation As MigrationOperation, builder As IndentedStringBuilder)

        Dim method = GetType(VisualBasicMigrationOperationGenerator).GetMethod(NameOf(Generate), BindingFlags.Instance Or BindingFlags.NonPublic, Nothing, New Type() {operation.GetType(), GetType(IndentedStringBuilder)}, Nothing)

        If method Is Nothing Then
            Generate(operation, builder)
        Else
            method.Invoke(Me, {operation, builder})
        End If

    End Sub

    ''' <summary>
    '''     Generates code for an unknown <see cref="MigrationOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As MigrationOperation, builder As IndentedStringBuilder)
        Throw New InvalidOperationException(VBDesignStrings.UnknownOperation(operation.GetType()))
    End Sub

    ''' <summary>
    '''     Generates code for an <see cref="AddColumnOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As AddColumnOperation, builder As IndentedStringBuilder)

        builder.Append(".AddColumn(Of ") _
               .Append(VBCode.Reference(operation.ClrType)) _
               .AppendLine(")(")

        Using builder.Indent()
            builder.Append("name:= ").Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then

                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.AppendLine(",") _
                   .Append("table:= ") _
                   .Append(VBCode.Literal(operation.Table))

            If Not operation.ColumnType Is Nothing Then
                builder.AppendLine(",") _
                       .Append("type:= ") _
                       .Append(VBCode.Literal(operation.ColumnType))
            End If

            If operation.IsUnicode = False Then
                builder.AppendLine(",") _
                       .Append("unicode:= False")
            End If

            If operation.IsFixedLength = True Then
                builder.AppendLine(",") _
                       .Append("fixedLength:= True")
            End If

            If operation.MaxLength.HasValue Then
                builder.AppendLine(",") _
                       .Append("maxLength:= ") _
                       .Append(VBCode.Literal(operation.MaxLength.Value))
            End If

            If operation.IsRowVersion Then
                builder.AppendLine(",") _
                       .Append("rowVersion:= True")
            End If

            builder.AppendLine(",") _
                   .Append("nullable:= ") _
                   .Append(VBCode.Literal(operation.IsNullable))

            If Not operation.DefaultValueSql Is Nothing Then

                builder.AppendLine(",") _
                       .Append("defaultValueSql:= ") _
                       .Append(VBCode.Literal(operation.DefaultValueSql))

            ElseIf Not operation.ComputedColumnSql Is Nothing Then
                builder.AppendLine(",") _
                       .Append("computedColumnSql:= ") _
                       .Append(VBCode.UnknownLiteral(operation.ComputedColumnSql))

            ElseIf Not operation.DefaultValue Is Nothing Then
                builder.AppendLine(",") _
                       .Append("defaultValue:= ") _
                       .Append(VBCode.UnknownLiteral(operation.DefaultValue))
            End If

            builder.Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using

    End Sub

    ''' <summary>
    '''     Generates code for an <see cref="AddForeignKeyOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As AddForeignKeyOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".AddForeignKey(")

        Using (builder.Indent())

            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.AppendLine(",") _
                   .Append("table:= ") _
                   .Append(VBCode.Literal(operation.Table)) _
                   .AppendLine(",")

            If operation.Columns.Length = 1 Then

                builder.Append("column:= ") _
                       .Append(VBCode.Literal(operation.Columns(0)))

            Else
                builder.Append("columns:= ") _
                       .Append(VBCode.Literal(operation.Columns))
            End If

            If Not operation.PrincipalSchema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("principalSchema:= ") _
                       .Append(VBCode.Literal(operation.PrincipalSchema))
            End If

            builder.AppendLine(",") _
                   .Append("principalTable:= ") _
                   .Append(VBCode.Literal(operation.PrincipalTable)) _
                   .AppendLine(",")

            If operation.PrincipalColumns.Length = 1 Then
                builder.Append("principalColumn:= ") _
                       .Append(VBCode.Literal(operation.PrincipalColumns(0)))
            Else

                builder.Append("principalColumns:= ") _
                       .Append(VBCode.Literal(operation.PrincipalColumns))
            End If

            If operation.OnUpdate <> ReferentialAction.NoAction Then
                builder.AppendLine(",") _
                       .Append("onUpdate:= ") _
                       .Append(VBCode.Literal(CType(operation.OnUpdate, [Enum])))
            End If

            If operation.OnDelete <> ReferentialAction.NoAction Then

                builder.AppendLine(",") _
                       .Append("onDelete:= ") _
                       .Append(VBCode.Literal(CType(operation.OnDelete, [Enum])))
            End If

            builder.Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for an <see cref="AddPrimaryKeyOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As AddPrimaryKeyOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".AddPrimaryKey(")

        Using (builder.Indent())
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.AppendLine(",") _
                   .Append("table:= ") _
                   .Append(VBCode.Literal(operation.Table)) _
                   .AppendLine(",")

            If operation.Columns.Length = 1 Then
                builder.Append("column:= ") _
                       .Append(VBCode.Literal(operation.Columns(0)))
            Else

                builder.Append("columns:= ") _
                       .Append(VBCode.Literal(operation.Columns))
            End If

            builder.Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for an <see cref="AddUniqueConstraintOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As AddUniqueConstraintOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".AddUniqueConstraint(")

        Using (builder.Indent())

            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.AppendLine(",") _
                   .Append("table:= ") _
                   .Append(VBCode.Literal(operation.Table)) _
                   .AppendLine(",")

            If (operation.Columns.Length = 1) Then
                builder.Append("column:= ") _
                       .Append(VBCode.Literal(operation.Columns(0)))
            Else
                builder.Append("columns:= ") _
                       .Append(VBCode.Literal(operation.Columns))
            End If

            builder.Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for an <see cref="AlterColumnOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As AlterColumnOperation, builder As IndentedStringBuilder)
        builder.Append(".AlterColumn(Of ") _
               .Append(VBCode.Reference(operation.ClrType)) _
               .AppendLine(")(")

        Using (builder.Indent())
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.AppendLine(",") _
                   .Append("table:= ") _
                   .Append(VBCode.Literal(operation.Table))

            If Not operation.ColumnType Is Nothing Then
                builder.AppendLine(",") _
                    .Append("type:= ") _
                    .Append(VBCode.Literal(operation.ColumnType))
            End If

            If operation.IsUnicode = False Then
                builder.AppendLine(",") _
                       .Append("unicode:= False")
            End If

            If operation.IsFixedLength = True Then
                builder.AppendLine(",") _
                       .Append("fixedLength:= True")
            End If

            If operation.MaxLength.HasValue Then
                builder.AppendLine(",") _
                       .Append("maxLength:= ") _
                       .Append(VBCode.Literal(operation.MaxLength.Value))
            End If

            If operation.IsRowVersion Then
                builder.AppendLine(",") _
                       .Append("rowVersion:= True")
            End If

            builder.AppendLine(",") _
                   .Append("nullable:= ") _
                   .Append(VBCode.Literal(operation.IsNullable))

            If Not operation.DefaultValueSql Is Nothing Then
                builder.AppendLine(",") _
                       .Append("defaultValueSql:= ") _
                       .Append(VBCode.Literal(operation.DefaultValueSql))

            ElseIf Not operation.ComputedColumnSql Is Nothing Then
                builder.AppendLine(",") _
                       .Append("computedColumnSql:= ") _
                       .Append(VBCode.UnknownLiteral(operation.ComputedColumnSql))

            ElseIf Not operation.DefaultValue Is Nothing Then
                builder.AppendLine(",") _
                       .Append("defaultValue:= ") _
                       .Append(VBCode.UnknownLiteral(operation.DefaultValue))
            End If

            If Not operation.OldColumn.ClrType Is Nothing Then
                builder.AppendLine(",") _
                       .Append("oldClrType:= GetType(") _
                       .Append(VBCode.Reference(operation.OldColumn.ClrType)) _
                       .Append(")")
            End If

            If Not operation.OldColumn.ColumnType Is Nothing Then
                builder.AppendLine(",") _
                       .Append("oldType:= ") _
                       .Append(VBCode.Literal(operation.OldColumn.ColumnType))
            End If

            If operation.OldColumn.IsUnicode = False Then
                builder.AppendLine(",") _
                       .Append("oldUnicode:= False")
            End If

            If operation.OldColumn.IsFixedLength = True Then
                builder.AppendLine(",") _
                       .Append("oldFixedLength:= True")
            End If

            If operation.OldColumn.MaxLength.HasValue Then
                builder.AppendLine(",") _
                       .Append("oldMaxLength:= ") _
                       .Append(VBCode.Literal(operation.OldColumn.MaxLength.Value))
            End If

            If operation.OldColumn.IsRowVersion Then
                builder.AppendLine(",") _
                       .Append("oldRowVersion:= True")
            End If

            If operation.OldColumn.IsNullable Then
                builder.AppendLine(",") _
                       .Append("oldNullable:= True")
            End If

            If Not operation.OldColumn.DefaultValueSql Is Nothing Then
                builder.AppendLine(",") _
                       .Append("oldDefaultValueSql:= ") _
                       .Append(VBCode.Literal(operation.OldColumn.DefaultValueSql))
            ElseIf Not operation.OldColumn.ComputedColumnSql Is Nothing Then
                builder.AppendLine(",") _
                       .Append("oldComputedColumnSql:= ") _
                       .Append(VBCode.UnknownLiteral(operation.OldColumn.ComputedColumnSql))
            ElseIf Not operation.OldColumn.DefaultValue Is Nothing Then
                builder.AppendLine(",") _
                       .Append("oldDefaultValue:= ") _
                       .Append(VBCode.UnknownLiteral(operation.OldColumn.DefaultValue))
            End If

            builder.Append(")")

            Annotations(operation.GetAnnotations(), builder)
            OldAnnotations(operation.OldColumn.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for an <see cref="AlterDatabaseOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As AlterDatabaseOperation, builder As IndentedStringBuilder)

        builder.Append(".AlterDatabase()")

        Using builder.Indent()

            Annotations(operation.GetAnnotations(), builder)
            OldAnnotations(operation.OldDatabase.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for an <see cref="AlterSequenceOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As AlterSequenceOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".AlterSequence(")

        Using (builder.Indent())
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            If operation.IncrementBy <> 1 Then
                builder.AppendLine(",") _
                       .Append("incrementBy:= ") _
                       .Append(VBCode.Literal(operation.IncrementBy))
            End If

            If Not operation.MinValue Is Nothing Then
                builder.AppendLine(",") _
                       .Append("minValue:= ") _
                       .Append(VBCode.Literal(operation.MinValue))
            End If

            If Not operation.MaxValue Is Nothing Then
                builder.AppendLine(",") _
                       .Append("maxValue:= ") _
                       .Append(VBCode.Literal(operation.MaxValue))
            End If

            If operation.IsCyclic Then
                builder.AppendLine(",") _
                       .Append("cyclic:= True")
            End If

            If operation.OldSequence.IncrementBy <> 1 Then
                builder.AppendLine(",") _
                       .Append("oldIncrementBy:= ") _
                       .Append(VBCode.Literal(operation.OldSequence.IncrementBy))
            End If

            If Not operation.OldSequence.MinValue Is Nothing Then
                builder.AppendLine(",") _
                       .Append("oldMinValue:= ") _
                       .Append(VBCode.Literal(operation.OldSequence.MinValue))
            End If

            If Not operation.OldSequence.MaxValue Is Nothing Then
                builder.AppendLine(",") _
                       .Append("oldMaxValue:= ") _
                       .Append(VBCode.Literal(operation.OldSequence.MaxValue))
            End If

            If operation.OldSequence.IsCyclic Then
                builder.AppendLine(",") _
                       .Append("oldCyclic:= True")
            End If

            builder.Append(")")

            Annotations(operation.GetAnnotations(), builder)
            OldAnnotations(operation.OldSequence.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for an <see cref="AlterTableOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As AlterTableOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".AlterTable(")

        Using builder.Indent()
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.Append(")")

            Annotations(operation.GetAnnotations(), builder)
            OldAnnotations(operation.OldTable.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="CreateIndexOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As CreateIndexOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".CreateIndex(")

        Using builder.Indent()

            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.AppendLine(",") _
                   .Append("table:= ") _
                   .Append(VBCode.Literal(operation.Table)) _
                   .AppendLine(",")

            If operation.Columns.Length = 1 Then
                builder.Append("column:= ") _
                       .Append(VBCode.Literal(operation.Columns(0)))
            Else
                builder.Append("columns:= ") _
                       .Append(VBCode.Literal(operation.Columns))
            End If

            If operation.IsUnique Then
                builder.AppendLine(",") _
                       .Append("unique:= True")
            End If

            If Not operation.Filter Is Nothing Then
                builder.AppendLine(",") _
                           .Append("filter:= ") _
                           .Append(VBCode.Literal(operation.Filter))
            End If

            builder.Append(")")
            Annotations(operation.GetAnnotations(), builder)

        End Using
    End Sub

    ''' <summary>
    '''     Generates code for an <see cref="EnsureSchemaOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As EnsureSchemaOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".EnsureSchema(")

        Using builder.Indent()
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name)) _
                   .Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="CreateSequenceOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As CreateSequenceOperation, builder As IndentedStringBuilder)

        builder.Append(".CreateSequence")

        If Not operation.ClrType = GetType(Long) Then
            builder.Append("(Of ") _
                   .Append(VBCode.Reference(operation.ClrType)) _
                   .Append(")")
        End If

        builder.AppendLine("(")

        Using builder.Indent()
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            If operation.StartValue <> 1L Then
                builder.AppendLine(",") _
                       .Append("startValue:= ") _
                       .Append(VBCode.Literal(operation.StartValue))
            End If

            If operation.IncrementBy <> 1 Then
                builder.AppendLine(",") _
                       .Append("incrementBy:= ") _
                       .Append(VBCode.Literal(operation.IncrementBy))
            End If

            If operation.MinValue.HasValue Then
                builder.AppendLine(",") _
                       .Append("minValue:= ") _
                       .Append(VBCode.Literal(operation.MinValue.Value))
            End If

            If operation.MaxValue.HasValue Then
                builder.AppendLine(",") _
                       .Append("maxValue:= ") _
                       .Append(VBCode.Literal(operation.MaxValue.Value))
            End If

            If operation.IsCyclic Then
                builder.AppendLine(",") _
                       .Append("cyclic:= True")
            End If

            builder.Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="CreateTableOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As CreateTableOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".CreateTable(")

        Using builder.Indent()
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.AppendLine(",") _
                   .AppendLine("columns:= Function(table) New With ") _
                   .AppendLine("{")

            Dim map = New Dictionary(Of String, String)
            Using builder.Indent()
                Dim scope = New List(Of String)
                For i = 0 To operation.Columns.Count - 1
                    Dim column = operation.Columns(i)
                    Dim propertyName = VBCode.Identifier(column.Name, scope)
                    map.Add(column.Name, propertyName)

                    builder.Append(".") _
                           .Append(propertyName) _
                           .Append(" = table.Column(Of ") _
                           .Append(VBCode.Reference(column.ClrType)) _
                           .Append(")(")

                    If propertyName <> column.Name Then
                        builder.Append("name:= ") _
                               .Append(VBCode.Literal(column.Name)) _
                               .Append(", ")
                    End If

                    If Not column.ColumnType Is Nothing Then
                        builder.Append("type:= ") _
                               .Append(VBCode.Literal(column.ColumnType)) _
                               .Append(", ")
                    End If

                    If column.IsUnicode = False Then
                        builder.Append("unicode:= False, ")
                    End If

                    If column.IsFixedLength = True Then
                        builder.Append("fixedLength:= True, ")
                    End If

                    If (column.MaxLength.HasValue) Then
                        builder.Append("maxLength:= ") _
                               .Append(VBCode.Literal(column.MaxLength.Value)) _
                               .Append(", ")
                    End If

                    If column.IsRowVersion Then
                        builder.Append("rowVersion:= True, ")
                    End If

                    builder.Append("nullable:= ") _
                           .Append(VBCode.Literal(column.IsNullable))

                    If Not column.DefaultValueSql Is Nothing Then
                        builder.Append(", defaultValueSql:= ") _
                               .Append(VBCode.Literal(column.DefaultValueSql))
                    ElseIf Not column.ComputedColumnSql Is Nothing Then
                        builder.Append(", computedColumnSql:= ") _
                               .Append(VBCode.Literal(column.ComputedColumnSql))
                    ElseIf Not column.DefaultValue Is Nothing Then
                        builder.Append(", defaultValue:= ") _
                               .Append(VBCode.UnknownLiteral(column.DefaultValue))
                    End If

                    builder.Append(")")

                    Using builder.Indent()
                        Annotations(column.GetAnnotations(), builder)
                    End Using

                    If i <> operation.Columns.Count - 1 Then
                        builder.Append(",")
                    End If

                    builder.AppendLine()
                Next
            End Using

            builder.AppendLine("},") _
            .AppendLine("constraints:= Sub(table)")

            Using builder.Indent()
                If Not operation.PrimaryKey Is Nothing Then
                    builder.Append("table.PrimaryKey(") _
                            .Append(VBCode.Literal(operation.PrimaryKey.Name)) _
                            .Append(", ") _
                            .Append(VBCode.Lambda(operation.PrimaryKey.Columns.Select(Function(c) map(c)).ToList())) _
                            .AppendLine(")")

                    Using (builder.Indent())
                        Annotations(operation.PrimaryKey.GetAnnotations(), builder)
                    End Using

                End If

                For Each uniqueConstraint In operation.UniqueConstraints
                    builder.Append("table.UniqueConstraint(") _
                            .Append(VBCode.Literal(uniqueConstraint.Name)) _
                            .Append(", ") _
                            .Append(VBCode.Lambda(uniqueConstraint.Columns.Select(Function(c) map(c)).ToList())) _
                            .Append(")")

                    Using (builder.Indent())
                        Annotations(uniqueConstraint.GetAnnotations(), builder)
                        builder.AppendLine()
                    End Using

                Next

                For Each foreignKey In operation.ForeignKeys
                    builder.AppendLine("table.ForeignKey(")

                    Using builder.Indent()
                        builder.Append("name:= ") _
                                .Append(VBCode.Literal(foreignKey.Name)) _
                                .AppendLine(",") _
                                .Append(If(foreignKey.Columns.Length = 1, "column:= ", "columns:= ")) _
                                .Append(VBCode.Lambda(foreignKey.Columns.Select(Function(c) map(c)).ToList()))

                        If Not foreignKey.PrincipalSchema Is Nothing Then
                            builder.AppendLine(",") _
                                   .Append("principalSchema:= ") _
                                   .Append(VBCode.Literal(foreignKey.PrincipalSchema))
                        End If

                        builder.AppendLine(",") _
                               .Append("principalTable:= ") _
                               .Append(VBCode.Literal(foreignKey.PrincipalTable)) _
                               .AppendLine(",")

                        If foreignKey.PrincipalColumns.Length = 1 Then
                            builder.Append("principalColumn:= ") _
                                   .Append(VBCode.Literal(foreignKey.PrincipalColumns(0)))
                        Else
                            builder.Append("principalColumns:= ") _
                                   .Append(VBCode.Literal(foreignKey.PrincipalColumns))
                        End If

                        If foreignKey.OnUpdate <> ReferentialAction.NoAction Then
                            builder.AppendLine(",") _
                                       .Append("onUpdate:= ") _
                                       .Append(VBCode.Literal(CType(foreignKey.OnUpdate, [Enum])))
                        End If

                        If foreignKey.OnDelete <> ReferentialAction.NoAction Then
                            builder.AppendLine(",") _
                                   .Append("onDelete:= ") _
                                   .Append(VBCode.Literal(CType(foreignKey.OnDelete, [Enum])))
                        End If

                        builder.AppendLine(")")

                        Annotations(foreignKey.GetAnnotations(), builder)
                    End Using

                Next
            End Using

            builder.Append("End Sub)")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="DropColumnOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As DropColumnOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".DropColumn(")

        Using builder.Indent()
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.AppendLine(",") _
                   .Append("table:= ") _
                   .Append(VBCode.Literal(operation.Table)) _
                   .Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="DropForeignKeyOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As DropForeignKeyOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".DropForeignKey(")

        Using (builder.Indent())
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.AppendLine(",") _
                   .Append("table:= ") _
                   .Append(VBCode.Literal(operation.Table)) _
                   .Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="DropIndexOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As DropIndexOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".DropIndex(")

        Using builder.Indent()
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.AppendLine(",") _
                   .Append("table:= ") _
                   .Append(VBCode.Literal(operation.Table)) _
                   .Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="DropPrimaryKeyOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As DropPrimaryKeyOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".DropPrimaryKey(")

        Using builder.Indent()
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.AppendLine(",") _
                   .Append("table:= ") _
                   .Append(VBCode.Literal(operation.Table)) _
                   .Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="DropSchemaOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As DropSchemaOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".DropSchema(")

        Using builder.Indent()
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name)) _
                   .Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="DropSequenceOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As DropSequenceOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".DropSequence(")

        Using builder.Indent()
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="DropTableOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As DropTableOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".DropTable(")

        Using builder.Indent()
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="DropUniqueConstraintOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As DropUniqueConstraintOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".DropUniqueConstraint(")

        Using builder.Indent()
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.AppendLine(",") _
                   .Append("table:= ") _
                   .Append(VBCode.Literal(operation.Table)) _
                   .Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="RenameColumnOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As RenameColumnOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".RenameColumn(")

        Using (builder.Indent())
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.AppendLine(",") _
                   .Append("table:= ") _
                   .Append(VBCode.Literal(operation.Table)) _
                   .AppendLine(",") _
                   .Append("newName:= ") _
                   .Append(VBCode.Literal(operation.NewName)) _
                   .Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="RenameIndexOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As RenameIndexOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".RenameIndex(")

        Using (builder.Indent())
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.AppendLine(",") _
                   .Append("table:= ") _
                   .Append(VBCode.Literal(operation.Table)) _
                   .AppendLine(",") _
                   .Append("newName:= ") _
                   .Append(VBCode.Literal(operation.NewName)) _
                   .Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="RenameSequenceOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As RenameSequenceOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".RenameSequence(")

        Using builder.Indent()
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            If Not operation.NewName Is Nothing Then
                builder.AppendLine(",") _
                       .Append("newName:= ") _
                       .Append(VBCode.Literal(operation.NewName))
            End If

            If Not operation.NewSchema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("newSchema:= ") _
                       .Append(VBCode.Literal(operation.NewSchema))
            End If

            builder.Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="RenameTableOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As RenameTableOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".RenameTable(")

        Using builder.Indent()
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            If Not operation.NewName Is Nothing Then
                builder.AppendLine(",") _
                       .Append("newName:= ") _
                       .Append(VBCode.Literal(operation.NewName))
            End If

            If Not operation.NewSchema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("newSchema:= ") _
                       .Append(VBCode.Literal(operation.NewSchema))
            End If

            builder.Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="RestartSequenceOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As RestartSequenceOperation, builder As IndentedStringBuilder)

        builder.AppendLine(".RestartSequence(")

        Using builder.Indent()
            builder.Append("name:= ") _
                   .Append(VBCode.Literal(operation.Name))

            If Not operation.Schema Is Nothing Then
                builder.AppendLine(",") _
                       .Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema))
            End If

            builder.AppendLine(",") _
                   .Append("startValue:= ") _
                   .Append(VBCode.Literal(operation.StartValue)) _
                   .Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="SqlOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(operation As SqlOperation, builder As IndentedStringBuilder)

        builder.Append(".Sql(") _
               .Append(VBCode.Literal(operation.Sql)) _
               .Append(")")

        Using (builder.Indent())
            Annotations(operation.GetAnnotations(), builder)
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for an <see cref="InsertDataOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(
              operation As InsertDataOperation,
              builder As IndentedStringBuilder)

        builder.AppendLine(".InsertData(")

        Using builder.Indent()
            If Not operation.Schema Is Nothing Then
                builder.Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema)) _
                       .AppendLine(",")
            End If

            builder.Append("table:= ") _
                   .Append(VBCode.Literal(operation.Table)) _
                   .AppendLine(",")

            If operation.Columns.Length = 1 Then
                builder.Append("column:= ") _
                       .Append(VBCode.Literal(operation.Columns(0)))
            Else
                builder.Append("columns:= ") _
                       .Append(VBCode.Literal(operation.Columns))
            End If

            builder.AppendLine(",")

            If operation.Values.GetLength(0) = 1 AndAlso operation.Values.GetLength(1) = 1 Then
                builder.Append("value:= ") _
                       .Append(VBCode.UnknownLiteral(operation.Values(0, 0)))
            ElseIf operation.Values.GetLength(0) = 1 Then
                builder.Append("values:= ") _
                       .Append(VBCode.Literal(ToOnedimensionalArray(operation.Values)))
            ElseIf operation.Values.GetLength(1) = 1 Then
                builder.Append("values:= ") _
                       .AppendLines(
                        VBCode.Literal(
                            ToOnedimensionalArray(operation.Values, firstDimension:=True),
                            vertical:=True),
                        skipFinalNewline:=True)
            Else
                builder.Append("values:= ") _
                       .AppendLines(VBCode.Literal(operation.Values), skipFinalNewline:=True)
            End If

            builder.Append(")")
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for a <see cref="DeleteDataOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code Is added to. </param>
    Protected Overridable Overloads Sub Generate(
             operation As DeleteDataOperation,
             builder As IndentedStringBuilder)

        builder.AppendLine(".DeleteData(")

        Using builder.Indent()
            If Not operation.Schema Is Nothing Then
                builder.Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema)) _
                       .AppendLine(",")
            End If

            builder.Append("table:= ") _
                   .Append(VBCode.Literal(operation.Table)) _
                   .AppendLine(",")

            If (operation.KeyColumns.Length = 1) Then
                builder.Append("keyColumn:= ") _
                       .Append(VBCode.Literal(operation.KeyColumns(0)))
            Else
                builder.Append("keyColumns:= ") _
                       .Append(VBCode.Literal(operation.KeyColumns))
            End If

            builder.AppendLine(",")

            If operation.KeyValues.GetLength(0) = 1 AndAlso operation.KeyValues.GetLength(1) = 1 Then
                builder.Append("keyValue:= ") _
                       .Append(VBCode.UnknownLiteral(operation.KeyValues(0, 0)))
            ElseIf operation.KeyValues.GetLength(0) = 1 Then
                builder.Append("keyValues:= ") _
                       .Append(VBCode.Literal(ToOnedimensionalArray(operation.KeyValues)))
            ElseIf (operation.KeyValues.GetLength(1) = 1) Then
                builder.Append("keyValues:= ") _
                       .AppendLines(
                            VBCode.Literal(
                                ToOnedimensionalArray(operation.KeyValues, firstDimension:=True),
                                vertical:=True),
                            skipFinalNewline:=True)
            Else
                builder.Append("keyValues:= ") _
                       .AppendLines(VBCode.Literal(operation.KeyValues), skipFinalNewline:=True)
            End If

            builder.Append(")")
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for an <see cref="UpdateDataOperation" />.
    ''' </summary>
    ''' <param name="operation"> The operation. </param>
    ''' <param name="builder"> The builder code is added to. </param>
    Protected Overridable Overloads Sub Generate(
              operation As UpdateDataOperation,
              builder As IndentedStringBuilder)

        builder.AppendLine(".UpdateData(")

        Using builder.Indent()
            If Not operation.Schema Is Nothing Then
                builder.Append("schema:= ") _
                       .Append(VBCode.Literal(operation.Schema)) _
                       .AppendLine(",")
            End If

            builder.Append("table:= ") _
                   .Append(VBCode.Literal(operation.Table)) _
                   .AppendLine(",")

            If operation.KeyColumns.Length = 1 Then
                builder.Append("keyColumn:= ") _
                       .Append(VBCode.Literal(operation.KeyColumns(0)))
            Else
                builder.Append("keyColumns:= ") _
                       .Append(VBCode.Literal(operation.KeyColumns))
            End If

            builder.AppendLine(",")

            If operation.KeyValues.GetLength(0) = 1 AndAlso operation.KeyValues.GetLength(1) = 1 Then
                builder.Append("keyValue:= ") _
                       .Append(VBCode.UnknownLiteral(operation.KeyValues(0, 0)))
            ElseIf operation.KeyValues.GetLength(0) = 1 Then
                builder.Append("keyValues:= ") _
                       .Append(VBCode.Literal(ToOnedimensionalArray(operation.KeyValues)))
            ElseIf operation.KeyValues.GetLength(1) = 1 Then
                builder.Append("keyValues:= ") _
                       .AppendLines(
                            VBCode.Literal(
                                ToOnedimensionalArray(operation.KeyValues, firstDimension:=True),
                                                    vertical:=True),
                                                skipFinalNewline:=True)
            Else
                builder.Append("keyValues:= ") _
                       .AppendLines(VBCode.Literal(operation.KeyValues), skipFinalNewline:=True)
            End If

            builder.AppendLine(",")

            If operation.Columns.Length = 1 Then
                builder.Append("column:= ") _
                       .Append(VBCode.Literal(operation.Columns(0)))
            Else
                builder.Append("columns:= ") _
                       .Append(VBCode.Literal(operation.Columns))
            End If

            builder.AppendLine(",")

            If operation.Values.GetLength(0) = 1 AndAlso operation.Values.GetLength(1) = 1 Then
                builder.Append("value:= ") _
                       .Append(VBCode.UnknownLiteral(operation.Values(0, 0)))
            ElseIf operation.Values.GetLength(0) = 1 Then
                builder.Append("values:= ") _
                       .Append(VBCode.Literal(ToOnedimensionalArray(operation.Values)))
            ElseIf operation.Values.GetLength(1) = 1 Then
                builder.Append("values:= ") _
                       .AppendLines(
                            VBCode.Literal(
                                ToOnedimensionalArray(operation.Values, firstDimension:=True),
                                                    vertical:=True),
                                                skipFinalNewline:=True)
            Else
                builder.Append("values:= ") _
                       .AppendLines(VBCode.Literal(operation.Values), skipFinalNewline:=True)
            End If

            builder.Append(")")
        End Using
    End Sub

    ''' <summary>
    '''     Generates code for <see cref="Annotation" /> objects.
    ''' </summary>
    ''' <param name="annotations"> The annotations. </param>
    ''' <param name="builder"> The builder code is added to. </param>
    Protected Overridable Overloads Sub Annotations(
         annotations As IEnumerable(Of Annotation),
         builder As IndentedStringBuilder)

        For Each annotation In annotations

            ' TODO: Give providers an opportunity To render these As provider-specific extension methods
            builder.AppendLine(" _") _
                   .Append(".Annotation(") _
                   .Append(VBCode.Literal(annotation.Name)) _
                   .Append(", ") _
                   .Append(VBCode.UnknownLiteral(annotation.Value)) _
                   .Append(")")
        Next
    End Sub

    ''' <summary>
    '''     Generates code for removed <see cref="Annotation" /> objects.
    ''' </summary>
    ''' <param name="annotations"> The annotations. </param>
    ''' <param name="builder"> The builder code is added to. </param>
    Protected Overridable Overloads Sub OldAnnotations(
         annotations As IEnumerable(Of Annotation),
         builder As IndentedStringBuilder)

        For Each annotation In annotations
            ' TODO: Give providers an opportunity To render these As provider-specific extension methods
            builder.AppendLine(" _") _
                       .Append(".OldAnnotation(") _
                       .Append(VBCode.Literal(annotation.Name)) _
                       .Append(", ") _
                       .Append(VBCode.UnknownLiteral(annotation.Value)) _
                       .Append(")")
        Next
    End Sub

    Private Shared Function ToOnedimensionalArray(values As Object(,), Optional firstDimension As Boolean = False) As Object()
        Debug.Assert(
                values.GetLength(If(firstDimension, 1, 0)) = 1,
                String.Format("Length of dimension {0} is not 1.", If(firstDimension, 1, 0)))

        Dim result = New Object(values.Length - 1) {}
        For i = 0 To values.Length - 1
            result(i) = If(firstDimension, values(i, 0), values(0, i))
        Next

        Return result
    End Function
End Class