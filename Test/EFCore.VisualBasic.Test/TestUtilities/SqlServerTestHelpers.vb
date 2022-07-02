Imports Microsoft.Data.SqlClient
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports Microsoft.EntityFrameworkCore.SqlServer.Diagnostics.Internal
Imports Microsoft.EntityFrameworkCore.TestUtilities
Imports Microsoft.Extensions.DependencyInjection

Friend Class SqlServerTestHelpers
    Inherits TestHelpers

    Protected Sub New()
    End Sub

    Public Shared ReadOnly Property Instance As SqlServerTestHelpers = New SqlServerTestHelpers

    Public Overrides Function AddProviderServices(services As IServiceCollection) As IServiceCollection
        Return services.AddEntityFrameworkSqlServer()
    End Function

    Public Overrides Function UseProviderOptions(optionsBuilder As DbContextOptionsBuilder) As DbContextOptionsBuilder
        Return optionsBuilder.UseSqlServer(New SqlConnection("Database=DummyDatabase"))
    End Function

    Public Overrides ReadOnly Property LoggingDefinitions As LoggingDefinitions = New SqlServerLoggingDefinitions
End Class
