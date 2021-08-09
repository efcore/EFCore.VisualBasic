Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.Extensions.DependencyInjection

Namespace Design.AnnotationCodeGeneratorProvider
    <VisualBasicDesignTimeProviderServices(InMemoryVisualBasicServices.ForProvider)>
    Public Class InMemoryVisualBasicServices
        Implements IDesignTimeServices

        Public Const ForProvider = "Microsoft.EntityFrameworkCore.InMemory"

        Public Sub ConfigureDesignTimeServices(services As IServiceCollection) Implements IDesignTimeServices.ConfigureDesignTimeServices
            services.AddSingleton(Of IVisualBasicRuntimeAnnotationCodeGenerator, InMemoryVisualBasicRuntimeAnnotationCodeGenerator)
        End Sub
    End Class
End Namespace
