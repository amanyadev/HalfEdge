using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DataStructure.HalfEdge
{
    public static class HalfEdgeData2Extensions
    {
        public static MyVector2 GetDirection(this HalfEdge2 edge)
        {
            if(edge.Twin == null || edge.Twin.origin == null)
            {
                Debug.Log("Has no twin");
                return new MyVector2(0, 0);
            }
            var dir =  (edge.Twin.origin.position - edge.origin.position);
            Vector2 normalized = new Vector2(dir.x, dir.y).normalized;
            return new MyVector2(normalized.x, normalized.y);
        }
        
        public static IEnumerable<HalfEdge2> GetOutGoingEdges(this HalfEdgeData2 data, HalfEdgeVertex2 vertex)
        {
            IEnumerable<HalfEdge2> outgoingEdges= data.edges.Where(e => e.origin.Equals(vertex)).ToList();
            return outgoingEdges;
        }
        
        // returns all surrounding edges
        public static IEnumerable<HalfEdge2> GetSiblingOutgoingEdges(this HalfEdgeData2 data, HalfEdge2 edge,HashSet<HalfEdge2> visitedEdges = null)
        {
            IEnumerable<HalfEdge2> outgoingEdges;
            if (visitedEdges == null)
            {
               outgoingEdges= data.edges.Where(e => e.origin.Equals(edge.origin) && !e.Equals(edge)).ToList();
            }
            else
            {
               outgoingEdges= data.edges.Where(e => e.origin.Equals(edge.origin) && !e.Equals(edge) && !visitedEdges.Contains(e)).ToList();
            }
            return outgoingEdges;
        }
        
        public static IEnumerable<HalfEdge2> GetIncomingEdges(this HalfEdgeData2 data, HalfEdgeVertex2 vertex)
        {
            IEnumerable<HalfEdge2> incomingEdges= data.edges.Where(e => e.Twin.origin.Equals(vertex)).ToList();
            return incomingEdges;
        }
        public static IEnumerable<HalfEdge2> GetSiblingIncomingEdges(this HalfEdgeData2 data, HalfEdge2 edge)
        {
            IEnumerable<HalfEdge2> incomingEdges= data.edges.Where(e => e.Twin.origin.Equals(edge.origin) && !e.Equals(edge.Twin)).ToList();
            return incomingEdges;
        }
    }
}