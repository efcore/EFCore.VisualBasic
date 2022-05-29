Imports Microsoft.CodeAnalysis
Imports Microsoft.Extensions.DependencyModel
Imports IOPath = System.IO.Path

Public Class BuildReference

    Private Sub New(references As IEnumerable(Of MetadataReference), Optional copyLocal As Boolean = False, Optional path As String = Nothing)
        Me.References = references
        Me.CopyLocal = copyLocal
        Me.Path = path
    End Sub

    Public ReadOnly Property References As IEnumerable(Of MetadataReference)

    Public ReadOnly Property CopyLocal As Boolean
    Public ReadOnly Property Path As String

    Public Shared Function ByName(name As String, Optional copyLocal As Boolean = False) As BuildReference

        Dim references = (From l In DependencyContext.Default.CompileLibraries
                          Where l.Assemblies.Any(Function(a) IOPath.GetFileNameWithoutExtension(a) = name)
                          From r In l.ResolveReferencePaths()
                          Where IOPath.GetFileNameWithoutExtension(r) = name
                          Select MetadataReference.CreateFromFile(r)).ToList()

        If references.Count = 0 Then
            Throw New InvalidOperationException(
                    $"Assembly '{name}' not found." &
                    "You may be missing '<PreserveCompilationContext>true</PreserveCompilationContext>' in your test project's vbproj.")
        End If

        Return New BuildReference(references, copyLocal)
    End Function

    Public Shared Function ByPath(path As String) As BuildReference
        Return New BuildReference({MetadataReference.CreateFromFile(path)}, path:=path)
    End Function
End Class