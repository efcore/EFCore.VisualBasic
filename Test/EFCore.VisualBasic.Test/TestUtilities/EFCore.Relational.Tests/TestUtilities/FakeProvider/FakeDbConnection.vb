Imports System.Data
Imports System.Data.Common
Imports System.Threading

Namespace TestUtilities.FakeProvider
    Public Class FakeDbConnection
        Inherits DbConnection
        Private ReadOnly _commandExecutor As FakeCommandExecutor
        Private _state As ConnectionState
        Private ReadOnly _dbCommands As New List(Of FakeDbCommand)()
        Private ReadOnly _dbTransactions As New List(Of FakeDbTransaction)()

        Public Sub New(
            connectionString As String,
            Optional commandExecutor As FakeCommandExecutor = Nothing,
            Optional state As ConnectionState = ConnectionState.Closed)
            Me.ConnectionString = connectionString
            _commandExecutor = If(commandExecutor, New FakeCommandExecutor)
            _state = state
        End Sub
        Public Sub SetState(state As ConnectionState)
            _state = state
        End Sub
        Public Overrides ReadOnly Property State As ConnectionState
            Get
                Return _state
            End Get
        End Property

        Public ReadOnly Property DbCommands As IReadOnlyList(Of FakeDbCommand)
            Get
                Return _dbCommands
            End Get
        End Property

        Public Overrides Property ConnectionString As String

        Public Overrides ReadOnly Property Database As String = "Fake Database"

        Public Overrides ReadOnly Property DataSource As String = "Fake DataSource"
        Public Overrides ReadOnly Property ServerVersion As String
            Get
                Throw New NotImplementedException
            End Get
        End Property
        Public Overrides Sub ChangeDatabase(databaseName As String)
            Throw New NotImplementedException
        End Sub

        Private _openCount As Integer
        Public Property OpenCount As Integer
            Get
                Return _openCount
            End Get
            Private Set(Value As Integer)
                _openCount = Value
            End Set
        End Property
        Public Overrides Sub Open()
            OpenCount += 1
            _state = ConnectionState.Open
        End Sub

        Private _openAsyncCount As Integer
        Public Property OpenAsyncCount As Integer
            Get
                Return _openAsyncCount
            End Get
            Private Set(Value As Integer)
                _openAsyncCount = Value
            End Set
        End Property
        Public Overrides Function OpenAsync(cancellationToken1 As CancellationToken) As Task
            OpenAsyncCount += 1
            Return MyBase.OpenAsync(cancellationToken1)
        End Function

        Private _closeCount As Integer
        Public Property CloseCount As Integer
            Get
                Return _closeCount
            End Get
            Private Set(Value As Integer)
                _closeCount = Value
            End Set
        End Property
        Public Overrides Sub Close()
            CloseCount += 1
            _state = ConnectionState.Closed
        End Sub
        Protected Overrides Function CreateDbCommand() As DbCommand
            Dim command As New FakeDbCommand(Me, _commandExecutor)

            _dbCommands.Add(command)

            Return command
        End Function
        Public ReadOnly Property DbTransactions As IReadOnlyList(Of FakeDbTransaction)
            Get
                Return _dbTransactions
            End Get
        End Property

        Public Property ActiveTransaction As FakeDbTransaction
        Protected Overrides Function BeginDbTransaction(isolationLevel1 As IsolationLevel) As DbTransaction
            ActiveTransaction = New FakeDbTransaction(Me, isolationLevel1)

            _dbTransactions.Add(ActiveTransaction)

            Return ActiveTransaction
        End Function

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
    End Class
End Namespace
