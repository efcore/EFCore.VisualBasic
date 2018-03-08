Imports Microsoft.EntityFrameworkCore

Public Class TestContext
    Inherits DbContext

    Public Property Blogs As DbSet(Of Blog)

    Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
        optionsBuilder.UseSqlite("Data Source=test.db")
    End Sub


End Class

Public Class Blog
    Public Property Id As Integer
End Class
