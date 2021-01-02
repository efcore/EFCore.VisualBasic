' Copyright (c) .NET Foundation. All rights reserved.
' Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

Imports Microsoft.EntityFrameworkCore.Metadata

Namespace Scaffolding.Internal
    ''' <summary>
    '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
    '''     any release. You should only use it directly in your code with extreme caution and knowing that
    '''     doing so can result in application failures when updating to a new Entity Framework Core release.
    ''' </summary>
    Public Interface IVisualBasicDbContextGenerator
        ''' <summary>
        '''     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        '''     the same compatibility standards as public APIs. It may be changed or removed without notice in
        '''     any release. You should only use it directly in your code with extreme caution and knowing that
        '''     doing so can result in application failures when updating to a new Entity Framework Core release.
        ''' </summary>
        Function WriteCode(
            model As IModel,
            contextName As String,
            connectionString As String,
            rootNamespace As String,
            contextNamespace As String,
            modelNamespace As String,
            useDataAnnotations As Boolean,
            suppressConnectionStringWarning As Boolean,
            suppressOnConfiguring As Boolean) As String
    End Interface


End Namespace
