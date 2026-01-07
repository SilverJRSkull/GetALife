using System;
using System.Collections.Generic;
using GetALife.Game;
using TMPro;
using UnityEngine;

namespace GetALife.UI
{
    public class CharacterCreationUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterGenerator characterGenerator;
        [SerializeField] private GameSession gameSession;
        [SerializeField] private UIPanelController panelController;

        [Header("Panels")]
        [SerializeField] private GameObject characterCreationPanel;
        [SerializeField] private GameObject gameplayPanel;

        [Header("Custom Life Inputs")]
        [SerializeField] private TMP_InputField firstNameInput;
        [SerializeField] private TMP_InputField lastNameInput;
        [SerializeField] private TMP_Dropdown countryDropdown;
        [SerializeField] private TMP_Dropdown cityDropdown;
        [SerializeField] private TMP_Dropdown genderDropdown;
        [SerializeField] private TMP_Dropdown birthMonthDropdown;
        [SerializeField] private TMP_Dropdown birthDayDropdown;
        [SerializeField] private TMP_Dropdown birthYearDropdown;

        private readonly List<string> monthOptions = new List<string>
        {
            "January",
            "February",
            "March",
            "April",
            "May",
            "June",
            "July",
            "August",
            "September",
            "October",
            "November",
            "December"
        };

        private void OnEnable()
        {
            EnsureDropdowns();
            RandomizeFromGenerator();
        }

        public void OnStartLifeClicked()
        {
            if (gameSession == null)
            {
                return;
            }

            string firstName = GetInputText(firstNameInput);
            string lastName = GetInputText(lastNameInput);
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                return;
            }

            CharacterData character = new CharacterData
            {
                FirstName = firstName,
                LastName = lastName,
                Country = GetDropdownValue(countryDropdown),
                City = GetDropdownValue(cityDropdown),
                Gender = GetDropdownValue(genderDropdown),
                BirthMonth = GetSelectedMonth(),
                BirthDay = GetSelectedDay(),
                BirthYear = GetSelectedYear()
            };

