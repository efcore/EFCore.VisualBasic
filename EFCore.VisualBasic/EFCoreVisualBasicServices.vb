Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Migrations.Design
Imports Microsoft.EntityFrameworkCore.Scaffolding
Imports Microsoft.Extensions.DependencyInjection

Public Class EFCoreVisualBasicServices
    Implements IDesignTimeServices

    Public Sub ConfigureDesignTimeServices(services As IServiceCollection) Implements IDesignTimeServices.ConfigureDesignTimeServices
        services.AddSingleton(Of IModelCodeGenerator, VisualBasicModelGenerator)()
        services.AddSingleton(Of IMigrationsCodeGenerator, VisualBasicMigrationsGenerator)()
    End Sub
End Class