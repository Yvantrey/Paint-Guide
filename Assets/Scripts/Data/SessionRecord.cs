using System;
using System.Collections.Generic;

// One painting session as saved on the phone. Plain fields so JsonUtility can save it.
[Serializable]
public class SessionRecord
{
    public string sessionId;
    public string paintingId;
    public string paintingName;
    public int stageCount;
    public string startTimeUtc; // ISO 8601
    public List<StageRecord> stages = new List<StageRecord>();
    public float totalSeconds;
    public bool completed;

    public DateTime StartTime =>
        DateTime.TryParse(startTimeUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var t) ? t : DateTime.MinValue;
}

[Serializable]
public class StageRecord
{
    public int stageIndex;
    public string completedTimeUtc;
    public float durationSeconds;
}

// JsonUtility cannot save a bare list, so the list is wrapped.
[Serializable]
public class SessionRecordList
{
    public List<SessionRecord> sessions = new List<SessionRecord>();
}
