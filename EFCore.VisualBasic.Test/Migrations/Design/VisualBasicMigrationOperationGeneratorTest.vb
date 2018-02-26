Imports System.Reflection
Imports Bricelam.EntityFrameworkCore.VisualBasic.Microsoft.EntityFrameworkCore.TestUtilities
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.Migrations
Imports Microsoft.EntityFrameworkCore.Migrations.Operations
Imports Xunit

Public Class VisualBasicMigrationOperationGeneratorTest

    Private Function CreateGenerator() As VisualBasicMigrationOperationGenerator
        Return New VisualBasicMigrationOperationGenerator(
                New VisualBasicMigrationOperationGeneratorDependencies(New VisualBasicHelper()))
    End Function

    <Fact>
    Public Sub Generate_seperates_operations_by_a_blank_line()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        generator.Generate(
                "mb",
                {
                    New SqlOperation With {.Sql = "-- Don't stand so"},
                    New SqlOperation With {.Sql = "-- close to me"}
                },
                builder)

        Dim expectedCode =
"mb.Sql(""-- Don't stand so"")

mb.Sql(""-- close to me"")"

        Assert.Equal(expectedCode, builder.ToString())
    End Sub


    'Private Sub Test(Of T As MigrationOperation)(operation As T, expectedCode As String, assertAction As Action(Of T))
    '    Dim generator = New VisualBasicMigrationOperationGenerator(
    '        New VisualBasicMigrationOperationGeneratorDependencies(New VisualBasicHelper()))

    '    Dim builder = New IndentedStringBuilder()
    '    generator.Generate("mb", {operation}, builder)
    '    Dim code = builder.ToString()

    '    Assert.Equal(expectedCode, code)

    '    Dim build = New BuildSource() With
    '    {
    '        .References =
    '        {
    '            BuildReference.ByName("Microsoft.EntityFrameworkCore.Relational")
    '        },
    '        .Sources =
    '        {
    '            "
    '            Imports Microsoft.EntityFrameworkCore.Migrations

    '             Public Module OperationsFactory

    '                Public Shared Sub Create(mb as MigrationBuilder)
    '                    " + code + "
    '                End Sub

    '             End Class
    '        "
    '        }
    '    }

    '    Dim assembly = build.BuildInMemory()
    '    Dim factoryType = assembly.GetType("OperationsFactory")
    '    Dim createMethod = factoryType.GetTypeInfo().GetDeclaredMethod("Create")
    '    Dim mb = New MigrationBuilder(activeProvider:=Nothing)
    '    createMethod.Invoke(Nothing, {mb})
    '    Dim result = mb.Operations.Cast(Of T)().Single()

    '    assertAction(result)
    'End Sub

    <Fact>
    Public Sub AddColumnOperation_required_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AddColumnOperation() With
            {
                .Name = "Id",
                .Table = "Post",
                .ClrType = GetType(Integer)
            }

        Dim expectedCode =
"mb.AddColumn(Of Integer)(
    name:= ""Id"",
    table:= ""Post"",
    nullable:= False)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())
    End Sub

    <Fact>
    Public Sub AddColumnOperation_all_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AddColumnOperation() With
            {
                .Name = "Id",
                .Schema = "dbo",
                .Table = "Post",
                .ClrType = GetType(Integer),
                .ColumnType = "Integer",
                .IsUnicode = False,
                .MaxLength = 30,
                .IsRowVersion = True,
                .IsNullable = True,
                .DefaultValue = 1
            }

        Dim expectedCode =
"mb.AddColumn(Of Integer)(
    name:= ""Id"",
    schema:= ""dbo"",
    table:= ""Post"",
    type:= ""Integer"",
    unicode:= False,
    maxLength:= 30,
    rowVersion:= True,
    nullable:= True,
    defaultValue:= 1)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())
    End Sub

    <Fact>
    Public Sub AddColumnOperation_DefaultValueSql()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AddColumnOperation() With
            {
                .Name = "Id",
                .Table = "Post",
                .ClrType = GetType(Integer),
                .DefaultValueSql = "1"
            }

        Dim expectedCode =
"mb.AddColumn(Of Integer)(
    name:= ""Id"",
    table:= ""Post"",
    nullable:= False,
    defaultValueSql:= ""1"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())
    End Sub

    <Fact>
    Public Sub AddColumnOperation_ComputedExpression()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AddColumnOperation() With
            {
                .Name = "Id",
                .Table = "Post",
                .ClrType = GetType(Integer),
                .ComputedColumnSql = "1"
            }

        Dim expectedCode =
"mb.AddColumn(Of Integer)(
    name:= ""Id"",
    table:= ""Post"",
    nullable:= False,
    computedColumnSql:= ""1"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub AddForeignKeyOperation_required_args()


        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AddForeignKeyOperation() With
            {
                .Table = "Post",
                .Name = "FK_Post_Blog_BlogId",
                .Columns = {"BlogId"},
                .PrincipalTable = "Blog",
                .PrincipalColumns = {"Id"}
            }

        Dim expectedCode =
"mb.AddForeignKey(
    name:= ""FK_Post_Blog_BlogId"",
    table:= ""Post"",
    column:= ""BlogId"",
    principalTable:= ""Blog"",
    principalColumn:= ""Id"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub AddForeignKeyOperation_all_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AddForeignKeyOperation() With
            {
                .Schema = "dbo",
                .Table = "Post",
                .Name = "FK_Post_Blog_BlogId",
                .Columns = {"BlogId"},
                .PrincipalSchema = "my",
                .PrincipalTable = "Blog",
                .PrincipalColumns = {"Id"},
                .OnUpdate = ReferentialAction.Restrict,
                .OnDelete = ReferentialAction.Cascade
            }

        Dim expectedCode =
