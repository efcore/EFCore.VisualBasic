Imports Microsoft.Data.SqlClient
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports Microsoft.EntityFrameworkCore.Metadata.Conventions
Imports Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure
Imports Microsoft.EntityFrameworkCore.SqlServer.Diagnostics.Internal
Imports Microsoft.EntityFrameworkCore.TestUtilities
Imports Microsoft.Extensions.DependencyInjection

Friend Class SqlServerTestHelpers
    Inherits TestHelpers

    Protected Sub New()
    End Sub

    Public Shared ReadOnly Property Instance As New SqlServerTestHelpers

    Public Overrides Function AddProviderServices(services As IServiceCollection) As IServiceCollection
        Return services.AddEntityFrameworkSqlServer()
    End Function

    Public Overrides Sub UseProviderOptions(optionsBuilder As DbContextOptionsBuilder)
        Call optionsBuilder.UseSqlServer(New SqlConnection("Database=DummyDatabase"))
    End Sub

    Public Overrides ReadOnly Property LoggingDefinitions As LoggingDefinitions = New SqlServerLoggingDefinitions

    'TODO Temporary, will be in TestHelpers in efcore 5.0.9
    Public Overloads Function CreateConventionBuilder(
            Optional skipValidation As Boolean = False,
            Optional customServices As IServiceCollection = Nothing) As ModelBuilder

        customServices = If(customServices, New ServiceCollection())
        Dim contextServices = CreateContextServices(customServices)
        Dim conventionSet = contextServices.GetRequiredService(Of IConventionSetBuilder)().CreateConventionSet()

        If skipValidation Then
            ConventionSet.Remove(conventionSet.ModelFinalizedConventions, GetType(ValidatingConvention))
        End If

        Return New ModelBuilder(conventionSet)
    End Function

End Class
