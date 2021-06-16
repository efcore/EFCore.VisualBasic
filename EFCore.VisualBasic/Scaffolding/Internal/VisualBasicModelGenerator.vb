Imports System.IO
Imports EntityFrameworkCore.VisualBasic.Design
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Scaffolding

Namespace Scaffolding.Internal

    Public Class VisualBasicModelGenerator
        Inherits ModelCodeGenerator

        Private _VBDbContextGenerator As VisualBasicDbContextGenerator
        Private _VBEntityTypeGenerator As VisualBasicEntityTypeGenerator

        Public Sub New(dependencies As ModelCodeGeneratorDependencies,
                       annotationCodeGenerator As IAnnotationCodeGenerator,
                       providerConfigurationCodeGenerator As IProviderConfigurationCodeGenerator,
                       vbHelper As IVisualBasicHelper)

            MyBase.New(dependencies)

            _VBDbContextGenerator = New VisualBasicDbContextGenerator(annotationCodeGenerator,
                                                                      providerConfigurationCodeGenerator,
                                                                      vbHelper)

            _VBEntityTypeGenerator = New VisualBasicEntityTypeGenerator(annotationCodeGenerator,
                                                                        vbHelper)
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

            Dim generatedCode = _VBDbContextGenerator.
                                    WriteCode(
                                        model,
                                        options.ContextName,
                                        options.ConnectionString,
                                        options.RootNamespace,
                                        options.ContextNamespace,
                                        options.ModelNamespace,
                                        options.UseDataAnnotations,
                                        options.SuppressConnectionStringWarning,
                                        options.SuppressOnConfiguring
                                    )

            'output DbContext .vb file
            Dim dbContextFileName = options.ContextName & FileExtension
            Dim resultingFiles As New ScaffoldedModel With {
                .ContextFile = New ScaffoldedFile With {
                    .Path = If(options.ContextDir IsNot Nothing,
                                Path.Combine(options.ContextDir, dbContextFileName),
                                dbContextFileName),
                    .Code = generatedCode
                }
            }

            For Each entityType In model.GetEntityTypes()
                generatedCode = _VBEntityTypeGenerator.
                                    WriteCode(
                                        entityType,
                                        options.RootNamespace,
                                        options.ModelNamespace,
                                        options.UseDataAnnotations
                                    )

                ' output EntityType poco.vb file
                Dim entityTypeFileName = entityType.Name & FileExtension
                resultingFiles.AdditionalFiles.Add(
                    New ScaffoldedFile With {
                        .Path = entityTypeFileName,
                        .Code = generatedCode
                    })
            Next

            Return resultingFiles
        End Function

    End Class

End Namespace