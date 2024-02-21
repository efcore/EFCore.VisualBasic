
Imports System.Data
Imports System.Data.Common

Namespace TestUtilities.FakeProvider
    Public Class FakeDbTransaction
        Inherits DbTransaction
        Public Sub New(connection As FakeDbConnection, Optional isolationLevel1 As IsolationLevel = IsolationLevel.Unspecified)
            DbConnection = connection
            Me.IsolationLevel = isolationLevel1
        End Sub

        Protected Overrides ReadOnly Property DbConnection As DbConnection

        Public Overrides ReadOnly Property IsolationLevel As IsolationLevel

        Private _commitCount As Integer
        Public Property CommitCount As Integer
            Get
                Return _commitCount
            End Get
            Private Set
                _commitCount = Value
            End Set
        End Property

        Public Overrides Sub Commit()
            CommitCount += 1
        End Sub

        Private _rollbackCount As Integer
        Public Property RollbackCount As Integer
            Get
                Return _rollbackCount
            End Get
            Private Set
                _rollbackCount = Value
            End Set
        End Property

        Public Overrides Sub Rollback()
            RollbackCount += 1
        End Sub

        Private _disposeCount As Integer
        Public Property DisposeCount As Integer
            Get
                Return _disposeCount
            End Get
            Private Set
                _disposeCount = Value
            End Set
        End Property

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                DisposeCount += 1

                CType(DbConnection, FakeDbConnection).ActiveTransaction = Nothing
            End If

            MyBase.Dispose(disposing)
        End Sub
    End Class
End Namespace
