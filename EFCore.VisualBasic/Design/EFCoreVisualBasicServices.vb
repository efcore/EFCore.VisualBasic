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

                .AddSingleton(Of VisualBasicHelper)()

                'Migrations
                .AddSingleton(Of VisualBasicMigrationOperationGenerator)()
                .AddSingleton(Of VisualBasicSnapshotGenerator)()
                .AddSingleton(Of IMigrationsCodeGenerator, VisualBasicMigrationsGenerator)()

                'Scaffolding
                .AddSingleton(Of VisualBasicDbContextGenerator)()
                .AddSingleton(Of VisualBasicEntityTypeGenerator, VisualBasicEntityTypeGenerator)()
                .AddSingleton(Of IModelCodeGenerator, VisualBasicModelGenerator)()

            End With

        End Sub
    End Class
End Namespace
