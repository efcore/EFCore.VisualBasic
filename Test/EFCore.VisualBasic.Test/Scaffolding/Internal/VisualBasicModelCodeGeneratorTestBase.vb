Imports EntityFrameworkCore.VisualBasic.Design
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Scaffolding
Imports Microsoft.EntityFrameworkCore.SqlServer.Design.Internal
Imports Microsoft.Extensions.DependencyInjection
Imports Xunit

Public MustInherit Class VisualBasicModelCodeGeneratorTestBase

    Sub Test(buildModel As Action(Of ModelBuilder),
             options As ModelCodeGenerationOptions,
             assertScaffold As Action(Of ScaffoldedModel),
             assertModel As Action(Of IModel),
             Optional skipBuild As Boolean = False)

        Dim designServices = New ServiceCollection()
        AddModelServices(designServices)

        Dim mb = SqlServerTestHelpers.Instance.CreateConventionBuilder(skipValidation:=True, customServices:=designServices)

        mb.Model.RemoveAnnotation(CoreAnnotationNames.ProductVersion)
        buildModel(mb)

        Call mb.Model.GetEntityTypeErrors()

        Dim model = mb.FinalizeModel()

        Dim Services = New ServiceCollection()
        Services.AddEntityFrameworkDesignTimeServices()

        Dim vbServices = New EFCoreVisualBasicServices
        vbServices.ConfigureDesignTimeServices(Services)

        With New SqlServerDesignTimeServices()
            .ConfigureDesignTimeServices(Services)
        End With
        AddScaffoldingServices(Services)

        Dim generator = Services.
                            BuildServiceProvider().
                            GetRequiredService(Of IModelCodeGenerator)()

        options.ModelNamespace = If(options.ModelNamespace, "TestNamespace")
        options.ContextNamespace = If(options.ContextNamespace, options.ModelNamespace)
        options.ContextName = "TestDbContext"
        options.ConnectionString = "Initial Catalog=TestDatabase"

        Dim scaffoldedModel = generator.GenerateModel(model, options)

        assertScaffold(scaffoldedModel)

        Dim build As New BuildSource(options.RootNamespace) With {
            .References =
            {
                BuildReference.ByName("Microsoft.VisualBasic.Core"),
                BuildReference.ByName("System.Runtime"),
                BuildReference.ByName("System.Linq.Expressions"),
                BuildReference.ByName("netstandard"),
                BuildReference.ByName("Microsoft.EntityFrameworkCore.Abstractions"),
                BuildReference.ByName("Microsoft.EntityFrameworkCore"),
                BuildReference.ByName("Microsoft.EntityFrameworkCore.Relational"),
                BuildReference.ByName("Microsoft.EntityFrameworkCore.SqlServer"),
                BuildReference.ByName("System.ComponentModel.Annotations"),
                BuildReference.ByName("System.ComponentModel.DataAnnotations"),
                BuildReference.ByName("System.ComponentModel.Primitives"),
                BuildReference.ByName("System.Data.Common"),
                BuildReference.ByName("System.Collections")
            },
            .Sources = {scaffoldedModel.ContextFile}.
                            Concat(scaffoldedModel.AdditionalFiles).
                            Select(Function(f) f.Code).
                            ToList
        }

        If Not skipBuild Then
            Dim assembly = build.BuildInMemory()
            Dim dbContextNameSpace = assembly.ExportedTypes.FirstOrDefault(Function(t) t.Name = options.ContextName)?.FullName

            Dim context = CType(assembly.CreateInstance(dbContextNameSpace), DbContext)

            If assertModel IsNot Nothing Then
                Dim compiledModel = context.Model
                assertModel(compiledModel)
            End If
        End If
    End Sub

    Protected Overridable Sub AddModelServices(services As IServiceCollection)
    End Sub

    Protected Overridable Sub AddScaffoldingServices(services As IServiceCollection)
    End Sub

    Protected Shared Sub AssertFileContents(expectedCode As String,
                                            file As ScaffoldedFile)

        Assert.Equal(expectedCode, file.Code, ignoreLineEndingDifferences:=True)
    End Sub
End Class
