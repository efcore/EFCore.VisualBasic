Imports System.Reflection
Imports EntityFrameworkCore.VisualBasic.Design.Internal
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Migrations
Imports Microsoft.EntityFrameworkCore.Migrations.Operations
Imports Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal
Imports Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal
Imports Microsoft.EntityFrameworkCore.Storage
Imports Microsoft.EntityFrameworkCore.TestUtilities
Imports NetTopologySuite
Imports NetTopologySuite.Geometries

Imports Xunit

Namespace Migrations.Design

    Public Class VisualBasicMigrationOperationGeneratorTest

        Private Shared ReadOnly _nl As String = Environment.NewLine

        <ConditionalFact>
        Public Sub Generate_seperates_operations_by_a_blank_line()

            Dim generator As New VisualBasicMigrationOperationGenerator(
                New VisualBasicHelper(
                New SqlServerTypeMappingSource(
                    TestServiceFactory.Instance.Create(Of TypeMappingSourceDependencies)(),
                    TestServiceFactory.Instance.Create(Of RelationalTypeMappingSourceDependencies)(),
                    New SqlServerSingletonOptions())))

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

            Assert.Equal(expectedCode, builder.ToString(), ignoreLineEndingDifferences:=True)
        End Sub

        <ConditionalFact>
        Public Sub AddColumnOperation_required_args()
            Test(
                New AddColumnOperation With
                {
                    .Name = "Id",
                    .Table = "Post",
                    .ClrType = GetType(Integer)
                },
"mb.AddColumn(Of Integer)(
    name:=""Id"",
    table:=""Post"",
    nullable:=False)",
                Sub(o)
                    Assert.Equal("Id", o.Name)
                    Assert.Equal("Post", o.Table)
                    Assert.Equal(GetType(Integer), o.ClrType)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AddColumnOperation_all_args()
            Dim operation = New AddColumnOperation() With
            {
                .Name = "Id",
                .Schema = "dbo",
                .Table = "Post",
                .ClrType = GetType(Integer),
                .ColumnType = "int",
                .IsUnicode = False,
                .MaxLength = 30,
                .Precision = 10,
                .Scale = 5,
                .IsRowVersion = True,
                .IsNullable = True,
                .DefaultValue = 1,
                .IsFixedLength = True,
                .Comment = "My Comment",
                .Collation = "Some Collation"
            }

            Dim expectedCode =
"mb.AddColumn(Of Integer)(
    name:=""Id"",
    schema:=""dbo"",
    table:=""Post"",
    type:=""int"",
    unicode:=False,
    fixedLength:=True,
    maxLength:=30,
    precision:=10,
    scale:=5,
    rowVersion:=True,
    nullable:=True,
    defaultValue:=1,
    comment:=""My Comment"",
    collation:=""Some Collation"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Id", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal(GetType(Integer), o.ClrType)
                 Assert.Equal("int", o.ColumnType)
                 Assert.True(o.IsNullable)
                 Assert.Equal(1, o.DefaultValue)
                 Assert.False(o.IsUnicode)
                 Assert.True(o.IsFixedLength)
                 Assert.Equal("My Comment", o.Comment)
                 Assert.Equal("Some Collation", o.Collation)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AddColumnOperation_DefaultValueSql()
            Dim operation = New AddColumnOperation() With
            {
                 .Name = "Id",
                 .Table = "Post",
                 .ClrType = GetType(Integer),
                 .DefaultValueSql = "1"
            }

            Dim expectedCode =
"mb.AddColumn(Of Integer)(
    name:=""Id"",
    table:=""Post"",
    nullable:=False,
    defaultValueSql:=""1"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Id", o.Name)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal(GetType(Integer), o.ClrType)
                 Assert.Equal("1", o.DefaultValueSql)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AddColumnOperation_ComputedExpression()
            Dim operation = New AddColumnOperation() With
            {
                 .Name = "Id",
                 .Table = "Post",
                 .ClrType = GetType(Integer),
                 .ComputedColumnSql = "1",
                 .IsStored = True
            }

            Dim expectedCode =
"mb.AddColumn(Of Integer)(
    name:=""Id"",
    table:=""Post"",
    nullable:=False,
    computedColumnSql:=""1"",
    stored:=True)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Id", o.Name)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal(GetType(Integer), o.ClrType)
                 Assert.Equal("1", o.ComputedColumnSql)
                 Assert.True(o.IsStored)
             End Sub)

        End Sub

        <ConditionalFact>
        Public Sub AddForeignKeyOperation_required_args()
            Dim operation = New AddForeignKeyOperation() With
            {
                .Name = "FK_Post_Blog_BlogId",
                .Table = "Post",
                .Columns = {"BlogId"},
                .PrincipalTable = "Blog"
            }

            Dim expectedCode =
"mb.AddForeignKey(
    name:=""FK_Post_Blog_BlogId"",
    table:=""Post"",
    column:=""BlogId"",
    principalTable:=""Blog"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("FK_Post_Blog_BlogId", o.Name)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal({"BlogId"}, o.Columns)
                 Assert.Equal("Blog", o.PrincipalTable)
                 Assert.Null(o.PrincipalColumns)
             End Sub)

        End Sub

        <ConditionalFact>
        Public Sub AddForeignKeyOperation_required_args_composite()

            Test(
                New AddForeignKeyOperation With
                        {
                    .Name = "FK_Post_Blog_BlogId1_BlogId2",
                    .Table = "Post",
                    .Columns = {"BlogId1", "BlogId2"},
                    .PrincipalTable = "Blog"
                        },
"mb.AddForeignKey(
    name:=""FK_Post_Blog_BlogId1_BlogId2"",
    table:=""Post"",
    columns:={""BlogId1"", ""BlogId2""},
    principalTable:=""Blog"")",
                Sub(o)
                    Assert.Equal("FK_Post_Blog_BlogId1_BlogId2", o.Name)
                    Assert.Equal("Post", o.Table)
                    Assert.Equal({"BlogId1", "BlogId2"}, o.Columns)
                    Assert.Equal("Blog", o.PrincipalTable)
                    Assert.Null(o.PrincipalColumns)
                End Sub)
        End Sub



        <ConditionalFact>
        Public Sub AddForeignKeyOperation_all_args()
            Dim operation = New AddForeignKeyOperation() With
            {
                .Name = "FK_Post_Blog_BlogId",
                .Schema = "dbo",
                .Table = "Post",
                .Columns = {"BlogId"},
                .PrincipalSchema = "my",
                .PrincipalTable = "Blog",
                .PrincipalColumns = {"Id"},
                .OnUpdate = ReferentialAction.Restrict,
                .OnDelete = ReferentialAction.Cascade
            }

            Dim expectedCode =
"mb.AddForeignKey(
    name:=""FK_Post_Blog_BlogId"",
    schema:=""dbo"",
    table:=""Post"",
    column:=""BlogId"",
    principalSchema:=""my"",
    principalTable:=""Blog"",
    principalColumn:=""Id"",
    onUpdate:=ReferentialAction.Restrict,
    onDelete:=ReferentialAction.Cascade)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("FK_Post_Blog_BlogId", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal({"BlogId"}, o.Columns)
                 Assert.Equal("my", o.PrincipalSchema)
                 Assert.Equal("Blog", o.PrincipalTable)
                 Assert.Equal({"Id"}, o.PrincipalColumns)
                 Assert.Equal(ReferentialAction.Restrict, o.OnUpdate)
                 Assert.Equal(ReferentialAction.Cascade, o.OnDelete)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AddForeignKeyOperation_all_args_composite()
            Dim operation = New AddForeignKeyOperation() With
            {
                .Name = "FK_Post_Blog_BlogId1_BlogId2",
                .Schema = "dbo",
                .Table = "Post",
                .Columns = {"BlogId1", "BlogId2"},
                .PrincipalSchema = "my",
                .PrincipalTable = "Blog",
                .PrincipalColumns = {"Id1", "Id2"},
                .OnUpdate = ReferentialAction.Restrict,
                .OnDelete = ReferentialAction.Cascade
            }

            Dim expectedCode =
"mb.AddForeignKey(
    name:=""FK_Post_Blog_BlogId1_BlogId2"",
    schema:=""dbo"",
    table:=""Post"",
    columns:={""BlogId1"", ""BlogId2""},
    principalSchema:=""my"",
    principalTable:=""Blog"",
    principalColumns:={""Id1"", ""Id2""},
    onUpdate:=ReferentialAction.Restrict,
    onDelete:=ReferentialAction.Cascade)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("FK_Post_Blog_BlogId1_BlogId2", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal({"BlogId1", "BlogId2"}, o.Columns)
                 Assert.Equal("my", o.PrincipalSchema)
                 Assert.Equal("Blog", o.PrincipalTable)
                 Assert.Equal({"Id1", "Id2"}, o.PrincipalColumns)
                 Assert.Equal(ReferentialAction.Restrict, o.OnUpdate)
                 Assert.Equal(ReferentialAction.Cascade, o.OnDelete)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AddPrimaryKey_required_args()
            Dim operation = New AddPrimaryKeyOperation() With
            {
                 .Name = "PK_Post",
                 .Table = "Post",
                 .Columns = {"Id"}
            }

            Dim expectedCode =
"mb.AddPrimaryKey(
    name:=""PK_Post"",
    table:=""Post"",
    column:=""Id"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("PK_Post", o.Name)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal({"Id"}, o.Columns)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AddPrimaryKey_all_args()
            Dim operation = New AddPrimaryKeyOperation() With
            {
                 .Name = "PK_Post",
                 .Schema = "dbo",
                 .Table = "Post",
                 .Columns = {"Id"}
            }

            Dim expectedCode =
"mb.AddPrimaryKey(
    name:=""PK_Post"",
    schema:=""dbo"",
    table:=""Post"",
    column:=""Id"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("PK_Post", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal({"Id"}, o.Columns)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AddPrimaryKey_composite()
            Dim operation = New AddPrimaryKeyOperation() With
            {
                 .Name = "PK_Post",
                 .Table = "Post",
                 .Columns = {"Id1", "Id2"}
            }

            Dim expectedCode =
"mb.AddPrimaryKey(
    name:=""PK_Post"",
    table:=""Post"",
    columns:={""Id1"", ""Id2""})"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("PK_Post", o.Name)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal({"Id1", "Id2"}, o.Columns)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AddUniqueConstraint_required_args()

            Dim operation = New AddUniqueConstraintOperation() With
            {
                 .Name = "AK_Post_AltId",
                 .Table = "Post",
                 .Columns = {"AltId"}
            }

            Dim expectedCode =
"mb.AddUniqueConstraint(
    name:=""AK_Post_AltId"",
    table:=""Post"",
    column:=""AltId"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("AK_Post_AltId", o.Name)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal({"AltId"}, o.Columns)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AddUniqueConstraint_all_args()
            Dim operation = New AddUniqueConstraintOperation() With
            {
                 .Name = "AK_Post_AltId",
                 .Schema = "dbo",
                 .Table = "Post",
                 .Columns = {"AltId"}
            }

            Dim expectedCode =
