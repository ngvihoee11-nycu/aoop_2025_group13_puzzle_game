using UnityEngine;

public class Portal : MonoBehaviour
{
    public Transform linkedPortal;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (linkedPortal == null) return;

            PlayerController player = other.GetComponent<PlayerController>();

            if (player == null) return;

            player.enabled = false;

            Vector3 portalToPlayer = player.transform.position - transform.position;
            float dotProduct = Vector3.Dot(transform.forward, portalToPlayer);

            // Check if the player is entering the portal from the front
            if (dotProduct > 0f)
            {
                // Calculate the player's position relative to the portal
                Vector3 localPosition = transform.InverseTransformPoint(player.transform.position);
                Vector3 mirroredPosition = new Vector3(-localPosition.x, localPosition.y, localPosition.z);
                Vector3 linkedPosition = linkedPortal.TransformPoint(mirroredPosition) + linkedPortal.forward * 0.5f; // Offset to avoid immediate re-triggering

                // Calculate the player's rotation relative to the portal
                Quaternion localRotation = Quaternion.Inverse(transform.rotation) * Quaternion.Euler(player.pitch, player.yaw + 180, 0);
                Quaternion linkedRotation = linkedPortal.rotation * localRotation;
                Vector3 linkedEular = linkedRotation.eulerAngles;

                // Teleport the player to the linked portal's position and rotation
                player.transform.position = linkedPosition;
                player.pitch = linkedEular.x;
                player.yaw = linkedEular.y;

                Physics.SyncTransforms(); // Ensure physics is updated after teleportation
            }

            player.enabled = true;
        }
    }

}