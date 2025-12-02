Imports EntityFrameworkCore.VisualBasic.TestUtilities.FakeProvider
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports Microsoft.EntityFrameworkCore.TestUtilities
Imports Microsoft.Extensions.DependencyInjection

Namespace TestUtilities

    Public Class FakeRelationalTestHelpers
        Inherits TestHelpers

        Protected Sub New()
        End Sub

        Public Shared ReadOnly Property Instance As FakeRelationalTestHelpers = New FakeRelationalTestHelpers

        Protected Overrides Function CreateEntityFrameworkDesignServicesBuilder(services As IServiceCollection) As EntityFrameworkDesignServicesBuilder
            Return New EntityFrameworkRelationalDesignServicesBuilder(services)
        End Function

        Public Overrides Function AddProviderServices(services As IServiceCollection) As IServiceCollection
            Return FakeRelationalOptionsExtension.AddEntityFrameworkRelationalDatabase(services)
        End Function

        Public Overrides Function UseProviderOptions(optionsBuilder As DbContextOptionsBuilder) As DbContextOptionsBuilder
            Return optionsBuilder.UseFakeRelational()
        End Function

        Public Overrides ReadOnly Property LoggingDefinitions As LoggingDefinitions = New TestRelationalLoggingDefinitions()
    End Class
End Namespace
