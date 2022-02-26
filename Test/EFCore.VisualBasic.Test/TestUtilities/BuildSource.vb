Imports System.IO
Imports System.Reflection
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Public Class BuildSource

    Sub New()
    End Sub

    Sub New(rootNamespace As String)
        _RootNamespace = rootNamespace
    End Sub

    Public Property References As ICollection(Of BuildReference) = New List(Of BuildReference) From
        {
            BuildReference.ByName("System.Runtime"),
            BuildReference.ByName("System.Collections"),
            BuildReference.ByName("System.ComponentModel.Annotations"),
            BuildReference.ByName("System.Data.Common"),
            BuildReference.ByName("System.Linq.Expressions"),
            BuildReference.ByName("System.Text.RegularExpressions")
        }

    Public Property TargetDir As String
    Public Property Sources As New Dictionary(Of String, String)
    Public Property EmitDocumentationDiagnostics As Boolean

    Private _RootNamespace As String = Nothing

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
                assemblyName:=projectName,
                syntaxTrees:=Sources.Select(
                Function(s) SyntaxFactory.ParseSyntaxTree(
                   text:=s.Value,
                   path:=s.Key,
                   options:=New VisualBasicParseOptions(
                        LanguageVersion.Latest,
                        If(EmitDocumentationDiagnostics, DocumentationMode.Diagnose, DocumentationMode.Parse)))),
                references:=refs,
                CreateVisualBasicCompilationOptions())

        Dim targetPath = Path.Combine(If(TargetDir, Path.GetTempPath()), projectName & ".dll")

        Using fStream = File.OpenWrite(targetPath)
            Dim result = compilation.Emit(fStream)
            If Not result.Success Then
                Throw New InvalidOperationException(
                        $"Build failed:
{String.Join(Environment.NewLine, result.Diagnostics)}")
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
                assemblyName:=projectName,
                syntaxTrees:=Sources.Select(
                    Function(s) SyntaxFactory.ParseSyntaxTree(
                    text:=s.Value,
                    path:=s.Key,
                    options:=New VisualBasicParseOptions(
                        LanguageVersion.Latest,
                        If(EmitDocumentationDiagnostics, DocumentationMode.Diagnose, DocumentationMode.Parse)))),
                references:=refs,
                CreateVisualBasicCompilationOptions())

        Dim diagnostics = compilation.GetDiagnostics()
        If Not diagnostics.IsEmpty Then

            Throw New InvalidOperationException(
                $"Build failed.

First diagnostic:
{diagnostics(0)}

Location:
{diagnostics(0).Location.SourceTree?.GetRoot().FindNode(diagnostics(0).Location.SourceSpan)}

All diagnostics:
{String.Join(Environment.NewLine, diagnostics)}")
        End If

        Dim asm As Assembly

        Using memStream = New MemoryStream()
            Dim result = compilation.Emit(memStream)
            If Not result.Success Then
                Throw New InvalidOperationException(
                        $"Failed to emit compilation:
{String.Join(Environment.NewLine, result.Diagnostics)}")
            End If

            asm = Assembly.Load(memStream.ToArray())
        End Using

        Return asm
    End Function

    Private Function CreateVisualBasicCompilationOptions() As VisualBasicCompilationOptions
        Return New VisualBasicCompilationOptions(
                outputKind:=OutputKind.DynamicallyLinkedLibrary,
                rootNamespace:=_RootNamespace,
                globalImports:=GlobalImport.Parse({"Microsoft.VisualBasic",
                                                   "System",
                                                   "System.Collections",
                                                   "System.Collections.Generic",
                                                   "System.Linq"}),
                specificDiagnosticOptions:=New Dictionary(Of String, ReportDiagnostic) From {
 _ ' Unused import statement.
                    {"BC50001", ReportDiagnostic.Suppress},
 _ ' Casing of namespace Namespace does not match casing of namespace Namespace
                    {"BC40055", ReportDiagnostic.Suppress}
                })
    End Function

End Class
