' This API supports the Entity Framework Core infrastructure and is not intended to be used
' directly from your code. This API may change Or be removed In future releases.

Public NotInheritable Class VBDesignStrings
    Private Sub New()
    End Sub

    Public Shared Function UnknownOperation(operationType As Object) As String
        Return $"The current VisualBasicMigrationOperationGenerator cannot scaffold operations of type '{operationType}'. Configure your services to use one that can."
    End Function

    Public Shared Function UnknownLiteral(operationType As Type) As String
        Return $"The current VisualBasicHelper cannot scaffold operations of type '{operationType}'. Configure your services to use one that can."
    End Function

    Public Shared Function CompiledModelIncompatibleTypeMapping(typeMapping As Object) As String
        Return String.Format("The type mapping used is incompatible with a compiled model. The mapping type must have a 'Public Shared Readonly Property {0}.Default As {0}'.", typeMapping)
    End Function
End Class
