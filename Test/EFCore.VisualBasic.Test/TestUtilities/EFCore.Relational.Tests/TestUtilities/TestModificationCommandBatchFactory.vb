
Imports System.Linq
Imports Bricelam.EntityFrameworkCore.VisualBasic.TestUtilities.FakeProvider
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Update

Namespace TestUtilities
    Public Class TestModificationCommandBatchFactory
        Implements IModificationCommandBatchFactory

        Private ReadOnly _dependencies As ModificationCommandBatchFactoryDependencies
        Private ReadOnly _options As IDbContextOptions

        Public Sub New(
            dependencies As ModificationCommandBatchFactoryDependencies,
            options As IDbContextOptions)
            _dependencies = dependencies
            _options = options
        End Sub

        Private _createCount As Integer
        Public Property CreateCount As Integer
            Get
                Return _createCount
            End Get
            Private Set(Value As Integer)
                _createCount = Value
            End Set
        End Property
        Public Overridable Function Create() As ModificationCommandBatch Implements IModificationCommandBatchFactory.Create
            CreateCount += 1

            Dim optionsExtension = _options.Extensions.OfType(Of FakeRelationalOptionsExtension)().FirstOrDefault()

            Return New TestModificationCommandBatch(_dependencies, optionsExtension?.MaxBatchSize)
        End Function

    End Class
End Namespace