"mb.AddForeignKey(
    name:= ""FK_Post_Blog_BlogId"",
    schema:= ""dbo"",
    table:= ""Post"",
    column:= ""BlogId"",
    principalSchema:= ""my"",
    principalTable:= ""Blog"",
    principalColumn:= ""Id"",
    onUpdate:= ReferentialAction.Restrict,
    onDelete:= ReferentialAction.Cascade)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())
    End Sub

    <Fact>
    Public Sub AddForeignKeyOperation_composite()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AddForeignKeyOperation() With
            {
.Name = "FK_Post_Blog_BlogId1_BlogId2",
                .Table = "Post",
                .Columns = {"BlogId1", "BlogId2"},
                .PrincipalTable = "Blog",
                .PrincipalColumns = {"Id1", "Id2"}
            }

        Dim expectedCode =
"mb.AddForeignKey(
    name:= ""FK_Post_Blog_BlogId1_BlogId2"",
    table:= ""Post"",
    columns:= {""BlogId1"", ""BlogId2""},
    principalTable:= ""Blog"",
    principalColumns:= {""Id1"", ""Id2""})"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())
    End Sub

    <Fact>
    Public Sub AddPrimaryKey_required_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AddPrimaryKeyOperation() With
            {
                .Name = "PK_Post",
                .Table = "Post",
                .Columns = {"Id"}
            }

        Dim expectedCode =
"mb.AddPrimaryKey(
    name:= ""PK_Post"",
    table:= ""Post"",
    column:= ""Id"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub AddPrimaryKey_all_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AddPrimaryKeyOperation() With
            {
                .Name = "PK_Post",
                .Schema = "dbo",
                .Table = "Post",
                .Columns = {"Id"}
            }

        Dim expectedCode =
"mb.AddPrimaryKey(
    name:= ""PK_Post"",
    schema:= ""dbo"",
    table:= ""Post"",
    column:= ""Id"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub AddPrimaryKey_composite()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AddPrimaryKeyOperation() With
            {
                .Name = "PK_Post",
                .Table = "Post",
                .Columns = {"Id1", "Id2"}
            }

        Dim expectedCode =
"mb.AddPrimaryKey(
    name:= ""PK_Post"",
    table:= ""Post"",
    columns:= {""Id1"", ""Id2""})"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub AddUniqueConstraint_required_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AddUniqueConstraintOperation() With
            {
                .Name = "AK_Post_AltId",
                .Table = "Post",
                .Columns = {"AltId"}
            }

        Dim expectedCode =
"mb.AddUniqueConstraint(
    name:= ""AK_Post_AltId"",
    table:= ""Post"",
    column:= ""AltId"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub AddUniqueConstraint_all_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AddUniqueConstraintOperation() With
            {
                .Name = "AK_Post_AltId",
                .Schema = "dbo",
                .Table = "Post",
                .Columns = {"AltId"}
            }

        Dim expectedCode =
"mb.AddUniqueConstraint(
    name:= ""AK_Post_AltId"",
    schema:= ""dbo"",
    table:= ""Post"",
    column:= ""AltId"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub AddUniqueConstraint_composite()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AddUniqueConstraintOperation() With
            {
                .Name = "AK_Post_AltId",
                .Table = "Post",
                .Columns = {"AltId1", "AltId2"}
            }

        Dim expectedCode =
"mb.AddUniqueConstraint(
    name:= ""AK_Post_AltId"",
    table:= ""Post"",
    columns:= {""AltId1"", ""AltId2""})"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub AlterColumnOperation_required_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AlterColumnOperation() With
            {
                .Name = "Id",
                .Table = "Post",
                .ClrType = GetType(Integer)
            }

        Dim expectedCode =
"mb.AlterColumn(Of Integer)(
    name:= ""Id"",
    table:= ""Post"",
    nullable:= False)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub AlterColumnOperation_all_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AlterColumnOperation() With
            {
                .Name = "Id",
                .Schema = "dbo",
                .Table = "Post",
                .ClrType = GetType(Integer),
                .ColumnType = "Integer",
                .IsUnicode = False,
                .MaxLength = 30,
                .IsRowVersion = True,
                .IsNullable = True,
                .DefaultValue = 1,
                .OldColumn = New ColumnOperation With
                {
                   .ClrType = GetType(String),
                    .ColumnType = "String",
                    .IsUnicode = False,
                    .MaxLength = 20,
                    .IsRowVersion = True,
                    .IsNullable = True,
                    .DefaultValue = 0
                }
            }

        Dim expectedCode =
"mb.AlterColumn(Of Integer)(
    name:= ""Id"",
    schema:= ""dbo"",
    table:= ""Post"",
    type:= ""Integer"",
    unicode:= False,
    maxLength:= 30,
    rowVersion:= True,
    nullable:= True,
    defaultValue:= 1,
    oldClrType:= GetType(String),
    oldType:= ""String"",
    oldUnicode:= False,
    oldMaxLength:= 20,
    oldRowVersion:= True,
    oldNullable:= True,
    oldDefaultValue:= 0)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub AlterColumnOperation_DefaultValueSql()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AlterColumnOperation() With
            {
                .Name = "Id",
                .Table = "Post",
                .ClrType = GetType(Integer),
                .DefaultValueSql = "1"
            }

        Dim expectedCode =
"mb.AlterColumn(Of Integer)(
    name:= ""Id"",
    table:= ""Post"",
    nullable:= False,
    defaultValueSql:= ""1"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())
    End Sub

    <Fact>
    Public Sub AlterColumnOperation_computedColumnSql()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AlterColumnOperation() With
            {
                .Name = "Id",
                .Table = "Post",
                .ClrType = GetType(Integer),
                .ComputedColumnSql = "1"
            }

        Dim expectedCode =
"mb.AlterColumn(Of Integer)(
    name:= ""Id"",
    table:= ""Post"",
    nullable:= False,
    computedColumnSql:= ""1"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub AlterDatabaseOperation()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AlterDatabaseOperation()
        operation("foo") = "bar"
        operation.OldDatabase("bar") = "foo"

        Dim expectedCode =