"mb.AddUniqueConstraint(
    name:=""AK_Post_AltId"",
    schema:=""dbo"",
    table:=""Post"",
    column:=""AltId"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("AK_Post_AltId", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal({"AltId"}, o.Columns)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AddUniqueConstraint_composite()
            Dim operation = New AddUniqueConstraintOperation() With
            {
                 .Name = "AK_Post_AltId1_AltId2",
                 .Table = "Post",
                 .Columns = {"AltId1", "AltId2"}
            }

            Dim expectedCode =
"mb.AddUniqueConstraint(
    name:=""AK_Post_AltId1_AltId2"",
    table:=""Post"",
    columns:={""AltId1"", ""AltId2""})"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("AK_Post_AltId1_AltId2", o.Name)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal({"AltId1", "AltId2"}, o.Columns)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AddCheckConstraint_required_args()
            Test(New AddCheckConstraintOperation With
                    {.Name = "CK_Post_AltId1_AltId2",
                      .Table = "Post",
                      .Sql = "AltId1 > AltId2"},
"mb.AddCheckConstraint(
    name:=""CK_Post_AltId1_AltId2"",
    table:=""Post"",
    sql:=""AltId1 > AltId2"")",
        Sub(o)
            Assert.Equal("CK_Post_AltId1_AltId2", o.Name)
            Assert.Equal("Post", o.Table)
            Assert.Equal("AltId1 > AltId2", o.Sql)
        End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AddCheckConstraint_all_args()

            Test(New AddCheckConstraintOperation With
                {.Name = "CK_Post_AltId1_AltId2",
                  .Schema = "dbo",
                  .Table = "Post",
                  .Sql = "AltId1 > AltId2"},
"mb.AddCheckConstraint(
    name:=""CK_Post_AltId1_AltId2"",
    schema:=""dbo"",
    table:=""Post"",
    sql:=""AltId1 > AltId2"")",
        Sub(o)
            Assert.Equal("CK_Post_AltId1_AltId2", o.Name)
            Assert.Equal("dbo", o.Schema)
            Assert.Equal("Post", o.Table)
            Assert.Equal("AltId1 > AltId2", o.Sql)
        End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AlterColumnOperation_required_args()
            Dim operation = New AlterColumnOperation() With
            {
                 .Name = "Id",
                 .Table = "Post",
                 .ClrType = GetType(Integer)
            }

            Dim expectedCode =
"mb.AlterColumn(Of Integer)(
    name:=""Id"",
    table:=""Post"",
    nullable:=False)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Id", o.Name)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal(GetType(Integer), o.ClrType)
                 Assert.Null(o.ColumnType)
                 Assert.Null(o.IsUnicode)
                 Assert.Null(o.IsFixedLength)
                 Assert.Null(o.MaxLength)
                 Assert.Null(o.Precision)
                 Assert.Null(o.Scale)
                 Assert.False(o.IsRowVersion)
                 Assert.False(o.IsNullable)
                 Assert.Null(o.DefaultValue)
                 Assert.Null(o.DefaultValueSql)
                 Assert.Null(o.ComputedColumnSql)
                 Assert.Null(o.Comment)
                 Assert.Null(o.Collation)
                 Assert.Equal(GetType(Integer), o.OldColumn.ClrType)
                 Assert.Null(o.OldColumn.ColumnType)
                 Assert.Null(o.OldColumn.IsUnicode)
                 Assert.Null(o.OldColumn.IsFixedLength)
                 Assert.Null(o.OldColumn.MaxLength)
                 Assert.Null(o.OldColumn.Precision)
                 Assert.Null(o.OldColumn.Scale)
                 Assert.False(o.OldColumn.IsRowVersion)
                 Assert.False(o.OldColumn.IsNullable)
                 Assert.Null(o.OldColumn.DefaultValue)
                 Assert.Null(o.OldColumn.DefaultValueSql)
                 Assert.Null(o.OldColumn.ComputedColumnSql)
                 Assert.Null(o.OldColumn.Comment)
                 Assert.Null(o.OldColumn.Collation)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AlterColumnOperation_all_args()
            Dim operation = New AlterColumnOperation With
            {
                .Name = "Id",
                .Schema = "dbo",
                .Table = "Post",
                .ClrType = GetType(Integer),
                .ColumnType = "int",
                .IsUnicode = False,
                .MaxLength = 30,
                .Precision = 10,
                .Scale = 5,
                .IsRowVersion = True,
                .IsNullable = True,
                .DefaultValue = 1,
                .IsFixedLength = True,
                .Comment = "My Comment 2",
                .Collation = "Some Collation 2",
                .OldColumn = New AlterColumnOperation With {
                                    .ClrType = GetType(String),
                                    .ColumnType = "string",
                                    .IsUnicode = False,
                                    .MaxLength = 20,
                                    .Precision = 5,
                                    .Scale = 1,
                                    .IsRowVersion = True,
                                    .IsNullable = True,
                                    .DefaultValue = 0,
                                    .IsFixedLength = True,
                                    .Comment = "My Comment",
                                    .Collation = "Some Collation"
                                }
            }


            Dim expectedCode =
"mb.AlterColumn(Of Integer)(
    name:=""Id"",
    schema:=""dbo"",
    table:=""Post"",
    type:=""int"",
    unicode:=False,
    fixedLength:=True,
    maxLength:=30,
    precision:=10,
    scale:=5,
    rowVersion:=True,
    nullable:=True,
    defaultValue:=1,
    comment:=""My Comment 2"",
    collation:=""Some Collation 2"",
    oldClrType:=GetType(String),
    oldType:=""string"",
    oldUnicode:=False,
    oldFixedLength:=True,
    oldMaxLength:=20,
    oldPrecision:=5,
    oldScale:=1,
    oldRowVersion:=True,
    oldNullable:=True,
    oldDefaultValue:=0,
    oldComment:=""My Comment"",
    oldCollation:=""Some Collation"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Id", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal(GetType(Integer), o.ClrType)
                 Assert.Equal("int", o.ColumnType)
                 Assert.[False](o.IsUnicode)
                 Assert.[True](o.IsFixedLength)
                 Assert.Equal(30, o.MaxLength)
                 Assert.Equal(10, o.Precision)
                 Assert.Equal(5, o.Scale)
                 Assert.[True](o.IsRowVersion)
                 Assert.[True](o.IsNullable)
                 Assert.Equal(1, o.DefaultValue)
                 Assert.Null(o.DefaultValueSql)
                 Assert.Null(o.ComputedColumnSql)
                 Assert.Equal("My Comment 2", o.Comment)
                 Assert.Equal("Some Collation 2", o.Collation)
                 Assert.Equal(GetType(String), o.OldColumn.ClrType)
                 Assert.Equal("string", o.OldColumn.ColumnType)
                 Assert.[False](o.OldColumn.IsUnicode)
                 Assert.[True](o.OldColumn.IsFixedLength)
                 Assert.Equal(20, o.OldColumn.MaxLength)
                 Assert.Equal(5, o.OldColumn.Precision)
                 Assert.Equal(1, o.OldColumn.Scale)
                 Assert.[True](o.OldColumn.IsRowVersion)
                 Assert.[True](o.OldColumn.IsNullable)
                 Assert.Equal(0, o.OldColumn.DefaultValue)
                 Assert.Null(o.OldColumn.DefaultValueSql)
                 Assert.Null(o.OldColumn.ComputedColumnSql)
                 Assert.Equal("My Comment", o.OldColumn.Comment)
                 Assert.Equal("Some Collation", o.OldColumn.Collation)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AlterColumnOperation_DefaultValueSql()
            Dim operation = New AlterColumnOperation() With
            {
                 .Name = "Id",
                 .Table = "Post",
                 .ClrType = GetType(Integer),
                 .DefaultValueSql = "1"
            }

            Dim expectedCode =
"mb.AlterColumn(Of Integer)(
    name:=""Id"",
    table:=""Post"",
    nullable:=False,
    defaultValueSql:=""1"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Id", o.Name)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal("1", o.DefaultValueSql)
                 Assert.Equal(GetType(Integer), o.ClrType)
                 Assert.Null(o.ColumnType)
                 Assert.Null(o.IsUnicode)
                 Assert.Null(o.IsFixedLength)
                 Assert.Null(o.MaxLength)
                 Assert.False(o.IsRowVersion)
                 Assert.False(o.IsNullable)
                 Assert.Null(o.DefaultValue)
                 Assert.Null(o.ComputedColumnSql)
                 Assert.Equal(GetType(Integer), o.OldColumn.ClrType)
                 Assert.Null(o.OldColumn.ColumnType)
                 Assert.Null(o.OldColumn.IsUnicode)
                 Assert.Null(o.OldColumn.IsFixedLength)
                 Assert.Null(o.OldColumn.MaxLength)
                 Assert.False(o.OldColumn.IsRowVersion)
                 Assert.False(o.OldColumn.IsNullable)
                 Assert.Null(o.OldColumn.DefaultValue)
                 Assert.Null(o.OldColumn.DefaultValueSql)
                 Assert.Null(o.OldColumn.ComputedColumnSql)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AlterColumnOperation_computedColumnSql()
            Dim operation = New AlterColumnOperation() With
            {
                 .Name = "Id",
                 .Table = "Post",
                 .ClrType = GetType(Integer),
                 .ComputedColumnSql = "1",
                 .IsStored = True
            }

            Dim expectedCode =
"mb.AlterColumn(Of Integer)(
    name:=""Id"",
    table:=""Post"",
    nullable:=False,
    computedColumnSql:=""1"",
    stored:=True)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Id", o.Name)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal("1", o.ComputedColumnSql)
                 Assert.[True](o.IsStored)
                 Assert.Equal(GetType(Integer), o.ClrType)
                 Assert.Null(o.ColumnType)
                 Assert.Null(o.IsUnicode)
                 Assert.Null(o.IsFixedLength)
                 Assert.Null(o.MaxLength)
                 Assert.[False](o.IsRowVersion)
                 Assert.[False](o.IsNullable)
                 Assert.Null(o.DefaultValue)
                 Assert.Null(o.DefaultValueSql)
                 Assert.Equal(GetType(Integer), o.OldColumn.ClrType)
                 Assert.Null(o.OldColumn.ColumnType)
                 Assert.Null(o.OldColumn.IsUnicode)
                 Assert.Null(o.OldColumn.IsFixedLength)
                 Assert.Null(o.OldColumn.MaxLength)
                 Assert.Null(o.OldColumn.Precision)
                 Assert.Null(o.OldColumn.Scale)
                 Assert.[False](o.OldColumn.IsRowVersion)
                 Assert.[False](o.OldColumn.IsNullable)
                 Assert.Null(o.OldColumn.DefaultValue)
                 Assert.Null(o.OldColumn.DefaultValueSql)
                 Assert.Null(o.OldColumn.ComputedColumnSql)
                 Assert.Null(o.OldColumn.IsStored)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AlterDatabaseOperation()
            Dim operation = New AlterDatabaseOperation()
            operation.Collation = "Some collation"
            operation("foo") = "bar"
            operation.OldDatabase("bar") = "foo"
            operation.OldDatabase.Collation = "Some other collation"

            Dim expectedCode =
