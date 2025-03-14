using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Weather;

public class RobBasics : MonoBehaviour
{
    // Const
    private int MELEE = 0;
    private int RANGED = 1;
    private int AOE = 2;

    //variables
    //rob values
    public int health = 100;
    [SerializeField] private int startingHealth = 20;
    [SerializeField] private int endMenuSceneIndex = 3;
    public float StartingHealth => startingHealth;
    public float moveSpd = 2;
    [SerializeField] private float speedScalar = 3;
    [SerializeField] private float startingMoveSpeed;
    
    

    // Use this to manage attacks in the inspector (including colliders).
    public Attack[] attacks;

    ////box colliders
    //public BoxCollider2D meleeBox;
    //public BoxCollider2D rangeBox;
    //public BoxCollider2D aoeBox;

    //bools
    private bool isAttacking;

    //animator
    private Animator animator;

    [SerializeField]
    private InputTracker inputTracker;
    [SerializeField]
    private GameObject magnetoRock;
    [SerializeField]
    private Transform rockSpawnPos;
    [SerializeField]
    private GameObject hellfire;
    [SerializeField]
    private Transform bobPos;

    // Jump variables
    bool isGrounded;
    [SerializeField] private float jumpForce = 5f;
    bool applyDownwardForce;
    Rigidbody2D rb;

    [SerializeField] private Image attack1, attack2, attack3;

    private EventBinding<BobRespawnEvent> bobRespawnEvent;
    private EventBinding<WeatherChanged> weatherChangedEventBinding;

    // Cooldown timers
    [Header("Cooldowns")]
    [SerializeField] float meteorAttackCooldown = 2f;
    [SerializeField] float hellfireAttackCooldown = 3f;
    float meteorTimer;
    float hellfireTimer;

    /// <summary>
    /// Reset Robs health on bob respawn
    /// </summary>
    private void OnEnable()
    {
        bobRespawnEvent = new EventBinding<BobRespawnEvent>(() =>
        {
            health = startingHealth;
            EventBus<RobHealthDecrease>.Raise(new RobHealthDecrease()
            {
            });
        });
        
        EventBus<BobRespawnEvent>.Register(bobRespawnEvent);
        
        weatherChangedEventBinding = new EventBinding<WeatherChanged>((weatherChanged) =>
        {
            // Lambda Function: Updates the speed based on weather
            Weather.State state = weatherChanged.WeatherParameters.weatherState;
            if (state == State.SnowStorm || state == State.Snowy)
            {
                moveSpd = startingMoveSpeed / speedScalar;
            }
            else if (state == State.RainStorm || state == State.Rainy) 
            {
                moveSpd = startingMoveSpeed * speedScalar;
            }
            else
            {
                moveSpd = startingMoveSpeed;
            }
        });
        EventBus<WeatherChanged>.Register(weatherChangedEventBinding);
    }

    private void OnDisable()
    {
        EventBus<BobRespawnEvent>.Deregister(bobRespawnEvent);
        EventBus<WeatherChanged>.Deregister(weatherChangedEventBinding);
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    // Start is called before the first frame update
    void Start()
    {
        health = startingHealth;
        moveSpd = startingMoveSpeed;
        //moveSpd = 2;
        isGrounded = true;
        isAttacking = false;
        animator = gameObject.GetComponent<Animator>();
        meteorTimer = Mathf.Infinity;
        hellfireTimer = Mathf.Infinity;

        foreach (Attack attack in attacks)
        {
            attack.collider.gameObject.SetActive(false);
        }

        //get position of bob
        if (bobPos != null)
        {
            bobPos = GameObject.Find("Bob").transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        InputCheck();
        UpdateTimers();
    }
    private void FixedUpdate()
    {
        // If is in the air, bring Rob back down! 
        if (!isGrounded && rb.velocity.y < 9.8f) applyDownwardForce = true;
        if (applyDownwardForce) rb.velocity -= Vector2.up * 0.16f;
    }
    void UpdateTimers()
    {
        meteorTimer += Time.deltaTime;
        hellfireTimer += Time.deltaTime;
    }
    public bool CanUseMeteor()
    {
        if (meteorTimer - attacks[RANGED].totalActionTime < meteorAttackCooldown) return false;
        // If Bob is not inside of the arena, cannot use AOE attacks. 
        if (bobPos.position.x < -12f) return false;
        return true;
    }
    public bool CanUseHellfire()
    {
        if (hellfireTimer - attacks[AOE].totalActionTime < hellfireAttackCooldown) return false;
        // If Bob is not inside of the arena, cannot use AOE attacks. 
        if (bobPos.position.x < -12f) return false;
        return true;
    }

    //Health function. Called from Bob's script.
    public void TakeHealth(int dmg)
    {
        health -= dmg;
        StartCoroutine(Blink());
        EventBus<RobHealthDecrease>.Raise(new RobHealthDecrease());
        if(health <= 0) SceneManager.LoadScene(endMenuSceneIndex);//hardcoded scene
    }
    
    private IEnumerator Blink()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        
        spriteRenderer.color = Color.red;
        
        yield return new WaitForSeconds(0.2f);
        
        spriteRenderer.color = Color.white;
        
    }

    //handles all inputs
    void InputCheck()
    {
        // ban input when attacking.
        if (isAttacking)
        {
            animator.ResetTrigger("trRun");
            animator.SetTrigger("trIdle");
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        //move left
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            transform.localScale = new Vector2(1.5f, 1.5f);
            animator.SetTrigger("trRun");
            rb.velocity = new Vector2(-moveSpd, rb.velocity.y);
        }

        //move right
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            animator.SetTrigger("trRun");
            transform.localScale = new Vector2(-1.5f, 1.5f);
            rb.velocity = new Vector2(moveSpd, rb.velocity.y);
        }

        //stop left
        if (Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.A))
        {
            animator.ResetTrigger("trRun");
            animator.SetTrigger("trIdle");
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        //stop right
        if (Input.GetKeyUp(KeyCode.RightArrow) || Input.GetKeyUp(KeyCode.D))
        {
            animator.ResetTrigger("trRun");
            animator.SetTrigger("trIdle");
            rb.velocity = rb.velocity = new Vector2(0f, rb.velocity.y);
        }

        //attack 1
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            animator.SetTrigger("trMelee");
            StartCoroutine(ConductAttack(attacks[MELEE]));
            StartCoroutine(EnableCollider(attacks[MELEE]));
            AttackImageAnimation(attacks[MELEE].totalActionTime, 1);
        }

