
Imports System.Data.Common
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports System.Runtime.CompilerServices
Imports Microsoft.EntityFrameworkCore

Namespace TestUtilities.FakeProvider
    Public Module FakeRelationalDbContextOptionsExtension

        <Extension()>
        Public Function UseFakeRelational(
optionsBuilder As DbContextOptionsBuilder,
            Optional fakeRelationalOptionsAction As Action(Of FakeRelationalDbContextOptionsBuilder) = Nothing) As DbContextOptionsBuilder
            Return optionsBuilder.UseFakeRelational("Database=Fake", fakeRelationalOptionsAction)
        End Function

        <Extension()>
        Public Function UseFakeRelational(
optionsBuilder As DbContextOptionsBuilder,
            connectionString As String,
            Optional fakeRelationalOptionsAction As Action(Of FakeRelationalDbContextOptionsBuilder) = Nothing) As DbContextOptionsBuilder
            Return optionsBuilder.UseFakeRelational(New FakeDbConnection(connectionString), fakeRelationalOptionsAction)
        End Function

        <Extension()>
        Public Function UseFakeRelational(optionsBuilder As DbContextOptionsBuilder,
            connection As DbConnection,
            Optional fakeRelationalOptionsAction As Action(Of FakeRelationalDbContextOptionsBuilder) = Nothing) As DbContextOptionsBuilder
            Dim extension = CType(GetOrCreateExtension(optionsBuilder).WithConnection(connection), FakeRelationalOptionsExtension)

            CType(optionsBuilder, IDbContextOptionsBuilderInfrastructure).AddOrUpdateExtension(extension)

            fakeRelationalOptionsAction?.Invoke(New FakeRelationalDbContextOptionsBuilder(optionsBuilder))

            Return optionsBuilder
        End Function

        Private Function GetOrCreateExtension(optionsBuilder As DbContextOptionsBuilder) As FakeRelationalOptionsExtension
            Return If(optionsBuilder.Options.FindExtension(Of FakeRelationalOptionsExtension)(), New FakeRelationalOptionsExtension)
        End Function
    End Module
End Namespace
