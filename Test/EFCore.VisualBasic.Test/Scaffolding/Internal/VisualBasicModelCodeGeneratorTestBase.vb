Imports EntityFrameworkCore.VisualBasic.Design
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design.Internal
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Scaffolding
Imports Microsoft.EntityFrameworkCore.TestUtilities
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

        Dim mb = SqlServerTestHelpers.Instance.CreateConventionBuilder(customServices:=designServices)
        mb.Model.RemoveAnnotation(CoreAnnotationNames.ProductVersion)
        buildModel(mb)

        Dim model = mb.FinalizeModel(designTime:=True, skipValidation:=True)

        Dim services = CreateServices()
        AddScaffoldingServices(services)

        Dim generator = services.BuildServiceProvider().
                                 GetRequiredService(Of IModelCodeGenerator)()

        options.ModelNamespace = If(options.ModelNamespace, "TestNamespace")
        options.ContextNamespace = If(options.ContextNamespace, options.ModelNamespace)
        options.ContextName = "TestDbContext"
        options.ConnectionString = "Initial Catalog=TestDatabase"

        Dim scaffoldedModel = generator.GenerateModel(model, options)

        assertScaffold(scaffoldedModel)

        Dim build As New BuildSource(options.RootNamespace) With {
            .Sources = {scaffoldedModel.ContextFile}.
                        Concat(scaffoldedModel.AdditionalFiles).
                        ToDictionary(Function(f) f.Path, Function(f) f.Code)
        }

        With build.References
            .Add(BuildReference.ByName("Microsoft.EntityFrameworkCore.Abstractions"))
            .Add(BuildReference.ByName("Microsoft.EntityFrameworkCore"))
            .Add(BuildReference.ByName("Microsoft.EntityFrameworkCore.Relational"))
            .Add(BuildReference.ByName("Microsoft.EntityFrameworkCore.SqlServer"))
            .Add(BuildReference.ByName("System.ComponentModel.DataAnnotations"))
            .Add(BuildReference.ByName("System.ComponentModel.Primitives"))
        End With

        If Not skipBuild Then
            Dim assembly = build.BuildInMemory()

            If assertModel IsNot Nothing Then
                Dim dbContextNameSpace = assembly.ExportedTypes.FirstOrDefault(Function(t) t.Name = options.ContextName)?.FullName

                Dim context = CType(assembly.CreateInstance(dbContextNameSpace), DbContext)

                Dim compiledModel = context.GetService(Of IDesignTimeModel)().Model
                assertModel(compiledModel)
            End If
        End If
    End Sub

    Function CreateServices() As IServiceCollection
        Dim testAssembly = GetType(VisualBasicModelCodeGeneratorTestBase).Assembly
        Dim reporter = New TestOperationReporter()
        Dim services = New DesignTimeServicesBuilder(testAssembly, testAssembly, reporter, New String() {}).
            CreateServiceCollection("Microsoft.EntityFrameworkCore.SqlServer")

        Dim vbServices = New EFCoreVisualBasicServices
        vbServices.ConfigureDesignTimeServices(services)

        Return services
    End Function

    Protected Overridable Sub AddModelServices(services As IServiceCollection)
    End Sub

    Protected Overridable Sub AddScaffoldingServices(services As IServiceCollection)
    End Sub

    Protected Shared Sub AssertFileContents(expectedCode As String,
                                            file As ScaffoldedFile)

        Assert.Equal(expectedCode, file.Code, ignoreLineEndingDifferences:=True)
    End Sub
End Class