        // Check if grounded by raycasting downward and seeing if the ground is getting hit
        RaycastHit2D hit = Physics2D.Raycast(this.transform.position, -transform.up, 3f, LayerMask.GetMask("Ground"));
        if (hit) { isGrounded = true; applyDownwardForce = false; }
        else isGrounded = false;

        // Jump
        if(Input.GetKeyUp(KeyCode.Space) && isGrounded)
        {
            isGrounded = false;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        //attack 2
        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            // If still in cooldown, ignore
            if (!CanUseMeteor()) return;

            animator.SetTrigger("trRanged");
            GameObject rock = GameObject.Instantiate(magnetoRock, rockSpawnPos.position, Quaternion.identity, transform);
            StartCoroutine(ConductAttack(attacks[RANGED]));
            StartCoroutine(EnableCollider(attacks[RANGED]));
            AttackImageAnimation(attacks[RANGED].totalActionTime, 2);
            meteorTimer = 0f;
        }

        //attack 3
        if (Input.GetKeyUp(KeyCode.Alpha3))
        {
            // If still in cooldown, ignore
            if (!CanUseHellfire()) return;

            animator.SetTrigger("trAoe");
            GameObject go = GameObject.Instantiate(hellfire, new Vector3(bobPos.position.x, 0f, 0f), Quaternion.identity);
            StartCoroutine(ConductAttack(attacks[AOE]));
            StartCoroutine(EnableCollider(attacks[AOE]));
            AttackImageAnimation(attacks[AOE].totalActionTime, 3);
            hellfireTimer = 0f;
        }
    }

    private IEnumerator ConductAttack(Attack attack)
    {
        // Set isAttacking to true, so that all other inputs are banned.
        isAttacking = true;

        // Give input tracker attack info.
        inputTracker.AddInput(attack.id.ToString(), attack.collider, attack.timeBeforeHit, attack.totalActionTime);

        // wait until the animation ends.
        yield return new WaitForSeconds(attack.totalActionTime);

        // set isAttcaking to false, so that other inputs are available.
        isAttacking = false;
    }

    private IEnumerator EnableCollider(Attack attack)
    {
        // Activate the corresponding attack object.
        attack.collider.gameObject.SetActive(true);

        // If collider is AoE
        if (attack.collider.gameObject.name == "AoE Attack")
        {
            attack.collider.gameObject.transform.position = new Vector3(bobPos.position.x, bobPos.position.y);
        }

        // wait until hitbox ends.
        yield return new WaitForSeconds(attack.timeBeforeHit);

        // Deactivate the attack object.
        attack.collider.gameObject.SetActive(false);
    }

    ///<summary> Calling this for UI animaiton of selected attack button </summary>
    private void AttackImageAnimation(float attackAnimTime, int attackNum)
    {
        StartCoroutine(gameObject.GetComponent<AttackSelectionHandler>().MoveCard(attack1, attack2, attack3, attackAnimTime, true, attackNum));
    }
}


/// <summary>
/// This is a public class for attack managing.
/// This 'Attack' class can be accessed by any other scripts, so change it carefully.
/// </summary>
[Serializable]
public class Attack
{
    public int id;
    public float occurTimes;

    public bool hasInfluence;
    public float damage;
    public float timeBeforeHit;
    public float totalActionTime;

    public Collider2D collider;


    public float MinRange
    {
        get
        {
            if (!hasInfluence) return 0;

            float localX = Mathf.Abs(collider.transform.localPosition.x);
            float extentX = collider.bounds.extents.x;

            return Mathf.Max(0, localX - extentX);
        }
    }

    public float MaxRange
    {
        get
        {
            if (!hasInfluence) return 0;

            float localX = Mathf.Abs(collider.transform.localPosition.x);
            float extentX = collider.bounds.extents.x;

            return Mathf.Max(0, localX + extentX);
        }
    }
}