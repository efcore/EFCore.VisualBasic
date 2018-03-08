Imports Microsoft.EntityFrameworkCore.Metadata

Public Interface IVisualBasicEntityTypeGenerator

    Function WriteCode(entityType As IEntityType,
                       [namespace] As String,
                       useDataAnnotations As Boolean) As String

End Interface
