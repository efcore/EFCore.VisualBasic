Imports System.Reflection
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text

<Generator(LanguageNames.VisualBasic)>
Public Class VisualBasicRuntimeModelCodeGenerator
    Implements ISourceGenerator

    Private Shared ReadOnly _nl As String = Environment.NewLine

    Public Sub Initialize(context As GeneratorInitializationContext) Implements ISourceGenerator.Initialize
    End Sub

    Public Sub Execute(context As GeneratorExecutionContext) Implements ISourceGenerator.Execute

        Dim CurrentMajorVersion = Assembly.GetExecutingAssembly.GetName.Version.Major

        Dim Providers() As IProvider = {
            New InMemory,
            New Relational,
            New SqlLite,
            New SqlServer
        }

        Dim Pro As New HashSet(Of IProvider)

        For Each Assembly In context.Compilation.ReferencedAssemblyNames

            If Assembly.Version.Major <> CurrentMajorVersion Then Continue For

            For Each Provider In Providers
                If Assembly.Name = Provider.ForProvider Then
                    Pro.Add(Provider)
                End If
            Next
        Next

        For Each Provider In Pro
            If Not String.IsNullOrEmpty(Provider.GetDesignTimeServices(context.Compilation.AssemblyName)) Then
                context.AddSource("EFCoreVisualBasicServices" & Provider.ForProvider.Split("."c).Last,
                                  SourceText.From(Provider.GetDesignTimeServices(context.Compilation.AssemblyName), Encoding.UTF8))
            End If
            context.AddSource(Provider.ForProvider.Split("."c).Last & "VisualBasicRuntimeAnnotationCodeGenerator",
                              SourceText.From(Provider.GetRuntimeAnnotationCodeGenerator(), Encoding.UTF8))
        Next

    End Sub

End Class
