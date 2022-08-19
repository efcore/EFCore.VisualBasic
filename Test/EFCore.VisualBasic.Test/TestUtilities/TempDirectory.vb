Imports System.IO
Imports System.IO.Path

Namespace TestUtilities
    Public Class TempDirectory
        Implements IDisposable
        Public Sub New()
            Path = Combine(GetTempPath(), GetRandomFileName())
            Directory.CreateDirectory(Path)
        End Sub

        Public ReadOnly Property Path As String

        Public Shared Widening Operator CType(dir As TempDirectory) As String
            Return dir.Path
        End Operator

        Public Sub Dispose() Implements IDisposable.Dispose
            Directory.Delete(Path, recursive:=True)
        End Sub
    End Class
End Namespace
