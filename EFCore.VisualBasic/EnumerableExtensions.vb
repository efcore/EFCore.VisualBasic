Imports System.Runtime.CompilerServices

Module EnumerableExtensions

    <Extension()>
    Function Join(source As IEnumerable(Of Object),
                   Optional separator As String = ", ") As String
        Return String.Join(separator, source)
    End Function
End Module
