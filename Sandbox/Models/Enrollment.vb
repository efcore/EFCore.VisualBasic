Namespace Models

    Public Class Enrollment
        Public Property EnrollmentID As Integer
        Public Property CourseID As Integer
        Public Property StudentID As Integer
        Public Property Grade As Grade?

        Public Property Course As Course
        Public Property Student As Student
    End Class

    Public Enum Grade
        A
        B
        C
        D
        F
    End Enum
End Namespace
