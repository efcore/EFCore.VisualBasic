Imports System.Diagnostics.CodeAnalysis
Imports System.Runtime.CompilerServices

<DebuggerStepThrough>
Friend Module DictionaryExtensions
    <Extension()>
    Public Function GetOrAddNew(Of TKey, TValue As New)(source As IDictionary(Of TKey, TValue),
                                                        key As TKey) As TValue
        Dim value As TValue = Nothing
        If Not source.TryGetValue(key, value) Then
            value = New TValue
            source.Add(key, value)
        End If

        Return value
    End Function

    <Extension()>
    Public Function Find(Of TKey, TValue)(source As IReadOnlyDictionary(Of TKey, TValue),
                                          key As TKey) As TValue
        Dim value As TValue = Nothing
        Return If(Not source.TryGetValue(key, value), Nothing, value)
    End Function

    <Extension()>
    Public Function TryGetAndRemove(Of TKey, TValue, TReturn)(source As IDictionary(Of TKey, TValue),
                                                              key As TKey,
                                                              <NotNullWhen(True)> ByRef annotationValue As TReturn) As Boolean
        Dim value As TValue = Nothing
        If source.TryGetValue(key, value) Then
            If value IsNot Nothing Then
                source.Remove(key)
                annotationValue = CType(CObj(value), TReturn)
                Return True
            End If
        End If

        annotationValue = Nothing
        Return False
    End Function

    <Extension()>
    Public Sub Remove(Of TKey, TValue)(source As IDictionary(Of TKey, TValue),
                                       predicate As Func(Of TKey, TValue, Boolean))

        Call source.Remove(Function(k, v, p) p(k, v), predicate)
    End Sub

    <Extension()>
    Public Sub Remove(Of TKey, TValue, TState)(source As IDictionary(Of TKey, TValue),
                                               predicate As Func(Of TKey, TValue, TState, Boolean),
                                               state As TState)

        Dim found As Boolean = False
        Dim firstRemovedKey As TKey = Nothing
        Dim pairsRemainder As List(Of KeyValuePair(Of TKey, TValue)) = Nothing

        For Each pair In source
            If found Then
                If pairsRemainder Is Nothing Then
                    pairsRemainder = New List(Of KeyValuePair(Of TKey, TValue))
                End If

                pairsRemainder.Add(pair)
                Continue For
            End If

            If Not predicate(pair.Key, pair.Value, state) Then
                Continue For
            End If

            If Not found Then
                found = True
                firstRemovedKey = pair.Key
            End If
        Next

        If found Then
            source.Remove(firstRemovedKey)
            If pairsRemainder Is Nothing Then
                Return
            End If

            For Each pair In pairsRemainder
                If predicate(pair.Key, pair.Value, state) Then
                    source.Remove(pair.Key)
                End If
            Next
        End If
    End Sub
End Module
