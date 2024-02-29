using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static DataStructure.CustomAttributes;

//A collection of classes that implements the Half-Edge Data Structure
namespace DataStructure.HalfEdge
{
    //A collection of classes that implements the Half-Edge Data Structure
    //From https://www.openmesh.org/media/Documentations/OpenMesh-6.3-Documentation/a00010.html
    
    //2D space
    public enum EdgeTopology
    {
        Wire, // no face  && no twin face
        BoundaryInnerEdge, //  face && no twin face
        BoundaryOuterEdge, // no face  && twin face
        ManifoldSame, // face && twin face - both face are same -- inner edges which are part of loop
        ManifoldDifferent, // face && twin face - both are different
        NonContiguous, // --
        Biblical // --
    }

    public enum HalfEdgeOperation
    {
        AddedEdgePair,
        RemovedEdgePair
    }

    [Serializable]
    public class HalfEdgeData2
    {
        [JsonIgnore] public Action<HalfEdgeVertex2> OnVertexAdded;
        [JsonIgnore] public Action<HalfEdge2> OnEdgeAdded;
        [JsonIgnore] public Action<HalfEdgeFace2> OnFaceAdded;

        [JsonProperty("_vertices")] List<HalfEdgeVertex2> _vertices;
        [JsonProperty("_faces")] private List<HalfEdgeFace2> _faces;
        [JsonProperty("_edges")] private List<HalfEdge2> _edges;
        
        
        int _nextVertexId = 1;
        int _nextFaceId = 1;
        int _nextEdgeId = 1;
        public int GetNextVertexId() => _nextVertexId++;
        public int GetNextFaceId() => _nextFaceId++;
        public int GetNextEdgeId() => _nextEdgeId++;
        
        public HalfEdgeVertex2 GetVertex(int id) => _vertices.FirstOrDefault(v => v.Id.Equals(id));
        public HalfEdgeFace2 GetFace(int id) => _faces.FirstOrDefault(f => f.Id.Equals(id));
        public HalfEdge2 GetEdge(int id) => _edges.FirstOrDefault(e => e.Id.Equals(id));
        public HalfEdge2 GetEdge(int id,int twinId) => _edges.FirstOrDefault(e => e.origin.Id.Equals(id) && e.Twin.origin.Id.Equals(twinId));
        [JsonIgnore]
        public List<HalfEdgeVertex2> vertices
        {
            get { return _vertices; }
            set
            {
                _vertices = value;
            }
        }

        [JsonIgnore]
        public List<HalfEdgeFace2> faces
        {
            get { return _faces; }
            set
            {
                _faces = value;
            }
        }

        [JsonIgnore]
        public List<HalfEdge2> edges
        {
            get { return _edges; }
            set
            {
                _edges = value;
            }
        }

        [JsonIgnore] public Action<HalfEdgeFace2> faceAdded;
        [JsonIgnore] public Action<HalfEdgeFace2> faceRemoved;

        [JsonIgnore] public Action<HalfEdgeVertex2> vertexAdded;
        [JsonIgnore] public Action<HalfEdgeVertex2> vertexRemoved;

        [JsonIgnore] public Action<HalfEdge2> edgeAdded;
        [JsonIgnore] public Action<HalfEdge2> edgeRemoved;

        // Function to create a new vertex with a unique ID
        public HalfEdgeVertex2 CreateNewVertex(MyVector2 position)
        {
            HalfEdgeVertex2 newVertex = new HalfEdgeVertex2(position);
            newVertex.Id = GetNextVertexId();

            // Optionally, you can trigger an event or perform other actions when a vertex is added
            if (vertexAdded != null)
                vertexAdded.Invoke(newVertex);

            return newVertex;
        }

        // Function to create a new face with a unique ID
        public HalfEdgeFace2 CreateNewFace(HalfEdge2 edge2)
        {
            HalfEdgeFace2 newFace = new HalfEdgeFace2(edge2); // Pass null for now, as the edge will be set later
            newFace.Id = GetNextFaceId();

            return newFace;
        }

