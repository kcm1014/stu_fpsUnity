using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;

    public float groundDrag;

    [Header("Jumping")]
    public float jumpForce;
    public float jumpCoolDown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    public Transform orientation;
    private bool exitingSlope;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState state;

    public enum MovementState{
        walking,
        sprinting,
        crouching,
        air
    }
    [SerializeField] TextMeshProUGUI text_speed;

    private void Awake() {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;
        startYScale = transform.localScale.y;
    }

    private void Update() {
        grounded =Physics.Raycast(transform.position,Vector3.down,playerHeight * 0.5f + 0.2f,whatIsGround);

        if(grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;

        MyInput();
        StateHandler();
        OnSlope();
        SpeedControl();
        

    }

    private void FixedUpdate() {
        MovePlayer();
    }


    private void MyInput(){
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if(Input.GetKeyDown(jumpKey) && grounded && readyToJump){
            readyToJump = false;
            Jump();

            Invoke(nameof(ResetJump),jumpCoolDown);
        }

        if(Input.GetKeyDown(crouchKey)){
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale,transform.localScale.z);
            rb.AddForce(Vector3.down * 5f,ForceMode.Impulse); // 높이 축소로 인하여 뜸...
        }
        if(Input.GetKeyUp(crouchKey)){
            transform.localScale = new Vector3(transform.localScale.x,startYScale,transform.localScale.z);
        }
    }

    private void StateHandler(){
        if(Input.GetKey(crouchKey)){
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }else if(grounded && Input.GetKey(sprintKey)){
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }else if(grounded){
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }else{
            state = MovementState.air;
        }

        
    }

    private void MovePlayer(){
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if(OnSlope() && !exitingSlope){
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);
            //펌핑 현상으로 인해서 눌러줘야 한다.
            if(rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }

        if(grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else if(!grounded)
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);


        rb.useGravity = !OnSlope();
    }

    private void SpeedControl(){
        if(OnSlope() && !exitingSlope){
            if(rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;
        }else{

            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if(flatVel.magnitude > moveSpeed){
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x,rb.velocity.y,limitedVel.z);
            }
            
        }
        text_speed.SetText("Speed : " + String.Format("{0:0.00}",rb.velocity.magnitude));
    }

    private void Jump(){
        exitingSlope = true;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce,ForceMode.Impulse);
    }
    private void ResetJump(){
        readyToJump = true;
        exitingSlope = false;
    }

    private bool OnSlope(){
        if(Physics.Raycast(transform.position,Vector3.down,out slopeHit,playerHeight * 0.5f + 0.3f)){
            float angle = Vector3.Angle(Vector3.up,slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }

        return false;
    }

    private Vector3 GetSlopeMoveDirection(){
        return Vector3.ProjectOnPlane(moveDirection,slopeHit.normal).normalized;
    }
}
