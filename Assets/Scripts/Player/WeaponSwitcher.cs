using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponSwitcher : MonoBehaviour
{
    [System.Serializable]
    private class Weapon
    {
        public AttackBase Attack;
        public GameObject Model;
    }

    [SerializeField] private InputActionReference _swordAction;
    [SerializeField] private InputActionReference _spearAction;
    [SerializeField] private Weapon _sword;
    [SerializeField] private Weapon _spear;

    private void OnEnable()
    {
        if(_swordAction == null)
        {
            Debug.LogError($"{GetType().Name}: falta asignar Sword Action en el Inspector");
            return;
        }
        if(_spearAction == null)
        {
            Debug.LogError($"{GetType().Name}: falta asignar Spear Action en el Inspector");
            return;
        }

        _swordAction.action.Enable();
        _spearAction.action.Enable();
    }

    private void Start()
    {
        Equip(_sword, _spear);
    }

    private void Update()
    {
        if(_swordAction == null) return;
        if(_spearAction == null) return;

        if(_swordAction.action.WasPressedThisFrame()){
            Equip(_sword, _spear);
            return;
        }

        if (_spearAction.action.WasPressedThisFrame())
        {
            Equip(_spear, _sword);
            return;
        }
    }

    private void Equip(Weapon weaponOn, Weapon weaponOff)
    {
        SetWeaponActive(weaponOn, true);
        SetWeaponActive(weaponOff, false);
    }

    private void SetWeaponActive(Weapon weapon, bool isActive)
    {
        if(weapon.Attack == null)
        {
            Debug.LogError($"{GetType().Name}: falta asignar Attack en {weapon}");
            return;
        }
        if(weapon.Model == null)
        {
            Debug.LogError($"{GetType().Name}: falta asignar Model en {weapon}");
            return;
        }

        weapon.Attack.enabled = isActive;
        weapon.Model.SetActive(isActive);
    }

}
