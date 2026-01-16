#if CMPSETUP_COMPLETE
using System;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObject/Character Selection")]
public class CharacterSO : ScriptableObject
{
    public List<PlayerSelectionDetails> characters = new List<PlayerSelectionDetails>();
    private const string Key = "Character Selection";

    public int GetSelectedCharacterIndex => PlayerPrefs.GetInt(Key, 0);
    public void SaveSelectedCharacter(PlayerSelectionDetails character)
    {
        var result = characters.IndexOf(character);
        if(result<0)
            return;
        PlayerPrefs.SetInt(Key, result);
    }

    public PlayerSelectionDetails GetSelectedCharacter()
    {
        return characters[PlayerPrefs.GetInt(Key,0)];
    }
}
[Serializable]
public class PlayerSelectionDetails
{
    public string characterName;
    public ThirdPersonController character;
    public GameObject displayModel;
}
#endif