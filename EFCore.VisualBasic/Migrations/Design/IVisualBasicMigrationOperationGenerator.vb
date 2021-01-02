Imports Microsoft.EntityFrameworkCore.Infrastructure
Imports Microsoft.EntityFrameworkCore.Migrations.Operations

Namespace Migrations.Design
    Public Interface IVisualBasicMigrationOperationGenerator
        ''' <summary>
        '''     Used to generate VB code for creating <see cref="MigrationOperation" /> objects.
        ''' </summary>
        ''' <param name="builderName"> The <see cref="MigrationOperation" /> variable name. </param>
        ''' <param name="operations"> The operations. </param>
        ''' <param name="builder"> The builder code is added to. </param>
        Sub Generate(builderName As String,
                 operations As IReadOnlyList(Of MigrationOperation),
                 builder As IndentedStringBuilder)
    End Interface
End Namespace