using UnityEngine;

[RequireComponent(typeof(Animator))]
public class MenuZombieShamble : MonoBehaviour
{
    public float walkAnimSpeed = 2f;
    public float killDistance = 4f;
    public Vector3 spawnPosition;

    Animator animator;
    Vector3 forward;
    Transform cam;
    float speed;

    void Awake()
    {
        animator = GetComponent<Animator>();
        forward = transform.forward;
    }

    void Start()
    {
        ApplyWalkState();
        cam = Camera.main != null ? Camera.main.transform : null;
    }

    public void ApplyWalkState()
    {
        animator.SetBool("isPatrolling", true);
        animator.Play("Patrolling_State", 0, Random.value);
        animator.speed = Random.Range(0.78f, 1.06f);
        speed = walkAnimSpeed * animator.speed;
    }

    void Update()
    {
        transform.position += forward * speed * Time.deltaTime;

        if (spawnPosition == Vector3.zero || cam == null) return;

        Vector3 flatDelta = transform.position - cam.position;
        flatDelta.y = 0f;
        if (flatDelta.magnitude < killDistance)
        {
            transform.position = spawnPosition; 
            animator.speed = Random.Range(0.85f, 1.0f);
            speed = walkAnimSpeed * animator.speed;
        }
    }
}
