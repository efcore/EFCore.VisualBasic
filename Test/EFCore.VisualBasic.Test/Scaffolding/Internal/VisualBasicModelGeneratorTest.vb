
Imports System.IO
Imports EntityFrameworkCore.VisualBasic.Design
Imports EntityFrameworkCore.VisualBasic.TestUtilities
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Design.Internal
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Microsoft.EntityFrameworkCore.Scaffolding
Imports Microsoft.Extensions.DependencyInjection
Imports Xunit

Namespace Scaffolding.Internal
    Public Class VisualBasicModelGeneratorTest

        <ConditionalFact>
        Public Sub Language_works()
            Dim generator = CreateGenerator()

            Dim result = generator.Language

            Assert.Equal("VB", result)
        End Sub

        <ConditionalFact>
        Public Sub WriteCode_works()
            Dim generator = CreateGenerator()
            Dim modelBuilder = SqlServerTestHelpers.Instance.CreateConventionBuilder()

            modelBuilder.Entity("TestEntity").[Property](Of Integer)("Id").HasAnnotation(ScaffoldingAnnotationNames.ColumnOrdinal, 0)

            Dim result = generator.GenerateModel(
                modelBuilder.FinalizeModel(designTime:=True),
                New ModelCodeGenerationOptions With
                {
                    .ModelNamespace = "TestNamespace",
                    .ContextNamespace = "ContextNameSpace",
                    .ContextDir = Path.Combine("..", "TestContextDir" & Path.DirectorySeparatorChar),
                    .ContextName = "TestContext",
                    .ConnectionString = "Data Source=Test"
                })

            Assert.Equal(Path.Combine("..", "TestContextDir", "TestContext.vb"), result.ContextFile.Path)
            Assert.NotEmpty(result.ContextFile.Code)

            Assert.Equal(1, result.AdditionalFiles.Count)
            Assert.Equal("TestEntity.vb", result.AdditionalFiles(0).Path)
            Assert.NotEmpty(result.AdditionalFiles(0).Code)
        End Sub

        Private Shared Function CreateGenerator() As IModelCodeGenerator
            Dim testAssembly As Reflection.Assembly = GetType(VisualBasicModelGeneratorTest).Assembly
            Dim reporter As New TestOperationReporter

            Dim services = New DesignTimeServicesBuilder(testAssembly, testAssembly, reporter, New String() {}).
                CreateServiceCollection("Microsoft.EntityFrameworkCore.SqlServer").
                AddSingleton(Of IAnnotationCodeGenerator, AnnotationCodeGenerator)().
                AddSingleton(Of IProviderConfigurationCodeGenerator, TestProviderCodeGenerator)()

            Dim vbServices = New EFCoreVisualBasicServices
            vbServices.ConfigureDesignTimeServices(services)

            Return services.
                BuildServiceProvider().
                GetRequiredService(Of IModelCodeGenerator)()
        End Function
    End Class

    Public Class TestProviderCodeGenerator
        Inherits ProviderCodeGenerator
        Public Sub New(dependencies As ProviderCodeGeneratorDependencies)
            MyBase.New(dependencies)
        End Sub
        Public Overrides Function GenerateUseProvider(
        connectionString As String,
        providerOptions As MethodCallCodeFragment) As MethodCallCodeFragment
            Return New MethodCallCodeFragment(
                        "UseTestProvider",
                        If(providerOptions Is Nothing, New Object() {connectionString} _
                            , New Object() {connectionString, New NestedClosureCodeFragment("x", providerOptions)}))
        End Function
    End Class

End Namespace