"mb.AlterDatabase()
    .Annotation(""foo"", ""bar"")
    .OldAnnotation(""bar"", ""foo"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())
    End Sub

    <Fact>
    Public Sub AlterSequenceOperation_required_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AlterSequenceOperation() With
            {
            .Name = "EntityFrameworkHiLoSequence"
        }

        Dim expectedCode =
"mb.AlterSequence(
    name:= ""EntityFrameworkHiLoSequence"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())
    End Sub

    <Fact>
    Public Sub AlterSequenceOperation_all_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AlterSequenceOperation() With
            {
 .Name = "EntityFrameworkHiLoSequence",
                .Schema = "dbo",
                .IncrementBy = 3,
                .MinValue = 2,
                .MaxValue = 4,
                .IsCyclic = True,
                .OldSequence = New SequenceOperation() With
                {
                    .IncrementBy = 4,
                    .MinValue = 3,
                    .MaxValue = 5,
                    .IsCyclic = True
                }
        }

        Dim expectedCode =
"mb.AlterSequence(
    name:= ""EntityFrameworkHiLoSequence"",
    schema:= ""dbo"",
    incrementBy:= 3,
    minValue:= 2L,
    maxValue:= 4L,
    cyclic:= True,
    oldIncrementBy:= 4,
    oldMinValue:= 3L,
    oldMaxValue:= 5L,
    oldCyclic:= True)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub AlterTableOperation()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New AlterTableOperation() With
        {
            .Name = "Customer",
            .Schema = "dbo"
        }

        Dim expectedCode =
"mb.AlterTable(
    name:= ""Customer"",
    schema:= ""dbo"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())
    End Sub

    <Fact>
    Public Sub CreateIndexOperation_required_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateIndexOperation() With
        {
                .Name = "IX_Post_Title",
                .Table = "Post",
                .Columns = {"Title"}
        }

        Dim expectedCode =
"mb.CreateIndex(
    name:= ""IX_Post_Title"",
    table:= ""Post"",
    column:= ""Title"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateIndexOperation_all_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateIndexOperation() With
        {
                .Name = "IX_Post_Title",
                .Table = "Post",
                .Columns = {"Title"},
                .IsUnique = True,
                .Filter = "[Title] IS NOT NULL"
        }

        Dim expectedCode =
"mb.CreateIndex(
    name:= ""IX_Post_Title"",
    table:= ""Post"",
    column:= ""Title"",
    unique:= True,
    filter:= ""[Title] IS NOT NULL"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateIndexOperation_composite()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateIndexOperation() With
        {
                .Name = "IX_Post_Title_Subtitle",
                .Table = "Post",
                .Columns = {"Title", "Subtitle"}
        }

        Dim expectedCode =
"mb.CreateIndex(
    name:= ""IX_Post_Title_Subtitle"",
    table:= ""Post"",
    columns:= {""Title"", ""Subtitle""})"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateSchemaOperation_required_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New EnsureSchemaOperation() With {.Name = "my"}

        Dim expectedCode =
"mb.EnsureSchema(
    name:= ""my"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateSequenceOperation_required_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateSequenceOperation() With
        {
            .Name = "EntityFrameworkHiLoSequence",
            .ClrType = GetType(Long)
        }

        Dim expectedCode =
"mb.CreateSequence(
    name:= ""EntityFrameworkHiLoSequence"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateSequenceOperation_required_args_not_long()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateSequenceOperation() With
        {
            .Name = "EntityFrameworkHiLoSequence",
            .ClrType = GetType(Integer)
        }

        Dim expectedCode =
"mb.CreateSequence(Of Integer)(
    name:= ""EntityFrameworkHiLoSequence"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateSequenceOperation_all_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateSequenceOperation() With
        {
            .Name = "EntityFrameworkHiLoSequence",
                .Schema = "dbo",
                .ClrType = GetType(Long),
                .StartValue = 3,
                .IncrementBy = 5,
               .MinValue = 2,
               .MaxValue = 4,
                .IsCyclic = True
        }

        Dim expectedCode =
"mb.CreateSequence(
    name:= ""EntityFrameworkHiLoSequence"",
    schema:= ""dbo"",
    startValue:= 3L,
    incrementBy:= 5,
    minValue:= 2L,
    maxValue:= 4L,
    cyclic:= True)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateSequenceOperation_all_args_not_long()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateSequenceOperation() With
        {
            .Name = "EntityFrameworkHiLoSequence",
                .Schema = "dbo",
                .ClrType = GetType(Integer),
                .StartValue = 3,
                .IncrementBy = 5,
               .MinValue = 2,
               .MaxValue = 4,
                .IsCyclic = True
        }

        Dim expectedCode =
"mb.CreateSequence(Of Integer)(
    name:= ""EntityFrameworkHiLoSequence"",
    schema:= ""dbo"",
    startValue:= 3L,
    incrementBy:= 5,
    minValue:= 2L,
    maxValue:= 4L,
    cyclic:= True)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateTableOperation_Columns_required_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateTableOperation() With {.Name = "Post"}
        operation.Columns.Add(
            New AddColumnOperation() With
            {
                .Name = "Id",
                .Table = "Post",
                .ClrType = GetType(Integer)
            })

        Dim expectedCode =
"mb.CreateTable(
    name:= ""Post"",
    columns:= Function(table) New With 
    {
        .Id = table.Column(Of Integer)(nullable:= False)
    }
    End Function,
    constraints:= Function(table)
    End Function)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateTableOperation_Columns_all_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateTableOperation() With
        {
            .Name = "Post",
            .Schema = "dbo"
        }
        operation.Columns.Add(
            New AddColumnOperation() With
            {
                .Name = "Post Id",
                .Schema = "dbo",
                .Table = "Post",
                .ClrType = GetType(Integer),
                .ColumnType = "Integer",
                .IsUnicode = False,
                .MaxLength = 30,
                .IsRowVersion = True,
                .IsNullable = True,
                .DefaultValue = 1
            })

        Dim expectedCode =
