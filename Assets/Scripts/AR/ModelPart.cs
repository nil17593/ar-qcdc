using UnityEngine;

namespace QCDC.Core
{
    /// <summary>
    /// Holds the name and details for a specific piece of the 3D model.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ModelPart : MonoBehaviour
    {
        [HideInInspector]
        [SerializeField] private string partName;

        [TextArea]
        [SerializeField] private string description;

        // Automatically cleans up the object's name to use as a readable title
        private void Awake()
        {
            partName = gameObject.name.Replace("_", " ").Trim();
        }

        // Gives the clean name of this part
        public string GetPartName()
        {
            return partName;
        }

        // Gives the text description of this part
        public string GetDescription()
        {
            return description;
        }
    }
}