Imports System.Reflection
Imports Microsoft.EntityFrameworkCore.Diagnostics
Imports Microsoft.EntityFrameworkCore.Metadata
Imports Microsoft.EntityFrameworkCore.Metadata.Internal
Imports Xunit

Namespace Utilities

    Public Class MultigraphTest

#Region "Fixture"
        Private Class Vertex
            Public Property Id As Integer
            Public Overrides Function ToString() As String
                Return Id.ToString()
            End Function
        End Class

        Private Class Edge
            Public Property Id As Integer
            Public Overrides Function ToString() As String
                Return Id.ToString()
            End Function
        End Class

        Private Class A
            Public Shared ReadOnly PProperty As PropertyInfo = GetType(A).GetProperty("P")

            Public Property P As Integer
            Public Property P2 As Integer
        End Class

        Private Class B
            Public Shared ReadOnly PProperty As PropertyInfo = GetType(B).GetProperty("P")

            Public Property P As Integer
            Public Property P2 As Integer
        End Class

        Private Class C
            Public Shared ReadOnly PProperty As PropertyInfo = GetType(C).GetProperty("P")

            Public Property P As Integer
            Public Property P2 As Integer
        End Class

        Private Class D
            Public Shared ReadOnly PProperty As PropertyInfo = GetType(D).GetProperty("P")

            Public Property P As Integer
            Public Property P2 As Integer
        End Class

        Private Class E
            Public Shared ReadOnly PProperty As PropertyInfo = GetType(E).GetProperty("P")

            Public Property P As Integer
            Public Property P2 As Integer
        End Class

        Private Class EntityTypeGraph
            Inherits Multigraph(Of IReadOnlyEntityType, IReadOnlyForeignKey)

            Public Sub Populate(ParamArray entityTypes As IReadOnlyEntityType())

                AddVertices(entityTypes)

                For Each entityType In entityTypes
                    For Each foreignKey In entityType.GetForeignKeys()
                        AddEdge(foreignKey.PrincipalEntityType, foreignKey.DeclaringEntityType, foreignKey)
                    Next
                Next
            End Sub

            Protected Overrides Function ToString(vertex As IReadOnlyEntityType) As String
                Return vertex.DisplayName()
            End Function
        End Class