"mb.AlterDatabase(
    collation:=""Some collation"",
    oldCollation:=""Some other collation"").
        Annotation(""foo"", ""bar"").
        OldAnnotation(""bar"", ""foo"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Some collation", o.Collation)
                 Assert.Equal("Some other collation", o.OldDatabase.Collation)
                 Assert.Equal("bar", o("foo"))
                 Assert.Equal("foo", o.OldDatabase("bar"))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AlterDatabaseOperation_with_default_old_collation()
            Test(
                New AlterDatabaseOperation With
                {
                    .Collation = "Some collation"},
"mb.AlterDatabase(
    collation:=""Some collation"")",
                    Sub(o)
                        Assert.Equal("Some collation", o.Collation)
                        Assert.Null(o.OldDatabase.Collation)
                    End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AlterDatabaseOperation_with_default_new_collation()
            Dim Operation = New AlterDatabaseOperation
            Operation.OldDatabase.Collation = "Some collation"

            Test(Of AlterDatabaseOperation)(
                Operation,
"mb.AlterDatabase(
    oldCollation:=""Some collation"")",
                Sub(o)
                    Assert.Null(o.Collation)
                    Assert.Equal("Some collation", o.OldDatabase.Collation)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AlterSequenceOperation_required_args()
            Dim operation = New AlterSequenceOperation() With
            {
            .Name = "EntityFrameworkHiLoSequence"
        }

            Dim expectedCode =
"mb.AlterSequence(
    name:=""EntityFrameworkHiLoSequence"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("EntityFrameworkHiLoSequence", o.Name)
                 Assert.Null(o.Schema)
                 Assert.Equal(1, o.IncrementBy)
                 Assert.Null(o.MinValue)
                 Assert.Null(o.MaxValue)
                 Assert.False(o.IsCyclic)
                 Assert.Equal(1, o.OldSequence.IncrementBy)
                 Assert.Null(o.OldSequence.MinValue)
                 Assert.Null(o.OldSequence.MaxValue)
                 Assert.False(o.OldSequence.IsCyclic)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AlterSequenceOperation_all_args()
            Dim operation As New AlterSequenceOperation With
                {
                    .Name = "EntityFrameworkHiLoSequence",
                    .Schema = "dbo",
                    .IncrementBy = 3,
                    .MinValue = 2,
                    .MaxValue = 4,
                    .IsCyclic = True,
                    .OldSequence = New AlterSequenceOperation With {
                                                                    .IncrementBy = 4,
                                                                    .MinValue = 3,
                                                                    .MaxValue = 5,
                                                                    .IsCyclic = True}
                }


            Dim expectedCode =
"mb.AlterSequence(
    name:=""EntityFrameworkHiLoSequence"",
    schema:=""dbo"",
    incrementBy:=3,
    minValue:=2L,
    maxValue:=4L,
    cyclic:=True,
    oldIncrementBy:=4,
    oldMinValue:=3L,
    oldMaxValue:=5L,
    oldCyclic:=True)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("EntityFrameworkHiLoSequence", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal(3, o.IncrementBy)
                 Assert.Equal(2, o.MinValue)
                 Assert.Equal(4, o.MaxValue)
                 Assert.True(o.IsCyclic)
                 Assert.Equal(4, o.OldSequence.IncrementBy)
                 Assert.Equal(3, o.OldSequence.MinValue)
                 Assert.Equal(5, o.OldSequence.MaxValue)
                 Assert.True(o.OldSequence.IsCyclic)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AlterTableOperation_required_args()
            Dim operation = New AlterTableOperation() With
        {
            .Name = "Customer"
        }

            Dim expectedCode =
"mb.AlterTable(
    name:=""Customer"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Customer", o.Name)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AlterTableOperation_all_args()
            Dim operation = New AlterTableOperation() With
        {
            .Name = "Customer",
            .Schema = "dbo",
            .Comment = "My Comment 2",
            .OldTable = New AlterTableOperation With {.Comment = "My Comment"}
        }

            Dim expectedCode =
"mb.AlterTable(
    name:=""Customer"",
    schema:=""dbo"",
    comment:=""My Comment 2"",
    oldComment:=""My Comment"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Customer", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("My Comment 2", o.Comment)
                 Assert.Equal("My Comment", o.OldTable.Comment)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateIndexOperation_required_args()
            Dim operation = New CreateIndexOperation() With
        {
                 .Name = "IX_Post_Title",
                 .Table = "Post",
                 .Columns = {"Title"}
        }

            Dim expectedCode =
"mb.CreateIndex(
    name:=""IX_Post_Title"",
    table:=""Post"",
    column:=""Title"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("IX_Post_Title", o.Name)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal({"Title"}, o.Columns)
                 Assert.False(o.IsUnique)
                 Assert.Null(o.IsDescending)
                 Assert.Null(o.Filter)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateIndexOperation_all_args()
            Dim operation = New CreateIndexOperation() With
        {
                 .Name = "IX_Post_Title",
                 .Schema = "dbo",
                 .Table = "Post",
                 .Columns = {"Title", "Name"},
                 .IsUnique = True,
                 .IsDescending = {True, False},
                 .Filter = "[Title] IS NOT NULL"
        }

            Dim expectedCode =
"mb.CreateIndex(
    name:=""IX_Post_Title"",
    schema:=""dbo"",
    table:=""Post"",
    columns:={""Title"", ""Name""},
    unique:=True,
    descending:={True, False},
    filter:=""[Title] IS NOT NULL"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("IX_Post_Title", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal({"Title", "Name"}, o.Columns)
                 Assert.True(o.IsUnique)
                 Assert.Equal({True, False}, o.IsDescending)
                 Assert.Equal("[Title] IS NOT NULL", o.Filter)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateIndexOperation_composite()
            Dim operation = New CreateIndexOperation() With
        {
                 .Name = "IX_Post_Title_Subtitle",
                 .Table = "Post",
                 .Columns = {"Title", "Subtitle"}
        }

            Dim expectedCode =
"mb.CreateIndex(
    name:=""IX_Post_Title_Subtitle"",
    table:=""Post"",
    columns:={""Title"", ""Subtitle""})"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("IX_Post_Title_Subtitle", o.Name)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal({"Title", "Subtitle"}, o.Columns)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateSchemaOperation_required_args()
            Dim operation = New EnsureSchemaOperation() With {.Name = "my"}

            Dim expectedCode =
"mb.EnsureSchema(
    name:=""my"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("my", o.Name)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateSequenceOperation_required_args()
            Dim operation = New CreateSequenceOperation() With
        {
            .Name = "EntityFrameworkHiLoSequence",
            .ClrType = GetType(Long)
        }

            Dim expectedCode =
"mb.CreateSequence(
    name:=""EntityFrameworkHiLoSequence"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("EntityFrameworkHiLoSequence", o.Name)
                 Assert.Equal(GetType(Long), o.ClrType)
             End Sub)

        End Sub

        <ConditionalFact>
        Public Sub CreateSequenceOperation_required_args_not_long()
            Dim operation = New CreateSequenceOperation() With
        {
            .Name = "EntityFrameworkHiLoSequence",
            .ClrType = GetType(Integer)
        }

            Dim expectedCode =
"mb.CreateSequence(Of Integer)(
    name:=""EntityFrameworkHiLoSequence"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("EntityFrameworkHiLoSequence", o.Name)
                 Assert.Equal(GetType(Integer), o.ClrType)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateSequenceOperation_all_args()
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
    name:=""EntityFrameworkHiLoSequence"",
    schema:=""dbo"",
    startValue:=3L,
    incrementBy:=5,
    minValue:=2L,
    maxValue:=4L,
    cyclic:=True)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("EntityFrameworkHiLoSequence", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal(GetType(Long), o.ClrType)
                 Assert.Equal(3, o.StartValue)
                 Assert.Equal(5, o.IncrementBy)
                 Assert.Equal(2, o.MinValue)
                 Assert.Equal(4, o.MaxValue)
                 Assert.True(o.IsCyclic)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateSequenceOperation_all_args_not_long()
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
    name:=""EntityFrameworkHiLoSequence"",
    schema:=""dbo"",
    startValue:=3L,
    incrementBy:=5,
    minValue:=2L,
    maxValue:=4L,
    cyclic:=True)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("EntityFrameworkHiLoSequence", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal(GetType(Integer), o.ClrType)
                 Assert.Equal(3, o.StartValue)
                 Assert.Equal(5, o.IncrementBy)
                 Assert.Equal(2, o.MinValue)
                 Assert.Equal(4, o.MaxValue)
                 Assert.True(o.IsCyclic)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_Columns_required_args()
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
    name:=""Post"",
    columns:=Function(table) New With {
        .Id = table.Column(Of Integer)(nullable:=False)
    },
    constraints:=Sub(table)
    End Sub)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Post", o.Name)
                 Assert.Equal(1, o.Columns.Count)

                 Assert.Equal("Id", o.Columns(0).Name)
                 Assert.Equal("Post", o.Columns(0).Table)
                 Assert.Equal(GetType(Integer), o.Columns(0).ClrType)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_Columns_all_args()
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
                        .ColumnType = "int",
                        .IsUnicode = False,
                        .IsFixedLength = True,
                        .MaxLength = 30,
                        .Precision = 20,
                        .Scale = 10,
                        .IsRowVersion = True,
                        .IsNullable = True,
                        .DefaultValue = 1,
                        .Comment = "My Comment",
                        .Collation = "Some Collation"
            })

            Dim expectedCode =
"mb.CreateTable(
    name:=""Post"",
    schema:=""dbo"",
    columns:=Function(table) New With {
        .PostId = table.Column(Of Integer)(name:=""Post Id"", type:=""int"", unicode:=False, fixedLength:=True, maxLength:=30, precision:=20, scale:=10, rowVersion:=True, nullable:=True, defaultValue:=1, comment:=""My Comment"", collation:=""Some Collation"")
    },
    constraints:=Sub(table)
    End Sub)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Post", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.[Single](o.Columns)

                 Assert.Equal("Post Id", o.Columns(0).Name)
                 Assert.Equal("dbo", o.Columns(0).Schema)
                 Assert.Equal("Post", o.Columns(0).Table)
                 Assert.Equal(GetType(Integer), o.Columns(0).ClrType)
                 Assert.Equal("int", o.Columns(0).ColumnType)
                 Assert.[True](o.Columns(0).IsNullable)
                 Assert.[False](o.Columns(0).IsUnicode)
                 Assert.[True](o.Columns(0).IsFixedLength)
                 Assert.Equal(1, o.Columns(0).DefaultValue)
                 Assert.Equal("My Comment", o.Columns(0).Comment)
                 Assert.Equal("Some Collation", o.Columns(0).Collation)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_Columns_DefaultValueSql()
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
    name:=""Post"",
    columns:=Function(table) New With {
        .Id = table.Column(Of Integer)(nullable:=False, defaultValueSql:=""1"")
    },
    constraints:=Sub(table)
    End Sub)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal(1, o.Columns.Count)

                 Assert.Equal("Id", o.Columns(0).Name)
                 Assert.Equal("Post", o.Columns(0).Table)
                 Assert.Equal(GetType(Integer), o.Columns(0).ClrType)
                 Assert.Equal("1", o.Columns(0).DefaultValueSql)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_Columns_computedColumnSql()
            Dim operation = New CreateTableOperation() With {.Name = "Post"}
            operation.Columns.Add(
            New AddColumnOperation() With
            {
                 .Name = "Id",
                 .Table = "Post",
                 .ClrType = GetType(Integer),
                 .ComputedColumnSql = "1",
                 .IsStored = True
            })

            Dim expectedCode =
