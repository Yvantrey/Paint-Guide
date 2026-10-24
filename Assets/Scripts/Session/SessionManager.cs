using System;
using UnityEngine;

// Tracks one painting session: which stage the painter is on and how long each stage takes.
// The timer only runs while the guide is locked and the app is open.
public class SessionManager : MonoBehaviour
{
    public event Action<int> OnStageChanged;
    // Raised after every completed stage, so progress can be saved straight away.
    public event Action<SessionRecord> OnStageCompleted;
    public event Action<SessionRecord> OnSessionCompleted;

    float[] stageSeconds;
    string[] stageCompletedAt;
    bool timing;
    double timerStartedAt;
    bool resumeAfterAppPause;

    public Painting Painting { get; private set; }
    public SessionRecord Record { get; private set; }
    public int StageIndex { get; private set; }
    public bool IsActive => Painting != null;
    public bool IsLastStage => IsActive && StageIndex == Painting.Stages.Count - 1;

    static double Now => Time.realtimeSinceStartupAsDouble;

    public void Begin(Painting painting)
    {
        int count = painting.Stages.Count;
        Painting = painting;
        StageIndex = 0;
        stageSeconds = new float[count];
        stageCompletedAt = new string[count];
        timing = false;
        Record = new SessionRecord
        {
            sessionId = Guid.NewGuid().ToString("N"),
            paintingId = painting.Id,
            paintingName = painting.Name,
            stageCount = count,
            startTimeUtc = DateTime.UtcNow.ToString("o")
        };
        OnStageChanged?.Invoke(StageIndex);
    }

    public void End()
    {
        PauseTimer();
        Painting = null;
    }

    public void StartTimer()
    {
        if (!IsActive || timing)
            return;
        timing = true;
        timerStartedAt = Now;
    }

    public void PauseTimer()
    {
        if (!timing)
            return;
        stageSeconds[StageIndex] += (float)(Now - timerStartedAt);
        timing = false;
    }

    public void Next()
    {
        if (!IsActive)
            return;

        bool wasTiming = timing;
        PauseTimer();
        if (stageCompletedAt[StageIndex] == null)
            stageCompletedAt[StageIndex] = DateTime.UtcNow.ToString("o");

        bool finished = IsLastStage;
        UpdateRecord(finished);
        OnStageCompleted?.Invoke(Record);

        if (finished)
        {
            OnSessionCompleted?.Invoke(Record);
            return;
        }

        StageIndex++;
        OnStageChanged?.Invoke(StageIndex);
        if (wasTiming)
            StartTimer();
    }

    public void Previous()
    {
        if (!IsActive || StageIndex == 0)
            return;

        bool wasTiming = timing;
        PauseTimer();
        StageIndex--;
        OnStageChanged?.Invoke(StageIndex);
        if (wasTiming)
            StartTimer();
    }

    void UpdateRecord(bool completed)
    {
        Record.stages.Clear();
        float total = 0f;
        for (int i = 0; i < stageSeconds.Length; i++)
        {
            total += stageSeconds[i];
            if (stageCompletedAt[i] != null)
            {
                Record.stages.Add(new StageRecord
                {
                    stageIndex = i,
                    completedTimeUtc = stageCompletedAt[i],
                    durationSeconds = stageSeconds[i]
                });
            }
        }
        Record.totalSeconds = total;
        Record.completed = completed;
    }

    // Don't count time while the app is in the background.
    void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            resumeAfterAppPause = timing;
            PauseTimer();
        }
        else if (resumeAfterAppPause)
        {
            resumeAfterAppPause = false;
            StartTimer();
        }
    }
}