#End Region

        <ConditionalFact>
        Public Sub AddVertex_adds_a_vertex()
            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}

            Dim graph As New Multigraph(Of Vertex, Edge)()

            graph.AddVertex(vertexOne)
            graph.AddVertex(vertexTwo)

            Assert.Equal(2, graph.Vertices.Count())
            Assert.Equal(2, graph.Vertices.Intersect({vertexOne, vertexTwo}).Count())
        End Sub

        <ConditionalFact>
        Public Sub AddVertices_add_vertices()
            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}
            Dim vertexThree As New Vertex With {.Id = 3}

            Dim Graph As New Multigraph(Of Vertex, Edge)()

            Graph.AddVertices({vertexOne, vertexTwo})
            Graph.AddVertices({vertexTwo, vertexThree})

            Assert.Equal(3, Graph.Vertices.Count())
            Assert.Equal(3, Graph.Vertices.Intersect({vertexOne, vertexTwo, vertexThree}).Count())
        End Sub

        <ConditionalFact>
        Public Sub AddEdge_adds_an_edge()
            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}

            Dim edgeOne As New Edge With {.Id = 1}
            Dim edgeTwo As New Edge With {.Id = 2}

            Dim Graph As New Multigraph(Of Vertex, Edge)()
            Graph.AddVertices({vertexOne, vertexTwo})
            Graph.AddEdge(vertexOne, vertexTwo, edgeOne)
            Graph.AddEdge(vertexOne, vertexTwo, edgeTwo)

            Assert.Empty(Graph.GetEdges(vertexTwo, vertexOne))
            Assert.Equal(2, Graph.GetEdges(vertexOne, vertexTwo).Count())
            Assert.Equal(2, Graph.GetEdges(vertexOne, vertexTwo).Intersect({edgeOne, edgeTwo}).Count())
        End Sub

        <ConditionalFact>
        Public Sub AddEdge_updates_incoming_and_outgoing_neighbors()
            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}
            Dim vertexThree As New Vertex With {.Id = 3}

            Dim edgeOne As New Edge With {.Id = 1}
            Dim edgeTwo As New Edge With {.Id = 2}
            Dim edgeThree As New Edge With {.Id = 3}

            Dim graph As New Multigraph(Of Vertex, Edge)()
            graph.AddVertices({vertexOne, vertexTwo, vertexThree})
            graph.AddEdge(vertexOne, vertexTwo, edgeOne)
            graph.AddEdge(vertexOne, vertexThree, edgeTwo)
            graph.AddEdge(vertexTwo, vertexThree, edgeThree)

            Assert.Equal(2, graph.GetOutgoingNeighbors(vertexOne).Count())
            Assert.Equal(2, graph.GetOutgoingNeighbors(vertexOne).Intersect({vertexTwo, vertexThree}).Count())

            Assert.Equal(2, graph.GetIncomingNeighbors(vertexThree).Count())
            Assert.Equal(2, graph.GetIncomingNeighbors(vertexThree).Intersect({vertexOne, vertexTwo}).Count())
        End Sub

        <ConditionalFact>
        Public Sub TopologicalSort_on_graph_with_no_edges_returns_all_vertices()
            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}
            Dim vertexThree As New Vertex With {.Id = 3}

            Dim Graph As New Multigraph(Of Vertex, Edge)()
            Graph.AddVertices({vertexOne, vertexTwo, vertexThree})

            Dim result = Graph.TopologicalSort()
            Assert.Equal(3, result.Count())
            Assert.Equal(3, result.Intersect({vertexOne, vertexTwo, vertexThree}).Count())
        End Sub

        <ConditionalFact>
        Public Sub TopologicalSort_on_simple_graph_returns_all_vertices_in_order()
            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}
            Dim vertexThree As New Vertex With {.Id = 3}

            Dim edgeOne As New Edge With {.Id = 1}
            Dim edgeTwo As New Edge With {.Id = 2}

            Dim graph As New Multigraph(Of Vertex, Edge)()
            graph.AddVertices({vertexOne, vertexTwo, vertexThree})

            ' 2-> {1}
            graph.AddEdge(vertexTwo, vertexOne, edgeOne)
            ' 1 -> {3}
            graph.AddEdge(vertexOne, vertexThree, edgeTwo)

            Assert.Equal(
                {vertexTwo, vertexOne, vertexThree},
                graph.TopologicalSort().AsEnumerable())
        End Sub

        <ConditionalFact>
        Public Sub TopologicalSort_on_tree_graph_returns_all_vertices_in_order()
            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}
            Dim vertexThree As New Vertex With {.Id = 3}

            Dim edgeOne As New Edge With {.Id = 1}
            Dim edgeTwo As New Edge With {.Id = 2}
            Dim edgeThree As New Edge With {.Id = 3}

            Dim graph As New Multigraph(Of Vertex, Edge)()
            graph.AddVertices({vertexOne, vertexTwo, vertexThree})

            ' 1 -> {2, 3}
            graph.AddEdge(vertexOne, vertexTwo, edgeOne)
            graph.AddEdge(vertexOne, vertexThree, edgeTwo)
            ' 3 -> {2}
            graph.AddEdge(vertexThree, vertexTwo, edgeThree)

            Assert.Equal(
                {vertexOne, vertexThree, vertexTwo},
                graph.TopologicalSort().AsEnumerable())
        End Sub

        <ConditionalFact>
        Public Sub TopologicalSort_on_self_ref_can_break_cycle()
            Dim vertexOne As New Vertex With {.Id = 1}

            Dim edgeOne As New Edge With {.Id = 1}

            Dim graph As New Multigraph(Of Vertex, Edge)()
            graph.AddVertex(vertexOne)

            ' 1 -> {1}
            graph.AddEdge(vertexOne, vertexOne, edgeOne)

            Assert.Equal(
                {vertexOne},
                graph.TopologicalSort(
                    Function([from] As Vertex, [to] As Vertex, edges As IEnumerable(Of Edge))
                        Return [from] Is vertexOne AndAlso
                               [to] Is vertexOne AndAlso
                               edges.Intersect({edgeOne}).Count() = 1
                    End Function).AsEnumerable())
        End Sub

        <ConditionalFact>
        Public Sub TopologicalSort_can_break_simple_cycle()
            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}
            Dim vertexThree As New Vertex With {.Id = 3}

            Dim edgeOne As New Edge With {.Id = 1}
            Dim edgeTwo As New Edge With {.Id = 2}
            Dim edgeThree As New Edge With {.Id = 3}

            Dim graph As New Multigraph(Of Vertex, Edge)()
            graph.AddVertices({vertexOne, vertexTwo, vertexThree})

            ' 1 -> {2}
            graph.AddEdge(vertexOne, vertexTwo, edgeOne)
            ' 2 -> {3}
            graph.AddEdge(vertexTwo, vertexThree, edgeTwo)
            ' 3 -> {1}
            graph.AddEdge(vertexThree, vertexOne, edgeThree)

            Assert.Equal(
                {vertexOne, vertexTwo, vertexThree},
                graph.TopologicalSort(
                    Function([from], [to], edges)
                        Return ([from] Is vertexThree) AndAlso
                               ([to] Is vertexOne) AndAlso
                               (edges.Single() Is edgeThree)
                    End Function).AsEnumerable())
        End Sub

        <ConditionalFact>
        Public Sub TopologicalSort_can_break_two_cycles()
            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}
            Dim vertexThree As New Vertex With {.Id = 3}
            Dim vertexFour As New Vertex With {.Id = 4}
            Dim vertexFive As New Vertex With {.Id = 5}

            Dim edgeOne As New Edge With {.Id = 1}
            Dim edgeTwo As New Edge With {.Id = 2}
            Dim edgeThree As New Edge With {.Id = 3}
            Dim edgeFour As New Edge With {.Id = 4}
            Dim edgeFive As New Edge With {.Id = 5}
            Dim edgeSix As New Edge With {.Id = 6}

            Dim graph As New Multigraph(Of Vertex, Edge)()
            graph.AddVertices({vertexOne, vertexTwo, vertexThree, vertexFour, vertexFive})

            ' 1 -> {2, 4}
            graph.AddEdge(vertexOne, vertexTwo, edgeOne)
            graph.AddEdge(vertexOne, vertexFour, edgeTwo)
            ' 2 -> {3}
            graph.AddEdge(vertexTwo, vertexThree, edgeThree)
            ' 3 -> {1}
            graph.AddEdge(vertexThree, vertexOne, edgeFour)
            ' 4 -> {5}
            graph.AddEdge(vertexFour, vertexFive, edgeFive)
            ' 5 -> {1}
            graph.AddEdge(vertexFive, vertexOne, edgeSix)

            Assert.Equal(
                {vertexTwo, vertexThree, vertexOne, vertexFour, vertexFive},
                graph.TopologicalSort(
                    Function([from], [to], edges)
                        Dim edge = edges.Single()
                        Return (edge Is edgeOne) OrElse (edge Is edgeSix)
                    End Function).AsEnumerable())
        End Sub

        <ConditionalFact>
        Public Sub TopologicalSort_throws_with_default_message_when_cycle_cannot_be_broken()
            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}
            Dim vertexThree As New Vertex With {.Id = 3}

            Dim edgeOne As New Edge With {.Id = 1}
            Dim edgeTwo As New Edge With {.Id = 2}
            Dim edgeThree As New Edge With {.Id = 3}

            Dim graph As New Multigraph(Of Vertex, Edge)()
            graph.AddVertices({vertexOne, vertexTwo, vertexThree})

            ' 1 -> {2}
            graph.AddEdge(vertexOne, vertexTwo, edgeOne)
            ' 2 -> {3}
            graph.AddEdge(vertexTwo, vertexThree, edgeTwo)
            ' 3 -> {1}
            graph.AddEdge(vertexThree, vertexOne, edgeThree)

            Assert.Equal(
                CoreStrings.CircularDependency(
                    String.Join(
                        " ->" & Environment.NewLine, {vertexOne, vertexTwo, vertexThree, vertexOne}.Select(Function(v) v.ToString()))),
                Assert.Throws(Of InvalidOperationException)(Sub() graph.TopologicalSort()).Message)
        End Sub

        <ConditionalFact>
        Public Sub TopologicalSort_throws_with_formatted_message_when_cycle_cannot_be_broken()
            Const message As String = "Formatted cycle"

            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}
            Dim vertexThree As New Vertex With {.Id = 3}

            Dim edgeOne As New Edge With {.Id = 1}
            Dim edgeTwo As New Edge With {.Id = 2}
            Dim edgeThree As New Edge With {.Id = 3}

            Dim graph As New Multigraph(Of Vertex, Edge)()
            graph.AddVertices({vertexOne, vertexTwo, vertexThree})

            ' 1 -> {2}
            graph.AddEdge(vertexOne, vertexTwo, edgeOne)
            ' 2 -> {3}
            graph.AddEdge(vertexTwo, vertexThree, edgeTwo)
            ' 3 -> {1}
            graph.AddEdge(vertexThree, vertexOne, edgeThree)

            Dim cycleData As Dictionary(Of Vertex, Tuple(Of Vertex, Vertex, IEnumerable(Of Edge))) = Nothing

            Dim formatter = Function(data As IEnumerable(Of Tuple(Of Vertex, Vertex, IEnumerable(Of Edge))))
                                cycleData = data.ToDictionary(Function(entry) entry.Item1)
                                Return message
                            End Function

            Assert.Equal(
                CoreStrings.CircularDependency(message),
                Assert.Throws(Of InvalidOperationException)(Sub() graph.TopologicalSort(formatter)).Message)

            Assert.Equal(3, cycleData.Count())

            Assert.Equal(vertexTwo, cycleData(vertexOne).Item2)
            Assert.Equal({edgeOne}, cycleData(vertexOne).Item3)

            Assert.Equal(vertexThree, cycleData(vertexTwo).Item2)
            Assert.Equal({edgeTwo}, cycleData(vertexTwo).Item3)

            Assert.Equal(vertexOne, cycleData(vertexThree).Item2)
            Assert.Equal({edgeThree}, cycleData(vertexThree).Item3)
        End sub

        <ConditionalFact>
        Public Sub TopologicalSort_with_secondary_sort()
            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}
            Dim vertexThree As New Vertex With {.Id = 3}
            Dim vertexFour As New Vertex With {.Id = 4}

            Dim edgeOne As New Edge With {.Id = 1}
            Dim edgeTwo As New Edge With {.Id = 2}

            Dim graph As New Multigraph(Of Vertex, Edge)(Function(v1, v2) Comparer(Of Integer).Default.Compare(v1.Id, v2.Id))
            graph.AddVertices({vertexFour, vertexThree, vertexTwo, vertexOne})

            ' 1 -> {3}
            graph.AddEdge(vertexOne, vertexThree, edgeOne)
            ' 2 -> {4}
            graph.AddEdge(vertexTwo, vertexFour, edgeTwo)

            Assert.Equal(
                {vertexOne, vertexTwo, vertexThree, vertexFour},
                graph.TopologicalSort().AsEnumerable())
        End Sub

        <ConditionalFact>
        Public Sub TopologicalSort_without_secondary_sort()
            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}
            Dim vertexThree As New Vertex With {.Id = 3}
            Dim vertexFour As New Vertex With {.Id = 4}

            Dim edgeOne As New Edge With {.Id = 1}
            Dim edgeTwo As New Edge With {.Id = 2}

            Dim graph As New Multigraph(Of Vertex, Edge)()
            graph.AddVertices({vertexFour, vertexThree, vertexTwo, vertexOne})

            ' 1 -> {3}
            graph.AddEdge(vertexOne, vertexThree, edgeOne)
            ' 2 -> {4}
            graph.AddEdge(vertexTwo, vertexFour, edgeTwo)

            Assert.Equal(
                {vertexTwo, vertexOne, vertexFour, vertexThree},
                graph.TopologicalSort().AsEnumerable())
        End Sub

        <ConditionalFact>
        Public Sub BatchingTopologicalSort_throws_with_formatted_message_when_cycle_cannot_be_broken()
            Const message As String = "Formatted cycle"

            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}
            Dim vertexThree As New Vertex With {.Id = 3}

            Dim edgeOne As New Edge With {.Id = 1}
            Dim edgeTwo As New Edge With {.Id = 2}
            Dim edgeThree As New Edge With {.Id = 3}

            Dim graph As New Multigraph(Of Vertex, Edge)()
            graph.AddVertices({vertexOne, vertexTwo, vertexThree})

            ' 1 -> {2}
            graph.AddEdge(vertexOne, vertexTwo, edgeOne)
            ' 2 -> {3}
            graph.AddEdge(vertexTwo, vertexThree, edgeTwo)
            ' 3 -> {1}
            graph.AddEdge(vertexThree, vertexOne, edgeThree)

            Dim cycleData As Dictionary(Of Vertex, Tuple(Of Vertex, Vertex, IEnumerable(Of Edge))) = Nothing

            Dim formatter = Function(data As IEnumerable(Of Tuple(Of Vertex, Vertex, IEnumerable(Of Edge)))) As String
                                cycleData = data.ToDictionary(Function(entry) entry.Item1)
                                Return message
                            End Function

            Assert.Equal(
                CoreStrings.CircularDependency(message),
                Assert.Throws(Of InvalidOperationException)(Sub() graph.BatchingTopologicalSort(Nothing, formatter)).Message)

            Assert.Equal(3, cycleData.Count())

            Assert.Equal(vertexTwo, cycleData(vertexOne).Item2)
            Assert.Equal({edgeOne}, cycleData(vertexOne).Item3)

            Assert.Equal(vertexThree, cycleData(vertexTwo).Item2)
            Assert.Equal({edgeTwo}, cycleData(vertexTwo).Item3)

            Assert.Equal(vertexOne, cycleData(vertexThree).Item2)
            Assert.Equal({edgeThree}, cycleData(vertexThree).Item3)
        End Sub

        <ConditionalFact>
        Public Sub BatchingTopologicalSort_throws_with_formatted_message_with_no_tail_when_cycle_cannot_be_broken()

            Const message As String = "Formatted cycle"

            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}
            Dim vertexThree As New Vertex With {.Id = 3}
            Dim vertexFour As New Vertex With {.Id = 4}

            Dim edgeOne As New Edge With {.Id = 1}
            Dim edgeTwo As New Edge With {.Id = 2}
            Dim edgeThree As New Edge With {.Id = 3}
            Dim edgeFour As New Edge With {.Id = 4}

            Dim graph As New Multigraph(Of Vertex, Edge)()
            graph.AddVertices({vertexOne, vertexTwo, vertexThree, vertexFour})

            ' 2 -> {1}
            graph.AddEdge(vertexTwo, vertexOne, edgeOne)
            ' 3 -> {2}
            graph.AddEdge(vertexThree, vertexTwo, edgeTwo)
            ' 4 -> {3}
            graph.AddEdge(vertexFour, vertexThree, edgeThree)
            ' 3 -> {4}
            graph.AddEdge(vertexThree, vertexFour, edgeFour)

            Dim cycleData As Dictionary(Of Vertex, Tuple(Of Vertex, Vertex, IEnumerable(Of Edge))) = Nothing

            Dim formatter = Function(data As IEnumerable(Of Tuple(Of Vertex, Vertex, IEnumerable(Of Edge))))
                                cycleData = data.ToDictionary(Function(entry) entry.Item1)
                                Return message
                            End Function

            Assert.Equal(
                CoreStrings.CircularDependency(message),
                Assert.Throws(Of InvalidOperationException)(Sub() graph.BatchingTopologicalSort(Nothing, formatter)).Message)

            Assert.Equal(2, cycleData.Count)

            Assert.Equal(vertexFour, cycleData(vertexThree).Item2)
            Assert.Equal({edgeFour}, cycleData(vertexThree).Item3)

            Assert.Equal(vertexThree, cycleData(vertexFour).Item2)
            Assert.Equal({edgeThree}, cycleData(vertexFour).Item3)
        End Sub

        <ConditionalFact>
        Public Sub BatchingTopologicalSort_sorts_simple()
            Dim model = CreateModel()

            Dim entityTypeA = model.AddEntityType(GetType(A))
            entityTypeA.SetPrimaryKey(entityTypeA.AddProperty("Id", GetType(Integer)))

            Dim entityTypeB = model.AddEntityType(GetType(B))
            entityTypeB.SetPrimaryKey(entityTypeB.AddProperty("Id", GetType(Integer)))

            Dim entityTypeC = model.AddEntityType(GetType(C))
            entityTypeC.SetPrimaryKey(entityTypeC.AddProperty("Id", GetType(Integer)))

            ' B -> A -> C
            entityTypeC.AddForeignKey(entityTypeC.AddProperty(C.PProperty), entityTypeA.FindPrimaryKey(), entityTypeA)
            entityTypeA.AddForeignKey(entityTypeA.AddProperty(A.PProperty), entityTypeB.FindPrimaryKey(), entityTypeB)

            Dim graph As New EntityTypeGraph()
            graph.Populate(entityTypeA, entityTypeB, entityTypeC)

            Assert.Equal(
                {entityTypeB.Name, entityTypeA.Name, entityTypeC.Name},
                graph.BatchingTopologicalSort().SelectMany(Function(E) E).Select(Function(E) E.Name).AsEnumerable())
        End Sub

        <ConditionalFact>
        Public Sub BatchingTopologicalSort_sorts_reverse()
            Dim model = CreateModel()

            Dim entityTypeA = model.AddEntityType(GetType(A))
            entityTypeA.SetPrimaryKey(entityTypeA.AddProperty("Id", GetType(Integer)))

            Dim entityTypeB = model.AddEntityType(GetType(B))
            entityTypeB.SetPrimaryKey(entityTypeB.AddProperty("Id", GetType(Integer)))

            Dim entityTypeC = model.AddEntityType(GetType(C))
            entityTypeC.SetPrimaryKey(entityTypeC.AddProperty("Id", GetType(Integer)))

            ' C -> B -> A
            entityTypeA.AddForeignKey(entityTypeA.AddProperty(A.PProperty), entityTypeB.FindPrimaryKey(), entityTypeB)
            entityTypeB.AddForeignKey(entityTypeB.AddProperty(B.PProperty), entityTypeC.FindPrimaryKey(), entityTypeC)

            Dim graph As New EntityTypeGraph()
            graph.Populate(entityTypeA, entityTypeB, entityTypeC)

            Assert.Equal(
                {entityTypeC.Name, entityTypeB.Name, entityTypeA.Name},
                graph.BatchingTopologicalSort().SelectMany(Function(E) E).Select(Function(E) E.Name).AsEnumerable())
        End Sub

        <ConditionalFact>
        Public Sub BatchingTopologicalSort_sorts_preserves_graph()
            Dim model = CreateModel()

            Dim entityTypeA = model.AddEntityType(GetType(A))
            entityTypeA.SetPrimaryKey(entityTypeA.AddProperty("Id", GetType(Integer)))

            Dim entityTypeB = model.AddEntityType(GetType(B))
            entityTypeB.SetPrimaryKey(entityTypeB.AddProperty("Id", GetType(Integer)))

            Dim entityTypeC = model.AddEntityType(GetType(C))
            entityTypeC.SetPrimaryKey(entityTypeC.AddProperty("Id", GetType(Integer)))

            ' B -> A -> C
            entityTypeC.AddForeignKey(entityTypeC.AddProperty(C.PProperty), entityTypeA.FindPrimaryKey(), entityTypeA)
            entityTypeA.AddForeignKey(entityTypeA.AddProperty(A.PProperty), entityTypeB.FindPrimaryKey(), entityTypeB)

            Dim graph As New EntityTypeGraph()
            graph.Populate(entityTypeA, entityTypeB, entityTypeC)

            Assert.Equal(
                {entityTypeB.Name, entityTypeA.Name, entityTypeC.Name},
                graph.BatchingTopologicalSort().SelectMany(Function(E) E).Select(Function(E) E.Name).AsEnumerable())

            Assert.Equal(
                {entityTypeA, entityTypeB, entityTypeC},
                graph.Vertices)

            Assert.Equal(
                {entityTypeC},
                graph.GetOutgoingNeighbors(entityTypeA))

            Assert.Equal(
                {entityTypeA},
                graph.GetOutgoingNeighbors(entityTypeB))

            Assert.Equal(
                {entityTypeB.Name, entityTypeA.Name, entityTypeC.Name},
                graph.BatchingTopologicalSort().SelectMany(Function(E) E).Select(Function(E) E.Name).AsEnumerable())
        End Sub

        <ConditionalFact>
        Public Sub BatchingTopologicalSort_sorts_tree()
            Dim model = CreateModel()

            Dim entityTypeA = model.AddEntityType(GetType(A))
            entityTypeA.SetPrimaryKey(entityTypeA.AddProperty("Id", GetType(Integer)))

            Dim entityTypeB = model.AddEntityType(GetType(B))
            entityTypeB.SetPrimaryKey(entityTypeB.AddProperty("Id", GetType(Integer)))

            Dim entityTypeC = model.AddEntityType(GetType(C))
            entityTypeC.SetPrimaryKey(entityTypeC.AddProperty("Id", GetType(Integer)))

            ' A -> B, A -> C, C -> B
            entityTypeB.AddForeignKey(entityTypeB.AddProperty(B.PProperty), entityTypeA.FindPrimaryKey(), entityTypeA)
            entityTypeC.AddForeignKey(entityTypeC.AddProperty(C.PProperty), entityTypeA.FindPrimaryKey(), entityTypeA)
            entityTypeB.AddForeignKey(entityTypeB.AddProperty("P2", GetType(Integer)), entityTypeC.FindPrimaryKey(), entityTypeC)

            Dim graph As New EntityTypeGraph()
            graph.Populate(entityTypeA, entityTypeB, entityTypeC)

            Assert.Equal(
                {entityTypeA.Name, entityTypeC.Name, entityTypeB.Name},
                graph.BatchingTopologicalSort().SelectMany(Function(E) E).Select(Function(E) E.Name).AsEnumerable())
        End Sub

        <ConditionalFact>
        Public Sub BatchingTopologicalSort_sorts_no_edges()
            Dim model = CreateModel()

            Dim entityTypeA = model.AddEntityType(GetType(A))
            entityTypeA.SetPrimaryKey(entityTypeA.AddProperty("Id", GetType(Integer)))

            Dim entityTypeB = model.AddEntityType(GetType(B))
            entityTypeB.SetPrimaryKey(entityTypeB.AddProperty("Id", GetType(Integer)))

            Dim entityTypeC = model.AddEntityType(GetType(C))
            entityTypeC.SetPrimaryKey(entityTypeC.AddProperty("Id", GetType(Integer)))

            ' A B C
            Dim graph As New EntityTypeGraph()
            graph.Populate(entityTypeC, entityTypeA, entityTypeB)

            Assert.Equal(
                {entityTypeC.Name, entityTypeA.Name, entityTypeB.Name},
                graph.BatchingTopologicalSort().SelectMany(Function(E) E).Select(Function(E) E.Name).AsEnumerable())
        End Sub

        <ConditionalFact>
        Public Sub BatchingTopologicalSort_sorts_self_ref()
            Dim model = CreateModel()

            Dim entityTypeA = model.AddEntityType(GetType(A))
            Dim prop = entityTypeA.AddProperty("Id", GetType(Integer))
            entityTypeA.SetPrimaryKey(prop)

            ' A -> A
            entityTypeA.AddForeignKey(entityTypeA.AddProperty(A.PProperty), entityTypeA.FindPrimaryKey(), entityTypeA)

            Dim graph As New EntityTypeGraph()
            graph.Populate(entityTypeA)

            Assert.Equal(
                CoreStrings.CircularDependency(NameOf(A) & " ->" & Environment.NewLine & NameOf(A)),
                Assert.Throws(Of InvalidOperationException)(Sub() graph.BatchingTopologicalSort()).Message)
        End Sub

        <ConditionalFact>
        Public Sub BatchingTopologicalSort_sorts_circular_direct()
            Dim model = CreateModel()

            Dim entityTypeA = model.AddEntityType(GetType(A))
            entityTypeA.SetPrimaryKey(entityTypeA.AddProperty("Id", GetType(Integer)))

            Dim entityTypeB = model.AddEntityType(GetType(B))
            entityTypeB.SetPrimaryKey(entityTypeB.AddProperty("Id", GetType(Integer)))

            Dim entityTypeC = model.AddEntityType(GetType(C))
            entityTypeC.SetPrimaryKey(entityTypeC.AddProperty("Id", GetType(Integer)))

            ' C, A -> B -> A
            entityTypeA.AddForeignKey(entityTypeA.AddProperty(A.PProperty), entityTypeB.FindPrimaryKey(), entityTypeB)
            entityTypeB.AddForeignKey(entityTypeB.AddProperty(B.PProperty), entityTypeA.FindPrimaryKey(), entityTypeA)

            Dim graph As New EntityTypeGraph()
            graph.Populate(entityTypeC, entityTypeA, entityTypeB)

            Assert.Equal(
                CoreStrings.CircularDependency(
                    NameOf(A) & " ->" & Environment.NewLine & NameOf(B) & " ->" & Environment.NewLine & NameOf(A)),
                Assert.Throws(Of InvalidOperationException)(Sub() graph.BatchingTopologicalSort()).Message)
        End Sub

        <ConditionalFact>
        Public Sub BatchingTopologicalSort_sorts_circular_transitive()
            Dim model = CreateModel()

            Dim entityTypeA = model.AddEntityType(GetType(A))
            entityTypeA.SetPrimaryKey(entityTypeA.AddProperty("Id", GetType(Integer)))

            Dim entityTypeB = model.AddEntityType(GetType(B))
            entityTypeB.SetPrimaryKey(entityTypeB.AddProperty("Id", GetType(Integer)))

            Dim entityTypeC = model.AddEntityType(GetType(C))
            entityTypeC.SetPrimaryKey(entityTypeC.AddProperty("Id", GetType(Integer)))

            ' A -> C -> B -> A
            entityTypeA.AddForeignKey(entityTypeA.AddProperty(A.PProperty), entityTypeB.FindPrimaryKey(), entityTypeB)
            entityTypeB.AddForeignKey(entityTypeB.AddProperty(B.PProperty), entityTypeC.FindPrimaryKey(), entityTypeC)
            entityTypeC.AddForeignKey(entityTypeC.AddProperty(C.PProperty), entityTypeA.FindPrimaryKey(), entityTypeA)

            Dim graph As New EntityTypeGraph()
            graph.Populate(entityTypeA, entityTypeB, entityTypeC)

            Assert.Equal(
                CoreStrings.CircularDependency(
                    NameOf(A) &
                    " ->" &
                    Environment.NewLine & NameOf(C) &
                    " ->" & Environment.NewLine &
                    NameOf(B) & " ->" &
                    Environment.NewLine & NameOf(A)),
                Assert.Throws(Of InvalidOperationException)(Sub() graph.BatchingTopologicalSort()).Message)
        End sub

        <ConditionalFact>
        Public Sub BatchingTopologicalSort_sorts_two_cycles()
            Dim model = CreateModel()

            Dim entityTypeA = model.AddEntityType(GetType(A))
            entityTypeA.SetPrimaryKey(entityTypeA.AddProperty("Id", GetType(Integer)))

            Dim entityTypeB = model.AddEntityType(GetType(B))
            entityTypeB.SetPrimaryKey(entityTypeB.AddProperty("Id", GetType(Integer)))

            Dim entityTypeC = model.AddEntityType(GetType(C))
            entityTypeC.SetPrimaryKey(entityTypeC.AddProperty("Id", GetType(Integer)))

            Dim entityTypeD = model.AddEntityType(GetType(D))
            entityTypeD.SetPrimaryKey(entityTypeD.AddProperty("Id", GetType(Integer)))

            Dim entityTypeE = model.AddEntityType(GetType(E))
            entityTypeE.SetPrimaryKey(entityTypeE.AddProperty("Id", GetType(Integer)))

            ' A -> C -> B -> A
            entityTypeA.AddForeignKey(entityTypeA.AddProperty(A.PProperty), entityTypeB.FindPrimaryKey(), entityTypeB)
            entityTypeB.AddForeignKey(entityTypeB.AddProperty(B.PProperty), entityTypeC.FindPrimaryKey(), entityTypeC)
            entityTypeC.AddForeignKey(entityTypeC.AddProperty(C.PProperty), entityTypeA.FindPrimaryKey(), entityTypeA)

            ' A -> E -> D -> A
            entityTypeA.AddForeignKey(entityTypeA.AddProperty("P2", GetType(Integer)), entityTypeD.FindPrimaryKey(), entityTypeD)
            entityTypeD.AddForeignKey(entityTypeD.AddProperty("P2", GetType(Integer)), entityTypeE.FindPrimaryKey(), entityTypeE)
            entityTypeE.AddForeignKey(entityTypeE.AddProperty("P2", GetType(Integer)), entityTypeA.FindPrimaryKey(), entityTypeA)

            Dim graph As New EntityTypeGraph()
            graph.Populate(entityTypeA, entityTypeB, entityTypeC, entityTypeD, entityTypeE)

            Assert.Equal(
                CoreStrings.CircularDependency(
                    NameOf(A) & " ->" &
                    Environment.NewLine &
                    NameOf(C) & " ->" &
                    Environment.NewLine &
                    NameOf(B) & " ->" &
                    Environment.NewLine &
                    NameOf(A)),
                Assert.Throws(Of InvalidOperationException)(Sub() graph.BatchingTopologicalSort()).Message)
        End Sub

        <ConditionalFact>
        Public Sub BatchingTopologicalSort_sorts_leafy_cycle()
            Dim model = CreateModel()

            Dim entityTypeA = model.AddEntityType(GetType(A))
            entityTypeA.SetPrimaryKey(entityTypeA.AddProperty("Id", GetType(Integer)))

            Dim entityTypeB = model.AddEntityType(GetType(B))
            entityTypeB.SetPrimaryKey(entityTypeB.AddProperty("Id", GetType(Integer)))

            Dim entityTypeC = model.AddEntityType(GetType(C))
            entityTypeC.SetPrimaryKey(entityTypeC.AddProperty("Id", GetType(Integer)))

            ' C -> B -> C -> A
            entityTypeB.AddForeignKey(entityTypeB.AddProperty(B.PProperty), entityTypeC.FindPrimaryKey(), entityTypeC)
            entityTypeC.AddForeignKey(entityTypeC.AddProperty(C.PProperty), entityTypeB.FindPrimaryKey(), entityTypeB)
            entityTypeA.AddForeignKey(entityTypeA.AddProperty(A.PProperty), entityTypeC.FindPrimaryKey(), entityTypeC)

            Dim graph As New EntityTypeGraph()
            graph.Populate(entityTypeA, entityTypeB, entityTypeC)

            Assert.Equal(
                CoreStrings.CircularDependency(
                    NameOf(C) & " ->" & Environment.NewLine & NameOf(B) & " ->" & Environment.NewLine & NameOf(C)),
                Assert.Throws(Of InvalidOperationException)(Sub() graph.BatchingTopologicalSort()).Message)
        End Sub

        <ConditionalFact>
        Public Sub batchingtopologicalsort_with_secondary_sort()
            Dim vertexone As New Vertex With {.Id = 1}
            Dim vertextwo As New Vertex With {.Id = 2}
            Dim vertexthree As New Vertex With {.Id = 3}
            Dim vertexfour As New Vertex With {.Id = 4}

            Dim edgeone As New Edge With {.Id = 1}
            Dim edgetwo As New Edge With {.Id = 2}

            Dim graph As New Multigraph(Of Vertex, Edge)(Function(v1, v2) Comparer(Of Integer).Default.Compare(v1.Id, v2.Id))
            graph.AddVertices({vertexfour, vertexthree, vertextwo, vertexone})

            ' 1 -> {3}
            graph.AddEdge(vertexone, vertexthree, edgeone)
            ' 2 -> {4}
            graph.AddEdge(vertextwo, vertexfour, edgetwo)

            Assert.Equal(
                {vertexone, vertextwo, vertexthree, vertexfour},
                graph.BatchingTopologicalSort().Single().AsEnumerable())
        End Sub

        <ConditionalFact>
        Public Sub BatchingTopologicalSort_without_secondary_sort()
            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}
            Dim vertexThree As New Vertex With {.Id = 3}
            Dim vertexFour As New Vertex With {.Id = 4}

            Dim edgeOne As New Edge With {.Id = 1}
            Dim edgeTwo As New Edge With {.Id = 2}

            Dim graph As New Multigraph(Of Vertex, Edge)()
            graph.AddVertices({vertexFour, vertexThree, vertexTwo, vertexOne})

            ' 1 -> {3}
            graph.AddEdge(vertexOne, vertexThree, edgeOne)
            ' 2 -> {4}
            graph.AddEdge(vertexTwo, vertexFour, edgeTwo)

            Assert.Equal(
                {vertexTwo, vertexOne, vertexFour, vertexThree},
                graph.BatchingTopologicalSort().Single().AsEnumerable())
        End Sub

        <ConditionalFact>
        Public Sub BatchingTopologicalSort_with_batching_boundary_edge()
            Dim vertexOne As New Vertex With {.Id = 1}
            Dim vertexTwo As New Vertex With {.Id = 2}
            Dim vertexThree As New Vertex With {.Id = 3}
            Dim vertexFour As New Vertex With {.Id = 4}

            Dim edgeOne As New Edge With {.Id = 1}
            Dim edgeTwo As New Edge With {.Id = 2}

            Dim graph As New Multigraph(Of Vertex, Edge)(Function(v1, v2) Comparer(Of Integer).Default.Compare(v1.Id, v2.Id))
            graph.AddVertices({vertexFour, vertexThree, vertexTwo, vertexOne})

            ' 1 -> {3}
            graph.AddEdge(vertexOne, vertexThree, edgeOne, requiresBatchingBoundary:=True)
            ' 2 -> {4}
            graph.AddEdge(vertexTwo, vertexFour, edgeTwo)

            Dim batches = graph.BatchingTopologicalSort()

            Assert.Collection(
                batches,
                Sub(B) Assert.Equal({vertexOne, vertexTwo}, B.AsEnumerable()),
                Sub(B) Assert.Equal({vertexThree, vertexFour}, B.AsEnumerable()))
        End Sub

        Private Shared Function CreateModel() As IMutableModel
            Return New Model()
        End Function
    End Class
End Namespace