"mb.CreateTable(
    name:=""Post"",
    columns:=Function(table) New With {
        .Id = table.Column(Of Integer)(nullable:=False, computedColumnSql:=""1"", stored:=True)
    },
    constraints:=Sub(table)
    End Sub)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal(1, o.Columns.Count)

                 Assert.Equal("Id", o.Columns(0).Name)
                 Assert.Equal("Post", o.Columns(0).Table)
                 Assert.Equal(GetType(Integer), o.Columns(0).ClrType)
                 Assert.Equal("1", o.Columns(0).ComputedColumnSql)
                 Assert.True(o.Columns(0).IsStored)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_ForeignKeys_required_args()
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
                 .PrincipalTable = "Blog"
            })

            Dim expectedCode =
"mb.CreateTable(
    name:=""Post"",
    columns:=Function(table) New With {
        .BlogId = table.Column(Of Integer)(nullable:=False)
    },
    constraints:=Sub(table)
        table.ForeignKey(
            name:=""FK_Post_Blog_BlogId"",
            column:=Function(x) x.BlogId,
            principalTable:=""Blog"")
    End Sub)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal(1, o.ForeignKeys.Count)

                 Dim fk = o.ForeignKeys.First()
                 Assert.Equal("FK_Post_Blog_BlogId", fk.Name)
                 Assert.Equal("Post", fk.Table)
                 Assert.Equal({"BlogId"}, fk.Columns.ToArray())
                 Assert.Equal("Blog", fk.PrincipalTable)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_ForeignKeys_all_args()
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
    name:=""Post"",
    schema:=""dbo"",
    columns:=Function(table) New With {
        .BlogId = table.Column(Of Integer)(nullable:=False)
    },
    constraints:=Sub(table)
        table.ForeignKey(
            name:=""FK_Post_Blog_BlogId"",
            column:=Function(x) x.BlogId,
            principalSchema:=""my"",
            principalTable:=""Blog"",
            principalColumn:=""Id"",
            onUpdate:=ReferentialAction.SetNull,
            onDelete:=ReferentialAction.SetDefault)
    End Sub)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Single(o.ForeignKeys)

                 Dim fk = o.ForeignKeys.First()
                 Assert.Equal("Post", fk.Table)
                 Assert.Equal("dbo", fk.Schema)
                 Assert.Equal("FK_Post_Blog_BlogId", fk.Name)
                 Assert.Equal({"BlogId"}, fk.Columns.ToArray())
                 Assert.Equal("Blog", fk.PrincipalTable)
                 Assert.Equal("my", fk.PrincipalSchema)
                 Assert.Equal({"Id"}, fk.PrincipalColumns)
                 Assert.Equal(ReferentialAction.SetNull, fk.OnUpdate)
                 Assert.Equal(ReferentialAction.SetDefault, fk.OnDelete)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_ForeignKeys_composite()
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
    name:=""Post"",
    columns:=Function(table) New With {
        .BlogId1 = table.Column(Of Integer)(nullable:=False),
        .BlogId2 = table.Column(Of Integer)(nullable:=False)
    },
    constraints:=Sub(table)
        table.ForeignKey(
            name:=""FK_Post_Blog_BlogId1_BlogId2"",
            columns:=Function(x) New With {x.BlogId1, x.BlogId2},
            principalTable:=""Blog"",
            principalColumns:={""Id1"", ""Id2""})
    End Sub)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal(1, o.ForeignKeys.Count)

                 Dim fk = o.ForeignKeys.First()
                 Assert.Equal("Post", fk.Table)
                 Assert.Equal({"BlogId1", "BlogId2"}, fk.Columns.ToArray())
                 Assert.Equal("Blog", fk.PrincipalTable)
                 Assert.Equal({"Id1", "Id2"}, fk.PrincipalColumns)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_ForeignKeys_composite_no_principal_columns()

            Dim operation = New CreateTableOperation With {.Name = "Post"}

            operation.Columns.AddRange(
                {
                    New AddColumnOperation With {.Name = "BlogId1", .ClrType = GetType(Integer)},
                    New AddColumnOperation With {.Name = "BlogId2", .ClrType = GetType(Integer)}
                })

            operation.ForeignKeys.Add(New AddForeignKeyOperation With {
                            .Name = "FK_Post_Blog_BlogId1_BlogId2",
                            .Table = "Post",
                            .Columns = {"BlogId1", "BlogId2"},
                            .PrincipalTable = "Blog"
                        })

            Dim expectedCode =
"mb.CreateTable(
    name:=""Post"",
    columns:=Function(table) New With {
        .BlogId1 = table.Column(Of Integer)(nullable:=False),
        .BlogId2 = table.Column(Of Integer)(nullable:=False)
    },
    constraints:=Sub(table)
        table.ForeignKey(
            name:=""FK_Post_Blog_BlogId1_BlogId2"",
            column:=Function(x) New With {x.BlogId1, x.BlogId2},
            principalTable:=""Blog"")
    End Sub)"

            Test(
                operation,
                expectedCode,
                Sub(o)
                    Assert.Single(o.ForeignKeys)

                    Dim fk = o.ForeignKeys.First()
                    Assert.Equal("Post", fk.Table)
                    Assert.Equal({"BlogId1", "BlogId2"}, fk.Columns.ToArray())
                    Assert.Equal("Blog", fk.PrincipalTable)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_PrimaryKey_required_args()
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
    name:=""Post"",
    columns:=Function(table) New With {
        .Id = table.Column(Of Integer)(nullable:=False)
    },
    constraints:=Sub(table)
        table.PrimaryKey(""PK_Post"", Function(x) x.Id)
    End Sub)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.NotNull(o.PrimaryKey)

                 Assert.Equal("PK_Post", o.PrimaryKey.Name)
                 Assert.Equal("Post", o.PrimaryKey.Table)
                 Assert.Equal({"Id"}, o.PrimaryKey.Columns)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_PrimaryKey_all_args()
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
    name:=""Post"",
    schema:=""dbo"",
    columns:=Function(table) New With {
        .Id = table.Column(Of Integer)(nullable:=False)
    },
    constraints:=Sub(table)
        table.PrimaryKey(""PK_Post"", Function(x) x.Id)
    End Sub)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.NotNull(o.PrimaryKey)

                 Assert.Equal("PK_Post", o.PrimaryKey.Name)
                 Assert.Equal("dbo", o.PrimaryKey.Schema)
                 Assert.Equal("Post", o.PrimaryKey.Table)
                 Assert.Equal({"Id"}, o.PrimaryKey.Columns)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_PrimaryKey_composite()
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
    name:=""Post"",
    columns:=Function(table) New With {
        .Id1 = table.Column(Of Integer)(nullable:=False),
        .Id2 = table.Column(Of Integer)(nullable:=False)
    },
    constraints:=Sub(table)
        table.PrimaryKey(""PK_Post"", Function(x) New With {x.Id1, x.Id2})
    End Sub)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.NotNull(o.PrimaryKey)

                 Assert.Equal("PK_Post", o.PrimaryKey.Name)
                 Assert.Equal("Post", o.PrimaryKey.Table)
                 Assert.Equal({"Id1", "Id2"}, o.PrimaryKey.Columns)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_UniqueConstraints_required_args()
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
    name:=""Post"",
    columns:=Function(table) New With {
        .AltId = table.Column(Of Integer)(nullable:=False)
    },
    constraints:=Sub(table)
        table.UniqueConstraint(""AK_Post_AltId"", Function(x) x.AltId)
    End Sub)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Single(o.UniqueConstraints)

                 Assert.Equal("AK_Post_AltId", o.UniqueConstraints(0).Name)
                 Assert.Equal("Post", o.UniqueConstraints(0).Table)
                 Assert.Equal({"AltId"}, o.UniqueConstraints(0).Columns)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_UniqueConstraints_all_args()
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
    name:=""Post"",
    schema:=""dbo"",
    columns:=Function(table) New With {
        .AltId = table.Column(Of Integer)(nullable:=False)
    },
    constraints:=Sub(table)
        table.UniqueConstraint(""AK_Post_AltId"", Function(x) x.AltId)
    End Sub)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal(1, o.UniqueConstraints.Count)

                 Assert.Equal("AK_Post_AltId", o.UniqueConstraints(0).Name)
                 Assert.Equal("dbo", o.UniqueConstraints(0).Schema)
                 Assert.Equal("Post", o.UniqueConstraints(0).Table)
                 Assert.Equal({"AltId"}, o.UniqueConstraints(0).Columns)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_UniqueConstraints_composite()
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
                 .Name = "AK_Post_AltId1_AltId2",
                 .Schema = "dbo",
                 .Table = "Post",
                 .Columns = {"AltId1", "AltId2"}
            })

            Dim expectedCode =
