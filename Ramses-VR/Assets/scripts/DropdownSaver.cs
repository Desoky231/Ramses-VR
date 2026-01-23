using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

[System.Serializable]
public class DropdownSelection
{
    public string languages;
    public string Character;
}

public class DropdownSaver : MonoBehaviour
{
    public TMP_Dropdown Language;
    public TMP_Dropdown Character;
    private string filePath;

    private void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, "dropdownSelection.json");
        Debug.Log("File saved at: " + filePath);

        // Save the default selections at the start
        SaveSelectionToJson();
    }

    public void SaveSelectionToJson()
    {
        if (Language == null)
        {
            Debug.LogError("Dropdown 'Language' is not assigned in the Inspector!");
            return;
        }

        if (Character == null)
        {
            Debug.LogError("Dropdown 'Character' is not assigned in the Inspector!");
            return;
        }

        string selectedText = Language.options[Language.value].text;
        string selectedscreen = Character.options[Character.value].text;

        DropdownSelection selection = new DropdownSelection
        {
            languages = selectedText,
            Character = selectedscreen
        };

        string json = JsonUtility.ToJson(selection, true);
        File.WriteAllText(filePath, json);

        Debug.Log("Saved to JSON: " + json);
    }
}
