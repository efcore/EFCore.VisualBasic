Imports System.IO
Imports System.Reflection
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Public Class BuildSource

    Public Property References As ICollection(Of BuildReference) = New List(Of BuildReference) From
        {
            BuildReference.ByName("System.Runtime"),
            BuildReference.ByName("netstandard"),
            BuildReference.ByName("System.Collections"),
            BuildReference.ByName("System.ComponentModel.Annotations"),
            BuildReference.ByName("System.Data.Common"),
            BuildReference.ByName("System.Linq.Expressions"),
            BuildReference.ByName("System.Runtime"),
            BuildReference.ByName("System.Text.RegularExpressions")
        }

    Public Property TargetDir As String
    Public Property Sources As ICollection(Of String) = New List(Of String)

    Public Function Build() As BuildFileResult

        Dim projectName = Path.GetRandomFileName()
        Dim refs = New List(Of MetadataReference)

        For Each reference In References
            If reference.CopyLocal Then
                If String.IsNullOrEmpty(reference.Path) Then
                    Throw New InvalidOperationException("Could not find path for reference " & reference.ToString())
                End If
                File.Copy(reference.Path, Path.Combine(TargetDir, Path.GetFileName(reference.Path)), overwrite:=True)
            End If

            refs.AddRange(reference.References)
        Next

        Dim compilation = VisualBasicCompilation.Create(
                projectName,
                Sources.Select(Function(s) SyntaxFactory.ParseSyntaxTree(s)),
                refs,
                New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

        Dim targetPath = Path.Combine(If(TargetDir, Path.GetTempPath()), projectName & ".dll")

        Using fStream = File.OpenWrite(targetPath)
            Dim result = compilation.Emit(fStream)
            If Not result.Success Then
                Throw New InvalidOperationException(
                        $"Build failed. Diagnostics: {String.Join(Environment.NewLine, result.Diagnostics)}")
            End If
        End Using

        Return New BuildFileResult(targetPath)
    End Function

    Public Function BuildInMemory() As Assembly
        Dim projectName = Path.GetRandomFileName()
        Dim refs = New List(Of MetadataReference)

        For Each reference In References
            refs.AddRange(reference.References)
        Next

        Dim compilation = VisualBasicCompilation.Create(
                projectName,
                Sources.Select(Function(s) SyntaxFactory.ParseSyntaxTree(s)),
                refs,
                New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary))

        Dim asm As Assembly

        Using memStream = New MemoryStream()
            Dim result = compilation.Emit(memStream)
            If Not result.Success Then
                Throw New InvalidOperationException(
                        $"Build failed. Diagnostics: {String.Join(Environment.NewLine, result.Diagnostics)}")
            End If

            asm = Assembly.Load(memStream.ToArray())
        End Using

        Return asm
    End Function
End Class