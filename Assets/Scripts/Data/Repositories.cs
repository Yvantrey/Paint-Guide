using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Repository pattern: the rest of the app only sees these interfaces,
// so how and where data is stored can change without touching app logic.
public interface ISessionRepository
{
    IReadOnlyList<SessionRecord> LoadAll();

    // Adds the session, or replaces it if one with the same sessionId exists. Returns false if saving failed.
    bool Save(SessionRecord record);
}

public interface IPreferencesRepository
{
    Preferences Load();
    void Save(Preferences preferences);
}

// Keeps the newest sessions in one JSON file in the app's private storage.
public class JsonSessionRepository : ISessionRepository
{
    readonly string path;
    readonly int maxRecords;
    List<SessionRecord> cache;

    public JsonSessionRepository(string directory, int maxRecords)
    {
        path = Path.Combine(directory, "sessions.json");
        this.maxRecords = maxRecords;
    }

    public IReadOnlyList<SessionRecord> LoadAll()
    {
        if (cache != null)
            return cache;

        cache = new List<SessionRecord>();
        try
        {
            if (File.Exists(path))
            {
                var list = JsonUtility.FromJson<SessionRecordList>(File.ReadAllText(path));
                if (list?.sessions != null)
                    cache = list.sessions;
            }
        }
        catch (Exception e)
        {
            // A damaged file shouldn't stop the app. Start fresh and keep the bad file for inspection.
            Debug.LogWarning($"RUGA: Could not read saved sessions ({e.Message}). Starting with an empty history.");
            TryBackup();
        }

        return cache;
    }

    public bool Save(SessionRecord record)
    {
        var sessions = new List<SessionRecord>(LoadAll());
        sessions.RemoveAll(s => s.sessionId == record.sessionId);
        sessions.Add(record);

        sessions = sessions
            .OrderByDescending(s => s.StartTime)
            .Take(maxRecords)
            .ToList();

        try
        {
            // Write to a temporary file first, so a crash mid-write can't destroy the saved history.
            var json = JsonUtility.ToJson(new SessionRecordList { sessions = sessions });
            var temp = path + ".tmp";
            File.WriteAllText(temp, json);
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temp, path);
            cache = sessions;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"RUGA: Could not save the session: {e.Message}");
            return false;
        }
    }

    void TryBackup()
    {
        try
        {
            File.Copy(path, path + ".bad", true);
        }
        catch (Exception)
        {
            // Nothing more we can do; the history just starts empty.
        }
    }
}

public class PlayerPrefsPreferencesRepository : IPreferencesRepository
{
    const string ColourKey = "ruga.guideColour";
    const string OpacityKey = "ruga.defaultOpacity";

    public Preferences Load()
    {
        var defaults = new Preferences();
        var colour = PlayerPrefs.GetInt(ColourKey, (int)defaults.GuideColour);
        return new Preferences
        {
            GuideColour = Enum.IsDefined(typeof(GuideColour), colour) ? (GuideColour)colour : defaults.GuideColour,
            DefaultOpacity = Mathf.Clamp(PlayerPrefs.GetFloat(OpacityKey, defaults.DefaultOpacity), Preferences.MinOpacity, Preferences.MaxOpacity)
        };
    }

    public void Save(Preferences preferences)
    {
        PlayerPrefs.SetInt(ColourKey, (int)preferences.GuideColour);
        PlayerPrefs.SetFloat(OpacityKey, preferences.DefaultOpacity);
        PlayerPrefs.Save();
    }
}
