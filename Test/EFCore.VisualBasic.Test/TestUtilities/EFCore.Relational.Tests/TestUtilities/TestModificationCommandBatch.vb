
Imports Microsoft.EntityFrameworkCore.Update

Namespace TestUtilities
    Public Class TestModificationCommandBatch
        Inherits SingularModificationCommandBatch

        Private ReadOnly _maxBatchSize As Integer

        Public Sub New(
            dependencies As ModificationCommandBatchFactoryDependencies,
            maxBatchSize As Integer?)
            MyBase.New(dependencies)
            _maxBatchSize = If(maxBatchSize, 1)
        End Sub
        Protected Overrides Function CanAddCommand(modificationCommand1 As IReadOnlyModificationCommand) As Boolean
            Return ModificationCommands.Count < _maxBatchSize
        End Function
    End Class
End Namespace