            gameSession.StartNewLife(character);
            ShowGameplay();
        }

        public void OnRandomizeClicked()
        {
            RandomizeFromGenerator();
        }

        public void OnBirthMonthChanged()
        {
            UpdateDayOptions();
        }

        public void OnCountryChanged()
        {
            UpdateCityOptions();
        }

        public void OnGenderChanged()
        {
            if (characterGenerator == null)
            {
                return;
            }

            if (firstNameInput != null)
            {
                string gender = GetDropdownValue(genderDropdown);
                string newName = characterGenerator.GetRandomFirstName(gender);
                string currentName = firstNameInput.text;

                if (!string.IsNullOrWhiteSpace(currentName) && !string.Equals(newName, currentName, StringComparison.OrdinalIgnoreCase))
                {
                    firstNameInput.SetTextWithoutNotify(newName);
                }
                else
                {
                    string[] namePool = characterGenerator.GetFirstNamesForGender(gender);
                    newName = PickDifferentValue(namePool, currentName);
                    firstNameInput.SetTextWithoutNotify(newName);
                }

                firstNameInput.ForceLabelUpdate();
            }
        }

        public void OnBirthYearChanged()
        {
            UpdateDayOptions();
        }

        private void ShowGameplay()
        {
            if (panelController == null)
            {
                return;
            }

            panelController.NavigateTo(gameplayPanel, new System.Collections.Generic.List<GameObject>
            {
                characterCreationPanel
            });
        }

        private void EnsureDropdowns()
        {
            if (characterGenerator != null)
            {
                PopulateDropdown(countryDropdown, characterGenerator.Countries);
                PopulateDropdown(genderDropdown, characterGenerator.Genders);
            }

            if (birthMonthDropdown != null)
            {
                birthMonthDropdown.ClearOptions();
                birthMonthDropdown.AddOptions(monthOptions);
            }

            if (birthYearDropdown != null)
            {
                birthYearDropdown.ClearOptions();
                birthYearDropdown.AddOptions(BuildYearOptions());
            }

            UpdateCityOptions();
            UpdateDayOptions();
        }

        private void UpdateDayOptions()
        {
            if (birthDayDropdown == null)
            {
                return;
            }

            int year = GetSelectedYear();
            int month = GetSelectedMonth();
            int daysInMonth = year > 0 && month > 0 ? DateTime.DaysInMonth(year, month) : 31;

            int previousDay = GetSelectedDay();
            birthDayDropdown.ClearOptions();
            birthDayDropdown.AddOptions(BuildNumberOptions(1, daysInMonth));

            if (previousDay > 0)
            {
                int clampedDay = Mathf.Clamp(previousDay, 1, daysInMonth);
                birthDayDropdown.value = clampedDay - 1;
                birthDayDropdown.RefreshShownValue();
            }
        }

        private void UpdateCityOptions()
        {
            if (cityDropdown == null)
            {
                return;
            }

            if (characterGenerator == null)
            {
                cityDropdown.ClearOptions();
                return;
            }

            string selectedCountry = GetDropdownValue(countryDropdown);
            string[] cityOptions = characterGenerator.GetCitiesForCountry(selectedCountry);
            PopulateDropdown(cityDropdown, cityOptions);
            SetDropdownRandomValue(cityDropdown, GetDropdownValue(cityDropdown));
            cityDropdown.RefreshShownValue();
        }

        private string GetInputText(TMP_InputField inputField)
        {
            return inputField != null ? inputField.text : string.Empty;
        }

        private string GetDropdownValue(TMP_Dropdown dropdown)
        {
            if (dropdown == null || dropdown.options.Count == 0)
            {
                return string.Empty;
            }

            int index = dropdown.value;
            if (index < 0 || index >= dropdown.options.Count)
            {
                return string.Empty;
            }

            return dropdown.options[index].text;
        }

        private void SetDropdownRandomValue(TMP_Dropdown dropdown)
        {
            if (dropdown == null || dropdown.options.Count == 0)
            {
                return;
            }

            dropdown.value = UnityEngine.Random.Range(0, dropdown.options.Count);
            dropdown.RefreshShownValue();
        }

        private void SetDropdownRandomValue(TMP_Dropdown dropdown, string currentValue)
        {
            if (dropdown == null || dropdown.options.Count == 0)
            {
                return;
            }

            if (dropdown.options.Count == 1)
            {
                dropdown.value = 0;
                dropdown.RefreshShownValue();
                return;
            }

            int currentIndex = dropdown.value;
            if (!string.IsNullOrWhiteSpace(currentValue))
            {
                for (int i = 0; i < dropdown.options.Count; i++)
                {
                    if (string.Equals(dropdown.options[i].text, currentValue, StringComparison.OrdinalIgnoreCase))
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }

            int newIndex = currentIndex;
            int guard = 0;
            while (newIndex == currentIndex && guard < 10)
            {
                newIndex = UnityEngine.Random.Range(0, dropdown.options.Count);
                guard++;
            }

            dropdown.value = newIndex;
            dropdown.RefreshShownValue();
        }

        private string PickDifferentValue(string[] options, string currentValue)
        {
            if (options == null || options.Length == 0)
            {
                return string.Empty;
            }

            if (options.Length == 1)
            {
                return options[0];
            }

            string selection = options[UnityEngine.Random.Range(0, options.Length)];
            int guard = 0;
            while (string.Equals(selection, currentValue, StringComparison.OrdinalIgnoreCase) && guard < 10)
            {
                selection = options[UnityEngine.Random.Range(0, options.Length)];
                guard++;
            }

            return selection;
        }

        private void PopulateDropdown(TMP_Dropdown dropdown, string[] options)
        {
            if (dropdown == null)
            {
                return;
            }

            dropdown.ClearOptions();

            if (options == null || options.Length == 0)
            {
                return;
            }

            dropdown.AddOptions(new List<string>(options));
        }

        private void RandomizeFromGenerator()
        {
            if (characterGenerator == null)
            {
                return;
            }

            PopulateDropdown(genderDropdown, characterGenerator.Genders);
            PopulateDropdown(countryDropdown, characterGenerator.Countries);

            SetDropdownRandomValue(genderDropdown);
            string gender = GetDropdownValue(genderDropdown);

            if (firstNameInput != null)
            {
                firstNameInput.text = characterGenerator.GetRandomFirstName(gender);
            }

            if (lastNameInput != null)
            {
                lastNameInput.text = characterGenerator.GetRandomLastName();
            }

            SetDropdownRandomValue(countryDropdown);
            UpdateCityOptions();
            SetDropdownRandomValue(cityDropdown);

            SetDropdownRandomValue(birthMonthDropdown);
            SetDropdownRandomValue(birthYearDropdown);
            UpdateDayOptions();
            SetDropdownRandomValue(birthDayDropdown);
        }

        private int GetSelectedYear()
        {
            if (birthYearDropdown == null || birthYearDropdown.options.Count == 0)
            {
                return 0;
            }

            string value = birthYearDropdown.options[birthYearDropdown.value].text;
            return int.TryParse(value, out int year) ? year : 0;
        }

        private int GetSelectedMonth()
        {
            if (birthMonthDropdown == null || birthMonthDropdown.options.Count == 0)
            {
                return 0;
            }

            return birthMonthDropdown.value + 1;
        }

        private int GetSelectedDay()
        {
            if (birthDayDropdown == null || birthDayDropdown.options.Count == 0)
            {
                return 0;
            }

            string value = birthDayDropdown.options[birthDayDropdown.value].text;
            return int.TryParse(value, out int day) ? day : 0;
        }

        private List<string> BuildYearOptions()
        {
            int currentYear = DateTime.Now.Year;
            List<string> options = new List<string>();

            for (int year = currentYear; year >= 1950; year--)
            {
                options.Add(year.ToString());
            }

            return options;
        }

        private List<string> BuildNumberOptions(int start, int end)
        {
            List<string> options = new List<string>();

            for (int value = start; value <= end; value++)
            {
                options.Add(value.ToString());
            }

            return options;
        }
    }
}