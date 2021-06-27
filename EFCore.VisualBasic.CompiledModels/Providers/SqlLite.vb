Imports System.Reflection

Friend Class SqlLite
    Implements IProvider

    Public ReadOnly Property ForProvider As String Implements IProvider.ForProvider
        Get
            Return "Microsoft.EntityFrameworkCore.Sqlite"
        End Get
    End Property

    Function GetDesignTimeServices(currentAssemblyName As String) As String Implements IProvider.GetDesignTimeServices
        Return "Imports EntityFrameworkCore.VisualBasic.Design
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.Extensions.DependencyInjection

<Assembly: DesignTimeServicesReference( """ & Assembly.CreateQualifiedName(currentAssemblyName, "EntityFrameworkCore.VisualBasic.CompiledModels.EFCoreVisualBasicServicesSqlite") & """, """ & ForProvider & """)>

Namespace Global.EntityFrameworkCore.VisualBasic.CompiledModels
    Public Class EFCoreVisualBasicServicesSqlite
        Implements IDesignTimeServices

        Public Sub ConfigureDesignTimeServices(services As IServiceCollection) Implements IDesignTimeServices.ConfigureDesignTimeServices                   
            services.AddSingleton(Of IVisualBasicRuntimeAnnotationCodeGenerator, SqliteVisualBasicRuntimeAnnotationCodeGenerator)
        End Sub
    End Class
End Namespace
"
    End Function

    Public Function GetRuntimeAnnotationCodeGenerator() As String Implements IProvider.GetRuntimeAnnotationCodeGenerator
        Return <![CDATA[Imports EntityFrameworkCore.VisualBasic.Design
Imports Microsoft.EntityFrameworkCore.Design
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Sqlite.Metadata.Internal

Namespace Global.EntityFrameworkCore.VisualBasic.CompiledModels
    ''' <summary>
    '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
    '''     any release. You should only use it directly in your code with extreme caution and knowing that
    '''     doing so can result in application failures when updating to a new Entity Framework Core release.
    ''' </summary>
    Public Class SqliteVisualBasicRuntimeAnnotationCodeGenerator
        Inherits RelationalVisualBasicRuntimeAnnotationCodeGenerator

        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Public Sub New(vbHelper As IVisualBasicHelper)
            MyBase.New(vbHelper)
        End Sub

        ''' <inheritdoc/>
        Public Overrides Sub Generate([property] As IProperty, parameters As VisualBasicRuntimeAnnotationCodeGeneratorParameters)
            Dim annotations1 = parameters.Annotations
            If Not parameters.IsRuntime Then
                annotations1.Remove(SqliteAnnotationNames.Srid)
            End If

            MyBase.Generate([property], parameters)
        End Sub
    End Class
End Namespace
]]>.Value
    End Function
End Class
