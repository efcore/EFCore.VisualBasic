Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.Extensions.DependencyInjection

Namespace Design.AnnotationCodeGeneratorProvider
    <VisualBasicDesignTimeProviderServices(CosmosVisualBasicServices.ForProvider)>
    Public Class CosmosVisualBasicServices
        Implements IDesignTimeServices

        Public Const ForProvider = "Microsoft.EntityFrameworkCore.Cosmos"

        Public Sub ConfigureDesignTimeServices(services As IServiceCollection) Implements IDesignTimeServices.ConfigureDesignTimeServices
            services.AddSingleton(Of IVisualBasicRuntimeAnnotationCodeGenerator, CosmosVisualBasicRuntimeAnnotationCodeGenerator)
        End Sub
    End Class
End Namespace
