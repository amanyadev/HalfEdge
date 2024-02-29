using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace DataStructure.HalfEdge
{
    /// <summary>
    /// Extension class for HalfEdgeData, contains methods for adding and removing vertices and edges to Layout and Graph
    /// </summary>
    public static class HalfEdgeE
    {
     
        // ** NOT USED **
        // Takes half-edge data as input, 
        // the half-edge data must have previous and next edges assigned to each edge
        // otherwise it might not work as expected
        private static void PopulateFaces(this HalfEdgeData2 data)
        {
            foreach (HalfEdge2 halfEdge in data.edges)
            {
                // Check if the face has been already processed
                if (halfEdge.Face == null)
                {
                    // Start a new face
                    HalfEdgeFace2 face = new HalfEdgeFace2(halfEdge);
                    HashSet<HalfEdge2> visitedEdges = new HashSet<HalfEdge2>();
                    visitedEdges.Add(halfEdge);
                    // halfEdge.face = face;

                    face.Id = data.faces.Count + 1;

                    HalfEdge2 currentEdge = halfEdge.NextEdge;

                    // Traverse the incident edges to form the face
                    while (currentEdge != null && !currentEdge.Equals(halfEdge)) // TODO: Handle null: for current edge
                    {
                        // currentEdge.face = face;
                        // currentEdge.face = face;
                        visitedEdges.Add(currentEdge);
                        currentEdge = currentEdge.NextEdge;
                    }

                    // Check if the loop is closed
                    if (currentEdge != null && currentEdge.Equals(halfEdge))
                    {
                        HalfEdgeFace2 f = visitedEdges.AssignNewFace();
                        data.faces.Add(face);
                    }
                }
            }
        }

        // Assigns a new face to hashset of half edges, returns the face
        private static HalfEdgeFace2 AssignNewFace(this HashSet<HalfEdge2> halfEdge2s)
        {
            if (halfEdge2s.Count < 1)
            {
                Debug.Log("Cannot create face from less than 1 edges");
                return null;
            }

            HalfEdgeFace2 face = new HalfEdgeFace2(halfEdge2s.First());
            foreach (HalfEdge2 halfEdge2 in halfEdge2s)
            {
                halfEdge2.SetFace(face);
            }

            return face;
        }


        private static void PrintFaceInformation(this HalfEdgeData2 data)
        {
            Debug.Log("Printing face information");
            Debug.Log("Number of faces: " + data.faces.Count);

            foreach (HalfEdgeFace2 face in data.faces)
            {
                Debug.Log(face);
            }
        }

        // Populates the next and prev edges for each edge in the half-edge data structure
        public static void PopulateNextAndPreviousEdges(this HalfEdgeData2 data)
        {
            HashSet<HalfEdge2> visitedEdges = new HashSet<HalfEdge2>();
            // traverse edges clockwise and add prev and next
            foreach (HalfEdge2 i_edge in data.edges)
            {
                // first assign next
                // get the vertex of the twin
                HalfEdgeVertex2 vertex_next = i_edge.Twin.origin;
                // get outgoing edges from vertex
                IEnumerable<HalfEdge2> outgoing_edges_with_no_face =
                    data.edges.Where(e => e.origin.Equals(vertex_next) && !e.Equals(i_edge.Twin) && !visitedEdges.Contains(e)).ToList();

                Debug.Log($"Outgoing edges for edge {i_edge} are " + outgoing_edges_with_no_face.Count());
                // string
                // get the edge that is clockwise to the current edge
                if (outgoing_edges_with_no_face.Any())
                {
                    HalfEdge2 next_edge = GetNextEdge(i_edge, outgoing_edges_with_no_face.ToList(), Direction.Right);
                    i_edge.SetNextEdge(next_edge);
                    visitedEdges.Add(next_edge);
                    next_edge.SetPrevEdge(i_edge);
                }
            }
        }

        // Adds vertices and edges to the half-edge data structure from the layout adjacency list


        /// <summary>
        ///Only creates a half-edge pair from two vertices, returns the half-edges with twins assigned
        /// doesn't add the half-edges to the half-edge data structure 
        ///doesn't add prev and next edges
        /// </summary>
        /// <param name="hV">First vertex</param>
        /// <param name="hVNext">Second vertex</param>
        /// <param name="h_e">half-edge First->Second</param>
        /// <param name="he_twin">half-edge Second->First</param>
        public static void CreateHalfEdgePair(HalfEdgeData2 data,HalfEdgeVertex2 hV, HalfEdgeVertex2 hVNext, out HalfEdge2 h_e, out HalfEdge2 he_twin)
        {
            h_e = data.CreateNewEdge(hV);
            he_twin = data.CreateNewEdge(hVNext);

            h_e.SetTwin(he_twin);
            he_twin.SetTwin(h_e);
        }
        
        public static HalfEdge2 GetNextEdge(this HalfEdgeData2 data2, HalfEdge2 edge, Direction direction, HashSet<HalfEdge2> excludeEdges = null,
            List<int> includedSections = null)
        {
            IEnumerable<HalfEdge2> siblings = data2.GetSiblingOutgoingEdges(edge.Twin, excludeEdges);

            if (includedSections != null)
            {
                siblings = siblings.Where(e => includedSections.Contains(e.Face.edge.origin.Id) && includedSections.Contains(e.Face.edge.Twin.origin.Id));
            }

            if (siblings.Any())
            {
                return GetNextEdge(edge, siblings.ToList(), direction);
            }
            else
            {
                return null;
            }
        }


        // Turn right to find the next edge pass sibling edges
        public enum Direction
        {
            Right,
            Left
        }

        public static HalfEdge2 GetNextEdge(this HalfEdge2 edge, List<HalfEdge2> edges, Direction direction)
        {
            if (edges.Count == 1)
            {
                // Debug.Log($"immediate {direction.ToString().ToLower()} for {edge} to {edges[0]}");
                return edges[0];
            }

            // Get the reference direction from the edge
            MyVector2 referenceDirection = edge.GetDirection(); // Normalize the reference direction

            HalfEdge2 immediateEdge = null;
            double smallestSignedAngle = double.MaxValue;

            foreach (HalfEdge2 nextEdge in edges)
            {
                MyVector2 nextEdgeDirection = nextEdge.GetDirection();

                // Calculate the signed angle between the reference direction and the next edge direction using cross product
                double signedAngle = Mathf.Atan2(referenceDirection.x * nextEdgeDirection.y - referenceDirection.y * nextEdgeDirection.x,
                    referenceDirection.x * nextEdgeDirection.x + referenceDirection.y * nextEdgeDirection.y);

                // Ensure the signed angle is in the range [-PI, PI)
                signedAngle = (signedAngle + Mathf.PI) % (2 * Mathf.PI) - Mathf.PI;

                // Use switch case for better readability
                switch (direction)
                {
                    case Direction.Right:
                        if (signedAngle < smallestSignedAngle)
                        {
                            smallestSignedAngle = signedAngle;
                            immediateEdge = nextEdge;
                        }

                        break;

                    case Direction.Left:
                        if (signedAngle > -smallestSignedAngle)
                        {
                            smallestSignedAngle = -signedAngle;
                            immediateEdge = nextEdge;
                        }

                        break;
                }
            }

            if (immediateEdge != null)
            {
                // Debug.Log($"immediate {direction.ToString().ToLower()} for {edge} to {immediateEdge}");
                return immediateEdge;
            }
            else
            {
                // No valid immediate edge found
                // Debug.Log($"No valid immediate {direction.ToString().ToLower()} edge found for {edge}");
                return null;
            }
        }

        

        // Setting next and previous edges for each edge, using Clockwise traversal -> Direction.Right,
        // for changing the direction we can use Direction.Left
        // NOTE: No Face information is modified or set over here, don't set faces here.
        public static void PopulateNextAndPreviousEdges(HalfEdgeData2 halfEdgeData, HalfEdge2 he1, HalfEdge2 he2)
        {
            // find next edge
            HalfEdge2 next = halfEdgeData.GetNextEdge(he1, HalfEdgeE.Direction.Right);
            if (next != null)
            {
                he1.SetNextEdge(next);
                next.SetPrevEdge(he1);
                Debug.Log($"Found next edge for {he1}: {next}");
            }
            else
            {
                he1.SetNextEdge(he2);
                he2.SetPrevEdge(he1);
            }

            HalfEdge2 twin_next = halfEdgeData.GetNextEdge(he2, HalfEdgeE.Direction.Right);

            if (twin_next != null)
            {
                he2.SetNextEdge(twin_next);
                twin_next.SetPrevEdge(he2);
                Debug.Log($"Found next edge for {he2}: {twin_next}");
            }
            else
            {
                he2.SetNextEdge(he1);
                he1.SetPrevEdge(he2);
            }

            //find prev edge // TODO: can be done faster without for loop similar to next edge
            foreach (HalfEdge2 incomingEdge in halfEdgeData.GetSiblingIncomingEdges(he1))
            {
                if (halfEdgeData.GetNextEdge(incomingEdge, HalfEdgeE.Direction.Right) == he1)
                {
                    incomingEdge.SetNextEdge(he1);
                    he1.SetPrevEdge(incomingEdge);
                    // Debug.Log($"Found prev edge for {he1}: {incomingEdge}");
                }
            }

            if (he1.PrevEdge == null)
            {
                he1.SetPrevEdge(he2);
                he2.SetNextEdge(he1);
            }

            foreach (HalfEdge2 incomingEdge in halfEdgeData.GetSiblingIncomingEdges(he2))
            {
                if (halfEdgeData.GetNextEdge(incomingEdge, HalfEdgeE.Direction.Right) == he2)
                {
                    incomingEdge.SetNextEdge(he2);
                    he2.SetPrevEdge(incomingEdge);
                    // Debug.Log($"Found prev edge for {he2}: {incomingEdge}");
                }
            }

            if (he2.PrevEdge == null)
            {
                he2.SetPrevEdge(he1);
                he1.SetNextEdge(he2);
            }
        }

        public static List<HalfEdge2> GetEdgesLooped(this HalfEdge2 edge2)
        {
            if (edge2 == null) return null;
            if (edge2.NextEdge.Equals(edge2.Twin) && edge2.Twin.NextEdge.Equals(edge2))
                return new List<HalfEdge2> { edge2, edge2.Twin };


            HalfEdge2 startEdge = edge2;
            List<HalfEdge2> edges = new();
            int counter = 1024;
            do
            {
                edges.Add(edge2);
                edge2 = edge2.NextEdge;
                counter--;
            } while (edge2 != startEdge && counter > 0);

            return edges;
        }

        public static HalfEdgeFace2 GetFaceLooped(this HalfEdge2 edge2)
        {
            if (edge2 == null) return null;
            HalfEdge2 startEdge = edge2;

            int lim = 1024;
            while (!edge2.NextEdge.Equals(startEdge) && lim > 0)
            {
                lim--;
                edge2 = edge2.NextEdge;
                if (edge2.Face != null)
                    return edge2.Face;
            }

            return null;
        }

        public static HalfEdgeFace2 GetExistingFaceLooped(this HalfEdge2 edge2)
        {
            if (edge2 == null)
                return null;
            if (edge2.NextEdge.Equals(edge2.Twin) && edge2.Twin.NextEdge.Equals(edge2))
                return null;

            HalfEdge2 startEdge = edge2;
            if (edge2.Face != null)
                return edge2.Face;
            int lim = 1024;
            while (edge2.NextEdge != startEdge && lim > 0)
            {
                lim--;
                if(edge2 == null)
                {
                    return null;
                }
                edge2 = edge2.NextEdge;
                if (edge2.Face != null)
                    return edge2.Face;
            }

            return null;
        }
    }
}