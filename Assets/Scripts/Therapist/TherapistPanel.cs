using PsyCurio.Shop.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PsyCurio.Shop.Therapist
{
    /// <summary>
    /// The therapist's live exposure-intensity controls, toggled with T and
    /// visually distinct from the patient view. Owns the single live
    /// ExposureSettings instance — the session log snapshots it per SUDS
    /// rating — and applies every change immediately to the scene.
    /// </summary>
    public sealed class TherapistPanel : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Toggle eyeContactToggle;
        [SerializeField] private Slider delaySlider;
        [SerializeField] private TextMeshProUGUI delayLabel;
        [SerializeField] private Slider bystanderSlider;
        [SerializeField] private TextMeshProUGUI bystanderLabel;
        [SerializeField] private Slider noiseSlider;
        [SerializeField] private TextMeshProUGUI noiseLabel;

        [Header("Scene targets")]
        [SerializeField] private CashierEyeContact eyeContact;
        [SerializeField] private Cashier cashier;
        [SerializeField] private BystanderSpawner bystanders;
        [SerializeField] private AmbientNoise ambientNoise;

        [Header("SUDS")]
        [SerializeField] private Button sudsButton;
        [SerializeField] private SudsPrompt sudsPrompt;

        private readonly ExposureSettings settings = new ExposureSettings();

        /// <summary>Live settings; snapshot before persisting.</summary>
        public ExposureSettings CurrentSettings => settings;

        private void Start()
        {
            // Start, not Awake: every scene target must have finished its own
            // Awake before defaults are pushed into it.
            // UI reflects the code defaults once, then the listeners own it.
            eyeContactToggle.SetIsOnWithoutNotify(settings.eyeContact);
            delaySlider.SetValueWithoutNotify(cashier.ResponseDelaySeconds);
            bystanderSlider.SetValueWithoutNotify(0f);
            noiseSlider.SetValueWithoutNotify(0f);

            eyeContactToggle.onValueChanged.AddListener(ApplyEyeContact);
            delaySlider.onValueChanged.AddListener(ApplyDelay);
            bystanderSlider.onValueChanged.AddListener(ApplyBystanders);
            noiseSlider.onValueChanged.AddListener(ApplyNoise);
            sudsButton.onClick.AddListener(sudsPrompt.Open);

            ApplyEyeContact(eyeContactToggle.isOn);
            ApplyDelay(delaySlider.value);
            ApplyBystanders(bystanderSlider.value);
            ApplyNoise(noiseSlider.value);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                TogglePanel();
            }
        }

        /// <summary>Also bound to the on-screen Therapist button — touch
        /// devices have no T key, and the brief wants everything clickable.</summary>
        public void TogglePanel()
        {
            panelRoot.SetActive(!panelRoot.activeSelf);
        }

        private void ApplyEyeContact(bool value)
        {
            settings.eyeContact = value;
            eyeContact.EyeContact = value;
        }

        private void ApplyDelay(float value)
        {
            settings.responseDelaySeconds = value;
            cashier.ResponseDelaySeconds = value;
            delayLabel.text = $"Response delay: {value:0.0} s";
        }

        private void ApplyBystanders(float value)
        {
            // Clamp before storing: the logged snapshot must record how many
            // bystanders actually spawned, not what the slider asked for.
            var count = Mathf.Clamp(Mathf.RoundToInt(value), 0, bystanders.MaxCount);
            settings.bystanderCount = count;
            bystanders.SetCount(count);
            bystanderLabel.text = $"Bystanders: {count}";
        }

        private void ApplyNoise(float value)
        {
            settings.ambientNoiseLevel = value;
            ambientNoise.SetLevel(value);
            noiseLabel.text = $"Ambient noise: {Mathf.RoundToInt(value * 100f)} %";
        }
    }
}