"mb.CreateTable(
    name:= ""Post"",
    schema:= ""dbo"",
    columns:= Function(table) New With 
    {
        .PostId = table.Column(Of Integer)(name:= ""Post Id"", type:= ""Integer"", unicode:= False, maxLength:= 30, rowVersion:= True, nullable:= True, defaultValue:= 1)
    }
    End Function,
    constraints:= Function(table)
    End Function)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateTableOperation_Columns_DefaultValueSql()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateTableOperation() With {.Name = "Post"}
        operation.Columns.Add(
            New AddColumnOperation() With
            {
                .Name = "Id",
                .Table = "Post",
                .ClrType = GetType(Integer),
                .DefaultValueSql = "1"
            })

        Dim expectedCode =
"mb.CreateTable(
    name:= ""Post"",
    columns:= Function(table) New With 
    {
        .Id = table.Column(Of Integer)(nullable:= False, defaultValueSql:= ""1"")
    }
    End Function,
    constraints:= Function(table)
    End Function)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateTableOperation_Columns_computedColumnSql()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateTableOperation() With {.Name = "Post"}
        operation.Columns.Add(
            New AddColumnOperation() With
            {
                .Name = "Id",
                .Table = "Post",
                .ClrType = GetType(Integer),
                .ComputedColumnSql = "1"
            })

        Dim expectedCode =
"mb.CreateTable(
    name:= ""Post"",
    columns:= Function(table) New With 
    {
        .Id = table.Column(Of Integer)(nullable:= False, computedColumnSql:= ""1"")
    }
    End Function,
    constraints:= Function(table)
    End Function)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateTableOperation_ForeignKeys_required_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateTableOperation() With {.Name = "Post"}
        operation.Columns.Add(
            New AddColumnOperation() With
            {
                .Name = "BlogId",
                .ClrType = GetType(Integer)
            })
        operation.ForeignKeys.Add(
            New AddForeignKeyOperation() With
            {
                .Name = "FK_Post_Blog_BlogId",
                .Table = "Post",
                .Columns = {"BlogId"},
                .PrincipalTable = "Blog",
                .PrincipalColumns = {"Id"}
            })

        Dim expectedCode =
"mb.CreateTable(
    name:= ""Post"",
    columns:= Function(table) New With 
    {
        .BlogId = table.Column(Of Integer)(nullable:= False)
    }
    End Function,
    constraints:= Function(table)
        table.ForeignKey(
            name:= ""FK_Post_Blog_BlogId"",
            column:= Function(x) x.BlogId,
            principalTable:= ""Blog"",
            principalColumn:= ""Id"")
    End Function)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateTableOperation_ForeignKeys_all_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateTableOperation() With
        {
            .Name = "Post",
            .Schema = "dbo"
        }
        operation.Columns.Add(
            New AddColumnOperation() With
            {
                .Name = "BlogId",
                .ClrType = GetType(Integer)
            })
        operation.ForeignKeys.Add(
            New AddForeignKeyOperation() With
            {
                .Schema = "dbo",
                .Table = "Post",
                .Name = "FK_Post_Blog_BlogId",
                .Columns = {"BlogId"},
                .PrincipalTable = "Blog",
                .PrincipalSchema = "my",
                .PrincipalColumns = {"Id"},
                .OnUpdate = ReferentialAction.SetNull,
                .OnDelete = ReferentialAction.SetDefault
            })

        Dim expectedCode =
"mb.CreateTable(
    name:= ""Post"",
    schema:= ""dbo"",
    columns:= Function(table) New With 
    {
        .BlogId = table.Column(Of Integer)(nullable:= False)
    }
    End Function,
    constraints:= Function(table)
        table.ForeignKey(
            name:= ""FK_Post_Blog_BlogId"",
            column:= Function(x) x.BlogId,
            principalSchema:= ""my"",
            principalTable:= ""Blog"",
            principalColumn:= ""Id"",
            onUpdate:= ReferentialAction.SetNull,
            onDelete:= ReferentialAction.SetDefault)
    End Function)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateTableOperation_ForeignKeys_composite()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateTableOperation() With {.Name = "Post"}
        operation.Columns.AddRange(
            {
                New AddColumnOperation() With
                {
                    .Name = "BlogId1",
                    .ClrType = GetType(Integer)
                },
                New AddColumnOperation() With
                {
                    .Name = "BlogId2",
                    .ClrType = GetType(Integer)
                }
            })
        operation.ForeignKeys.Add(
            New AddForeignKeyOperation() With
            {
                .Name = "FK_Post_Blog_BlogId1_BlogId2",
                .Table = "Post",
                .Columns = {"BlogId1", "BlogId2"},
                .PrincipalTable = "Blog",
                .PrincipalColumns = {"Id1", "Id2"}
            })

        Dim expectedCode =
"mb.CreateTable(
    name:= ""Post"",
    columns:= Function(table) New With 
    {
        .BlogId1 = table.Column(Of Integer)(nullable:= False),
        .BlogId2 = table.Column(Of Integer)(nullable:= False)
    }
    End Function,
    constraints:= Function(table)
        table.ForeignKey(
            name:= ""FK_Post_Blog_BlogId1_BlogId2"",
            columns:= Function(x) New With {x.BlogId1, x.BlogId2},
            principalTable:= ""Blog"",
            principalColumns:= {""Id1"", ""Id2""})
    End Function)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateTableOperation_PrimaryKey_required_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateTableOperation() With {.Name = "Post"}
        operation.Columns.Add(
            New AddColumnOperation() With
            {
                .Name = "Id",
                .ClrType = GetType(Integer)
            })
        operation.PrimaryKey = New AddPrimaryKeyOperation() With
            {
                .Name = "PK_Post",
                .Table = "Post",
                .Columns = {"Id"}
            }

        Dim expectedCode =
