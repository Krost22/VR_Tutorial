using UnityEngine;
using System.Collections.Generic;

namespace EditorPets
{
    public enum PetState
    {
        Idle,
        Walk,
        Sleep,
        Interact,
        Drag,
        Eat,
        Play
    }

    public class HeartParticle
    {
        public Vector2 position;
        public float lifeTime = 2f;
        public const float MaxLife = 2f;
    }

    public class PetController
    {
        private static GUIStyle _nameLabelStyle;

        public PetData data;
        public PetState currentState = PetState.Idle;
        
        public Vector2 position;
        public bool facingLeft = false;
        
        private float stateTimer = 0f;
        private float animationTimer = 0f;
        private int currentFrame = 0;

        private float nextStateTime = 3f;

        // For dragging
        public Vector2 dragOffset;

        // For food
        public Texture2D foodTexture;

        // For playing
        public Vector2 targetPosition;

        // Particles
        public List<HeartParticle> hearts = new List<HeartParticle>();
        private static Texture2D HeartTexture => ScenePetOverlay.settings != null ? ScenePetOverlay.settings.heartTexture : null;

        public PetController(PetData data, Vector2 startPosition)
        {
            this.data = data;
            this.position = startPosition;
            ChangeState(PetState.Idle);
        }

        public void Update(float deltaTime, Rect bounds)
        {
            if (data == null) return;

            // Animation logic
            animationTimer += deltaTime;
            float frameDuration = 1f / data.animationSpeed;
            if (animationTimer >= frameDuration)
            {
                animationTimer -= frameDuration;
                currentFrame++;
            }

            // State Machine Logic
            if (currentState != PetState.Drag && currentState != PetState.Interact && currentState != PetState.Eat)
            {
                stateTimer += deltaTime;
                if (stateTimer >= nextStateTime)
                {
                    ChooseNextState();
                }
            }

            switch (currentState)
            {
                case PetState.Walk:
                    position.x += (facingLeft ? -1 : 1) * data.moveSpeed * deltaTime;
                    
                    if (position.x < 0)
                    {
                        position.x = 0;
                        facingLeft = false;
                    }
                    else if (position.x + data.size.x > bounds.width)
                    {
                        position.x = bounds.width - data.size.x;
                        facingLeft = true;
                    }
                    break;

                case PetState.Interact:
                    stateTimer += deltaTime;
                    if (stateTimer >= 2f)
                    {
                        ChangeState(PetState.Idle);
                    }
                    break;

                case PetState.Eat:
                    stateTimer += deltaTime;
                    if (stateTimer >= 4f) // Eat for 4 seconds
                    {
                        ChangeState(PetState.Idle);
                    }
                    break;

                case PetState.Play:
                    float dist = targetPosition.x - (position.x + data.size.x / 2);
                    if (Mathf.Abs(dist) > 10f)
                    {
                        facingLeft = dist < 0;
                        position.x += (facingLeft ? -1 : 1) * data.moveSpeed * 1.6f * deltaTime; 
                    }
                    else
                    {
                        // If they reached it and it's not moving much, stay around it
                        if (Random.value > 0.995f) SpawnHeart();
                        
                        // Stay in play state but maybe wait a bit to "think"
                        stateTimer += deltaTime;
                        if (stateTimer > 2f) 
                        {
                            // After 2 seconds of being at the ball, maybe go idle or keep playing
                            if (Random.value > 0.7f) ChangeState(PetState.Idle);
                            else stateTimer = 0; // reset and keep playing
                        }
                    }
                    break;
                    
                case PetState.Drag:
                    break;
            }

            if (position.x > bounds.width) position.x = Mathf.Max(0, bounds.width - data.size.x);

            // Update hearts
            for (int i = hearts.Count - 1; i >= 0; i--)
            {
                hearts[i].lifeTime -= deltaTime;
                hearts[i].position.y -= 30f * deltaTime; // Float up (offset from the pet)
                if (hearts[i].lifeTime <= 0)
                {
                    hearts.RemoveAt(i);
                }
            }
        }

        public void SnapToFloor(float floorY)
        {
            if (currentState == PetState.Drag) return;
            position.y = floorY;
        }

