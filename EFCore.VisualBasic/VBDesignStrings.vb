'
' Résumé :
'     This API supports the Entity Framework Core infrastructure and is not intended
'     to be used directly from your code. This API may change or be removed in future
'     releases.
Imports System.Runtime.Serialization

Public NotInheritable Class VBDesignStrings
    Public Shared Function UnknownOperation(operationType As Object) As String
        Return $"The current VisualBasicMigrationOperationGenerator cannot scaffold operations of type '{operationType}'. Configure your services to use one that can."
    End Function

    Public Shared Function UnknownLiteral(operationType As Type) As String
        Return $"The current VisualBasicHelper cannot scaffold operations of type '{operationType}'. Configure your services to use one that can."
    End Function
End Class