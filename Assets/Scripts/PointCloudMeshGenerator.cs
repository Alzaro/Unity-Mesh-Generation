using System.Collections.Generic;
using System.IO;
using TinyPlyNet;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PointCloudMeshGenerator : MonoBehaviour
{
    //WIP: Add optimizations for large meshes (e.g., decimation, LODs, downsampling)

    [Tooltip("Path to the .ply file")]
    public string plyFilePath = "Assets/Classic side table.ply";
    [Tooltip("Path to the .jpg texture file")]
    public string textureFilePath = "Assets/Classic side table_0.jpg";

    // Reduction options
    public enum ReductionMode
    {
        None,
        UniformSampling, //keep every Nth triangle
        VoxelClustering  //grid-based clustering (averages verts & uvs)
    }

    [Header("Triangle Reduction")]
    public bool reduceTriangles = true;
    public ReductionMode reductionMode = ReductionMode.VoxelClustering;
    [Range(0.01f, 1f)]
    [Tooltip("Fraction of triangles to keep. 1 = keep all, 0.5 = keep ~50%")]
    public float reductionFactor = 0.5f;
    [Tooltip("Optional exact target triangle count (if > 0, will attempt to reach this instead of using fraction)")]
    public int targetTriangleCount = 0;

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

            // Guard against malformed multiTextureFaceIndicies length
            if (wedgeOffset + 2 >= multiTextureFaceIndicies.Count)
            {
                Debug.LogWarning("multiTextureFaceIndicies shorter than expected; stopping face processing.");
                break;
            }

            // Add the triangle's three corners once (in clockwise order to flip normals)
            int w0 = multiTextureFaceIndicies[wedgeOffset + 0];
            int w1 = multiTextureFaceIndicies[wedgeOffset + 1];
            int w2 = multiTextureFaceIndicies[wedgeOffset + 2];

            // Ensure uv indices are valid
            if (w0 < 0 || w0 >= uvTemp.Count || w1 < 0 || w1 >= uvTemp.Count || w2 < 0 || w2 >= uvTemp.Count)
            {
                wedgeOffset += 3;
                continue;
            }

            // Ensure geometry indices are valid
            if (geoFace[0] < 0 || geoFace[0] >= vertsTemp.Count ||
                geoFace[1] < 0 || geoFace[1] >= vertsTemp.Count ||
                geoFace[2] < 0 || geoFace[2] >= vertsTemp.Count)
            {
                wedgeOffset += 3;
                continue;
            }

            // Corner 0 (geoFace[0])
            finalVertices.Add(vertsTemp[geoFace[0]]);
            finalUVs.Add(uvTemp[w0]);
            finalTriangles.Add(finalVertices.Count - 1);

            // Corner 2 (geoFace[2]) -- clockwise order: 0,2,1
            finalVertices.Add(vertsTemp[geoFace[2]]);
            finalUVs.Add(uvTemp[w2]);
            finalTriangles.Add(finalVertices.Count - 1);

            // Corner 1 (geoFace[1])
            finalVertices.Add(vertsTemp[geoFace[1]]);
            finalUVs.Add(uvTemp[w1]);
            finalTriangles.Add(finalVertices.Count - 1);

            //Increment wedge offset by 3 for next face
            wedgeOffset += 3;
        }

        if (reduceTriangles)
        {
            Debug.Log("TOTAL TRIANGLES BEFORE DOWNSAMPLE: " + finalTriangles.Count);
        }


        // If requested, reduce triangle count here
        if (reduceTriangles && reductionMode != ReductionMode.None && finalTriangles.Count >= 3)
        {
            int originalTriangleCount = finalTriangles.Count / 3;
            int desiredTriangleCount = targetTriangleCount > 0 ? Mathf.Clamp(targetTriangleCount, 1, originalTriangleCount) : Mathf.Clamp(Mathf.RoundToInt(originalTriangleCount * reductionFactor), 1, originalTriangleCount);

            if (desiredTriangleCount < originalTriangleCount)
            {
                switch (reductionMode)
                {
                    case ReductionMode.UniformSampling:
                        UniformSampleTriangles(finalVertices, finalUVs, finalTriangles, desiredTriangleCount, out finalVertices, out finalUVs, out finalTriangles);
                        break;

                    case ReductionMode.VoxelClustering:
                        VoxelClusterSimplify(finalVertices, finalUVs, finalTriangles, desiredTriangleCount, out finalVertices, out finalUVs, out finalTriangles);
                        break;
                }
            }
        }

        // Cleanup invalid vertices/uvs/triangles to avoid invalid MinMaxAABB errors
        if (!CleanupMeshData(finalVertices, finalUVs, finalTriangles, out finalVertices, out finalUVs, out finalTriangles))
        {
            Debug.LogError("Mesh creation aborted: no valid vertices/triangles after cleanup.");
            return;
        }

        // Build mesh
        Mesh mesh = new Mesh();
        if (finalVertices.Count > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        
        mesh.SetVertices(finalVertices);
        mesh.SetTriangles(finalTriangles, 0);
        mesh.SetUVs(0, finalUVs);

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
        if(reduceTriangles)
        {
            Debug.Log("TOTAL TRIANGLES AFTER DOWNSAMPLE: " + mesh.triangles.Length);
        }

    }

    // Keep every Nth triangle until we reach desired count
    void UniformSampleTriangles(List<Vector3> srcVerts, List<Vector2> srcUVs, List<int> srcTris, int desiredTriangleCount, out List<Vector3> outVerts, out List<Vector2> outUVs, out List<int> outTris)
    {
        outVerts = new List<Vector3>();
        outUVs = new List<Vector2>();
        outTris = new List<int>();

        int totalTriangles = srcTris.Count / 3;
        if (desiredTriangleCount <= 0 || totalTriangles <= desiredTriangleCount)
        {
            // nothing to do
            outVerts.AddRange(srcVerts);
            outUVs.AddRange(srcUVs);
            outTris.AddRange(srcTris);
            return;
        }

        int step = Mathf.Max(1, Mathf.FloorToInt((float)totalTriangles / desiredTriangleCount));
        int kept = 0;
        for (int t = 0; t < totalTriangles; t++)
        {
            if (t % step != 0 && kept < desiredTriangleCount) continue;

            int baseIdx = t * 3;
            // add triangle vertices as new independent vertices (preserves UVs & winding)
            for (int k = 0; k < 3; k++)
            {
                int srcIndex = srcTris[baseIdx + k];
                outVerts.Add(srcVerts[srcIndex]);
                outUVs.Add(srcUVs[srcIndex]);
                outTris.Add(outVerts.Count - 1);
            }

            kept++;
            if (kept >= desiredTriangleCount) break;
        }
    }

    // Voxel grid clustering: groups nearby vertices into voxels and averages their positions and UVs.
    // Tries to reach approximately desiredTriangleCount.
    void VoxelClusterSimplify(List<Vector3> srcVerts, List<Vector2> srcUVs, List<int> srcTris, int desiredTriangleCount, out List<Vector3> outVerts, out List<Vector2> outUVs, out List<int> outTris)
    {
        outVerts = new List<Vector3>();
        outUVs = new List<Vector2>();
        outTris = new List<int>();

        int totalTriangles = srcTris.Count / 3;
        if (desiredTriangleCount <= 0 || totalTriangles <= desiredTriangleCount)
        {
            outVerts.AddRange(srcVerts);
            outUVs.AddRange(srcUVs);
            outTris.AddRange(srcTris);
            return;
        }

        // Determine bounding box of vertices
        Vector3 min = srcVerts[0];
        Vector3 max = srcVerts[0];
        for (int i = 1; i < srcVerts.Count; i++)
        {
            min = Vector3.Min(min, srcVerts[i]);
            max = Vector3.Max(max, srcVerts[i]);
        }
        Vector3 size = max - min;
        // Compute target vertex count roughly proportional to desired triangles (assume triangles ~= verts)
        int targetVertCount = Mathf.Max(1, Mathf.RoundToInt((srcVerts.Count / (float)(totalTriangles)) * desiredTriangleCount));
        // fallback
        targetVertCount = Mathf.Clamp(targetVertCount, 1, srcVerts.Count);

        // Choose voxel grid resolution
        int voxelCubicRoot = Mathf.Max(1, Mathf.RoundToInt(Mathf.Pow(targetVertCount, 1f / 3f)));
        float maxDim = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        int nx = Mathf.Max(1, Mathf.RoundToInt(voxelCubicRoot * (size.x / (maxDim > 0f ? maxDim : 1f))));
        int ny = Mathf.Max(1, Mathf.RoundToInt(voxelCubicRoot * (size.y / (maxDim > 0f ? maxDim : 1f))));
        int nz = Mathf.Max(1, Mathf.RoundToInt(voxelCubicRoot * (size.z / (maxDim > 0f ? maxDim : 1f))));

        // ensure non-zero cell sizes
        float cellX = (nx > 0 && size.x > 0f) ? size.x / nx : 1f;
        float cellY = (ny > 0 && size.y > 0f) ? size.y / ny : 1f;
        float cellZ = (nz > 0 && size.z > 0f) ? size.z / nz : 1f;

        // Group vertices into voxels
        Dictionary<long, GroupAccumulator> groups = new Dictionary<long, GroupAccumulator>(srcVerts.Count);
        long[] indexToGroup = new long[srcVerts.Count];
        for (int i = 0; i < srcVerts.Count; i++)
        {
            Vector3 v = srcVerts[i];
            int ix = cellX > 0f ? Mathf.Clamp((int)((v.x - min.x) / cellX), 0, nx - 1) : 0;
            int iy = cellY > 0f ? Mathf.Clamp((int)((v.y - min.y) / cellY), 0, ny - 1) : 0;
            int iz = cellZ > 0f ? Mathf.Clamp((int)((v.z - min.z) / cellZ), 0, nz - 1) : 0;
            long key = (((long)ix) << 42) | (((long)iy) << 21) | (long)iz;
            if (!groups.TryGetValue(key, out GroupAccumulator acc))
            {
                acc = new GroupAccumulator();
            }
            acc.SumPos += v;
            acc.SumUV += srcUVs[i];
            acc.Count++;
            groups[key] = acc;
            indexToGroup[i] = key;
        }

        // Create mapping from group key to new index
        Dictionary<long, int> groupKeyToNewIndex = new Dictionary<long, int>(groups.Count);
        foreach (KeyValuePair<long, GroupAccumulator> kvp in groups)
        {
            long key = kvp.Key;
            GroupAccumulator acc = kvp.Value;
            Vector3 avgPos = acc.SumPos / acc.Count;
            Vector2 avgUV = acc.SumUV / acc.Count;
            int newIndex = outVerts.Count;
            outVerts.Add(avgPos);
            outUVs.Add(avgUV);
            groupKeyToNewIndex[key] = newIndex;
        }

        // Build triangles mapping old indices -> new grouped indices and skip degenerate triangles
        for (int t = 0; t < totalTriangles; t++)
        {
            int a = srcTris[t * 3 + 0];
            int b = srcTris[t * 3 + 1];
            int c = srcTris[t * 3 + 2];

            long keyA = indexToGroup[a];
            long keyB = indexToGroup[b];
            long keyC = indexToGroup[c];

            if (!groupKeyToNewIndex.TryGetValue(keyA, out int na)) continue;
            if (!groupKeyToNewIndex.TryGetValue(keyB, out int nb)) continue;
            if (!groupKeyToNewIndex.TryGetValue(keyC, out int nc)) continue;

            // skip degenerate triangles formed by clustering
            if (na == nb || nb == nc || na == nc) continue;

            outTris.Add(na);
            outTris.Add(nb);
            outTris.Add(nc);

            // Early exit if we already reached desired triangle count
            if (outTris.Count / 3 >= desiredTriangleCount) break;
        }

        // If result still too large (unlikely), fall back to uniform sampling on the reduced mesh
        if (outTris.Count / 3 > desiredTriangleCount)
        {
            // sample down
            List<Vector3> sampledVerts;
            List<Vector2> sampledUVs;
            List<int> sampledTris;
            UniformSampleTriangles(outVerts, outUVs, outTris, desiredTriangleCount, out sampledVerts, out sampledUVs, out sampledTris);
            outVerts = sampledVerts;
            outUVs = sampledUVs;
            outTris = sampledTris;
        }
    }

    // Validate and clean mesh lists; returns false if resulting mesh would be invalid/empty.
    bool CleanupMeshData(List<Vector3> verts, List<Vector2> uvs, List<int> tris, out List<Vector3> outVerts, out List<Vector2> outUVs, out List<int> outTris)
    {
        outVerts = new List<Vector3>();
        outUVs = new List<Vector2>();
        outTris = new List<int>();

        if (verts == null || verts.Count == 0)
        {
            Debug.LogError("No vertices available to build mesh.");
            return false;
        }

        // Ensure UV list is at least same length as verts (fill missing with zero)
        if (uvs == null) uvs = new List<Vector2>();
        while (uvs.Count < verts.Count)
        {
            uvs.Add(Vector2.zero);
        }

        int vCount = verts.Count;
        bool[] keep = new bool[vCount];
        for (int i = 0; i < vCount; i++)
        {
            Vector3 v = verts[i];
            bool valid = !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                           float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
            keep[i] = valid;
            if (!valid)
            {
                Debug.LogWarning($"Removing invalid vertex at index {i}: {v}");
            }
        }

        // Build mapping old index -> new index and build compacted lists
        int[] map = new int[vCount];
        for (int i = 0; i < vCount; i++) map[i] = -1;

        for (int i = 0; i < vCount; i++)
        {
            if (!keep[i]) continue;
            int newIndex = outVerts.Count;
            outVerts.Add(verts[i]);
            outUVs.Add(uvs[i]);
            map[i] = newIndex;
        }

        // Rebuild triangles, mapping indices and skipping invalid/degenerate triangles
        if (tris != null)
        {
            int triCount = tris.Count / 3;
            for (int t = 0; t < triCount; t++)
            {
                int ia = tris[t * 3 + 0];
                int ib = tris[t * 3 + 1];
                int ic = tris[t * 3 + 2];

                if (ia < 0 || ia >= vCount || ib < 0 || ib >= vCount || ic < 0 || ic >= vCount)
                {
                    // invalid indices, skip
                    continue;
                }

                int na = map[ia];
                int nb = map[ib];
                int nc = map[ic];

                if (na == -1 || nb == -1 || nc == -1)
                {
                    // one of the vertices was removed, skip triangle
                    continue;
                }

                // skip degenerate triangles
                if (na == nb || nb == nc || na == nc) continue;

                outTris.Add(na);
                outTris.Add(nb);
                outTris.Add(nc);
            }
        }

        if (outVerts.Count == 0 || outTris.Count == 0)
        {
            Debug.LogWarning("Cleanup removed all valid geometry (vertices or triangles).");
            return false;
        }

        return true;
    }

    struct GroupAccumulator
    {
        public Vector3 SumPos;
        public Vector2 SumUV;
        public int Count;
    }
}