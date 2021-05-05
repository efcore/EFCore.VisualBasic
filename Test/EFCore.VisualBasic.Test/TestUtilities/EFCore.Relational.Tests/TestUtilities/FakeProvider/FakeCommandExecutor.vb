
Imports System.Data
Imports System.Data.Common
Imports System.Threading

Namespace TestUtilities.FakeProvider
    Public Class FakeCommandExecutor
        Private ReadOnly _executeNonQuery As Func(Of FakeDbCommand, Integer)
        Private ReadOnly _executeScalar As Func(Of FakeDbCommand, Object)
        Private ReadOnly _executeReader As Func(Of FakeDbCommand, CommandBehavior, DbDataReader)
        Private ReadOnly _executeNonQueryAsync As Func(Of FakeDbCommand, CancellationToken, Task(Of Integer))
        Private ReadOnly _executeScalarAsync As Func(Of FakeDbCommand, CancellationToken, Task(Of Object))
        Private ReadOnly _executeReaderAsync As Func(Of FakeDbCommand, CommandBehavior, CancellationToken, Task(Of DbDataReader))

        Public Sub New(
            Optional executeNonQuery As Func(Of FakeDbCommand, Integer) = Nothing,
            Optional executeScalar As Func(Of FakeDbCommand, Object) = Nothing,
            Optional executeReader As Func(Of FakeDbCommand, CommandBehavior, DbDataReader) = Nothing,
            Optional executeNonQueryAsync As Func(Of FakeDbCommand, CancellationToken, Task(Of Integer)) = Nothing,
            Optional executeScalarAsync As Func(Of FakeDbCommand, CancellationToken, Task(Of Object)) = Nothing,
            Optional executeReaderAsync As Func(Of FakeDbCommand, CommandBehavior, CancellationToken, Task(Of DbDataReader)) = Nothing)
            _executeNonQuery = If(executeNonQuery, Function(c) -1)

            _executeScalar = If(executeScalar, Function(c) Nothing)

            _executeReader = If(executeReader, Function(c, b) As System.Data.Common.DbDataReader
                                                   Return New FakeDbDataReader
                                               End Function)

            _executeNonQueryAsync = If(executeNonQueryAsync, Function(c, ct) Task.FromResult(-1))

            _executeScalarAsync = If(executeScalarAsync, Function(c, ct) Task.FromResult(Of Object)(Nothing))

            _executeReaderAsync = If(executeReaderAsync, Function(c, ct, b) Task.FromResult(Of DbDataReader)(New FakeDbDataReader))
        End Sub
        Public Overridable Function ExecuteNonQuery(command As FakeDbCommand) As Integer
            Return _executeNonQuery(command)
        End Function
        Public Overridable Function ExecuteScalar(command As FakeDbCommand) As Object
            Return _executeScalar(command)
        End Function
        Public Overridable Function ExecuteReader(command As FakeDbCommand, behavior As CommandBehavior) As DbDataReader
            Return _executeReader(command, behavior)
        End Function
        Public Function ExecuteNonQueryAsync(command As FakeDbCommand, cancellationToken1 As CancellationToken) As Task(Of Integer)
            Return _executeNonQueryAsync(command, cancellationToken1)
        End Function
        Public Function ExecuteScalarAsync(command As FakeDbCommand, cancellationToken1 As CancellationToken) As Task(Of Object)
            Return _executeScalarAsync(command, cancellationToken1)
        End Function
        Public Function ExecuteReaderAsync(command As FakeDbCommand, behavior As CommandBehavior, cancellationToken1 As CancellationToken) As Task(Of DbDataReader)
            Return _executeReaderAsync(command, behavior, cancellationToken1)
        End Function
    End Class
End Namespace
