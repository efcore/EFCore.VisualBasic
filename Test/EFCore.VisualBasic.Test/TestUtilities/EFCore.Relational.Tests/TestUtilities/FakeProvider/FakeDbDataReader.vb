Imports System.Data.Common
Imports System.Threading

Namespace TestUtilities.FakeProvider
    Public Class FakeDbDataReader
        Inherits DbDataReader
        Private ReadOnly _columnNames As String()
        Private _results As IList(Of Object())
        Private ReadOnly _resultSets As IList(Of IList(Of Object()))
        Private _currentResultSet As Integer
        Private _currentRow As Object()
        Private _rowIndex As Integer
        Private _closed As Boolean

        Public Sub New(Optional columnNames As String() = Nothing, Optional results As IList(Of Object()) = Nothing)
            _columnNames = If(columnNames, Array.Empty(Of String)())
            _results = If(results, New List(Of Object()))
            _resultSets = New List(Of IList(Of Object())) From {
                _results}
        End Sub

        Public Sub New(columnNames As String(), resultSets As IList(Of IList(Of Object())))
            _columnNames = If(columnNames, Array.Empty(Of String)())
            _resultSets = If(resultSets, New List(Of IList(Of Object())) From {
                New List(Of Object())})
            _results = _resultSets(0)
        End Sub
        Public Overrides Function Read() As Boolean
            _currentRow = If(_rowIndex < _results.Count, _results(Math.Min(Interlocked.Increment(_rowIndex), _rowIndex - 1)) _
                , Nothing)

            Return _currentRow IsNot Nothing
        End Function

        Private _readAsyncCount As Integer
        Public Property ReadAsyncCount As Integer
            Get
                Return _readAsyncCount
            End Get
            Private Set
                _readAsyncCount = Value
            End Set
        End Property
        Public Overrides Function ReadAsync(cancellationToken1 As CancellationToken) As Task(Of Boolean)
            ReadAsyncCount += 1

            _currentRow = If(_rowIndex < _results.Count, _results(Math.Min(Interlocked.Increment(_rowIndex), _rowIndex - 1)) _
                , Nothing)

            Return Task.FromResult(_currentRow IsNot Nothing)
        End Function

        Private _closeCount As Integer
        Public Property CloseCount As Integer
            Get
                Return _closeCount
            End Get
            Private Set
                _closeCount = Value
            End Set
        End Property
        Public Overrides Sub Close()
            CloseCount += 1
            _closed = True
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

                MyBase.Dispose(True)
            End If

            _closed = True
        End Sub
        Public Overrides ReadOnly Property FieldCount As Integer
            Get
                Return _columnNames.Length
            End Get
        End Property
        Public Overrides Function GetName(ordinal As Integer) As String
            Return _columnNames(ordinal)
        End Function
        Public Overrides Function IsDBNull(ordinal As Integer) As Boolean
            Return _currentRow(ordinal) Is DBNull.Value
        End Function
        Public Overrides Function GetValue(ordinal As Integer) As Object
            Return _currentRow(ordinal)
        End Function

        Private _getInt32Count As Integer
        Public Property GetInt32Count As Integer
            Get
                Return _getInt32Count
            End Get
            Private Set
                _getInt32Count = Value
            End Set
        End Property
        Public Overrides Function GetInt32(ordinal As Integer) As Integer
            GetInt32Count += 1

            Return CInt(Fix(_currentRow(ordinal)))
        End Function
        Default Public Overrides ReadOnly Property item(name As String) As Object
            Get
                Throw New NotImplementedException
            End Get
        End Property
        Default Public Overrides ReadOnly Property item(ordinal As Integer) As Object
            Get
                Throw New NotImplementedException
            End Get
        End Property
        Public Overrides ReadOnly Property Depth As Integer
            Get
                Throw New NotImplementedException
            End Get
        End Property
        Public Overrides ReadOnly Property HasRows As Boolean
            Get
                Return _results.Count <> 0
            End Get
        End Property
        Public Overrides ReadOnly Property IsClosed As Boolean
            Get
                Return _closed
            End Get
        End Property
        Public Overrides ReadOnly Property RecordsAffected As Integer
            Get
                Return _resultSets.Aggregate(0, Function(a, r) a + r.Count)
            End Get
        End Property
        Public Overrides Function GetBoolean(ordinal As Integer) As Boolean
            Return CBool(_currentRow(ordinal))
        End Function
        Public Overrides Function GetByte(ordinal As Integer) As Byte
            Return CByte(_currentRow(ordinal))
        End Function
        Public Overrides Function GetBytes(ordinal As Integer, dataOffset As Long, buffer As Byte(), bufferOffset As Integer, length1 As Integer) As Long
            Throw New NotImplementedException
        End Function
        Public Overrides Function GetChar(ordinal As Integer) As Char
            Return CChar(_currentRow(ordinal))
        End Function
        Public Overrides Function GetChars(ordinal As Integer, dataOffset As Long, buffer As Char(), bufferOffset As Integer, length1 As Integer) As Long
            Throw New NotImplementedException
        End Function
        Public Overrides Function GetDataTypeName(ordinal As Integer) As String
            Return GetFieldType(ordinal).Name
        End Function
        Public Overrides Function GetDateTime(ordinal As Integer) As DateTime
            Return CDate(_currentRow(ordinal))
        End Function
        Public Overrides Function GetDecimal(ordinal As Integer) As Decimal
            Return CDec(_currentRow(ordinal))
        End Function
        Public Overrides Function GetDouble(ordinal As Integer) As Double
            Return CDbl(_currentRow(ordinal))
        End Function
        Public Overrides Function GetEnumerator() As IEnumerator
            Throw New NotImplementedException
        End Function
        Public Overrides Function GetFieldType(ordinal As Integer) As Type
            Return If(_results.Count > 0, If(_results(0)(ordinal)?.[GetType](), GetType(Object)) _
                            , GetType(Object))
        End Function
        Public Overrides Function GetFloat(ordinal As Integer) As Single
            Return CSng(_currentRow(ordinal))
        End Function
        Public Overrides Function GetGuid(ordinal As Integer) As Guid
            Return CType(_currentRow(ordinal), Guid)
        End Function
        Public Overrides Function GetInt16(ordinal As Integer) As Short
            Return CShort(Fix(_currentRow(ordinal)))
        End Function
        Public Overrides Function GetInt64(ordinal As Integer) As Long
            Return CLng(Fix(_currentRow(ordinal)))
        End Function
        Public Overrides Function GetOrdinal(name As String) As Integer
            Throw New NotImplementedException
        End Function
        Public Overrides Function GetString(ordinal As Integer) As String
            Return CStr(_currentRow(ordinal))
        End Function
        Public Overrides Function GetValues(values As Object()) As Integer
            Throw New NotImplementedException
        End Function
        Public Overrides Function NextResult() As Boolean
            Dim hasResult As Boolean = _resultSets.Count > Interlocked.Increment(_currentResultSet)
            If hasResult Then
                _results = _resultSets(_currentResultSet)
            End If

            Return hasResult
        End Function
    End Class
End Namespace
