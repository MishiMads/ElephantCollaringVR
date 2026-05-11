using Meta.WitAi.TTS.Utilities;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Whisper.Samples;
using Whisper.Utils;

public class ConversationManager : MonoBehaviour
{
    public TTSSpeaker speaker;
    public WhisperTranscriptionService whisper;
    public EyelidController eyelidController;

    public List<string> dialogueParts;

    private int indexDialogue = 0;
    private bool inQuestionLoop = false;
    private bool ignoreNextLLM = false;
    public bool allowLLM = true;
    private bool exitAfterLLM = false;
    private bool isFreeConversationMode = false;

    private int freeQuestionCount = 0;
    private bool waitingForProcedureAnswer = false;

    private int questionIndexForProcedure = -1;

    private bool procedureStarted = false;

    public List<GameObject> itemsAndElephant;
    public List<GameObject> dudesA;

    public GameObject procedureObjects;

    public enum ConversationState
    {
        Idle,
        NPCSpeaking,
        LLMSpeaking,
        WaitingForPlayer,
        QuestionLoop,
        PlayerSpeaking,
        Processing,
        FreeConversation
    }
    bool IsExitKeyword(string text)
    {
        text = text.ToLower().Trim();

        string[] noKeywords =
        {
        "no", "nope", "nah", "negative", "not now","not really","i'm good","im good",
        "we're good","were good", "that's all", "thats all", "nothing else", "no thanks",
        "no thank you","don't", "dont", "stop", "cancel", "exit", "quit",
        "leave", "end", "finished", "done", "all set", "you can proceed", "go ahead",
        "continue"
    };

        return noKeywords.Any(k => text.Contains(k));
    }

    bool IsYesKeyword(string text)
    {
        text = text.ToLower().Trim();

        string[] yesKeywords =
        {
        "yes", "yeah", "yep", "yup","sure","okay","ok","affirmative", "definitely",
        "absolutely","certainly", "of course", "sounds good", "why not",
        "please do", "go ahead", "continue", "proceed", "do it", "let's do it",
        "lets do it", "i agree", "works for me", "fine", "alright", "all right"
    };

        return yesKeywords.Any(k => text.Contains(k));
    }

    string[] followUps = {
    "Any more questions?",
    "Is there anything else you'd like to ask?",
    "Do you need clarification on anything?",
    "Still unsure about something?"
};

    private ConversationState currentState = ConversationState.Idle;

    private bool waitingForLLMResponse = false;

    void Start()
    {
        speaker.Events.OnPlaybackComplete.AddListener(OnTTSFinished);


        whisper.OnTranscriptCompleted += OnPlayerTranscription;

        StartConversation();
    }
    public void SafeSpeak(string text)
    {
        if (speaker.IsSpeaking)
        {
            speaker.Stop();
        }

        speaker.Speak(text);
    }

    // 🚀 START
    void StartConversation()
    {
        if (dialogueParts.Count == 0)
        {
            Debug.Log("No dialogue.");
            return;
        }

        SpeakCurrentDialogue();
    }

    // 🗣 NPC speaks predetermined line
    void SpeakCurrentDialogue()
    {
        // 🛑 STOP ANY OTHER TTS SYSTEM FIRST
        var rag = FindObjectOfType<LLMUnitySamples.LLMWithRAG>();
        if (rag != null)
        {
            if (waitingForLLMResponse)
            {
                rag.CancelRequests();
            } // stops LLM + TTS
        }

        speaker.Stop(); // stop own speaker too

        if (indexDialogue >= dialogueParts.Count)
        {
            Debug.Log("Dialogue finished → entering free conversation mode");

            currentState = ConversationState.FreeConversation;
            isFreeConversationMode = true;
            inQuestionLoop = false;

            SafeSpeak("That’s everything. You can ask me anything now.");

            return;
        }

        currentState = ConversationState.NPCSpeaking;

        string text = dialogueParts[indexDialogue];
        Debug.Log("NPC says: " + text);

        StartCoroutine(SpeakInChunks(text));
    }