"mb.CreateTable(
    name:= ""Post"",
    columns:= Function(table) New With 
    {
        .Id = table.Column(Of Integer)(nullable:= False)
    }
    End Function,
    constraints:= Function(table)
        table.PrimaryKey(""PK_Post"", Function(x) x.Id)
    End Function)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateTableOperation_PrimaryKey_all_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateTableOperation() With
        {
            .Name = "Post",
            .Schema = "dbo"
        }
        operation.Columns.Add(
            New AddColumnOperation() With
            {
                .Name = "Id",
                .ClrType = GetType(Integer)
            })
        operation.PrimaryKey = New AddPrimaryKeyOperation() With
            {
                .Name = "PK_Post",
                .Schema = "dbo",
                .Table = "Post",
                .Columns = {"Id"}
            }

        Dim expectedCode =
"mb.CreateTable(
    name:= ""Post"",
    schema:= ""dbo"",
    columns:= Function(table) New With 
    {
        .Id = table.Column(Of Integer)(nullable:= False)
    }
    End Function,
    constraints:= Function(table)
        table.PrimaryKey(""PK_Post"", Function(x) x.Id)
    End Function)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateTableOperation_PrimaryKey_composite()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateTableOperation() With {.Name = "Post"}
        operation.Columns.AddRange(
            {
                New AddColumnOperation() With
                {
                    .Name = "Id1",
                    .ClrType = GetType(Integer)
                },
                New AddColumnOperation() With
                {
                    .Name = "Id2",
                    .ClrType = GetType(Integer)
                }
            })
        operation.PrimaryKey = New AddPrimaryKeyOperation() With
            {
                .Name = "PK_Post",
                .Table = "Post",
                .Columns = {"Id1", "Id2"}
            }

        Dim expectedCode =
"mb.CreateTable(
    name:= ""Post"",
    columns:= Function(table) New With 
    {
        .Id1 = table.Column(Of Integer)(nullable:= False),
        .Id2 = table.Column(Of Integer)(nullable:= False)
    }
    End Function,
    constraints:= Function(table)
        table.PrimaryKey(""PK_Post"", Function(x) New With {x.Id1, x.Id2})
    End Function)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateTableOperation_UniqueConstraints_required_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateTableOperation() With {.Name = "Post"}
        operation.Columns.Add(
            New AddColumnOperation() With
            {
                .Name = "AltId",
                .ClrType = GetType(Integer)
            })
        operation.UniqueConstraints.Add(
            New AddUniqueConstraintOperation() With
            {
                .Name = "AK_Post_AltId",
                .Table = "Post",
                .Columns = {"AltId"}
            })

        Dim expectedCode =
"mb.CreateTable(
    name:= ""Post"",
    columns:= Function(table) New With 
    {
        .AltId = table.Column(Of Integer)(nullable:= False)
    }
    End Function,
    constraints:= Function(table)
        table.UniqueConstraint(""AK_Post_AltId"", Function(x) x.AltId)
    End Function)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateTableOperation_UniqueConstraints_all_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateTableOperation() With
        {
            .Name = "Post",
            .Schema = "dbo"
        }
        operation.Columns.Add(
            New AddColumnOperation() With
            {
                .Name = "AltId",
                .ClrType = GetType(Integer)
            })
        operation.UniqueConstraints.Add(
            New AddUniqueConstraintOperation() With
            {
                .Name = "AK_Post_AltId",
                .Schema = "dbo",
                .Table = "Post",
                .Columns = {"AltId"}
            })

        Dim expectedCode =
"mb.CreateTable(
    name:= ""Post"",
    schema:= ""dbo"",
    columns:= Function(table) New With 
    {
        .AltId = table.Column(Of Integer)(nullable:= False)
    }
    End Function,
    constraints:= Function(table)
        table.UniqueConstraint(""AK_Post_AltId"", Function(x) x.AltId)
    End Function)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub CreateTableOperation_UniqueConstraints_composite()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New CreateTableOperation() With
        {
            .Name = "Post"
        }
        operation.Columns.AddRange(
            {
                New AddColumnOperation() With
                {
                    .Name = "AltId1",
                    .ClrType = GetType(Integer)
                },
                New AddColumnOperation() With
                {
                    .Name = "AltId2",
                    .ClrType = GetType(Integer)
                }
            })
        operation.UniqueConstraints.Add(
            New AddUniqueConstraintOperation() With
            {
                .Name = "AK_Post_AltId",
                .Schema = "dbo",
                .Table = "Post",
                .Columns = {"AltId1", "AltId2"}
            })

        Dim expectedCode =
"mb.CreateTable(
    name:= ""Post"",
    columns:= Function(table) New With 
    {
        .AltId1 = table.Column(Of Integer)(nullable:= False),
        .AltId2 = table.Column(Of Integer)(nullable:= False)
    }
    End Function,
    constraints:= Function(table)
        table.UniqueConstraint(""AK_Post_AltId"", Function(x) New With {x.AltId1, x.AltId2})
    End Function)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DropColumnOperation_required_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DropColumnOperation() With
        {
            .Name = "Id",
            .Table = "Post"
        }

        Dim expectedCode =
"mb.DropColumn(
    name:= ""Id"",
    table:= ""Post"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DropColumnOperation_all_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DropColumnOperation() With
        {
            .Name = "Id",
            .Schema = "dbo",
            .Table = "Post"
        }

        Dim expectedCode =
"mb.DropColumn(
    name:= ""Id"",
    schema:= ""dbo"",
    table:= ""Post"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DropForeignKeyOperation_required_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DropForeignKeyOperation() With
        {
            .Name = "FK_Post_BlogId",
            .Table = "Post"
        }

        Dim expectedCode =
"mb.DropForeignKey(
    name:= ""FK_Post_BlogId"",
    table:= ""Post"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DropForeignKeyOperation_all_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DropForeignKeyOperation() With
        {
            .Name = "FK_Post_BlogId",
            .Schema = "dbo",
            .Table = "Post"
        }

        Dim expectedCode =
"mb.DropForeignKey(
    name:= ""FK_Post_BlogId"",
    schema:= ""dbo"",
    table:= ""Post"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DropIndexOperation_required_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DropIndexOperation() With
        {
            .Name = "IX_Post_Title",
            .Table = "Post"
        }

        Dim expectedCode =
