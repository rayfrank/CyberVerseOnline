using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PasswordLessonTerminal : MonoBehaviour
{
    [Header("References")]
    public CommandWriter commandWriter;   // your existing screen script
    public TMP_InputField inputField;     // where the player types
    public Button submitButton;           // submit / enter button

    [Header("Console Settings")]
    public int maxLines = 150;
    public bool autoFocusInput = true;

    private readonly List<string> history = new List<string>();
    private int step = 0; // which part of the lesson we’re in

    void Start()
    {
        if (commandWriter == null || inputField == null || submitButton == null)
        {
            Debug.LogError("PasswordLessonTerminal: hook up CommandWriter, InputField and Button in the Inspector.");
            enabled = false;
            return;
        }

        submitButton.onClick.AddListener(OnSubmit);

        // Initial screen text
        history.Clear();
        history.Add("NURU CYBER LAB – PASSWORD SECURITY TERMINAL");
        history.Add("--------------------------------------------------");
        history.Add("Type 'ready' and press ENTER / Submit to begin the lesson.");
        RefreshScreen();

        if (autoFocusInput)
            FocusInput();
    }

    private void OnSubmit()
    {
        string raw = inputField.text;
        string trimmed = raw.Trim();

        if (string.IsNullOrEmpty(trimmed))
            return;

        inputField.text = "";

        string cmdLower = trimmed.ToLowerInvariant();
        string cmdOriginal = trimmed;

        HandleCommand(cmdLower, cmdOriginal);
        RefreshScreen();
    


        if (autoFocusInput)
            FocusInput();
    }

    private void HandleCommand(string cmdLower, string original)
    {
        switch (step)
        {
            case 0:
                HandleIntro(cmdLower, original);
                break;
            case 1:
                HandleStrongPasswordQuiz(cmdLower, original);
                break;
            case 2:
                HandleMfaQuestion(cmdLower, original);
                break;
            case 3:
                HandleWrapUp(cmdLower, original);
                break;
            default:
                history.Clear();
                history.Add("> " + original);
                history.Add("");
                history.Add("You’ve already completed this lesson.");
                history.Add("Type 'restart' to take it again.");

                if (cmdLower == "restart")
                {
                    step = 0;
                    history.Clear();
                    history.Add("NURU CYBER LAB – PASSWORD SECURITY TERMINAL");
                    history.Add("Lesson started. Type 'ready' to begin again.");
                }
                break;
        }
    }

    public void ForceSetScreen(string txt)
    {
        commandWriter.SetCommands(txt, false, false);
    }

    public void ForceSetStep(int s)
    {
        step = s;
    }


    // STEP 0 – waiting for "ready"
    private void HandleIntro(string cmd, string original)
    {
        history.Clear();
        history.Add("> " + original);
        history.Add("");

        if (cmd == "ready" || cmd == "start" || cmd == "go")
        {
            history.Add("Great. Let’s talk about STRONG passwords.");
            history.Add("");
            history.Add("RULES FOR STRONG PASSWORDS:");
            history.Add("  - At least 12 characters");
            history.Add("  - Mix of UPPER + lower letters, numbers, and symbols");
            history.Add("  - Do NOT use your birthday, pet name, or '123456'");
            history.Add("");
            history.Add("QUESTION 1:");
            history.Add("Which of these is the strongest password?");
            history.Add("  A) ray123");
            history.Add("  B) password2024");
            history.Add("  C) Mwanzo@309!");
            history.Add("  D) 123456");
            history.Add("Type A, B, C or D and press ENTER / Submit.");

            step = 1;
        }
        else
        {
            history.Add("Type 'ready' when you are prepared to begin.");
        }
    }

    // STEP 1 – multiple choice strong password
    private void HandleStrongPasswordQuiz(string cmd, string original)
    {
        history.Clear();
        history.Add("> " + original);
        history.Add("");

        if (cmd == "c" || cmd == "c)" || cmd == "3" || cmd.Contains("mwanzo"))
        {
            history.Add("✅ Correct!");
            history.Add("Mwanzo@309! is long, unique and uses letters, numbers and a symbol.");
            history.Add("");
            history.Add("Attackers use tools that try billions of combinations.");
            history.Add("Short or simple passwords fall quickly. Long, random ones survive.");
            history.Add("");
            history.Add("QUESTION 2:");
            history.Add("Even if someone learns your password, what extra layer");
            history.Add("can still protect your account? (hint: 'multi-factor ...')");
            history.Add("Type your answer in words, e.g. 'multi factor authentication' or 'MFA'.");

            step = 2;
        }
        else if (cmd == "a" || cmd == "b" || cmd == "d")
        {
            history.Add("❌ Not quite.");
            history.Add("Think like a hacker: which one would be HARDEST to guess or brute-force?");
            history.Add("Try again: A, B, C or D.");
        }
        else
        {
            history.Add("Please answer with A, B, C or D.");
        }
    }

    // STEP 2 – MFA question
    private void HandleMfaQuestion(string cmd, string original)
    {
        history.Clear();
        history.Add("> " + original);
        history.Add("");

        string c = cmd.Replace("-", " ").Replace("_", " ");

        bool saysMfa = c.Contains("mfa");
        bool says2fa = c.Contains("2fa");
        bool saysFull =
            c.Contains("multi factor authentication") ||
            c.Contains("two factor authentication");

        if (saysMfa || says2fa || saysFull)
        {
            history.Add("✅ Exactly.");
            history.Add("Multi-Factor Authentication (MFA) adds a second proof,");
            history.Add("like a code sent to your phone. Even if someone knows");
            history.Add("your password, they still cannot log in without that code.");
            history.Add("");
            history.Add("FINAL TASK:");
            history.Add("Type ONE rule you will follow from now on to secure");
            history.Add("your passwords. Example: 'I will use 12+ characters'.");
            history.Add("Be specific.");

            step = 3;
        }
        else
        {
            history.Add("Close, but not quite.");
            history.Add("Think of the SMS / app code you have to enter after your password.");
            history.Add("That's called MFA / 2FA.");
            history.Add("Try again: what is that extra layer called?");
        }
    }

    // STEP 3 – player writes their own rule
    private void HandleWrapUp(string cmd, string original)
    {
        history.Clear();
        history.Add("> " + original);
        history.Add("");

        if (cmd.Length < 4)
        {
            history.Add("Give me a real rule 🙂 Something like:");
            history.Add("  'I will stop reusing the same password everywhere.'");
            return;
        }

        history.Add("Nice commitment:");
        history.Add("  \"" + original + "\"");
        history.Add("");
        history.Add("SUMMARY:");
        history.Add("  - Use long, complex passwords (12+ characters).");
        history.Add("  - Never reuse the same password on many sites.");
        history.Add("  - Turn on Multi-Factor Authentication wherever possible.");
        history.Add("");
        history.Add("You’ve completed the PASSWORD GUARDIAN lesson. 🎖");
        history.Add("Type 'restart' if you want to go through it again.");

        step = 4; // finished
    }

    private void RefreshScreen()
    {
        string all = string.Join("\n", history);
        commandWriter.SetCommands(all, randomOrder: false, shouldLoop: false);
    }

    private void FocusInput()
    {
        inputField.ActivateInputField();
        inputField.Select();
    }
}
