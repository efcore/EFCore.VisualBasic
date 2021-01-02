Imports Microsoft.EntityFrameworkCore
Imports Sandbox.Models

Namespace Data

    Public Class SchoolContext
        Inherits DbContext

        Sub New()

        End Sub

        Public Property Courses As DbSet(Of Course)
        Public Property Enrollments As DbSet(Of Enrollment)
        Public Property Students As DbSet(Of Student)

        Protected Overrides Sub OnConfiguring(optionsBuilder As DbContextOptionsBuilder)
            optionsBuilder.UseSqlite("Data Source=School.db")
        End Sub

        Protected Overrides Sub OnModelCreating(mb As ModelBuilder)
            mb.Entity(Of Course)().ToTable("Course")
            mb.Entity(Of Enrollment)().ToTable("Enrollment")
            mb.Entity(Of Student)().ToTable("Student")
        End Sub


    End Class

End Namespace
