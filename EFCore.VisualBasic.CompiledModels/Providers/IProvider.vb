Friend Interface IProvider
    ReadOnly Property ForProvider As String

    Function GetDesignTimeServices(currentAssemblyName As String) As String
    Function GetRuntimeAnnotationCodeGenerator() As String
End Interface
