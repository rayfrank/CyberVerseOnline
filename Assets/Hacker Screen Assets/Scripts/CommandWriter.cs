using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class CommandWriter : MonoBehaviour
{
    [SerializeField]
    private Material material_output; // the screen material that will display all the commands

    [SerializeField]
    private Camera attached_camera; // the attached camera that renders what is shown in this canvas

    [SerializeField]
    private Text display_command_text; // the text that will show all the commands

    [SerializeField]
    private Image Background; // the background panel

    [SerializeField]
    [Range(0, 0.5f)]
    private float typing_delay; // delay time in seconds before typing the next character

    [SerializeField]
    [Range(30, 70)]
    private int text_font_size = 45; // font size of the command texts

    [SerializeField]
    private Color text_color;

    [SerializeField]
    private Color background_color;

    [SerializeField]
    private bool random_order; // should we print text in the order it is written or randomly?

    [SerializeField]
    private bool loop; // should we keep looping or should we stop at the end of the text. this only works when we do not follow random order

    [SerializeField]
    [Multiline(30)]
    private string all_commands; // the string that contains all the commands

    private List<string> commands_per_new_line_list = new List<string>(); // this will chop the command into new lines and store them as a list

    private float time_counter_for_typing; // will be used for tracking time in seconds

    private RenderTexture render_texture; // render texture for material and camera

    private Queue<char> current_line_command_characters = new Queue<char>(); // a queue of characters to store the strings in a current line

    private int current_line; // the current line we are reading

    private bool stop; // stop when we get to the end of the entire command text, if we are not doing random order

    // Start is called before the first frame update
    void Start()
    {
        attached_camera.gameObject.SetActive(true); // set camera game object active as it should be inactive by default

        render_texture = new RenderTexture(1024, 1024, 16, RenderTextureFormat.ARGB32); // initialize render texture with 1024 X 1024

        attached_camera.targetTexture = render_texture;
        material_output.mainTexture = render_texture;

        display_command_text.fontSize = text_font_size;
        display_command_text.color = text_color;

        Background.color = background_color;

        // split the all command strings with the new line character then add each line to the commands_per_new_line_list
        foreach (string s in all_commands.Split('\n'))
        {
            commands_per_new_line_list.Add(s);
        }

        // if random order then choose random value, otherwise start from 0
        current_line = random_order ? UnityEngine.Random.Range(1, commands_per_new_line_list.Count) : 0;
    }

    // Update is called once per frame
    private void Update()
    {
        if (!stop || loop)
        {
            // wait for delay before typing. The higher the delay, the slower we type
            if (time_counter_for_typing >= typing_delay)
            {
                populateText();
                time_counter_for_typing = 0;
            }
            else
            {
                time_counter_for_typing += Time.deltaTime;
            }
        }
    }

    private void populateText()
    {
        // if there are no characters in current_line_command_characters
        if (current_line_command_characters.Count == 0)
        {
            string sentence = commands_per_new_line_list[current_line];
            foreach (char character in sentence)
            {
                // add every character in the sentence to the current_line_command_characters queue
                current_line_command_characters.Enqueue(character);
            }
            // also add a new line character
            current_line_command_characters.Enqueue('\n');

            if (random_order) // if random order then pick a random sentence
            {
                current_line = UnityEngine.Random.Range(1, commands_per_new_line_list.Count);
            }
            else
            {
                if (current_line == commands_per_new_line_list.Count - 1)
                {
                    // if not random order and we have gotten to the end, then stop and set current line back to zero
                    // setting it back to zero will be useful for looping
                    current_line = 0;
                    stop = true;
                }
                else
                {
                    // if not random order and we have not gotten to the end, just increment current line
                    current_line += 1;
                }
            }
        }
        else // if there are characters, then dequeue them and show them on the display command text
        {
            display_command_text.text += current_line_command_characters.Dequeue();
        }
    }

    // allow other scripts to change what is shown on the hacker screen
    public void SetCommands(string newCommands, bool randomOrder = false, bool shouldLoop = false)
    {
        all_commands = newCommands;
        random_order = randomOrder;
        loop = shouldLoop;

        // reset state
        commands_per_new_line_list.Clear();
        current_line_command_characters.Clear();
        display_command_text.text = "";
        stop = false;
        time_counter_for_typing = 0f;

        foreach (string s in all_commands.Split('\n'))
        {
            commands_per_new_line_list.Add(s);
        }

        current_line = random_order ? UnityEngine.Random.Range(1, commands_per_new_line_list.Count) : 0;
    }
}
