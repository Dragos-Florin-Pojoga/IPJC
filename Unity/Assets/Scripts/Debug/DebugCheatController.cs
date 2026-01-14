using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Debug cheat controller for development/testing.
/// Uses numpad keys for various debug functions.
/// 
/// Controls:
///   Numpad 4 - Randomize player's current spell
///   Numpad 7 - Toggle God Mode (constant healing)
///   Numpad 8 - Previous scene (by build index)
///   Numpad 9 - Next scene (by build index)
///   Numpad 5 - Pause game (Time.timeScale = 0)
///   Numpad 6 - Resume game (Time.timeScale = 1)
/// </summary>
public class DebugCheatController : MonoBehaviour
{
    [Header("God Mode")]
    
    private bool m_godModeActive = false;
    private StatController m_playerStats;
    private PlayerWeaponController m_weaponController;
    
    private GUIStyle m_labelStyle;
    private bool m_isPaused = false;
    private bool m_hasRandomSpell = false;
    private string m_randomSpellInfo = "";
    
    void Start()
    {
        // Find player stats
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) {
            m_playerStats = player.GetComponent<StatController>();
            if (m_playerStats == null) {
                m_playerStats = player.GetComponentInChildren<StatController>();
            }
        }
        
        // Try finding by component if tag didn't work
        if (m_playerStats == null) {
            var playerController = FindFirstObjectByType<PlayerControllerClean>();
            if (playerController != null) {
                m_playerStats = playerController.GetComponent<StatController>();
            }
        }
        
        // Find weapon controller for random spell cheat
        m_weaponController = FindFirstObjectByType<PlayerWeaponController>();
    }
    
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        
        // Numpad 4 - Randomize Spell
        if (keyboard.numpad4Key.wasPressedThisFrame) {
            RandomizeSpell();
        }
        
        // Numpad 7 - Toggle God Mode
        if (keyboard.numpad7Key.wasPressedThisFrame) {
            ToggleGodMode();
        }
        
        // Numpad 8 - Previous Scene
        if (keyboard.numpad8Key.wasPressedThisFrame) {
            LoadPreviousScene();
        }
        
        // Numpad 9 - Next Scene
        if (keyboard.numpad9Key.wasPressedThisFrame) {
            LoadNextScene();
        }
        
        // Numpad 5 - Pause
        if (keyboard.numpad5Key.wasPressedThisFrame) {
            PauseGame();
        }
        
        // Numpad 6 - Resume
        if (keyboard.numpad6Key.wasPressedThisFrame) {
            ResumeGame();
        }
        
        // Apply god mode healing
        if (m_godModeActive && m_playerStats != null) {
            m_playerStats.SetResourceToMax(StatType.Health);
        }
    }
    
    private void ToggleGodMode()
    {
        m_godModeActive = !m_godModeActive;
        Debug.Log($"God Mode: {(m_godModeActive ? "ON" : "OFF")}");
    }
    
    private void RandomizeSpell()
    {
        if (m_weaponController == null) {
            m_weaponController = FindFirstObjectByType<PlayerWeaponController>();
        }
        
        if (m_weaponController == null || m_weaponController.currentWeapon == null) {
            Debug.LogWarning("[DebugCheats] No weapon equipped to randomize!");
            return;
        }
        
        var weapon = m_weaponController.currentWeapon;
        if (weapon.baseSpell == null || weapon.baseSpell.projectilePrefab == null) {
            Debug.LogWarning("[DebugCheats] Weapon has no base spell or projectile prefab!");
            return;
        }
        
        // Generate and apply random spell
        var prefab = weapon.baseSpell.projectilePrefab;
        weapon.baseSpell = RandomSpellGenerator.GenerateRandomSpell(prefab);
        
        m_hasRandomSpell = true;
        m_randomSpellInfo = RandomSpellGenerator.GetSpellSummary(weapon.baseSpell);
        
        Debug.Log($"[DebugCheats] Randomized spell applied!\n{m_randomSpellInfo}");
    }
    
    private void LoadPreviousScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int prevIndex = currentIndex - 1;
        
        if (prevIndex < 0) {
            prevIndex = SceneManager.sceneCountInBuildSettings - 1;
        }
        
        Debug.Log($"Loading previous scene: {prevIndex}");
        SceneManager.LoadScene(prevIndex);
    }
    
    private void LoadNextScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;
        
        if (nextIndex >= SceneManager.sceneCountInBuildSettings) {
            nextIndex = 0;
        }
        
        Debug.Log($"Loading next scene: {nextIndex}");
        SceneManager.LoadScene(nextIndex);
    }
    
    private void PauseGame()
    {
        Time.timeScale = 0f;
        m_isPaused = true;
        Debug.Log("Game Paused");
    }
    
    private void ResumeGame()
    {
        Time.timeScale = 1f;
        m_isPaused = false;
        Debug.Log("Game Resumed");
    }
    
    void OnGUI()
    {
        // Show status in top-left corner
        if (!m_godModeActive && !m_isPaused && !m_hasRandomSpell) return;
        
        if (m_labelStyle == null) {
            m_labelStyle = new GUIStyle(GUI.skin.label);
            m_labelStyle.fontSize = 14;
            m_labelStyle.fontStyle = FontStyle.Bold;
        }
        
        float y = 10;
        
        if (m_godModeActive) {
            m_labelStyle.normal.textColor = Color.green;
            GUI.Label(new Rect(10, y, 200, 25), "GOD MODE", m_labelStyle);
            y += 20;
        }
        
        if (m_isPaused) {
            m_labelStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(10, y, 200, 25), "PAUSED", m_labelStyle);
            y += 20;
        }
        
        if (m_hasRandomSpell) {
            m_labelStyle.normal.textColor = Color.magenta;
            GUI.Label(new Rect(10, y, 300, 25), "RANDOM SPELL", m_labelStyle);
            y += 20;
            m_labelStyle.fontSize = 11;
            m_labelStyle.fontStyle = FontStyle.Normal;
            GUI.Label(new Rect(10, y, 350, 80), m_randomSpellInfo, m_labelStyle);
            m_labelStyle.fontSize = 14;
            m_labelStyle.fontStyle = FontStyle.Bold;
        }
    }
}