"mb.DropIndex(
    name:= ""IX_Post_Title"",
    table:= ""Post"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DropIndexOperation_all_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DropIndexOperation() With
        {
            .Name = "IX_Post_Title",
            .Schema = "dbo",
            .Table = "Post"
        }

        Dim expectedCode =
"mb.DropIndex(
    name:= ""IX_Post_Title"",
    schema:= ""dbo"",
    table:= ""Post"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DropPrimaryKeyOperation_required_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DropPrimaryKeyOperation() With
        {
            .Name = "PK_Post",
            .Table = "Post"
        }

        Dim expectedCode =
"mb.DropPrimaryKey(
    name:= ""PK_Post"",
    table:= ""Post"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DropPrimaryKeyOperation_all_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DropPrimaryKeyOperation() With
        {
            .Name = "PK_Post",
            .Schema = "dbo",
            .Table = "Post"
        }

        Dim expectedCode =
"mb.DropPrimaryKey(
    name:= ""PK_Post"",
    schema:= ""dbo"",
    table:= ""Post"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())
    End Sub

    <Fact>
    Public Sub DropSchemaOperation_required_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DropSchemaOperation() With {.Name = "my"}

        Dim expectedCode =
"mb.DropSchema(
    name:= ""my"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DropSequenceOperation_required_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DropSequenceOperation() With {.Name = "EntityFrameworkHiLoSequence"}

        Dim expectedCode =
"mb.DropSequence(
    name:= ""EntityFrameworkHiLoSequence"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())
    End Sub

    <Fact>
    Public Sub DropSequenceOperation_all_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DropSequenceOperation() With
        {
            .Name = "EntityFrameworkHiLoSequence",
            .Schema = "dbo"
        }

        Dim expectedCode =
"mb.DropSequence(
    name:= ""EntityFrameworkHiLoSequence"",
    schema:= ""dbo"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DropTableOperation_required_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DropTableOperation() With {.Name = "Post"}

        Dim expectedCode =
"mb.DropTable(
    name:= ""Post"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DropTableOperation_all_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DropTableOperation() With
        {
            .Name = "Post",
            .Schema = "dbo"
        }

        Dim expectedCode =
"mb.DropTable(
    name:= ""Post"",
    schema:= ""dbo"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DropUniqueConstraintOperation_required_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DropUniqueConstraintOperation() With
        {
            .Name = "AK_Post_AltId",
            .Table = "Post"
        }

        Dim expectedCode =
"mb.DropUniqueConstraint(
    name:= ""AK_Post_AltId"",
    table:= ""Post"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DropUniqueConstraintOperation_all_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DropUniqueConstraintOperation() With
        {
            .Name = "AK_Post_AltId",
            .Schema = "dbo",
            .Table = "Post"
        }

        Dim expectedCode =
"mb.DropUniqueConstraint(
    name:= ""AK_Post_AltId"",
    schema:= ""dbo"",
    table:= ""Post"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub RenameColumnOperation_required_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New RenameColumnOperation() With
        {
            .Name = "Id",
            .Table = "Post",
            .NewName = "PostId"
        }

        Dim expectedCode =
"mb.RenameColumn(
    name:= ""Id"",
    table:= ""Post"",
    newName:= ""PostId"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub RenameColumnOperation_all_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New RenameColumnOperation() With
        {
            .Name = "Id",
            .Schema = "dbo",
            .Table = "Post",
            .NewName = "PostId"
        }

        Dim expectedCode =
"mb.RenameColumn(
    name:= ""Id"",
    schema:= ""dbo"",
    table:= ""Post"",
    newName:= ""PostId"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub RenameIndexOperation_required_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New RenameIndexOperation() With
        {
            .Name = "IX_Post_Title",
            .Table = "Post",
            .NewName = "IX_Post_PostTitle"
        }

        Dim expectedCode =
"mb.RenameIndex(
    name:= ""IX_Post_Title"",
    table:= ""Post"",
    newName:= ""IX_Post_PostTitle"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub RenameIndexOperation_all_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New RenameIndexOperation() With
        {
            .Name = "IX_Post_Title",
            .Schema = "dbo",
            .Table = "Post",
            .NewName = "IX_dbo.Post_PostTitle"
        }

        Dim expectedCode =
"mb.RenameIndex(
    name:= ""IX_Post_Title"",
    schema:= ""dbo"",
    table:= ""Post"",
    newName:= ""IX_dbo.Post_PostTitle"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub RenameSequenceOperation_required_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New RenameSequenceOperation() With {.Name = "EntityFrameworkHiLoSequence"}

        Dim expectedCode =
"mb.RenameSequence(
    name:= ""EntityFrameworkHiLoSequence"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub RenameSequenceOperation_all_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New RenameSequenceOperation() With
        {
            .Name = "EntityFrameworkHiLoSequence",
            .Schema = "dbo",
            .NewName = "MySequence",
            .NewSchema = "my"
        }

        Dim expectedCode =
"mb.RenameSequence(
    name:= ""EntityFrameworkHiLoSequence"",
    schema:= ""dbo"",
    newName:= ""MySequence"",
    newSchema:= ""my"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub RenameTableOperation_required_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New RenameTableOperation() With {.Name = "Post"}

        Dim expectedCode =
"mb.RenameTable(
    name:= ""Post"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())
    End Sub

    <Fact>
    Public Sub RenameTableOperation_all_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New RenameSequenceOperation() With
        {
            .Name = "Post",
            .Schema = "dbo",
            .NewName = "Posts",
            .NewSchema = "my"
        }

        Dim expectedCode =
"mb.RenameSequence(
    name:= ""Post"",
    schema:= ""dbo"",
    newName:= ""Posts"",
    newSchema:= ""my"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub RestartSequenceOperation_required_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New RestartSequenceOperation() With
        {
            .Name = "EntityFrameworkHiLoSequence",
            .StartValue = 1
        }

        Dim expectedCode =
