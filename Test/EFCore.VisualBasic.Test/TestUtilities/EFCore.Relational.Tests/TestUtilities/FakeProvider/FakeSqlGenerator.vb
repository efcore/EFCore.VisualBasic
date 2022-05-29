
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
            command As IReadOnlyModificationCommand,
            commandPosition As Integer,
            ByRef requiresTransaction As Boolean) As ResultSetMapping
            AppendInsertOperationCalls += 1
            Return MyBase.AppendInsertOperation(commandStringBuilder, command, commandPosition, requiresTransaction)
        End Function

        Public Overrides Function AppendUpdateOperation(
            commandStringBuilder As StringBuilder,
            command As IReadOnlyModificationCommand,
            commandPosition As Integer,
            ByRef requiresTransaction As Boolean) As ResultSetMapping
            AppendUpdateOperationCalls += 1
            Return MyBase.AppendUpdateOperation(commandStringBuilder, command, commandPosition, requiresTransaction)
        End Function

        Public Overrides Function AppendDeleteOperation(
            commandStringBuilder As StringBuilder,
            command As IReadOnlyModificationCommand,
            commandPosition As Integer,
            ByRef requiresTransaction As Boolean) As ResultSetMapping
            AppendDeleteOperationCalls += 1
            Return MyBase.AppendDeleteOperation(commandStringBuilder, command, commandPosition, requiresTransaction)
        End Function

        Public Property AppendBatchHeaderCalls As Integer
        Public Property AppendInsertOperationCalls As Integer
        Public Property AppendUpdateOperationCalls As Integer
        Public Property AppendDeleteOperationCalls As Integer

        Public Overrides Sub AppendBatchHeader(commandStringBuilder As StringBuilder)
            AppendBatchHeaderCalls += 1
            MyBase.AppendBatchHeader(commandStringBuilder)
        End Sub
    End Class
End Namespace