"mb.CreateTable(
    name:=""Post"",
    columns:=Function(table) New With {
        .AltId1 = table.Column(Of Integer)(nullable:=False),
        .AltId2 = table.Column(Of Integer)(nullable:=False)
    },
    constraints:=Sub(table)
        table.UniqueConstraint(""AK_Post_AltId1_AltId2"", Function(x) New With {x.AltId1, x.AltId2})
    End Sub)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal(1, o.UniqueConstraints.Count)

                 Assert.Equal("AK_Post_AltId1_AltId2", o.UniqueConstraints(0).Name)
                 Assert.Equal("Post", o.UniqueConstraints(0).Table)
                 Assert.Equal({"AltId1", "AltId2"}, o.UniqueConstraints(0).Columns)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_CheckConstraints_required_args()

            Dim operation = New CreateTableOperation() With {
                .Name = "Post"
            }

            operation.Columns.Add(New AddColumnOperation With {
                .Name = "AltId1",
                .ClrType = GetType(Integer)})

            operation.Columns.Add(New AddColumnOperation With {
                .Name = "AltId2",
                .ClrType = GetType(Integer)})

            operation.CheckConstraints.Add(New AddCheckConstraintOperation With {
                .Name = "CK_Post_AltId1_AltId2",
                .Table = "Post",
                .Sql = "AltId1 > AltId2"})

            Test(operation,
"mb.CreateTable(
    name:=""Post"",
    columns:=Function(table) New With {
        .AltId1 = table.Column(Of Integer)(nullable:=False),
        .AltId2 = table.Column(Of Integer)(nullable:=False)
    },
    constraints:=Sub(table)
        table.CheckConstraint(""CK_Post_AltId1_AltId2"", ""AltId1 > AltId2"")
    End Sub)",
                Sub(o)
                    Assert.[Single](o.CheckConstraints)

                    Assert.Equal("CK_Post_AltId1_AltId2", o.CheckConstraints(0).Name)
                    Assert.Equal("Post", o.CheckConstraints(0).Table)
                    Assert.Equal("AltId1 > AltId2", o.CheckConstraints(0).Sql)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_ChecksConstraints_all_args()

            Dim operation = New CreateTableOperation() With {
                .Name = "Post",
                .Schema = "dbo"
            }

            operation.Columns.Add(New AddColumnOperation With {
                .Name = "AltId1",
                .ClrType = GetType(Integer)})

            operation.Columns.Add(New AddColumnOperation With {
                .Name = "AltId2",
                .ClrType = GetType(Integer)})

            operation.CheckConstraints.Add(New AddCheckConstraintOperation With {
                .Name = "CK_Post_AltId1_AltId2",
                .Schema = "dbo",
                .Table = "Post",
                .Sql = "AltId1 > AltId2"})

            Test(operation,
"mb.CreateTable(
    name:=""Post"",
    schema:=""dbo"",
    columns:=Function(table) New With {
        .AltId1 = table.Column(Of Integer)(nullable:=False),
        .AltId2 = table.Column(Of Integer)(nullable:=False)
    },
    constraints:=Sub(table)
        table.CheckConstraint(""CK_Post_AltId1_AltId2"", ""AltId1 > AltId2"")
    End Sub)",
            Sub(o)
                Assert.Single(o.CheckConstraints)

                Assert.Equal("CK_Post_AltId1_AltId2", o.CheckConstraints(0).Name)
                Assert.Equal("dbo", o.CheckConstraints(0).Schema)
                Assert.Equal("Post", o.CheckConstraints(0).Table)
                Assert.Equal("AltId1 > AltId2", o.CheckConstraints(0).Sql)
            End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_Comment()

            Dim operation = New CreateTableOperation() With {
                .Name = "Post",
                .Schema = "dbo",
                .Comment = "My Comment"
            }

            operation.Columns.Add(New AddColumnOperation With {
                .Name = "AltId1",
                .ClrType = GetType(Integer)
             })

            Test(operation,
"mb.CreateTable(
    name:=""Post"",
    schema:=""dbo"",
    columns:=Function(table) New With {
        .AltId1 = table.Column(Of Integer)(nullable:=False)
    },
    constraints:=Sub(table)
    End Sub,
    comment:=""My Comment"")",
                Sub(o)
                    Assert.Equal("My Comment", o.Comment)
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub CreateTableOperation_TableComment_ColumnComment()

            Dim operation = New CreateTableOperation() With {
                .Name = "Post",
                .Schema = "dbo",
                .Comment = "My Operation Comment"
            }

            operation.Columns.Add(New AddColumnOperation With {
                .Name = "AltId1",
                .ClrType = GetType(Integer),
                .Comment = "My Column Comment"})

            Test(operation,
"mb.CreateTable(
    name:=""Post"",
    schema:=""dbo"",
    columns:=Function(table) New With {
        .AltId1 = table.Column(Of Integer)(nullable:=False, comment:=""My Column Comment"")
    },
    constraints:=Sub(table)
    End Sub,
    comment:=""My Operation Comment"")",
            Sub(o)
                Assert.Equal("My Operation Comment", o.Comment)
                Assert.Equal("My Column Comment", o.Columns(0).Comment)
            End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropColumnOperation_required_args()
            Dim operation = New DropColumnOperation() With
        {
            .Name = "Id",
            .Table = "Post"
        }

            Dim expectedCode =
"mb.DropColumn(
    name:=""Id"",
    table:=""Post"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Id", o.Name)
                 Assert.Equal("Post", o.Table)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropColumnOperation_all_args()
            Dim operation = New DropColumnOperation() With
        {
            .Name = "Id",
            .Schema = "dbo",
            .Table = "Post"
        }

            Dim expectedCode =
"mb.DropColumn(
    name:=""Id"",
    schema:=""dbo"",
    table:=""Post"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Id", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("Post", o.Table)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropForeignKeyOperation_required_args()
            Dim operation = New DropForeignKeyOperation() With
        {
            .Name = "FK_Post_BlogId",
            .Table = "Post"
        }

            Dim expectedCode =
"mb.DropForeignKey(
    name:=""FK_Post_BlogId"",
    table:=""Post"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("FK_Post_BlogId", o.Name)
                 Assert.Equal("Post", o.Table)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropForeignKeyOperation_all_args()
            Dim operation = New DropForeignKeyOperation() With
        {
            .Name = "FK_Post_BlogId",
            .Schema = "dbo",
            .Table = "Post"
        }

            Dim expectedCode =
"mb.DropForeignKey(
    name:=""FK_Post_BlogId"",
    schema:=""dbo"",
    table:=""Post"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("FK_Post_BlogId", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("Post", o.Table)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropIndexOperation_required_args()
            Dim operation = New DropIndexOperation() With {
            .Name = "IX_Post_Title"
            }

            Dim expectedCode =
"mb.DropIndex(
    name:=""IX_Post_Title"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("IX_Post_Title", o.Name)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropIndexOperation_all_args()
            Dim operation = New DropIndexOperation() With
        {
            .Name = "IX_Post_Title",
            .Schema = "dbo",
            .Table = "Post"
        }

            Dim expectedCode =
"mb.DropIndex(
    name:=""IX_Post_Title"",
    schema:=""dbo"",
    table:=""Post"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("IX_Post_Title", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("Post", o.Table)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropPrimaryKeyOperation_required_args()
            Dim operation = New DropPrimaryKeyOperation() With
        {
            .Name = "PK_Post",
            .Table = "Post"
        }

            Dim expectedCode =
"mb.DropPrimaryKey(
    name:=""PK_Post"",
    table:=""Post"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("PK_Post", o.Name)
                 Assert.Equal("Post", o.Table)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropPrimaryKeyOperation_all_args()
            Dim operation = New DropPrimaryKeyOperation() With
        {
            .Name = "PK_Post",
            .Schema = "dbo",
            .Table = "Post"
        }

            Dim expectedCode =
"mb.DropPrimaryKey(
    name:=""PK_Post"",
    schema:=""dbo"",
    table:=""Post"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("PK_Post", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("Post", o.Table)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropSchemaOperation_required_args()
            Dim operation = New DropSchemaOperation() With {.Name = "my"}

            Dim expectedCode =
"mb.DropSchema(
    name:=""my"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("my", o.Name)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropSequenceOperation_required_args()
            Dim operation = New DropSequenceOperation() With {.Name = "EntityFrameworkHiLoSequence"}

            Dim expectedCode =
"mb.DropSequence(
    name:=""EntityFrameworkHiLoSequence"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("EntityFrameworkHiLoSequence", o.Name)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropSequenceOperation_all_args()
            Dim operation = New DropSequenceOperation() With
        {
            .Name = "EntityFrameworkHiLoSequence",
            .Schema = "dbo"
        }

            Dim expectedCode =
"mb.DropSequence(
    name:=""EntityFrameworkHiLoSequence"",
    schema:=""dbo"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("EntityFrameworkHiLoSequence", o.Name)
                 Assert.Equal("dbo", o.Schema)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropTableOperation_required_args()
            Dim operation = New DropTableOperation() With {.Name = "Post"}

            Dim expectedCode =
"mb.DropTable(
    name:=""Post"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Post", o.Name)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropTableOperation_all_args()
            Dim operation = New DropTableOperation() With
        {
            .Name = "Post",
            .Schema = "dbo"
        }

            Dim expectedCode =
"mb.DropTable(
    name:=""Post"",
    schema:=""dbo"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Post", o.Name)
                 Assert.Equal("dbo", o.Schema)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropUniqueConstraintOperation_required_args()
            Dim operation = New DropUniqueConstraintOperation() With
        {
            .Name = "AK_Post_AltId",
            .Table = "Post"
        }

            Dim expectedCode =
"mb.DropUniqueConstraint(
    name:=""AK_Post_AltId"",
    table:=""Post"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("AK_Post_AltId", o.Name)
                 Assert.Equal("Post", o.Table)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropUniqueConstraintOperation_all_args()
            Dim operation = New DropUniqueConstraintOperation() With
            {
                .Name = "AK_Post_AltId",
                .Schema = "dbo",
                .Table = "Post"
            }

            Dim expectedCode =
"mb.DropUniqueConstraint(
    name:=""AK_Post_AltId"",
    schema:=""dbo"",
    table:=""Post"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("AK_Post_AltId", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("Post", o.Table)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropCheckConstraintOperation_required_args()

            Dim operation = New DropCheckConstraintOperation() With {
                .Name = "CK_Post_AltId1_AltId2",
                .Table = "Post"}

            Test(operation,
"mb.DropCheckConstraint(
    name:=""CK_Post_AltId1_AltId2"",
    table:=""Post"")",
            Sub(o)
                Assert.Equal("CK_Post_AltId1_AltId2", o.Name)
                Assert.Equal("Post", o.Table)
            End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DropCheckConstraintOperation_all_args()

            Dim operation = New DropCheckConstraintOperation() With {
                .Name = "CK_Post_AltId1_AltId2",
                .Schema = "dbo",
                .Table = "Post"}

            Test(operation,
"mb.DropCheckConstraint(
    name:=""CK_Post_AltId1_AltId2"",
    schema:=""dbo"",
    table:=""Post"")",
            Sub(o)
                Assert.Equal("CK_Post_AltId1_AltId2", o.Name)
                Assert.Equal("dbo", o.Schema)
                Assert.Equal("Post", o.Table)
            End Sub)
        End Sub

        <ConditionalFact>
        Public Sub RenameColumnOperation_required_args()
            Dim operation = New RenameColumnOperation() With
        {
            .Name = "Id",
            .Table = "Post",
            .NewName = "PostId"
        }

            Dim expectedCode =
"mb.RenameColumn(
    name:=""Id"",
    table:=""Post"",
    newName:=""PostId"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Id", o.Name)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal("PostId", o.NewName)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub RenameColumnOperation_all_args()
            Dim operation = New RenameColumnOperation() With
        {
            .Name = "Id",
            .Schema = "dbo",
            .Table = "Post",
            .NewName = "PostId"
        }

            Dim expectedCode =
"mb.RenameColumn(
    name:=""Id"",
    schema:=""dbo"",
    table:=""Post"",
    newName:=""PostId"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Id", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal("PostId", o.NewName)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub RenameIndexOperation_required_args()
            Dim operation = New RenameIndexOperation() With
        {
            .Name = "IX_Post_Title",
            .NewName = "IX_Post_PostTitle"
        }

            Dim expectedCode =
"mb.RenameIndex(
    name:=""IX_Post_Title"",
    newName:=""IX_Post_PostTitle"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("IX_Post_Title", o.Name)
                 Assert.Equal("IX_Post_PostTitle", o.NewName)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub RenameIndexOperation_all_args()
            Dim operation = New RenameIndexOperation() With
        {
            .Name = "IX_dbo.Post_Title",
            .Schema = "dbo",
            .Table = "Post",
            .NewName = "IX_dbo.Post_PostTitle"
        }

            Dim expectedCode =
"mb.RenameIndex(
    name:=""IX_dbo.Post_Title"",
    schema:=""dbo"",
    table:=""Post"",
    newName:=""IX_dbo.Post_PostTitle"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("IX_dbo.Post_Title", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("Post", o.Table)
                 Assert.Equal("IX_dbo.Post_PostTitle", o.NewName)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub RenameSequenceOperation_required_args()
            Dim operation = New RenameSequenceOperation() With {.Name = "EntityFrameworkHiLoSequence"}

            Dim expectedCode =
"mb.RenameSequence(
    name:=""EntityFrameworkHiLoSequence"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("EntityFrameworkHiLoSequence", o.Name)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub RenameSequenceOperation_all_args()
            Dim operation = New RenameSequenceOperation() With
        {
            .Name = "EntityFrameworkHiLoSequence",
            .Schema = "dbo",
            .NewName = "MySequence",
            .NewSchema = "my"
        }

            Dim expectedCode =
