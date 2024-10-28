using UnityEngine;

namespace ShipInventoryFork.Objects
{
    public class DraggableObject : MonoBehaviour
    {
        private bool isDragging;
        private Vector3 offset;

        private void OnMouseDown()
        {
            // Start dragging when the object is clicked
            isDragging = true;
            offset = transform.position - GetMouseWorldPos();
        }

        private void OnMouseUp()
        {
            // Stop dragging
            isDragging = false;
        }

        private void Update()
        {
            if (isDragging)
            {
                transform.position = GetMouseWorldPos() + offset;
            }
        }

        private Vector3 GetMouseWorldPos()
        {
            // Convert mouse position to world position
            Vector3 mousePoint = Input.mousePosition;
            mousePoint.z = Camera.main.WorldToScreenPoint(transform.position).z;
            return Camera.main.ScreenToWorldPoint(mousePoint);
        }
    }
}
