
/// <summary>
/// Permite modificar el daño antes de que Health lo descuente
/// (por ejemplo, un escudo que lo reduce).
///
/// Health busca estos componentes en su mismo GameObject.
/// Así se agregan defensas nuevas sin modificar Health otra vez.
/// </summary>
public interface IDamageModifier
{
    // Recibe el daño entrante y devuelve el daño final.
    float ModifyDamage(float damage);
}