"mb.RenameSequence(
    name:=""EntityFrameworkHiLoSequence"",
    schema:=""dbo"",
    newName:=""MySequence"",
    newSchema:=""my"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("EntityFrameworkHiLoSequence", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("MySequence", o.NewName)
                 Assert.Equal("my", o.NewSchema)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub RenameTableOperation_required_args()
            Dim operation = New RenameTableOperation() With {.Name = "Post"}

            Dim expectedCode =
"mb.RenameTable(
    name:=""Post"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Post", o.Name)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub RenameTableOperation_all_args()
            Dim operation = New RenameSequenceOperation() With
        {
            .Name = "Post",
            .Schema = "dbo",
            .NewName = "Posts",
            .NewSchema = "my"
        }

            Dim expectedCode =
"mb.RenameSequence(
    name:=""Post"",
    schema:=""dbo"",
    newName:=""Posts"",
    newSchema:=""my"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("Post", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("Posts", o.NewName)
                 Assert.Equal("my", o.NewSchema)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub RestartSequenceOperation_required_args()
            Dim operation = New RestartSequenceOperation() With
        {
            .Name = "EntityFrameworkHiLoSequence",
            .StartValue = 1
        }

            Dim expectedCode =
"mb.RestartSequence(
    name:=""EntityFrameworkHiLoSequence"",
    startValue:=1L)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("EntityFrameworkHiLoSequence", o.Name)
                 Assert.Equal(1, o.StartValue)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub RestartSequenceOperation_all_args()
            Dim operation = New RestartSequenceOperation() With
        {
            .Name = "EntityFrameworkHiLoSequence",
            .Schema = "dbo",
            .StartValue = 1
        }

            Dim expectedCode =
"mb.RestartSequence(
    name:=""EntityFrameworkHiLoSequence"",
    schema:=""dbo"",
    startValue:=1L)"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("EntityFrameworkHiLoSequence", o.Name)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal(1, o.StartValue)
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub SqlOperation_required_args()
            Dim operation = New SqlOperation() With {.Sql = "-- I <3 DDL"}

            Dim expectedCode = "mb.Sql(""-- I <3 DDL"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("-- I <3 DDL", o.Sql)
             End Sub)
        End Sub

        Private Shared ReadOnly _lineString1 As New LineString(
            {New Coordinate(1.1, 2.2), New Coordinate(2.2, 2.2), New Coordinate(2.2, 1.1), New Coordinate(7.1, 7.2)}) With {
                .SRID = 4326}

        Private Shared ReadOnly _lineString2 As New LineString(
            {New Coordinate(7.1, 7.2), New Coordinate(20.2, 20.2), New Coordinate(20.2, 1.1), New Coordinate(70.1, 70.2)}) With {
                .SRID = 4326}

        Private Shared ReadOnly _multiPoint As New MultiPoint(
            {New Point(1.1, 2.2), New Point(2.2, 2.2), New Point(2.2, 1.1)}) With {
                .SRID = 4326}

        Private Shared ReadOnly _polygon1 As New Polygon(
            New LinearRing({New Coordinate(1.1, 2.2), New Coordinate(2.2, 2.2), New Coordinate(2.2, 1.1), New Coordinate(1.1, 2.2)})) With {
                .SRID = 4326}

        Private Shared ReadOnly _polygon2 As New Polygon(
            New LinearRing({New Coordinate(10.1, 20.2), New Coordinate(20.2, 20.2), New Coordinate(20.2, 10.1), New Coordinate(10.1, 20.2)})) With {
                .SRID = 4326}

        Private Shared ReadOnly _point1 As New Point(1.1, 2.2, 3.3) With {
            .SRID = 4326}

        Private Shared ReadOnly _multiLineString As New MultiLineString({_lineString1, _lineString2}) With {
            .SRID = 4326}

        Private Shared ReadOnly _multiPolygon As New MultiPolygon({_polygon2, _polygon1}) With {
            .SRID = 4326}

        Private Shared ReadOnly _geometryCollection As New GeometryCollection(
            New Geometry() {_lineString1, _lineString2, _multiPoint, _polygon1, _polygon2, _point1, _multiLineString, _multiPolygon}) With {
                .SRID = 4326}

        <ConditionalFact>
        Public Sub InsertDataOperation_all_args()

            Dim operation = New InsertDataOperation With {
                .Schema = "dbo",
                .Table = "People",
                .Columns = {"Id", "Full Name", "Geometry"},
                .Values = New Object(,) {
                            {0, Nothing, Nothing},
                            {1, "Daenerys Targaryen", _point1},
                            {2, "John Snow", _polygon1},
                            {3, "Arya Stark", _lineString1},
                            {4, "Harry Strickland", _multiPoint},
                            {5, "The Imp", _multiPolygon},
                            {6, "The Kingslayer", _multiLineString},
                            {7, "Aemon Targaryen", _geometryCollection}
                        }
             }

            Dim expectedCode =
"mb.InsertData(
    schema:=""dbo"",
    table:=""People"",
    columns:={""Id"", ""Full Name"", ""Geometry""},
    values:=New Object(,) {
        {0, Nothing, Nothing},
        {1, ""Daenerys Targaryen"", CType(New NetTopologySuite.IO.WKTReader().Read(""SRID=4326;POINT Z(1.1 2.2 3.3)""), NetTopologySuite.Geometries.Point)},
        {2, ""John Snow"", CType(New NetTopologySuite.IO.WKTReader().Read(""SRID=4326;POLYGON ((1.1 2.2, 2.2 2.2, 2.2 1.1, 1.1 2.2))""), NetTopologySuite.Geometries.Polygon)},
        {3, ""Arya Stark"", CType(New NetTopologySuite.IO.WKTReader().Read(""SRID=4326;LINESTRING (1.1 2.2, 2.2 2.2, 2.2 1.1, 7.1 7.2)""), NetTopologySuite.Geometries.LineString)},
        {4, ""Harry Strickland"", CType(New NetTopologySuite.IO.WKTReader().Read(""SRID=4326;MULTIPOINT ((1.1 2.2), (2.2 2.2), (2.2 1.1))""), NetTopologySuite.Geometries.MultiPoint)},
        {5, ""The Imp"", CType(New NetTopologySuite.IO.WKTReader().Read(""SRID=4326;MULTIPOLYGON (((10.1 20.2, 20.2 20.2, 20.2 10.1, 10.1 20.2)), ((1.1 2.2, 2.2 2.2, 2.2 1.1, 1.1 2.2)))""), NetTopologySuite.Geometries.MultiPolygon)},
        {6, ""The Kingslayer"", CType(New NetTopologySuite.IO.WKTReader().Read(""SRID=4326;MULTILINESTRING ((1.1 2.2, 2.2 2.2, 2.2 1.1, 7.1 7.2), (7.1 7.2, 20.2 20.2, 20.2 1.1, 70.1 70.2))""), NetTopologySuite.Geometries.MultiLineString)},
        {7, ""Aemon Targaryen"", CType(New NetTopologySuite.IO.WKTReader().Read(""SRID=4326;GEOMETRYCOLLECTION Z(LINESTRING Z(1.1 2.2 NaN, 2.2 2.2 NaN, 2.2 1.1 NaN, 7.1 7.2 NaN), LINESTRING Z(7.1 7.2 NaN, 20.2 20.2 NaN, 20.2 1.1 NaN, 70.1 70.2 NaN), MULTIPOINT Z((1.1 2.2 NaN), (2.2 2.2 NaN), (2.2 1.1 NaN)), POLYGON Z((1.1 2.2 NaN, 2.2 2.2 NaN, 2.2 1.1 NaN, 1.1 2.2 NaN)), POLYGON Z((10.1 20.2 NaN, 20.2 20.2 NaN, 20.2 10.1 NaN, 10.1 20.2 NaN)), POINT Z(1.1 2.2 3.3), MULTILINESTRING Z((1.1 2.2 NaN, 2.2 2.2 NaN, 2.2 1.1 NaN, 7.1 7.2 NaN), (7.1 7.2 NaN, 20.2 20.2 NaN, 20.2 1.1 NaN, 70.1 70.2 NaN)), MULTIPOLYGON Z(((10.1 20.2 NaN, 20.2 20.2 NaN, 20.2 10.1 NaN, 10.1 20.2 NaN)), ((1.1 2.2 NaN, 2.2 2.2 NaN, 2.2 1.1 NaN, 1.1 2.2 NaN))))""), NetTopologySuite.Geometries.GeometryCollection)}
    })"

            Test(operation,
             expectedCode,
            Sub(o)
                Assert.Equal("dbo", o.Schema)
                Assert.Equal("People", o.Table)
                Assert.Equal(3, o.Columns.Length)
                Assert.Equal(8, o.Values.GetLength(0))
                Assert.Equal(3, o.Values.GetLength(1))
                Assert.Equal("John Snow", o.Values(2, 1))
                Assert.Equal(_point1, o.Values(1, 2))
                Assert.Equal(_polygon1, o.Values(2, 2))
                Assert.Equal(_lineString1, o.Values(3, 2))
                Assert.Equal(_multiPoint, o.Values(4, 2))
                Assert.Equal(_multiPolygon, o.Values(5, 2))
                Assert.Equal(_multiLineString, o.Values(6, 2))
                Assert.Equal(_geometryCollection, o.Values(7, 2))
            End Sub)
        End Sub

        <ConditionalFact>
        Public Sub InsertDataOperation_required_args()
            Dim operation = New InsertDataOperation() With
            {
                .Table = "People",
                .Columns = {"Geometry"},
                .Values = New Object(,) {{_point1}}
            }

            Dim expectedCode =
"mb.InsertData(
    table:=""People"",
    column:=""Geometry"",
    value:=CType(New NetTopologySuite.IO.WKTReader().Read(""SRID=4326;POINT Z(1.1 2.2 3.3)""), NetTopologySuite.Geometries.Point))"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("People", o.Table)
                 Assert.Single(o.Columns)
                 Assert.Equal(1, o.Values.GetLength(0))
                 Assert.Equal(1, o.Values.GetLength(1))
                 Assert.Equal(_point1, o.Values(0, 0))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub InsertDataOperation_required_empty_array()

            Dim operation = New InsertDataOperation() With
            {
                .Table = "People",
                .Columns = {"Tags"},
                .Values = New Object(,) {{New String() {}}}
            }

            Dim expectedCode =
