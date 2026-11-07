using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace RoboRougelike.R4_1
{
    // Data validation only. No Mesh, Collider or gameplay integration is created here.
    [Serializable] public sealed class R4ColliderBundle
    {
        public int schemaVersion;
        public string sourceRevision,sourceCatalogSha256,coordinates;
        public float gridMeters;
        public R4ColliderShape[] shapes;

        public const string ExpectedBundleSha256="768f45f036c407dc81656a72be3b85087d3586f69b797ef72a4bb6e27ec51f59";
        const string Revision="03af5207143da76faeeb8004543a51f26c7f9c0d";
        const string CatalogSha="3b4208e917d7314673f77d16934bb1bf4894d5b7b0839889c5d9a7fbb49adc74";

        // Call on the same raw text that will immediately be deserialized.
        public static bool IsTrustedSource(string json)
        {
            if(json==null)return false;
            using(var hash=SHA256.Create())
            {
                var bytes=hash.ComputeHash(Encoding.UTF8.GetBytes(json));
                return string.Equals(BitConverter.ToString(bytes).Replace("-","").ToLowerInvariant(),ExpectedBundleSha256,StringComparison.Ordinal);
            }
        }
        static bool Fail(out string error,string message){error=message;return false;}
        static bool Hex64(string value)
        {
            if(value==null||value.Length!=64)return false;
            foreach(char c in value)if(!((c>='0'&&c<='9')||(c>='a'&&c<='f')))return false;
            return true;
        }
        static int ExpectedPieces(string id)=>id=="R4B_Arch"?3:
            id=="R4B_Step"||id=="R4B_CoveEdge"||id=="R4B_CornerL3"||id=="R4B_Tee4"?2:1;
        static bool ValidPieceName(string id,string name)
        {
            string prefix="UCX_"+id;
            if(name==prefix)return true;
            if(name==null||!name.StartsWith(prefix+"_",StringComparison.Ordinal))return false;
            string suffix=name.Substring(prefix.Length+1);
            if(suffix.Length==0)return false;
            foreach(char c in suffix)if(c<'0'||c>'9')return false;
            return true;
        }
        public bool Validate(out string error)
        {
            error=null;
            if(schemaVersion!=1||sourceRevision!=Revision||sourceCatalogSha256!=CatalogSha||
               coordinates!="source_glb_xyz_unmodified"||float.IsNaN(gridMeters)||float.IsInfinity(gridMeters)||Math.Abs(gridMeters-.55f)>.000001f)
                return Fail(out error,"Unsupported bundle version or source metadata.");
            if(shapes==null||shapes.Length!=30)return Fail(out error,"Expected 30 shapes.");
            var expected=new HashSet<string>(StringComparer.Ordinal);
            foreach(var entry in global::R4BlockCatalog.All)expected.Add(entry.Id);
            var seen=new HashSet<string>(StringComparer.Ordinal);int totalPieces=0;
            foreach(var shape in shapes)
            {
                if(shape==null||string.IsNullOrEmpty(shape.id)||!expected.Contains(shape.id)||!seen.Add(shape.id)||!Hex64(shape.sourceSha256))
                    return Fail(out error,"Unknown, duplicate or invalid shape metadata.");
                if(shape.pieces==null||shape.pieces.Length!=ExpectedPieces(shape.id))return Fail(out error,"Unexpected collider piece count: "+shape.id);
                var names=new HashSet<string>(StringComparer.Ordinal);
                foreach(var piece in shape.pieces)
                {
                    if(piece==null||!ValidPieceName(shape.id,piece.name)||!names.Add(piece.name))return Fail(out error,"Invalid collider piece name: "+shape.id);
                    if(piece.positions==null||piece.positions.Length<12||piece.positions.Length%3!=0||
                       piece.indices==null||piece.indices.Length<12||piece.indices.Length%3!=0)return Fail(out error,"Invalid mesh array lengths: "+piece.name);
                    foreach(float n in piece.positions)if(float.IsNaN(n)||float.IsInfinity(n))return Fail(out error,"Non-finite vertex: "+piece.name);
                    int vertexCount=piece.positions.Length/3;
                    foreach(int index in piece.indices)if(index<0||index>=vertexCount)return Fail(out error,"Triangle index out of range: "+piece.name);
                    for(int i=0;i<piece.indices.Length;i+=3)
                    {
                        int a=piece.indices[i]*3,b=piece.indices[i+1]*3,c=piece.indices[i+2]*3;
                        double ux=piece.positions[b]-piece.positions[a],uy=piece.positions[b+1]-piece.positions[a+1],uz=piece.positions[b+2]-piece.positions[a+2];
                        double vx=piece.positions[c]-piece.positions[a],vy=piece.positions[c+1]-piece.positions[a+1],vz=piece.positions[c+2]-piece.positions[a+2];
                        double nx=uy*vz-uz*vy,ny=uz*vx-ux*vz,nz=ux*vy-uy*vx;
                        if(nx*nx+ny*ny+nz*nz<=1e-24)return Fail(out error,"Degenerate triangle: "+piece.name);
                    }
                    totalPieces++;
                }
            }
            return totalPieces==36||Fail(out error,"Expected 36 collider pieces.");
        }
    }
    [Serializable] public sealed class R4ColliderShape
    {
        public string id,sourceSha256;
        public R4ColliderPiece[] pieces;
    }
    [Serializable] public sealed class R4ColliderPiece
    {
        public string name;
        public float[] positions;
        public int[] indices;
    }
}
