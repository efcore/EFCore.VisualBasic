Imports System.CodeDom.Compiler
Imports System.IO
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Design.Internal
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Scaffolding
Imports Microsoft.EntityFrameworkCore.Scaffolding.Internal

Namespace Scaffolding.Internal

    Public Class VisualBasicModelGenerator
        Inherits ModelCodeGenerator

        Private ReadOnly _reporter As IOperationReporter
        Private ReadOnly _serviceProvider As IServiceProvider

        Public Sub New(dependencies As ModelCodeGeneratorDependencies,
                       reporter As IOperationReporter,
                       serviceProvider As IServiceProvider)

            MyBase.New(dependencies)

            _reporter = reporter
            _serviceProvider = serviceProvider
        End Sub

        Private Const FileExtension = ".vb"

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Public Overrides ReadOnly Property Language As String
            Get
                Return "VB"
            End Get
        End Property

        Public Overrides Function GenerateModel(model As IModel,
                                                options As ModelCodeGenerationOptions) As ScaffoldedModel

            NotNull(model, NameOf(model))
            NotNull(options, NameOf(options))

            If options.ContextName Is Nothing Then
                Throw New ArgumentException(CoreStrings.ArgumentPropertyNull(NameOf(options.ContextName), NameOf(options)), NameOf(options))
            End If

            If options.ConnectionString Is Nothing Then
                Throw New ArgumentException(CoreStrings.ArgumentPropertyNull(NameOf(options.ConnectionString), NameOf(options)), NameOf(options))
            End If

            Dim host As New TextTemplatingEngineHost(_serviceProvider)
            host.SetFileExtension(FileExtension)

            Dim contextTemplate As New VisualBasicDbContextGenerator With {
                .Host = host,
                .Session = host.CreateSession()
            }

            With contextTemplate
                .Session.Add("Model", model)
                .Session.Add("Options", options)
                .Session.Add("NamespaceHint", If(options.ContextNamespace, options.ModelNamespace))
                .Session.Add("ProjectDefaultNamespace", options.RootNamespace)
                .Initialize()
            End With

            Dim generatedCode = ProcessTemplate(contextTemplate)

            'output DbContext .vb file
            Dim dbContextFileName = options.ContextName & host.Extension
            Dim resultingFiles As New ScaffoldedModel With {
                .ContextFile = New ScaffoldedFile(
                    path:=If(options.ContextDir IsNot Nothing,
                                Path.Combine(options.ContextDir, dbContextFileName),
                                dbContextFileName),
                    code:=generatedCode
                )
            }

            For Each entityType In model.GetEntityTypes()

                host.Initialize()
                host.SetFileExtension(FileExtension)

                Dim entityTypeTemplate As New VisualBasicEntityTypeGenerator With {
                    .Host = host,
                    .Session = host.CreateSession()
                }

                With entityTypeTemplate
                    .Session.Add("EntityType", entityType)
                    .Session.Add("Options", options)
                    .Session.Add("NamespaceHint", options.ModelNamespace)
                    .Session.Add("ProjectDefaultNamespace", options.RootNamespace)
                    .Initialize()
                End With

                generatedCode = ProcessTemplate(entityTypeTemplate)
                If String.IsNullOrWhiteSpace(generatedCode) Then Continue For

                ' output EntityType poco .vb file
                Dim entityTypeFileName = entityType.Name & host.Extension
                resultingFiles.AdditionalFiles.Add(
                    New ScaffoldedFile(
                        path:=entityTypeFileName,
                        code:=generatedCode
                    ))
            Next

            Return resultingFiles
        End Function

        Private Function ProcessTemplate(transformation As ITextTransformation) As String
            Dim output = transformation.TransformText()

            For Each err As CompilerError In transformation.Errors
                _reporter.Write(err)
            Next

            If transformation.Errors.HasErrors Then
                Throw New OperationException(DesignStrings.ErrorGeneratingOutput(transformation.GetType().Name))
            End If

            Return output
        End Function
    End Class
End Namespace
