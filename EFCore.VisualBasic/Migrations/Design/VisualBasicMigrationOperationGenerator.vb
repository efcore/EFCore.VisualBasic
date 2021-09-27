Imports System.Reflection
Imports EntityFrameworkCore.VisualBasic.Design
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Migrations
Imports Microsoft.EntityFrameworkCore.Migrations.Operations

Namespace Migrations.Design

    ''' <summary>
    '''     Used to generate Visual Basic code for creating <see cref="MigrationOperation" /> objects.
    ''' </summary>
    ''' <remarks>
    '''     See <see href="https://aka.ms/efcore-docs-migrations">Database migrations</see>, And
    '''     <see href="https://aka.ms/efcore-docs-design-time-services">EF Core design-time services</see> for more information.
    ''' </remarks>
    Public Class VisualBasicMigrationOperationGenerator

        ''' <summary>
        '''     Initializes a New instance of the <see cref="VisualBasicMigrationOperationGenerator" /> class.
        ''' </summary>
        ''' <param name="vbHelper">The Visual Basic helper.</param>
        Public Sub New(vbHelper As IVisualBasicHelper)
            VBCode = NotNull(vbHelper, NameOf(vbHelper))
        End Sub

        ''' <summary>
        '''     The VB helper.
        ''' </summary>
        Protected Overridable ReadOnly Property VBCode As IVisualBasicHelper

        ''' <summary>
        '''     Generates code for creating <see cref="MigrationOperation" /> objects.
        ''' </summary>
        ''' <param name="builderName">The <see cref="MigrationOperation" /> variable name.</param>
        ''' <param name="operations">The operations.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Public Overloads Sub Generate(builderName As String,
                                      operations As IReadOnlyList(Of MigrationOperation),
                                      builder As IndentedStringBuilder)

            NotEmpty(builderName, NameOf(builderName))
            NotNull(operations, NameOf(operations))
            NotNull(builder, NameOf(builder))

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

            Dim method = GetType(VisualBasicMigrationOperationGenerator).
                            GetMethod(NameOf(Generate), BindingFlags.Instance Or BindingFlags.NonPublic, Nothing, New Type() {operation.GetType(), GetType(IndentedStringBuilder)}, Nothing)

            If method Is Nothing Then
                Generate(operation, builder)
            Else
                method.Invoke(Me, {operation, builder})
            End If

        End Sub

        ''' <summary>
        '''     Generates code for an unknown <see cref="MigrationOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As MigrationOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            Throw New InvalidOperationException(VBDesignStrings.UnknownOperation(operation.GetType()))
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="AddColumnOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As AddColumnOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.
                Append(".AddColumn(Of ").
                Append(VBCode.Reference(operation.ClrType)).
                AppendLine(")(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.
                    AppendLine(",").
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table))

                If operation.ColumnType IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("type:=").
                        Append(VBCode.Literal(operation.ColumnType))
                End If

                If operation.IsUnicode = False Then
                    builder.
                        AppendLine(",").
                        Append("unicode:=False")
                End If

                If operation.IsFixedLength = True Then
                    builder.
                        AppendLine(",").
                        Append("fixedLength:=True")
                End If

                If operation.MaxLength.HasValue Then
                    builder.
                        AppendLine(",").
                        Append("maxLength:=").
                        Append(VBCode.Literal(operation.MaxLength.Value))
                End If

                If operation.Precision.HasValue Then
                    builder.
                        AppendLine(",").
                        Append("precision:=").
                        Append(VBCode.Literal(operation.Precision.Value))
                End If

                If operation.Scale.HasValue Then
                    builder.
                        AppendLine(",").
                        Append("scale:=").
                        Append(VBCode.Literal(operation.Scale.Value))
                End If

                If operation.IsRowVersion Then
                    builder.
                        AppendLine(",").
                        Append("rowVersion:=True")
                End If

                builder.
                    AppendLine(",").
                    Append("nullable:=").
                    Append(VBCode.Literal(operation.IsNullable))

                If operation.DefaultValueSql IsNot Nothing Then

                    builder.
                        AppendLine(",").
                        Append("defaultValueSql:=").
                        Append(VBCode.Literal(operation.DefaultValueSql))

                ElseIf operation.ComputedColumnSql IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("computedColumnSql:=").
                        Append(VBCode.UnknownLiteral(operation.ComputedColumnSql))

                    If operation.IsStored IsNot Nothing Then
                        builder.
                            AppendLine(",").
                            Append("stored:=").
                            Append(VBCode.Literal(operation.IsStored))
                    End If

                ElseIf operation.DefaultValue IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("defaultValue:=").
                        Append(VBCode.UnknownLiteral(operation.DefaultValue))
                End If

                If operation.Comment IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("comment:=").
                        Append(VBCode.Literal(operation.Comment))
                End If

                If operation.Collation IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("collation:=").
                        Append(VBCode.Literal(operation.Collation))
                End If

                builder.Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using

        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="AddForeignKeyOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As AddForeignKeyOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".AddForeignKey(")

            Using builder.Indent()

                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.
                    AppendLine(",").
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table)).
                    AppendLine(",")

                If operation.Columns.Length = 1 Then
                    builder.
                        Append("column:=").
                        Append(VBCode.Literal(operation.Columns(0)))
                Else
                    builder.
                        Append("columns:=").
                        Append(VBCode.Literal(operation.Columns))
                End If

                If operation.PrincipalSchema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("principalSchema:=").
                        Append(VBCode.Literal(operation.PrincipalSchema))
                End If

                builder.
                    AppendLine(",").
                    Append("principalTable:=").
                    Append(VBCode.Literal(operation.PrincipalTable))

                If operation.PrincipalColumns IsNot Nothing Then
                    If operation.PrincipalColumns.Length = 1 Then
                        builder.
                            AppendLine(",").
                            Append("principalColumn:=").
                            Append(VBCode.Literal(operation.PrincipalColumns(0)))
                    Else
                        builder.
                            AppendLine(",").
                            Append("principalColumns:=").
                            Append(VBCode.Literal(operation.PrincipalColumns))
                    End If
                End If

                If operation.OnUpdate <> ReferentialAction.NoAction Then
                    builder.
                        AppendLine(",").
                        Append("onUpdate:=").
                        Append(VBCode.Literal(CType(operation.OnUpdate, [Enum])))
                End If

                If operation.OnDelete <> ReferentialAction.NoAction Then
                    builder.
                        AppendLine(",").
                        Append("onDelete:=").
                        Append(VBCode.Literal(CType(operation.OnDelete, [Enum])))
                End If

                builder.Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="AddPrimaryKeyOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As AddPrimaryKeyOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".AddPrimaryKey(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.
                    AppendLine(",").
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table)).
                    AppendLine(",")

                If operation.Columns.Length = 1 Then
                    builder.
                        Append("column:=").
                        Append(VBCode.Literal(operation.Columns(0)))
                Else

                    builder.
                        Append("columns:=").
                        Append(VBCode.Literal(operation.Columns))
                End If

                builder.Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="AddUniqueConstraintOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As AddUniqueConstraintOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".AddUniqueConstraint(")

            Using builder.Indent()

                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.
                    AppendLine(",").
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table)).
                    AppendLine(",")

                If operation.Columns.Length = 1 Then
                    builder.
                        Append("column:=").
                        Append(VBCode.Literal(operation.Columns(0)))
                Else
                    builder.
                        Append("columns:=").
                        Append(VBCode.Literal(operation.Columns))
                End If

                builder.Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="AddCheckConstraintOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As AddCheckConstraintOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".AddCheckConstraint(")

            Using builder.Indent()

                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.
                    AppendLine(",").
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table)).
                    AppendLine(",").
                    Append("sql:=").
                    Append(VBCode.Literal(operation.Sql)).
                    Append(")")
            End Using

            Annotations(operation.GetAnnotations(), builder)
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="AlterColumnOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As AlterColumnOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.
                Append(".AlterColumn(Of ").
                Append(VBCode.Reference(operation.ClrType)).
                AppendLine(")(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.
                    AppendLine(",").
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table))

                If operation.ColumnType IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("type:=").
                        Append(VBCode.Literal(operation.ColumnType))
                End If

                If operation.IsUnicode = False Then
                    builder.
                        AppendLine(",").
                        Append("unicode:=False")
                End If

                If operation.IsFixedLength = True Then
                    builder.
                        AppendLine(",").
                        Append("fixedLength:=True")
                End If

                If operation.MaxLength.HasValue Then
                    builder.
                        AppendLine(",").
                        Append("maxLength:=").
                        Append(VBCode.Literal(operation.MaxLength.Value))
                End If

                If operation.Precision.HasValue Then
                    builder.
                        AppendLine(",").
                        Append("precision:=").
                        Append(VBCode.Literal(operation.Precision.Value))
                End If

                If operation.Scale.HasValue Then
                    builder.
                        AppendLine(",").
                        Append("scale:=").
                        Append(VBCode.Literal(operation.Scale.Value))
                End If

                If operation.IsRowVersion Then
                    builder.
                        AppendLine(",").
                        Append("rowVersion:=True")
                End If

                builder.
                    AppendLine(",").
                    Append("nullable:=").
                    Append(VBCode.Literal(operation.IsNullable))

                If operation.DefaultValueSql IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("defaultValueSql:=").
                        Append(VBCode.Literal(operation.DefaultValueSql))

                ElseIf operation.ComputedColumnSql IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("computedColumnSql:=").
                        Append(VBCode.UnknownLiteral(operation.ComputedColumnSql))

                    If operation.IsStored IsNot Nothing Then
                        builder.
                            AppendLine(",").
                            Append("stored:=").
                            Append(VBCode.Literal(operation.IsStored))
                    End If

                ElseIf operation.DefaultValue IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("defaultValue:=").
                        Append(VBCode.UnknownLiteral(operation.DefaultValue))
                End If

                If operation.Comment IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("comment:=").
                        Append(VBCode.Literal(operation.Comment))
                End If

                If operation.Collation IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("collation:=").
                        Append(VBCode.Literal(operation.Collation))
                End If

                If operation.OldColumn.ClrType IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("oldClrType:=GetType(").
                        Append(VBCode.Reference(operation.OldColumn.ClrType)).
                        Append(")")
                End If

                If operation.OldColumn.ColumnType IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("oldType:=").
                        Append(VBCode.Literal(operation.OldColumn.ColumnType))
                End If

                If operation.OldColumn.IsUnicode = False Then
                    builder.
                        AppendLine(",").
                        Append("oldUnicode:=False")
                End If

                If operation.OldColumn.IsFixedLength = True Then
                    builder.
                        AppendLine(",").
                        Append("oldFixedLength:=True")
                End If

                If operation.OldColumn.MaxLength.HasValue Then
                    builder.
                        AppendLine(",").
                        Append("oldMaxLength:=").
                        Append(VBCode.Literal(operation.OldColumn.MaxLength.Value))
                End If

                If operation.OldColumn.Precision.HasValue Then
                    builder.
                        AppendLine(",").
                        Append("oldPrecision:=").
                        Append(VBCode.Literal(operation.OldColumn.Precision.Value))
                End If

                If operation.OldColumn.Scale.HasValue Then
                    builder.
                        AppendLine(",").
                        Append("oldScale:=").
                        Append(VBCode.Literal(operation.OldColumn.Scale.Value))
                End If

                If operation.OldColumn.IsRowVersion Then
                    builder.
                        AppendLine(",").
                        Append("oldRowVersion:=True")
                End If

                If operation.OldColumn.IsNullable Then
                    builder.
                        AppendLine(",").
                        Append("oldNullable:=True")
                End If

                If operation.OldColumn.DefaultValueSql IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("oldDefaultValueSql:=").
                        Append(VBCode.Literal(operation.OldColumn.DefaultValueSql))
                ElseIf operation.OldColumn.ComputedColumnSql IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("oldComputedColumnSql:=").
                        Append(VBCode.UnknownLiteral(operation.OldColumn.ComputedColumnSql))

                    If operation.IsStored IsNot Nothing Then
                        builder.
                            AppendLine(",").
                            Append("oldStored:=").
                            Append(VBCode.Literal(operation.OldColumn.IsStored))
                    End If

                ElseIf operation.OldColumn.DefaultValue IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("oldDefaultValue:=").
                        Append(VBCode.UnknownLiteral(operation.OldColumn.DefaultValue))
                End If

                If operation.OldColumn.Comment IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("oldComment:=").
                        Append(VBCode.Literal(operation.OldColumn.Comment))
                End If

                If operation.OldColumn.Collation IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("oldCollation:=").
                        Append(VBCode.Literal(operation.OldColumn.Collation))
                End If

                builder.Append(")")

                Annotations(operation.GetAnnotations(), builder)
                OldAnnotations(operation.OldColumn.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="AlterDatabaseOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As AlterDatabaseOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.Append(".AlterDatabase(")

            Using builder.Indent()

                Dim needComma = False

                If operation.Collation IsNot Nothing Then
                    builder.
                        AppendLine().
                        Append("collation:=").
                        Append(VBCode.Literal(operation.Collation))
                    needComma = True
                End If

                If operation.OldDatabase.Collation IsNot Nothing Then
                    If needComma Then
                        builder.Append(",")
                    End If

                    builder.
                        AppendLine().
                        Append("oldCollation:=").
                        Append(VBCode.Literal(operation.OldDatabase.Collation))
                    needComma = True
                End If

                builder.Append(")")

                Annotations(operation.GetAnnotations(), builder)
                OldAnnotations(operation.OldDatabase.GetAnnotations(), builder)

            End Using
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="AlterSequenceOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As AlterSequenceOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".AlterSequence(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                If operation.IncrementBy <> 1 Then
                    builder.
                        AppendLine(",").
                        Append("incrementBy:=").
                        Append(VBCode.Literal(operation.IncrementBy))
                End If

                If operation.MinValue IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("minValue:=").
                        Append(VBCode.Literal(operation.MinValue))
                End If

                If operation.MaxValue IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("maxValue:=").
                        Append(VBCode.Literal(operation.MaxValue))
                End If

                If operation.IsCyclic Then
                    builder.
                        AppendLine(",").
                        Append("cyclic:=True")
                End If

                If operation.OldSequence.IncrementBy <> 1 Then
                    builder.
                        AppendLine(",").
                        Append("oldIncrementBy:=").
                        Append(VBCode.Literal(operation.OldSequence.IncrementBy))
                End If

                If operation.OldSequence.MinValue IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("oldMinValue:=").
                        Append(VBCode.Literal(operation.OldSequence.MinValue))
                End If

                If operation.OldSequence.MaxValue IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("oldMaxValue:=").
                        Append(VBCode.Literal(operation.OldSequence.MaxValue))
                End If

                If operation.OldSequence.IsCyclic Then
                    builder.
                        AppendLine(",").
                        Append("oldCyclic:=True")
                End If

                builder.Append(")")

                Annotations(operation.GetAnnotations(), builder)
                OldAnnotations(operation.OldSequence.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="AlterTableOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As AlterTableOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".AlterTable(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                If operation.Comment IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("comment:=").
                        Append(VBCode.Literal(operation.Comment))
                End If

                If operation.OldTable.Comment IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("oldComment:=").
                        Append(VBCode.Literal(operation.OldTable.Comment))
                End If

                builder.Append(")")

                Annotations(operation.GetAnnotations(), builder)
                OldAnnotations(operation.OldTable.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="CreateIndexOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As CreateIndexOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".CreateIndex(")

            Using builder.Indent()

                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.
                    AppendLine(",").
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table)).
                    AppendLine(",")

                If operation.Columns.Length = 1 Then
                    builder.
                        Append("column:=").
                        Append(VBCode.Literal(operation.Columns(0)))
                Else
                    builder.
                        Append("columns:=").
                        Append(VBCode.Literal(operation.Columns))
                End If

                If operation.IsUnique Then
                    builder.
                        AppendLine(",").
                        Append("unique:=True")
                End If

                If operation.Filter IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("filter:=").
                        Append(VBCode.Literal(operation.Filter))
                End If

                builder.Append(")")
                Annotations(operation.GetAnnotations(), builder)

            End Using
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="EnsureSchemaOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As EnsureSchemaOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".EnsureSchema(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name)).
                    Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="CreateSequenceOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As CreateSequenceOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.Append(".CreateSequence")

            If Not operation.ClrType = GetType(Long) Then
                builder.
                    Append("(Of ").
                    Append(VBCode.Reference(operation.ClrType)).
                    Append(")")
            End If

            builder.AppendLine("(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                If operation.StartValue <> 1L Then
                    builder.
                        AppendLine(",").
                        Append("startValue:=").
                        Append(VBCode.Literal(operation.StartValue))
                End If

                If operation.IncrementBy <> 1 Then
                    builder.
                        AppendLine(",").
                        Append("incrementBy:=").
                        Append(VBCode.Literal(operation.IncrementBy))
                End If

                If operation.MinValue.HasValue Then
                    builder.
                        AppendLine(",").
                        Append("minValue:=").
                        Append(VBCode.Literal(operation.MinValue.Value))
                End If

                If operation.MaxValue.HasValue Then
                    builder.
                        AppendLine(",").
                        Append("maxValue:=").
                        Append(VBCode.Literal(operation.MaxValue.Value))
                End If

                If operation.IsCyclic Then
                    builder.
                        AppendLine(",").
                        Append("cyclic:=True")
                End If

                builder.Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="CreateTableOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As CreateTableOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".CreateTable(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.
                    AppendLine(",").
                    AppendLine("columns:=Function(table) New With {")

                Dim map = New Dictionary(Of String, String)
                Using builder.Indent()
                    Dim scope = New List(Of String)
                    For i = 0 To operation.Columns.Count - 1
                        Dim column = operation.Columns(i)
                        Dim propertyName = VBCode.Identifier(column.Name, scope)
                        map.Add(column.Name, propertyName)

                        builder.
                            Append(".").
                            Append(propertyName).
                            Append(" = table.Column(Of ").
                            Append(VBCode.Reference(column.ClrType)).
                            Append(")(")

                        If propertyName <> column.Name Then
                            builder.
                                Append("name:=").
                                Append(VBCode.Literal(column.Name)).
                                Append(", ")
                        End If

                        If column.ColumnType IsNot Nothing Then
                            builder.
                                Append("type:=").
                                Append(VBCode.Literal(column.ColumnType)).
                                Append(", ")
                        End If

                        If column.IsUnicode = False Then
                            builder.Append("unicode:=False, ")
                        End If

                        If column.IsFixedLength = True Then
                            builder.Append("fixedLength:=True, ")
                        End If

                        If column.MaxLength.HasValue Then
                            builder.
                                Append("maxLength:=").
                                Append(VBCode.Literal(column.MaxLength.Value)).
                                Append(", ")
                        End If

                        If column.Precision.HasValue Then
                            builder.
                                Append("precision:=").
                                Append(VBCode.Literal(column.Precision.Value)).
                                Append(", ")
                        End If

                        If column.Scale.HasValue Then
                            builder.
                                Append("scale:=").
                                Append(VBCode.Literal(column.Scale.Value)).
                                Append(", ")
                        End If

                        If column.IsRowVersion Then
                            builder.Append("rowVersion:=True, ")
                        End If

                        builder.
                            Append("nullable:=").
                            Append(VBCode.Literal(column.IsNullable))

                        If column.DefaultValueSql IsNot Nothing Then
                            builder.
                                Append(", defaultValueSql:=").
                                Append(VBCode.Literal(column.DefaultValueSql))
                        ElseIf column.ComputedColumnSql IsNot Nothing Then
                            builder.
                                Append(", computedColumnSql:=").
                                Append(VBCode.Literal(column.ComputedColumnSql))

                            If column.IsStored IsNot Nothing Then
                                builder.
                                    Append(", stored:=").
                                    Append(VBCode.Literal(column.IsStored))
                            End If

                        ElseIf column.DefaultValue IsNot Nothing Then
                            builder.
                                Append(", defaultValue:=").
                                Append(VBCode.UnknownLiteral(column.DefaultValue))
                        End If

                        If column.Comment IsNot Nothing Then
                            builder.
                                Append(", comment:=").
                                Append(VBCode.Literal(column.Comment))
                        End If

                        If column.Collation IsNot Nothing Then
                            builder.
                                Append(", collation:=").
                                Append(VBCode.Literal(column.Collation))
                        End If

                        builder.Append(")")

                        Annotations(column.GetAnnotations(), builder)

                        If i <> operation.Columns.Count - 1 Then
                            builder.Append(",")
                        End If

                        builder.AppendLine()
                    Next
                End Using

                builder.
                AppendLine("},").
                AppendLine("constraints:=Sub(table)")

                Using builder.Indent()
                    If operation.PrimaryKey IsNot Nothing Then
                        builder.
                            Append("table.PrimaryKey(").
                            Append(VBCode.Literal(operation.PrimaryKey.Name)).
                            Append(", ").
                            Append(VBCode.Lambda(operation.PrimaryKey.Columns.Select(Function(c) map(c)).ToList())).
                            Append(")")

                        Annotations(operation.PrimaryKey.GetAnnotations(), builder)
                        builder.AppendLine()
                    End If

                    For Each uniqueConstraint In operation.UniqueConstraints
                        builder.
                            Append("table.UniqueConstraint(").
                            Append(VBCode.Literal(uniqueConstraint.Name)).
                            Append(", ").
                            Append(VBCode.Lambda(uniqueConstraint.Columns.Select(Function(c) map(c)).ToList())).
                            Append(")")

                        Annotations(uniqueConstraint.GetAnnotations(), builder)
                        builder.AppendLine()
                    Next

                    For Each checkConstraints In operation.CheckConstraints
                        builder.
                            Append("table.CheckConstraint(").
                            Append(VBCode.Literal(checkConstraints.Name)).
                            Append(", ").
                            Append(VBCode.Literal(checkConstraints.Sql)).
                            Append(")")

                        Annotations(checkConstraints.GetAnnotations(), builder)
                        builder.AppendLine()
                    Next

                    For Each foreignKey In operation.ForeignKeys
                        builder.AppendLine("table.ForeignKey(")

                        Using builder.Indent()
                            builder.
                                Append("name:=").
                                Append(VBCode.Literal(foreignKey.Name)).
                                AppendLine(",").
                                Append(If(foreignKey.Columns.Length = 1 OrElse foreignKey.PrincipalColumns Is Nothing,
                                            "column:=",
                                            "columns:=")).
                                Append(VBCode.Lambda(foreignKey.Columns.Select(Function(c) map(c)).ToList()))

                            If foreignKey.PrincipalSchema IsNot Nothing Then
                                builder.
                                    AppendLine(",").
                                    Append("principalSchema:=").
                                    Append(VBCode.Literal(foreignKey.PrincipalSchema))
                            End If

                            builder.
                                AppendLine(",").
                                Append("principalTable:=").
                                Append(VBCode.Literal(foreignKey.PrincipalTable))

                            If foreignKey.PrincipalColumns IsNot Nothing Then
                                builder.AppendLine(",")

                                If foreignKey.PrincipalColumns.Length = 1 Then
                                    builder.
                                    Append("principalColumn:=").
                                    Append(VBCode.Literal(foreignKey.PrincipalColumns(0)))
                                Else
                                    builder.
                                    Append("principalColumns:=").
                                    Append(VBCode.Literal(foreignKey.PrincipalColumns))
                                End If
                            End If

                            If foreignKey.OnUpdate <> ReferentialAction.NoAction Then
                                builder.
                                    AppendLine(",").
                                    Append("onUpdate:=").
                                    Append(VBCode.Literal(CType(foreignKey.OnUpdate, [Enum])))
                            End If

                            If foreignKey.OnDelete <> ReferentialAction.NoAction Then
                                builder.
                                    AppendLine(",").
                                    Append("onDelete:=").
                                    Append(VBCode.Literal(CType(foreignKey.OnDelete, [Enum])))
                            End If

                            builder.Append(")")

                            Annotations(foreignKey.GetAnnotations(), builder)

                            builder.AppendLine()
                        End Using

                    Next
                End Using

                builder.Append("End Sub")

                If operation.Comment IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("comment:=").
                        Append(VBCode.Literal(operation.Comment))
                End If

                builder.Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="DropColumnOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As DropColumnOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".DropColumn(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.
                    AppendLine(",").
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table)).
                    Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="DropForeignKeyOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As DropForeignKeyOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".DropForeignKey(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.
                    AppendLine(",").
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table)).
                    Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="DropIndexOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As DropIndexOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".DropIndex(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                If operation.Table IsNot Nothing Then
                    builder.
                    AppendLine(",").
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table))
                End If

                builder.Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="DropPrimaryKeyOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As DropPrimaryKeyOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".DropPrimaryKey(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.
                    AppendLine(",").
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table)).
                    Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="DropSchemaOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As DropSchemaOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".DropSchema(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name)).
                    Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="DropSequenceOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As DropSequenceOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".DropSequence(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="DropTableOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As DropTableOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".DropTable(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="DropUniqueConstraintOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As DropUniqueConstraintOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".DropUniqueConstraint(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.
                    AppendLine(",").
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table)).
                    Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="DropCheckConstraintOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As DropCheckConstraintOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".DropCheckConstraint(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.
                    AppendLine(",").
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table)).
                    Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub


        ''' <summary>
        '''     Generates code for a <see cref="RenameColumnOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As RenameColumnOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".RenameColumn(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.
                    AppendLine(",").
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table)).
                    AppendLine(",").
                    Append("newName:=").
                    Append(VBCode.Literal(operation.NewName)).
                    Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="RenameIndexOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As RenameIndexOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".RenameIndex(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                If operation.Table IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("table:=").
                        Append(VBCode.Literal(operation.Table))
                End If

                builder.
                    AppendLine(",").
                    Append("newName:=").
                    Append(VBCode.Literal(operation.NewName)).
                    Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="RenameSequenceOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As RenameSequenceOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".RenameSequence(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                If operation.NewName IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("newName:=").
                        Append(VBCode.Literal(operation.NewName))
                End If

                If operation.NewSchema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("newSchema:=").
                        Append(VBCode.Literal(operation.NewSchema))
                End If

                builder.Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="RenameTableOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As RenameTableOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".RenameTable(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                If operation.NewName IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("newName:=").
                        Append(VBCode.Literal(operation.NewName))
                End If

                If operation.NewSchema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("newSchema:=").
                        Append(VBCode.Literal(operation.NewSchema))
                End If

                builder.Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="RestartSequenceOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As RestartSequenceOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".RestartSequence(")

            Using builder.Indent()
                builder.
                    Append("name:=").
                    Append(VBCode.Literal(operation.Name))

                If operation.Schema IsNot Nothing Then
                    builder.
                        AppendLine(",").
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema))
                End If

                builder.
                    AppendLine(",").
                    Append("startValue:=").
                    Append(VBCode.Literal(operation.StartValue)).
                    Append(")")

                Annotations(operation.GetAnnotations(), builder)
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="SqlOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As SqlOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.
                Append(".Sql(").
                Append(VBCode.Literal(operation.Sql)).
                Append(")")

            Annotations(operation.GetAnnotations(), builder)
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="InsertDataOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As InsertDataOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".InsertData(")

            Using builder.Indent()
                If operation.Schema IsNot Nothing Then
                    builder.
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema)).
                        AppendLine(",")
                End If

                builder.
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table)).
                    AppendLine(",")

                If operation.Columns.Length = 1 Then
                    builder.
                        Append("column:=").
                        Append(VBCode.Literal(operation.Columns(0)))
                Else
                    builder.
                        Append("columns:=").
                        Append(VBCode.Literal(operation.Columns))
                End If

                builder.AppendLine(",")

                If operation.Values.GetLength(0) = 1 AndAlso operation.Values.GetLength(1) = 1 Then
                    builder.
                        Append("value:=").
                        Append(VBCode.UnknownLiteral(operation.Values(0, 0)))
                ElseIf operation.Values.GetLength(0) = 1 Then
                    builder.
                        Append("values:=").
                        Append(VBCode.Literal(ToOnedimensionalArray(operation.Values)))
                ElseIf operation.Values.GetLength(1) = 1 Then
                    builder.
                        Append("values:=").
                        AppendLines(
                            VBCode.Literal(
                                ToOnedimensionalArray(operation.Values, firstDimension:=True),
                                vertical:=True),
                            skipFinalNewline:=True)
                Else
                    builder.
                        Append("values:=").
                        AppendLines(VBCode.Literal(operation.Values), skipFinalNewline:=True)
                End If

                builder.Append(")")
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for a <see cref="DeleteDataOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code Is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As DeleteDataOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".DeleteData(")

            Using builder.Indent()
                If operation.Schema IsNot Nothing Then
                    builder.
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema)).
                        AppendLine(",")
                End If

                builder.
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table)).
                    AppendLine(",")

                If operation.KeyColumns.Length = 1 Then
                    builder.
                        Append("keyColumn:=").
                        Append(VBCode.Literal(operation.KeyColumns(0)))
                Else
                    builder.
                        Append("keyColumns:=").
                        Append(VBCode.Literal(operation.KeyColumns))
                End If

                builder.AppendLine(",")

                If operation.KeyColumnTypes IsNot Nothing Then
                    If operation.KeyColumnTypes.Length = 1 Then
                        builder.
                            Append("keyColumnType:=").
                            Append(VBCode.Literal(operation.KeyColumnTypes(0)))
                    Else
                        builder.
                            Append("keyColumnTypes:=").
                            Append(VBCode.Literal(operation.KeyColumnTypes))
                    End If

                    builder.AppendLine(",")
                End If

                If operation.KeyValues.GetLength(0) = 1 AndAlso operation.KeyValues.GetLength(1) = 1 Then
                    builder.
                        Append("keyValue:=").
                        Append(VBCode.UnknownLiteral(operation.KeyValues(0, 0)))
                ElseIf operation.KeyValues.GetLength(0) = 1 Then
                    builder.
                        Append("keyValues:=").
                        Append(VBCode.Literal(ToOnedimensionalArray(operation.KeyValues)))
                ElseIf operation.KeyValues.GetLength(1) = 1 Then
                    builder.
                        Append("keyValues:=").
                        AppendLines(
                            VBCode.Literal(
                                ToOnedimensionalArray(operation.KeyValues, firstDimension:=True),
                                vertical:=True),
                            skipFinalNewline:=True)
                Else
                    builder.
                        Append("keyValues:=").
                        AppendLines(VBCode.Literal(operation.KeyValues), skipFinalNewline:=True)
                End If

                builder.Append(")")
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for an <see cref="UpdateDataOperation" />.
        ''' </summary>
        ''' <param name="operation">The operation.</param>
        ''' <param name="builder">The builder code is added to.</param>
        Protected Overridable Overloads Sub Generate(operation As UpdateDataOperation, builder As IndentedStringBuilder)

            NotNull(operation, NameOf(operation))
            NotNull(builder, NameOf(builder))

            builder.AppendLine(".UpdateData(")

            Using builder.Indent()
                If operation.Schema IsNot Nothing Then
                    builder.
                        Append("schema:=").
                        Append(VBCode.Literal(operation.Schema)).
                        AppendLine(",")
                End If

                builder.
                    Append("table:=").
                    Append(VBCode.Literal(operation.Table)).
                    AppendLine(",")

                If operation.KeyColumns.Length = 1 Then
                    builder.
                        Append("keyColumn:=").
                        Append(VBCode.Literal(operation.KeyColumns(0)))
                Else
                    builder.
                        Append("keyColumns:=").
                        Append(VBCode.Literal(operation.KeyColumns))
                End If

                builder.AppendLine(",")

                If operation.KeyValues.GetLength(0) = 1 AndAlso operation.KeyValues.GetLength(1) = 1 Then
                    builder.
                        Append("keyValue:=").
                        Append(VBCode.UnknownLiteral(operation.KeyValues(0, 0)))
                ElseIf operation.KeyValues.GetLength(0) = 1 Then
                    builder.
                        Append("keyValues:=").
                        Append(VBCode.Literal(ToOnedimensionalArray(operation.KeyValues)))
                ElseIf operation.KeyValues.GetLength(1) = 1 Then
                    builder.
                        Append("keyValues:=").
                        AppendLines(
                            VBCode.Literal(
                                ToOnedimensionalArray(
                                    operation.KeyValues,
                                    firstDimension:=True),
                                vertical:=True),
                            skipFinalNewline:=True)
                Else
                    builder.
                        Append("keyValues:=").
                        AppendLines(VBCode.Literal(operation.KeyValues), skipFinalNewline:=True)
                End If

                builder.AppendLine(",")

                If operation.Columns.Length = 1 Then
                    builder.
                        Append("column:=").
                        Append(VBCode.Literal(operation.Columns(0)))
                Else
                    builder.
                        Append("columns:=").
                        Append(VBCode.Literal(operation.Columns))
                End If

                builder.AppendLine(",")

                If operation.Values.GetLength(0) = 1 AndAlso operation.Values.GetLength(1) = 1 Then
                    builder.
                        Append("value:=").
                        Append(VBCode.UnknownLiteral(operation.Values(0, 0)))
                ElseIf operation.Values.GetLength(0) = 1 Then
                    builder.
                        Append("values:=").
                        Append(VBCode.Literal(ToOnedimensionalArray(operation.Values)))
                ElseIf operation.Values.GetLength(1) = 1 Then
                    builder.
                        Append("values:=").
                        AppendLines(
                            VBCode.Literal(
                                ToOnedimensionalArray(
                                    operation.Values,
                                    firstDimension:=True),
                                vertical:=True),
                            skipFinalNewline:=True)
                Else
                    builder.
                        Append("values:=").
                        AppendLines(VBCode.Literal(operation.Values), skipFinalNewline:=True)
                End If

                builder.Append(")")
            End Using
        End Sub

        ''' <summary>
        '''     Generates code for <see cref="Annotation" /> objects.
        ''' </summary>
        ''' <param name="annotations">The annotations.</param>
        ''' <param name="builder">The builder code is added to.</param>
        Protected Overridable Overloads Sub Annotations(annotations As IEnumerable(Of Annotation), builder As IndentedStringBuilder)

            NotNull(annotations, NameOf(annotations))
            NotNull(builder, NameOf(builder))

            For Each annotation In annotations

                ' TODO: Give providers an opportunity To render these As provider-specific extension methods
                builder.
                    AppendLine(".").
                    IncrementIndent().
                    Append("Annotation(").
                    Append(VBCode.Literal(annotation.Name)).
                    Append(", ").
                    Append(VBCode.UnknownLiteral(annotation.Value)).
                    Append(")").
                    DecrementIndent()
            Next
        End Sub

        ''' <summary>
        '''     Generates code for removed <see cref="Annotation" /> objects.
        ''' </summary>
        ''' <param name="annotations">The annotations.</param>
        ''' <param name="builder">The builder code is added to.</param>
        Protected Overridable Overloads Sub OldAnnotations(annotations As IEnumerable(Of Annotation), builder As IndentedStringBuilder)

            NotNull(annotations, NameOf(annotations))
            NotNull(builder, NameOf(builder))

            For Each annotation In annotations
                ' TODO: Give providers an opportunity To render these As provider-specific extension methods
                builder.
                    AppendLine(".").
                    IncrementIndent().
                    Append("OldAnnotation(").
                    Append(VBCode.Literal(annotation.Name)).
                    Append(", ").
                    Append(VBCode.UnknownLiteral(annotation.Value)).
                    Append(")").
                    DecrementIndent()
            Next
        End Sub

        Private Shared Function ToOnedimensionalArray(values As Object(,), Optional firstDimension As Boolean = False) As Object()
            DebugAssert(
                values.GetLength(If(firstDimension, 1, 0)) = 1,
                $"Length of dimension {If(firstDimension, 1, 0)} is not 1.")

            Dim result(values.Length - 1) As Object
            For i = 0 To values.Length - 1
                result(i) = If(firstDimension, values(i, 0), values(0, i))
            Next

            Return result
        End Function
    End Class

End Namespace
