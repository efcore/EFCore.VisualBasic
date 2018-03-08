Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.Extensions.DependencyInjection

<Assembly: DesignTimeServicesReference("Bricelam.EntityFrameworkCore.VisualBasic.EFCoreVisualBasicServices, Bricelam.EntityFrameworkCore.VisualBasic")>

Class DesignTimeServices
    Implements IDesignTimeServices

    Public Sub ConfigureDesignTimeServices(serviceCollection As IServiceCollection) Implements IDesignTimeServices.ConfigureDesignTimeServices
        SQLitePCL.Batteries_V2.Init()
    End Sub
End Class