        // Function to create a new edge with a unique ID
        public HalfEdge2 CreateNewEdge(HalfEdgeVertex2 origin)
        {
            HalfEdge2 newEdge = new HalfEdge2(origin);
            newEdge.origin = origin;
            newEdge.Id = GetNextEdgeId();

            // Optionally, you can trigger an event or perform other actions when an edge is added
            if (edgeAdded != null)
                edgeAdded.Invoke(newEdge);

            return newEdge;
        }

        public HalfEdgeData2()
        {
            this._vertices = new List<HalfEdgeVertex2>();

            this._faces = new List<HalfEdgeFace2>();

            this._edges = new List<HalfEdge2>();

            _nextEdgeId = 1;
            _nextFaceId = 1;
            _nextVertexId = 1;
            
        }

        public void Initialize()
        {
            foreach (HalfEdgeVertex2 v in vertices) v.Initialize(this);

            foreach (HalfEdge2 e in edges) e.InitializeOrigin(this);

            foreach (HalfEdge2 edge in edges)
            {
                edge.InitializeNext(this);
            }

            foreach (HalfEdge2 edge in edges)
            {
                edge.InitializePrev(this);
            }

            foreach (HalfEdge2 edge in edges)
            {
                edge.InitializeTwin(this);
            }

            foreach (HalfEdgeFace2 face in faces)
            {
                face.InitializeFaceEdge(this);
            }

            foreach (HalfEdge2 edge in edges)
            {
                HalfEdgeFace2 face = this.faces.FirstOrDefault(f => f.Id == edge.FaceId);
                if (face != null)
                {
                    edge.SetFace(face);
                }
            }

            foreach (HalfEdgeFace2 face in faces)
            {
                if (face != null)
                {
                    IEnumerable<HalfEdge2> faceEdges = face.GetEdges();
                    if (faceEdges != null)
                    {
                        foreach (HalfEdge2 e in faceEdges)
                        {
                            if (e != null)
                                e.SetFace(face);
                        }
                    }
                    else
                    {
                        Debug.Log("Face edges are null for face " + face.Id + "While initializing the data structure");
                    }
                }
            }


            // InitializeEdgeFace();
            _nextEdgeId = _edges.Any()? _edges.Max(e => e.Id)+1:1;
            _nextFaceId = _faces.Any()? _faces.Max(f => f.Id)+1:1;
            _nextVertexId = _vertices.Any()?_vertices.Max(v => v.Id)+1:1;
        }

        public void InitializeEdgeFace()
        {
            foreach (HalfEdge2 e in edges)
            {
                if (e.Face != null && e.Face.edge != null)
                {
                    HalfEdgeFace2 matchingFace = faces.FirstOrDefault(f => f.edge.Id == e.Face.edge.Id);
                    if (matchingFace != null)
                    {
                        Debug.Log($"Setting face to {matchingFace}");
                        e.SetFace(matchingFace);
                    }
                    else
                    {
                        Debug.Log("Matching face is null");
                    }
                }
                else
                {
                    Debug.Log("Face is null");
                }
            }
        }

