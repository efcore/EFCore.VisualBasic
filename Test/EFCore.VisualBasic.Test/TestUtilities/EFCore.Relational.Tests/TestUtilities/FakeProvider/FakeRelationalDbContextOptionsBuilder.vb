
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Infrastructure

Namespace TestUtilities.FakeProvider
    Public Class FakeRelationalDbContextOptionsBuilder
        Inherits RelationalDbContextOptionsBuilder(Of FakeRelationalDbContextOptionsBuilder, FakeRelationalOptionsExtension)
        Public Sub New(optionsBuilder As DbContextOptionsBuilder)
            MyBase.New(optionsBuilder)
        End Sub
    End Class
End Namespace