    public void OnLLMResponseReady(string text)
    {
        Debug.Log("LLM says: " + text);

        speaker.Stop();

        currentState = ConversationState.LLMSpeaking; // 🔥 ADD THIS

        SafeSpeak(text);
        waitingForLLMResponse = false;

        // 🔥 NEW: handle procedure trigger HERE instead of TTS
        if (isFreeConversationMode && freeQuestionCount == questionIndexForProcedure)
        {
            questionIndexForProcedure = -1;
            waitingForProcedureAnswer = true;

            StartCoroutine(AskProcedureAfterDelay());
        }
    }

    // 🎤 Player starts speaking (called externally)
    public void StartPlayerRecording()
    {
        if (waitingForLLMResponse)
        {
            Debug.Log("Blocked recording: waiting for LLM");
            return;
        }

        if (currentState != ConversationState.WaitingForPlayer
            && currentState != ConversationState.FreeConversation)
            return;

        if (speaker.IsSpeaking)
        {
            Debug.Log("Blocked recording: NPC is speaking");
            return;
        }

        currentState = ConversationState.PlayerSpeaking;
        whisper.StartRecording();
    }

    // ⏳ Wait for whisper to finish


    void OnPlayerTranscription(string playerText)
    {
        var rag = FindObjectOfType<LLMUnitySamples.LLMWithRAG>();
        if (waitingForLLMResponse)
        {
            Debug.Log("Ignoring duplicate transcription");
            return;
        }
        Debug.Log("TRANSCRIPTION TRIGGERED");
        Debug.Log("Player said (FINAL): " + playerText);

        playerText = playerText.ToLower().Trim();

        if (isFreeConversationMode)
        {
            Debug.Log("Free conversation mode → handling input");

            // 🟡 If we're waiting for YES/NO about procedure
            if (waitingForProcedureAnswer)
            {
                if (IsYesKeyword(playerText))
                {
                    Debug.Log("User is ready → starting procedure");

                    waitingForProcedureAnswer = false;

                    StartProcedure();

                    currentState = ConversationState.WaitingForPlayer;
                    return;
                }
                else if (IsExitKeyword(playerText)) // your "no"
                {
                    Debug.Log("User not ready → continue conversation");

                    waitingForProcedureAnswer = false;
                    currentState = ConversationState.WaitingForPlayer;
                    return;
                }
                else
                {
                    // unclear answer → ask again
                    SafeSpeak("Please say yes when you're ready, or no if you need more time.");
                    return;
                }
            }

            // 🧠 Count questions
            freeQuestionCount++;

            Debug.Log("Free question count: " + freeQuestionCount);

            // 🔥 Every 3 questions → ask about procedure
            if (!procedureStarted && freeQuestionCount % 3 == 0)
            {
                Debug.Log("Will ask procedure AFTER LLM response");

                questionIndexForProcedure = freeQuestionCount;
            }

            // 🧠 Normal LLM flow
            currentState = ConversationState.Processing;

            
            if (rag != null)
            {
                if (waitingForLLMResponse)
                {
                    rag.CancelRequests();
                }
                rag.SubmitExternalInput(playerText); //with rag
                waitingForLLMResponse = true;
            }

            return;
        }

        // 🔥 EXIT LOOP
        if (inQuestionLoop && IsExitKeyword(playerText))
        {
            Debug.Log("Player said NO → final LLM response");

            inQuestionLoop = false;
            currentState = ConversationState.Processing;
            exitAfterLLM = true;

            
            if (rag != null)
            {
                rag.SubmitExternalInput(
                    "The user said they have no more questions. Reply briefly like: 'Alright, let's continue.'"
                );
            }

            return;
        }

        // 🚫 PREVENT accidental LLM trigger
        if (ignoreNextLLM)
        {
            ignoreNextLLM = false;
            Debug.Log("LLM response ignored");
            return;
        }

        currentState = ConversationState.Processing;

        
        if (rag != null)
        {
            // rag.SubmitDirectLLM(playerText); //not with rag
            rag.SubmitExternalInput(playerText); //with rag
            waitingForLLMResponse = true;
        }

    }



