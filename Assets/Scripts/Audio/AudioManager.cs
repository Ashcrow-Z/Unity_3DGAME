using UnityEngine;

namespace SipLab
{
    /// <summary>
    /// Singleton audio hub. Procedurally synthesises every SFX at startup so the
    /// project ships with zero audio assets. Two volume channels:
    ///   - Interaction: pickups, keypad, doors, success/error
    ///   - Action: footsteps, crate collisions
    /// Volumes are persisted in PlayerPrefs and survive restarts.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        const string PP_INTERACT = "siplab_vol_interact";
        const string PP_ACTION = "siplab_vol_action";

        [Range(0f, 1f)] public float InteractionVolume = 0.8f;
        [Range(0f, 1f)] public float ActionVolume = 0.7f;

        AudioSource _interactSrc;
        AudioSource _actionSrc;
        AudioSource _footSrc;

        AudioClip _pickup, _keycardPickup, _powerOn, _keypadBeep,
                  _success, _error, _keycardSwipe, _doorOpen, _safeOpen,
                  _footstep, _crateThud, _victory, _rackActivate;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            InteractionVolume = PlayerPrefs.GetFloat(PP_INTERACT, 0.8f);
            ActionVolume = PlayerPrefs.GetFloat(PP_ACTION, 0.7f);

            _interactSrc = gameObject.AddComponent<AudioSource>();
            _interactSrc.playOnAwake = false;
            _interactSrc.spatialBlend = 0f;
            _interactSrc.ignoreListenerPause = true;

            _actionSrc = gameObject.AddComponent<AudioSource>();
            _actionSrc.playOnAwake = false;
            _actionSrc.spatialBlend = 0f;

            _footSrc = gameObject.AddComponent<AudioSource>();
            _footSrc.playOnAwake = false;
            _footSrc.spatialBlend = 0f;