        private void ChooseNextState()
        {
            float rand = Random.value;
            if (currentState == PetState.Sleep)
            {
                ChangeState(PetState.Idle);
            }
            else if (currentState == PetState.Walk)
            {
                ChangeState(rand > 0.3f ? PetState.Idle : PetState.Sleep);
            }
            else // From Idle
            {
                if (rand > 0.5f)
                {
                    ChangeState(PetState.Walk);
                    facingLeft = Random.value > 0.5f;
                }
                else if (rand > 0.8f)
                {
                    ChangeState(PetState.Sleep);
                }
                else
                {
                    ChangeState(PetState.Idle);
                }
            }
        }

        public void ChangeState(PetState newState)
        {
            currentState = newState;
            stateTimer = 0f;
            currentFrame = 0;
            animationTimer = 0f;
            
            nextStateTime = Random.Range(3f, 8f);
            if (newState == PetState.Sleep) nextStateTime = Random.Range(10f, 20f);
        }

        public void Feed(Texture2D foodTex)
        {
            foodTexture = foodTex;
            ChangeState(PetState.Eat);
        }

        public void SpawnHeart()
        {
            if (HeartTexture == null) return;
            // Stored relative to the pet: its Y is re-snapped per Scene View, so absolute coords would drift between views.
            hearts.Add(new HeartParticle { position = new Vector2(data.size.x / 2 - 16, -10) });
        }

        public void Draw(float opacity, bool showName)
        {
            if (data == null) return;

            var (tex, totalFrames) = data.GetAnimation(currentState);
            if (tex == null) tex = Texture2D.whiteTexture; // no Idle sheet yet: placeholder square

            int frame = currentFrame % totalFrames;

            float uWidth = 1f / totalFrames;
            float uStart = frame * uWidth;
            
            Rect texCoords = new Rect(uStart, 0, uWidth, 1);
            
            if (facingLeft)
            {
                texCoords.x = uStart + uWidth;
                texCoords.width = -uWidth;
            }

            float yOffset = 0;
            if (currentState == PetState.Interact)
            {
                yOffset = Mathf.Sin(Time.realtimeSinceStartup * 20f) * 5f;
            }

            // Draw shadow
            Rect drawRect = new Rect(position.x, position.y + yOffset, data.size.x, data.size.y);
            GUI.color = new Color(0, 0, 0, 0.3f * opacity);
            GUI.DrawTexture(new Rect(drawRect.x + 5, drawRect.yMax - 5, drawRect.width - 10, 10), Texture2D.whiteTexture);
            GUI.color = new Color(1, 1, 1, opacity);

            // Draw food if eating
            if (currentState == PetState.Eat && foodTexture != null)
            {
                float foodX = facingLeft ? position.x + data.size.x * 0.25f - 32 : position.x + data.size.x * 0.75f;
                GUI.DrawTexture(new Rect(foodX, position.y + data.size.y - 32, 32, 32), foodTexture);
            }

            // Draw Pet
            GUI.DrawTextureWithTexCoords(drawRect, tex, texCoords, true);

            // Draw Hearts
            foreach (var heart in hearts)
            {
                if (HeartTexture == null) break;
                float alpha = (heart.lifeTime / HeartParticle.MaxLife) * opacity;
                GUI.color = new Color(1, 1, 1, alpha);
                GUI.DrawTexture(new Rect(position.x + heart.position.x, position.y + heart.position.y, 32, 32), HeartTexture);
            }
            GUI.color = Color.white;
            
            // Draw Name Tag
            if (showName)
            {
                if (_nameLabelStyle == null)
                {
                    _nameLabelStyle = new GUIStyle(GUI.skin.label);
                    _nameLabelStyle.alignment = TextAnchor.MiddleCenter;
                }
                Color skinText = GUI.skin.label.normal.textColor;
                _nameLabelStyle.normal.textColor = new Color(skinText.r, skinText.g, skinText.b, opacity);
                Color prevColor = GUI.color;
                GUI.color = new Color(0, 0, 0, 0.5f * opacity);
                Rect nameRect = new Rect(position.x, position.y - 20, data.size.x, 18);
                GUI.DrawTexture(nameRect, Texture2D.whiteTexture);
                GUI.color = prevColor;
                GUI.Label(nameRect, data.petName, _nameLabelStyle);
            }
        }
    }
}
