
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Un diálogo: quién habla y las líneas que dice (ScriptableObject).
///
/// Los textos se editan desde el Inspector, sin tocar código.
/// </summary>
[CreateAssetMenu(fileName = "Dialogue", menuName = "Elden Animal/Dialogue")]
public class DialogueData : ScriptableObject
{
    [SerializeField] private string _speakerName = "NPC";
    [SerializeField, TextArea(2, 4)] private string[] _lines = new string[0];

    public string SpeakerName => _speakerName;
    public IReadOnlyList<string> Lines => _lines;
}