            BuildClips();
        }

        public void SetInteractionVolume(float v)
        {
            InteractionVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(PP_INTERACT, InteractionVolume);
            PlayerPrefs.Save();
        }

        public void SetActionVolume(float v)
        {
            ActionVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(PP_ACTION, ActionVolume);
            PlayerPrefs.Save();
        }

        void PlayInteract(AudioClip c, float volume = 1f, float pitch = 1f)
        {
            if (c == null || _interactSrc == null) return;
            _interactSrc.pitch = pitch;
            _interactSrc.PlayOneShot(c, Mathf.Clamp01(volume) * InteractionVolume);
        }

        void PlayAction(AudioClip c, float volume = 1f, float pitch = 1f)
        {
            if (c == null || _actionSrc == null) return;
            _actionSrc.pitch = pitch;
            _actionSrc.PlayOneShot(c, Mathf.Clamp01(volume) * ActionVolume);
        }

        // ---------- Public sound triggers ----------
        public void PlayPickupCore() { PlayInteract(_pickup, 0.9f, 1.0f); }
        public void PlayPickupKeycard() { PlayInteract(_keycardPickup, 0.9f, 1.0f); }
        public void PlayPowerOn() { PlayInteract(_powerOn, 1.0f, 1.0f); }
        public void PlayKeypadBeep(string label)
        {
            // pitch shift per key so different digits sound distinct
            float p = 1.0f;
            if (label == "C") p = 0.75f;
            else if (label == "E") p = 1.35f;
            else if (int.TryParse(label, out int n)) p = 0.95f + n * 0.04f;
            PlayInteract(_keypadBeep, 0.6f, p);
        }
        public void PlayCodeSuccess() { PlayInteract(_success, 1f, 1f); }
        public void PlayCodeError() { PlayInteract(_error, 1f, 1f); }
        public void PlayKeycardSwipe() { PlayInteract(_keycardSwipe, 0.9f, 1f); }
        public void PlayBlastDoor() { PlayInteract(_doorOpen, 1f, 1f); }
        public void PlaySafeDoor() { PlayInteract(_safeOpen, 0.85f, 1.05f); }
        public void PlayVictory() { PlayInteract(_victory, 1f, 1f); }
        public void PlayRackActivate() { PlayInteract(_rackActivate, 1f, 1f); }

        public void PlayFootstep()
        {
            if (_footstep == null || _footSrc == null) return;
            _footSrc.pitch = Random.Range(0.92f, 1.08f);
            _footSrc.PlayOneShot(_footstep, 0.5f * ActionVolume);
        }

        public void PlayCrateImpact(float impactStrength)
        {
            // impactStrength in [0..1+]
            float vol = Mathf.Clamp01(0.25f + impactStrength * 0.75f);
            float pitch = Mathf.Lerp(1.15f, 0.75f, Mathf.Clamp01(impactStrength));
            PlayAction(_crateThud, vol, pitch);
        }

        // ---------- Procedural synthesis ----------
        void BuildClips()
        {
            _pickup = Synth.TwoNoteBlip(880f, 1320f, 0.06f, 0.10f, "sfx_pickup");
            _keycardPickup = Synth.TwoNoteBlip(1100f, 1650f, 0.05f, 0.08f, "sfx_keycard_pickup");
            _powerOn = Synth.PowerHum("sfx_power_on");
            _keypadBeep = Synth.SquareBeep(800f, 0.06f, "sfx_keypad");
            _success = Synth.AscendingChord("sfx_success");
            _error = Synth.ErrorBuzz("sfx_error");
            _keycardSwipe = Synth.NoiseBurst(0.18f, 1500f, 4500f, "sfx_swipe");
            _doorOpen = Synth.HeavyWhoosh(1.2f, "sfx_door");
            _safeOpen = Synth.HeavyWhoosh(0.7f, "sfx_safe");
            _footstep = Synth.Footstep("sfx_step");
            _crateThud = Synth.CrateThud("sfx_thud");
            _victory = Synth.VictoryFanfare("sfx_victory");
            _rackActivate = Synth.RackActivate("sfx_rack");
        }
    }

    /// <summary>Tiny procedural-audio helpers.</summary>
    static class Synth
    {
        const int SR = 44100;

        public static AudioClip TwoNoteBlip(float f1, float f2, float dur1, float dur2, string name)
        {
            int n1 = Mathf.RoundToInt(SR * dur1);
            int n2 = Mathf.RoundToInt(SR * dur2);
            int gap = Mathf.RoundToInt(SR * 0.02f);
            int total = n1 + gap + n2;
            var data = new float[total];
            FillSine(data, 0, n1, f1, 0.6f, 0.005f, 0.04f);
            FillSine(data, n1 + gap, n2, f2, 0.6f, 0.005f, 0.06f);
            return MakeClip(name, data);
        }

        public static AudioClip SquareBeep(float freq, float dur, string name)
        {
            int n = Mathf.RoundToInt(SR * dur);
            var data = new float[n];
            float period = SR / freq;
            for (int i = 0; i < n; i++)
            {
                float phase = (i % period) / period;
                float s = phase < 0.5f ? 0.45f : -0.45f;
                float env = Env(i, n, 0.005f, 0.04f);
                data[i] = s * env;
            }
            return MakeClip(name, data);
        }

        public static AudioClip AscendingChord(string name)
        {
            float[] freqs = { 523.25f, 659.25f, 783.99f }; // C5 E5 G5
            float noteDur = 0.10f;
            int per = Mathf.RoundToInt(SR * noteDur);
            int total = per * freqs.Length;
            var data = new float[total];
            for (int k = 0; k < freqs.Length; k++)
                FillSine(data, k * per, per, freqs[k], 0.55f, 0.005f, 0.05f);
            return MakeClip(name, data);
        }

        public static AudioClip ErrorBuzz(string name)
        {
            float[] freqs = { 220f, 165f };
            int per = Mathf.RoundToInt(SR * 0.18f);
            int total = per * freqs.Length;
            var data = new float[total];
            for (int k = 0; k < freqs.Length; k++)
            {
                float f = freqs[k];
                float period = SR / f;
                int start = k * per;
                for (int i = 0; i < per; i++)
                {
                    float phase = (i % period) / period;
                    float s = phase < 0.5f ? 0.55f : -0.55f;
                    data[start + i] = s * Env(i, per, 0.005f, 0.06f);
                }
            }
            return MakeClip(name, data);
        }

        public static AudioClip PowerHum(string name)
        {
            float dur = 0.9f;
            int n = Mathf.RoundToInt(SR * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                // freq sweep from 80Hz -> 220Hz then settled hum at 110Hz
                float k = Mathf.Clamp01(t / 0.5f);
                float freq = Mathf.Lerp(80f, 220f, k);
                if (t > 0.5f) freq = 110f;
                float s = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.45f;
                // add a higher harmonic shimmer once power is up
                if (t > 0.45f) s += Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.12f;
                float env = Env(i, n, 0.05f, 0.20f);
                data[i] = s * env;
            }
            return MakeClip(name, data);
        }

        public static AudioClip NoiseBurst(float dur, float lpFreq, float bpFreq, string name)
        {
            int n = Mathf.RoundToInt(SR * dur);
            var data = new float[n];
            float prev = 0f;
            float alpha = Mathf.Clamp01(2f * Mathf.PI * lpFreq / SR);
            for (int i = 0; i < n; i++)
            {
                float white = Random.Range(-1f, 1f);
                prev = prev + alpha * (white - prev);
                float env = Env(i, n, 0.01f, 0.10f);
                data[i] = prev * 0.7f * env;
            }
            return MakeClip(name, data);
        }

        public static AudioClip HeavyWhoosh(float dur, string name)
        {
            int n = Mathf.RoundToInt(SR * dur);
            var data = new float[n];
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float white = Random.Range(-1f, 1f);
                // sweep low-pass frequency 200Hz -> 1500Hz over time
                float lp = Mathf.Lerp(200f, 1500f, t / dur);
                float a = Mathf.Clamp01(2f * Mathf.PI * lp / SR);
                prev = prev + a * (white - prev);
                // Add low rumble (40Hz)
                float rumble = Mathf.Sin(2f * Mathf.PI * 40f * t) * 0.35f;
                float env = Env(i, n, 0.05f, dur * 0.4f);
                data[i] = (prev * 0.55f + rumble) * env;
            }
            return MakeClip(name, data);
        }

        public static AudioClip Footstep(string name)
        {
            float dur = 0.10f;
            int n = Mathf.RoundToInt(SR * dur);
            var data = new float[n];
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float white = Random.Range(-1f, 1f);
                // low-pass at 600Hz - dull thud
                float a = Mathf.Clamp01(2f * Mathf.PI * 600f / SR);
                prev = prev + a * (white - prev);
                // quick exponential decay
                float env = Mathf.Exp(-t * 30f);
                data[i] = prev * 0.6f * env;
            }
            return MakeClip(name, data);
        }

        public static AudioClip CrateThud(string name)
        {
            float dur = 0.22f;
            int n = Mathf.RoundToInt(SR * dur);
            var data = new float[n];
            float prev = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float white = Random.Range(-1f, 1f);
                // low-pass at 350Hz, plus sine rumble
                float a = Mathf.Clamp01(2f * Mathf.PI * 350f / SR);
                prev = prev + a * (white - prev);
                float rumble = Mathf.Sin(2f * Mathf.PI * 80f * t) * 0.5f;
                float env = Mathf.Exp(-t * 14f);
                data[i] = (prev * 0.7f + rumble) * env;
            }
            return MakeClip(name, data);
        }

        public static AudioClip VictoryFanfare(string name)
        {
            // Triumphant: short C-G-C arpeggio then sustained C-major chord with harmonics
            float[] arp = { 523.25f, 783.99f, 1046.50f }; // C5 G5 C6
            int arpDur = Mathf.RoundToInt(SR * 0.13f);
            int gap = Mathf.RoundToInt(SR * 0.02f);
            int chordDur = Mathf.RoundToInt(SR * 0.95f);
            int total = arp.Length * (arpDur + gap) + chordDur;
            var data = new float[total];

            int cursor = 0;
            for (int k = 0; k < arp.Length; k++)
            {
                FillNoteRich(data, cursor, arpDur, arp[k], 0.45f, 0.005f, 0.04f);
                cursor += arpDur + gap;
            }

            // Final sustained C-major (C5 + E5 + G5 + C6) with slight detune for warmth
            float[] chord = { 523.25f, 659.25f, 783.99f, 1046.50f };
            float amp = 0.20f;
            for (int i = 0; i < chordDur; i++)
            {
                float t = i / (float)SR;
                float s = 0f;
                for (int c = 0; c < chord.Length; c++)
                {
                    float f = chord[c];
                    s += Mathf.Sin(2f * Mathf.PI * f * t) * amp;
                    s += Mathf.Sin(2f * Mathf.PI * (f * 2f) * t) * amp * 0.18f;
                }
                // tremolo: ~6 Hz amplitude wobble for celebratory shimmer
                float trem = 0.85f + 0.15f * Mathf.Sin(2f * Mathf.PI * 6f * t);
                float env = Env(i, chordDur, 0.04f, 0.45f);
                data[cursor + i] = s * trem * env;
            }
            return MakeClip(name, data);
        }

        public static AudioClip RackActivate(string name)
        {
            // ~2.2 s of low mechanical rumble with four "clunk" transients timed
            // to align with BinaryServerArray.RowDelay (0.4 s). Each clunk is a
            // short low-freq sine burst layered over filtered noise so it feels
            // like a physical drawer sliding out.
            float dur = 2.2f;
            int n = Mathf.RoundToInt(SR * dur);
            var data = new float[n];

            // ------ base rumble: 45 Hz + 90 Hz harmonic, slow tremolo ------
            float prev = 0f;
            float a = Mathf.Clamp01(2f * Mathf.PI * 400f / SR);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                float rumble = Mathf.Sin(2f * Mathf.PI * 45f * t) * 0.35f
                             + Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.18f;
                float white = Random.Range(-1f, 1f);
                prev = prev + a * (white - prev);
                float trem = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 3.5f * t);
                float env = Env(i, n, 0.1f, 0.4f);
                data[i] = (rumble + prev * 0.25f) * trem * env;
            }

            // ------ four mechanical clunks at 0.05, 0.45, 0.85, 1.25 s ------
            float[] clunkTimes = { 0.05f, 0.45f, 0.85f, 1.25f };
            for (int k = 0; k < clunkTimes.Length; k++)
            {
                int start = Mathf.RoundToInt(SR * clunkTimes[k]);
                int clunkLen = Mathf.RoundToInt(SR * 0.18f);
                float baseFreq = 120f - k * 10f; // slight pitch descent per row
                float clunkPrev = 0f;
                float clunkA = Mathf.Clamp01(2f * Mathf.PI * 800f / SR);
                for (int i = 0; i < clunkLen && start + i < n; i++)
                {
                    float t = i / (float)SR;
                    float body = Mathf.Sin(2f * Mathf.PI * baseFreq * t) * 0.55f;
                    float w = Random.Range(-1f, 1f);
                    clunkPrev = clunkPrev + clunkA * (w - clunkPrev);
                    float env = Mathf.Exp(-t * 22f);
                    data[start + i] += (body + clunkPrev * 0.45f) * env;
                }
            }

            // clamp
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            return MakeClip(name, data);
        }

        static void FillNoteRich(float[] data, int offset, int count, float freq, float amp, float attack, float release)
        {
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SR;
                float s = Mathf.Sin(2f * Mathf.PI * freq * t) * amp;
                s += Mathf.Sin(2f * Mathf.PI * (freq * 2f) * t) * amp * 0.25f;
                s += Mathf.Sin(2f * Mathf.PI * (freq * 3f) * t) * amp * 0.10f;
                data[offset + i] = s * Env(i, count, attack, release);
            }
        }

        // -- helpers --
        static void FillSine(float[] data, int offset, int count, float freq, float amp, float attack, float release)
        {
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SR;
                float s = Mathf.Sin(2f * Mathf.PI * freq * t) * amp;
                data[offset + i] = s * Env(i, count, attack, release);
            }
        }

        static float Env(int i, int total, float attack, float release)
        {
            float t = i / (float)SR;
            float dur = total / (float)SR;
            float a = attack > 0f ? Mathf.Clamp01(t / attack) : 1f;
            float r = release > 0f ? Mathf.Clamp01((dur - t) / release) : 1f;
            return Mathf.Min(a, r);
        }

        static AudioClip MakeClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
