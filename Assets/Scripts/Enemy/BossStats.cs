
using UnityEngine;

/// <summary>
/// Valores de la pelea contra el jefe (ScriptableObject).
///
/// Permite balancear la pelea (daño, mareo, fase 2) desde el Inspector,
/// sin tocar código.
/// </summary>
[CreateAssetMenu(fileName = "BossStats", menuName = "Elden Animal/Boss Stats")]
public class BossStats : ScriptableObject
{
    [Header("Golpe")]
    [SerializeField] private float _slamDamage = 20f;
    [SerializeField] private float _slamRadius = 3.5f;

    // Tiempo entre el inicio de la animación y el impacto.
    [SerializeField] private float _slamDelay = 0.6f;

    // Ventana en la que el jefe queda quieto después de golpear.
    [SerializeField] private float _dizzyDuration = 2f;

    [Header("Fase 2")]
    [SerializeField, Range(0f, 1f)] private float _enrageThreshold = 0.5f;
    [SerializeField] private float _enragedSpeedMultiplier = 1.5f;
    [SerializeField] private int _minionCount = 2;
    [SerializeField] private float _minionSpawnRadius = 3f;

    public float SlamDamage => _slamDamage;
    public float SlamRadius => _slamRadius;
    public float SlamDelay => _slamDelay;
    public float DizzyDuration => _dizzyDuration;
    public float EnrageThreshold => _enrageThreshold;
    public float EnragedSpeedMultiplier => _enragedSpeedMultiplier;
    public int MinionCount => _minionCount;
    public float MinionSpawnRadius => _minionSpawnRadius;
}
