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

        private void OnValidate()
        {
            framesIdle = Mathf.Max(1, framesIdle);
            framesWalk = Mathf.Max(1, framesWalk);
            framesSleep = Mathf.Max(1, framesSleep);
            framesEat = Mathf.Max(1, framesEat);
            framesPetted = Mathf.Max(1, framesPetted);
            animationSpeed = Mathf.Max(0.01f, animationSpeed);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            size.x = Mathf.Max(1f, size.x);
            size.y = Mathf.Max(1f, size.y);

            ClampFramesToTexture(ref framesIdle, idleTexture);
            ClampFramesToTexture(ref framesWalk, walkTexture);
            ClampFramesToTexture(ref framesSleep, sleepTexture);
            ClampFramesToTexture(ref framesEat, eatTexture);
            ClampFramesToTexture(ref framesPetted, pettedTexture);
        }

        private static void ClampFramesToTexture(ref int frames, Texture2D tex)
        {
            if (tex == null) return;
            int max = Mathf.Max(1, tex.width / 8);
            if (frames > max) frames = max;
        }
    }
}
