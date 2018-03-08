Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Migrations.Design
Imports Microsoft.EntityFrameworkCore.Scaffolding
Imports Microsoft.Extensions.DependencyInjection

Public Class EFCoreVisualBasicServices
    Implements IDesignTimeServices

    Public Sub ConfigureDesignTimeServices(services As IServiceCollection) Implements IDesignTimeServices.ConfigureDesignTimeServices
        services.AddSingleton(Of IModelCodeGenerator, VisualBasicModelGenerator)()
        services.AddSingleton(Of VisualBasicMigrationsGeneratorDependencies)()
        services.AddSingleton(Of IMigrationsCodeGenerator, VisualBasicMigrationsGenerator)()
        services.AddSingleton(Of IVisualBasicHelper, VisualBasicHelper)()
        services.AddSingleton(Of IVisualBasicMigrationOperationGenerator, VisualBasicMigrationOperationGenerator)()
        services.AddSingleton(Of IVisualBasicSnapshotGenerator, VisualBasicSnapshotGenerator)()
        services.AddSingleton(Of VisualBasicMigrationOperationGeneratorDependencies)()
        services.AddSingleton(Of VisualBasicSnapshotGeneratorDependencies)()
    End Sub
End Class