"mb.RestartSequence(
    name:= ""EntityFrameworkHiLoSequence"",
    startValue:= 1L)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub RestartSequenceOperation_all_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New RestartSequenceOperation() With
        {
            .Name = "EntityFrameworkHiLoSequence",
            .Schema = "dbo",
            .StartValue = 1
        }

        Dim expectedCode =
"mb.RestartSequence(
    name:= ""EntityFrameworkHiLoSequence"",
    schema:= ""dbo"",
    startValue:= 1L)"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub SqlOperation_required_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New SqlOperation() With {.Sql = "-- I <3 DDL"}

        Dim expectedCode = "mb.Sql(""-- I <3 DDL"")"

        generator.Generate("mb", {operation}, builder)

    End Sub

    <Fact>
    Public Sub InsertDataOperation_all_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New InsertDataOperation() With
        {
            .Schema = "dbo",
            .Table = "People",
            .Columns = {"Id", "Full Name"},
            .Values = New Object(,) {
                {0, Nothing},
                {1, "Daenerys Targaryen"},
                {2, "John Snow"},
                {3, "Arya Stark"},
                {4, "Harry Strickland"}
            }
        }

        Dim expectedCode =
"mb.InsertData(
    schema:= ""dbo"",
    table:= ""People"",
    columns:= {""Id"", ""Full Name""},
    values:= New Object(,) {
        {0, Nothing},
        {1, ""Daenerys Targaryen""},
        {2, ""John Snow""},
        {3, ""Arya Stark""},
        {4, ""Harry Strickland""}
    })"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub InsertDataOperation_required_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New InsertDataOperation() With
        {
            .Table = "People",
            .Columns = {"Full Name"},
            .Values = New Object(,) {
                {"John Snow"}
            }
        }

        Dim expectedCode =
"mb.InsertData(
    table:= ""People"",
    column:= ""Full Name"",
    value:= ""John Snow"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub InsertDataOperation_required_args_composite()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New InsertDataOperation() With
        {
            .Table = "People",
            .Columns = {"First Name", "Last Name"},
            .Values = New Object(,) {
                {"John", "Snow"}
            }
        }

        Dim expectedCode =
"mb.InsertData(
    table:= ""People"",
    columns:= {""First Name"", ""Last Name""},
    values:= New Object() {""John"", ""Snow""})"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub InsertDataOperation_required_args_multiple_rows()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New InsertDataOperation() With
        {
            .Table = "People",
            .Columns = {"Full Name"},
            .Values = New Object(,) {
                {"John Snow"},
                {"Daenerys Targaryen"}
            }
        }

        Dim expectedCode =
"mb.InsertData(
    table:= ""People"",
    column:= ""Full Name"",
    values:= New Object() {
        ""John Snow"",
        ""Daenerys Targaryen""
    })"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DeleteDataOperation_all_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DeleteDataOperation() With
        {
            .Schema = "dbo",
            .Table = "People",
            .KeyColumns = {"First Name"},
            .KeyValues = New Object(,) {
                {"Hodor"},
                {"Daenerys"},
                {"John"},
                {"Arya"},
                {"Harry"}
            }
        }

        Dim expectedCode =
"mb.DeleteData(
    schema:= ""dbo"",
    table:= ""People"",
    keyColumn:= ""First Name"",
    keyValues:= New Object() {
        ""Hodor"",
        ""Daenerys"",
        ""John"",
        ""Arya"",
        ""Harry""
    })"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DeleteDataOperation_all_args_composite()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DeleteDataOperation() With
        {
            .Table = "People",
            .KeyColumns = {"First Name", "Last Name"},
            .KeyValues = New Object(,) {
                {"Hodor", Nothing},
                {"Daenerys", "Targaryen"},
                {"John", "Snow"},
                {"Arya", "Stark"},
                {"Harry", "Strickland"}
            }
        }

        Dim expectedCode =
"mb.DeleteData(
    table:= ""People"",
    keyColumns:= {""First Name"", ""Last Name""},
    keyValues:= New Object(,) {
        {""Hodor"", Nothing},
        {""Daenerys"", ""Targaryen""},
        {""John"", ""Snow""},
        {""Arya"", ""Stark""},
        {""Harry"", ""Strickland""}
    })"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DeleteDataOperation_required_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DeleteDataOperation() With
        {
            .Table = "People",
            .KeyColumns = {"Last Name"},
            .KeyValues = New Object(,) {
                {"Snow"}
            }
        }

        Dim expectedCode =
"mb.DeleteData(
    table:= ""People"",
    keyColumn:= ""Last Name"",
    keyValue:= ""Snow"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub DeleteDataOperation_required_args_composite()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New DeleteDataOperation() With
        {
            .Table = "People",
            .KeyColumns = {"First Name", "Last Name"},
            .KeyValues = New Object(,) {
                {"John", "Snow"}
            }
        }

        Dim expectedCode =
"mb.DeleteData(
    table:= ""People"",
    keyColumns:= {""First Name"", ""Last Name""},
    keyValues:= New Object() {""John"", ""Snow""})"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub UpdateDataOperation_all_args()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New UpdateDataOperation() With
        {
            .Schema = "dbo",
            .Table = "People",
            .KeyColumns = {"First Name"},
            .KeyValues = New Object(,) {
                {"Hodor"},
                {"Daenerys"}
            },
            .Columns = {"Birthplace", "House Allegiance", "Culture"},
            .Values = New Object(,) {
                {"Winterfell", "Stark", "Northmen"},
                {"Dragonstone", "Targaryen", "Valyrian"}
            }
        }

        Dim expectedCode =
