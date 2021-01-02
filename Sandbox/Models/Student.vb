Namespace Models
    Public Class Student
        Public Property ID As Integer
        Public Property LastName As String
        Public Property FirstMidName As String
        Public Property EnrollmentDate As DateTime

        Public Property Enrollments As ICollection(Of Enrollment)
    End Class

End Namespace