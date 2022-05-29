
Imports Microsoft.EntityFrameworkCore.Update

Namespace TestUtilities
    Public Class TestModificationCommandBatch
        Inherits SingularModificationCommandBatch

        Public Sub New(
            dependencies As ModificationCommandBatchFactoryDependencies,
            maxBatchSize As Integer?)
            MyBase.New(dependencies)

            Me.MaxBatchSize = If(maxBatchSize, 42)
        End Sub

        Protected Overrides ReadOnly Property MaxBatchSize As Integer
    End Class
End Namespace
