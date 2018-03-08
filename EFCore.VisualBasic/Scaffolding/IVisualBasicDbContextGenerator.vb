Imports Microsoft.EntityFrameworkCore.Metadata

Public Interface IVisualBasicDbContextGenerator

    Function WriteCode(model As IModel,
                       [namespace] As String,
                       contextName As String,
                       connectionString As String,
                       useDataAnnotations As Boolean,
                       suppressConnectionStringWarning As Boolean) As String

End Interface