using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
public class PlayerBehaviourScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private Rigidbody playerrb;
    [SerializeField] private Transform FirePoint_1;
    [SerializeField] private Transform FirePoint_2;
    [SerializeField] private GameObject PlayerGunPrefab;
    private GameObject LeftGun;
    private GameObject RightGun;
    public HealthBar healthBar;
    [SerializeField] private PlayerInput playerInput = null;
    [SerializeField] private CharacterController controller = null;
    [SerializeField] private GameObject fuelArrow;
    [SerializeField] private GameObject speedArrow;

    [Header("Settings")]
    [SerializeField] private float movementSpeed = 20;
    [SerializeField] float playerbulletForce = 20f;
    private bool Rotation;
    public int maxHealth = 20;
    public int currentHealth;
    public float maxFuel = 20;
    public float currentFuel;
    [SerializeField] private float timeBetweenFuelLoss = 3f;
    private float timeForFuelLoss;
    private readonly float lockPos = 0f;
    private Transform cameraTransform;
    private Vector3 playerVelocity;
    private float defSpeed;

    public PlayerInput PlayerInput => playerInput;

    public float TimeBetweenFuelLoss { get { return timeBetweenFuelLoss; } }
    private Scaler scaler;
    private float maxDisplayHeightAtGameplay;
    private float maxDisplayWidthAtGameplay;

    [SerializeField]private float shootTiming;
    private float shootTimer;

    // Start is called before the first frame update
    void Start()
    {
        shootTimer = 0;
        playerrb = GetComponent<Rigidbody>();
        playerrb.useGravity = false;
        playerrb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
        currentHealth = maxHealth;
        healthBar.SetMaxHealth(maxHealth);
        currentFuel = maxFuel;
        timeForFuelLoss = Time.time + timeBetweenFuelLoss;
        Rotation = true;
        cameraTransform = Camera.main.transform;
        defSpeed = movementSpeed;
        fuelArrow.GetComponent<ArrowRotation>().MaxValue = maxFuel;
        speedArrow.GetComponent<ArrowRotation>().MaxValue = defSpeed;
        scaler = gameObject.GetComponent<Scaler>();
        maxDisplayHeightAtGameplay = 2.0f * (Mathf.Abs(Camera.main.transform.position.y)) * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
        maxDisplayWidthAtGameplay = maxDisplayHeightAtGameplay * Camera.main.aspect;
    }

    // Update is called once per frame
    void Update()
    {
        shootTimer += Time.deltaTime;
        if(shootTimer > shootTiming)
        {
            shootTimer = 0;
            playerInput.actions["Shoot"].performed += _ => Shoot();
        }
        Vector2 input = playerInput.actions["Movement"].ReadValue<Vector2>();
        Vector3 move = new Vector3(input.x, 0, input.y);
        move = move.x * cameraTransform.right + move.z * cameraTransform.up;
        move.y = 0f;
        controller.Move(Time.deltaTime * movementSpeed * move);

        controller.Move(playerVelocity * Time.deltaTime);

        float rotationOnZ = 2 * Mathf.Pow(movementSpeed, 2) * 360 * -input.x;
        if (Mathf.Abs(rotationOnZ) > 50) rotationOnZ = 50 * -input.x;
        if (Rotation) transform.rotation = Quaternion.Euler(lockPos, lockPos, rotationOnZ);


        // Player can't leave camera view
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x,  scaler.BorderSizeLeft - maxDisplayWidthAtGameplay / 2, maxDisplayWidthAtGameplay/2 - scaler.BorderSizeRight);
        pos.z = Mathf.Clamp(pos.z, -maxDisplayHeightAtGameplay/2, maxDisplayHeightAtGameplay/2);
        transform.position = pos;

        //FuelConsumption takes effect when time has passed
        if (timeForFuelLoss <= Time.time)
        {
            FuelConsumption(1f);
            timeForFuelLoss = Time.time + timeBetweenFuelLoss;
        }

        if (currentFuel <= 0 || currentHealth <= 0)
        {
            SceneManager.LoadScene("GameOverScreen");
        }
    }

    private void Shoot()
    {
        //Instantiates Bullets at the Gunpoints set on the playerasset
        LeftGun = Instantiate(PlayerGunPrefab, FirePoint_1.position, FirePoint_1.rotation);
        RightGun = Instantiate(PlayerGunPrefab, FirePoint_2.position, FirePoint_2.rotation);
        //adds Rigidbody to the bullets
        Rigidbody bulletrb = LeftGun.GetComponent<Rigidbody>();
        Rigidbody bullet2rb = RightGun.GetComponent<Rigidbody>();
        //adds Force to the bullets which pushes them forward
        bulletrb.AddForce(FirePoint_1.forward * playerbulletForce, ForceMode.Impulse);
        bullet2rb.AddForce(FirePoint_2.forward * playerbulletForce, ForceMode.Impulse);
    }

    private void FuelConsumption(float loss)
    {
        currentFuel -= loss;
        fuelArrow.GetComponent<ArrowRotation>().CurrentValue = currentFuel;
    }

    private void TakeDamage(int damage)
    {
        currentHealth -= damage;
        healthBar.SetHealth(currentHealth);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TakeDamage(2);
    }

    public void reduceSpeed()
    {
        float diff = movementSpeed / 5 * -1;
        movementSpeed += diff;
        StartCoroutine(resetSpeed(5, diff));
        speedArrow.GetComponent<ArrowRotation>().CurrentValue = movementSpeed;
    }

    public void increaseSpeed()
    {
        float diff = defSpeed * 2/3;
        movementSpeed += diff;
        StartCoroutine(resetSpeed(5, diff));
        speedArrow.GetComponent<ArrowRotation>().CurrentValue = movementSpeed;
    }

    public IEnumerator resetSpeed(float timeInSec, float diff)
    {
        yield return new WaitForSeconds(timeInSec);
        diff *= -1;
        movementSpeed += diff;
        speedArrow.GetComponent<ArrowRotation>().CurrentValue = movementSpeed;
    }
}