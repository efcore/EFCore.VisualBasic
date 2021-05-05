Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure
Imports Microsoft.EntityFrameworkCore.Migrations
Imports Microsoft.EntityFrameworkCore.Storage
Imports Microsoft.EntityFrameworkCore.TestUtilities
Imports Microsoft.EntityFrameworkCore.Update
Imports Microsoft.Extensions.DependencyInjection

Namespace TestUtilities.FakeProvider
    Public Class FakeRelationalOptionsExtension
        Inherits RelationalOptionsExtension

        Private _info As DbContextOptionsExtensionInfo

        Public Sub New()
        End Sub

        Protected Sub New(copyFrom As FakeRelationalOptionsExtension)
            MyBase.New(copyFrom)
        End Sub

        Public Overrides ReadOnly Property Info As DbContextOptionsExtensionInfo
            Get
                _info = New ExtensionInfo(Me)
                Return _info
            End Get
        End Property

        Protected Overrides Function Clone() As RelationalOptionsExtension
            Return New FakeRelationalOptionsExtension(Me)
        End Function

        Public Overrides Sub ApplyServices(services As IServiceCollection)
            Call AddEntityFrameworkRelationalDatabase(services)
        End Sub

        Public Shared Function AddEntityFrameworkRelationalDatabase(serviceCollection As IServiceCollection) As IServiceCollection
            Dim builder = New EntityFrameworkRelationalServicesBuilder(serviceCollection).
                TryAdd(Of LoggingDefinitions, TestRelationalLoggingDefinitions)().
                TryAdd(Of IDatabaseProvider, DatabaseProvider(Of FakeRelationalOptionsExtension))().
                TryAdd(Of ISqlGenerationHelper, RelationalSqlGenerationHelper)().
                TryAdd(Of IRelationalTypeMappingSource, TestRelationalTypeMappingSource)().
                TryAdd(Of IMigrationsSqlGenerator, TestRelationalMigrationSqlGenerator)().
                TryAdd(Of IProviderConventionSetBuilder, TestRelationalConventionSetBuilder)().
                TryAdd(Of IRelationalConnection, FakeRelationalConnection)().
                TryAdd(Of IHistoryRepository)(Function(x) Nothing).
                TryAdd(Of IUpdateSqlGenerator, FakeSqlGenerator)().
                TryAdd(Of IModificationCommandBatchFactory, TestModificationCommandBatchFactory)().
                TryAdd(Of IRelationalDatabaseCreator, FakeRelationalDatabaseCreator)()

            builder.TryAddCoreServices()

            Return serviceCollection
        End Function

        Private NotInheritable Class ExtensionInfo
            Inherits RelationalExtensionInfo

            Public Sub New(extension As IDbContextOptionsExtension)
                MyBase.New(extension)
            End Sub

            Public Overrides Sub PopulateDebugInfo(debugInfo As IDictionary(Of String, String))
            End Sub
        End Class

    End Class
End Namespace
