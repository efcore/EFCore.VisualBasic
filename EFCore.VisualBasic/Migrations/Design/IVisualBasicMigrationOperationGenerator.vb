Imports Microsoft.EntityFrameworkCore.Internal
Imports Microsoft.EntityFrameworkCore.Migrations.Operations

Public Interface IVisualBasicMigrationOperationGenerator
    ''' <summary>
    '''     Generates code for creating <see cref="MigrationOperation" /> objects.
    ''' </summary>
    ''' <param name="builderName"> The <see cref="MigrationOperation" /> variable name. </param>
    ''' <param name="operations"> The operations. </param>
    ''' <param name="builder"> The builder code is added to. </param>
    Sub Generate(
            builderName As String,
             operations As IReadOnlyList(Of MigrationOperation),
            builder As IndentedStringBuilder)
End Interface
