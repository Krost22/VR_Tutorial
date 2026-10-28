using Autohand;
using UnityEngine;
using VRTutorial.RunePuzzle;

namespace VRTutorial.GameFeel
{
    /// <summary>
    /// Brillos del altar de las runas. Cada hueco tiene un anillo del color de su elemento:
    /// - vacío: tenue y respirando (invita a poner algo);
    /// - runa correcta: se enciende fuerte con un pequeño rebote;
    /// - runa equivocada: destello rojo y vuelve a tenue.
    /// El círculo central (y sus líneas) se ilumina según cuántas runas correctas hay (progreso visible).
    /// Solo visual: los sonidos los ponen los FeelBurst que ya tiene la sala. Para quitarlo: borrar el componente.
    /// </summary>
    public class RuneAltarGlow : MonoBehaviour
    {
        [System.Serializable]
        public class Socket
        {
            public PlacePoint placePoint;
            public Renderer ring;
            public Color color = Color.white;
            public RunicSymbol required = RunicSymbol.Ninguno;
            [HideInInspector] public float level;      // 0 tenue → 1 encendido
            [HideInInspector] public float wrongFlash; // 1 → 0
            [HideInInspector] public bool correct;
            [System.NonSerialized] public UnityEngine.Events.UnityAction<PlacePoint, Grabbable> onPlace, onRemove;
        }

        public Socket[] sockets = new Socket[0];
        [Tooltip("Círculo central y líneas hacia los huecos: se encienden con el progreso.")]
        public Renderer[] centerParts = new Renderer[0];
        public Color centerColor = new Color(1f, 0.82f, 0.45f);
        [Range(0f, 1f)] public float idleLevel = 0.28f;
        public Color wrongColor = new Color(1f, 0.15f, 0.1f);

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock _mpb;
        float _center;

        void OnEnable()
        {
            _mpb ??= new MaterialPropertyBlock();
            foreach (var s in sockets)
            {
                if (s.placePoint == null) continue;
                var socket = s;
                s.onPlace = (p, g) => OnPlaced(socket, g);
                s.onRemove = (p, g) => OnRemoved(socket);
                s.placePoint.OnPlace.AddListener(s.onPlace);
                s.placePoint.OnRemove.AddListener(s.onRemove);
            }
        }

        void OnDisable()
        {
            // Solo se quitan los oyentes propios: los pilares también escuchan estos huecos.
            foreach (var s in sockets)
            {
                if (s.placePoint == null) continue;
                if (s.onPlace != null) s.placePoint.OnPlace.RemoveListener(s.onPlace);
                if (s.onRemove != null) s.placePoint.OnRemove.RemoveListener(s.onRemove);
            }
        }

        void OnPlaced(Socket s, Grabbable grab)
        {
            var rune = grab != null ? grab.GetComponent<RuneData>() : null;
            s.correct = s.required == RunicSymbol.Ninguno || (rune != null && rune.CurrentSymbol == s.required);
            if (s.correct)
            {
                if (s.ring != null) FeelTween.Punch(s.ring.transform, 0.25f, 0.35f);
            }
            else s.wrongFlash = 1f;
        }

        void OnRemoved(Socket s) => s.correct = false;

        void Update()
        {
            int correct = 0;
            float t = Time.time;
            for (int i = 0; i < sockets.Length; i++)
            {
                var s = sockets[i];
                if (s.correct) correct++;
                float target = s.correct ? 1f : idleLevel * (0.75f + 0.25f * Mathf.Sin(t * 2.2f + i * 1.3f));
                s.level = Mathf.MoveTowards(s.level, target, Time.deltaTime * (s.correct ? 4f : 2f));
                s.wrongFlash = Mathf.MoveTowards(s.wrongFlash, 0f, Time.deltaTime * 2.5f);
                var c = Color.Lerp(s.color * s.level, wrongColor, s.wrongFlash);
                c.a = 1f;
                Apply(s.ring, c);
            }
            if (centerParts.Length > 0)
            {
                float target = sockets.Length == 0 ? 0f : correct / (float)sockets.Length;
                _center = Mathf.MoveTowards(_center, target, Time.deltaTime * 1.5f);
                float shimmer = 0.9f + 0.1f * Mathf.Sin(t * 3f);
                var c = centerColor * Mathf.Lerp(0.12f, 1f, _center) * shimmer;
                c.a = 1f;
                foreach (var r in centerParts) Apply(r, c);
            }
        }

        void Apply(Renderer r, Color c)
        {
            if (r == null) return;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, c);
            r.SetPropertyBlock(_mpb);
        }
    }
}
