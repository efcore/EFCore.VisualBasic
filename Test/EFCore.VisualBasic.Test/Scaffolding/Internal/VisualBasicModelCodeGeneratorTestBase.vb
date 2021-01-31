Imports Bricelam.EntityFrameworkCore.VisualBasic.Design.Internal
Imports Bricelam.EntityFrameworkCore.VisualBasic.Scaffolding.Internal
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Scaffolding
Imports Microsoft.EntityFrameworkCore.SqlServer.Design.Internal
Imports Microsoft.Extensions.DependencyInjection

Friend Module VisualBasicModelCodeGeneratorTestBase

    Sub Test(
            buildModel As Action(Of ModelBuilder),
            options As ModelCodeGenerationOptions,
            assertScaffold As Action(Of ScaffoldedModel),
            assertModel As Action(Of IModel))

        Dim mb = SqlServerTestHelpers.Instance.CreateConventionBuilder(skipValidation:=True)
        mb.Model.RemoveAnnotation(CoreAnnotationNames.ProductVersion)
        buildModel(mb)

        Call mb.Model.GetEntityTypeErrors()

        Dim model = mb.FinalizeModel()

        Dim services = New ServiceCollection
        services.AddEntityFrameworkDesignTimeServices()
        services.AddSingleton(Of IVisualBasicHelper, VisualBasicHelper)()
        services.AddSingleton(Of IVisualBasicDbContextGenerator, VisualBasicDbContextGenerator)()
        services.AddSingleton(Of IVisualBasicEntityTypeGenerator, VisualBasicEntityTypeGenerator)()
        services.AddSingleton(Of IModelCodeGenerator, VisualBasicModelGenerator)()

        Dim sqlSrvD = New SqlServerDesignTimeServices
        sqlSrvD.ConfigureDesignTimeServices(services)

        Dim generator = services.BuildServiceProvider().GetRequiredService(Of IModelCodeGenerator)()

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
            .Sources = New List(Of String)({scaffoldedModel.ContextFile.Code}.
                                                Concat(scaffoldedModel.
                                                AdditionalFiles.
                                                Select(Function(f) f.Code)))
        }

        Dim assembly = build.BuildInMemory()
        Dim dbContextNameSpace = assembly.ExportedTypes.FirstOrDefault(Function(t) t.Name = options.ContextName)?.FullName

        Dim context = CType(assembly.CreateInstance(dbContextNameSpace), DbContext)

        If assertModel IsNot Nothing Then
            Dim compiledModel = context.Model
            assertModel(compiledModel)
        End If
    End Sub

End Module
