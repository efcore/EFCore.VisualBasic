Namespace Design.AnnotationCodeGeneratorProvider

    ''' <summary>
    '''     This Is an internal API that supports the Entity Framework Core infrastructure And Not subject to
    '''     the same compatibility standards As public APIs. It may be changed Or removed without notice in
    '''     any release. You should only use it directly in your code with extreme caution And knowing that
    '''     doing so can result in application failures when updating to a New Entity Framework Core release.
    ''' </summary>
    <AttributeUsage(AttributeTargets.Class, AllowMultiple:=True)>
    Public Class VisualBasicDesignTimeProviderServicesAttribute
        Inherits Attribute

        Public ReadOnly Property ForProvider As String

        Sub New(forProvider As String)
            Me.ForProvider = NotNull(forProvider, NameOf(forProvider))
        End Sub
    End Class
End Namespace
