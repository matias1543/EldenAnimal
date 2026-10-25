# ELDEN ANIMAL — Sistema de vida, daño y enemigos

**Programación 2 | Documentación técnica de integración**

## 1. Objetivo

Este documento explica cómo utilizar el sistema compartido de vida y daño (`IDamageable` y `Health`) y la plantilla de enemigos (`EnemyTemplate`) en **Elden Animal**, un juego de acción 3D desarrollado en Unity.

El objetivo es evitar la duplicación de código entre jugador y enemigos y facilitar que los integrantes del equipo trabajen en paralelo.

## 2. Arquitectura

```text
Ataque del jugador / enemigo
           |
           v
IDamageable.TakeDamage(damage)
           |
           v
         Health
           |
           +--> OnHealthChanged --> UI / otros sistemas
           |
           +--> OnDeath ---------> EnemyTemplate / PlayerController
                                     |
                                     +--> Death / Game Over
```

**Responsabilidades:**

- `IDamageable`: contrato que indica que un objeto puede recibir daño.
- `Health`: almacena vida actual y máxima, recibe daño, cura y emite eventos.
- `EnemyTemplate`: controla la detección, persecución, ataque, estados y muerte del enemigo; **no almacena vida**.
- `TurtleEnemy`: ejemplo de enemigo concreto que hereda de `EnemyTemplate` e implementa `Attack()`.
- `PlayerController` / `PlayerAttack`: scripts de prueba del jugador; pueden ser reemplazados por las implementaciones definitivas del equipo.

## 3. IDamageable

Interfaz compartida para cualquier objeto que pueda recibir daño.

```csharp
public interface IDamageable
{
    void TakeDamage(float damage);
}
```

Los ataques deben utilizar esta interfaz en lugar de depender de `Health` o de una clase concreta del jugador/enemigo.

```csharp
IDamageable target = other.GetComponentInParent<IDamageable>();

if (target != null)
{
    target.TakeDamage(10f);
}
```

> `GetComponentInParent` es útil cuando el collider golpeado pertenece a un hijo del objeto que tiene `Health`. Para un collider en el mismo GameObject, también sirve `GetComponent<IDamageable>()`.

## 4. Health

Agregar el componente **Health** al GameObject principal del jugador o enemigo.

### Propiedades públicas (solo lectura)

| Propiedad | Descripción |
| --- | --- |
| `CurrentHealth` | Vida actual. |
| `MaxHealth` | Vida máxima configurada en el Inspector. |
| `IsDead` | Indica si la vida llegó a cero. |

### Métodos

| Método | Descripción |
| --- | --- |
| `TakeDamage(float damage)` | Descuenta vida; ignora daño no positivo o si ya está muerto. |
| `Heal(float amount)` | Recupera vida sin superar el máximo; no revive personajes muertos. |

### Eventos

- `OnHealthChanged(float current, float max)`: se emite cuando un daño o curación válida modifica la vida; sirve para actualizar la UI.
- `OnDeath`: se emite cuando la vida llega a cero; otros componentes deciden cómo reaccionar.

**Importante:** `Health` no reproduce animaciones, no destruye objetos y no presenta pantallas de derrota. Esas responsabilidades pertenecen a otros sistemas.

### Ejemplo para la UI (Marcos)

```csharp
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerHealthUIBridge : MonoBehaviour
{
    private Health _health;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        _health.OnHealthChanged += HandleHealthChanged;
    }

    private void Start()
    {
        // El evento no se emite al inicializar Health.
        // Actualizamos la UI con la vida inicial.
        HandleHealthChanged(_health.CurrentHealth, _health.MaxHealth);
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        // Conectar aquí con la barra de vida real.
        Debug.Log($"Vida: {current}/{max}");
    }
}
```

> Esta clase es un ejemplo de integración, no es necesaria para que `Health` funcione.

## 5. EnemyTemplate

`EnemyTemplate` es una clase **abstracta**. No se agrega directamente a un GameObject: cada enemigo debe utilizar una clase concreta que herede de ella.

### Estados

| Estado | Comportamiento |
| --- | --- |
| `Idle` | Permanece quieto hasta detectar al jugador. |
| `Chase` | Persigue al jugador con `NavMeshAgent`. |
| `Attack` | Ejecuta `Attack()` al estar dentro del rango y cumplir el cooldown. |
| `Death` | Detiene el movimiento, activa la animación `Die` y destruye el objeto tras 2 segundos. |

`EnemyTemplate` obtiene el componente `Health` y se suscribe a `OnDeath` para pasar a `Death` cuando se agota la vida.

### Parámetros configurables en Inspector

| Campo | Descripción |
| --- | --- |
| `Detection Range` | Distancia máxima para detectar/perseguir al jugador. |
| `Attack Range` | Distancia a la que comienza a atacar. |
| `Attack Cooldown` | Tiempo mínimo entre ataques. |
| `Move Speed` | Velocidad del `NavMeshAgent`. |

### Ejemplo: TurtleEnemy (Matías)

```csharp
using UnityEngine;

public class TurtleEnemy : EnemyTemplate
{
    [SerializeField] private float _attackDamage = 10f;

    protected override void Attack()
    {
        if (_animator != null)
            _animator.SetTrigger("Attack");

        if (_player == null)
            return;

        IDamageable target = _player.GetComponent<IDamageable>();

        if (target != null)
            target.TakeDamage(_attackDamage);
    }
}
```

