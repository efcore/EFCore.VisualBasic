Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.Extensions.DependencyInjection

Namespace Design.AnnotationCodeGeneratorProvider
    <VisualBasicDesignTimeProviderServices(SqliteVisualBasicServices.ProviderName)>
    Public Class SqliteVisualBasicServices
        Implements IDesignTimeServices

        Public Const ProviderName = "Microsoft.EntityFrameworkCore.Sqlite"

        Public Sub ConfigureDesignTimeServices(services As IServiceCollection) Implements IDesignTimeServices.ConfigureDesignTimeServices
            services.AddSingleton(Of IVisualBasicRuntimeAnnotationCodeGenerator, SqliteVisualBasicRuntimeAnnotationCodeGenerator)
        End Sub
    End Class
End Namespace
