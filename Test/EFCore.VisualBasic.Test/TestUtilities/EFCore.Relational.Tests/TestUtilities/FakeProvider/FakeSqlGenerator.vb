
Imports System.Text
Imports Microsoft.EntityFrameworkCore.Update

Namespace TestUtilities.FakeProvider
    Public Class FakeSqlGenerator
        Inherits UpdateSqlGenerator
        Public Sub New(dependencies As UpdateSqlGeneratorDependencies)
            MyBase.New(dependencies)
        End Sub
        Public Overrides Function AppendInsertOperation(
            commandStringBuilder As StringBuilder,
            command As ModificationCommand,
            commandPosition As Integer) As ResultSetMapping
            AppendInsertOperationCalls += 1
            Return MyBase.AppendInsertOperation(commandStringBuilder, command, commandPosition)
        End Function
        Public Overrides Function AppendUpdateOperation(
            commandStringBuilder As StringBuilder,
            command As ModificationCommand,
            commandPosition As Integer) As ResultSetMapping
            AppendUpdateOperationCalls += 1
            Return MyBase.AppendUpdateOperation(commandStringBuilder, command, commandPosition)
        End Function
        Public Overrides Function AppendDeleteOperation(
            commandStringBuilder As StringBuilder,
            command As ModificationCommand,
            commandPosition As Integer) As ResultSetMapping
            AppendDeleteOperationCalls += 1
            Return MyBase.AppendDeleteOperation(commandStringBuilder, command, commandPosition)
        End Function

        Public Property AppendBatchHeaderCalls As Integer
        Public Property AppendInsertOperationCalls As Integer
        Public Property AppendUpdateOperationCalls As Integer
        Public Property AppendDeleteOperationCalls As Integer
        Public Overrides Sub AppendBatchHeader(commandStringBuilder As StringBuilder)
            AppendBatchHeaderCalls += 1
            MyBase.AppendBatchHeader(commandStringBuilder)
        End Sub
        Protected Overrides Sub AppendIdentityWhereCondition(commandStringBuilder As StringBuilder, columnModification1 As ColumnModification)
            Call commandStringBuilder _
                            .Append(SqlGenerationHelper.DelimitIdentifier(columnModification1.ColumnName)) _
                            .Append(" = ") _
                            .Append("provider_specific_identity()")
        End Sub
        Protected Overrides Function AppendSelectAffectedCountCommand(
            commandStringBuilder As StringBuilder,
            name As String,
            schema As String,
            commandPosition As Integer) As ResultSetMapping
            commandStringBuilder _
                .Append("SELECT provider_specific_rowcount();").Append(Environment.NewLine).Append(Environment.NewLine)

            Return ResultSetMapping.LastInResultSet
        End Function
        Protected Overrides Sub AppendRowsAffectedWhereCondition(commandStringBuilder As StringBuilder, expectedRowsAffected As Integer)
            Call commandStringBuilder _
                            .Append("provider_specific_rowcount() = ").Append(expectedRowsAffected)
        End Sub
    End Class
End Namespace
