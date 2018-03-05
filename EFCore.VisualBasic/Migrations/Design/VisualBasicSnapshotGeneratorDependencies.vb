
''' <summary>
'''     <para>
'''         Service dependencies parameter class for <see cref="VisualBasicSnapshotGenerator" />
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
Public Class VisualBasicSnapshotGeneratorDependencies

    ''' <summary>
    '''     <para>
    '''         Creates the service dependencies parameter object for a <see cref="VisualBasicSnapshotGenerator" />.
    '''     </para>
    '''     <para>
    '''         Do Not call this constructor directly from either provider Or application code as it may change
    '''         as New dependencies are added. Instead, use this type in your constructor so that an instance
    '''         will be created And injected automatically by the dependency injection container. To create
    '''         an instance with some dependent services replaced, first resolve the object from the dependency
    '''         injection container, then replace selected services using the 'With...' methods. Do not call
    '''         the constructor at any point in this process.
    '''     </para>
    '''     <para>
    '''         This API supports the Entity Framework Core infrastructure And Is Not intended to be used
    '''         directly from your code. This API may change Or be removed in future releases.
    '''     </para>
    ''' </summary>
    ''' <param name="vbHelper"> The Visual Basic helper. </param>
    Public Sub New(vbHelper As IVisualBasicHelper)
        VisualBasicHelper = vbHelper

    End Sub

    ''' <summary>
    '''     The Visual Basic helper.
    ''' </summary>
    Public ReadOnly Property VisualBasicHelper As IVisualBasicHelper
End Class