**Nota:** este ejemplo aplica daño en el instante en que se ejecuta `Attack()`, no en el frame de impacto de la animación. Más adelante puede sincronizarse con un **Animation Event**.

## 6. Configuración de la tortuga en Unity

Jerarquía recomendada:

```text
TurtleEnemy (GameObject principal)
├── TurtleEnemy (script)
├── Health
├── NavMeshAgent
├── Collider
└── MonsterModel (hijo visual del asset)
    └── Animator
```

Pasos:

1. Crear o duplicar un prefab de la tortuga y conservar el asset original.
2. En el objeto principal agregar `TurtleEnemy`, `Health`, `NavMeshAgent` y un collider adecuado. `RequireComponent` agrega automáticamente `Health` y `NavMeshAgent` si faltan al incorporar el script.
3. Configurar `Max Health` en `Health` y los rangos, velocidad y cooldown en `TurtleEnemy`.
4. Verificar que el objeto del jugador tenga el **tag `Player`** y el componente `Health`.
5. Configurar una superficie navegable con **NavMesh** para que el enemigo pueda desplazarse.
6. Asignar al Animator del modelo hijo el **Animator Controller correcto**, preferentemente una copia del original del asset.
7. Si `NavMeshAgent` controla el desplazamiento, desactivar **Apply Root Motion** en el Animator para evitar movimientos en conflicto.

### Animator Controller

Parámetros requeridos, respetando exactamente las mayúsculas:

| Parámetro | Tipo | Uso |
| --- | --- | --- |
| `IsMoving` | Bool | Pasar entre quieto y desplazamiento. |
| `Attack` | Trigger | Reproducir el ataque. |
| `Death` | Trigger | Reproducir la muerte. |

Transiciones mínimas:

| Desde | Hacia | Condición | Has Exit Time |
| --- | --- | --- | --- |
| Entry | IdleNormal | Por defecto | — |
| IdleNormal | RunFWD | `IsMoving = true` | Desactivado |
| RunFWD | IdleNormal | `IsMoving = false` | Desactivado |
| IdleNormal | Attack01 | Trigger `Attack` | Desactivado |
| RunFWD | Attack01 | Trigger `Attack` | Desactivado |
| Attack01 | IdleNormal | Sin condición | Activado |
| Any State | Die | Trigger `Death` | Desactivado |

El estado **Die no debe regresar a Idle**. Comprobar que el clip de muerte no esté configurado para repetirse.

## 7. Integración con el jugador (Marcos)

1. Agregar `Health` al GameObject principal del jugador.
2. Configurar su vida máxima desde el Inspector.
3. Para consultar valores, usar `CurrentHealth`, `MaxHealth` e `IsDead`.
4. Para la UI, suscribirse a `OnHealthChanged` y actualizar la barra inicialmente en `Start()`.
5. Para la derrota, suscribirse a `OnDeath` y comunicarlo al sistema de Game Over.
6. No implementar otra variable de vida ni otro `TakeDamage()` en el controlador del jugador.

## 8. Pruebas realizadas

En la escena de prueba se comprobó lo siguiente:

- La tortuga detecta al jugador y lo persigue.
- Reproduce la animación de ataque y descuenta vida al jugador.
- El jugador puede golpear a la tortuga mediante `IDamageable`.
- `Health` reduce la vida y emite `OnDeath` al llegar a cero.
- `EnemyTemplate` pasa al estado `Death`, reproduce la animación y destruye el enemigo.

### Pruebas adicionales recomendadas

- Verificar que un enemigo muerto no vuelva a atacar ni reciba daño.
- Verificar que `Health` nunca quede por debajo de cero ni supere `MaxHealth` al curar.
- Confirmar que un enemigo con varios colliders no reciba varios impactos por un único ataque (el `PlayerAttack` temporal aún no deduplica objetivos).
- Comprobar animaciones y rangos con diferentes prefabs.
- Verificar qué ocurre al morir el jugador y al reiniciar la escena.

## 9. Convenciones de trabajo e integración

- No duplicar lógica de vida o daño en las clases del jugador y enemigos.
- Para aplicar daño utilizar `IDamageable`; para consultar vida o escuchar eventos utilizar `Health`.
- Antes de modificar `Health`, `IDamageable` o `EnemyTemplate`, coordinar con el responsable del sistema.
- Si una clase hija sobrescribe métodos del ciclo de Unity (`Awake`, `OnEnable`, `OnDisable`, `Update`, etc.), llamar a `base.Metodo()` cuando corresponda para conservar la lógica común.
- Hacer commits pequeños y probar la escena tras integrar cambios.
- No modificar directamente los assets originales descargados; usar copias de prefabs y controllers.

## 10. Trabajo pendiente / mejoras opcionales

- Sincronizar el daño de los ataques con las animaciones mediante Animation Events.
- Implementar una UI definitiva de vida y el Game Over.
- Evitar impactos duplicados si un objetivo posee varios colliders.
- Agregar sonidos, efectos y mejoras visuales una vez terminado el gameplay básico.

---

**Entregables internos:**
- `IDamageable` + `Health`: lunes 26/10/2026.
- `EnemyTemplate` + estados básicos: jueves 29/10/2026.

Documento preparado para la integración entre los tres integrantes del equipo de Elden Animal.
