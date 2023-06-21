Imports EntityFrameworkCore.VisualBasic.Design
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design.Internal
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Scaffolding
Imports Microsoft.EntityFrameworkCore.Scaffolding.Internal
Imports Microsoft.EntityFrameworkCore.Scaffolding.Metadata
Imports Microsoft.EntityFrameworkCore.TestUtilities
Imports Microsoft.Extensions.DependencyInjection
Imports Xunit
Imports Xunit.Abstractions

Namespace Scaffolding.Internal

    <Collection(NameOf(ModelCodeGeneratorTestCollection))>
    Public MustInherit Class VisualBasicModelCodeGeneratorTestBase

        Private ReadOnly _fixture As ModelCodeGeneratorTestFixture
        Private ReadOnly _output As ITestOutputHelper

        Protected Sub New(fixture As ModelCodeGeneratorTestFixture, output As ITestOutputHelper)
            _fixture = fixture
            _output = output
        End Sub

        'Sub Test(buildModel As Action(Of ModelBuilder),
        '         options As ModelCodeGenerationOptions,
        '         assertScaffold As Action(Of ScaffoldedModel),
        '         assertModel As Action(Of IModel),
        '         Optional skipBuild As Boolean = False)

        '    Dim designServices = New ServiceCollection()
        '    AddModelServices(designServices)

        '    Dim services = CreateServices()
        '    AddScaffoldingServices(services)

        '    Dim generators = services.BuildServiceProvider(validateScopes:=True).
        '                              GetServices(Of IModelCodeGenerator)()

        '    options.ModelNamespace = If(options.ModelNamespace, "TestNamespace")
        '    options.ContextNamespace = If(options.ContextNamespace, options.ModelNamespace)
        '    options.ContextName = "TestDbContext"
        '    options.ConnectionString = "Initial Catalog=TestDatabase"
        '    options.ProjectDir = _fixture.ProjectDir

        '    For Each generator In {generators.Last(Function(g) TypeOf g Is VisualBasicModelGenerator),
        '                           generators.Last(Function(g) TypeOf g Is TextTemplatingModelGenerator)}

        '        _output.WriteLine($"Test with {generator.GetType()}")

        '        Test(buildModel, options, assertScaffold, assertModel, generator, designServices, skipBuild)
        '    Next
        'End Sub

        Protected Sub Test(buildModel As Action(Of ModelBuilder),
                           options As ModelCodeGenerationOptions,
                           assertScaffold As Action(Of ScaffoldedModel),
                           assertModel As Action(Of IModel),
                           Optional skipBuild As Boolean = False)

            Dim designServices As New ServiceCollection()
            AddModelServices(designServices)

            Dim modelBuilder = SqlServerTestHelpers.Instance.CreateConventionBuilder(customServices:=designServices)
            buildModel(modelBuilder)

            Dim Model = modelBuilder.FinalizeModel(designTime:=True, skipValidation:=True)

            Dim services = CreateServices()
            AddScaffoldingServices(services)

            Dim serviceProvider = services.BuildServiceProvider(validateScopes:=True)

            Test(serviceProvider, Model, options, assertScaffold, assertModel, skipBuild)
        End Sub

        Protected Sub Test(buildModel As Func(Of IServiceProvider, IModel),
                           options As ModelCodeGenerationOptions,
                           assertScaffold As Action(Of ScaffoldedModel),
                           assertModel As Action(Of IModel),
                           Optional skipBuild As Boolean = False)

            Dim designServices As New ServiceCollection()
            AddModelServices(designServices)
            Dim services = CreateServices()
            AddScaffoldingServices(services)
            Dim serviceProvider = services.BuildServiceProvider(validateScopes:=True)
            Dim Model = buildModel(serviceProvider)

            Test(serviceProvider, Model, options, assertScaffold, assertModel, skipBuild)
        End Sub


        'Private Sub Test(buildModel As Action(Of ModelBuilder),
        '                 options As ModelCodeGenerationOptions,
        '                 assertScaffold As Action(Of ScaffoldedModel),
        '                 assertModel As Action(Of IModel),
        '                 modelGenerator As IModelCodeGenerator,
        '                 designServices As ServiceCollection,
        '                 skipBuild As Boolean)

        '    Dim mb = SqlServerTestHelpers.Instance.CreateConventionBuilder(customServices:=designServices)
        '    mb.Model.RemoveAnnotation(CoreAnnotationNames.ProductVersion)
        '    buildModel(mb)

        '    Dim model = mb.FinalizeModel(designTime:=True, skipValidation:=True)

        '    Dim scaffoldedModel = modelGenerator.GenerateModel(model, options)

        '    assertScaffold(scaffoldedModel)

        '    Dim build As New BuildSource(options.RootNamespace) With {
        '        .Sources = {scaffoldedModel.ContextFile}.
        '                        Concat(scaffoldedModel.AdditionalFiles).
        '                        ToDictionary(Function(f) f.Path, Function(f) f.Code)
        '    }

        '    With build.References
        '        .Add(BuildReference.ByName("Microsoft.EntityFrameworkCore.Abstractions"))
        '        .Add(BuildReference.ByName("Microsoft.EntityFrameworkCore"))
        '        .Add(BuildReference.ByName("Microsoft.EntityFrameworkCore.Relational"))
        '        .Add(BuildReference.ByName("Microsoft.EntityFrameworkCore.SqlServer"))
        '        '.Add(BuildReference.ByName("System.ComponentModel.DataAnnotations"))
        '        '.Add(BuildReference.ByName("System.ComponentModel.Primitives"))
        '    End With

        '    If Not skipBuild Then
        '        Dim assembly = build.BuildInMemory()

        '        If assertModel IsNot Nothing Then
        '            Dim dbContextNameSpace = assembly.ExportedTypes.FirstOrDefault(Function(t) t.Name = options.ContextName)?.FullName

        '            Dim context = CType(assembly.CreateInstance(dbContextNameSpace), DbContext)

        '            Dim compiledModel = context.GetService(Of IDesignTimeModel)().Model
        '            assertModel(compiledModel)
        '        End If
        '    End If
        'End Sub

        Protected Sub Test(serviceProvider As IServiceProvider,
                                model As IModel,
                                options As ModelCodeGenerationOptions,
                                assertScaffold As Action(Of ScaffoldedModel),
                                assertModel As Action(Of IModel),
                                Optional skipBuild As Boolean = False)

            Dim generators = serviceProvider.GetServices(Of IModelCodeGenerator)()

            For Each generator In {generators.Last(Function(g) TypeOf g Is VisualBasicModelGenerator),
                                   generators.Last(Function(g) TypeOf g Is TextTemplatingModelGenerator)}

                options.ModelNamespace = If(options.ModelNamespace, "TestNamespace")
                options.ContextName = "TestDbContext"
                options.ConnectionString = "Initial Catalog=TestDatabase"
                options.ProjectDir = _fixture.ProjectDir

                Dim ScaffoldedModel = generator.GenerateModel(
                model,
                options)
                assertScaffold(ScaffoldedModel)

                Dim build As New BuildSource(options.RootNamespace) With {
                    .Sources = {ScaffoldedModel.ContextFile}.
                                    Concat(ScaffoldedModel.AdditionalFiles).
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

                        Dim contextNamespace = If(options.ContextNamespace, options.ModelNamespace)

                        Dim context = DirectCast(assembly.CreateInstance(
                        If(Not String.IsNullOrEmpty(contextNamespace),
                         contextNamespace & "." & options.ContextName,
                        options.ContextName), ignoreCase:=True), DbContext)

                        Dim compiledModel = context.GetService(Of IDesignTimeModel)().Model
                        assertModel(compiledModel)
                    End If
                End If
            Next
        End Sub

        Protected Shared Function BuildModelWithColumn(storeType As String, sql As String, expected As Object) As DatabaseModel

            Dim dbColumn As New DatabaseColumn() With {
                .Name = "Column",
                .StoreType = storeType,
                .DefaultValueSql = sql,
                .DefaultValue = expected
            }

            Dim dbTable As New DatabaseTable() With {
                .Database = New DatabaseModel(),
                .Name = "Table"
            }
            dbTable.Columns.Add(dbColumn)

            Dim dbModel As New DatabaseModel()
            dbModel.Tables.Add(dbTable)

            Dim Table = dbModel.Tables.Single()
            Table.Database = dbModel
            Table.Columns.Single().Table = Table

            Return dbModel
        End Function

        Function CreateServices() As IServiceCollection
            Dim testAssembly = GetType(VisualBasicModelCodeGeneratorTestBase).Assembly
            Dim reporter = New TestOperationReporter(_output)
            Dim services = New DesignTimeServicesBuilder(testAssembly, testAssembly, reporter, New String() {}).
                CreateServiceCollection("Microsoft.EntityFrameworkCore.SqlServer")

            Dim vbServices As New EFCoreVisualBasicServices
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

        Protected Shared Sub AssertContains(expected As String,
                                            actual As String)

            ' Normalize line endings to Environment.Newline
            expected = expected.Replace(vbCrLf, vbLf).
                                Replace(vbLf & vbCr, vbLf).
                                Replace(vbCr, vbLf).
                                Replace(vbLf, Environment.NewLine)

            Assert.Contains(expected, actual)
        End Sub
    End Class
End Namespace
