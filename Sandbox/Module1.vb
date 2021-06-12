Imports Sandbox.Data
Imports Sandbox.Models

Module Module1

    Sub Main()

        '-------------------------------------
        'Migration
        '-------------------------------------

        'Add-Migration InitialCreate -Context 'SchoolContext'
        'Update-Database -Context 'SchoolContext'
        'Script-Migration -Context 'SchoolContext'


        '-------------------------------------
        'Reverse Engineering, https://docs.microsoft.com/en-us/ef/core/managing-schemas/scaffolding?tabs=vs
        '-------------------------------------
        '
        '-- Northwind --
        'Reverse Engineering, using Fluent API
        'Scaffold-DbContext 'Data Source=northwind.db' Microsoft.EntityFrameworkCore.Sqlite -OutputDir Scaffolding\Northwind -ContextDir Scaffolding\Northwind\Context
        '
        'Reverse Engineering, using Data Annotations
        'Scaffold-DbContext 'Data Source=northwind.db' Microsoft.EntityFrameworkCore.Sqlite -OutputDir Scaffolding\Northwind -ContextDir Scaffolding\Northwind\Context -DataAnnotations
        '

        Using dbContext As New SchoolContext()
            dbContext.Students.Add(
                New Student With {
                    .FirstMidName = "Brice",
                    .LastName = "Lambson",
                    .EnrollmentDate = Now}
                )
            dbContext.SaveChanges()

            Console.WriteLine($"Students count : {dbContext.Students.Count}")

        End Using

        Console.WriteLine("Press any keys to exit")
        Console.ReadKey()

    End Sub

End Module
