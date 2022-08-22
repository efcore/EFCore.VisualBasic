Imports System.IO
Imports System.Reflection
Imports EntityFrameworkCore.VisualBasic.Design
Imports EntityFrameworkCore.VisualBasic.TestUtilities
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Design.Internal
Imports Microsoft.EntityFrameworkCore.Scaffolding
Imports Microsoft.EntityFrameworkCore.TestUtilities
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
            Dim modelBuilder = FakeRelationalTestHelpers.Instance.CreateConventionBuilder()

            modelBuilder.Entity("TestEntity").Property(Of Integer)("Id")

            Dim result = generator.GenerateModel(
                modelBuilder.FinalizeModel(designTime:=True),
                New ModelCodeGenerationOptions With {
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
            Dim testAssembly As Assembly = GetType(VisualBasicModelGeneratorTest).Assembly
            Dim reporter As New TestOperationReporter

            Dim services = New DesignTimeServicesBuilder(testAssembly, testAssembly, reporter, New String() {}).
                CreateServiceCollection("Microsoft.EntityFrameworkCore.SqlServer").
                AddSingleton(Of IAnnotationCodeGenerator, AnnotationCodeGenerator)().
                AddSingleton(Of IProviderConfigurationCodeGenerator, TestProviderCodeGenerator)()

            Dim vbServices = New EFCoreVisualBasicServices
            vbServices.ConfigureDesignTimeServices(services)

            Return services.
                BuildServiceProvider(validateScopes:=True).
                GetServices(Of IModelCodeGenerator)().
                Last(Function(g) TypeOf g Is VisualBasicModelGenerator)
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
                        _useTestProviderMethodInfo,
                        If(providerOptions Is Nothing,
                            New Object() {connectionString},
                            New Object() {connectionString, New NestedClosureCodeFragment("x", providerOptions)}))
        End Function

        Private Shared ReadOnly _useTestProviderMethodInfo As MethodInfo = GetRequiredRuntimeMethod(GetType(TestProviderCodeGenerator),
                                                                                                    NameOf(UseTestProvider),
                                                                                                    GetType(DbContextOptionsBuilder),
                                                                                                    GetType(String),
                                                                                                    GetType(Action(Of Object)))

        Public Shared Sub UseTestProvider(optionsBuilder As DbContextOptionsBuilder,
                                          connectionString As String,
                                          Optional optionsAction As Action(Of Object) = Nothing)
            Throw New NotSupportedException()
        End Sub

        Private Shared Function GetRequiredRuntimeMethod(type As Type, name As String, ParamArray parameters As Type()) As MethodInfo
            Dim r = type.GetTypeInfo().GetRuntimeMethod(name, parameters)
            If r Is Nothing Then Throw New InvalidOperationException($"Could Not find method '{name}' on type '{type}'")
            Return r
        End Function
    End Class
End Namespace
