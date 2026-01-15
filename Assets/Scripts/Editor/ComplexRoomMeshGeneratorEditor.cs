using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ComplexRoomMeshGenerator))]
public class ComplexRoomMeshGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ComplexRoomMeshGenerator myScript = (ComplexRoomMeshGenerator)target;
        if(EditorApplication.isPlaying)
        {
            if (GUILayout.Button("Update Room Mesh"))
            {
                myScript.UpdateRoomMesh();
            }
        }
    }
}

