using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Text;
using TinyPlyNet;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PointCloudMeshGenerator : MonoBehaviour
{
    //TODO: Add optimizations for large meshes (e.g., decimation, LODs, downsampling)

    [Tooltip("Path to the .ply file")]
    public string plyFilePath = "Assets/Classic side table.ply";
    [Tooltip("Path to the .jpg texture file")]
    public string textureFilePath = "Assets/Classic side table_0.jpg";

    void Start()
    {
        GenerateMeshFromPly();
    }

    void GenerateMeshFromPly()
    {
        if (!File.Exists(plyFilePath))
        {
            Debug.LogError("PLY file not found: " + plyFilePath);
            return;
        }

        //HEADER OF PLY FILE FOR REFERENCE
        /*
         *  ply
            format binary_little_endian 1.0
            comment File exported by Artec Group 3D Scanning Solutions
            comment www.artec-group.com
            element vertex 499899
            property float x
            property float y
            property float z
            element face 999790
            property list uchar int vertex_indices
            element multi_texture_vertex 494207
            property uchar tx
            property float u
            property float v
            element multi_texture_face 999790
            property uchar tx
            property uint tn
            property list uchar int texture_vertex_indices
            end_header
        */

        // Read PLY file
        using FileStream stream = File.OpenRead(plyFilePath);
        PlyFile file = new PlyFile(stream);
        // Request vertex and face data
        List<float> vertices = new List<float>();
        file.RequestPropertyFromElement("vertex", new[] { "x", "y", "z" }, vertices);
        List<List<int>> faces = new List<List<int>>();
        file.RequestListPropertyFromElement("face", "vertex_indices", faces);
        // Request multi texture data
        List<byte> wedgeTx = new List<byte>();
        List<float> wedgeUV = new List<float>();
        file.RequestPropertyFromElement("multi_texture_vertex", new[] { "tx" }, wedgeTx);
        file.RequestPropertyFromElement("multi_texture_vertex", new[] { "u", "v" }, wedgeUV);
        // Request multi texture face indices
        List<int> multiTextureFaceIndicies = new List<int>();
        file.RequestPropertyFromElement("multi_texture_face", new[] { "texture_vertex_indices" }, multiTextureFaceIndicies);

        // Read rest of the file
        file.Read(stream);

        //Process vertex list into Vector3
        List<Vector3> vertsTemp = new List<Vector3>();
        for (int i = 0; i < vertices.Count; i += 3)
        {
            //Invert X coordinate to convert from right-handed to left-handed coordinate system since the .ply file uses right handed
            vertsTemp.Add(new Vector3(-vertices[i], vertices[i + 1], vertices[i + 2]));
        }
        //Process UV list into Vector2 and flip V coordinate
        //We flip the V coordinate because Unity's UV origin is bottom left and the .ply file uses top left
        List<Vector2> uvTemp = new List<Vector2>();
        for (int i = 0; i < wedgeUV.Count; i += 2)
        {
            uvTemp.Add(new Vector2(wedgeUV[i], 1f - wedgeUV[i + 1]));
        }

        //Set up final lists of vertices, uvs, and triangles based on multi texture face indicies
        List<Vector3> finalVertices = new List<Vector3>();
        List<Vector2> finalUVs = new List<Vector2>();
        List<int> finalTriangles = new List<int>();

        //Iterate through faces and build final vertex, uv, and triangle lists
        int wedgeOffset = 0;
        for (int curFace = 0; curFace < faces.Count; curFace++)
        {
            List<int> geoFace = faces[curFace];
            //if not a triangle, skip
            if (geoFace.Count != 3) continue;

            //for each corner of the triangle add the corresponding vertex and uv 
            for (int curCorner = 0; curCorner < 3; curCorner++)
            {
                //get the geometry index and wedge index
                int geoIdx = geoFace[curCorner];
                int wedgeIdx = multiTextureFaceIndicies[wedgeOffset + curCorner];

                //add to final lists, this was in counter clockwise order originally but led to inverted textures and normals
                //finalVertices.Add(vertsTemp[geoIdx]);
                //finalUVs.Add(uvTemp[wedgeIdx]);
                //finalTriangles.Add(finalVertices.Count - 1);

                //Add in clockwise order to flip normals (0 2 1) instead of (0 1 2)
                finalVertices.Add(vertsTemp[geoFace[0]]);
                finalUVs.Add(uvTemp[multiTextureFaceIndicies[wedgeOffset + 0]]);
                finalTriangles.Add(finalVertices.Count - 1);

                finalVertices.Add(vertsTemp[geoFace[2]]);
                finalUVs.Add(uvTemp[multiTextureFaceIndicies[wedgeOffset + 2]]);
                finalTriangles.Add(finalVertices.Count - 1);

                finalVertices.Add(vertsTemp[geoFace[1]]);
                finalUVs.Add(uvTemp[multiTextureFaceIndicies[wedgeOffset + 1]]);
                finalTriangles.Add(finalVertices.Count - 1);
            }
            //Increment wedge offset by 3 for next face
            wedgeOffset += 3;
        }



        // Original approach (not working with multi texture UVs)
        //List<int> triangles = new List<int>();
        //foreach (var face in faces)
        //{
        //    if (face.Count != 3)
        //    {
        //        Debug.LogWarning("Non-triangular face found, skipping.");
        //    }
        //    triangles.Add(face[0]);
        //    triangles.Add(face[1]);
        //    triangles.Add(face[2]);
        //}

        // Build mesh
        Mesh mesh = new Mesh();
        if (finalVertices.Count > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        mesh.SetVertices(finalVertices.ToArray());
        mesh.SetTriangles(finalTriangles.ToArray(), 0);
        mesh.SetUVs(0, finalUVs.ToArray());

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();


        //Only need if updating the mesh frequently
        //mesh.MarkDynamic();

        GetComponent<MeshFilter>().mesh = mesh;

        //texture
        byte[] fileData = File.ReadAllBytes(textureFilePath);
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
        tex.LoadImage(fileData);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply(true, false);

        // Material
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.mainTexture = tex;
        GetComponent<MeshRenderer>().material = mat;
        
    }
}