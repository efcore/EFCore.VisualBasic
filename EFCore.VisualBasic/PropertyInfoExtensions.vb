Imports System.Runtime.CompilerServices

Namespace Global.System.Reflection
    <DebuggerStepThrough>
    Friend Module PropertyInfoExtensions
        <Extension()>
        Public Function IsStatic(propertyInfo As PropertyInfo) As Boolean
            Return If(propertyInfo.GetMethod, propertyInfo.SetMethod).IsStatic
        End Function
    End Module
End Namespace
