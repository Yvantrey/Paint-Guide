using System.Collections.Generic;
using UnityEngine;

// Owns everything RUGA loads or saves: the built-in paintings, session history and preferences.
public class DataManager : MonoBehaviour
{
    public const int MaxSavedSessions = 20;

    ISessionRepository sessionRepository;
    IPreferencesRepository preferencesRepository;

    public IReadOnlyList<Painting> Paintings { get; private set; }
    public Preferences Preferences { get; private set; }
    public IReadOnlyList<SessionRecord> Sessions => sessionRepository.LoadAll();

    public void Load()
    {
        sessionRepository = new JsonSessionRepository(Application.persistentDataPath, MaxSavedSessions);
        preferencesRepository = new PlayerPrefsPreferencesRepository();

        Paintings = PaintingFactory.LoadAll();
        Preferences = preferencesRepository.Load();

        if (Paintings.Count == 0)
            Debug.LogError("RUGA: No paintings found in Assets/Resources/Paintings.");
    }

    public bool SaveSession(SessionRecord record) => sessionRepository.Save(record);

    public void SavePreferences() => preferencesRepository.Save(Preferences);
}
