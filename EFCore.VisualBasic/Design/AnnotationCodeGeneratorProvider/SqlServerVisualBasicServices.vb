Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.Extensions.DependencyInjection

Namespace Design.AnnotationCodeGeneratorProvider
    <VisualBasicDesignTimeProviderServices(SqlServerVisualBasicServices.ForProvider)>
    Public Class SqlServerVisualBasicServices
        Implements IDesignTimeServices

        Public Const ForProvider = "Microsoft.EntityFrameworkCore.SqlServer"

        Public Sub ConfigureDesignTimeServices(services As IServiceCollection) Implements IDesignTimeServices.ConfigureDesignTimeServices
            services.AddSingleton(Of IVisualBasicRuntimeAnnotationCodeGenerator, SqlServerVisualBasicRuntimeAnnotationCodeGenerator)
        End Sub
    End Class

End Namespace
