using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PsyCurio.Shop.Therapist
{
    /// <summary>
    /// The patient-facing SUDS question (Subjective Units of Distress, 0–100):
    /// large, high-contrast, opened from the therapist panel. The dimmed
    /// backdrop swallows scene clicks while open. Confirm raises the rating
    /// for the session logger and closes the prompt.
    /// </summary>
    public sealed class SudsPrompt : MonoBehaviour
    {
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Slider slider;
        [SerializeField] private TextMeshProUGUI valueLabel;
        [SerializeField] private Button confirmButton;

        public event Action<int> Confirmed;

        public bool IsOpen => promptRoot.activeSelf;

        private void Start()
        {
            slider.onValueChanged.AddListener(value =>
                valueLabel.text = Mathf.RoundToInt(value).ToString());
            confirmButton.onClick.AddListener(Confirm);
        }

        public void Open()
        {
            // Neutral midpoint start — the patient moves it, not a default.
            slider.SetValueWithoutNotify(50f);
            valueLabel.text = "50";
            promptRoot.SetActive(true);
        }

        private void Confirm()
        {
            promptRoot.SetActive(false);
            Confirmed?.Invoke(Mathf.RoundToInt(slider.value));
        }
    }
}
