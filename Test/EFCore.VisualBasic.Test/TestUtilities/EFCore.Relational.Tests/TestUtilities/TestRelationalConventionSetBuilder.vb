
Imports Microsoft.EntityFrameworkCore.Metadata.Conventions
Imports Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure

Namespace TestUtilities
    Public Class TestRelationalConventionSetBuilder
        Inherits RelationalConventionSetBuilder
        Public Sub New(
            dependencies As ProviderConventionSetBuilderDependencies,
            relationalDependencies As RelationalConventionSetBuilderDependencies)
            MyBase.New(dependencies, relationalDependencies)
        End Sub
        Public Shared Function Build() As ConventionSet
            Return ConventionSet.CreateConventionSet(RelationalTestHelpers.Instance.CreateContext())
        End Function
    End Class
End Namespace
