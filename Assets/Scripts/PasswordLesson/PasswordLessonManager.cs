using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PasswordLessonManager : MonoBehaviour
{
    [Header("Optional: Hacker Screen Terminal")]
    public CommandWriter commandWriter;   // leave null if you don't want to mirror text on the 3D screen

    [Header("Training Panel UI")]
    public TMP_Text lessonTitleText;
    public TMP_Text progressText;
    public TMP_Text mainBodyText;
    public TMP_Text feedbackText;

    [Header("Answer Buttons & Labels")]
    public GameObject answersGroup;       // parent object of the 4 answer buttons
    public Button[] answerButtons;        // size 4
    public TMP_Text[] answerLabelTexts;       // size 4, labels on buttons A–D

    [Header("Lesson Texts")]
    [TextArea(3, 10)]
    public string introText =
        "Welcome to Lesson 1: Password Security.\n\n" +
        "A strong password is your first line of defense.\n" +
        "- Use 12+ characters\n" +
        "- Mix letters, numbers & symbols\n" +
        "- Do NOT use birthdays, pet names, or 123456\n";

    [TextArea(3, 10)]
    public string strongPasswordExample =
        "Examples of strong passwords:\n\n" +
        "Mwanzo@309!\n" +
        "Ray@2025!mk\n\n" +
        "These are long, unique and hard to guess.\n";

    [TextArea(3, 10)]
    public string quizPrompt =
        "QUIZ: Which password is strongest?";

    // answers for the quiz UI
    private string[] quizAnswers =
    {
        "A) ray123",
        "B) password2024",
        "C) Mwanzo@309!",
        "D) 123456"
    };

    // 0 = A, 1 = B, 2 = C, 3 = D
    private int correctAnswerIndex = 2;

    private void Start()
    {
        // default view
        ShowIntro();
    }

    // called by "Intro" button
    public void ShowIntro()
    {
        SetCommonHeader("Lesson 1 – Password Security", "Step 1 of 3");

        mainBodyText.text = introText;
        feedbackText.text = "";
        answersGroup.SetActive(false);

        if (commandWriter != null)
        {
            commandWriter.SetCommands(introText, randomOrder: false, shouldLoop: false);
        }
    }

    // called by "Examples" button
    public void ShowExamples()
    {
        SetCommonHeader("Lesson 1 – Examples", "Step 2 of 3");

        mainBodyText.text = strongPasswordExample;
        feedbackText.text = "";
        answersGroup.SetActive(false);

        if (commandWriter != null)
        {
            commandWriter.SetCommands(strongPasswordExample, randomOrder: false, shouldLoop: false);
        }
    }

    // called by "Quiz" button
    public void ShowQuiz()
    {
        SetCommonHeader("Lesson 1 – Quiz", "Step 3 of 3");

        mainBodyText.text = quizPrompt;
        feedbackText.text = "Choose the best answer below.";
        answersGroup.SetActive(true);

        // fill answer labels
        for (int i = 0; i < answerLabelTexts.Length && i < quizAnswers.Length; i++)
        {
            answerLabelTexts[i].text = quizAnswers[i];
        }

        // re-enable buttons (in case we disabled them earlier)
        foreach (var btn in answerButtons)
        {
            btn.interactable = true;
        }

        if (commandWriter != null)
        {
            // show question + options on hacker screen as well
            string combined = quizPrompt + "\n\n" +
                              string.Join("\n", quizAnswers);
            commandWriter.SetCommands(combined, randomOrder: false, shouldLoop: false);
        }
    }

    // called by answer buttons: A->0, B->1, C->2, D->3
    public void Answer(int index)
    {
        // simple safety
        if (index < 0 || index > 3) return;

        // optional: stop spamming clicks
        foreach (var btn in answerButtons)
        {
            btn.interactable = false;
        }

        if (index == correctAnswerIndex)
        {
            feedbackText.text =
                "✅ Correct! Mwanzo@309! is long, unique and hard to crack.\n" +
                "Hongera, you earned the Password Guardian badge.";

            if (commandWriter != null)
            {
                commandWriter.SetCommands(
                    "Correct! Mwanzo@309! is long, unique and hard to crack.\n" +
                    "Hongera, you earned the Password Guardian badge.\n",
                    randomOrder: false,
                    shouldLoop: false
                );
            }
        }
        else
        {
            feedbackText.text =
                "❌ Not quite.\n" +
                "Remember: strong passwords are long and unpredictable.\n" +
                "Try again, think like a hacker.";

            if (commandWriter != null)
            {
                commandWriter.SetCommands(
                    "Not quite.\n" +
                    "Remember: strong passwords are long and unpredictable.\n" +
                    "Jaribu tena, think kama hacker.\n",
                    randomOrder: false,
                    shouldLoop: false
                );
            }
        }
    }

    private void SetCommonHeader(string title, string progress)
    {
        if (lessonTitleText != null)
            lessonTitleText.text = title;

        if (progressText != null)
            progressText.text = progress;
    }
}
