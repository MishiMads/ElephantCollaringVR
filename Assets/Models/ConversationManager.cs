using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.WitAi.TTS.Utilities;
using Whisper.Samples;

public class ConversationManager : MonoBehaviour
{
    public TTSSpeaker speaker;
    public WhisperTranscriptionService whisper;

    public List<string> dialogueParts;

    private int indexDialogue = 0;
    private bool inQuestionLoop = false;
    private bool ignoreNextLLM = false;
    public bool allowLLM = true;
    private bool exitAfterLLM = false;

    public enum ConversationState
    {
        Idle,
        NPCSpeaking,
        WaitingForPlayer,
        QuestionLoop,
        PlayerSpeaking,
        Processing
    }
    bool IsExitKeyword(string text)
    {
        text = text.ToLower().Trim();

        return text.StartsWith("no")
            || text == "no"
            || text.Contains("no thanks")
            || text.Contains("that's all")
            || text.Contains("nothing else")
            || text.Contains("you can proceed");
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
            rag.CancelRequests(); // stops LLM + TTS
        }

        speaker.Stop(); // stop own speaker too

        if (indexDialogue >= dialogueParts.Count)
        {
            Debug.Log("Conversation complete.");
            currentState = ConversationState.Idle;
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
        speaker.Speak(text);
    }

    // 🎤 Player starts speaking (called externally)
    public void StartPlayerRecording()
    {
        if (currentState != ConversationState.WaitingForPlayer)
            return;

        currentState = ConversationState.PlayerSpeaking;

        whisper.StartRecording();
    }

    // ⏳ Wait for whisper to finish


    void OnPlayerTranscription(string playerText)
    {
        Debug.Log("TRANSCRIPTION TRIGGERED");
        Debug.Log("Player said (FINAL): " + playerText);

        playerText = playerText.ToLower().Trim();

        // 🔥 EXIT LOOP
        if (inQuestionLoop && IsExitKeyword(playerText))
        {
            Debug.Log("Player said NO → final LLM response");

            inQuestionLoop = false;
            currentState = ConversationState.Processing;
            exitAfterLLM = true;

            var rag = FindObjectOfType<LLMUnitySamples.LLMWithRAG>();
            if (rag != null)
            {
                rag.SubmitDirectLLM(
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

        // 🧠 NORMAL FLOW
        currentState = ConversationState.Processing;
        waitingForLLMResponse = true;

        StartCoroutine(WaitForLLMToSpeak());

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
            case ConversationState.Processing:
                waitingForLLMResponse = false;

                if (exitAfterLLM)
                {
                    Debug.Log("LLM finished → exiting loop properly");

                    exitAfterLLM = false;

                    currentState = ConversationState.NPCSpeaking;

                    indexDialogue++;
                    SpeakCurrentDialogue();
                }
                else if (inQuestionLoop)
                {
                    currentState = ConversationState.WaitingForPlayer;

                    speaker.Speak(followUps[Random.Range(0, followUps.Length)]);
                }
                else
                {
                    indexDialogue++;
                    SpeakCurrentDialogue();
                }
                break;
        }
    }

    IEnumerator SpeakInChunks(string text)
    {
        string[] parts = text.Split('\n'); // split by lines

        foreach (var part in SplitByLength(text))
        {
            speaker.Speak(part);
            yield return new WaitUntil(() => !speaker.IsSpeaking);
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
    IEnumerator WaitForLLMToSpeak()
    {
        yield return new WaitUntil(() => speaker.IsSpeaking);

        currentState = ConversationState.Processing;
        waitingForLLMResponse = true;

        Debug.Log("LLM speech detected");
    }
}