        public void RemoveFace(HalfEdgeFace2 face)
        {
            if(face == null)
            {
                Debug.Log("Face is null");
                return;
            }
            HalfEdgeFace2 face2 = null;
            foreach (HalfEdgeFace2 f in faces)
            {
                if (f.edge.Equals(face.edge))
                {
                    face2 = f;
                }
            }
            if(face2 != null)
            {
                Debug.Log($"Face removed successfully {face2}");
                faces.Remove(face2);
                _faces.Remove(face2);
                if (faceRemoved != null)
                    faceRemoved.Invoke(face2);
            }
            else
            {
                Debug.Log("Face not found in the list");
            }
        }
        public void RemoveEdge(HalfEdge2 edge)
        {
            if (edge == null)
            {
                Debug.Log("Edge is null");
                return;
            }

            HalfEdge2 edge2 = null;
            foreach (HalfEdge2 e in edges)
            {
                if (edge.origin.Equals(e.origin) && edge.Twin.origin.Equals(e.Twin.origin))
                {
                    edge2 = e;
                }
            }

            if (edge2 != null)
            {
                edges.Remove(edge2);
                _edges.Remove(edge2);
                Debug.Log($"Edge removed successfully {edge2}");
                if (edgeRemoved != null)
                    edgeRemoved.Invoke(edge2);
            }
            else
            {
                Debug.Log("Edge not found in the list");
            }
        }
        
        public void RemoveVertex(HalfEdgeVertex2 cpHalfEdgeVertex)
        {
            if (cpHalfEdgeVertex == null)
            {
                Debug.Log("Vertex is null");
                return;
            }

            HalfEdgeVertex2 vertex = null;
            foreach (HalfEdgeVertex2 v in vertices)
            {
                if (v == null) continue;
                if (v.Id.Equals(cpHalfEdgeVertex.Id))
                {
                    vertex = v;
                }
            }

            if (vertex != null)
            {
                vertices.Remove(vertex);
                _vertices.Remove(vertex);
                if (vertexRemoved != null)
                    vertexRemoved.Invoke(vertex);
            }
            else
            {
                Debug.Log("Vertex not found in the list");
            }
        }
    }
    


    [System.Serializable]
    public class HalfEdgeVertex2 : IEquatable<HalfEdgeVertex2>
    {
        public int Id { get; set; }

        //The position of the vertex
        public MyVector2 position;

        //Each vertex references an half-edge that starts at this point
        //Might seem strange because each halfEdge references a vertex the edge is going to?

        public HalfEdgeVertex2(MyVector2 position)
        {
            this.position = position;
        }

        public void Initialize(HalfEdgeData2 halfEdgeData2)
        {
            if (halfEdgeData2 == null) return;

            foreach (HalfEdge2 e in halfEdgeData2.edges)
            {
                if (e == null)
                {
                    Debug.Log("Edge is null while initializing a vertex to it");
                    continue;
                }

                if (e.origin == null)
                {
                    Debug.Log("Origin is null while initializing a vertex to it");
                    continue;
                }

                HalfEdgeVertex2 first = null;
                foreach (HalfEdgeVertex2 v in halfEdgeData2.vertices)
                {
                    if (v == null)
                    {
                        Debug.Log(">> Vertex is null while initializing a vertex to it");
                        continue;
                    }

                    if (v.Id == e.origin.Id)
                    {
                        first = v;
                        break;
                    }
                }

                e.origin = first;
            }
        }

        public bool Equals(HalfEdgeVertex2 other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return position.Equals(other.position);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + position.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return "V:" + Id + "(" + position.x + "," + position.y + ")";
        }
    }

    //This face could be a triangle or whatever we need
    [System.Serializable]
    public class HalfEdgeFace2 : IEquatable<HalfEdgeFace2>
    {
        // The half-edges which are not part of any face will have face as null, instead of f0(extra face formed by outer boundary) https://courses.engr.illinois.edu/cs173/fa2011/lectures/planargraphs.pdf
        public int Id;

        //Each face references one of the halfedges bounding it
        //If you need the vertices, you can use this edge
        public HalfEdge2 edge;


        public HalfEdgeFace2(HalfEdge2 edge)
        {
            this.edge = edge;
        }

        public void InitializeFaceEdge(HalfEdgeData2 data)
        {
            if (edge != null)
            {
                edge = data.edges.FirstOrDefault(e => e.Id.Equals(this.edge.Id));
            }
        }


