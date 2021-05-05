Imports EntityFrameworkCore.VisualBasic.Design.Internal
Imports EntityFrameworkCore.VisualBasic.Migrations.Design
Imports EntityFrameworkCore.VisualBasic.Scaffolding.Internal
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Migrations.Design
Imports Microsoft.EntityFrameworkCore.Scaffolding
Imports Microsoft.Extensions.DependencyInjection

Namespace Design
    Public Class EFCoreVisualBasicServices
        Implements IDesignTimeServices

        Public Sub ConfigureDesignTimeServices(services As IServiceCollection) Implements IDesignTimeServices.ConfigureDesignTimeServices

            With services

                .AddSingleton(Of IVisualBasicHelper, VisualBasicHelper)()

                'Migrations

                .AddSingleton(Of VisualBasicMigrationOperationGeneratorDependencies)()
                .AddSingleton(Of IVisualBasicMigrationOperationGenerator, VisualBasicMigrationOperationGenerator)()

                .AddSingleton(Of VisualBasicSnapshotGeneratorDependencies)()
                .AddSingleton(Of IVisualBasicSnapshotGenerator, VisualBasicSnapshotGenerator)()

                .AddSingleton(Of VisualBasicMigrationsGeneratorDependencies)()
                .AddSingleton(Of IMigrationsCodeGenerator, VisualBasicMigrationsGenerator)()

                'Scaffolding
                .AddSingleton(Of IVisualBasicDbContextGenerator, VisualBasicDbContextGenerator)()
                .AddSingleton(Of IVisualBasicEntityTypeGenerator, VisualBasicEntityTypeGenerator)()
                .AddSingleton(Of IModelCodeGenerator, VisualBasicModelGenerator)()

            End With

        End Sub
    End Class
End Namespace
