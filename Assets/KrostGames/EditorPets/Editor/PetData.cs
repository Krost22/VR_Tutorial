using UnityEngine;

namespace EditorPets
{
    public enum PetLocation { Scene, House }

    [CreateAssetMenu(fileName = "NewPet", menuName = "EditorPets/Pet Data")]
    public class PetData : ScriptableObject
    {
        public string petName = "Doggo";
        public bool isActive = true;
        public PetLocation location = PetLocation.Scene;
        
        [Header("Sprite Sheets (Horizontal frames)")]
        public Texture2D idleTexture;
        public Texture2D walkTexture;
        public Texture2D sleepTexture;
        public Texture2D eatTexture;
        public Texture2D pettedTexture;
        
        [Header("Animation Frames")]
        public int framesIdle = 1;
        public int framesWalk = 2;
        public int framesSleep = 1;
        public int framesEat = 1;
        public int framesPetted = 1;
        
        [Header("Settings")]
        public float animationSpeed = 5f; // Frames per second
        public float moveSpeed = 50f;     // Pixels per second
        
        public Vector2 size = new Vector2(64, 64);
    }
}
