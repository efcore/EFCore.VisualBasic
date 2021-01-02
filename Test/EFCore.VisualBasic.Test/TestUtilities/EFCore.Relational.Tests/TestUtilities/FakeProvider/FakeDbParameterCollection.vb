
Imports System.Collections
Imports System.Collections.Generic
Imports System.Data.Common

Namespace TestUtilities.FakeProvider
    Public Class FakeDbParameterCollection
        Inherits DbParameterCollection

        Private ReadOnly _parameters As New List(Of Object)()
        Public Overrides ReadOnly Property Count As Integer
            Get
                Return _parameters.Count
            End Get
        End Property

        Public Overrides Function Add(value As Object) As Integer
            _parameters.Add(value)

            Return _parameters.Count - 1
        End Function

        Protected Overrides Function GetParameter(index As Integer) As DbParameter
            Return CType(_parameters(index), DbParameter)
        End Function

        Public Overrides Function GetEnumerator() As IEnumerator
            Return _parameters.GetEnumerator()
        End Function

        Public Overrides ReadOnly Property SyncRoot As Object
            Get
                Throw New NotImplementedException
            End Get
        End Property

        Public Overrides Sub AddRange(values As Array)
            Throw New NotImplementedException
        End Sub

        Public Overrides Sub Clear()
            ' no-op to test that parameters are passed correctly to db command.
        End Sub

        Public Overrides Function Contains(value As String) As Boolean
            Throw New NotImplementedException
        End Function

        Public Overrides Function Contains(value As Object) As Boolean
            Throw New NotImplementedException
        End Function

        Public Overrides Sub CopyTo(array1 As Array, index As Integer)
            Throw New NotImplementedException
        End Sub

        Public Overrides Function IndexOf(parameterName As String) As Integer
            Throw New NotImplementedException
        End Function

        Public Overrides Function IndexOf(value As Object) As Integer
            Throw New NotImplementedException
        End Function

        Public Overrides Sub Insert(index As Integer, value As Object)
            Throw New NotImplementedException
        End Sub

        Public Overrides Sub Remove(value As Object)
            Throw New NotImplementedException
        End Sub

        Public Overrides Sub RemoveAt(parameterName As String)
            Throw New NotImplementedException
        End Sub

        Public Overrides Sub RemoveAt(index As Integer)
            Throw New NotImplementedException
        End Sub

        Protected Overrides Function GetParameter(parameterName As String) As DbParameter
            Throw New NotImplementedException
        End Function

        Protected Overrides Sub SetParameter(parameterName As String, value As DbParameter)
            Throw New NotImplementedException
        End Sub

        Protected Overrides Sub SetParameter(index As Integer, value As DbParameter)
            Throw New NotImplementedException
        End Sub

    End Class
End Namespace
