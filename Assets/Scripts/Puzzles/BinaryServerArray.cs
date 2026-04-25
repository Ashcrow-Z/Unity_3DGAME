using System.Collections;
using UnityEngine;

namespace SipLab
{
    /// <summary>
    /// Controls the 4×4 wall-mounted server rack that visually encodes the
    /// 4-digit access code in binary. Remains dormant (all drawers flush,
    /// all LEDs dark) until GameStateManager raises OnPowerRestored; then the
    /// rack reveals itself one row at a time, drawers with Bit=1 extruding
    /// and their LEDs turning green.
    ///
    /// Drawer index layout (MSB left, top row = thousands digit):
    ///     [ 0][ 1][ 2][ 3]    <- thousands (weights 8 4 2 1)
    ///     [ 4][ 5][ 6][ 7]    <- hundreds
    ///     [ 8][ 9][10][11]    <- tens
    ///     [12][13][14][15]    <- units
    /// </summary>
    public class BinaryServerArray : MonoBehaviour
    {
        [Tooltip("16 drawers, row-major. Row 0 = top. Col 0 = leftmost (MSB, weight 8).")]
        public ServerDrawer[] Drawers = new ServerDrawer[16];

        [Tooltip("Delay between successive rows during the power-on reveal.")]
        public float RowDelay = 0.4f;

        bool _revealed;
        bool _subscribed;

        void Start()
        {
            var gsm = GameStateManager.Instance;
            if (gsm != null)
            {
                gsm.OnRunStarted += ApplyBitsFromAccessCode;
                if (!string.IsNullOrEmpty(gsm.AccessCode) && gsm.AccessCode.Length == 4)
                    ApplyBitsFromAccessCode();
            }
            // Subscribe in Start (not OnEnable) to guarantee GameStateManager
            // has finished its Awake and Instance is non-null. Matches the
            // pattern used by GeneratorTerminal and avoids a race condition
            // where OnEnable could fire before GameStateManager.Awake.
            TrySubscribe();
        }

        void OnDestroy()
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.OnRunStarted -= ApplyBitsFromAccessCode;
        }

        void ApplyBitsFromAccessCode()
        {
            var gsm = GameStateManager.Instance;
            if (gsm == null || string.IsNullOrEmpty(gsm.AccessCode) || gsm.AccessCode.Length != 4) return;
            string code = gsm.AccessCode;
            for (int row = 0; row < 4; row++)
            {
                int d = code[row] - '0';
                if (d < 0 || d > 9) return;
                for (int col = 0; col < 4; col++)
                {
                    int weight = 8 >> col;
                    bool bit = (d & weight) != 0;
                    int idx = row * 4 + col;
                    if (idx < Drawers.Length && Drawers[idx] != null)
                        Drawers[idx].Bit = bit;
                }
            }
        }

        void OnDisable()
        {
            var gsm = GameStateManager.Instance;
            if (gsm != null && _subscribed)
            {
                gsm.OnPowerRestored -= HandlePowerRestored;
                _subscribed = false;
            }
        }

        void TrySubscribe()
        {
            if (_subscribed) return;
            var gsm = GameStateManager.Instance;
            if (gsm == null) return;
            gsm.OnPowerRestored += HandlePowerRestored;
            _subscribed = true;
            // If power was somehow restored before we subscribed (e.g. load
            // from a saved state), reveal immediately.
            if (gsm.IsPowerOn) TryReveal();
        }

        void HandlePowerRestored() => TryReveal();

        void TryReveal()
        {
            if (_revealed) return;
            _revealed = true;
            StartCoroutine(CoReveal());
        }

        IEnumerator CoReveal()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayRackActivate();
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    int idx = row * 4 + col;
                    if (idx < Drawers.Length && Drawers[idx] != null)
                        Drawers[idx].Reveal();
                }
                yield return new WaitForSeconds(RowDelay);
            }
        }
    }
}