"mb.UpdateData(
    schema:= ""dbo"",
    table:= ""People"",
    keyColumn:= ""First Name"",
    keyValues:= New Object() {
        ""Hodor"",
        ""Daenerys""
    },
    columns:= {""Birthplace"", ""House Allegiance"", ""Culture""},
    values:= New Object(,) {
        {""Winterfell"", ""Stark"", ""Northmen""},
        {""Dragonstone"", ""Targaryen"", ""Valyrian""}
    })"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub UpdateDataOperation_all_args_composite()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New UpdateDataOperation() With
        {
            .Table = "People",
            .KeyColumns = {"First Name", "Last Name"},
            .KeyValues = New Object(,) {
                {"Hodor", Nothing},
                {"Daenerys", "Targaryen"}
            },
            .Columns = {"House Allegiance"},
            .Values = New Object(,) {
                {"Stark"},
                {"Targaryen"}
            }
        }

        Dim expectedCode =
"mb.UpdateData(
    table:= ""People"",
    keyColumns:= {""First Name"", ""Last Name""},
    keyValues:= New Object(,) {
        {""Hodor"", Nothing},
        {""Daenerys"", ""Targaryen""}
    },
    column:= ""House Allegiance"",
    values:= New Object() {
        ""Stark"",
        ""Targaryen""
    })"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub UpdateDataOperation_all_args_composite_multi()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New UpdateDataOperation() With
        {
            .Table = "People",
            .KeyColumns = {"First Name", "Last Name"},
            .KeyValues = New Object(,) {
                {"Hodor", Nothing},
                {"Daenerys", "Targaryen"}
            },
            .Columns = {"Birthplace", "House Allegiance", "Culture"},
            .Values = New Object(,) {
                {"Winterfell", "Stark", "Northmen"},
                {"Dragonstone", "Targaryen", "Valyrian"}
            }
        }

        Dim expectedCode =
"mb.UpdateData(
    table:= ""People"",
    keyColumns:= {""First Name"", ""Last Name""},
    keyValues:= New Object(,) {
        {""Hodor"", Nothing},
        {""Daenerys"", ""Targaryen""}
    },
    columns:= {""Birthplace"", ""House Allegiance"", ""Culture""},
    values:= New Object(,) {
        {""Winterfell"", ""Stark"", ""Northmen""},
        {""Dragonstone"", ""Targaryen"", ""Valyrian""}
    })"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub UpdateDataOperation_required_args()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New UpdateDataOperation() With
        {
            .Table = "People",
            .KeyColumns = {"First Name"},
            .KeyValues = New Object(,) {
                {"Daenerys"}
            },
            .Columns = {"House Allegiance"},
            .Values = New Object(,) {
                {"Targaryen"}
            }
        }

        Dim expectedCode =
"mb.UpdateData(
    table:= ""People"",
    keyColumn:= ""First Name"",
    keyValue:= ""Daenerys"",
    column:= ""House Allegiance"",
    value:= ""Targaryen"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub UpdateDataOperation_required_args_multiple_rows()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New UpdateDataOperation() With
        {
            .Table = "People",
            .KeyColumns = {"First Name"},
            .KeyValues = New Object(,) {
                {"Hodor"},
                {"Daenerys"}
            },
            .Columns = {"House Allegiance"},
            .Values = New Object(,) {
                {"Stark"},
                {"Targaryen"}
            }
        }

        Dim expectedCode =
"mb.UpdateData(
    table:= ""People"",
    keyColumn:= ""First Name"",
    keyValues:= New Object() {
        ""Hodor"",
        ""Daenerys""
    },
    column:= ""House Allegiance"",
    values:= New Object() {
        ""Stark"",
        ""Targaryen""
    })"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub UpdateDataOperation_required_args_composite()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New UpdateDataOperation() With
        {
            .Table = "People",
            .KeyColumns = {"First Name", "Last Name"},
            .KeyValues = New Object(,) {
                {"Daenerys", "Targaryen"}
            },
            .Columns = {"House Allegiance"},
            .Values = New Object(,) {
                {"Targaryen"}
            }
        }

        Dim expectedCode =
"mb.UpdateData(
    table:= ""People"",
    keyColumns:= {""First Name"", ""Last Name""},
    keyValues:= New Object() {""Daenerys"", ""Targaryen""},
    column:= ""House Allegiance"",
    value:= ""Targaryen"")"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub UpdateDataOperation_required_args_composite_multi()
        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New UpdateDataOperation() With
        {
            .Table = "People",
            .KeyColumns = {"First Name", "Last Name"},
            .KeyValues = New Object(,) {
                {"Daenerys", "Targaryen"}
            },
            .Columns = {"Birthplace", "House Allegiance", "Culture"},
            .Values = New Object(,) {
                {"Dragonstone", "Targaryen", "Valyrian"}
            }
        }

        Dim expectedCode =
"mb.UpdateData(
    table:= ""People"",
    keyColumns:= {""First Name"", ""Last Name""},
    keyValues:= New Object() {""Daenerys"", ""Targaryen""},
    columns:= {""Birthplace"", ""House Allegiance"", ""Culture""},
    values:= New Object() {""Dragonstone"", ""Targaryen"", ""Valyrian""})"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

    <Fact>
    Public Sub UpdateDataOperation_required_args_multi()

        Dim generator = CreateGenerator()
        Dim builder = New IndentedStringBuilder()

        Dim operation = New UpdateDataOperation() With
        {
            .Table = "People",
            .KeyColumns = {"Full Name"},
            .KeyValues = New Object(,) {
                {"Daenerys Targaryen"}
            },
            .Columns = {"Birthplace", "House Allegiance", "Culture"},
            .Values = New Object(,) {
                {"Dragonstone", "Targaryen", "Valyrian"}
            }
        }

        Dim expectedCode =
"mb.UpdateData(
    table:= ""People"",
    keyColumn:= ""Full Name"",
    keyValue:= ""Daenerys Targaryen"",
    columns:= {""Birthplace"", ""House Allegiance"", ""Culture""},
    values:= New Object() {""Dragonstone"", ""Targaryen"", ""Valyrian""})"

        generator.Generate("mb", {operation}, builder)

        Assert.Equal(expectedCode, builder.ToString())

    End Sub

End Class