"mb.InsertData(
    table:=""People"",
    column:=""Tags"",
    value:=New String() {})"

            Test(operation,
                expectedCode,
                Sub(o)
                    Assert.Equal("People", o.Table)
                    Assert.[Single](o.Columns)
                    Assert.Equal(1, o.Values.GetLength(0))
                    Assert.Equal(1, o.Values.GetLength(1))
                    Assert.Equal(New String() {}, CType(o.Values(0, 0), String()))
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub InsertDataOperation_required_empty_array_composite()

            Dim operation = New InsertDataOperation() With
            {
                .Table = "People",
                .Columns = {"First Name", "Last Name", "Geometry"},
                .Values = New Object(,) {{"John", Nothing, New String() {}}}
            }

            Dim expectedCode =
"mb.InsertData(
    table:=""People"",
    columns:={""First Name"", ""Last Name"", ""Geometry""},
    values:=New Object() {""John"", Nothing, New String() {}})"

            Test(operation,
                expectedCode,
               Sub(o)
                   Assert.Equal("People", o.Table)
                   Assert.Equal(3, o.Columns.Length)
                   Assert.Equal(1, o.Values.GetLength(0))
                   Assert.Equal(3, o.Values.GetLength(1))
                   Assert.Null(o.Values(0, 1))
                   Assert.Equal(New String() {}, CType(o.Values(0, 2), String()))
               End Sub)
        End Sub

        <ConditionalFact>
        Public Sub InsertDataOperation_required_args_composite()
            Dim operation = New InsertDataOperation() With {
                .Table = "People",
                .Columns = {"First Name", "Last Name", "Geometry"},
                .Values = New Object(,) {{"John", "Snow", _polygon1}}
                }

            Dim expectedCode =
"mb.InsertData(
    table:=""People"",
    columns:={""First Name"", ""Last Name"", ""Geometry""},
    values:=New Object() {""John"", ""Snow"", CType(New NetTopologySuite.IO.WKTReader().Read(""SRID=4326;POLYGON ((1.1 2.2, 2.2 2.2, 2.2 1.1, 1.1 2.2))""), NetTopologySuite.Geometries.Polygon)})"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("People", o.Table)
                 Assert.Equal(3, o.Columns.Length)
                 Assert.Equal(1, o.Values.GetLength(0))
                 Assert.Equal(3, o.Values.GetLength(1))
                 Assert.Equal("Snow", o.Values(0, 1))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub InsertDataOperation_required_args_multiple_rows()
            Dim operation As New InsertDataOperation With {
                .Table = "People",
                .Columns = {"Geometries"},
                .Values = New Object(,) {
                    {_lineString1},
                    {_multiPoint}}
                }

            Dim expectedCode =
"mb.InsertData(
    table:=""People"",
    column:=""Geometries"",
    values:=New Object() {
        CType(New NetTopologySuite.IO.WKTReader().Read(""SRID=4326;LINESTRING (1.1 2.2, 2.2 2.2, 2.2 1.1, 7.1 7.2)""), NetTopologySuite.Geometries.LineString),
        CType(New NetTopologySuite.IO.WKTReader().Read(""SRID=4326;MULTIPOINT ((1.1 2.2), (2.2 2.2), (2.2 1.1))""), NetTopologySuite.Geometries.MultiPoint)
    })"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("People", o.Table)
                 Assert.Single(o.Columns)
                 Assert.Equal(2, o.Values.GetLength(0))
                 Assert.Equal(1, o.Values.GetLength(1))
                 Assert.Equal(_lineString1, o.Values(0, 0))
                 Assert.Equal(_multiPoint, o.Values(1, 0))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub InsertDataOperation_args_with_linebreaks()

            Dim operation As New InsertDataOperation With {
                .Schema = "dbo",
                .Table = "TestLineBreaks",
                .Columns = {"Id", "Description"},
                .Values = New Object(,) {
                            {0, "Contains" & vbCrLf & "a Windows linebreak"},
                            {1, "Contains a" & vbLf & "Linux linebreak"},
                            {2, "Contains a single Backslash r," & vbCr & "just in case"}
                        }
                }

            Dim expectedCode =
"mb.InsertData(
    schema:=""dbo"",
    table:=""TestLineBreaks"",
    columns:={""Id"", ""Description""},
    values:=New Object(,) {
        {0, ""Contains"" & vbCrLf & ""a Windows linebreak""},
        {1, ""Contains a"" & vbLf & ""Linux linebreak""},
        {2, ""Contains a single Backslash r,"" & vbCr & ""just in case""}
    })"
            Test(operation,
                 expectedCode,
                Sub(o)
                    Assert.Equal("dbo", o.Schema)
                    Assert.Equal("TestLineBreaks", o.Table)
                    Assert.Equal(2, o.Columns.Length)
                    Assert.Equal(3, o.Values.GetLength(0))
                    Assert.Equal(2, o.Values.GetLength(1))
                    Assert.Equal("Contains" & vbCrLf & "a Windows linebreak", o.Values(0, 1))
                    Assert.Equal("Contains a" & vbLf & "Linux linebreak", o.Values(1, 1))
                    Assert.Equal("Contains a single Backslash r," & vbCr & "just in case", o.Values(2, 1))
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DeleteDataOperation_all_args()
            Dim operation = New DeleteDataOperation() With
        {
            .Schema = "dbo",
            .Table = "People",
            .KeyColumns = {"First Name"},
            .KeyColumnTypes = {"string"},
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
    schema:=""dbo"",
    table:=""People"",
    keyColumn:=""First Name"",
    keyColumnType:=""string"",
    keyValues:=New Object() {
        ""Hodor"",
        ""Daenerys"",
        ""John"",
        ""Arya"",
        ""Harry""
    })"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("People", o.Table)
                 Assert.Equal(1, o.KeyColumns.Length)
                 Assert.Equal(5, o.KeyValues.GetLength(0))
                 Assert.Equal(1, o.KeyValues.GetLength(1))
                 Assert.Equal("John", o.KeyValues(2, 0))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DeleteDataOperation_all_args_composite()
            Dim operation = New DeleteDataOperation() With {
                .Table = "People",
                .KeyColumns = {"First Name", "Last Name"},
                .KeyColumnTypes = {"string", "string"},
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
    table:=""People"",
    keyColumns:={""First Name"", ""Last Name""},
    keyColumnTypes:={""string"", ""string""},
    keyValues:=New Object(,) {
        {""Hodor"", Nothing},
        {""Daenerys"", ""Targaryen""},
        {""John"", ""Snow""},
        {""Arya"", ""Stark""},
        {""Harry"", ""Strickland""}
    })"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("People", o.Table)
                 Assert.Equal(2, o.KeyColumns.Length)
                 Assert.Equal(5, o.KeyValues.GetLength(0))
                 Assert.Equal(2, o.KeyValues.GetLength(1))
                 Assert.Equal("Snow", o.KeyValues(2, 1))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DeleteDataOperation_required_args()
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
    table:=""People"",
    keyColumn:=""Last Name"",
    keyValue:=""Snow"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("People", o.Table)
                 Assert.Equal(1, o.KeyColumns.Length)
                 Assert.Equal(1, o.KeyValues.GetLength(0))
                 Assert.Equal(1, o.KeyValues.GetLength(1))
                 Assert.Equal("Snow", o.KeyValues(0, 0))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DeleteDataOperation_required_args_composite()
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
    table:=""People"",
    keyColumns:={""First Name"", ""Last Name""},
    keyValues:=New Object() {""John"", ""Snow""})"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("People", o.Table)
                 Assert.Equal(2, o.KeyColumns.Length)
                 Assert.Equal(1, o.KeyValues.GetLength(0))
                 Assert.Equal(2, o.KeyValues.GetLength(1))
                 Assert.Equal("Snow", o.KeyValues(0, 1))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub DeleteDataOperation_args_with_linebreaks()

            Dim operation = New DeleteDataOperation() With
            {
                .Table = "TestLineBreaks",
                .KeyColumns = {"Id", "Description"},
                .KeyValues = New Object(,) {
                    {0, "Contains" & vbCrLf & "a Windows linebreak"},
                    {1, "Contains a" & vbLf & "Linux linebreak"},
                    {2, "Contains a single Backslash r," & vbCr & "just in case"}
                }
            }

            Dim expectedCode =
"mb.DeleteData(
    table:=""TestLineBreaks"",
    keyColumns:={""Id"", ""Description""},
    keyValues:=New Object(,) {
        {0, ""Contains"" & vbCrLf & ""a Windows linebreak""},
        {1, ""Contains a"" & vbLf & ""Linux linebreak""},
        {2, ""Contains a single Backslash r,"" & vbCr & ""just in case""}
    })"

            Test(operation,
                 expectedCode,
                Sub(o)
                    Assert.Equal("TestLineBreaks", o.Table)
                    Assert.Equal(2, o.KeyColumns.Length)
                    Assert.Equal(3, o.KeyValues.GetLength(0))
                    Assert.Equal(2, o.KeyValues.GetLength(1))
                    Assert.Equal("Contains" & vbCrLf & "a Windows linebreak", o.KeyValues(0, 1))
                    Assert.Equal("Contains a" & vbLf & "Linux linebreak", o.KeyValues(1, 1))
                    Assert.Equal("Contains a single Backslash r," & vbCr & "just in case", o.KeyValues(2, 1))
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub UpdateDataOperation_all_args()
            Dim operation = New UpdateDataOperation() With {
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
    schema:=""dbo"",
    table:=""People"",
    keyColumn:=""First Name"",
    keyValues:=New Object() {
        ""Hodor"",
        ""Daenerys""
    },
    columns:={""Birthplace"", ""House Allegiance"", ""Culture""},
    values:=New Object(,) {
        {""Winterfell"", ""Stark"", ""Northmen""},
        {""Dragonstone"", ""Targaryen"", ""Valyrian""}
    })"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("People", o.Table)
                 Assert.Equal(1, o.KeyColumns.Length)
                 Assert.Equal(2, o.KeyValues.GetLength(0))
                 Assert.Equal(1, o.KeyValues.GetLength(1))
                 Assert.Equal("Daenerys", o.KeyValues(1, 0))
                 Assert.Equal(3, o.Columns.Length)
                 Assert.Equal(2, o.Values.GetLength(0))
                 Assert.Equal(3, o.Values.GetLength(1))
                 Assert.Equal("Targaryen", o.Values(1, 1))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub UpdateDataOperation_all_args_composite()
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
    table:=""People"",
    keyColumns:={""First Name"", ""Last Name""},
    keyValues:=New Object(,) {
        {""Hodor"", Nothing},
        {""Daenerys"", ""Targaryen""}
    },
    column:=""House Allegiance"",
    values:=New Object() {
        ""Stark"",
        ""Targaryen""
    })"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("People", o.Table)
                 Assert.Equal(2, o.KeyColumns.Length)
                 Assert.Equal(2, o.KeyValues.GetLength(0))
                 Assert.Equal(2, o.KeyValues.GetLength(1))
                 Assert.Equal("Daenerys", o.KeyValues(1, 0))
                 Assert.Equal(1, o.Columns.Length)
                 Assert.Equal(2, o.Values.GetLength(0))
                 Assert.Equal(1, o.Values.GetLength(1))
                 Assert.Equal("Targaryen", o.Values(1, 0))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub UpdateDataOperation_all_args_composite_multi()
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
    table:=""People"",
    keyColumns:={""First Name"", ""Last Name""},
    keyValues:=New Object(,) {
        {""Hodor"", Nothing},
        {""Daenerys"", ""Targaryen""}
    },
    columns:={""Birthplace"", ""House Allegiance"", ""Culture""},
    values:=New Object(,) {
        {""Winterfell"", ""Stark"", ""Northmen""},
        {""Dragonstone"", ""Targaryen"", ""Valyrian""}
    })"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("People", o.Table)
                 Assert.Equal(2, o.KeyColumns.Length)
                 Assert.Equal(2, o.KeyValues.GetLength(0))
                 Assert.Equal(2, o.KeyValues.GetLength(1))
                 Assert.Equal("Daenerys", o.KeyValues(1, 0))
                 Assert.Equal(3, o.Columns.Length)
                 Assert.Equal(2, o.Values.GetLength(0))
                 Assert.Equal(3, o.Values.GetLength(1))
                 Assert.Equal("Targaryen", o.Values(1, 1))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub UpdateDataOperation_all_args_multi()
            Dim operation = New UpdateDataOperation With
        {
            .Schema = "dbo",
            .Table = "People",
            .KeyColumns = {"Full Name"},
            .KeyValues =
            {
                {"Daenerys Targaryen"}
            },
            .Columns = {"Birthplace", "House Allegiance", "Culture"},
            .Values =
            {
                {"Dragonstone", "Targaryen", "Valyrian"}
            }
        }

            Dim expectedCode =
