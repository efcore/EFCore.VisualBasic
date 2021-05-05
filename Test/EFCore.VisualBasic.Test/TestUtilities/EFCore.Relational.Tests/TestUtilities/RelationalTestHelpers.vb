Imports EntityFrameworkCore.VisualBasic.TestUtilities.FakeProvider
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports Microsoft.EntityFrameworkCore.TestUtilities
Imports Microsoft.Extensions.DependencyInjection

Namespace TestUtilities
    Public Class RelationalTestHelpers
        Inherits TestHelpers
        Protected Sub New()
        End Sub

        Public Shared ReadOnly Property Instance As New RelationalTestHelpers
        Public Overrides Function AddProviderServices(services As IServiceCollection) As IServiceCollection
            Return FakeRelationalOptionsExtension.AddEntityFrameworkRelationalDatabase(services)
        End Function
        Public Overrides Sub UseProviderOptions(optionsBuilder As DbContextOptionsBuilder)
            Call optionsBuilder.UseFakeRelational()
        End Sub

        Public Overrides ReadOnly Property LoggingDefinitions As LoggingDefinitions = New TestRelationalLoggingDefinitions
    End Class
End Namespace
