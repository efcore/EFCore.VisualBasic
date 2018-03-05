Imports System
Imports System.Globalization
Imports System.Reflection
Imports System.Threading
Imports Xunit.Sdk

Namespace Microsoft.EntityFrameworkCore.TestUtilities.Xunit

    <AttributeUsage(AttributeTargets.[Class] Or AttributeTargets.Method)>
    Public Class UseCultureAttribute
        Inherits BeforeAfterTestAttribute

        Private _originalCulture As CultureInfo

        Private _originalUICulture As CultureInfo

        Public Sub New(ByVal culture As String)
            MyClass.New(culture, culture)
        End Sub

        Public Sub New(ByVal newCulture As String, ByVal newUiCulture As String)
            Culture = New CultureInfo(newCulture)
            UICulture = New CultureInfo(newUiCulture)
        End Sub

        Public Property Culture As CultureInfo

        Public Property UICulture As CultureInfo

        Public Overrides Sub Before(ByVal methodUnderTest As MethodInfo)
            _originalCulture = CultureInfo.CurrentCulture
            _originalUICulture = CultureInfo.CurrentUICulture
            CultureInfo.CurrentCulture = Culture
            CultureInfo.CurrentUICulture = UICulture
        End Sub

        Public Overrides Sub After(ByVal methodUnderTest As MethodInfo)
            CultureInfo.CurrentCulture = _originalCulture
            CultureInfo.CurrentUICulture = _originalUICulture
        End Sub
    End Class
End Namespace