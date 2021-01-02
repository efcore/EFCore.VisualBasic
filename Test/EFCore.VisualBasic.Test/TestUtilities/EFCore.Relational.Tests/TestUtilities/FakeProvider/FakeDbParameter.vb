
Imports System.Data
Imports System.Data.Common

Namespace TestUtilities.FakeProvider
    Public Class FakeDbParameter
        Inherits DbParameter
        Public Overrides Property ParameterName As String

        Public Overrides Property Value As Object

        Public Overrides Property Direction As ParameterDirection
        Public Shared DefaultIsNullable As Boolean = False
        Public Overrides Property IsNullable As Boolean = DefaultIsNullable
        Public Shared DefaultDbType As DbType = DbType.AnsiString
        Public Overrides Property DbType As DbType = DefaultDbType

        Public Overrides Property Size As Integer
        Public Overrides Property SourceColumn As String
            Get
                Throw New NotImplementedException
            End Get

            Set(Value As String)
                Throw New NotImplementedException
            End Set
        End Property
        Public Overrides Property SourceColumnNullMapping As Boolean
            Get
                Throw New NotImplementedException
            End Get

            Set(Value As Boolean)
                Throw New NotImplementedException
            End Set
        End Property
        Public Overrides Sub ResetDbType()
            Throw New NotImplementedException
        End Sub
    End Class
End Namespace