"mb.UpdateData(
    schema:=""dbo"",
    table:=""People"",
    keyColumn:=""Full Name"",
    keyValue:=""Daenerys Targaryen"",
    columns:={""Birthplace"", ""House Allegiance"", ""Culture""},
    values:=New Object() {""Dragonstone"", ""Targaryen"", ""Valyrian""})"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("dbo", o.Schema)
                 Assert.Equal("People", o.Table)
                 Assert.Equal(1, o.KeyColumns.Length)
                 Assert.Equal(1, o.KeyValues.GetLength(0))
                 Assert.Equal(1, o.KeyValues.GetLength(1))
                 Assert.Equal("Daenerys Targaryen", o.KeyValues(0, 0))
                 Assert.Equal(3, o.Columns.Length)
                 Assert.Equal(1, o.Values.GetLength(0))
                 Assert.Equal(3, o.Values.GetLength(1))
                 Assert.Equal("Targaryen", o.Values(0, 1))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub UpdateDataOperation_required_args()
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
    table:=""People"",
    keyColumn:=""First Name"",
    keyValue:=""Daenerys"",
    column:=""House Allegiance"",
    value:=""Targaryen"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("People", o.Table)
                 Assert.Equal(1, o.KeyColumns.Length)
                 Assert.Equal(1, o.KeyValues.GetLength(0))
                 Assert.Equal(1, o.KeyValues.GetLength(1))
                 Assert.Equal("Daenerys", o.KeyValues(0, 0))
                 Assert.Equal(1, o.Columns.Length)
                 Assert.Equal(1, o.Values.GetLength(0))
                 Assert.Equal(1, o.Values.GetLength(1))
                 Assert.Equal("Targaryen", o.Values(0, 0))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub UpdateDataOperation_required_args_multiple_rows()
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
    table:=""People"",
    keyColumn:=""First Name"",
    keyValues:=New Object() {
        ""Hodor"",
        ""Daenerys""
    },
    column:=""House Allegiance"",
    values:=New Object() {
        ""Stark"",
        ""Targaryen""
    })"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("People", o.Table)
                 Assert.Single(o.KeyColumns)
                 Assert.Equal(2, o.KeyValues.GetLength(0))
                 Assert.Equal(1, o.KeyValues.GetLength(1))
                 Assert.Equal("Daenerys", o.KeyValues(1, 0))
                 Assert.Single(o.Columns)
                 Assert.Equal(2, o.Values.GetLength(0))
                 Assert.Equal(1, o.Values.GetLength(1))
                 Assert.Equal("Targaryen", o.Values(1, 0))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub UpdateDataOperation_required_args_composite()
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
    table:=""People"",
    keyColumns:={""First Name"", ""Last Name""},
    keyValues:=New Object() {""Daenerys"", ""Targaryen""},
    column:=""House Allegiance"",
    value:=""Targaryen"")"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("People", o.Table)
                 Assert.Equal(2, o.KeyColumns.Length)
                 Assert.Equal(1, o.KeyValues.GetLength(0))
                 Assert.Equal(2, o.KeyValues.GetLength(1))
                 Assert.Equal("Daenerys", o.KeyValues(0, 0))
                 Assert.Single(o.Columns)
                 Assert.Equal(1, o.Values.GetLength(0))
                 Assert.Equal(1, o.Values.GetLength(1))
                 Assert.Equal("Targaryen", o.Values(0, 0))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub UpdateDataOperation_required_args_composite_multi()
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
    table:=""People"",
    keyColumns:={""First Name"", ""Last Name""},
    keyValues:=New Object() {""Daenerys"", ""Targaryen""},
    columns:={""Birthplace"", ""House Allegiance"", ""Culture""},
    values:=New Object() {""Dragonstone"", ""Targaryen"", ""Valyrian""})"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("People", o.Table)
                 Assert.Equal(2, o.KeyColumns.Length)
                 Assert.Equal(1, o.KeyValues.GetLength(0))
                 Assert.Equal(2, o.KeyValues.GetLength(1))
                 Assert.Equal("Daenerys", o.KeyValues(0, 0))
                 Assert.Equal(3, o.Columns.Length)
                 Assert.Equal(1, o.Values.GetLength(0))
                 Assert.Equal(3, o.Values.GetLength(1))
                 Assert.Equal("Targaryen", o.Values(0, 1))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub UpdateDataOperation_required_args_multi()
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
    table:=""People"",
    keyColumn:=""Full Name"",
    keyValue:=""Daenerys Targaryen"",
    columns:={""Birthplace"", ""House Allegiance"", ""Culture""},
    values:=New Object() {""Dragonstone"", ""Targaryen"", ""Valyrian""})"

            Test(operation,
             expectedCode,
             Sub(o)
                 Assert.Equal("People", o.Table)
                 Assert.Equal(1, o.KeyColumns.Length)
                 Assert.Equal(1, o.KeyValues.GetLength(0))
                 Assert.Equal(1, o.KeyValues.GetLength(1))
                 Assert.Equal("Daenerys Targaryen", o.KeyValues(0, 0))
                 Assert.Equal(3, o.Columns.Length)
                 Assert.Equal(1, o.Values.GetLength(0))
                 Assert.Equal(3, o.Values.GetLength(1))
                 Assert.Equal("Targaryen", o.Values(0, 1))
             End Sub)
        End Sub

        <ConditionalFact>
        Public Sub UpdateDataOperation_with_linebreaks()

            Dim operation = New UpdateDataOperation() With
            {
                .Schema = "dbo",
                .Table = "TestLineBreaks",
                .KeyColumns = {"Id"},
                .KeyValues = New Object(,) {{0}, {1}, {2}},
                .Columns = {"Description"},
                .Values = New Object(,) {
                            {"Contains" & vbCrLf & "a Windows linebreak"},
                            {"Contains a" & vbLf & "Linux linebreak"},
                            {"Contains a single Backslash r," & vbCr & "just in case"}
                        }
            }

            Dim expectedCode =
"mb.UpdateData(
    schema:=""dbo"",
    table:=""TestLineBreaks"",
    keyColumn:=""Id"",
    keyValues:=New Object() {
        0,
        1,
        2
    },
    column:=""Description"",
    values:=New Object() {
        ""Contains"" & vbCrLf & ""a Windows linebreak"",
        ""Contains a"" & vbLf & ""Linux linebreak"",
        ""Contains a single Backslash r,"" & vbCr & ""just in case""
    })"

            Test(operation,
             expectedCode,
                Sub(o)
                    Assert.Equal("dbo", o.Schema)
                    Assert.Equal("TestLineBreaks", o.Table)
                    Assert.Single(o.KeyColumns)
                    Assert.Equal(3, o.KeyValues.GetLength(0))
                    Assert.Equal(1, o.KeyValues.GetLength(1))
                    Assert.Single(o.Columns)
                    Assert.Equal(3, o.Values.GetLength(0))
                    Assert.Equal(1, o.Values.GetLength(1))
                    Assert.Equal("Contains" & vbCrLf & "a Windows linebreak", o.Values(0, 0))
                    Assert.Equal("Contains a" & vbLf & "Linux linebreak", o.Values(1, 0))
                    Assert.Equal("Contains a single Backslash r," & vbCr & "just in case", o.Values(2, 0))
                End Sub)
        End Sub

        <ConditionalFact>
        Public Sub AlterTableOperation_annotation_set_to_null()
            Dim oldTable = New CreateTableOperation With {
                .Name = "Customer"
            }

            oldTable.AddAnnotation("MyAnnotation1", "Bar")
            oldTable.AddAnnotation("MyAnnotation2", Nothing)

            Dim alterTable As New AlterTableOperation With {
                .Name = "NewCustomer",
                .OldTable = oldTable
            }

            alterTable.AddAnnotation("MyAnnotation1", Nothing)
            alterTable.AddAnnotation("MyAnnotation2", "Foo")

            Dim expectedCode = <![CDATA[mb.AlterTable(
    name:="NewCustomer").
        Annotation("MyAnnotation1", Nothing).
        Annotation("MyAnnotation2", "Foo").
        OldAnnotation("MyAnnotation1", "Bar").
        OldAnnotation("MyAnnotation2", Nothing)]]>.Value

            Test(
                alterTable,
                expectedCode,
                Sub(o)
                    Assert.Equal("NewCustomer", o.Name)
                    Assert.Null(o.GetAnnotation("MyAnnotation1").Value)
                    Assert.Equal("Foo", o.GetAnnotation("MyAnnotation2").Value)
                End Sub)
        End Sub

        Private Sub Test(Of T As MigrationOperation)(operation As T, expectedCode As String, assertAction As Action(Of T))

            Dim generator As New VisualBasicMigrationOperationGenerator(
                New VisualBasicHelper(
                    New SqlServerTypeMappingSource(
                        TestServiceFactory.Instance.Create(Of TypeMappingSourceDependencies)(),
                        New RelationalTypeMappingSourceDependencies(
                            New IRelationalTypeMappingSourcePlugin() {
                            New SqlServerNetTopologySuiteTypeMappingSourcePlugin(NtsGeometryServices.Instance)
                            }),
                        New SqlServerSingletonOptions())))

            Dim builder = New IndentedStringBuilder()
            generator.Generate("mb", {operation}, builder)
            Dim code = builder.ToString()

            Assert.Equal(expectedCode, code, ignoreLineEndingDifferences:=True)

            Dim build = New BuildSource() With {
                .Sources = New Dictionary(Of String, String) From {
                    {
                        "Migration.vb",
                        "
                            Imports Microsoft.EntityFrameworkCore.Migrations
                            Imports NetTopologySuite.Geometries

                             Public Class OperationsFactory

                                Public Shared Sub Create(mb As MigrationBuilder)
                                    " & code & "
                                End Sub

                             End Class
                        "
                    }
                }
            }

            With build.References
                .Add(BuildReference.ByName("Microsoft.EntityFrameworkCore.Relational"))
                .Add(BuildReference.ByName("NetTopologySuite"))
            End With

            Dim assembly = build.BuildInMemory()
            Dim factoryType = assembly.GetType("OperationsFactory")
            Dim createMethod = factoryType.GetTypeInfo().GetDeclaredMethod("Create")
            Dim mb = New MigrationBuilder(activeProvider:=Nothing)
            createMethod.Invoke(Nothing, {mb})
            Dim result = mb.Operations.Cast(Of T)().Single()

            assertAction(result)
        End Sub

    End Class

End Namespace