    // 🔥 THE CORE: handles ALL speech endings
    void OnTTSFinished(TTSSpeaker speaker, Meta.WitAi.TTS.Data.TTSClipData clip)
    {
        Debug.Log("TTS finished: " + clip.textToSpeak);

        switch (currentState)
        {
            // ✅ Predetermined NPC line finished
            case ConversationState.NPCSpeaking:
                inQuestionLoop = true;
                currentState = ConversationState.WaitingForPlayer;

                Debug.Log("Entering question loop...");
                break;

            // ✅ LLM response finished
            case ConversationState.LLMSpeaking:




                if (exitAfterLLM)
                {
                    Debug.Log("LLM finished → exiting loop properly");

                    exitAfterLLM = false;

                    currentState = ConversationState.NPCSpeaking;

                    indexDialogue++;
                    SpeakCurrentDialogue();
                }
                else if (isFreeConversationMode)
                {


                    currentState = ConversationState.WaitingForPlayer;
                }
                else if (inQuestionLoop)
                {
                    currentState = ConversationState.WaitingForPlayer;

                    SafeSpeak(followUps[Random.Range(0, followUps.Length)]);
                }
                else
                {
                    indexDialogue++;
                    SpeakCurrentDialogue();
                }
                break;
        }
    }
    IEnumerator AskProcedureAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);

        currentState = ConversationState.WaitingForPlayer; // 🔥 IMPORTANT
        SafeSpeak("Are you ready for the procedure?");
    }
    IEnumerator SpeakInChunks(string text)
    {
        string[] parts = text.Split('\n');

        foreach (var line in parts)
        {
            foreach (var chunk in SplitByLength(line))
            {
                SafeSpeak(chunk);
                yield return new WaitUntil(() => !speaker.IsSpeaking);
            }
        }
    }

    List<string> SplitByLength(string text, int maxChars = 200)
    {
        List<string> result = new List<string>();

        while (text.Length > maxChars)
        {
            int splitIndex = text.LastIndexOf('.', maxChars);
            if (splitIndex <= 0) splitIndex = maxChars;

            result.Add(text.Substring(0, splitIndex + 1));
            text = text.Substring(splitIndex + 1);
        }

        if (!string.IsNullOrWhiteSpace(text))
            result.Add(text);

        return result;
    }

    // 🧠 Call this FROM your LLM system when it starts speaking
    /*IEnumerator WaitForLLMToSpeak()
    {
        yield return new WaitUntil(() => speaker.IsSpeaking);

        currentState = ConversationState.Processing;
        waitingForLLMResponse = true;

        Debug.Log("LLM speech detects");

    }*/

    void StartProcedure()
    {
        if (procedureStarted)
            return;

        StartCoroutine(StartProcedureSequence());
    }

    IEnumerator StartProcedureSequence()
    {
        Debug.Log("🚀 PROCEDURE STARTED");

        freeQuestionCount = 0;
        procedureStarted = true;
        waitingForProcedureAnswer = false;

        currentState = ConversationState.Processing;

        SafeSpeak("Alright, we’ll begin the procedure now.");

        yield return new WaitUntil(() => !speaker.IsSpeaking);

        if (eyelidController != null)
        {
            yield return StartCoroutine(eyelidController.PlayProcedureTransition(() =>
            {
                procedureObjects.SetActive(true);
            }));
        }
        else
        {
            procedureObjects.SetActive(true);
        }

        SafeSpeak("You can still ask questions at any time.");

        currentState = ConversationState.WaitingForPlayer;
    }
}
