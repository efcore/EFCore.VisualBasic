''' <summary>
'''     <para>
'''         Service dependencies parameter class for <see cref="VisualBasicMigrationsGenerator" />
'''     </para>
'''     <para>
'''         This type Is typically used by database providers (And other extensions). It Is generally
'''         Not used in application code.
'''     </para>
'''     <para>
'''         Do Not construct instances of this class directly from either provider Or application code as the
'''         constructor signature may change as New dependencies are added. Instead, use this type in
'''         your constructor so that an instance will be created And injected automatically by the
'''         dependency injection container. To create an instance with some dependent services replaced,
'''         first resolve the object from the dependency injection container, then replace selected
'''         services using the 'With...' methods. Do not call the constructor at any point in this process.
'''     </para>
''' </summary>
Public NotInheritable Class VisualBasicMigrationsGeneratorDependencies

    ''' <summary>
    '''     <para>
    '''         Creates the service dependencies parameter object for a <see cref="VisualBasicMigrationsGenerator" />.
    '''     </para>
    '''     <para>
    '''         Do Not call this constructor directly from either provider Or application code as it may change
    '''         as New dependencies are added. Instead, use this type in your constructor so that an instance
    '''         will be created And injected automatically by the dependency injection container. To create
    '''         an instance with some dependent services replaced, first resolve the object from the dependency
    '''         injection container, then replace selected services using the 'With...' methods. Do not call
    '''         the constructor at any point in this process.
    '''     </para>
    ''' </summary>
    ''' 
    ''' <param name="vbHelper"> The Visual Basic helper. </param>
    ''' <param name="vbMigrationOperationGenerator"> The Visual Basic migration operation generator. </param>
    ''' <param name="vbSnapshotGenerator"> The Visual Basic model snapshot generator. </param>
    Public Sub New(
        vbHelper As IVisualBasicHelper,
        vbMigrationOperationGenerator As IVisualBasicMigrationOperationGenerator,
        vbSnapshotGenerator As IVisualBasicSnapshotGenerator)

        VisualBasicHelper = vbHelper
        VisualBasicMigrationOperationGenerator = vbMigrationOperationGenerator
        VisualBasicSnapshotGenerator = vbSnapshotGenerator

    End Sub

    ''' <summary>
    '''     The Visual Basic helper.
    ''' </summary>
    Public ReadOnly Property VisualBasicHelper As IVisualBasicHelper

    ''' <summary>
    '''     The Visual Basic migration operation generator.
    ''' </summary>
    Public ReadOnly Property VisualBasicMigrationOperationGenerator As IVisualBasicMigrationOperationGenerator

    ''' <summary>
    '''     The Visual Basic model snapshot generator.
    ''' </summary>
    Public ReadOnly Property VisualBasicSnapshotGenerator As IVisualBasicSnapshotGenerator

    ''' <summary>
    '''     Clones this dependency parameter object with one service replaced.
    ''' </summary>
    ''' <param name="vbHelper"> A replacement for the current dependency of this type. </param>
    ''' <returns> A New parameter object with the given service replaced. </returns>
    Public Function Uses(vbHelper As IVisualBasicHelper) As VisualBasicMigrationsGeneratorDependencies
        Return New VisualBasicMigrationsGeneratorDependencies(
            vbHelper,
            VisualBasicMigrationOperationGenerator,
            VisualBasicSnapshotGenerator)
    End Function

    ''' <summary>
    '''     Clones this dependency parameter object with one service replaced.
    ''' </summary>
    ''' <param name="vbMigrationOperationGenerator"> A replacement for the current dependency of this type. </param>
    ''' <returns> A New parameter object with the given service replaced. </returns>
    Public Function Uses(vbMigrationOperationGenerator As IVisualBasicMigrationOperationGenerator) As VisualBasicMigrationsGeneratorDependencies
        Return New VisualBasicMigrationsGeneratorDependencies(
            VisualBasicHelper,
            vbMigrationOperationGenerator,
            VisualBasicSnapshotGenerator)
    End Function

    ''' <summary>
    '''     Clones this dependency parameter object with one service replaced.
    ''' </summary>
    ''' <param name="vbSnapshotGenerator"> A replacement for the current dependency of this type. </param>
    ''' <returns> A New parameter object with the given service replaced. </returns>
    Public Function Uses(vbSnapshotGenerator As IVisualBasicSnapshotGenerator) As VisualBasicMigrationsGeneratorDependencies
        Return New VisualBasicMigrationsGeneratorDependencies(
            VisualBasicHelper,
            VisualBasicMigrationOperationGenerator,
            vbSnapshotGenerator)
    End Function

End Class