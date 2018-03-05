Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.TestUtilities
Imports Microsoft.Extensions.DependencyInjection

Public Class InMemoryTestHelpers
    Inherits TestHelpers

    Protected Sub New()

    End Sub

    Public Shared ReadOnly Property Instance As InMemoryTestHelpers = New InMemoryTestHelpers()

    Public Overrides Function AddProviderServices(services As IServiceCollection) As IServiceCollection
        Return services.AddEntityFrameworkInMemoryDatabase()
    End Function

    Protected Overrides Sub UseProviderOptions(optionsBuilder As DbContextOptionsBuilder)
        optionsBuilder.UseInMemoryDatabase(NameOf(InMemoryTestHelpers))
    End Sub
End Class
