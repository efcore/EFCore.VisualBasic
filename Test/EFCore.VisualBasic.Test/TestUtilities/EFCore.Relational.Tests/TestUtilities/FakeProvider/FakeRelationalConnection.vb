Imports System.Data.Common
Imports Microsoft.EntityFrameworkCore
Imports Microsoft.EntityFrameworkCore.Diagnostics.Internal
Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Infrastructure.Internal
Imports Microsoft.EntityFrameworkCore.Storage
Imports Microsoft.EntityFrameworkCore.Storage.Internal
Imports Microsoft.EntityFrameworkCore.TestUtilities
Imports Microsoft.Extensions.Logging

Namespace TestUtilities.FakeProvider
    Public Class FakeRelationalConnection
        Inherits RelationalConnection
        Private _connection As DbConnection
        Private ReadOnly _dbConnections As New List(Of FakeDbConnection)()

        Public Sub New(Optional options As IDbContextOptions = Nothing)
            MyBase.New(
                New RelationalConnectionDependencies(
                    If(options, CreateOptions()),
                    New DiagnosticsLogger(Of DbLoggerCategory.Database.Transaction)(
                        New LoggerFactory,
                        New LoggingOptions,
                        New DiagnosticListener("FakeDiagnosticListener"),
                        New TestRelationalLoggingDefinitions,
                        New NullDbContextLogger),
                    New RelationalConnectionDiagnosticsLogger(
                        New LoggerFactory,
                        New LoggingOptions,
                        New DiagnosticListener("FakeDiagnosticListener"),
                        New TestRelationalLoggingDefinitions,
                        New NullDbContextLogger,
                        CreateOptions()),
                    New NamedConnectionStringResolver(If(options, CreateOptions())),
                    New RelationalTransactionFactory(
                        New RelationalTransactionFactoryDependencies(
                            New RelationalSqlGenerationHelper(
                                New RelationalSqlGenerationHelperDependencies))),
                    New CurrentDbContext(New FakeDbContext()),
                    New RelationalCommandBuilderFactory(
                        New RelationalCommandBuilderDependencies(
                            New TestRelationalTypeMappingSource(
                                TestServiceFactory.Instance.Create(Of TypeMappingSourceDependencies)(),
                                TestServiceFactory.Instance.Create(Of RelationalTypeMappingSourceDependencies)())))))
        End Sub

        Private Class FakeDbContext
            Inherits DbContext
        End Class

        Private Shared Function CreateOptions() As IDbContextOptions
            Dim optionsBuilder As New DbContextOptionsBuilder

            DirectCast(optionsBuilder, IDbContextOptionsBuilderInfrastructure).
                AddOrUpdateExtension(New FakeRelationalOptionsExtension().WithConnectionString("Database=Dummy"))

            Return optionsBuilder.Options
        End Function
        Public Sub UseConnection(connection As DbConnection)
            _connection = connection
        End Sub
        Public Overrides Property DbConnection As DbConnection
            Get
                Return If(_connection, MyBase.DbConnection)
            End Get
            Set
                _connection = Value
            End Set
        End Property
        Public ReadOnly Property DbConnections As IReadOnlyList(Of FakeDbConnection)
            Get
                Return _dbConnections
            End Get
        End Property
        Protected Overrides Function CreateDbConnection() As DbConnection
            Dim connection As New FakeDbConnection(ConnectionString)

            _dbConnections.Add(connection)

            Return connection
        End Function
    End Class
End Namespace
