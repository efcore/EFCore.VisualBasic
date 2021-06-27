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
                .AddSingleton(Of IMigrationsCodeGenerator, VisualBasicMigrationsGenerator)()
                .AddSingleton(Of IModelCodeGenerator, VisualBasicModelGenerator)()

                'Compiled Models
                .AddSingleton(Of IVisualBasicRuntimeAnnotationCodeGenerator, VisualBasicRuntimeAnnotationCodeGenerator)
                .AddSingleton(Of ICompiledModelCodeGenerator, VisualBasicRuntimeModelCodeGenerator)
            End With

        End Sub
    End Class
End Namespace