        public bool Equals(HalfEdgeFace2 other)
        {
            if (ReferenceEquals(null, other)) return false;

            return Equals(this.edge, other.edge);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + edge.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            if (edge == null)
                return "F{" + Id + "}: " + "null";

            HalfEdge2 edgeNextEdge = edge;
            int max = 1000;
            string str = "F{" + Id + "}: ";
            int i = 0;
            do
            {
                i++;
                if (i > max)
                {
                    Debug.Log($"face has gone bananas -_- , {Id}");
                    break;
                }

                str += edgeNextEdge?.origin?.Id + "->";
                edgeNextEdge = edgeNextEdge?.NextEdge;
            } while (edgeNextEdge != edge);

            return str;
        }

        public IEnumerable<HalfEdgeVertex2> GetVertices()
        {
            if (this.edge == null) return null;

            HalfEdge2 edgeNextEdge = this.edge;
            List<HalfEdge2> visitedEdges = new List<HalfEdge2>();
            List<HalfEdgeVertex2> vertices = new List<HalfEdgeVertex2>();

            do
            {
                if (edgeNextEdge.origin == null || visitedEdges.Contains(edgeNextEdge))
                {
                    break;
                }

                visitedEdges.Add(edgeNextEdge);
                vertices.Add(edgeNextEdge.origin);

                edgeNextEdge = edgeNextEdge.NextEdge;
            } while (edgeNextEdge != this.edge);

            return vertices;
        }


        public IEnumerable<HalfEdge2> GetEdges()
        {
            if (this.edge == null) return null;

            HalfEdge2 edgeNextEdge = this.edge;
            List<HalfEdge2> edges = new List<HalfEdge2>();
            HashSet<HalfEdge2> visitedEdges = new HashSet<HalfEdge2>();

            do
            {
                if (!visitedEdges.Add(edgeNextEdge))
                {
                    break;
                }

                edges.Add(edgeNextEdge);

                edgeNextEdge = edgeNextEdge.NextEdge;
            } while (edgeNextEdge != this.edge && edgeNextEdge != null);

            return edges;
        }

// sets the face of all edges to null
        public void Detach()
        {
            foreach (HalfEdge2 _i in GetEdges())
            {
                _i.SetFace(null); // Added null check here
            }
        }

        public bool IsCyclic()
        {
            IEnumerable<HalfEdge2> edges = GetEdges();
            if (edges == null)
                return false;

            HashSet<HalfEdge2> visitedEdges = new HashSet<HalfEdge2>();
            HalfEdge2 edgeNextEdge = this.edge;

            do
            {
                if (!visitedEdges.Add(edgeNextEdge))
                {
                    return true;
                }

                // Go to twin edge if nextEdge is null
                edgeNextEdge = edgeNextEdge.NextEdge;
            } while (edgeNextEdge != this.edge && edgeNextEdge != null);

            return false;
        }
    }

    [Serializable]
    //An edge going in a direction
    public class HalfEdge2 : IEquatable<HalfEdge2>
    {
        public int Id;
        public int FaceId;
        public int NextEdgeId;
        public int PrevEdgeId;
        public int TwinId;


        //The vertex it points to
        public HalfEdgeVertex2 origin;

        //The face it belongs to
        [JsonIgnore] public HalfEdgeFace2 Face { get; private set; }

        //The next half-edge inside the face (ordered clockwise)
        //The document says counter-clockwise but clockwise is easier because that's how Unity is displaying triangles
        [JsonIgnore] public HalfEdge2 NextEdge { get; private set; }


        //(optionally) the previous halfedge in the face
        //If we assume the face is closed, then we could identify this edge by walking forward
        //until we reach it
        [JsonIgnore] public HalfEdge2 PrevEdge { get; private set; }

        //The opposite half-edge belonging to the neighbor
        [JsonIgnore] public HalfEdge2 Twin { get; private set; }

        public bool isAttached;

