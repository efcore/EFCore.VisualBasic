
Imports System.Data
Imports System.Data.Common
Imports System.Threading

Namespace TestUtilities.FakeProvider
    Public Class FakeDbCommand
        Inherits DbCommand

        Private ReadOnly _commandExecutor As FakeCommandExecutor

        Public Sub New()
        End Sub

        Public Sub New(
            connection As FakeDbConnection,
            commandExecutor As FakeCommandExecutor)
            DbConnection = connection
            _commandExecutor = commandExecutor
        End Sub

        Protected Overrides Property DbConnection As DbConnection

        Protected Overrides Property DbTransaction As DbTransaction
        Public Overrides Sub Cancel()
            Throw New NotImplementedException
        End Sub

        Public Overrides Property CommandText As String
        Public Shared DefaultCommandTimeout As Integer = 30

        Public Overrides Property CommandTimeout As Integer = DefaultCommandTimeout

        Public Overrides Property CommandType As CommandType
        Protected Overrides Function CreateDbParameter() As DbParameter
            Return New FakeDbParameter
        End Function

        Protected Overrides ReadOnly Property DbParameterCollection As DbParameterCollection = New FakeDbParameterCollection
        Public Overrides Sub Prepare()
            Throw New NotImplementedException
        End Sub
        Public Overrides Function ExecuteNonQuery() As Integer
            AssertTransaction()

            Return _commandExecutor.ExecuteNonQuery(Me)
        End Function
        Public Overrides Function ExecuteScalar() As Object
            AssertTransaction()

            Return _commandExecutor.ExecuteScalar(Me)
        End Function
        Protected Overrides Function ExecuteDbDataReader(behavior As CommandBehavior) As DbDataReader
            AssertTransaction()

            Return _commandExecutor.ExecuteReader(Me, behavior)
        End Function
        Public Overrides Function ExecuteNonQueryAsync(cancellationToken1 As CancellationToken) As Task(Of Integer)
            AssertTransaction()

            Return _commandExecutor.ExecuteNonQueryAsync(Me, cancellationToken1)
        End Function
        Public Overrides Function ExecuteScalarAsync(cancellationToken1 As CancellationToken) As Task(Of Object)
            AssertTransaction()

            Return _commandExecutor.ExecuteScalarAsync(Me, cancellationToken1)
        End Function
        Protected Overrides Function ExecuteDbDataReaderAsync(behavior As CommandBehavior, cancellationToken1 As CancellationToken) As Task(Of DbDataReader)
            AssertTransaction()

            Return _commandExecutor.ExecuteReaderAsync(Me, behavior, cancellationToken1)
        End Function
        Public Overrides Property DesignTimeVisible As Boolean
            Get
                Throw New NotImplementedException
            End Get

            Set
                Throw New NotImplementedException
            End Set
        End Property
        Public Overrides Property UpdatedRowSource As UpdateRowSource
            Get
                Throw New NotImplementedException
            End Get

            Set
                Throw New NotImplementedException
            End Set
        End Property

        Private _disposeCount As Integer
        Public Property DisposeCount As Integer
            Get
                Return _disposeCount
            End Get
            Private Set(Value As Integer)
                _disposeCount = Value
            End Set
        End Property
        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                DisposeCount += 1
            End If

            MyBase.Dispose(disposing)
        End Sub
        Private Sub AssertTransaction()

            If Transaction Is Nothing Then
                DebugAssert(CType(DbConnection, FakeDbConnection).ActiveTransaction Is Nothing,
                            "((FakeDbConnection)DbConnection).ActiveTransaction is null")
            Else
                Dim transaction1 = CType(Transaction, FakeDbTransaction)

                DebugAssert(transaction1.Connection Is Connection,
                            "transaction.Connection != Connection")

                DebugAssert(transaction1.DisposeCount = 0,
                            $"transaction.DisposeCount is {transaction1.DisposeCount}")
            End If
        End Sub
    End Class
End Namespace
