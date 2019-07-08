using UnityEngine;
using UnityEditor;
using PathCreation;

namespace PathCreationEditor
{
    public static class MouseUtility
    {
        /// <summary>
		/// Determines mouse position in world. If PathSpace is xy/xz, the position will be locked to that plane.
		/// If PathSpace is xyz, then depthFor3DSpace will be used as distance from scene camera.
		/// </summary>

        public static Vector3 MouseToFloorRaycast()
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(mouseRay, out hit, Mathf.Infinity))
            {
                return hit.point;
            }
            else
            {
                return Vector3.zero;
            }
        }
    }
}