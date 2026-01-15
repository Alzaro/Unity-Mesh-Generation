using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RoomMeshGenerator))]
public class RoomMeshGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RoomMeshGenerator myScript = (RoomMeshGenerator)target;
        if(EditorApplication.isPlaying)
        {
            if (GUILayout.Button("Update Room Mesh"))
            {
                myScript.UpdateRoomMesh();
            }
        }
    }
}

