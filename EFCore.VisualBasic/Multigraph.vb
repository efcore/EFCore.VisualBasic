Imports Microsoft.EntityFrameworkCore.Diagnostics

Namespace Utilities
    Friend Class Multigraph(Of TVertex, TEdge)
        Inherits Graph(Of TVertex)

        Private ReadOnly _vertices As New HashSet(Of TVertex)()
        Private ReadOnly _successorMap As New Dictionary(Of TVertex, Dictionary(Of TVertex, Object))()
        Private ReadOnly _predecessorMap As New Dictionary(Of TVertex, HashSet(Of TVertex))()

        Public Function GetEdges(from As TVertex, [to] As TVertex) As IEnumerable(Of TEdge)
            Dim successorSet As Dictionary(Of TVertex, Object) = Nothing
            If _successorMap.TryGetValue(from, successorSet) Then
                Dim edges As Object = Nothing
                If successorSet.TryGetValue([to], edges) Then
                    Return If(TypeOf edges Is IEnumerable(Of TEdge),
                                DirectCast(edges, IEnumerable(Of TEdge)),
                                {DirectCast(edges, TEdge)})
                End If
            End If

            Return Enumerable.Empty(Of TEdge)()
        End Function

        Public Sub AddVertex(vertex As TVertex)
            Call _vertices.Add(vertex)
        End Sub

        Public Sub AddVertices(vertices As IEnumerable(Of TVertex))
            Call _vertices.UnionWith(vertices)
        End Sub

        Public Sub AddEdge(from As TVertex, [to] As TVertex, edge As TEdge)
            Dim successorEdges As Dictionary(Of TVertex, Object) = Nothing
            If Not _successorMap.TryGetValue(from, successorEdges) Then
                successorEdges = New Dictionary(Of TVertex, Object)
                _successorMap.Add(from, successorEdges)
            End If

            Dim edges As Object = Nothing
            If successorEdges.TryGetValue([to], edges) Then

                Dim edgeList As List(Of TEdge)
                If TypeOf edges Is List(Of TEdge) Then
                    edgeList = DirectCast(edges, List(Of TEdge))
                Else
                    edgeList = New List(Of TEdge) From {DirectCast(edges, TEdge)}
                    successorEdges([to]) = edgeList
                End If

                edgeList.Add(edge)
            Else
                successorEdges.Add([to], edge)
            End If

            Dim predecessors As HashSet(Of TVertex) = Nothing
            If Not _predecessorMap.TryGetValue([to], predecessors) Then
                predecessors = New HashSet(Of TVertex)
                _predecessorMap.Add([to], predecessors)
            End If

            predecessors.Add(from)
        End Sub

        Public Overrides Sub Clear()
            _vertices.Clear()
            _successorMap.Clear()
            _predecessorMap.Clear()
        End Sub

        Public Function TopologicalSort() As IReadOnlyList(Of TVertex)
            Return TopologicalSort(Nothing, Nothing)
        End Function

        Public Function TopologicalSort(
            tryBreakEdge As Func(Of TVertex, TVertex, IEnumerable(Of TEdge), Boolean)) As IReadOnlyList(Of TVertex)
            Return TopologicalSort(tryBreakEdge, Nothing)
        End Function

        Public Function TopologicalSort(
            formatCycle As Func(Of IEnumerable(Of Tuple(Of TVertex, TVertex, IEnumerable(Of TEdge))), String)) As IReadOnlyList(Of TVertex)
            Return TopologicalSort(Nothing, formatCycle)
        End Function

        Public Function TopologicalSort(
            tryBreakEdge As Func(Of TVertex, TVertex, IEnumerable(Of TEdge), Boolean),
            formatCycle As Func(Of IReadOnlyList(Of Tuple(Of TVertex, TVertex, IEnumerable(Of TEdge))), String),
            Optional formatException As Func(Of String, String) = Nothing) As IReadOnlyList(Of TVertex)

            Dim queue As New List(Of TVertex)
            Dim predecessorCounts As New Dictionary(Of TVertex, Integer)(_predecessorMap.Count)

            For Each predecessor In _predecessorMap
                predecessorCounts(predecessor.Key) = predecessor.Value.Count
            Next

            For Each vertex In _vertices
                If Not predecessorCounts.ContainsKey(vertex) Then
                    queue.Add(vertex)
                End If
            Next

            Dim index = 0
            While queue.Count < _vertices.Count
                While index < queue.Count
                    Dim currentRoot = queue(index)
                    index += 1

                    For Each successor In GetOutgoingNeighbors(currentRoot)
                        predecessorCounts(successor) -= 1
                        If predecessorCounts(successor) = 0 Then
                            queue.Add(successor)
                        End If
                    Next
                End While

                ' Cycle breaking
                If queue.Count < _vertices.Count Then
                    Dim broken = False

                    Dim candidateVertices = predecessorCounts.Keys.ToList()
                    Dim candidateIndex = 0

                    While candidateIndex < candidateVertices.Count AndAlso
                          Not broken AndAlso
                          tryBreakEdge IsNot Nothing

                        Dim candidateVertex = candidateVertices(candidateIndex)
                        If predecessorCounts(candidateVertex) = 0 Then
                            candidateIndex += 1
                            Continue While
                        End If

                        ' Find a vertex in the unsorted portion of the graph that has edges to the candidate
                        Dim incomingNeighbor = GetIncomingNeighbors(candidateVertex).
                            First(Function(neighbor)
                                      Dim neighborPredecessors = 0
                                      Return predecessorCounts.TryGetValue(neighbor, neighborPredecessors) AndAlso
                                                neighborPredecessors > 0
                                  End Function)

                        If tryBreakEdge(incomingNeighbor, candidateVertex, GetEdges(incomingNeighbor, candidateVertex)) Then
                            _successorMap(incomingNeighbor).Remove(candidateVertex)
                            _predecessorMap(candidateVertex).Remove(incomingNeighbor)
                            predecessorCounts(candidateVertex) -= 1
                            If predecessorCounts(candidateVertex) = 0 Then
                                queue.Add(candidateVertex)
                                broken = True
                            End If
                            Continue While
                        End If

                        candidateIndex += 1
                    End While

                    If broken Then
                        Continue While
                    End If

                    Dim currentCycleVertex = _vertices.First(Function(v)
                                                                 Dim predecessorCount = 0
                                                                 Return predecessorCounts.TryGetValue(v, predecessorCount) AndAlso
                                                                        predecessorCount <> 0
                                                             End Function)
                    Dim cycle As New List(Of TVertex) From {currentCycleVertex}
                    Dim finished = False

                    While Not finished
                        For Each predecessor In GetIncomingNeighbors(currentCycleVertex)
                            Dim predecessorCount = 0
                            If Not predecessorCounts.TryGetValue(predecessor, predecessorCount) OrElse
                                predecessorCount = 0 Then

                                Continue For
                            End If

                            predecessorCounts(currentCycleVertex) = -1

                            currentCycleVertex = predecessor
                            cycle.Add(currentCycleVertex)
                            finished = predecessorCounts(predecessor) = -1
                            Exit For
                        Next
                    End While

                    cycle.Reverse()

                    ' Remove any tail that's not part of the cycle
                    Dim startingVertex = cycle(0)
                    For i = cycle.Count - 1 To 0 Step -1
                        If cycle(i).Equals(startingVertex) Then
                            Exit For
                        End If

                        cycle.RemoveAt(i)
                    Next

                    ThrowCycle(cycle, formatCycle, formatException)
                End If
            End While

            Return queue
        End Function

        Private Sub ThrowCycle(cycle As List(Of TVertex),
                               formatCycle As Func(Of IReadOnlyList(Of Tuple(Of TVertex, TVertex, IEnumerable(Of TEdge))), String),
                               Optional formatException As Func(Of String, String) = Nothing)

            Dim cycleString As String
            If formatCycle Is Nothing Then
                cycleString = cycle.Select(AddressOf ToString).Join(" ->" & Environment.NewLine)
            Else
                Dim currentCycleVertex = cycle.First()
                Dim cycleData As New List(Of Tuple(Of TVertex, TVertex, IEnumerable(Of TEdge)))

                For Each vertex As TVertex In cycle.Skip(1)
                    cycleData.Add(Tuple.Create(currentCycleVertex, vertex, GetEdges(currentCycleVertex, vertex)))
                    currentCycleVertex = vertex
                Next

                cycleString = formatCycle(cycleData)
            End If

            Dim message = If(formatException Is Nothing, CoreStrings.CircularDependency(cycleString), formatException(cycleString))
            Throw New InvalidOperationException(message)
        End Sub

        Protected Overridable Overloads Function ToString(vertex As TVertex) As String
            Return vertex.ToString()
        End Function

        Public Function BatchingTopologicalSort() As IReadOnlyList(Of List(Of TVertex))
            Return BatchingTopologicalSort(Nothing, Nothing)
        End Function

        Public Function BatchingTopologicalSort(
            tryBreakEdge As Func(Of TVertex, TVertex, IEnumerable(Of TEdge), Boolean),
            formatCycle As Func(Of IReadOnlyList(Of Tuple(Of TVertex, TVertex, IEnumerable(Of TEdge))), String)) As IReadOnlyList(Of List(Of TVertex))

            Dim currentRootsQueue As New List(Of TVertex)
            Dim predecessorCounts As New Dictionary(Of TVertex, Integer)(_predecessorMap.Count)

            For Each predecessor In _predecessorMap
                predecessorCounts(predecessor.Key) = predecessor.Value.Count
            Next

            For Each vertex In _vertices
                If Not predecessorCounts.ContainsKey(vertex) Then
                    currentRootsQueue.Add(vertex)
                End If
            Next

            Dim result As New List(Of List(Of TVertex))
            Dim nextRootsQueue As New List(Of TVertex)

            While result.Sum(Function(b) b.Count) <> _vertices.Count

                Dim currentRootIndex = 0

                While currentRootIndex < currentRootsQueue.Count
                    Dim currentRoot = currentRootsQueue(currentRootIndex)
                    currentRootIndex += 1

                    For Each successor In GetOutgoingNeighbors(currentRoot)
                        predecessorCounts(successor) -= 1
                        If predecessorCounts(successor) = 0 Then
                            nextRootsQueue.Add(successor)
                        End If
                    Next

                    ' Roll lists over for next batch
                    If currentRootIndex = currentRootsQueue.Count Then
                        result.Add(currentRootsQueue)

                        currentRootsQueue = nextRootsQueue
                        currentRootIndex = 0

                        If currentRootsQueue.Count <> 0 Then
                            nextRootsQueue = New List(Of TVertex)
                        End If
                    End If
                End While

                ' Cycle breaking
                If result.Sum(Function(b) b.Count) <> _vertices.Count Then

                    Dim broken = False

                    Dim candidateVertices = predecessorCounts.Keys.ToList()
                    Dim candidateIndex = 0

                    While candidateIndex < candidateVertices.Count AndAlso
                          Not broken AndAlso
                          tryBreakEdge IsNot Nothing

                        Dim candidateVertex = candidateVertices(candidateIndex)
                        If predecessorCounts(candidateVertex) = 0 Then
                            candidateIndex += 1
                            Continue While
                        End If

                        ' Find a vertex in the unsorted portion of the graph that has edges to the candidate
                        Dim incomingNeighbor = GetIncomingNeighbors(candidateVertex).First(
                                Function(neighbor)
                                    Dim neighborPredecessors = 0
                                    Return predecessorCounts.TryGetValue(neighbor, neighborPredecessors) AndAlso
                                           neighborPredecessors > 0
                                End Function)

                        If tryBreakEdge(incomingNeighbor, candidateVertex, GetEdges(incomingNeighbor, candidateVertex)) Then

                            _successorMap(incomingNeighbor).Remove(candidateVertex)
                            _predecessorMap(candidateVertex).Remove(incomingNeighbor)
                            predecessorCounts(candidateVertex) -= 1

                            If predecessorCounts(candidateVertex) = 0 Then
                                currentRootsQueue.Add(candidateVertex)
                                nextRootsQueue = New List(Of TVertex)
                                broken = True
                            End If
                            Continue While
                        End If

                        candidateIndex += 1
                    End While

                    If broken Then
                        Continue While
                    End If

                    Dim currentCycleVertex = _vertices.First(
                                        Function(v)
                                            Dim predecessorCount = 0
                                            Return predecessorCounts.TryGetValue(v, predecessorCount) AndAlso
                                                   predecessorCount <> 0
                                        End Function)
                    Dim cycle As New List(Of TVertex) From {currentCycleVertex}
                    Dim finished = False
                    While Not finished
                        For Each predecessor In GetIncomingNeighbors(currentCycleVertex)
                            Dim predecessorCount = 0
                            If Not predecessorCounts.TryGetValue(predecessor, predecessorCount) OrElse
                               predecessorCount = 0 Then
                                Continue For
                            End If

                            predecessorCounts(currentCycleVertex) = -1

                            currentCycleVertex = predecessor
                            cycle.Add(currentCycleVertex)
                            finished = predecessorCounts(predecessor) = -1
                            Exit For
                        Next
                    End While

                    cycle.Reverse()

                    ' Remove any tail that's not part of the cycle
                    Dim startingVertex = cycle(0)
                    For i = cycle.Count - 1 To 0 Step -1
                        If cycle(i).Equals(startingVertex) Then
                            Exit For
                        End If

                        cycle.RemoveAt(i)
                    Next

                    ThrowCycle(cycle, formatCycle)
                End If
            End While

            Return result
        End Function

        Public Overrides ReadOnly Property Vertices As IEnumerable(Of TVertex)
            Get
                Return _vertices
            End Get
        End Property

        Public Overrides Function GetOutgoingNeighbors(from As TVertex) As IEnumerable(Of TVertex)
            Dim successorSet As Dictionary(Of TVertex, Object) = Nothing
            Return If(_successorMap.TryGetValue(from, successorSet),
                        successorSet.Keys,
                        Enumerable.Empty(Of TVertex)())
        End Function

        Public Overrides Function GetIncomingNeighbors([to] As TVertex) As IEnumerable(Of TVertex)
            Dim predecessors As HashSet(Of TVertex) = Nothing
            Return If(_predecessorMap.TryGetValue([to], predecessors),
                        predecessors,
                        Enumerable.Empty(Of TVertex)())
        End Function
    End Class
End Namespace
