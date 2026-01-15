using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ComplexRoomMeshGenerator : MonoBehaviour
{
    //TODO: Add options for windows, multiple doors, wall thickness

    [Header("Room Dimensions")]
    public float roomWidth = 5f;
    public float roomHeight = 3f;
    public float roomDepth = 4f;

    [Header("Door Settings")]
    public bool addFrontDoor = true;
    [UnityEngine.Range(0.1f, 1f)]    
    public float doorXPos = 0.5f;
    public Vector2 doorSize = new Vector2(0.9f, 2.1f);


    Mesh mesh;

    void Start()
    {
        GenerateRoomMeshWithDoor();
    }

    void GenerateRoomMeshWithDoor()
    {
        mesh = new Mesh() { name = "Room Mesh" };
        GetComponent<MeshFilter>().mesh = mesh;

        List<Vector3> verticesList = new List<Vector3>();
        List<int> trianglesList = new List<int>();
        List<Color> colorsList = new List<Color>();

        // Shared corners
        Vector3 bottomFrontLeft = new Vector3(0, 0, 0);
        Vector3 bottomFrontRight = new Vector3(roomWidth, 0, 0);
        Vector3 bottomBackRight = new Vector3(roomWidth, 0, roomDepth);
        Vector3 bottomBackLeft = new Vector3(0, 0, roomDepth);

        Vector3 topFrontLeft = new Vector3(0, roomHeight, 0);
        Vector3 topFrontRight = new Vector3(roomWidth, roomHeight, 0);
        Vector3 topBackRight = new Vector3(roomWidth, roomHeight, roomDepth);
        Vector3 topBackLeft = new Vector3(0, roomHeight, roomDepth);

        // Floor
        int floorIdx = verticesList.Count;
        verticesList.Add(bottomFrontLeft);
        verticesList.Add(bottomFrontRight);
        verticesList.Add(bottomBackRight);
        verticesList.Add(bottomBackLeft);
        AddQuadTriangles(trianglesList, floorIdx + 0, floorIdx + 1, floorIdx + 2, floorIdx + 3);
        AddColor(colorsList, 4, Color.blue);

        // Ceiling
        int ceilIdx = verticesList.Count;
        verticesList.Add(topFrontLeft);
        verticesList.Add(topFrontRight);
        verticesList.Add(topBackRight);
        verticesList.Add(topBackLeft);
        AddQuadTriangles(trianglesList, ceilIdx + 3, ceilIdx + 2, ceilIdx + 1, ceilIdx + 0);
        AddColor(colorsList, 4, Color.red);

        // Back wall
        int backIdx = verticesList.Count;
        verticesList.Add(bottomBackLeft);
        verticesList.Add(bottomBackRight);
        verticesList.Add(topBackRight);
        verticesList.Add(topBackLeft);
        AddQuadTriangles(trianglesList, backIdx + 0, backIdx + 1, backIdx + 2, backIdx + 3);
        AddColor(colorsList, 4, Color.yellow);

        // Left wall
        int leftIdx = verticesList.Count;
        verticesList.Add(bottomFrontLeft);
        verticesList.Add(bottomBackLeft);
        verticesList.Add(topBackLeft);
        verticesList.Add(topFrontLeft);
        AddQuadTriangles(trianglesList, leftIdx + 0, leftIdx + 1, leftIdx + 2, leftIdx + 3);
        AddColor(colorsList, 4, Color.cyan);

        // Right wall
        int rightIdx = verticesList.Count;
        verticesList.Add(bottomFrontRight);
        verticesList.Add(bottomBackRight);
        verticesList.Add(topBackRight);
        verticesList.Add(topFrontRight);
        AddQuadTriangles(trianglesList, rightIdx + 0, rightIdx + 3, rightIdx + 2, rightIdx + 1);
        AddColor(colorsList, 4, Color.magenta);

        // Front wall with door
        if (!addFrontDoor)
        {
            int frontIdx = verticesList.Count;
            verticesList.Add(bottomFrontLeft);
            verticesList.Add(topFrontLeft);
            verticesList.Add(topFrontRight);
            verticesList.Add(bottomFrontRight);
            AddQuadTriangles(trianglesList, frontIdx + 0, frontIdx + 1, frontIdx + 2, frontIdx + 3);
            AddColor(colorsList, 4, Color.green);
        }
        else
        {
            float doorLeft = roomWidth * doorXPos - doorSize.x * 0.5f;
            float doorRight = doorLeft + doorSize.x;
            float doorBottom = 0f;
            float doorTop = doorSize.y;

            Color wallCol = Color.green;
            Color frameCol = new Color(0.55f, 0.35f, 0.15f);

            // Left piece
            int leftPiece = verticesList.Count;
            verticesList.Add(bottomFrontLeft);
            verticesList.Add(new Vector3(doorLeft, doorBottom, 0));
            verticesList.Add(new Vector3(doorLeft, doorTop, 0));
            verticesList.Add(topFrontLeft);
            AddQuadTriangles(trianglesList, leftPiece + 0, leftPiece + 3, leftPiece + 2, leftPiece + 1);
            AddColor(colorsList, 4, wallCol);

            // Right piece
            int rightPiece = verticesList.Count;
            verticesList.Add(new Vector3(doorRight, doorBottom, 0));
            verticesList.Add(bottomFrontRight);
            verticesList.Add(topFrontRight);
            verticesList.Add(new Vector3(doorRight, doorTop, 0));
            AddQuadTriangles(trianglesList, rightPiece + 0, rightPiece + 3, rightPiece + 2, rightPiece + 1);
            AddColor(colorsList, 4, wallCol);

            // Top piece
            int topPiece = verticesList.Count;
            verticesList.Add(new Vector3(doorLeft, doorTop, 0));
            verticesList.Add(new Vector3(doorRight, doorTop, 0));
            verticesList.Add(topFrontRight);
            verticesList.Add(topFrontLeft);
            AddQuadTriangles(trianglesList, topPiece + 0, topPiece + 3, topPiece + 2, topPiece + 1);
            AddColor(colorsList, 4, wallCol);

            // Frame thickness
            float frameThick = 0.08f;

            // Bottom frame
            int fBot = verticesList.Count;
            verticesList.Add(new Vector3(doorLeft, doorBottom, 0));
            verticesList.Add(new Vector3(doorRight, doorBottom, 0));
            verticesList.Add(new Vector3(doorRight, doorBottom + frameThick, 0));
            verticesList.Add(new Vector3(doorLeft, doorBottom + frameThick, 0));
            AddQuadTriangles(trianglesList, fBot + 0, fBot + 3, fBot + 2, fBot + 1);
            AddColor(colorsList, 4, frameCol);

            // Left frame
            int fLeft = verticesList.Count;
            verticesList.Add(new Vector3(doorLeft, doorBottom, 0));
            verticesList.Add(new Vector3(doorLeft, doorTop, 0));
            verticesList.Add(new Vector3(doorLeft + frameThick, doorTop, 0));
            verticesList.Add(new Vector3(doorLeft + frameThick, doorBottom, 0));
            AddQuadTriangles(trianglesList, fLeft + 0, fLeft + 1, fLeft + 2, fLeft + 3);
            AddColor(colorsList, 4, frameCol);

            // Right frame
            int fRight = verticesList.Count;
            verticesList.Add(new Vector3(doorRight, doorBottom, 0));
            verticesList.Add(new Vector3(doorRight - frameThick, doorBottom, 0));
            verticesList.Add(new Vector3(doorRight - frameThick, doorTop, 0));
            verticesList.Add(new Vector3(doorRight, doorTop, 0));
            AddQuadTriangles(trianglesList, fRight + 0, fRight + 1, fRight + 2, fRight + 3);
            AddColor(colorsList, 4, frameCol);

            // Top frame
            int fTop = verticesList.Count;
            verticesList.Add(new Vector3(doorLeft, doorTop, 0));
            verticesList.Add(new Vector3(doorRight, doorTop, 0));
            verticesList.Add(new Vector3(doorRight, doorTop - frameThick, 0));
            verticesList.Add(new Vector3(doorLeft, doorTop - frameThick, 0));
            AddQuadTriangles(trianglesList, fTop + 0, fTop + 1, fTop + 2, fTop + 3);
            AddColor(colorsList, 4, frameCol);
        }

        mesh.vertices = verticesList.ToArray();
        mesh.triangles = trianglesList.ToArray();
        mesh.colors = colorsList.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));
        mat.color = Color.white;
        GetComponent<MeshRenderer>().material = mat;

        CenterPivotAtFloorCenter(mesh);
    }

    private void LateUpdate()
    {
        //Rotate for demonstration
        transform.Rotate(Vector3.up, 20f * Time.deltaTime);
    }

    void AddQuadTriangles(List<int> tris, int a, int b, int c, int d)
    {
        // Triangle 1
        tris.Add(a); 
        tris.Add(b); 
        tris.Add(c);
        // Triangle 2
        tris.Add(c); 
        tris.Add(d); 
        tris.Add(a);
    }

    void AddColor(List<Color> colors, int count, Color col)
    {
        for (int i = 0; i < count; i++)
        {
            colors.Add(col);
        }
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
        GenerateRoomMeshWithDoor();
    }
}