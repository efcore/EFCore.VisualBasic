Imports Bricelam.EntityFrameworkCore.VisualBasic
Imports Microsoft.EntityFrameworkCore.Metadata

Public Class VisualBasicDbContextGenerator
    Implements IVisualBasicDbContextGenerator

    Public Function WriteCode(model As IModel, [namespace] As String, contextName As String, connectionString As String, useDataAnnotations As Boolean, suppressConnectionStringWarning As Boolean) As String Implements IVisualBasicDbContextGenerator.WriteCode
        Throw New NotImplementedException()
    End Function
End Class