        public void InitializeOrigin(HalfEdgeData2 data)
        { 
            if(this.origin == null)
            {
                Debug.Log("Origin is null for edge Id: " + this.Id);
                return;
            }
            if (Id != -1)
            {
                HalfEdgeVertex2 first = null;
                foreach (HalfEdgeVertex2 v in data.vertices)
                {
                    if(v==null) continue;
                    if (v.Id == this.origin.Id)
                    {
                        first = v;
                        break;
                    }
                }

                this.origin = first;
            }
        }

        public void InitializeNext(HalfEdgeData2 data)
        {
            if (this.NextEdgeId != -1)
                this.NextEdge = data.edges.FirstOrDefault(e => e.Id == this.NextEdgeId);
        }

        public void InitializePrev(HalfEdgeData2 data)
        {
            if (this.PrevEdgeId != -1)
                this.PrevEdge = data.edges.FirstOrDefault(e => e.Id == this.PrevEdgeId);
        }

        public void InitializeTwin(HalfEdgeData2 data)
        {
            if (this.TwinId != -1)
                this.Twin = data.edges.FirstOrDefault(e => e.Id == this.TwinId);
        }

        public void InitializeFace(HalfEdgeData2 data)
        {
            if (this.FaceId != -1)
                this.Face = data.faces.FirstOrDefault(f => f.Id == this.FaceId);
        }


        public HalfEdge2(HalfEdgeVertex2 origin)
        {
            this.origin = origin;
            this.Id = origin.Id;
        }

        public void SetTwin(HalfEdge2 twinEdge)
        {
            this.Twin = twinEdge;
            this.TwinId = twinEdge?.Id ?? -1;
        }

        // Setter method for setting the Face property
        public void SetFace(HalfEdgeFace2 face)
        {
            this.Face = face;
            this.FaceId = face?.Id ?? -1;
        }

        // Setter method for setting the NextEdge property
        public void SetNextEdge(HalfEdge2 nextEdge)
        {
            this.NextEdge = nextEdge;
            this.NextEdgeId = nextEdge?.Id ?? -1;
        }

        // Setter method for setting the PrevEdge property
        public void SetPrevEdge(HalfEdge2 prevEdge)
        {
            this.PrevEdge = prevEdge;
            this.PrevEdgeId = prevEdge?.Id ?? -1;
        }


        public override string ToString()
        {
            if (origin == null)
                return "origin: null";

            return this?.origin?.Id + "->" + this?.Twin?.origin?.Id;
        }

        public bool Equals(HalfEdge2 other)
        {
            if (ReferenceEquals(null, other)) return false;
            if(this.origin == null || other.origin == null)
            {
                Debug.Log($"Origin is null while checking equality for faces {Id}");
                return false;
            }
            return (this.origin.Id.Equals(other.origin.Id)) && (this.Twin.origin.Id.Equals(other.Twin.origin.Id));
        }

        public EdgeTopology GetTopology()
        {
            if (this.Twin == null || this == null)
            {
                Debug.Log(" Edge not found with valid twin");
                return EdgeTopology.Biblical;
            }

            if ((this.Face == null && this.Twin.Face == null))
            {
                Debug.Log("Wire Edge");
                return EdgeTopology.Wire;
            }
            else if (this.Face == null && this.Twin.Face != null)
            {
                Debug.Log("Boundary Inner Edge");
                return EdgeTopology.BoundaryInnerEdge;
            }
            else if (this.Face != null && this.Twin.Face == null)
            {
                Debug.Log("Boundary Outer Edge");
                return EdgeTopology.BoundaryOuterEdge;
            }
            else if (this.Face != null && this.Twin.Face != null)
            {
                Debug.Log("Manifold");
                if (this.Face == this.Twin.Face)
                {
                    return EdgeTopology.ManifoldSame;
                }
                else
                {
                    return EdgeTopology.ManifoldDifferent;
                }
            }
            else
            {
                return EdgeTopology.Biblical;
            }
        }
    }
}