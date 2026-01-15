using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RoomMeshGenerator : MonoBehaviour
{
    [Header("Room Dimensions")]
    public float roomWidth = 5f;
    public float roomHeight = 3f;
    public float roomDepth = 4f;

    Mesh mesh;

    void Start()
    {
        GenerateRoomMesh();
    }

    //Generates a simple box-shaped room mesh with colored faces
    void GenerateRoomMesh()
    {
        mesh = new Mesh() { name = "Room Mesh" };
        GetComponent<MeshFilter>().mesh = mesh;

        Vector3[] vertices = new Vector3[24]; // 4 vertices per face, 6 faces, 4*6

        //Floor
        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(roomWidth, 0, 0);
        vertices[2] = new Vector3(roomWidth, 0, roomDepth);
        vertices[3] = new Vector3(0, 0, roomDepth);

        //Ceiling
        vertices[4] = new Vector3(0, roomHeight, 0);
        vertices[5] = new Vector3(roomWidth, roomHeight, 0);
        vertices[6] = new Vector3(roomWidth, roomHeight, roomDepth);
        vertices[7] = new Vector3(0, roomHeight, roomDepth);

        //Walls
        //bottomleft
        vertices[8] = vertices[0];
        //topleft
        vertices[9] = vertices[4];
        //topright
        vertices[10] = vertices[5];
        //bottomright
        vertices[11] = vertices[1];

        // Back wall
        vertices[12] = vertices[3];
        vertices[13] = vertices[2];
        vertices[14] = vertices[6];
        vertices[15] = vertices[7];

        //Left wall
        vertices[16] = vertices[0];
        vertices[17] = vertices[3];
        vertices[18] = vertices[7];
        vertices[19] = vertices[4];

        //Right wall
        //frontbottom
        vertices[20] = vertices[1];
        //fronttop
        vertices[21] = vertices[5];
        //backtop
        vertices[22] = vertices[6];
        //backbottom
        vertices[23] = vertices[2];

        mesh.vertices = vertices;

        //Triangles, two per face
        int[] triangles = new int[36]; //6 faces * 6 indices (2 tris)
        int triIndex = 0;
        //Floor
        AddQuadTriangles(ref triangles, ref triIndex, 0, 1, 2, 3);
        //Ceiling
        AddQuadTriangles(ref triangles, ref triIndex, 7, 6, 5, 4);
        //Front
        AddQuadTriangles(ref triangles, ref triIndex, 8, 9, 10, 11);
        //Back
        AddQuadTriangles(ref triangles, ref triIndex, 12, 13, 14, 15);
        //Left
        AddQuadTriangles(ref triangles, ref triIndex, 16, 17, 18, 19);
        //Right
        AddQuadTriangles(ref triangles, ref triIndex, 20, 21, 22, 23);

        mesh.triangles = triangles;

        //vertex colors
        Color[] colors = new Color[24];
        //Floor: Blue
        colors[0] = colors[1] = colors[2] = colors[3] = Color.blue;
        //Ceiling: Red
        colors[4] = colors[5] = colors[6] = colors[7] = Color.red;
        //Front: Green
        colors[8] = colors[9] = colors[10] = colors[11] = Color.green;
        //Back: Yellow
        colors[12] = colors[13] = colors[14] = colors[15] = Color.yellow;
        //Left: Cyan
        colors[16] = colors[17] = colors[18] = colors[19] = Color.cyan;
        //Right: Magenta
        colors[20] = colors[21] = colors[22] = colors[23] = Color.magenta;
        mesh.colors = colors;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        //Assign material, use particle lit shader for vertex color support
        GetComponent<MeshRenderer>().material = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));
        GetComponent<MeshRenderer>().material.color = Color.white;

        //Center pivot at floor center
        CenterPivotAtFloorCenter(mesh);
    }

    private void LateUpdate()
    {
        //Rotate for demonstration
        transform.Rotate(Vector3.up, 20f * Time.deltaTime);
    }

    void AddQuadTriangles(ref int[] triangles, ref int index, int a, int b, int c, int d)
    {
        //triangle 1
        triangles[index++] = a; 
        triangles[index++] = b; 
        triangles[index++] = c;
        //triangle 2
        triangles[index++] = c; 
        triangles[index++] = d; 
        triangles[index++] = a;
    }

    void CenterPivotAtFloorCenter(Mesh mesh)
    {
        Vector3 centerOffset = new Vector3(roomWidth * 0.5f, 0f, roomDepth * 0.5f);

        // Shift all vertices
        Vector3[] verts = mesh.vertices;
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] -= centerOffset;
        }
        mesh.vertices = verts;

        mesh.RecalculateBounds();
    }

    public void UpdateRoomMesh()
    {
        GenerateRoomMesh();
    }
}