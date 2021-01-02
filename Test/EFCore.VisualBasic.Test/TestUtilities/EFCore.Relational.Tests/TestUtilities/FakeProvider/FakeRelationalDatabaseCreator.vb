
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.EntityFrameworkCore.Storage

Namespace TestUtilities.FakeProvider
    Public Class FakeRelationalDatabaseCreator
        Implements IRelationalDatabaseCreator

        Public Sub Create() Implements IRelationalDatabaseCreator.Create
            Throw New NotImplementedException()
        End Sub

        Public Sub Delete() Implements IRelationalDatabaseCreator.Delete
            Throw New NotImplementedException()
        End Sub

        Public Sub CreateTables() Implements IRelationalDatabaseCreator.CreateTables
            Throw New NotImplementedException()
        End Sub

        Public Function Exists() As Boolean Implements IRelationalDatabaseCreator.Exists
            Throw New NotImplementedException()
        End Function

        Public Function ExistsAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of Boolean) Implements IRelationalDatabaseCreator.ExistsAsync
            Throw New NotImplementedException()
        End Function

        Public Function HasTables() As Boolean Implements IRelationalDatabaseCreator.HasTables
            Throw New NotImplementedException()
        End Function

        Public Function HasTablesAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of Boolean) Implements IRelationalDatabaseCreator.HasTablesAsync
            Throw New NotImplementedException()
        End Function

        Public Function CreateAsync(Optional cancellationToken As CancellationToken = Nothing) As Task Implements IRelationalDatabaseCreator.CreateAsync
            Throw New NotImplementedException()
        End Function

        Public Function DeleteAsync(Optional cancellationToken As CancellationToken = Nothing) As Task Implements IRelationalDatabaseCreator.DeleteAsync
            Throw New NotImplementedException()
        End Function

        Public Function CreateTablesAsync(Optional cancellationToken As CancellationToken = Nothing) As Task Implements IRelationalDatabaseCreator.CreateTablesAsync
            Throw New NotImplementedException()
        End Function

        Public Function GenerateCreateScript() As String Implements IRelationalDatabaseCreator.GenerateCreateScript
            Throw New NotImplementedException()
        End Function

        Public Function EnsureDeleted() As Boolean Implements IDatabaseCreator.EnsureDeleted
            Throw New NotImplementedException()
        End Function

        Public Function EnsureDeletedAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of Boolean) Implements IDatabaseCreator.EnsureDeletedAsync
            Throw New NotImplementedException()
        End Function

        Public Function EnsureCreated() As Boolean Implements IDatabaseCreator.EnsureCreated
            Throw New NotImplementedException()
        End Function

        Public Function EnsureCreatedAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of Boolean) Implements IDatabaseCreator.EnsureCreatedAsync
            Throw New NotImplementedException()
        End Function

        Public Function CanConnect() As Boolean Implements IDatabaseCreator.CanConnect
            Throw New NotImplementedException()
        End Function

        Public Function CanConnectAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of Boolean) Implements IDatabaseCreator.CanConnectAsync
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
